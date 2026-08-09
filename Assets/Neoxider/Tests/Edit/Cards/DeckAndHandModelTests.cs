using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Cards;
using NUnit.Framework;

namespace Neo.Editor.Tests.Cards
{
    public sealed class DeckAndHandModelTests
    {
        [TestCase(DeckType.Standard36, 36, 9, 0)]
        [TestCase(DeckType.Standard52, 52, 13, 0)]
        [TestCase(DeckType.Standard54, 54, 13, 2)]
        public void Initialize_StandardDeck_HasCompleteDeterministicComposition(
            DeckType deckType,
            int expectedCount,
            int ranksPerSuit,
            int expectedJokers)
        {
            DeckModel deck = new DeckModel();

            deck.Initialize(deckType, false);

            Assert.That(deck.Cards, Has.Count.EqualTo(expectedCount));
            Assert.That(deck.Cards.Count(card => card.IsJoker), Is.EqualTo(expectedJokers));
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                List<CardData> suitedCards = deck.Cards.Where(card => !card.IsJoker && card.Suit == suit).ToList();
                Assert.That(suitedCards, Has.Count.EqualTo(ranksPerSuit));
                Assert.That(suitedCards.Select(card => card.Rank).Distinct().Count(), Is.EqualTo(ranksPerSuit));
            }
        }

        [Test]
        public void Shuffle_PreservesExactCardMultisetAndRaisesOneChangeNotification()
        {
            CardData duplicate = new CardData(Suit.Hearts, Rank.Ace);
            CardData[] input =
            {
                duplicate,
                new CardData(Suit.Clubs, Rank.Seven),
                CardData.CreateJoker(true),
                duplicate
            };
            DeckModel deck = new DeckModel();
            deck.Initialize(input, false);
            int changes = 0;
            deck.OnDeckChanged += () => changes++;

            deck.Shuffle();

            Assert.That(deck.RemainingCount, Is.EqualTo(input.Length));
            CollectionAssert.AreEquivalent(input, deck.Cards);
            Assert.That(deck.Cards.Count(card => card.Equals(duplicate)), Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Initialize_WithShuffle_RaisesExactlyOneChangeNotification()
        {
            DeckModel deck = new DeckModel();
            int changes = 0;
            deck.OnDeckChanged += () => changes++;

            deck.Initialize(DeckType.Standard36, true);

            Assert.That(deck.RemainingCount, Is.EqualTo(36));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Draw_EmptyDeck_ReturnsNullWithoutRaisingEmptyEventAgain()
        {
            DeckModel deck = new DeckModel();
            deck.Initialize(new[] { new CardData(Suit.Spades, Rank.Queen) }, false);
            int emptyEvents = 0;
            deck.OnDeckEmpty += () => emptyEvents++;

            CardData? onlyCard = deck.Draw();
            CardData? missingCard = deck.Draw();
            List<CardData> missingCards = deck.Draw(3);

            Assert.That(onlyCard.HasValue, Is.True);
            Assert.That(missingCard.HasValue, Is.False);
            Assert.That(missingCards, Is.Empty);
            Assert.That(deck.Peek().HasValue, Is.False);
            Assert.That(deck.PeekBottom().HasValue, Is.False);
            Assert.That(deck.IsEmpty, Is.True);
            Assert.That(emptyEvents, Is.EqualTo(1));
        }

        [Test]
        public void AddRange_OverflowIsAtomicAndDoesNotRaiseEvents()
        {
            HandModel hand = new HandModel { Capacity = 2 };
            CardData existing = new CardData(Suit.Hearts, Rank.Ace);
            hand.Add(existing);
            int addedEvents = 0;
            int changedEvents = 0;
            hand.OnCardAdded += card => addedEvents++;
            hand.OnHandChanged += () => changedEvents++;
            CardData[] overflow =
            {
                new CardData(Suit.Clubs, Rank.Seven),
                new CardData(Suit.Diamonds, Rank.Ten)
            };

            Assert.Throws<InvalidOperationException>(() => hand.AddRange(overflow));

            Assert.That(hand.Cards, Is.EqualTo(new[] { existing }));
            Assert.That(addedEvents, Is.Zero);
            Assert.That(changedEvents, Is.Zero);
        }

        [Test]
        public void AddRangeUntilFull_AddsOnlyCapacityAndRaisesOneChangeEvent()
        {
            HandModel hand = new HandModel { Capacity = 1 };
            int addedEvents = 0;
            int changedEvents = 0;
            hand.OnCardAdded += card => addedEvents++;
            hand.OnHandChanged += () => changedEvents++;
            CardData[] cards =
            {
                new CardData(Suit.Hearts, Rank.Five),
                new CardData(Suit.Spades, Rank.Ace)
            };

            int added = hand.AddRangeUntilFull(cards);

            Assert.That(added, Is.EqualTo(1));
            Assert.That(hand.Count, Is.EqualTo(1));
            Assert.That(hand.GetAt(0), Is.EqualTo(cards[0]));
            Assert.That(addedEvents, Is.EqualTo(1));
            Assert.That(changedEvents, Is.EqualTo(1));
        }

        [Test]
        public void AddRangeUntilFull_DoesNotAdvanceSourceAfterHandBecomesFull()
        {
            HandModel hand = new HandModel { Capacity = 1 };
            int sourceAdvances = 0;

            int added = hand.AddRangeUntilFull(CountedCards(() => sourceAdvances++));

            Assert.That(added, Is.EqualTo(1));
            Assert.That(sourceAdvances, Is.EqualTo(1));
        }

        [Test]
        public void EmptyBulkOperations_DoNotRaiseChangeNotifications()
        {
            HandModel hand = new HandModel();
            ICardContainer container = hand;
            int changes = 0;
            hand.OnHandChanged += () => changes++;

            hand.AddRange(Array.Empty<CardData>());
            hand.Clear();
            container.Clear();
            List<CardData> removed = container.RemoveAll();

            Assert.That(removed, Is.Empty);
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void CapacityAndIndexContracts_RejectInvalidMutationsWithoutChangingHand()
        {
            HandModel hand = new HandModel { Capacity = 1 };
            CardData card = new CardData(Suit.Spades, Rank.King);
            hand.Add(card);

            Assert.Throws<ArgumentOutOfRangeException>(() => hand.Capacity = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => hand.Insert(-1, card));
            Assert.Throws<ArgumentOutOfRangeException>(() => hand.Insert(2, card));
            Assert.Throws<InvalidOperationException>(() => hand.Insert(0, card));
            Assert.Throws<ArgumentOutOfRangeException>(() => hand.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => hand.RemoveAt(1));
            Assert.That(hand.Cards, Is.EqualTo(new[] { card }));
        }

        [Test]
        public void Remove_MissingCardDoesNotRaiseRemovalOrChangeEvents()
        {
            HandModel hand = new HandModel();
            hand.Add(new CardData(Suit.Hearts, Rank.Two));
            int removedEvents = 0;
            int changedEvents = 0;
            hand.OnCardRemoved += card => removedEvents++;
            hand.OnHandChanged += () => changedEvents++;

            bool removed = hand.Remove(new CardData(Suit.Clubs, Rank.Ace));

            Assert.That(removed, Is.False);
            Assert.That(removedEvents, Is.Zero);
            Assert.That(changedEvents, Is.Zero);
            Assert.That(hand.Count, Is.EqualTo(1));
        }

        private static IEnumerable<CardData> CountedCards(Action onAdvance)
        {
            onAdvance();
            yield return new CardData(Suit.Hearts, Rank.Five);
            onAdvance();
            yield return new CardData(Suit.Spades, Rank.Ace);
        }
    }
}
