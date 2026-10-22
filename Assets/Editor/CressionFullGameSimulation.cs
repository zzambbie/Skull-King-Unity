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
    /// 3~6인 AI 게임을 1~10라운드까지 반복 실행해 게임 로직이 끝까지 완주하는지 검사합니다.
    /// </summary>
    public static class CressionFullGameSimulation
    {
        private struct SimulationStats
        {
            public int Games;
            public int Rounds;
            public int Tricks;
            public int VoidedTricks;
        }

        public static void Run()
        {
            try
            {
                SimulationStats total = default;
                for (int playerCount = 3; playerCount <= 6; playerCount++)
                {
                    for (int seed = 1; seed <= 5; seed++)
                    {
                        SimulationStats result = SimulateGame(playerCount, 1000 + playerCount * 100 + seed);
                        total.Games += result.Games;
                        total.Rounds += result.Rounds;
                        total.Tricks += result.Tricks;
                        total.VoidedTricks += result.VoidedTricks;
                    }
                }

                string message =
                    $"전체 게임 {total.Games}회가 모두 정상 종료되었습니다.\n" +
                    $"검사한 라운드: {total.Rounds}\n" +
                    $"검사한 트릭: {total.Tricks}\n" +
                    $"크라켄 무효 트릭: {total.VoidedTricks}";
                Debug.Log($"[CROSSION Full Game Simulation] {message}");
                EditorUtility.DisplayDialog("CROSSION 전체 게임 검사", message, "확인");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "CROSSION 전체 게임 검사 실패",
                    exception.Message,
                    "확인");
            }
        }

        private static SimulationStats SimulateGame(int playerCount, int seed)
        {
            PlayerData[] players = CreatePlayers(playerCount);
            var state = new GameState
            {
                PlayerCount = playerCount,
                TotalRounds = 10,
                Players = players
            };
            var ai = new AIController(seed);
            int leader = seed % playerCount;
            int trickCount = 0;
            int voidedTricks = 0;

            for (int round = 1; round <= state.TotalRounds; round++)
            {
                state.PrepareNewRound(round, leader);
                var deck = new Deck(seed * 100 + round);
                deck.CreateFullDeck();
                deck.Shuffle();
                deck.DealToPlayers(players, round);

                Ensure(players.All(player => player.Hand.Count == round),
                    $"{playerCount}인 게임 {round}라운드: 분배 장수가 맞지 않습니다.");

                foreach (PlayerData player in players)
                {
                    player.Bid = ai.DecideBid(player.Hand, playerCount);
                    player.HasBid = true;
                    Ensure(player.Bid >= 0 && player.Bid <= round,
                        $"{playerCount}인 게임 {round}라운드: 잘못된 비딩 {player.Bid}");
                }

                Ensure(state.AllPlayersBid(),
                    $"{playerCount}인 게임 {round}라운드: 비딩 완료 상태가 아닙니다.");

                for (int trick = 1; trick <= round; trick++)
                {
                    state.PrepareNewTrick();
                    int currentPlayer = state.LeaderIndex;

                    for (int turn = 0; turn < playerCount; turn++)
                    {
                        PlayerData player = players[currentPlayer];
                        List<CardData> playable = TrickResolver.GetPlayableCards(player.Hand, state.LeadSuit);
                        Ensure(playable.Count > 0,
                            $"{playerCount}인 게임 {round}라운드 {trick}트릭: 낼 수 있는 카드가 없습니다.");

                        int remainingTricks = round - trick + 1;
                        CardData selected = ai.ChooseCard(
                            player.Hand,
                            playable,
                            state.TrickCards,
                            state.LeadSuit,
                            player.Bid,
                            player.TricksWon,
                            remainingTricks);

                        Ensure(playable.Any(card => card.Id == selected.Id),
                            $"{playerCount}인 게임 {round}라운드 {trick}트릭: AI가 제출 불가능한 카드를 골랐습니다.");
                        Ensure(player.RemoveCardFromHand(selected),
                            $"{playerCount}인 게임 {round}라운드 {trick}트릭: 선택한 카드를 손패에서 제거하지 못했습니다.");

                        TigressChoice tigressChoice = selected.Special == SpecialType.Tigress
                            ? ai.DecideTigressMode(player.Bid, player.TricksWon, remainingTricks)
                            : TigressChoice.NotApplicable;
                        state.TrickCards.Add(new PlayedCard
                        {
                            PlayerIndex = currentPlayer,
                            Card = selected,
                            TigressMode = tigressChoice
                        });

                        if (state.LeadSuit == CardSuit.None)
                            state.LeadSuit = TrickResolver.DetermineLeadSuit(state.TrickCards);

                        currentPlayer = state.GetNextPlayerIndex(currentPlayer);
                    }

                    Ensure(state.AllPlayersPlayedInTrick(),
                        $"{playerCount}인 게임 {round}라운드 {trick}트릭: 제출 카드 수가 부족합니다.");

                    TrickResult result = TrickResolver.Resolve(state.TrickCards, state.LeadSuit);
                    if (result.IsVoided)
                    {
                        voidedTricks++;
                        if (result.WouldBeWinnerIndex >= 0)
                        {
                            Ensure(result.WouldBeWinnerIndex < playerCount,
                                $"{playerCount}인 게임 {round}라운드 {trick}트릭: 크라켄 가정 승자가 잘못되었습니다.");
                            leader = result.WouldBeWinnerIndex;
                        }
                    }
                    else
                    {
                        Ensure(result.WinnerIndex >= 0 && result.WinnerIndex < playerCount,
                            $"{playerCount}인 게임 {round}라운드 {trick}트릭: 승자 인덱스가 잘못되었습니다.");
                        players[result.WinnerIndex].TricksWon++;
                        leader = result.WinnerIndex;
                    }

                    state.LeaderIndex = leader;
                    trickCount++;
                }

                Ensure(players.All(player => player.Hand.Count == 0),
                    $"{playerCount}인 게임 {round}라운드: 라운드 종료 후 손패가 남았습니다.");
                ScoreCalculator.CalculateAll(players, round);
                Ensure(players.All(player => player.TotalScore >= 0),
                    $"{playerCount}인 게임 {round}라운드: 누적 점수가 음수가 되었습니다.");
            }

            Ensure(players.All(player => player.ScoreHistory.Count == 10),
                $"{playerCount}인 게임: 10라운드 점수 기록이 완성되지 않았습니다.");
            Ensure(state.AllRoundsCompleted(),
                $"{playerCount}인 게임: 게임 종료 조건에 도달하지 않았습니다.");

            return new SimulationStats
            {
                Games = 1,
                Rounds = 10,
                Tricks = trickCount,
                VoidedTricks = voidedTricks
            };
        }

        private static PlayerData[] CreatePlayers(int count)
        {
            var players = new PlayerData[count];
            for (int i = 0; i < count; i++)
            {
                players[i] = new PlayerData(i, $"Simulation AI {i + 1}", false)
                {
                    TotalScore = 20
                };
            }
            return players;
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
