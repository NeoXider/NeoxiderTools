namespace Neo.Cards
{
    /// <summary>
    /// Масти игральных карт
    /// </summary>
    public enum Suit
    {
        /// <summary>
        /// Червы (красные)
        /// </summary>
        Hearts = 0,

        /// <summary>
        /// Бубны (красные)
        /// </summary>
        Diamonds = 1,

        /// <summary>
        /// Трефы (чёрные)
        /// </summary>
        Clubs = 2,

        /// <summary>
        /// Пики (чёрные)
        /// </summary>
        Spades = 3
    }

    /// <summary>
    /// Расширения для работы с мастями
    /// </summary>
    public static class SuitExtensions
    {
        /// <summary>
        /// Проверяет, является ли масть красной (Hearts или Diamonds)
        /// </summary>
        public static bool IsRed(this Suit suit)
        {
            return suit == Suit.Hearts || suit == Suit.Diamonds;
        }

        /// <summary>
        /// Проверяет, является ли масть чёрной (Clubs или Spades)
        /// </summary>
        public static bool IsBlack(this Suit suit)
        {
            return suit == Suit.Clubs || suit == Suit.Spades;
        }

        /// <summary>
        /// Возвращает символ масти в Unicode
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
        /// Возвращает название масти на русском языке
        /// </summary>
        public static string ToRussianName(this Suit suit)
        {
            return suit switch
            {
                Suit.Hearts => "Червы",
                Suit.Diamonds => "Бубны",
                Suit.Clubs => "Трефы",
                Suit.Spades => "Пики",
                _ => "Неизвестно"
            };
        }
    }
}

