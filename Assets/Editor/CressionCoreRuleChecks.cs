using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Editor
{
    /// <summary>
    /// 실제 게임 UI를 거치지 않고 핵심 카드 상성과 제출 규칙을 즉시 검사합니다.
    /// 규칙을 수정한 뒤에도 이 검사를 다시 실행해 기존 조합이 망가지지 않았는지 확인할 수 있습니다.
    /// </summary>
    public static class CressionCoreRuleChecks
    {
        private static int _nextId;

        public static void Run()
        {
            _nextId = 10000;
            var failures = new List<string>();
            int checks = 0;

            Check("같은 세력은 높은 숫자 승리", failures, ref checks, () =>
                Resolve(CardSuit.Green, Number(0, CardSuit.Green, 8), Number(1, CardSuit.Green, 12)).WinnerIndex == 1);

            Check("공허 숫자는 일반 세력보다 우선", failures, ref checks, () =>
                Resolve(CardSuit.Green, Number(0, CardSuit.Green, 14), Number(1, CardSuit.Black, 1)).WinnerIndex == 1);

            Check("리드 세력은 다른 일반 세력보다 우선", failures, ref checks, () =>
                Resolve(CardSuit.Green, Number(0, CardSuit.Green, 8), Number(1, CardSuit.Yellow, 14)).WinnerIndex == 0);

            Check("SKULL KING은 용사를 이김", failures, ref checks, () =>
                Resolve(CardSuit.None, Special(0, SpecialType.Pirate), Special(1, SpecialType.SkullKing)).WinnerIndex == 1);

            Check("MERMAID는 SKULL KING을 이김", failures, ref checks, () =>
                Resolve(CardSuit.None, Special(0, SpecialType.SkullKing), Special(1, SpecialType.Mermaid)).WinnerIndex == 1);

            Check("용사는 MERMAID를 이김", failures, ref checks, () =>
                Resolve(CardSuit.None, Special(0, SpecialType.Mermaid), Special(1, SpecialType.Pirate)).WinnerIndex == 1);

            Check("해적 모드 TIGRESS는 용사 역할", failures, ref checks, () =>
                Resolve(CardSuit.None,
                    Special(0, SpecialType.Mermaid),
                    Special(1, SpecialType.Tigress, TigressChoice.Pirate)).WinnerIndex == 1);

            Check("도주 모드 TIGRESS는 숫자 카드에 패배", failures, ref checks, () =>
                Resolve(CardSuit.Green,
                    Special(0, SpecialType.Tigress, TigressChoice.Escape),
                    Number(1, CardSuit.Green, 1)).WinnerIndex == 1);

            Check("KRAKEN은 트릭을 무효화", failures, ref checks, () =>
            {
                TrickResult result = Resolve(CardSuit.Green,
                    Number(0, CardSuit.Green, 5),
                    Number(1, CardSuit.Black, 1),
                    Special(2, SpecialType.Kraken));
                return result.IsVoided && result.WinnerIndex == -1 && result.WouldBeWinnerIndex == 1;
            });

            Check("WHITE WHALE은 특수 능력을 지우고 최고 숫자를 선택", failures, ref checks, () =>
                Resolve(CardSuit.Green,
                    Special(0, SpecialType.SkullKing),
                    Number(1, CardSuit.Green, 4),
                    Number(2, CardSuit.Yellow, 13),
                    Special(3, SpecialType.WhiteWhale)).WinnerIndex == 2);

            Check("모두 항복이면 첫 제출자 승리", failures, ref checks, () =>
                Resolve(CardSuit.None, Special(2, SpecialType.Escape), Special(4, SpecialType.Escape)).WinnerIndex == 2);

            Check("첫 숫자 카드가 리드 세력을 결정", failures, ref checks, () =>
                TrickResolver.DetermineLeadSuit(new List<PlayedCard>
                {
                    Special(0, SpecialType.Escape),
                    Number(1, CardSuit.Purple, 3),
                    Number(2, CardSuit.Green, 10)
                }) == CardSuit.Purple);

            Check("리드 세력이 있으면 같은 세력 숫자와 모든 특수 카드만 제출 가능", failures, ref checks, () =>
            {
                CardData green = CardData.CreateNumberCard(_nextId++, CardSuit.Green, 3);
                CardData yellow = CardData.CreateNumberCard(_nextId++, CardSuit.Yellow, 8);
                CardData kraken = CardData.CreateSpecialCard(_nextId++, SpecialType.Kraken);
                List<CardData> playable = TrickResolver.GetPlayableCards(
                    new List<CardData> { green, yellow, kraken }, CardSuit.Green);
                return playable.Count == 2 && playable.Contains(green) && playable.Contains(kraken) && !playable.Contains(yellow);
            });

            if (failures.Count == 0)
            {
                string message = $"핵심 규칙 {checks}개를 모두 통과했습니다.";
                Debug.Log($"[CROSSION Rule Checks] {message}");
                EditorUtility.DisplayDialog("CROSSION 규칙 검사", message, "확인");
                return;
            }

            string failureText = string.Join("\n", failures);
            Debug.LogError($"[CROSSION Rule Checks] {failures.Count}/{checks} 실패\n{failureText}");
            EditorUtility.DisplayDialog(
                "CROSSION 규칙 검사 실패",
                $"{failures.Count}/{checks}개가 실패했습니다.\n\n{failureText}",
                "확인");
        }

        private static void Check(string name, List<string> failures, ref int checks, Func<bool> condition)
        {
            checks++;
            try
            {
                if (!condition()) failures.Add($"• {name}");
            }
            catch (Exception exception)
            {
                failures.Add($"• {name}: {exception.GetType().Name}");
            }
        }

        private static TrickResult Resolve(CardSuit leadSuit, params PlayedCard[] cards)
        {
            return TrickResolver.Resolve(new List<PlayedCard>(cards), leadSuit);
        }

        private static PlayedCard Number(int playerIndex, CardSuit suit, int number)
        {
            return new PlayedCard
            {
                PlayerIndex = playerIndex,
                Card = CardData.CreateNumberCard(_nextId++, suit, number),
                TigressMode = TigressChoice.NotApplicable
            };
        }

        private static PlayedCard Special(
            int playerIndex,
            SpecialType special,
            TigressChoice tigressChoice = TigressChoice.NotApplicable)
        {
            return new PlayedCard
            {
                PlayerIndex = playerIndex,
                Card = CardData.CreateSpecialCard(_nextId++, special),
                TigressMode = tigressChoice
            };
        }
    }
}
