// ============================================================
// ScoreCalculator.cs — 라운드 점수 계산
// skull_king_core_rules.md 3번 항목의 구현
// ============================================================
using SkullKing.Data;

namespace SkullKing.Logic
{
    /// <summary>
    /// 라운드 점수 계산 결과
    /// </summary>
    public struct RoundScoreResult
    {
        /// <summary>플레이어 인덱스</summary>
        public int PlayerIndex;

        /// <summary>비딩 수</summary>
        public int Bid;

        /// <summary>실제 이긴 트릭 수</summary>
        public int TricksWon;

        /// <summary>비딩 성공 여부</summary>
        public bool BidSuccess;

        /// <summary>기본 점수 (비딩 성공/실패에 따른 점수)</summary>
        public int BaseScore;

        /// <summary>보너스 점수 (비딩 성공 시에만 가산)</summary>
        public int BonusScore;

        /// <summary>이번 라운드 최종 점수 (Base + Bonus)</summary>
        public int RoundTotal => BaseScore + BonusScore;

        /// <summary>상세 설명 (디버그/UI용)</summary>
        public string Description;
    }

    /// <summary>
    /// 라운드 종료 시 각 플레이어의 점수를 계산합니다.
    /// MonoBehaviour가 아닌 순수 C# 클래스입니다.
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>
        /// 한 플레이어의 라운드 점수를 계산합니다.
        /// </summary>
        /// <param name="player">플레이어 데이터</param>
        /// <param name="roundNumber">현재 라운드 번호 (1-based)</param>
        /// <returns>점수 계산 결과</returns>
        public static RoundScoreResult Calculate(PlayerData player, int roundNumber)
        {
            int bid = player.Bid;
            int wins = player.TricksWon;
            bool success = (bid == wins);

            int baseScore;
            int bonusScore = 0;
            string desc;

            if (success)
            {
                // ---- 비딩 성공 ----
                if (bid == 0)
                {
                    // 0비딩 성공: 라운드 번호 × 10
                    baseScore = roundNumber * 10;
                    desc = $"0비딩 성공! +{baseScore}점";
                }
                else
                {
                    // 일반 성공: 비딩 수 × 20
                    baseScore = bid * 20;
                    desc = $"비딩 {bid} 성공! +{baseScore}점";
                }

                // 보너스는 비딩 성공 시에만 가산
                bonusScore = player.BonusPoints;
                if (bonusScore > 0)
                {
                    desc += $" (보너스 +{bonusScore}점)";
                }
            }
            else
            {
                // ---- 비딩 실패 ----
                if (bid == 0)
                {
                    // 0비딩 실패: 라운드 번호 × -10
                    baseScore = roundNumber * -10;
                    desc = $"0비딩 실패... {baseScore}점";
                }
                else
                {
                    // 일반 실패: |비딩 - 실제 승리| × -10
                    int diff = System.Math.Abs(bid - wins);
                    baseScore = diff * -10;
                    desc = $"비딩 {bid}, 실제 {wins} — 실패! {baseScore}점";
                }
                // 실패 시 보너스 없음
                bonusScore = 0;
            }

            return new RoundScoreResult
            {
                PlayerIndex = player.Index,
                Bid = bid,
                TricksWon = wins,
                BidSuccess = success,
                BaseScore = baseScore,
                BonusScore = bonusScore,
                Description = desc
            };
        }

        /// <summary>
        /// 모든 플레이어의 라운드 점수를 계산하고 누적합니다.
        /// </summary>
        /// <param name="players">플레이어 배열</param>
        /// <param name="roundNumber">현재 라운드 번호</param>
        /// <returns>모든 플레이어의 점수 결과</returns>
        public static RoundScoreResult[] CalculateAll(PlayerData[] players, int roundNumber)
        {
            var results = new RoundScoreResult[players.Length];
            
            // 1단계: 모든 플레이어의 기본 점수 계산
            for (int i = 0; i < players.Length; i++)
            {
                results[i] = Calculate(players[i], roundNumber);
            }

            // 2단계: 보석/전리품 동맹 보너스 조건 학인 및 최종 스코어 반영
            for (int i = 0; i < players.Length; i++)
            {
                if (results[i].BidSuccess)
                {
                    int allianceBonus = 0;
                    foreach (int allyIdx in players[i].JewelAlliances)
                    {
                        // 동맹을 맺은 양측이 모두 비딩에 성공했을 때만 동맹 보너스 인정
                        if (results[allyIdx].BidSuccess)
                        {
                            allianceBonus += 20;
                        }
                    }

                    if (allianceBonus > 0)
                    {
                        results[i].BonusScore += allianceBonus;
                        results[i].Description += $" (보석 동맹 +{allianceBonus}점)";
                    }
                }

                // 누적 점수 갱신 (최소 0점 유지)
                players[i].TotalScore = System.Math.Max(0, players[i].TotalScore + results[i].RoundTotal);

                // 점수 이력에 기록
                players[i].ScoreHistory.Add(results[i].RoundTotal);
            }
            return results;
        }
    }
}
