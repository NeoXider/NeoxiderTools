namespace Neo.Cards
{
    /// <summary>
    /// Типы колод игральных карт
    /// </summary>
    public enum DeckType
    {
        /// <summary>
        /// Колода 36 карт (от шестёрки до туза) - для игры в "Дурака"
        /// </summary>
        Standard36 = 36,

        /// <summary>
        /// Стандартная колода 52 карты (от двойки до туза)
        /// </summary>
        Standard52 = 52,

        /// <summary>
        /// Колода 54 карты (52 + 2 джокера)
        /// </summary>
        Standard54 = 54
    }

    /// <summary>
    /// Расширения для работы с типами колод
    /// </summary>
    public static class DeckTypeExtensions
    {
        /// <summary>
        /// Возвращает минимальный ранг карты для данного типа колоды
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
        /// Возвращает количество карт в колоде (без джокеров)
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
        /// Возвращает количество джокеров в колоде
        /// </summary>
        public static int GetJokerCount(this DeckType deckType)
        {
            return deckType == DeckType.Standard54 ? 2 : 0;
        }

        /// <summary>
        /// Возвращает общее количество карт в колоде (с джокерами)
        /// </summary>
        public static int GetTotalCardCount(this DeckType deckType)
        {
            return (int)deckType;
        }

        /// <summary>
        /// Возвращает количество карт одной масти
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

