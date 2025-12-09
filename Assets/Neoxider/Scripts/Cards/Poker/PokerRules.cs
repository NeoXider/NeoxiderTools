using System.Collections.Generic;
using System.Linq;

namespace Neo.Cards.Poker
{
    /// <summary>
    ///     Правила покера для определения победителя
    /// </summary>
    public static class PokerRules
    {
        /// <summary>
        ///     Сравнивает две руки
        /// </summary>
        /// <param name="hand1">Первая рука</param>
        /// <param name="hand2">Вторая рука</param>
        /// <returns>Положительное если первая рука сильнее, отрицательное если вторая, 0 если равны</returns>
        public static int CompareHands(PokerHandResult hand1, PokerHandResult hand2)
        {
            if (hand1 == null && hand2 == null)
            {
                return 0;
            }

            if (hand1 == null)
            {
                return -1;
            }

            if (hand2 == null)
            {
                return 1;
            }

            return hand1.CompareTo(hand2);
        }

        /// <summary>
        ///     Сравнивает две руки по картам
        /// </summary>
        /// <param name="cards1">Карты первого игрока</param>
        /// <param name="cards2">Карты второго игрока</param>
        /// <returns>Положительное если первая рука сильнее, отрицательное если вторая, 0 если равны</returns>
        public static int CompareHands(IEnumerable<CardData> cards1, IEnumerable<CardData> cards2)
        {
            PokerHandResult result1 = PokerHandEvaluator.Evaluate(cards1);
            PokerHandResult result2 = PokerHandEvaluator.Evaluate(cards2);

            return CompareHands(result1, result2);
        }

        /// <summary>
        ///     Определяет победителей среди нескольких рук
        /// </summary>
        /// <param name="hands">Список результатов оценки рук</param>
        /// <returns>Индексы победителей (может быть несколько при split pot)</returns>
        public static List<int> GetWinners(IList<PokerHandResult> hands)
        {
            if (hands == null || hands.Count == 0)
            {
                return new List<int>();
            }

            List<int> winners = new() { 0 };
            PokerHandResult bestHand = hands[0];

            for (int i = 1; i < hands.Count; i++)
            {
                if (hands[i] == null)
                {
                    continue;
                }

                int compare = hands[i].CompareTo(bestHand);

                if (compare > 0)
                {
                    winners.Clear();
                    winners.Add(i);
                    bestHand = hands[i];
                }
                else if (compare == 0)
                {
                    winners.Add(i);
                }
            }

            return winners;
        }

        /// <summary>
        ///     Определяет победителей по картам игроков
        /// </summary>
        /// <param name="playerCards">Карты каждого игрока</param>
        /// <returns>Индексы победителей</returns>
        public static List<int> GetWinners(IList<IEnumerable<CardData>> playerCards)
        {
            List<PokerHandResult> hands = playerCards
                .Select(cards => cards != null ? PokerHandEvaluator.Evaluate(cards) : null)
                .ToList();

            return GetWinners(hands);
        }

        /// <summary>
        ///     Определяет победителей в Texas Hold'em
        /// </summary>
        /// <param name="communityCards">Общие карты на столе (3-5 карт)</param>
        /// <param name="playerHoleCards">Карманные карты каждого игрока (по 2 карты)</param>
        /// <returns>Индексы победителей</returns>
        public static List<int> GetWinnersTexasHoldem(
            IEnumerable<CardData> communityCards,
            IList<IEnumerable<CardData>> playerHoleCards)
        {
            List<CardData> community = communityCards.ToList();
            List<PokerHandResult> hands = new();

            foreach (IEnumerable<CardData> holeCards in playerHoleCards)
            {
                if (holeCards == null)
                {
                    hands.Add(null);
                    continue;
                }

                List<CardData> allCards = community.Concat(holeCards).ToList();
                hands.Add(PokerHandEvaluator.Evaluate(allCards));
            }

            return GetWinners(hands);
        }

        /// <summary>
        ///     Оценивает руку в Texas Hold'em
        /// </summary>
        /// <param name="communityCards">Общие карты на столе</param>
        /// <param name="holeCards">Карманные карты игрока</param>
        /// <returns>Результат оценки</returns>
        public static PokerHandResult EvaluateTexasHoldem(
            IEnumerable<CardData> communityCards,
            IEnumerable<CardData> holeCards)
        {
            List<CardData> allCards = communityCards.Concat(holeCards).ToList();
            return PokerHandEvaluator.Evaluate(allCards);
        }

        /// <summary>
        ///     Проверяет, выиграл ли игрок (или сыграл вничью)
        /// </summary>
        /// <param name="playerIndex">Индекс игрока</param>
        /// <param name="hands">Все руки</param>
        /// <returns>true если игрок среди победителей</returns>
        public static bool IsWinner(int playerIndex, IList<PokerHandResult> hands)
        {
            List<int> winners = GetWinners(hands);
            return winners.Contains(playerIndex);
        }

        /// <summary>
        ///     Возвращает лучшую руку среди всех
        /// </summary>
        /// <param name="hands">Список рук</param>
        /// <returns>Лучшая рука или null</returns>
        public static PokerHandResult GetBestHand(IEnumerable<PokerHandResult> hands)
        {
            PokerHandResult best = null;

            foreach (PokerHandResult hand in hands)
            {
                if (hand == null)
                {
                    continue;
                }

                if (best == null || hand.CompareTo(best) > 0)
                {
                    best = hand;
                }
            }

            return best;
        }

        /// <summary>
        ///     Рассчитывает вероятность улучшения руки (аутсы)
        /// </summary>
        /// <param name="currentHand">Текущие карты</param>
        /// <param name="targetCombination">Желаемая комбинация</param>
        /// <param name="remainingDeck">Оставшиеся карты в колоде</param>
        /// <returns>Количество аутсов</returns>
        public static int CountOuts(
            IEnumerable<CardData> currentHand,
            PokerCombination targetCombination,
            IEnumerable<CardData> remainingDeck)
        {
            List<CardData> current = currentHand.ToList();
            int outs = 0;

            foreach (CardData card in remainingDeck)
            {
                List<CardData> testHand = current.Concat(new[] { card }).ToList();

                if (testHand.Count >= 5)
                {
                    PokerHandResult result = PokerHandEvaluator.Evaluate(testHand);
                    if (result.Combination >= targetCombination)
                    {
                        outs++;
                    }
                }
            }

            return outs;
        }
    }
}