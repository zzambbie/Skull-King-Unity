using System.Collections.Generic;
using SkullKing.Data;

namespace SkullKing.UI
{
    /// <summary>Only submitted cards enter this model; no GameState/hand access.</summary>
    public sealed class PublicTrickHistory
    {
        public struct Entry
        {
            public PlayedCard played;
            public string owner;
        }
        public sealed class Trick
        {
            private readonly List<Entry> _entries = new List<Entry>(6);
            public IReadOnlyList<Entry> Entries => _entries;
            public int Round { get; internal set; }
            public int Number { get; internal set; }
            public int LeaderIndex { get; internal set; } = -1;
            public bool Voided { get; internal set; }
            internal bool Add(PlayedCard played, string owner)
            {
                if (_entries.Count >= 6 || _entries.Exists(e => e.played.PlayerIndex == played.PlayerIndex)) return false;
                _entries.Add(new Entry { played = played, owner = owner ?? "" });
                return true;
            }
        }
        public Trick Current { get; private set; } = new Trick();
        private readonly List<Trick> _tricks = new List<Trick>(10);
        public IReadOnlyList<Trick> Tricks => _tricks;
        public int Round { get; private set; }
        public void BeginRound(int round)
        {
            if (Round == round) return;
            Round = round;
            _tricks.Clear();
            Current = new Trick { Round = round };
        }
        public void Begin(int round, int number)
        {
            BeginRound(round);
            if (Current.Number == number) return;
            Current = new Trick { Round = round, Number = number };
            _tricks.Add(Current);
        }
        public bool Add(PlayedCard played, string owner) => Current.Add(played, owner);
        public void SetLeader(int cardIndex, bool voided)
        {
            Current.Voided = voided;
            Current.LeaderIndex = !voided && cardIndex >= 0 && cardIndex < Current.Entries.Count ? cardIndex : -1;
        }
    }
}
