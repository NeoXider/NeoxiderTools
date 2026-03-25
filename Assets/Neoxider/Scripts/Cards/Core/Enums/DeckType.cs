namespace Neo.Cards
{
    /// <summary>
    ///     Playing deck sizes / types.
    /// </summary>
    public enum DeckType
    {
        /// <summary>
        ///     36-card deck (six through ace), common for Durak-style games.
        /// </summary>
        Standard36 = 36,

        /// <summary>
        ///     Standard 52-card deck (two through ace).
        /// </summary>
        Standard52 = 52,

        /// <summary>
        ///     54-card deck (52 plus two jokers).
        /// </summary>
        Standard54 = 54
    }

    /// <summary>
    ///     Extension methods for deck types.
    /// </summary>
    public static class DeckTypeExtensions
    {
        /// <summary>
        ///     Minimum rank included in this deck type.
        /// </summary>
        public static Rank GetMinRank(this DeckType deckType)
        {
            return deckType switch
            {
                DeckType.Standard36 => Rank.Six,
                DeckType.Standard52 => Rank.Two,
                DeckType.Standard54 => Rank.Two,
                _ => Rank.Two
            };
        }

        /// <summary>
        ///     Number of playing cards (excluding jokers).
        /// </summary>
        public static int GetCardCount(this DeckType deckType)
        {
            return deckType switch
            {
                DeckType.Standard36 => 36,
                DeckType.Standard52 => 52,
                DeckType.Standard54 => 52,
                _ => 52
            };
        }

        /// <summary>
        ///     Number of jokers in the deck.
        /// </summary>
        public static int GetJokerCount(this DeckType deckType)
        {
            return deckType == DeckType.Standard54 ? 2 : 0;
        }

        /// <summary>
        ///     Total cards including jokers (matches enum value for these types).
        /// </summary>
        public static int GetTotalCardCount(this DeckType deckType)
        {
            return (int)deckType;
        }

        /// <summary>
        ///     Cards per suit for this deck type.
        /// </summary>
        public static int GetCardsPerSuit(this DeckType deckType)
        {
            return deckType switch
            {
                DeckType.Standard36 => 9,
                DeckType.Standard52 => 13,
                DeckType.Standard54 => 13,
                _ => 13
            };
        }
    }
}
