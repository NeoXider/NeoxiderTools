using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Cards.Poker
{
    /// <summary>
    ///     Result of evaluating a poker hand.
    /// </summary>
    public class PokerHandResult : IComparable<PokerHandResult>
    {
        /// <summary>
        ///     Creates a hand evaluation result.
        /// </summary>
        /// <param name="combination">Combination type.</param>
        /// <param name="combinationRanks">Ranks that form the combination.</param>
        /// <param name="kickers">Kicker ranks.</param>
        /// <param name="bestHand">Best five-card hand.</param>
        public PokerHandResult(
            PokerCombination combination,
            IReadOnlyList<Rank> combinationRanks,
            IReadOnlyList<Rank> kickers,
            IReadOnlyList<CardData> bestHand)
        {
            Combination = combination;
            CombinationRanks = combinationRanks ?? Array.Empty<Rank>();
            Kickers = kickers ?? Array.Empty<Rank>();
            BestHand = bestHand ?? Array.Empty<CardData>();
        }

        /// <summary>
        ///     Combination type.
        /// </summary>
        public PokerCombination Combination { get; }

        /// <summary>
        ///     Ranks in the combination (for breaking ties).
        /// </summary>
        public IReadOnlyList<Rank> CombinationRanks { get; }

        /// <summary>
        ///     Kicker ranks when combinations tie.
        /// </summary>
        public IReadOnlyList<Rank> Kickers { get; }

        /// <summary>
        ///     Cards that form the best five-card hand.
        /// </summary>
        public IReadOnlyList<CardData> BestHand { get; }

        /// <summary>
        ///     Compares two evaluated hands.
        /// </summary>
        /// <param name="other">Other hand.</param>
        /// <returns>Positive if this hand is stronger, negative if weaker, zero if equal.</returns>
        public int CompareTo(PokerHandResult other)
        {
            if (other == null)
            {
                return 1;
            }

            int combinationCompare = Combination.CompareTo(other.Combination);
            if (combinationCompare != 0)
            {
                return combinationCompare;
            }

            int ranksCompare = CompareRankLists(CombinationRanks, other.CombinationRanks);
            if (ranksCompare != 0)
            {
                return ranksCompare;
            }

            return CompareRankLists(Kickers, other.Kickers);
        }

        /// <summary>
        ///     Returns whether this hand is stronger than the other.
        /// </summary>
        public bool IsStrongerThan(PokerHandResult other)
        {
            return CompareTo(other) > 0;
        }

        /// <summary>
        ///     Returns whether both hands are equal in strength.
        /// </summary>
        public bool IsEqualTo(PokerHandResult other)
        {
            return CompareTo(other) == 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(Combination.ToEnglishName());

            if (CombinationRanks.Count > 0)
            {
                sb.Append(" (");
                for (int i = 0; i < CombinationRanks.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(CombinationRanks[i].ToShortString());
                }

                sb.Append(")");
            }

            if (Kickers.Count > 0)
            {
                sb.Append(" kickers: ");
                for (int i = 0; i < Kickers.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(Kickers[i].ToShortString());
                }
            }

            return sb.ToString();
        }

        private static int CompareRankLists(IReadOnlyList<Rank> a, IReadOnlyList<Rank> b)
        {
            int minCount = Math.Min(a.Count, b.Count);

            for (int i = 0; i < minCount; i++)
            {
                int compare = a[i].CompareTo(b[i]);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return 0;
        }
    }
}
