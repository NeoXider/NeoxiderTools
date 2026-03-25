namespace Neo.Cards
{
    /// <summary>
    ///     Playing card suits.
    /// </summary>
    public enum Suit
    {
        /// <summary>
        ///     Hearts (red).
        /// </summary>
        Hearts = 0,

        /// <summary>
        ///     Diamonds (red).
        /// </summary>
        Diamonds = 1,

        /// <summary>
        ///     Clubs (black).
        /// </summary>
        Clubs = 2,

        /// <summary>
        ///     Spades (black).
        /// </summary>
        Spades = 3
    }

    /// <summary>
    ///     Extension methods for suits.
    /// </summary>
    public static class SuitExtensions
    {
        /// <summary>
        ///     Returns whether the suit is red (hearts or diamonds).
        /// </summary>
        public static bool IsRed(this Suit suit)
        {
            return suit == Suit.Hearts || suit == Suit.Diamonds;
        }

        /// <summary>
        ///     Returns whether the suit is black (clubs or spades).
        /// </summary>
        public static bool IsBlack(this Suit suit)
        {
            return suit == Suit.Clubs || suit == Suit.Spades;
        }

        /// <summary>
        ///     Returns the Unicode symbol for the suit.
        /// </summary>
        public static char ToSymbol(this Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => '♥',
                Suit.Diamonds => '♦',
                Suit.Clubs => '♣',
                Suit.Spades => '♠',
                _ => '?'
            };
        }

        /// <summary>
        ///     Returns the English name of the suit.
        /// </summary>
        public static string ToEnglishName(this Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "Hearts",
                Suit.Diamonds => "Diamonds",
                Suit.Clubs => "Clubs",
                Suit.Spades => "Spades",
                _ => "Unknown"
            };
        }
    }
}
