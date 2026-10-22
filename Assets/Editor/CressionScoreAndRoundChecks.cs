using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Editor
{
    /// <summary>
    /// 덱 구성, 분배, 라운드 초기화, 기본 비딩 점수를 UI 없이 검사합니다.
    /// </summary>
    public static class CressionScoreAndRoundChecks
    {
        public static void Run()
        {
            var failures = new List<string>();
            int checks = 0;

            Check("전체 덱은 72장", failures, ref checks, () =>
            {
                Deck deck = BuildDeck();
                return deck.TotalCount == 72 && deck.RemainingCount == 72;
            });

            Check("숫자 카드는 네 세력 각 14장", failures, ref checks, () =>
            {
                List<CardData> cards = DrawFullDeck();
                return CountNumbers(cards, CardSuit.Green) == 14
                    && CountNumbers(cards, CardSuit.Yellow) == 14
                    && CountNumbers(cards, CardSuit.Purple) == 14
                    && CountNumbers(cards, CardSuit.Black) == 14;
            });

            Check("기본 특수 카드 수량이 정확함", failures, ref checks, () =>
            {
                List<CardData> cards = DrawFullDeck();
                return CountSpecial(cards, SpecialType.Escape) == 5
                    && CountSpecial(cards, SpecialType.Pirate) == 5
                    && CountSpecial(cards, SpecialType.Mermaid) == 2
                    && CountSpecial(cards, SpecialType.Tigress) == 1
                    && CountSpecial(cards, SpecialType.SkullKing) == 1
                    && CountSpecial(cards, SpecialType.Kraken) == 1
                    && CountSpecial(cards, SpecialType.WhiteWhale) == 1;
            });

            Check("5라운드는 모든 플레이어에게 5장씩 분배", failures, ref checks, () =>
            {
                Deck deck = BuildDeck();
                var players = CreatePlayers(4);
                deck.DealToPlayers(players, 5);
                return players.All(player => player.Hand.Count == 5)
                    && deck.RemainingCount == 52;
            });

            Check("일반 비딩 성공은 비딩 수 × 20점", failures, ref checks, () =>
                Score(bid: 3, wins: 3, round: 5).RoundTotal == 60);

            Check("0비딩 성공은 라운드 수 × 10점", failures, ref checks, () =>
                Score(bid: 0, wins: 0, round: 7).RoundTotal == 70);

            Check("일반 비딩 실패는 차이 × -10점", failures, ref checks, () =>
                Score(bid: 3, wins: 1, round: 5).RoundTotal == -20);

            Check("0비딩 실패는 라운드 수 × -10점", failures, ref checks, () =>
                Score(bid: 0, wins: 1, round: 7).RoundTotal == -70);

            Check("누적 점수는 0 아래로 내려가지 않음", failures, ref checks, () =>
            {
                PlayerData player = CreatePlayer(0);
                player.TotalScore = 20;
                player.Bid = 0;
                player.TricksWon = 1;
                ScoreCalculator.CalculateAll(new[] { player }, 10);
                return player.TotalScore == 0 && player.ScoreHistory.Last() == -100;
            });

            Check("새 라운드는 손패·비딩·승리 횟수를 초기화", failures, ref checks, () =>
            {
                PlayerData[] players = CreatePlayers(3);
                players[0].Hand.Add(CardData.CreateNumberCard(1, CardSuit.Green, 8));
                players[0].Bid = 2;
                players[0].HasBid = true;
                players[0].TricksWon = 1;
                players[0].JewelAlliances.Add(1);
                players[0].TotalScore = 40;

                var state = new GameState { PlayerCount = 3, Players = players };
                state.PrepareNewRound(6, 2);
                return state.CurrentRound == 6
                    && state.TricksInRound == 6
                    && state.RoundStartPlayerIndex == 2
                    && state.LeaderIndex == 2
                    && players[0].Hand.Count == 0
                    && players[0].Bid == 0
                    && !players[0].HasBid
                    && players[0].TricksWon == 0
                    && players[0].JewelAlliances.Count == 0
                    && players[0].TotalScore == 40;
            });

            Check("새 트릭은 이전 카드와 리드 세력을 초기화", failures, ref checks, () =>
            {
                var state = new GameState
                {
                    PlayerCount = 3,
                    Players = CreatePlayers(3),
                    CurrentRound = 4,
                    LeaderIndex = 2,
                    LeadSuit = CardSuit.Green
                };
                state.TrickCards.Add(new PlayedCard());
                state.PrepareNewTrick();
                return state.CurrentTrick == 1
                    && state.CurrentPlayerIndex == 2
                    && state.TrickCards.Count == 0
                    && state.LeadSuit == CardSuit.None;
            });

            Check("마지막 플레이어 다음은 첫 번째 플레이어", failures, ref checks, () =>
            {
                var state = new GameState { PlayerCount = 4 };
                return state.GetNextPlayerIndex(3) == 0;
            });

            if (failures.Count == 0)
            {
                string message = $"점수·덱·라운드 검사 {checks}개를 모두 통과했습니다.";
                Debug.Log($"[CROSSION Score/Round Checks] {message}");
                EditorUtility.DisplayDialog("CROSSION 진행 검사", message, "확인");
                return;
            }

            string failureText = string.Join("\n", failures);
            Debug.LogError($"[CROSSION Score/Round Checks] {failures.Count}/{checks} 실패\n{failureText}");
            EditorUtility.DisplayDialog(
                "CROSSION 진행 검사 실패",
                $"{failures.Count}/{checks}개가 실패했습니다.\n\n{failureText}",
                "확인");
        }

        private static Deck BuildDeck()
        {
            var deck = new Deck(12345);
            deck.CreateFullDeck();
            return deck;
        }

        private static List<CardData> DrawFullDeck()
        {
            Deck deck = BuildDeck();
            return deck.Draw(deck.TotalCount);
        }

        private static int CountNumbers(IEnumerable<CardData> cards, CardSuit suit)
        {
            return cards.Count(card => card.IsNumberCard && card.Suit == suit);
        }

        private static int CountSpecial(IEnumerable<CardData> cards, SpecialType special)
        {
            return cards.Count(card => card.Special == special);
        }

        private static RoundScoreResult Score(int bid, int wins, int round)
        {
            PlayerData player = CreatePlayer(0);
            player.Bid = bid;
            player.TricksWon = wins;
            return ScoreCalculator.Calculate(player, round);
        }

        private static PlayerData[] CreatePlayers(int count)
        {
            var players = new PlayerData[count];
            for (int i = 0; i < count; i++) players[i] = CreatePlayer(i);
            return players;
        }

        private static PlayerData CreatePlayer(int index)
        {
            return new PlayerData(index, $"Test {index}", index == 0);
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
    }
}
