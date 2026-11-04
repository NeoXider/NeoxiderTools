using System;
using System.Collections.Generic;

namespace Neo.Cards.Poker
{
    /// <summary>
    ///     Evaluates poker hands (best five from 5–7 cards).
    /// </summary>
    /// <remarks>
    ///     Every five-card combination is scored without allocations; only the winning combination is turned into
    ///     a <see cref="PokerHandResult" />. Use <see cref="EvaluateStrength" /> in hot loops (AI, equity, outs)
    ///     where only the comparable strength is needed.
    /// </remarks>
    public static class PokerHandEvaluator
    {
        private const int HandSize = 5;
        private const int MaxStackCards = 64;
        private const int RankBits = 4;
        private const int CombinationShift = RankBits * HandSize;

        /// <summary>
        ///     Evaluates a 5–7 card hand and returns the best five-card combination.
        /// </summary>
        /// <param name="cards">Cards to evaluate (5–7, non-joker).</param>
        /// <returns>Evaluation result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cards" /> is null.</exception>
        /// <exception cref="ArgumentException">Custom/invalid cards, or fewer than five non-joker cards.</exception>
        public static PokerHandResult Evaluate(IEnumerable<CardData> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            List<CardData> cardList = cards is IReadOnlyCollection<CardData> sized ? new(sized.Count) : new();
            foreach (CardData card in cards)
            {
                if (!card.IsJoker)
                {
                    cardList.Add(card);
                }
            }

            Span<int> indices = cardList.Count <= MaxStackCards
                ? stackalloc int[cardList.Count]
                : new int[cardList.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            ValidateStandardCards(cardList, indices);
            Span<int> best = stackalloc int[HandSize];
            FindBestCombination(cardList, indices, best);
            return BuildResult(cardList, best);
        }

        /// <summary>
        ///     Allocation-free strength of the best five-card hand among the non-joker cards. A higher value is a
        ///     stronger hand and equal values split the pot, exactly like <see cref="PokerHandResult.CompareTo" />.
        /// </summary>
        /// <param name="cards">Cards to evaluate (5+ non-joker standard cards; jokers are ignored).</param>
        /// <returns>Comparable hand strength; decode the combination with <see cref="GetCombination" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cards" /> is null.</exception>
        /// <exception cref="ArgumentException">Custom/invalid cards, or fewer than five non-joker cards.</exception>
        public static int EvaluateStrength(IReadOnlyList<CardData> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            Span<int> buffer = cards.Count <= MaxStackCards ? stackalloc int[cards.Count] : new int[cards.Count];
            int count = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].IsJoker)
                {
                    buffer[count++] = i;
                }
            }

            Span<int> indices = buffer.Slice(0, count);
            ValidateStandardCards(cards, indices);
            Span<int> best = stackalloc int[HandSize];
            return FindBestCombination(cards, indices, best);
        }

        /// <summary>
        ///     Decodes the combination from a value returned by <see cref="EvaluateStrength" />.
        /// </summary>
        /// <param name="strength">Hand strength.</param>
        /// <returns>Combination type.</returns>
        public static PokerCombination GetCombination(int strength)
        {
            return (PokerCombination)(strength >> CombinationShift);
        }

        private static void ValidateStandardCards(IReadOnlyList<CardData> cards, ReadOnlySpan<int> indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                ValidateStandardCard(cards[indices[i]]);
            }

            if (indices.Length < HandSize)
            {
                throw new ArgumentException("At least 5 non-joker cards are required for evaluation");
            }
        }

        private static void ValidateStandardCard(CardData card)
        {
            if (card.IsCustom || card.Rank < Rank.Two || card.Rank > Rank.Ace ||
                card.Suit < Suit.Hearts || card.Suit > Suit.Spades)
            {
                throw new ArgumentException("Poker evaluation requires standard suit/rank cards.", "cards");
            }
        }

        /// <summary>
        ///     Scores every five-card combination in lexicographic index order and keeps the first strongest one.
        /// </summary>
        private static int FindBestCombination(IReadOnlyList<CardData> cards, ReadOnlySpan<int> indices,
            Span<int> best)
        {
            int count = indices.Length;
            int bestScore = -1;
            Span<int> current = stackalloc int[HandSize];
            Span<int> sequence = stackalloc int[HandSize];
            for (int a = 0; a <= count - 5; a++)
            {
                current[0] = indices[a];
                for (int b = a + 1; b <= count - 4; b++)
                {
                    current[1] = indices[b];
                    for (int c = b + 1; c <= count - 3; c++)
                    {
                        current[2] = indices[c];
                        for (int d = c + 1; d <= count - 2; d++)
                        {
                            current[3] = indices[d];
                            for (int e = d + 1; e <= count - 1; e++)
                            {
                                current[4] = indices[e];
                                int score = ScoreFive(cards, current, sequence, out _, out _, out _);
                                // WHY: strict ">" keeps the earliest of equal combinations, as the original did.
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    current.CopyTo(best);
                                }
                            }
                        }
                    }
                }
            }

            return bestScore;
        }

        /// <summary>
        ///     Scores five cards. <paramref name="sequence" /> receives the combination ranks followed by the
        ///     kickers, in the order <see cref="PokerHandResult" /> compares them.
        /// </summary>
        private static int ScoreFive(IReadOnlyList<CardData> cards, ReadOnlySpan<int> hand, Span<int> sequence,
            out PokerCombination combination, out int combinationRankCount, out int sequenceLength)
        {
            Span<int> ranks = stackalloc int[HandSize];
            Span<int> rankCounts = stackalloc int[(int)Rank.Ace + 1];
            Suit firstSuit = cards[hand[0]].Suit;
            bool flush = true;
            for (int i = 0; i < HandSize; i++)
            {
                CardData card = cards[hand[i]];
                int rank = (int)card.Rank;
                flush &= card.Suit == firstSuit;
                rankCounts[rank]++;

                int j = i - 1;
                while (j >= 0 && ranks[j] < rank)
                {
                    ranks[j + 1] = ranks[j];
                    j--;
                }

                ranks[j + 1] = rank;
            }

            int four = 0;
            int three = 0;
            int highPair = 0;
            int lowPair = 0;
            int distinct = 0;
            for (int rank = (int)Rank.Ace; rank >= (int)Rank.Two; rank--)
            {
                int amount = rankCounts[rank];
                if (amount == 0)
                {
                    continue;
                }

                distinct++;
                if (amount == 4)
                {
                    four = rank;
                }
                else if (amount == 3)
                {
                    three = rank;
                }
                else if (amount == 2)
                {
                    if (highPair == 0)
                    {
                        highPair = rank;
                    }
                    else
                    {
                        lowPair = rank;
                    }
                }
            }

            int straightHigh = 0;
            if (distinct == HandSize)
            {
                if (ranks[0] - ranks[4] == 4)
                {
                    straightHigh = ranks[0];
                }
                else if (ranks[0] == (int)Rank.Ace && ranks[1] == (int)Rank.Five && ranks[4] == (int)Rank.Two)
                {
                    straightHigh = (int)Rank.Five;
                }
            }

            sequenceLength = 0;
            if (flush && straightHigh != 0)
            {
                combination = straightHigh == (int)Rank.Ace ? PokerCombination.RoyalFlush : PokerCombination.StraightFlush;
                combinationRankCount = 1;
                sequence[sequenceLength++] = straightHigh;
            }
            else if (four != 0)
            {
                combination = PokerCombination.FourOfAKind;
                combinationRankCount = 1;
                sequence[sequenceLength++] = four;
                AppendKickers(ranks, four, 0, 1, sequence, ref sequenceLength);
            }
            else if (three != 0 && highPair != 0)
            {
                combination = PokerCombination.FullHouse;
                combinationRankCount = 2;
                sequence[sequenceLength++] = three;
                sequence[sequenceLength++] = highPair;
            }
            else if (flush)
            {
                combination = PokerCombination.Flush;
                combinationRankCount = HandSize;
                for (int i = 0; i < HandSize; i++)
                {
                    sequence[sequenceLength++] = ranks[i];
                }
            }
            else if (straightHigh != 0)
            {
                combination = PokerCombination.Straight;
                combinationRankCount = 1;
                sequence[sequenceLength++] = straightHigh;
            }
            else if (three != 0)
            {
                combination = PokerCombination.ThreeOfAKind;
                combinationRankCount = 1;
                sequence[sequenceLength++] = three;
                AppendKickers(ranks, three, 0, 2, sequence, ref sequenceLength);
            }
            else if (lowPair != 0)
            {
                combination = PokerCombination.TwoPair;
                combinationRankCount = 2;
                sequence[sequenceLength++] = highPair;
                sequence[sequenceLength++] = lowPair;
                AppendKickers(ranks, highPair, lowPair, 1, sequence, ref sequenceLength);
            }
            else if (highPair != 0)
            {
                combination = PokerCombination.Pair;
                combinationRankCount = 1;
                sequence[sequenceLength++] = highPair;
                AppendKickers(ranks, highPair, 0, 3, sequence, ref sequenceLength);
            }
            else
            {
                combination = PokerCombination.HighCard;
                combinationRankCount = 1;
                for (int i = 0; i < HandSize; i++)
                {
                    sequence[sequenceLength++] = ranks[i];
                }
            }

            int score = (int)combination;
            for (int i = 0; i < HandSize; i++)
            {
                score = (score << RankBits) | (i < sequenceLength ? sequence[i] : 0);
            }

            return score;
        }

        private static void AppendKickers(ReadOnlySpan<int> sortedRanks, int excluded, int alsoExcluded, int take,
            Span<int> sequence, ref int sequenceLength)
        {
            for (int i = 0; i < sortedRanks.Length && take > 0; i++)
            {
                int rank = sortedRanks[i];
                if (rank != excluded && rank != alsoExcluded)
                {
                    sequence[sequenceLength++] = rank;
                    take--;
                }
            }
        }

        private static PokerHandResult BuildResult(IReadOnlyList<CardData> cards, ReadOnlySpan<int> best)
        {
            Span<int> sequence = stackalloc int[HandSize];
            ScoreFive(cards, best, sequence, out PokerCombination combination, out int combinationRankCount,
                out int sequenceLength);

            Rank[] combinationRanks = new Rank[combinationRankCount];
            for (int i = 0; i < combinationRankCount; i++)
            {
                combinationRanks[i] = (Rank)sequence[i];
            }

            Rank[] kickers = sequenceLength > combinationRankCount
                ? new Rank[sequenceLength - combinationRankCount]
                : Array.Empty<Rank>();
            for (int i = 0; i < kickers.Length; i++)
            {
                kickers[i] = (Rank)sequence[combinationRankCount + i];
            }

            // WHY: the best hand lists its cards by rank, highest first; equal ranks keep input order (stable).
            CardData[] bestHand = new CardData[HandSize];
            for (int i = 0; i < HandSize; i++)
            {
                CardData card = cards[best[i]];
                int j = i - 1;
                while (j >= 0 && bestHand[j].Rank < card.Rank)
                {
                    bestHand[j + 1] = bestHand[j];
                    j--;
                }

                bestHand[j + 1] = card;
            }

            return new PokerHandResult(combination, combinationRanks, kickers, bestHand);
        }
    }
}
