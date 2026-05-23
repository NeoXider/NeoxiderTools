using System.Collections.Generic;
using System.Linq;
using Neo.Cards;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class CardSystemTests
    {
        [Test]
        public void CardData_EqualityAndComparison_Correct()
        {
            var card1 = new CardData(Suit.Hearts, Rank.Ace);
            var card2 = new CardData(Suit.Hearts, Rank.Ace);
            var card3 = new CardData(Suit.Spades, Rank.King);

            Assert.AreEqual(card1, card2);
            Assert.AreNotEqual(card1, card3);

            var card4 = new CardData(Suit.Hearts, Rank.King);
            Assert.IsTrue(card1.Beats(card4, Suit.Clubs), "Ace of Hearts should beat King of Hearts");

            Assert.IsFalse(card1.Beats(card3, Suit.Clubs), "Different suits, neither is trump -> false");

            var trumpCard = new CardData(Suit.Clubs, Rank.Two);
            var nonTrumpCard = new CardData(Suit.Spades, Rank.Ace);
            Assert.IsTrue(trumpCard.Beats(nonTrumpCard, Suit.Clubs), "Trump 2 should beat non-trump Ace");

            var redJoker = CardData.CreateJoker(true);
            var blackJoker = CardData.CreateJoker(false);

            Assert.IsTrue(redJoker.IsJoker, "Joker should be identified");
            Assert.AreNotEqual(redJoker, blackJoker, "Red and Black Jokers are different");
        }

        [Test]
        public void DeckModel_Initialization_GeneratesCorrectCounts()
        {
            var deck = new DeckModel();

            deck.Initialize(DeckType.Standard36, false);
            Assert.AreEqual(36, deck.RemainingCount, "Standard36 should have 36 cards");
            Assert.IsFalse(deck.Cards.Any(c => c.IsJoker), "Standard36 should have no jokers");

            deck.Initialize(DeckType.Standard52, false);
            Assert.AreEqual(52, deck.RemainingCount, "Standard52 should have 52 cards");
            Assert.IsFalse(deck.Cards.Any(c => c.IsJoker), "Standard52 should have no jokers");

            deck.Initialize(DeckType.Standard54, false);
            Assert.AreEqual(54, deck.RemainingCount, "Standard54 should have 54 cards");
            Assert.AreEqual(2, deck.Cards.Count(c => c.IsJoker), "Standard54 should have 2 jokers");
        }

        [Test]
        public void DeckModel_DrawAction_ModifiesStateCorrectly()
        {
            var deck = new DeckModel();
            deck.Initialize(DeckType.Standard36, false);

            int initialCount = deck.RemainingCount;

            CardData? card = deck.Draw();
            Assert.IsNotNull(card, "Drawn card should not be null when deck is full");
            Assert.AreEqual(initialCount - 1, deck.RemainingCount, "Deck count should decrease");

            List<CardData> cards = deck.Draw(5);
            Assert.AreEqual(5, cards.Count, "Should draw exactly 5 cards");
            Assert.AreEqual(initialCount - 6, deck.RemainingCount, "Deck count should reflect all draws");
        }

        [Test]
        public void HandModel_SortingAndFetching_ReturnsExpected()
        {
            var hand = new HandModel();
            hand.Add(new CardData(Suit.Diamonds, Rank.Ten));
            hand.Add(new CardData(Suit.Clubs, Rank.Seven));
            hand.Add(new CardData(Suit.Diamonds, Rank.Ace));
            hand.Add(new CardData(Suit.Hearts, Rank.Two));

            Assert.AreEqual(4, hand.Count, "Hand should have 4 cards");

            hand.SortByRank(true);
            Assert.AreEqual(Rank.Two, hand.GetAt(0).Rank, "Lowest rank should be at index 0");
            Assert.AreEqual(Rank.Ace, hand.GetAt(3).Rank, "Highest rank should be at index 3");

            CardData? highestCard = hand.GetHighestCard(Suit.Clubs);
            // Non-trump Ace is Rank 14, Trump is Suit.Clubs (Seven is Rank 7). 
            // In GetHighestCard logic, Trump is considered strictly higher.
            Assert.IsNotNull(highestCard);
            Assert.AreEqual(Suit.Clubs, highestCard.Value.Suit, "Trump should be considered highest");
            Assert.AreEqual(Rank.Seven, highestCard.Value.Rank);

            CardData? lowestTrump = hand.GetLowestCard(Suit.Clubs);
            Assert.IsNotNull(lowestTrump);
            // Lowest card overall taking trump into account. 
            // Since trump is always > non-trump, the lowest card should be non-trump Two of Hearts
            Assert.AreEqual(Suit.Hearts, lowestTrump.Value.Suit);
            Assert.AreEqual(Rank.Two, lowestTrump.Value.Rank);

            List<CardData> testSuitCards = hand.GetCardsBySuit(Suit.Diamonds);
            Assert.AreEqual(2, testSuitCards.Count, "Should find two Diamonds");
        }
    }
}
