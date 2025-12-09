namespace Neo.Cards
{
    /// <summary>
    ///     Ранги (достоинства) игральных карт
    /// </summary>
    public enum Rank
    {
        /// <summary>
        ///     Двойка
        /// </summary>
        Two = 2,

        /// <summary>
        ///     Тройка
        /// </summary>
        Three = 3,

        /// <summary>
        ///     Четвёрка
        /// </summary>
        Four = 4,

        /// <summary>
        ///     Пятёрка
        /// </summary>
        Five = 5,

        /// <summary>
        ///     Шестёрка
        /// </summary>
        Six = 6,

        /// <summary>
        ///     Семёрка
        /// </summary>
        Seven = 7,

        /// <summary>
        ///     Восьмёрка
        /// </summary>
        Eight = 8,

        /// <summary>
        ///     Девятка
        /// </summary>
        Nine = 9,

        /// <summary>
        ///     Десятка
        /// </summary>
        Ten = 10,

        /// <summary>
        ///     Валет
        /// </summary>
        Jack = 11,

        /// <summary>
        ///     Дама
        /// </summary>
        Queen = 12,

        /// <summary>
        ///     Король
        /// </summary>
        King = 13,

        /// <summary>
        ///     Туз
        /// </summary>
        Ace = 14
    }

    /// <summary>
    ///     Расширения для работы с рангами карт
    /// </summary>
    public static class RankExtensions
    {
        /// <summary>
        ///     Проверяет, является ли карта картинкой (Jack, Queen, King)
        /// </summary>
        public static bool IsFaceCard(this Rank rank)
        {
            return rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King;
        }

        /// <summary>
        ///     Проверяет, является ли карта тузом
        /// </summary>
        public static bool IsAce(this Rank rank)
        {
            return rank == Rank.Ace;
        }

        /// <summary>
        ///     Возвращает короткое обозначение ранга (2, 3, ..., J, Q, K, A)
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
        ///     Возвращает название ранга на русском языке
        /// </summary>
        public static string ToRussianName(this Rank rank)
        {
            return rank switch
            {
                Rank.Two => "Двойка",
                Rank.Three => "Тройка",
                Rank.Four => "Четвёрка",
                Rank.Five => "Пятёрка",
                Rank.Six => "Шестёрка",
                Rank.Seven => "Семёрка",
                Rank.Eight => "Восьмёрка",
                Rank.Nine => "Девятка",
                Rank.Ten => "Десятка",
                Rank.Jack => "Валет",
                Rank.Queen => "Дама",
                Rank.King => "Король",
                Rank.Ace => "Туз",
                _ => "Неизвестно"
            };
        }

        /// <summary>
        ///     Возвращает числовое значение ранга для подсчёта очков
        /// </summary>
        /// <param name="rank">Ранг карты</param>
        /// <param name="aceAsOne">Считать туз за 1 (true) или за 14 (false)</param>
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