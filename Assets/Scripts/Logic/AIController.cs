// ============================================================
// AIController.cs — AI 비딩 및 카드 제출 로직
// skull_king_core_rules.md 4번 항목의 구현
// ============================================================
using System;
using System.Collections.Generic;
using SkullKing.Data;

namespace SkullKing.Logic
{
    /// <summary>
    /// AI 플레이어의 의사결정을 담당합니다.
    /// MonoBehaviour가 아닌 순수 C# 클래스입니다.
    /// </summary>
    public class AIController
    {
        private Random _rng;

        public AIController(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ============================================================
        // 비딩 로직
        // ============================================================

        /// <summary>
        /// AI가 손패를 분석하여 비딩 수를 결정합니다.
        /// </summary>
        /// <param name="hand">AI의 손패</param>
        /// <param name="playerCount">전체 플레이어 수</param>
        /// <returns>비딩 수</returns>
        public int DecideBid(List<CardData> hand, int playerCount)
        {
            if (hand.Count == 0) return 0;

            float winPower = 0f;

            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];

                if (card.Special == SpecialType.SkullKing)
                {
                    winPower += 0.95f; // 거의 확정 승리
                }
                else if (card.Special == SpecialType.Pirate)
                {
                    winPower += 0.8f;
                }
                else if (card.Special == SpecialType.Mermaid)
                {
                    winPower += 0.6f; // 해적에게 질 수 있으므로 약간 낮음
                }
                else if (card.Special == SpecialType.Tigress)
                {
                    winPower += 0.5f; // 유연하게 사용 가능
                }
                else if (card.Special == SpecialType.Kraken)
                {
                    // 크라켄은 깽판 카드 — 비딩에 도움이 안 됨
                    winPower += 0.0f;
                }
                else if (card.Special == SpecialType.WhiteWhale)
                {
                    winPower += 0.3f; // 숫자 높으면 유리하지만 불확실
                }
                else if (card.IsEscapeCard)
                {
                    // 도주 = 확정 패배, 비딩에 기여하지 않음
                    winPower += 0.0f;
                }
                else if (card.IsBlackNumberCard)
                {
                    // 검정 숫자카드는 매우 강력
                    float blackPower = card.Number / 14f; // 1~14를 0.07~1.0으로 정규화
                    winPower += 0.6f + blackPower * 0.35f; // 0.6 ~ 0.95
                }
                else if (card.IsNumberCard)
                {
                    // 일반 색상 숫자 카드
                    float numberPower = card.Number / 14f;
                    // 높은 숫자일수록 승률 높음, 하지만 검정에 밀림
                    float adjustedPower = numberPower * 0.5f; // 최대 0.5 (14번은 0.5)
                    // 플레이어 수가 많을수록 이길 확률 낮음
                    float playerPenalty = (playerCount - 2) * 0.05f;
                    winPower += Math.Max(0f, adjustedPower - playerPenalty);
                }
            }

            // 예상 승리 횟수
            int expectedWins = (int)Math.Round(winPower);

            // 약간의 랜덤 변동 (-1 ~ +1)
            int variation = _rng.Next(-1, 2); // -1, 0, 1
            expectedWins = Math.Max(0, Math.Min(hand.Count, expectedWins + variation));

            return expectedWins;
        }

        // ============================================================
        // 카드 선택 로직
        // ============================================================

        /// <summary>
        /// AI가 이번 트릭에 낼 카드를 선택합니다.
        /// </summary>
        /// <param name="hand">AI의 현재 손패</param>
        /// <param name="playableCards">낼 수 있는 카드 목록</param>
        /// <param name="trickCards">이미 제출된 카드들</param>
        /// <param name="leadSuit">리드 수트</param>
        /// <param name="bid">AI의 비딩 수</param>
        /// <param name="tricksWon">현재까지 이긴 트릭 수</param>
        /// <param name="remainingTricks">남은 트릭 수</param>
        /// <returns>선택한 카드</returns>
        public CardData ChooseCard(
            List<CardData> hand,
            List<CardData> playableCards,
            List<PlayedCard> trickCards,
            CardSuit leadSuit,
            int bid,
            int tricksWon,
            int remainingTricks)
        {
            if (playableCards.Count == 1)
                return playableCards[0];

            int tricksNeeded = bid - tricksWon;
            bool wantToWin = tricksNeeded > 0;
            bool mustLose = tricksNeeded <= 0; // 비딩 이미 달성 또는 초과

            if (wantToWin)
            {
                return ChooseOffensiveCard(playableCards, trickCards, leadSuit, tricksNeeded, remainingTricks);
            }
            else
            {
                return ChooseDefensiveCard(playableCards, trickCards, leadSuit);
            }
        }

        /// <summary>
        /// 타이그리스를 낼 때 해적/도주 중 어떤 모드로 낼지 결정합니다.
        /// </summary>
        public TigressChoice DecideTigressMode(int bid, int tricksWon, int remainingTricks)
        {
            int tricksNeeded = bid - tricksWon;

            // 이길 필요가 있으면 해적, 아니면 도주
            if (tricksNeeded > 0)
                return TigressChoice.Pirate;
            else
                return TigressChoice.Escape;
        }

        // ---- 공격적 카드 선택 (이기고 싶을 때) ----
        private CardData ChooseOffensiveCard(
            List<CardData> playableCards,
            List<PlayedCard> trickCards,
            CardSuit leadSuit,
            int tricksNeeded,
            int remainingTricks)
        {
            // 우선순위: 확실히 이기는 카드를 최소한의 비용으로 사용

            // 1. 선턴(첫 제출)이면 → 높은 숫자 검정 카드로 리드
            if (trickCards.Count == 0)
            {
                var bestBlack = FindBestNumberCard(playableCards, CardSuit.Black);
                if (bestBlack.HasValue) return bestBlack.Value;

                // 검정 없으면 가장 높은 숫자 카드
                var bestAny = FindBestNumberCardAnySuit(playableCards);
                if (bestAny.HasValue) return bestAny.Value;
            }

            // 2. 현재 트릭에서 가장 쉽게 이기는 카드 선택
            // 특수 카드 확인: 해적 > 스컬킹 순으로 (스컬킹은 최후의 수단으로 아끼기)
            bool trickHasSpecial = HasSpecialInTrick(trickCards);

            // 트릭에 스컬킹이 있으면 인어로 잡기
            if (HasSpecialInTrick(trickCards, SpecialType.SkullKing))
            {
                var mermaid = FindCard(playableCards, SpecialType.Mermaid);
                if (mermaid.HasValue) return mermaid.Value;
            }

            // 해적으로 이기기
            var pirate = FindCard(playableCards, SpecialType.Pirate);
            if (pirate.HasValue && !HasSpecialInTrick(trickCards, SpecialType.SkullKing))
                return pirate.Value;

            // 타이그리스(해적 모드)
            var tigress = FindCard(playableCards, SpecialType.Tigress);
            if (tigress.HasValue && !HasSpecialInTrick(trickCards, SpecialType.SkullKing))
                return tigress.Value;

            // 검정 높은 숫자
            var black = FindBestNumberCard(playableCards, CardSuit.Black);
            if (black.HasValue) return black.Value;

            // 리드 수트 높은 숫자 (이미 나온 카드보다 높아야 함)
            if (leadSuit != CardSuit.None)
            {
                var leadCard = FindBestNumberCard(playableCards, leadSuit);
                if (leadCard.HasValue) return leadCard.Value;
            }

            // 스컬킹 (최후의 보루)
            var sk = FindCard(playableCards, SpecialType.SkullKing);
            if (sk.HasValue) return sk.Value;

            // 아무거나 높은 카드
            var anyBest = FindBestNumberCardAnySuit(playableCards);
            if (anyBest.HasValue) return anyBest.Value;

            // 정말 낼 카드가 마땅치 않으면 첫 번째 카드
            return playableCards[0];
        }

        // ---- 방어적 카드 선택 (일부러 지고 싶을 때) ----
        private CardData ChooseDefensiveCard(
            List<CardData> playableCards,
            List<PlayedCard> trickCards,
            CardSuit leadSuit)
        {
            // 도주 카드가 있으면 사용
            var escape = FindCard(playableCards, SpecialType.Escape);
            if (escape.HasValue) return escape.Value;

            // 타이그리스(도주 모드)
            var tigress = FindCard(playableCards, SpecialType.Tigress);
            if (tigress.HasValue) return tigress.Value;

            // 크라켄으로 트릭 무효화 (아무도 안 이김)
            var kraken = FindCard(playableCards, SpecialType.Kraken);
            if (kraken.HasValue) return kraken.Value;

            // 가장 낮은 숫자 카드 (리드 수트 우선)
            var worst = FindWorstNumberCard(playableCards, leadSuit);
            if (worst.HasValue) return worst.Value;

            // 정말 선택지가 없으면 첫 번째 카드
            return playableCards[0];
        }

        // ---- 헬퍼 메서드들 ----

        private CardData? FindCard(List<CardData> cards, SpecialType type)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Special == type) return cards[i];
            }
            return null;
        }

        private CardData? FindBestNumberCard(List<CardData> cards, CardSuit suit)
        {
            CardData? best = null;
            int bestNum = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].IsNumberCard && cards[i].Suit == suit && cards[i].Number > bestNum)
                {
                    bestNum = cards[i].Number;
                    best = cards[i];
                }
            }
            return best;
        }

        private CardData? FindBestNumberCardAnySuit(List<CardData> cards)
        {
            CardData? best = null;
            int bestScore = -1;
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].IsNumberCard) continue;
                // 검정 > 나머지 색, 같은 색이면 숫자 높은 게 좋음
                int score = cards[i].Number + (cards[i].Suit == CardSuit.Black ? 100 : 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cards[i];
                }
            }
            return best;
        }

        private CardData? FindWorstNumberCard(List<CardData> cards, CardSuit leadSuit)
        {
            CardData? worst = null;
            int worstScore = int.MaxValue;
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].IsNumberCard) continue;
                // 리드 수트가 아닌 색의 낮은 숫자가 가장 약함
                int score = cards[i].Number;
                if (cards[i].Suit == CardSuit.Black) score += 200;
                else if (cards[i].Suit == leadSuit) score += 100;
                if (score < worstScore)
                {
                    worstScore = score;
                    worst = cards[i];
                }
            }
            return worst;
        }

        private bool HasSpecialInTrick(List<PlayedCard> trickCards)
        {
            for (int i = 0; i < trickCards.Count; i++)
            {
                if (trickCards[i].Card.IsSpecialCard) return true;
            }
            return false;
        }

        private bool HasSpecialInTrick(List<PlayedCard> trickCards, SpecialType type)
        {
            for (int i = 0; i < trickCards.Count; i++)
            {
                if (trickCards[i].Card.Special == type) return true;
            }
            return false;
        }
    }
}
