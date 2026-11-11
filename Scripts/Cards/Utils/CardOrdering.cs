using System;
using System.Collections.Generic;

namespace Neo.Cards
{
    /// <summary>
    ///     Allocation-light stable sorting shared by hand models, components and presenters.
    /// </summary>
    /// <remarks>
    ///     WHY: <see cref="List{T}.Sort(Comparison{T})" /> is unstable. Hands sort their data model and their
    ///     view list separately; a stable sort over identical keys yields the identical permutation for both,
    ///     so index-based removal keeps model and view in sync even with equal ranks or duplicate cards.
    /// </remarks>
    internal static class CardOrdering
    {
        private const int InsertionSortThreshold = 32;

        internal static readonly Comparison<CardData> RankAscending = CompareRankAscending;
        internal static readonly Comparison<CardData> RankDescending = CompareRankDescending;
        internal static readonly Comparison<CardData> SuitThenRankAscending = CompareSuitThenRankAscending;
        internal static readonly Comparison<CardData> SuitThenRankDescending = CompareSuitThenRankDescending;

        /// <summary>
        ///     Stable in-place sort. Small lists use insertion sort without allocating.
        /// </summary>
        internal static void StableSort<T>(List<T> items, Comparison<T> comparison)
        {
            int count = items.Count;
            if (count < 2)
            {
                return;
            }

            if (count <= InsertionSortThreshold)
            {
                InsertionSort(items, comparison);
                return;
            }

            T[] source = items.ToArray();
            T[] buffer = new T[count];
            MergeSort(source, buffer, 0, count, comparison);
            for (int i = 0; i < count; i++)
            {
                items[i] = source[i];
            }
        }

        private static void InsertionSort<T>(List<T> items, Comparison<T> comparison)
        {
            for (int i = 1; i < items.Count; i++)
            {
                T current = items[i];
                int j = i - 1;
                while (j >= 0 && comparison(items[j], current) > 0)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = current;
            }
        }

        private static void MergeSort<T>(T[] items, T[] buffer, int start, int end, Comparison<T> comparison)
        {
            if (end - start < 2)
            {
                return;
            }

            int middle = start + (end - start) / 2;
            MergeSort(items, buffer, start, middle, comparison);
            MergeSort(items, buffer, middle, end, comparison);

            int left = start;
            int right = middle;
            int write = start;
            while (left < middle && right < end)
            {
                // WHY: "<= 0" takes the left element on ties, which is what makes the merge stable.
                buffer[write++] = comparison(items[left], items[right]) <= 0 ? items[left++] : items[right++];
            }

            while (left < middle)
            {
                buffer[write++] = items[left++];
            }

            while (right < end)
            {
                buffer[write++] = items[right++];
            }

            Array.Copy(buffer, start, items, start, end - start);
        }

        // WHY: enum CompareTo(object) boxes both operands; comparing the underlying ints allocates nothing.
        private static int CompareRankAscending(CardData a, CardData b)
        {
            return ((int)a.Rank).CompareTo((int)b.Rank);
        }

        private static int CompareRankDescending(CardData a, CardData b)
        {
            return ((int)b.Rank).CompareTo((int)a.Rank);
        }

        private static int CompareSuitThenRankAscending(CardData a, CardData b)
        {
            int suitCompare = ((int)a.Suit).CompareTo((int)b.Suit);
            return suitCompare != 0 ? suitCompare : ((int)a.Rank).CompareTo((int)b.Rank);
        }

        private static int CompareSuitThenRankDescending(CardData a, CardData b)
        {
            int suitCompare = ((int)b.Suit).CompareTo((int)a.Suit);
            return suitCompare != 0 ? suitCompare : ((int)b.Rank).CompareTo((int)a.Rank);
        }
    }
}
