using System.Collections.Generic;
using SkullKing.Data;
using SkullKing.Logic;

namespace SkullKing.UI
{
    /// <summary>Resolve the top card from the entire presented trick, not only its newest card.</summary>
    public static class TrickPresentationRules
    {
        public static int WinnerCardIndex(List<PlayedCard> cards)
        {
            if (cards == null || cards.Count == 0) return -1;
            var result = TrickResolver.Resolve(cards, TrickResolver.DetermineLeadSuit(cards));
            if (result.IsVoided) return -1;
            return cards.FindIndex(card => card.PlayerIndex == result.WinnerIndex);
        }

        public static bool IsWhaleEntrance(List<PlayedCard> cards)
        {
            if (cards == null || cards.Count == 0 ||
                cards[cards.Count - 1].Card.Special != SpecialType.WhiteWhale) return false;
            // A later angel cancels an earlier Kraken and must show the numeric-winner transition.
            return TrickResolver.GetActiveTrickOverride(cards) == SpecialType.WhiteWhale;
        }
    }
}
