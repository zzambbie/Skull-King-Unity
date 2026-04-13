// ============================================================
// GameState.cs — 게임 상태 및 페이즈 정의
// ============================================================
using System.Collections.Generic;

namespace SkullKing.Data
{
    /// <summary>
    /// 게임 진행 페이즈
    /// </summary>
    public enum GamePhase
    {
        Lobby,          // 로비 (인원 설정)
        Dealing,        // 카드 분배 중
        Bidding,        // 비딩 페이즈
        Playing,        // 카드 제출 페이즈
        TrickResolve,   // 트릭 결과 판정
        RoundEnd,       // 라운드 종료 (점수 계산)
        GameEnd         // 게임 종료 (최종 결과)
    }

    /// <summary>
    /// 게임 전체 상태를 보관하는 클래스.
    /// GameManager가 이 상태를 갱신하며 게임을 진행합니다.
    /// </summary>
    public class GameState
    {
        // ---- 게임 설정 ----

        /// <summary>총 플레이어 수 (3~6)</summary>
        public int PlayerCount { get; set; }

        /// <summary>총 라운드 수 (기본 10)</summary>
        public int TotalRounds { get; set; } = 10;

        /// <summary>모든 플레이어 데이터</summary>
        public PlayerData[] Players { get; set; }

        // ---- 현재 진행 상태 ----

        /// <summary>현재 페이즈</summary>
        public GamePhase CurrentPhase { get; set; } = GamePhase.Lobby;

        /// <summary>현재 라운드 번호 (1-based)</summary>
        public int CurrentRound { get; set; }

        /// <summary>현재 트릭 번호 (1-based, 라운드 내)</summary>
        public int CurrentTrick { get; set; }

        /// <summary>이번 라운드에서 몇 번째 트릭까지 진행했는가</summary>
        public int TricksInRound => CurrentRound; // n라운드 = n트릭

        // ---- 트릭 진행 상태 ----

        /// <summary>이번 트릭의 선턴(리더) 플레이어 인덱스</summary>
        public int LeaderIndex { get; set; }

        /// <summary>현재 턴의 플레이어 인덱스</summary>
        public int CurrentPlayerIndex { get; set; }

        /// <summary>리드 수트 (첫 번째 숫자 카드의 색상)</summary>
        public CardSuit LeadSuit { get; set; } = CardSuit.None;

        /// <summary>이번 트릭에 제출된 카드들</summary>
        public List<PlayedCard> TrickCards { get; private set; } = new List<PlayedCard>();

        // ---- 라운드 관련 ----

        /// <summary>각 라운드의 선턴 시작 플레이어 (딜러 다음 사람)</summary>
        public int RoundStartPlayerIndex { get; set; }

        // ---- 메서드 ----

        /// <summary>새 라운드 준비</summary>
        /// <param name="roundNumber">라운드 번호</param>
        /// <param name="startPlayerIndex">이번 라운드의 선턴 시작 플레이어 인덱스</param>
        public void PrepareNewRound(int roundNumber, int startPlayerIndex = 0)
        {
            CurrentRound = roundNumber;
            CurrentTrick = 0;
            LeadSuit = CardSuit.None;
            TrickCards.Clear();

            // 모든 플레이어의 라운드 상태 초기화
            for (int i = 0; i < Players.Length; i++)
            {
                Players[i].ResetForNewRound();
            }

            // 선턴 시작 플레이어는 외부에서 지정 (이전 라운드 마지막 트릭 승자)
            RoundStartPlayerIndex = startPlayerIndex;
            LeaderIndex = RoundStartPlayerIndex;
        }

        /// <summary>새 트릭 준비</summary>
        public void PrepareNewTrick()
        {
            CurrentTrick++;
            TrickCards.Clear();
            LeadSuit = CardSuit.None;
            CurrentPlayerIndex = LeaderIndex;
        }

        /// <summary>다음 플레이어 인덱스 (시계 방향)</summary>
        public int GetNextPlayerIndex(int currentIndex)
        {
            return (currentIndex + 1) % PlayerCount;
        }

        /// <summary>현재 턴을 다음 플레이어로 넘김</summary>
        public void AdvanceToNextPlayer()
        {
            CurrentPlayerIndex = GetNextPlayerIndex(CurrentPlayerIndex);
        }

        /// <summary>모든 플레이어가 비딩을 완료했는가?</summary>
        public bool AllPlayersBid()
        {
            for (int i = 0; i < Players.Length; i++)
            {
                if (!Players[i].HasBid) return false;
            }
            return true;
        }

        /// <summary>이번 트릭에 모든 플레이어가 카드를 냈는가?</summary>
        public bool AllPlayersPlayedInTrick()
        {
            return TrickCards.Count >= PlayerCount;
        }

        /// <summary>이번 라운드의 모든 트릭이 끝났는가?</summary>
        public bool AllTricksCompleted()
        {
            return CurrentTrick >= TricksInRound;
        }

        /// <summary>모든 라운드가 끝났는가?</summary>
        public bool AllRoundsCompleted()
        {
            return CurrentRound >= TotalRounds;
        }
    }
}
