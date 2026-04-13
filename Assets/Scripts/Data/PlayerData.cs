// ============================================================
// PlayerData.cs — 플레이어 상태 데이터
// ============================================================
using System.Collections.Generic;

namespace SkullKing.Data
{
    /// <summary>
    /// 한 명의 플레이어 상태를 관리합니다.
    /// 인간/AI 모두 이 클래스를 사용합니다.
    /// </summary>
    public class PlayerData
    {
        /// <summary>플레이어 고유 인덱스 (0-based)</summary>
        public int Index { get; private set; }

        /// <summary>표시 이름</summary>
        public string Name { get; set; }

        /// <summary>인간 플레이어인가?</summary>
        public bool IsHuman { get; set; }

        /// <summary>현재 손패</summary>
        public List<CardData> Hand { get; private set; } = new List<CardData>();

        // ---- 라운드별 상태 ----

        /// <summary>이번 라운드의 비딩 수</summary>
        public int Bid { get; set; }

        /// <summary>비딩을 완료했는가?</summary>
        public bool HasBid { get; set; }

        /// <summary>이번 라운드에서 이긴 트릭 수</summary>
        public int TricksWon { get; set; }

        /// <summary>이번 라운드에서 획득한 보너스 점수 (Loot)</summary>
        public int BonusPoints { get; set; }

        /// <summary>이번 라운드에 형성된 보석 동맹(다른 플레이어의 인덱스) 목록</summary>
        public List<int> JewelAlliances { get; private set; } = new List<int>();

        // ---- 누적 점수 ----

        /// <summary>총 누적 점수</summary>
        public int TotalScore { get; set; }

        /// <summary>라운드별 점수 기록 (인덱스 = 라운드-1)</summary>
        public List<int> ScoreHistory { get; private set; } = new List<int>();

        // ---- 생성자 ----

        public PlayerData(int index, string name, bool isHuman)
        {
            Index = index;
            Name = name;
            IsHuman = isHuman;
        }

        /// <summary>
        /// 새 라운드 시작 시 라운드 관련 상태를 초기화합니다.
        /// </summary>
        public void ResetForNewRound()
        {
            Hand.Clear();
            Bid = 0;
            HasBid = false;
            TricksWon = 0;
            BonusPoints = 0;
            JewelAlliances.Clear();
        }

        /// <summary>
        /// 손패에서 카드를 제거합니다.
        /// </summary>
        public bool RemoveCardFromHand(CardData card)
        {
            for (int i = 0; i < Hand.Count; i++)
            {
                if (Hand[i].Id == card.Id)
                {
                    Hand.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 특정 수트의 카드를 손패에 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasSuit(CardSuit suit)
        {
            for (int i = 0; i < Hand.Count; i++)
            {
                if (Hand[i].Suit == suit && Hand[i].IsNumberCard)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 현재 상태를 문자열로 출력 (디버그용)
        /// </summary>
        public override string ToString()
        {
            string type = IsHuman ? "Human" : "AI";
            return $"[{type}] {Name} | Score: {TotalScore} | Bid: {Bid} | Won: {TricksWon} | Hand: {Hand.Count}장";
        }
    }
}
