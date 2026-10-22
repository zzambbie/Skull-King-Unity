#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SkullKing.Data;
using SkullKing.UI;

namespace SkullKing.Editor
{
    public static class CressionWhalePresentationChecks
    {
        private static int _checks;
        private static int _id;
        public static void Run()
        {
            int count = RunChecks();
            Debug.Log("[Whale Checks] " + count + "개 통과. 실제 날개 동작은 GameScene에서 확인하세요.");
        }

        public static int RunChecks()
        {
            _checks = 0;
            _id = 1000;
            Check("empty", TrickPresentationRules.WinnerCardIndex(new List<PlayedCard>()) == -1);
            Check("null", TrickPresentationRules.WinnerCardIndex(null) == -1);
            Winner(0, N(0, CardSuit.Green, 4));
            Winner(0, N(0, CardSuit.Green, 4), N(1, CardSuit.Yellow, 14));
            Winner(1, N(0, CardSuit.Green, 4), S(1, SpecialType.SkullKing));
            Winner(2, N(0, CardSuit.Green, 4), S(1, SpecialType.SkullKing), N(2, CardSuit.Yellow, 13), S(3, SpecialType.WhiteWhale));
            Winner(1, N(0, CardSuit.Black, 1), N(1, CardSuit.Yellow, 14), S(2, SpecialType.WhiteWhale));
            Winner(0, N(0, CardSuit.Green, 14), N(1, CardSuit.Yellow, 14), S(2, SpecialType.WhiteWhale));
            Winner(0, S(0, SpecialType.WhiteWhale));
            Winner(1, S(0, SpecialType.WhiteWhale), N(1, CardSuit.Green, 1));
            Winner(2, S(0, SpecialType.WhiteWhale), N(1, CardSuit.Green, 1), N(2, CardSuit.Purple, 14));
            Winner(2, S(0, SpecialType.SkullKing), S(1, SpecialType.Pirate), S(2, SpecialType.WhiteWhale));
            Winner(0, N(0, CardSuit.Green, 14), N(1, CardSuit.Yellow, 4), S(2, SpecialType.WhiteWhale));
            Winner(0, N(0, CardSuit.Green, 14), S(1, SpecialType.Kraken), S(2, SpecialType.WhiteWhale));
            Winner(-1, N(0, CardSuit.Green, 14), S(1, SpecialType.WhiteWhale), S(2, SpecialType.Kraken));
            Winner(2, N(5, CardSuit.Green, 4), S(2, SpecialType.SkullKing), N(1, CardSuit.Yellow, 13), S(4, SpecialType.WhiteWhale));
            Check("whale entrance", Entrance(N(0, CardSuit.Green, 14), S(1, SpecialType.WhiteWhale)));
            Check("whale first", Entrance(S(0, SpecialType.WhiteWhale)));
            Check("no replay on later number", !Entrance(S(0, SpecialType.WhiteWhale), N(1, CardSuit.Green, 14)));
            Check("later angel overrides kraken and plays wings", Entrance(S(0, SpecialType.Kraken), S(1, SpecialType.WhiteWhale)));
            Check("ordinary card", !Entrance(N(0, CardSuit.Green, 14)));
            Check("null entrance", !TrickPresentationRules.IsWhaleEntrance(null));
            var combined = new List<PlayedCard> { S(0, SpecialType.SkullKing), N(1, CardSuit.Black, 2),
                N(2, CardSuit.Green, 14), S(3, SpecialType.WhiteWhale), S(4, SpecialType.Kraken) };
            var combinedResult = SkullKing.Logic.TrickResolver.Resolve(combined, CardSuit.Black);
            Check("kraken voids angel trick", combinedResult.IsVoided && combinedResult.WinnerIndex == -1);
            Check("next leader uses angel numeric rule", combinedResult.WouldBeWinnerIndex == 2);
            combined.Reverse();
            var reversedResult = SkullKing.Logic.TrickResolver.Resolve(combined, CardSuit.Black);
            Check("later angel restores a normal numeric victory", !reversedResult.IsVoided && reversedResult.WinnerIndex == 2 && reversedResult.WouldBeWinnerIndex == -1);
            var onlySpecials = new List<PlayedCard> { S(0, SpecialType.SkullKing), S(1, SpecialType.WhiteWhale), S(2, SpecialType.Kraken) };
            Check("angel leads next when no numbers", SkullKing.Logic.TrickResolver.Resolve(onlySpecials, CardSuit.None).WouldBeWinnerIndex == 1);
            onlySpecials.Reverse();
            var angelLast = SkullKing.Logic.TrickResolver.Resolve(onlySpecials, CardSuit.None);
            Check("later angel wins special-only trick", !angelLast.IsVoided && angelLast.WinnerIndex == 1);
            Winner(-1, S(0, SpecialType.WhiteWhale), S(1, SpecialType.Kraken), N(2, CardSuit.Green, 14));
            Winner(2, S(0, SpecialType.Kraken), S(1, SpecialType.WhiteWhale), N(2, CardSuit.Green, 14));
            Winner(2, S(0, SpecialType.Kraken), S(1, SpecialType.WhiteWhale), N(2, CardSuit.Green, 14), N(3, CardSuit.Black, 1));
            Winner(2, N(0, CardSuit.Black, 3), S(1, SpecialType.Kraken), N(2, CardSuit.Yellow, 14), S(3, SpecialType.WhiteWhale));
            Winner(0, N(0, CardSuit.Green, 14), N(1, CardSuit.Yellow, 14), S(2, SpecialType.Kraken), S(3, SpecialType.WhiteWhale));
            Check("ordinary later cards do not change active override", SkullKing.Logic.TrickResolver.GetActiveTrickOverride(
                new List<PlayedCard> { S(0, SpecialType.WhiteWhale), S(1, SpecialType.Kraken), S(2, SpecialType.SkullKing) }) == SpecialType.Kraken);
            // Every ordering of two numbers, Skull King, Kraken and Angel (5! = 120).
            var mixed = new[] { N(0, CardSuit.Green, 14), N(1, CardSuit.Black, 3), S(2, SpecialType.SkullKing),
                S(3, SpecialType.Kraken), S(4, SpecialType.WhiteWhale) };
            foreach (var order in Permutations(mixed, 0))
            {
                int kraken = order.FindIndex(c => c.Card.Special == SpecialType.Kraken);
                int angel = order.FindIndex(c => c.Card.Special == SpecialType.WhiteWhale);
                var result = SkullKing.Logic.TrickResolver.Resolve(order, SkullKing.Logic.TrickResolver.DetermineLeadSuit(order));
                bool correct = kraken > angel
                    ? result.IsVoided && result.WinnerIndex == -1 && result.WouldBeWinnerIndex == 0
                    : !result.IsVoided && result.WinnerIndex == 0 && result.WouldBeWinnerIndex == -1;
                Check("all submitted orders: last override governs", correct);
            }
            var gate = new SkullKing.Core.PresentationGate();
            var owner = new object();
            var other = new object();
            Check("presentation initially unlocked", !gate.IsHeld);
            gate.Set(owner, true);
            Check("effect blocks progression", gate.IsHeld);
            gate.Set(owner, true);
            gate.Set(owner, false);
            Check("duplicate acquire needs only one release", !gate.IsHeld);
            gate.Set(owner, true);
            gate.Set(other, true);
            gate.Set(owner, false);
            Check("another effect still holds", gate.IsHeld);
            gate.Set(other, false);
            Check("last effect releases", !gate.IsHeld);
            gate.Set(owner, false);
            Check("cancel twice is safe", !gate.IsHeld);
            gate.Set(null, true);
            Check("null owner cannot deadlock", !gate.IsHeld);
            gate.Set(owner, true);
            gate.Set(owner, false);
            Check("can start another effect", !gate.IsHeld);
            return _checks;
        }

        private static bool Entrance(params PlayedCard[] cards) => TrickPresentationRules.IsWhaleEntrance(new List<PlayedCard>(cards));
        private static IEnumerable<List<PlayedCard>> Permutations(PlayedCard[] cards, int start)
        {
            if (start == cards.Length) { yield return new List<PlayedCard>(cards); yield break; }
            for (int i = start; i < cards.Length; i++)
            {
                var saved = cards[start]; cards[start] = cards[i]; cards[i] = saved;
                foreach (var order in Permutations(cards, start + 1)) yield return order;
                saved = cards[start]; cards[start] = cards[i]; cards[i] = saved;
            }
        }
        private static void Winner(int index, params PlayedCard[] cards)
            => Check("winning card index " + index, TrickPresentationRules.WinnerCardIndex(new List<PlayedCard>(cards)) == index);
        private static void Check(string label, bool passed)
        {
            _checks++;
            if (!passed) throw new InvalidOperationException("Whale presentation check failed: " + label + " (#" + _checks + ")");
        }
        private static PlayedCard N(int seat, CardSuit suit, int number)
            => new PlayedCard { PlayerIndex = seat, Card = CardData.CreateNumberCard(_id++, suit, number) };
        private static PlayedCard S(int seat, SpecialType type)
            => new PlayedCard { PlayerIndex = seat, Card = CardData.CreateSpecialCard(_id++, type) };
    }
}
#endif
