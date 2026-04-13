// ============================================================
// TrickResolver.cs — 트릭 승자 판정 (핵심 상성 규칙)
// skull_king_core_rules.md 2번 항목의 완전한 구현
// ============================================================
using System.Collections.Generic;
using SkullKing.Data;

namespace SkullKing.Logic
{
    /// <summary>
    /// 트릭 판정 결과
    /// </summary>
    public struct TrickResult
    {
        /// <summary>승자의 플레이어 인덱스 (-1이면 승자 없음 = 크라켄 무효화)</summary>
        public int WinnerIndex;

        /// <summary>트릭이 무효화되었는가 (크라켄)</summary>
        public bool IsVoided;

        /// <summary>이 트릭에서 발생한 보너스 점수</summary>
        public int BonusPoints;

        /// <summary>보너스 상세 설명 (디버그/UI용)</summary>
        public string BonusDescription;

        /// <summary>크라켄 무효 시, 크라켄이 없었더라면 이겼을 플레이어 (-1이면 없음)</summary>
        public int WouldBeWinnerIndex;

        public static TrickResult Voided(int wouldBeWinner = -1)
        {
            return new TrickResult
            {
                WinnerIndex = -1,
                IsVoided = true,
                BonusPoints = 0,
                BonusDescription = "크라켄 — 트릭 무효!",
                WouldBeWinnerIndex = wouldBeWinner
            };
        }
    }

    /// <summary>
    /// 트릭(턴)의 승자를 결정하는 핵심 판정 엔진.
    /// MonoBehaviour가 아닌 순수 C# 클래스입니다.
    /// </summary>
    public static class TrickResolver
    {
        /// <summary>
        /// 트릭에 제출된 카드들을 분석하여 승자를 결정합니다.
        /// </summary>
        /// <param name="playedCards">제출된 카드 리스트 (제출 순서)</param>
        /// <param name="leadSuit">리드 수트</param>
        /// <returns>판정 결과</returns>
        public static TrickResult Resolve(List<PlayedCard> playedCards, CardSuit leadSuit)
        {
            if (playedCards == null || playedCards.Count == 0)
            {
                return TrickResult.Voided();
            }

            // ============================================
            // 1단계: 크라켄 체크 — 트릭 즉시 무효화
            // 단, 크라켄이 없었을 때의 가정 승자도 계산
            // ============================================
            if (HasSpecial(playedCards, SpecialType.Kraken))
            {
                // 크라켄을 제외하고 재판정하여 가정 승자 결정
                var withoutKraken = new List<PlayedCard>();
                for (int i = 0; i < playedCards.Count; i++)
                {
                    if (playedCards[i].Card.Special != SpecialType.Kraken)
                        withoutKraken.Add(playedCards[i]);
                }

                int wouldBeWinner = -1;
                if (withoutKraken.Count > 0)
                {
                    var hypothetical = Resolve(withoutKraken, leadSuit);
                    wouldBeWinner = hypothetical.WinnerIndex;
                }
                else
                {
                    // 크라켄만 있는 경우 (불가능하지만 안전장치)
                    wouldBeWinner = playedCards[0].PlayerIndex;
                }

                return TrickResult.Voided(wouldBeWinner);
            }

            // ============================================
            // 2단계: 백경 체크 — 모든 특수 능력 무시, 숫자만으로 판정
            // ============================================
            if (HasSpecial(playedCards, SpecialType.WhiteWhale))
            {
                return ResolveWhiteWhale(playedCards);
            }

            // ============================================
            // 3단계: 특수 카드 상성 판정
            // ============================================
            bool hasSkullKing = HasSpecial(playedCards, SpecialType.SkullKing);
            bool hasMermaid = HasSpecial(playedCards, SpecialType.Mermaid);
            bool hasPirateOrTigressPirate = HasPirateRole(playedCards);

            // 스컬킹 + 인어 둘 다 있는 경우 → 인어 승리 (유일하게 스컬킹을 잡음)
            if (hasSkullKing && hasMermaid)
            {
                int mermaidIdx = FindFirstSpecial(playedCards, SpecialType.Mermaid);
                return new TrickResult
                {
                    WinnerIndex = playedCards[mermaidIdx].PlayerIndex,
                    IsVoided = false,
                    BonusPoints = 0,
                    BonusDescription = "인어가 스컬킹 포획 (보너스 삭제)"
                };
            }

            // 스컬킹만 있는 경우 → 스컬킹 승리
            if (hasSkullKing)
            {
                int skIdx = FindFirstSpecial(playedCards, SpecialType.SkullKing);
                return new TrickResult
                {
                    WinnerIndex = playedCards[skIdx].PlayerIndex,
                    IsVoided = false,
                    BonusPoints = 0,
                    BonusDescription = ""
                };
            }

            // 해적(또는 타이그리스 해적 모드)이 있는 경우 → 첫 번째 해적 승리
            if (hasPirateOrTigressPirate)
            {
                int pirateIdx = FindFirstPirateRole(playedCards);
                return new TrickResult
                {
                    WinnerIndex = playedCards[pirateIdx].PlayerIndex,
                    IsVoided = false,
                    BonusPoints = 0,
                    BonusDescription = ""
                };
            }

            // 인어만 있는 경우 (스컬킹/해적 없음) → 첫 번째 인어 승리
            if (hasMermaid)
            {
                int mermaidIdx = FindFirstSpecial(playedCards, SpecialType.Mermaid);
                return new TrickResult
                {
                    WinnerIndex = playedCards[mermaidIdx].PlayerIndex,
                    IsVoided = false,
                    BonusPoints = 0,
                    BonusDescription = ""
                };
            }

            // ============================================
            // 4단계: 숫자 카드 판정 (검정 > 리드 수트 > 나머지)
            // ============================================
            return ResolveNumberCards(playedCards, leadSuit);
        }

        // ---- 백경: 순수 숫자 최대값 판정 ----
        private static TrickResult ResolveWhiteWhale(List<PlayedCard> playedCards)
        {
            int bestIdx = -1;
            int bestNumber = -1;

            for (int i = 0; i < playedCards.Count; i++)
            {
                var pc = playedCards[i];
                // 숫자 카드만 고려 (특수 카드는 숫자 0이므로 무시됨)
                int num = pc.Card.Number;
                if (num > bestNumber)
                {
                    bestNumber = num;
                    bestIdx = i;
                }
            }

            // 만약 숫자 카드가 하나도 없으면 (전부 도주 등) → 백경을 낸 첫 플레이어라도 승리
            if (bestIdx == -1 || bestNumber <= 0)
            {
                int whaleIdx = FindFirstSpecial(playedCards, SpecialType.WhiteWhale);
                bestIdx = whaleIdx >= 0 ? whaleIdx : 0;
            }

            return new TrickResult
            {
                WinnerIndex = playedCards[bestIdx].PlayerIndex,
                IsVoided = false,
                BonusPoints = 0,
                BonusDescription = "백경 — 순수 숫자 대결!"
            };
        }

        // ---- 숫자 카드끼리 비교 ----
        private static TrickResult ResolveNumberCards(List<PlayedCard> playedCards, CardSuit leadSuit)
        {
            int bestIdx = -1;
            int bestPriority = -1; // 높을수록 강함
            int bestNumber = -1;

            for (int i = 0; i < playedCards.Count; i++)
            {
                var pc = playedCards[i];

                // 도주 카드 / 타이그리스 도주 → 무조건 짐
                if (pc.ActsAsEscape || pc.Card.Special == SpecialType.Escape)
                    continue;

                // 숫자 카드만 비교
                if (!pc.Card.IsNumberCard)
                    continue;

                int priority = GetSuitPriority(pc.Card.Suit, leadSuit);
                int number = pc.Card.Number;

                // 더 높은 우선순위의 수트 or 같은 수트에서 더 높은 숫자
                if (priority > bestPriority || (priority == bestPriority && number > bestNumber))
                {
                    bestPriority = priority;
                    bestNumber = number;
                    bestIdx = i;
                }
            }

            // 모든 카드가 도주인 경우 → 선턴(첫 번째 카드) 승리
            if (bestIdx == -1)
            {
                return new TrickResult
                {
                    WinnerIndex = playedCards[0].PlayerIndex,
                    IsVoided = false,
                    BonusPoints = 0,
                    BonusDescription = "모두 도주 — 선턴 승리"
                };
            }

            return new TrickResult
            {
                WinnerIndex = playedCards[bestIdx].PlayerIndex,
                IsVoided = false,
                BonusPoints = 0,
                BonusDescription = ""
            };
        }

        /// <summary>
        /// 수트 우선순위. 검정(3) > 리드 수트(2) > 나머지(1) > None(0)
        /// </summary>
        private static int GetSuitPriority(CardSuit suit, CardSuit leadSuit)
        {
            if (suit == CardSuit.Black) return 3;
            if (suit == leadSuit && leadSuit != CardSuit.None) return 2;
            if (suit != CardSuit.None) return 1;
            return 0;
        }

        // ---- 헬퍼 메서드들 ----

        private static bool HasSpecial(List<PlayedCard> cards, SpecialType type)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Card.Special == type) return true;
            }
            return false;
        }

        private static bool HasPirateRole(List<PlayedCard> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].ActsAsPirate) return true;
            }
            return false;
        }

        private static int FindFirstSpecial(List<PlayedCard> cards, SpecialType type)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Card.Special == type) return i;
            }
            return -1;
        }

        private static int FindFirstPirateRole(List<PlayedCard> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].ActsAsPirate) return i;
            }
            return -1;
        }

        private static int CountPirateRoles(List<PlayedCard> cards)
        {
            int count = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].ActsAsPirate) count++;
            }
            return count;
        }

        // ============================================
        // 플레이 가능한 카드 필터링 (리드 수트 의무)
        // ============================================

        /// <summary>
        /// 플레이어가 현재 낼 수 있는 카드 목록을 반환합니다.
        /// 리드 수트 의무: 리드 수트 카드가 있으면 반드시 그 수트를 내야 합니다.
        /// 단, 특수 카드는 언제든 낼 수 있습니다.
        /// </summary>
        public static List<CardData> GetPlayableCards(List<CardData> hand, CardSuit leadSuit)
        {
            // 리드 수트가 없거나(첫 번째 제출자), 손패에 리드 수트 숫자 카드가 없으면 전부 낼 수 있음
            if (leadSuit == CardSuit.None || !HasSuitInHand(hand, leadSuit))
            {
                return new List<CardData>(hand);
            }

            // 리드 수트 의무: 리드 수트 숫자 카드 + 모든 특수 카드
            var playable = new List<CardData>();
            for (int i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                // 특수 카드는 항상 낼 수 있음
                if (card.IsSpecialCard)
                {
                    playable.Add(card);
                }
                // 리드 수트와 같은 색의 숫자 카드
                else if (card.Suit == leadSuit)
                {
                    playable.Add(card);
                }
            }
            return playable;
        }

        /// <summary>
        /// 리드 수트를 결정합니다.
        /// 트릭의 첫 번째 숫자 카드의 수트가 리드 수트가 됩니다.
        /// </summary>
        public static CardSuit DetermineLeadSuit(List<PlayedCard> playedCards)
        {
            for (int i = 0; i < playedCards.Count; i++)
            {
                var card = playedCards[i].Card;
                if (card.IsNumberCard)
                {
                    return card.Suit;
                }
            }
            return CardSuit.None; // 숫자 카드가 아직 없음
        }

        private static bool HasSuitInHand(List<CardData> hand, CardSuit suit)
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == suit && hand[i].IsNumberCard)
                    return true;
            }
            return false;
        }
    }
}
