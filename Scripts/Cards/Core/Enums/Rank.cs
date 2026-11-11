namespace Neo.Cards
{
    /// <summary>
    ///     Playing card ranks (values).
    /// </summary>
    public enum Rank
    {
        /// <summary>
        ///     Two.
        /// </summary>
        Two = 2,

        /// <summary>
        ///     Three.
        /// </summary>
        Three = 3,

        /// <summary>
        ///     Four.
        /// </summary>
        Four = 4,

        /// <summary>
        ///     Five.
        /// </summary>
        Five = 5,

        /// <summary>
        ///     Six.
        /// </summary>
        Six = 6,

        /// <summary>
        ///     Seven.
        /// </summary>
        Seven = 7,

        /// <summary>
        ///     Eight.
        /// </summary>
        Eight = 8,

        /// <summary>
        ///     Nine.
        /// </summary>
        Nine = 9,

        /// <summary>
        ///     Ten.
        /// </summary>
        Ten = 10,

        /// <summary>
        ///     Jack.
        /// </summary>
        Jack = 11,

        /// <summary>
        ///     Queen.
        /// </summary>
        Queen = 12,

        /// <summary>
        ///     King.
        /// </summary>
        King = 13,

        /// <summary>
        ///     Ace.
        /// </summary>
        Ace = 14
    }

    /// <summary>
    ///     Extension methods for ranks.
    /// </summary>
    public static class RankExtensions
    {
        /// <summary>
        ///     Returns whether the rank is a face card (jack, queen, king).
        /// </summary>
        public static bool IsFaceCard(this Rank rank)
        {
            return rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King;
        }

        /// <summary>
        ///     Returns whether the rank is ace.
        /// </summary>
        public static bool IsAce(this Rank rank)
        {
            return rank == Rank.Ace;
        }

        /// <summary>
        ///     Returns short rank notation (2, 3, ..., J, Q, K, A).
        /// </summary>
        public static string ToShortString(this Rank rank)
        {
            return rank switch
            {
                Rank.Two => "2",
                Rank.Three => "3",
                Rank.Four => "4",
                Rank.Five => "5",
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "10",
                Rank.Jack => "J",
                Rank.Queen => "Q",
                Rank.King => "K",
                Rank.Ace => "A",
                _ => "?"
            };
        }

        /// <summary>
        ///     Returns the English name of the rank.
        /// </summary>
        public static string ToEnglishName(this Rank rank)
        {
            return rank switch
            {
                Rank.Two => "Two",
                Rank.Three => "Three",
                Rank.Four => "Four",
                Rank.Five => "Five",
                Rank.Six => "Six",
                Rank.Seven => "Seven",
                Rank.Eight => "Eight",
                Rank.Nine => "Nine",
                Rank.Ten => "Ten",
                Rank.Jack => "Jack",
                Rank.Queen => "Queen",
                Rank.King => "King",
                Rank.Ace => "Ace",
                _ => "Unknown"
            };
        }

        /// <summary>
        ///     Returns numeric value for scoring.
        /// </summary>
        /// <param name="rank">Card rank.</param>
        /// <param name="aceAsOne">Count ace as 1 (true) or 14 (false).</param>
        public static int ToValue(this Rank rank, bool aceAsOne = false)
        {
            if (aceAsOne && rank == Rank.Ace)
            {
                return 1;
            }

            return (int)rank;
        }
    }
}
