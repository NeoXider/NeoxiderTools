using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Cards.Poker
{
    /// <summary>
    ///     Оценщик покерных комбинаций
    /// </summary>
    public static class PokerHandEvaluator
    {
        /// <summary>
        ///     Оценивает руку из 5-7 карт и возвращает лучшую комбинацию
        /// </summary>
        /// <param name="cards">Карты для оценки (5-7 штук)</param>
        /// <returns>Результат оценки</returns>
        public static PokerHandResult Evaluate(IEnumerable<CardData> cards)
        {
            List<CardData> cardList = cards.Where(c => !c.IsJoker).ToList();

            if (cardList.Count < 5)
            {
                throw new ArgumentException("Требуется минимум 5 карт для оценки");
            }

            if (cardList.Count == 5)
            {
                return EvaluateFiveCards(cardList);
            }

            return FindBestHand(cardList);
        }

        /// <summary>
        ///     Находит лучшую комбинацию из 5 карт среди 6-7 карт
        /// </summary>
        private static PokerHandResult FindBestHand(List<CardData> cards)
        {
            PokerHandResult bestResult = null;

            foreach (IEnumerable<CardData> combination in GetCombinations(cards, 5))
            {
                PokerHandResult result = EvaluateFiveCards(combination.ToList());

                if (bestResult == null || result.CompareTo(bestResult) > 0)
                {
                    bestResult = result;
                }
            }

            return bestResult;
        }

        /// <summary>
        ///     Оценивает ровно 5 карт
        /// </summary>
        private static PokerHandResult EvaluateFiveCards(List<CardData> cards)
        {
            List<CardData> sortedCards = cards.OrderByDescending(c => c.Rank).ToList();
            Dictionary<int, List<Rank>> groups = GetRankGroups(sortedCards);
            bool isFlush = IsFlush(sortedCards);
            bool isStraight = IsStraight(sortedCards, out Rank highCard);

            if (isFlush && isStraight)
            {
                if (highCard == Rank.Ace)
                {
                    return new PokerHandResult(
                        PokerCombination.RoyalFlush,
                        new[] { Rank.Ace },
                        Array.Empty<Rank>(),
                        sortedCards);
                }

                return new PokerHandResult(
                    PokerCombination.StraightFlush,
                    new[] { highCard },
                    Array.Empty<Rank>(),
                    sortedCards);
            }

            if (groups.ContainsKey(4))
            {
                Rank fourRank = groups[4][0];
                List<Rank> kickers = sortedCards
                    .Where(c => c.Rank != fourRank)
                    .Select(c => c.Rank)
                    .Take(1)
                    .ToList();

                return new PokerHandResult(
                    PokerCombination.FourOfAKind,
                    new[] { fourRank },
                    kickers,
                    sortedCards);
            }

            if (groups.ContainsKey(3) && groups.ContainsKey(2))
            {
                Rank threeRank = groups[3][0];
                Rank pairRank = groups[2][0];

                return new PokerHandResult(
                    PokerCombination.FullHouse,
                    new[] { threeRank, pairRank },
                    Array.Empty<Rank>(),
                    sortedCards);
            }

            if (isFlush)
            {
                List<Rank> ranks = sortedCards.Select(c => c.Rank).ToList();

                return new PokerHandResult(
                    PokerCombination.Flush,
                    ranks,
                    Array.Empty<Rank>(),
                    sortedCards);
            }

            if (isStraight)
            {
                return new PokerHandResult(
                    PokerCombination.Straight,
                    new[] { highCard },
                    Array.Empty<Rank>(),
                    sortedCards);
            }

            if (groups.ContainsKey(3))
            {
                Rank threeRank = groups[3][0];
                List<Rank> kickers = sortedCards
                    .Where(c => c.Rank != threeRank)
                    .Select(c => c.Rank)
                    .Take(2)
                    .ToList();

                return new PokerHandResult(
                    PokerCombination.ThreeOfAKind,
                    new[] { threeRank },
                    kickers,
                    sortedCards);
            }

            if (groups.ContainsKey(2))
            {
                List<Rank> pairs = groups[2].OrderByDescending(r => r).ToList();

                if (pairs.Count >= 2)
                {
                    List<Rank> kickers = sortedCards
                        .Where(c => c.Rank != pairs[0] && c.Rank != pairs[1])
                        .Select(c => c.Rank)
                        .Take(1)
                        .ToList();

                    return new PokerHandResult(
                        PokerCombination.TwoPair,
                        new[] { pairs[0], pairs[1] },
                        kickers,
                        sortedCards);
                }

                Rank pairRank = pairs[0];
                List<Rank> kickersList = sortedCards
                    .Where(c => c.Rank != pairRank)
                    .Select(c => c.Rank)
                    .Take(3)
                    .ToList();

                return new PokerHandResult(
                    PokerCombination.Pair,
                    new[] { pairRank },
                    kickersList,
                    sortedCards);
            }

            List<Rank> highCardKickers = sortedCards.Select(c => c.Rank).ToList();

            return new PokerHandResult(
                PokerCombination.HighCard,
                new[] { highCardKickers[0] },
                highCardKickers.Skip(1).ToList(),
                sortedCards);
        }

        /// <summary>
        ///     Группирует карты по количеству повторений ранга
        /// </summary>
        private static Dictionary<int, List<Rank>> GetRankGroups(List<CardData> cards)
        {
            Dictionary<Rank, int> rankCounts = cards
                .GroupBy(c => c.Rank)
                .ToDictionary(g => g.Key, g => g.Count());

            Dictionary<int, List<Rank>> result = new();

            foreach (KeyValuePair<Rank, int> kvp in rankCounts)
            {
                int count = kvp.Value;
                if (!result.ContainsKey(count))
                {
                    result[count] = new List<Rank>();
                }

                result[count].Add(kvp.Key);
            }

            foreach (List<Rank> list in result.Values)
            {
                list.Sort((a, b) => b.CompareTo(a));
            }

            return result;
        }

        /// <summary>
        ///     Проверяет, является ли рука флешем (все карты одной масти)
        /// </summary>
        private static bool IsFlush(List<CardData> cards)
        {
            if (cards.Count == 0)
            {
                return false;
            }

            Suit firstSuit = cards[0].Suit;
            return cards.All(c => c.Suit == firstSuit);
        }

        /// <summary>
        ///     Проверяет, является ли рука стритом (последовательность)
        /// </summary>
        private static bool IsStraight(List<CardData> cards, out Rank highCard)
        {
            List<int> ranks = cards.Select(c => (int)c.Rank).Distinct().OrderByDescending(r => r).ToList();
            highCard = (Rank)ranks[0];

            if (ranks.Count < 5)
            {
                return false;
            }

            if (IsConsecutive(ranks))
            {
                highCard = (Rank)ranks[0];
                return true;
            }

            if (ranks.Contains((int)Rank.Ace) &&
                ranks.Contains((int)Rank.Five) &&
                ranks.Contains((int)Rank.Four) &&
                ranks.Contains((int)Rank.Three) &&
                ranks.Contains((int)Rank.Two))
            {
                highCard = Rank.Five;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Проверяет, являются ли ранги последовательными
        /// </summary>
        private static bool IsConsecutive(List<int> ranks)
        {
            for (int i = 0; i < ranks.Count - 1; i++)
            {
                if (ranks[i] - ranks[i + 1] != 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Генерирует все комбинации заданного размера
        /// </summary>
        private static IEnumerable<IEnumerable<T>> GetCombinations<T>(IList<T> list, int length)
        {
            if (length == 0)
            {
                yield return Array.Empty<T>();
                yield break;
            }

            for (int i = 0; i <= list.Count - length; i++)
            {
                foreach (IEnumerable<T> tail in GetCombinations(list.Skip(i + 1).ToList(), length - 1))
                {
                    yield return new[] { list[i] }.Concat(tail);
                }
            }
        }
    }
}