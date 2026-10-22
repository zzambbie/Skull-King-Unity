#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using SkullKing.UI;
using SkullKing.Data;

namespace SkullKing.Editor
{
    public static class CressionTrickHistoryChecks
    {
        private static int _checks;
        public static void Run() => Debug.Log("[Trick History Checks] " + RunChecks() + "개 통과. 카드 호버/Alt와 배치는 Unity에서 확인하세요.");
        public static int RunChecks()
        {
            _checks = 0;
            var history = new PublicTrickHistory();
            Check(history.Current.Entries.Count == 0 && history.Tricks.Count == 0);
            history.Begin(1, 1);
            Check(history.Current.Round == 1 && history.Current.Number == 1);
            Check(history.Add(Card(2, 20, 14), "가나다"));
            Check(history.Current.Entries[0].owner == "가나다");
            Check(history.Current.Entries[0].played.Card.Id == 20);
            Check(!history.Add(Card(2, 21, 3), "중복 좌석"));
            Check(history.Current.Entries.Count == 1);
            Check(history.Add(Card(0, 30, 1), "둘째"));
            Check(history.Current.Entries[1].played.PlayerIndex == 0);
            history.SetLeader(0, false);
            Check(history.Current.LeaderIndex == 0 && !history.Current.Voided);
            history.SetLeader(1, false);
            Check(history.Current.LeaderIndex == 1);
            history.SetLeader(0, true);
            Check(history.Current.Voided && history.Current.LeaderIndex == -1);
            history.SetLeader(0, false);
            Check(!history.Current.Voided && history.Current.LeaderIndex == 0);
            history.Begin(1, 2);
            Check(history.Current.Entries.Count == 0 && history.Tricks[0].Entries.Count == 2);
            Check(history.Tricks[0].Number == 1 && history.Tricks[0].LeaderIndex == 0);
            history.Add(Card(1, 40, 8), "새 트릭");
            Check(history.Tricks[0].Entries.Count == 2 && history.Tricks[0].Entries[0].played.Card.Id == 20);
            history.SetLeader(99, false);
            Check(history.Current.LeaderIndex == -1);
            history.Begin(2, 1);
            Check(history.Tricks.Count == 1 && history.Current.Round == 2);
            for (int i = 0; i < 6; i++) Check(history.Add(Card(i, 100 + i, i + 1), "P" + i));
            Check(!history.Add(Card(6, 106, 7), "일곱째"));
            Check(history.Current.Entries.Count == 6);
            history.Begin(2, 2);
            history.Begin(2, 3);
            Check(history.Tricks[0].Entries.Count == 6 && history.Tricks.Count == 3);
            history.BeginRound(10);
            Check(history.Tricks.Count == 0 && history.Current.Entries.Count == 0);
            for (int trick = 1; trick <= 10; trick++)
            {
                history.Begin(10, trick);
                for (int seat = 0; seat < 6; seat++)
                    Check(history.Add(Card(seat, trick * 10 + seat, seat + 1), "P" + seat));
                history.SetLeader(trick % 6, false);
                history.Begin(10, trick);
                Check(history.Tricks.Count == trick && history.Current.Entries.Count == 6);
            }
            Check(history.Tricks.Count == 10);
            for (int i = 0; i < 10; i++)
            {
                Check(history.Tricks[i].Round == 10 && history.Tricks[i].Number == i + 1);
                Check(history.Tricks[i].Entries[0].played.Card.Id == (i + 1) * 10);
                Check(history.Tricks[i].LeaderIndex == (i + 1) % 6);
            }
            history.BeginRound(10);
            Check(history.Tricks.Count == 10);
            history.BeginRound(11);
            Check(history.Tricks.Count == 0 && history.Current.Number == 0);
            return _checks;
        }
        private static PlayedCard Card(int seat, int id, int number)
            => new PlayedCard { PlayerIndex = seat, Card = CardData.CreateNumberCard(id, CardSuit.Green, number) };
        private static void Check(bool result)
        {
            _checks++;
            if (!result) throw new InvalidOperationException("Trick history check failed #" + _checks);
        }
    }
}
#endif
