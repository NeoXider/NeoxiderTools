using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    ///     Comparers for sorting cards.
    /// </summary>
    /// <remarks>
    ///     The rank/suit comparers target standard playing cards (custom cards have no rank or suit and compare
    ///     equal). For custom card data use <see cref="ByValueAscending" />, <see cref="ByValueDescending" /> or
    ///     <see cref="ByGroupThenValueAscending" />. Pass any comparer to <c>HandModel.Sort</c>,
    ///     <c>HandComponent.SortAsync</c> or <c>HandPresenter.SortAsync</c>.
    /// </remarks>
    public static class CardComparer
    {
        /// <summary>
        ///     Compare by rank ascending.
        /// </summary>
        public static readonly IComparer<CardData> ByRankAscending = new RankAscendingComparer();

        /// <summary>
        ///     Compare by rank descending.
        /// </summary>
        public static readonly IComparer<CardData> ByRankDescending = new RankDescendingComparer();

        /// <summary>
        ///     Compare by suit, then rank ascending.
        /// </summary>
        public static readonly IComparer<CardData> BySuitThenRankAscending = new SuitThenRankAscendingComparer();

        /// <summary>
        ///     Compare by suit, then rank descending.
        /// </summary>
        public static readonly IComparer<CardData> BySuitThenRankDescending = new SuitThenRankDescendingComparer();

        /// <summary>
        ///     Compare by <see cref="CardData.CompareTo" /> ascending: custom cards by
        ///     <see cref="CardData.SortValue" /> then id, standard cards by rank, jokers last.
        /// </summary>
        public static readonly IComparer<CardData> ByValueAscending = new ValueComparer(true);

        /// <summary>
        ///     Compare by <see cref="CardData.CompareTo" /> descending.
        /// </summary>
        public static readonly IComparer<CardData> ByValueDescending = new ValueComparer(false);

        /// <summary>
        ///     Compare by <see cref="CardData.Group" /> (ordinal), then by value ascending. Groups custom cards by
        ///     faction/class/color; standard cards group by suit name.
        /// </summary>
        public static readonly IComparer<CardData> ByGroupThenValueAscending = new GroupThenValueComparer();

        /// <summary>
        ///     Creates a comparer that accounts for trump (trumps sort last).
        /// </summary>
        /// <param name="trump">Trump suit.</param>
        /// <param name="ascending">Rank ascending if true.</param>
        /// <returns>Comparer instance.</returns>
        public static IComparer<CardData> WithTrump(Suit trump, bool ascending = true)
        {
            return new TrumpComparer(trump, ascending);
        }

        private class RankAscendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                {
                    return x.IsRedJoker.CompareTo(y.IsRedJoker);
                }

                if (x.IsJoker)
                {
                    return 1;
                }

                if (y.IsJoker)
                {
                    return -1;
                }

                return ((int)x.Rank).CompareTo((int)y.Rank);
            }
        }

        private class RankDescendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                {
                    return y.IsRedJoker.CompareTo(x.IsRedJoker);
                }

                if (x.IsJoker)
                {
                    return -1;
                }

                if (y.IsJoker)
                {
                    return 1;
                }

                return ((int)y.Rank).CompareTo((int)x.Rank);
            }
        }

        private class SuitThenRankAscendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                {
                    return x.IsRedJoker.CompareTo(y.IsRedJoker);
                }

                if (x.IsJoker)
                {
                    return 1;
                }

                if (y.IsJoker)
                {
                    return -1;
                }

                int suitCompare = ((int)x.Suit).CompareTo((int)y.Suit);
                if (suitCompare != 0)
                {
                    return suitCompare;
                }

                return ((int)x.Rank).CompareTo((int)y.Rank);
            }
        }

        private class SuitThenRankDescendingComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                {
                    return y.IsRedJoker.CompareTo(x.IsRedJoker);
                }

                if (x.IsJoker)
                {
                    return -1;
                }

                if (y.IsJoker)
                {
                    return 1;
                }

                int suitCompare = ((int)y.Suit).CompareTo((int)x.Suit);
                if (suitCompare != 0)
                {
                    return suitCompare;
                }

                return ((int)y.Rank).CompareTo((int)x.Rank);
            }
        }

        private sealed class ValueComparer : IComparer<CardData>
        {
            private readonly bool _ascending;

            public ValueComparer(bool ascending)
            {
                _ascending = ascending;
            }

            public int Compare(CardData x, CardData y)
            {
                return _ascending ? x.CompareTo(y) : y.CompareTo(x);
            }
        }

        private sealed class GroupThenValueComparer : IComparer<CardData>
        {
            public int Compare(CardData x, CardData y)
            {
                int groupCompare = string.CompareOrdinal(x.Group ?? string.Empty, y.Group ?? string.Empty);
                return groupCompare != 0 ? groupCompare : x.CompareTo(y);
            }
        }

        private class TrumpComparer : IComparer<CardData>
        {
            private readonly bool _ascending;
            private readonly Suit _trump;

            public TrumpComparer(Suit trump, bool ascending)
            {
                _trump = trump;
                _ascending = ascending;
            }

            public int Compare(CardData x, CardData y)
            {
                if (x.IsJoker && y.IsJoker)
                {
                    return _ascending ? x.IsRedJoker.CompareTo(y.IsRedJoker) : y.IsRedJoker.CompareTo(x.IsRedJoker);
                }

                if (x.IsJoker)
                {
                    return _ascending ? 1 : -1;
                }

                if (y.IsJoker)
                {
                    return _ascending ? -1 : 1;
                }

                bool xTrump = x.Suit == _trump;
                bool yTrump = y.Suit == _trump;

                if (xTrump && !yTrump)
                {
                    return 1;
                }

                if (!xTrump && yTrump)
                {
                    return -1;
                }

                int suitCompare = ((int)x.Suit).CompareTo((int)y.Suit);
                if (suitCompare != 0)
                {
                    return _ascending ? suitCompare : -suitCompare;
                }

                return _ascending ? ((int)x.Rank).CompareTo((int)y.Rank) : ((int)y.Rank).CompareTo((int)x.Rank);
            }
        }
    }
}
