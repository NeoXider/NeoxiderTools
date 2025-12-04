using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Cards.Poker
{
    /// <summary>
    /// Результат оценки покерной руки
    /// </summary>
    public class PokerHandResult : IComparable<PokerHandResult>
    {
        /// <summary>
        /// Тип комбинации
        /// </summary>
        public PokerCombination Combination { get; }

        /// <summary>
        /// Ранги карт в комбинации (для сравнения одинаковых комбинаций)
        /// </summary>
        public IReadOnlyList<Rank> CombinationRanks { get; }

        /// <summary>
        /// Кикеры - дополнительные карты для определения победителя при равных комбинациях
        /// </summary>
        public IReadOnlyList<Rank> Kickers { get; }

        /// <summary>
        /// Карты, составляющие лучшую руку
        /// </summary>
        public IReadOnlyList<CardData> BestHand { get; }

        /// <summary>
        /// Создаёт результат оценки руки
        /// </summary>
        /// <param name="combination">Тип комбинации</param>
        /// <param name="combinationRanks">Ранги в комбинации</param>
        /// <param name="kickers">Кикеры</param>
        /// <param name="bestHand">Лучшая рука из 5 карт</param>
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
        /// Сравнивает две руки
        /// </summary>
        /// <param name="other">Другая рука</param>
        /// <returns>Положительное если эта рука сильнее, отрицательное если слабее, 0 если равны</returns>
        public int CompareTo(PokerHandResult other)
        {
            if (other == null) return 1;

            int combinationCompare = Combination.CompareTo(other.Combination);
            if (combinationCompare != 0) return combinationCompare;

            int ranksCompare = CompareRankLists(CombinationRanks, other.CombinationRanks);
            if (ranksCompare != 0) return ranksCompare;

            return CompareRankLists(Kickers, other.Kickers);
        }

        /// <summary>
        /// Проверяет, сильнее ли эта рука
        /// </summary>
        public bool IsStrongerThan(PokerHandResult other)
        {
            return CompareTo(other) > 0;
        }

        /// <summary>
        /// Проверяет, равны ли руки по силе
        /// </summary>
        public bool IsEqualTo(PokerHandResult other)
        {
            return CompareTo(other) == 0;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Combination.ToRussianName());

            if (CombinationRanks.Count > 0)
            {
                sb.Append(" (");
                for (int i = 0; i < CombinationRanks.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(CombinationRanks[i].ToShortString());
                }
                sb.Append(")");
            }

            if (Kickers.Count > 0)
            {
                sb.Append(" кикеры: ");
                for (int i = 0; i < Kickers.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
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
                if (compare != 0) return compare;
            }

            return 0;
        }
    }
}

