using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    /// Компараторы для сортировки карт
    /// </summary>
    public static class CardComparer
    {
        /// <summary>
        /// Сравнивает карты по рангу (по возрастанию)
        /// </summary>
        public static readonly IComparer<CardData> ByRankAscending = new RankAscendingComparer();

        /// <summary>
        /// Сравнивает карты по рангу (по убыванию)
        /// </summary>
        public static readonly IComparer<CardData> ByRankDescending = new RankDescendingComparer();

        /// <summary>
        /// Сравнивает карты по масти, затем по рангу (по возрастанию)
        /// </summary>
        public static readonly IComparer<CardData> BySuitThenRankAscending = new SuitThenRankAscendingComparer();

        /// <summary>
        /// Сравнивает карты по масти, затем по рангу (по убыванию)
        /// </summary>
        public static readonly IComparer<CardData> BySuitThenRankDescending = new SuitThenRankDescendingComparer();

        /// <summary>
        /// Создаёт компаратор с учётом козырной масти (козыри в конце)
        /// </summary>
        /// <param name="trump">Козырная масть</param>
        /// <param name="ascending">По возрастанию ранга</param>
        /// <returns>Компаратор</returns>
        public static IComparer<CardData> WithTrump(Suit trump, bool ascending = true)
        {
            return new TrumpComparer(trump, ascending);
        }

        private class RankAscendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                    return x.IsRedJoker.CompareTo(y.IsRedJoker);

                if (x.IsJoker) return 1;
                if (y.IsJoker) return -1;

                return x.Rank.CompareTo(y.Rank);
            }
        }

        private class RankDescendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                    return y.IsRedJoker.CompareTo(x.IsRedJoker);

                if (x.IsJoker) return -1;
                if (y.IsJoker) return 1;

                return y.Rank.CompareTo(x.Rank);
            }
        }

        private class SuitThenRankAscendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                    return x.IsRedJoker.CompareTo(y.IsRedJoker);

                if (x.IsJoker) return 1;
                if (y.IsJoker) return -1;

                int suitCompare = x.Suit.CompareTo(y.Suit);
                if (suitCompare != 0) return suitCompare;

                return x.Rank.CompareTo(y.Rank);
            }
        }

        private class SuitThenRankDescendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                    return y.IsRedJoker.CompareTo(x.IsRedJoker);

                if (x.IsJoker) return -1;
                if (y.IsJoker) return 1;

                int suitCompare = y.Suit.CompareTo(x.Suit);
                if (suitCompare != 0) return suitCompare;

                return y.Rank.CompareTo(x.Rank);
            }
        }

        private class TrumpComparer : IComparer<CardData>
        {
            private readonly Suit _trump;
            private readonly bool _ascending;

            public TrumpComparer(Suit trump, bool ascending)
            {
                _trump = trump;
                _ascending = ascending;
            }

            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                    return _ascending ? x.IsRedJoker.CompareTo(y.IsRedJoker) : y.IsRedJoker.CompareTo(x.IsRedJoker);

                if (x.IsJoker) return _ascending ? 1 : -1;
                if (y.IsJoker) return _ascending ? -1 : 1;

                bool xTrump = x.Suit == _trump;
                bool yTrump = y.Suit == _trump;

                if (xTrump && !yTrump) return 1;
                if (!xTrump && yTrump) return -1;

                int suitCompare = x.Suit.CompareTo(y.Suit);
                if (suitCompare != 0) return _ascending ? suitCompare : -suitCompare;

                return _ascending ? x.Rank.CompareTo(y.Rank) : y.Rank.CompareTo(x.Rank);
            }
        }
    }
}

