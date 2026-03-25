using System.Collections.Generic;
using System.Linq;

namespace Neo.Cards.Poker
{
    /// <summary>
    ///     Poker rules helpers for comparing hands and winners.
    /// </summary>
    public static class PokerRules
    {
        /// <summary>
        ///     Compares two evaluated hands.
        /// </summary>
        /// <param name="hand1">First hand.</param>
        /// <param name="hand2">Second hand.</param>
        /// <returns>Positive if first wins, negative if second, zero if tied.</returns>
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
        ///     Compares two hands from raw card lists (evaluates each).
        /// </summary>
        /// <param name="cards1">First player's cards.</param>
        /// <param name="cards2">Second player's cards.</param>
        /// <returns>Positive if first wins, negative if second, zero if tied.</returns>
        public static int CompareHands(IEnumerable<CardData> cards1, IEnumerable<CardData> cards2)
        {
            PokerHandResult result1 = PokerHandEvaluator.Evaluate(cards1);
            PokerHandResult result2 = PokerHandEvaluator.Evaluate(cards2);

            return CompareHands(result1, result2);
        }

        /// <summary>
        ///     Winner indices among evaluated hands (split pots return multiple).
        /// </summary>
        /// <param name="hands">Evaluated hands.</param>
        /// <returns>Winning player indices.</returns>
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
        ///     Winner indices from each player's cards (evaluated per player).
        /// </summary>
        /// <param name="playerCards">Each player's cards.</param>
        /// <returns>Winning indices.</returns>
        public static List<int> GetWinners(IList<IEnumerable<CardData>> playerCards)
        {
            var hands = playerCards
                .Select(cards => cards != null ? PokerHandEvaluator.Evaluate(cards) : null)
                .ToList();

            return GetWinners(hands);
        }

        /// <summary>
        ///     Texas Hold'em winners from board and hole cards.
        /// </summary>
        /// <param name="communityCards">Board cards (3–5).</param>
        /// <param name="playerHoleCards">Each player's two hole cards.</param>
        /// <returns>Winning player indices.</returns>
        public static List<int> GetWinnersTexasHoldem(
            IEnumerable<CardData> communityCards,
            IList<IEnumerable<CardData>> playerHoleCards)
        {
            var community = communityCards.ToList();
            List<PokerHandResult> hands = new();

            foreach (IEnumerable<CardData> holeCards in playerHoleCards)
            {
                if (holeCards == null)
                {
                    hands.Add(null);
                    continue;
                }

                var allCards = community.Concat(holeCards).ToList();
                hands.Add(PokerHandEvaluator.Evaluate(allCards));
            }

            return GetWinners(hands);
        }

        /// <summary>
        ///     Evaluates a Texas Hold'em hand (board + hole cards).
        /// </summary>
        /// <param name="communityCards">Board.</param>
        /// <param name="holeCards">Player hole cards.</param>
        /// <returns>Evaluation result.</returns>
        public static PokerHandResult EvaluateTexasHoldem(
            IEnumerable<CardData> communityCards,
            IEnumerable<CardData> holeCards)
        {
            var allCards = communityCards.Concat(holeCards).ToList();
            return PokerHandEvaluator.Evaluate(allCards);
        }

        /// <summary>
        ///     Whether the player index is among the winners (including chops).
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <param name="hands">All evaluated hands.</param>
        /// <returns>True if that player ties or wins.</returns>
        public static bool IsWinner(int playerIndex, IList<PokerHandResult> hands)
        {
            List<int> winners = GetWinners(hands);
            return winners.Contains(playerIndex);
        }

        /// <summary>
        ///     Strongest hand in the sequence, or null if none.
        /// </summary>
        /// <param name="hands">Hand results.</param>
        /// <returns>Best hand or null.</returns>
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
        ///     Counts simple one-card outs: deck cards that improve to at least the target combination.
        /// </summary>
        /// <param name="currentHand">Known cards so far.</param>
        /// <param name="targetCombination">Minimum combination to count.</param>
        /// <param name="remainingDeck">Unknown / remaining deck cards.</param>
        /// <returns>Number of qualifying outs.</returns>
        public static int CountOuts(
            IEnumerable<CardData> currentHand,
            PokerCombination targetCombination,
            IEnumerable<CardData> remainingDeck)
        {
            var current = currentHand.ToList();
            int outs = 0;

            foreach (CardData card in remainingDeck)
            {
                var testHand = current.Concat(new[] { card }).ToList();

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
