namespace Neo.Cards.Poker
{
    /// <summary>
    /// Покерные комбинации в порядке возрастания силы
    /// </summary>
    public enum PokerCombination
    {
        /// <summary>
        /// Старшая карта - нет комбинации
        /// </summary>
        HighCard = 0,

        /// <summary>
        /// Пара - две карты одного ранга
        /// </summary>
        Pair = 1,

        /// <summary>
        /// Две пары - две разные пары
        /// </summary>
        TwoPair = 2,

        /// <summary>
        /// Тройка (сет) - три карты одного ранга
        /// </summary>
        ThreeOfAKind = 3,

        /// <summary>
        /// Стрит - пять последовательных карт разных мастей
        /// </summary>
        Straight = 4,

        /// <summary>
        /// Флеш - пять карт одной масти
        /// </summary>
        Flush = 5,

        /// <summary>
        /// Фулл хаус - тройка + пара
        /// </summary>
        FullHouse = 6,

        /// <summary>
        /// Каре - четыре карты одного ранга
        /// </summary>
        FourOfAKind = 7,

        /// <summary>
        /// Стрит-флеш - пять последовательных карт одной масти
        /// </summary>
        StraightFlush = 8,

        /// <summary>
        /// Роял-флеш - стрит-флеш от десятки до туза
        /// </summary>
        RoyalFlush = 9
    }

    /// <summary>
    /// Расширения для покерных комбинаций
    /// </summary>
    public static class PokerCombinationExtensions
    {
        /// <summary>
        /// Возвращает русское название комбинации
        /// </summary>
        public static string ToRussianName(this PokerCombination combination)
        {
            return combination switch
            {
                PokerCombination.HighCard => "Старшая карта",
                PokerCombination.Pair => "Пара",
                PokerCombination.TwoPair => "Две пары",
                PokerCombination.ThreeOfAKind => "Тройка",
                PokerCombination.Straight => "Стрит",
                PokerCombination.Flush => "Флеш",
                PokerCombination.FullHouse => "Фулл хаус",
                PokerCombination.FourOfAKind => "Каре",
                PokerCombination.StraightFlush => "Стрит-флеш",
                PokerCombination.RoyalFlush => "Роял-флеш",
                _ => "Неизвестно"
            };
        }

        /// <summary>
        /// Возвращает английское название комбинации
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

