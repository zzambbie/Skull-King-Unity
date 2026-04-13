// ============================================================
// GameManager.cs — 상태 머신 기반 게임 진행 관리자
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Core
{
    /// <summary>
    /// 게임의 전체 진행을 관리하는 중앙 컨트롤러.
    /// 상태 머신 패턴을 사용하여 각 페이즈를 순서대로 진행합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ============================================================
        // 이벤트 — UI Binding Layer에서 구독
        // ============================================================

        /// <summary>페이즈가 변경됨</summary>
        public event Action<GamePhase> OnPhaseChanged;

        /// <summary>새 라운드 시작</summary>
        public event Action<int> OnRoundStarted; // roundNumber

        /// <summary>카드 분배 완료</summary>
        public event Action OnCardsDealt;

        /// <summary>인간 플레이어에게 비딩 요청</summary>
        public event Action<PlayerData, int> OnBidRequested; // player, maxBid

        /// <summary>플레이어가 비딩을 완료함</summary>
        public event Action<PlayerData> OnPlayerBid;

        /// <summary>모든 비딩 완료</summary>
        public event Action OnAllBidsComplete;

        /// <summary>새 트릭 시작</summary>
        public event Action<int, int> OnTrickStarted; // trickNumber, leaderIndex

        /// <summary>인간 플레이어에게 카드 제출 요청</summary>
        public event Action<PlayerData, List<CardData>> OnCardPlayRequested; // player, playableCards

        /// <summary>타이그리스 모드 선택 요청 (인간 플레이어)</summary>
        public event Action<PlayerData> OnTigressChoiceRequested;

        /// <summary>플레이어가 카드를 제출함</summary>
        public event Action<PlayedCard> OnCardPlayed;

        /// <summary>트릭 결과 판정 완료</summary>
        public event Action<TrickResult> OnTrickResolved;

        /// <summary>라운드 종료 및 점수 계산</summary>
        public event Action<RoundScoreResult[]> OnRoundEnded;

        /// <summary>게임 종료</summary>
        public event Action<PlayerData> OnGameEnded; // winner

        /// <summary>현재 턴 플레이어 변경</summary>
        public event Action<int> OnCurrentPlayerChanged; // playerIndex

        // ============================================================
        // 게임 상태 (외부에서 읽기 전용 접근 가능)
        // ============================================================

        public GameState State { get; private set; }

        /// <summary>현재 게임이 진행 중인가?</summary>
        public bool IsGameRunning { get; private set; }

        // ============================================================
        // 내부 변수
        // ============================================================

        private Deck _deck;
        private AIController _ai;

        // 인간 플레이어의 입력 대기용
        private bool _waitingForHumanBid;
        private bool _waitingForHumanCard;
        private bool _waitingForTigressChoice;
        private int _humanBidValue;
        private CardData _humanSelectedCard;
        private TigressChoice _humanTigressChoice;

        // AI 턴 딜레이 (초)
        [SerializeField] private float _aiTurnDelay = 1.0f;

        // 트릭 사이 딜레이
        [SerializeField] private float _trickResolveDelay = 1.5f;

        // 라운드 간 선턴 이어받기용
        private int _lastTrickWinnerIndex = 0;
        private bool _isFirstRound = true;

        // 라운드 결과 확인 대기
        private bool _waitingForRoundConfirm;

        // ============================================================
        // 게임 시작
        // ============================================================

        /// <summary>
        /// 게임을 시작합니다. LobbyManager 또는 외부에서 호출합니다.
        /// </summary>
        /// <param name="playerCount">총 플레이어 수 (3~6)</param>
        /// <param name="humanPlayerIndex">인간 플레이어의 인덱스 (기본 0)</param>
        public void StartGame(int playerCount, int humanPlayerIndex = 0)
        {
            Debug.Log($"[GameManager] 게임 시작! 플레이어 {playerCount}명");

            // 게임 상태 초기화
            State = new GameState
            {
                PlayerCount = playerCount,
                TotalRounds = 10,
                Players = new PlayerData[playerCount]
            };

            // 판타지 테마 AI 이름 목록
            string[] aiNames = { "엘론드", "카르나", "발루르", "시리엔", "티리엘", "모르간", "보르스", "란슬롯" };
            var rnd = new System.Random();
            // 이름 섞기
            for (int i = 0; i < aiNames.Length; i++)
            {
                int swapIdx = rnd.Next(i, aiNames.Length);
                var temp = aiNames[i];
                aiNames[i] = aiNames[swapIdx];
                aiNames[swapIdx] = temp;
            }

            // 플레이어 생성 (초기 점수 20점)
            int aiNameIndex = 0;
            for (int i = 0; i < playerCount; i++)
            {
                bool isHuman = (i == humanPlayerIndex);
                string name = isHuman ? "Player" : aiNames[aiNameIndex++];
                State.Players[i] = new PlayerData(i, name, isHuman);
                State.Players[i].TotalScore = 20;
            }

            // 로직 초기화
            _deck = new Deck();
            _ai = new AIController();

            IsGameRunning = true;

            // 첫 라운드 시작
            StartCoroutine(GameLoop());
        }

        // ============================================================
        // 메인 게임 루프
        // ============================================================

        private IEnumerator GameLoop()
        {
            _isFirstRound = true;
            // 1라운드 첫 선플레이어도 완전히 랜덤하게 지정
            _lastTrickWinnerIndex = UnityEngine.Random.Range(0, State.PlayerCount); 
            Debug.Log($"[GameManager] 1라운드 첫 선: {State.Players[_lastTrickWinnerIndex].Name}");

            for (int round = 1; round <= State.TotalRounds; round++)
            {
                yield return StartCoroutine(RunRound(round));
                _isFirstRound = false;

                if (State.AllRoundsCompleted())
                    break;
            }

            // 게임 종료
            EndGame();
        }

        private IEnumerator RunRound(int roundNumber)
        {
            Debug.Log($"\n===== 라운드 {roundNumber} 시작 =====");

            // ---- 1. 딜링 페이즈 ----
            State.PrepareNewRound(roundNumber, _lastTrickWinnerIndex);
            ChangePhase(GamePhase.Dealing);
            OnRoundStarted?.Invoke(roundNumber);

            _deck.CreateFullDeck();
            _deck.Shuffle();
            _deck.DealToPlayers(State.Players, roundNumber);

            Debug.Log($"[Dealing] 각 플레이어에게 {roundNumber}장 분배 완료");
            OnCardsDealt?.Invoke();

            yield return new WaitForSeconds(0.5f);

            // ---- 2. 비딩 페이즈 ----
            ChangePhase(GamePhase.Bidding);
            yield return StartCoroutine(RunBiddingPhase(roundNumber));

            // ---- 3. 플레이 페이즈 (트릭들) ----
            ChangePhase(GamePhase.Playing);
            for (int trick = 1; trick <= roundNumber; trick++)
            {
                yield return StartCoroutine(RunTrick(trick));
            }

            // ---- 4. 라운드 종료 ----
            ChangePhase(GamePhase.RoundEnd);
            var scores = ScoreCalculator.CalculateAll(State.Players, roundNumber);

            Debug.Log($"===== 라운드 {roundNumber} 점수 =====");
            for (int i = 0; i < scores.Length; i++)
            {
                Debug.Log($"  {State.Players[i].Name}: {scores[i].Description} (누적: {State.Players[i].TotalScore})");
            }

            OnRoundEnded?.Invoke(scores);

            // 라운드 결과 확인 대기 (인간이 "다음" 버튼을 누를 때까지)
            _waitingForRoundConfirm = true;
            Debug.Log("[라운드 결과] 확인 대기 중... (다음 버튼을 눌러주세요)");
            while (_waitingForRoundConfirm)
            {
                yield return null;
            }
        }

        // ============================================================
        // 비딩 페이즈
        // ============================================================

        private IEnumerator RunBiddingPhase(int roundNumber)
        {
            Debug.Log("[Bidding] 비딩 페이즈 시작");

            // 선턴부터 시계 방향으로 비딩
            int currentIdx = State.RoundStartPlayerIndex;

            for (int i = 0; i < State.PlayerCount; i++)
            {
                var player = State.Players[currentIdx];
                State.CurrentPlayerIndex = currentIdx;
                OnCurrentPlayerChanged?.Invoke(currentIdx);

                if (player.IsHuman)
                {
                    // 인간 플레이어: 이벤트로 UI에 비딩 요청
                    _waitingForHumanBid = true;
                    OnBidRequested?.Invoke(player, roundNumber);

                    Debug.Log($"[Bidding] {player.Name}의 비딩을 기다리는 중...");

                    // 인간 입력 대기
                    while (_waitingForHumanBid)
                    {
                        yield return null;
                    }

                    player.Bid = _humanBidValue;
                }
                else
                {
                    // AI 비딩
                    yield return new WaitForSeconds(_aiTurnDelay * 0.5f);
                    player.Bid = _ai.DecideBid(player.Hand, State.PlayerCount);
                }

                player.HasBid = true;
                Debug.Log($"[Bidding] {player.Name} → {player.Bid}");
                OnPlayerBid?.Invoke(player);

                currentIdx = State.GetNextPlayerIndex(currentIdx);
            }

            OnAllBidsComplete?.Invoke();
            Debug.Log("[Bidding] 모든 비딩 완료!");
        }

        // ============================================================
        // 트릭 (한 턴) 진행
        // ============================================================

        private IEnumerator RunTrick(int trickNumber)
        {
            State.PrepareNewTrick();
            Debug.Log($"\n--- 트릭 {trickNumber} (선턴: {State.Players[State.LeaderIndex].Name}) ---");
            OnTrickStarted?.Invoke(trickNumber, State.LeaderIndex);

            int currentIdx = State.LeaderIndex;

            for (int i = 0; i < State.PlayerCount; i++)
            {
                var player = State.Players[currentIdx];
                State.CurrentPlayerIndex = currentIdx;
                OnCurrentPlayerChanged?.Invoke(currentIdx);

                // 플레이 가능한 카드 계산
                var playableCards = TrickResolver.GetPlayableCards(player.Hand, State.LeadSuit);

                CardData selectedCard;
                TigressChoice tigressMode = TigressChoice.NotApplicable;

                if (player.IsHuman)
                {
                    // 인간 플레이어: 카드 선택 요청
                    _waitingForHumanCard = true;
                    OnCardPlayRequested?.Invoke(player, playableCards);

                    Debug.Log($"[Play] {player.Name}의 카드 선택을 기다리는 중... (가능: {playableCards.Count}장)");

                    while (_waitingForHumanCard)
                    {
                        yield return null;
                    }

                    selectedCard = _humanSelectedCard;

                    // 타이그리스 선택 처리
                    if (selectedCard.Special == SpecialType.Tigress)
                    {
                        _waitingForTigressChoice = true;
                        OnTigressChoiceRequested?.Invoke(player);

                        while (_waitingForTigressChoice)
                        {
                            yield return null;
                        }

                        tigressMode = _humanTigressChoice;
                    }
                }
                else
                {
                    // AI 카드 선택
                    yield return new WaitForSeconds(_aiTurnDelay);

                    int remainingTricks = State.TricksInRound - State.CurrentTrick + 1;
                    selectedCard = _ai.ChooseCard(
                        player.Hand, playableCards, State.TrickCards,
                        State.LeadSuit, player.Bid, player.TricksWon, remainingTricks
                    );

                    // AI 타이그리스 선택
                    if (selectedCard.Special == SpecialType.Tigress)
                    {
                        int remaining = State.TricksInRound - State.CurrentTrick + 1;
                        tigressMode = _ai.DecideTigressMode(player.Bid, player.TricksWon, remaining);
                    }
                }

                // 카드 제출 처리
                player.RemoveCardFromHand(selectedCard);

                var playedCard = new PlayedCard
                {
                    PlayerIndex = currentIdx,
                    Card = selectedCard,
                    TigressMode = tigressMode
                };

                State.TrickCards.Add(playedCard);

                // 리드 수트 갱신 (첫 숫자 카드가 리드)
                if (State.LeadSuit == CardSuit.None)
                {
                    State.LeadSuit = TrickResolver.DetermineLeadSuit(State.TrickCards);
                }

                Debug.Log($"[Play] {player.Name} → {playedCard}");
                OnCardPlayed?.Invoke(playedCard);

                currentIdx = State.GetNextPlayerIndex(currentIdx);
            }

            // ---- 트릭 판정 ----
            yield return new WaitForSeconds(_trickResolveDelay * 0.5f);

            ChangePhase(GamePhase.TrickResolve);
            var result = TrickResolver.Resolve(State.TrickCards, State.LeadSuit);

            if (result.IsVoided)
            {
                Debug.Log("[Resolve] 트릭 무효화됨! (크라켄)");
                // 크라켄 무효 시: 가정 승자가 다음 선턴
                if (result.WouldBeWinnerIndex >= 0)
                {
                    State.LeaderIndex = result.WouldBeWinnerIndex;
                    _lastTrickWinnerIndex = result.WouldBeWinnerIndex;
                    Debug.Log($"[Resolve] 크라켄 없었으면 승자: {State.Players[result.WouldBeWinnerIndex].Name} → 다음 선턴");
                }
                // WouldBeWinnerIndex가 없으면 선턴 유지
            }
            else
            {
                var winner = State.Players[result.WinnerIndex];
                winner.TricksWon++;
                winner.BonusPoints += result.BonusPoints;
                State.LeaderIndex = result.WinnerIndex;
                _lastTrickWinnerIndex = result.WinnerIndex;

                Debug.Log($"[Resolve] 승자: {winner.Name}" +
                    (result.BonusPoints > 0 ? $" ({result.BonusDescription})" : ""));
            }

            // ---- 보석(Jewel) 동맹 결성 로직 ----
            if (!result.IsVoided)
            {
                var winner = State.Players[result.WinnerIndex];
                foreach (var pc in State.TrickCards)
                {
                    if (pc.Card.Special == SpecialType.Jewel)
                    {
                        var giver = State.Players[pc.PlayerIndex];
                        if (giver.Index != winner.Index)
                        {
                            giver.JewelAlliances.Add(winner.Index);
                            winner.JewelAlliances.Add(giver.Index);
                            Debug.Log($"[동맹 결성] 보석 카드 교환으로 인한 운명 공동체 형성: {giver.Name} ↔ {winner.Name}");
                        }
                    }
                }
            }

            OnTrickResolved?.Invoke(result);

            // 플레이 페이즈로 복귀 (다음 트릭 준비)
            if (State.CurrentTrick < State.TricksInRound)
            {
                ChangePhase(GamePhase.Playing);
            }

            yield return new WaitForSeconds(_trickResolveDelay * 0.5f);
        }

        // ============================================================
        // 게임 종료
        // ============================================================

        private void EndGame()
        {
            ChangePhase(GamePhase.GameEnd);
            IsGameRunning = false;

            // 최고 점수 플레이어 찾기
            PlayerData winner = State.Players[0];
            for (int i = 1; i < State.Players.Length; i++)
            {
                if (State.Players[i].TotalScore > winner.TotalScore)
                    winner = State.Players[i];
            }

            Debug.Log($"\n★★★ 게임 종료! 우승: {winner.Name} ({winner.TotalScore}점) ★★★");
            OnGameEnded?.Invoke(winner);
        }

        // ============================================================
        // 인간 플레이어 입력 수신 (UI → System)
        // ============================================================

        /// <summary>
        /// 인간 플레이어의 비딩 값을 제출합니다.
        /// UI의 비딩 패널에서 호출합니다.
        /// </summary>
        public void SubmitHumanBid(int bidValue)
        {
            if (!_waitingForHumanBid)
            {
                Debug.LogWarning("[GameManager] 비딩 대기 상태가 아닙니다!");
                return;
            }

            // 유효성 검사
            bidValue = Mathf.Clamp(bidValue, 0, State.CurrentRound);
            _humanBidValue = bidValue;
            _waitingForHumanBid = false;

            Debug.Log($"[Input] 인간 비딩: {bidValue}");
        }

        /// <summary>
        /// 인간 플레이어가 카드를 선택/제출합니다.
        /// UI의 카드 버튼에서 호출합니다.
        /// </summary>
        public void SubmitHumanCard(CardData card)
        {
            if (!_waitingForHumanCard)
            {
                Debug.LogWarning("[GameManager] 카드 제출 대기 상태가 아닙니다!");
                return;
            }

            _humanSelectedCard = card;
            _waitingForHumanCard = false;

            Debug.Log($"[Input] 인간 카드 제출: {card}");
        }

        /// <summary>
        /// 인간 플레이어의 타이그리스 모드 선택.
        /// </summary>
        public void SubmitTigressChoice(TigressChoice choice)
        {
            if (!_waitingForTigressChoice)
            {
                Debug.LogWarning("[GameManager] 타이그리스 선택 대기 상태가 아닙니다!");
                return;
            }

            _humanTigressChoice = choice;
            _waitingForTigressChoice = false;

            Debug.Log($"[Input] 타이그리스 선택: {choice}");
        }

        /// <summary>
        /// 라운드 결과 확인 후 다음으로 진행합니다.
        /// UI의 "다음" 버튼에서 호출합니다.
        /// </summary>
        public void SubmitRoundConfirm()
        {
            if (!_waitingForRoundConfirm)
            {
                Debug.LogWarning("[GameManager] 라운드 결과 확인 대기 상태가 아닙니다!");
                return;
            }

            _waitingForRoundConfirm = false;
            Debug.Log("[Input] 라운드 결과 확인 → 다음 라운드 진행");
        }

        // ============================================================
        // 내부 유틸리티
        // ============================================================

        private void ChangePhase(GamePhase newPhase)
        {
            State.CurrentPhase = newPhase;
            Debug.Log($"[Phase] → {newPhase}");
            OnPhaseChanged?.Invoke(newPhase);
        }
    }
}
