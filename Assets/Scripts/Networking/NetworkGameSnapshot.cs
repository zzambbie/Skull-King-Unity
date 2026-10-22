using System;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.Networking
{
    [Serializable]
    public sealed class NetworkPlayerView
    {
        public string name;
        public int bid, wins, score, handCount;
        public bool hasBid;
        public CardData[] hand;
        public int[] history, alliances;
    }

    // Only these explicitly selected fields cross the network. Never serialize GameState itself.
    [Serializable]
    public sealed class NetworkGameSnapshot
    {
        public string kind;
        public int sequence, request, round, trick, totalRounds, current, leader, roundLeader, actor;
        public GamePhase phase;
        public CardSuit leadSuit;
        public NetworkPlayerView[] players;
        public PlayedCard[] trickCards;
        public PlayedCard played;
        public TrickResult result;
        public RoundScoreResult[] scores;

        public static int ViewIndex(int serverIndex, int viewer, int count)
            => serverIndex < 0 ? serverIndex : (serverIndex - viewer + count) % count;

        public static NetworkGameSnapshot Capture(GameState state, int viewer, string kind,
            int sequence, int request, int actor = 0, PlayedCard played = default,
            TrickResult result = default, RoundScoreResult[] scores = null)
        {
            int count = state.PlayerCount;
            var view = new NetworkGameSnapshot
            {
                kind = kind, sequence = sequence, request = request,
                round = state.CurrentRound, trick = state.CurrentTrick, totalRounds = state.TotalRounds,
                current = ViewIndex(state.CurrentPlayerIndex, viewer, count),
                leader = ViewIndex(state.LeaderIndex, viewer, count),
                roundLeader = ViewIndex(state.RoundStartPlayerIndex, viewer, count),
                actor = ViewIndex(actor, viewer, count), phase = state.CurrentPhase, leadSuit = state.LeadSuit,
                players = new NetworkPlayerView[count], trickCards = state.TrickCards.ToArray(),
                played = played, result = result
            };
            for (int i = 0; i < count; i++)
            {
                PlayerData p = state.Players[i];
                int target = ViewIndex(i, viewer, count);
                int[] allies = p.JewelAlliances.ToArray();
                for (int j = 0; j < allies.Length; j++) allies[j] = ViewIndex(allies[j], viewer, count);
                view.players[target] = new NetworkPlayerView
                {
                    name = p.Name, bid = p.Bid, hasBid = p.HasBid, wins = p.TricksWon,
                    score = p.TotalScore, handCount = p.Hand.Count,
                    hand = i == viewer ? p.Hand.ToArray() : Array.Empty<CardData>(),
                    history = p.ScoreHistory.ToArray(), alliances = allies
                };
            }
            for (int i = 0; i < view.trickCards.Length; i++)
                view.trickCards[i].PlayerIndex = ViewIndex(view.trickCards[i].PlayerIndex, viewer, count);
            view.played.PlayerIndex = ViewIndex(played.PlayerIndex, viewer, count);
            view.result.WinnerIndex = ViewIndex(result.WinnerIndex, viewer, count);
            view.result.WouldBeWinnerIndex = ViewIndex(result.WouldBeWinnerIndex, viewer, count);
            if (scores != null)
            {
                view.scores = new RoundScoreResult[count];
                foreach (var score in scores)
                {
                    var mapped = score;
                    mapped.PlayerIndex = ViewIndex(score.PlayerIndex, viewer, count);
                    view.scores[mapped.PlayerIndex] = mapped;
                }
            }
            return view;
        }

        public GameState Restore()
        {
            var state = new GameState
            {
                PlayerCount = players.Length, Players = new PlayerData[players.Length],
                TotalRounds = totalRounds, CurrentRound = round, CurrentTrick = trick,
                CurrentPhase = phase, LeadSuit = leadSuit, CurrentPlayerIndex = current,
                LeaderIndex = leader, RoundStartPlayerIndex = roundLeader
            };
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                var restored = new PlayerData(i, p.name, i == 0)
                { Bid = p.bid, HasBid = p.hasBid, TricksWon = p.wins, TotalScore = p.score };
                if (i == 0) restored.Hand.AddRange(p.hand);
                else for (int j = 0; j < p.handCount; j++) restored.Hand.Add(new CardData { Id = -1 });
                restored.ScoreHistory.AddRange(p.history);
                restored.JewelAlliances.AddRange(p.alliances);
                state.Players[i] = restored;
            }
            state.TrickCards.AddRange(trickCards);
            return state;
        }
    }

    [Serializable]
    public sealed class NetworkGamePacket
    {
        public int protocol = 1;
        public string session, kind, command;
        public int request, value;
        public NetworkGameSnapshot snapshot;
    }
}
