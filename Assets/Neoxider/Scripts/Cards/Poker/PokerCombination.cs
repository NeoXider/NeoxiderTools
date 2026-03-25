namespace Neo.Cards.Poker
{
    /// <summary>
    ///     Poker hand categories in ascending strength.
    /// </summary>
    public enum PokerCombination
    {
        /// <summary>
        ///     High card — no made hand.
        /// </summary>
        HighCard = 0,

        /// <summary>
        ///     Pair — two cards of the same rank.
        /// </summary>
        Pair = 1,

        /// <summary>
        ///     Two pair — two different pairs.
        /// </summary>
        TwoPair = 2,

        /// <summary>
        ///     Three of a kind — three cards of the same rank.
        /// </summary>
        ThreeOfAKind = 3,

        /// <summary>
        ///     Straight — five consecutive ranks, mixed suits.
        /// </summary>
        Straight = 4,

        /// <summary>
        ///     Flush — five cards of the same suit.
        /// </summary>
        Flush = 5,

        /// <summary>
        ///     Full house — three of a kind plus a pair.
        /// </summary>
        FullHouse = 6,

        /// <summary>
        ///     Four of a kind — four cards of the same rank.
        /// </summary>
        FourOfAKind = 7,

        /// <summary>
        ///     Straight flush — five consecutive cards of the same suit.
        /// </summary>
        StraightFlush = 8,

        /// <summary>
        ///     Royal flush — straight flush from ten to ace.
        /// </summary>
        RoyalFlush = 9
    }

    /// <summary>
    ///     Extension methods for poker combinations.
    /// </summary>
    public static class PokerCombinationExtensions
    {
        /// <summary>
        ///     Returns the English name of the combination.
        /// </summary>
        public static string ToEnglishName(this PokerCombination combination)
        {
            return combination switch
            {
                PokerCombination.HighCard => "High Card",
                PokerCombination.Pair => "Pair",
                PokerCombination.TwoPair => "Two Pair",
                PokerCombination.ThreeOfAKind => "Three of a Kind",
                PokerCombination.Straight => "Straight",
                PokerCombination.Flush => "Flush",
                PokerCombination.FullHouse => "Full House",
                PokerCombination.FourOfAKind => "Four of a Kind",
                PokerCombination.StraightFlush => "Straight Flush",
                PokerCombination.RoyalFlush => "Royal Flush",
                _ => "Unknown"
            };
        }
    }
}
