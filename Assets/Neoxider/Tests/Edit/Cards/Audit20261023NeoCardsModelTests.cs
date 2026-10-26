using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Cards;
using Neo.Cards.Poker;
using NUnit.Framework;

namespace Neo.Editor.Tests.Cards
{
    public sealed class Audit20261023NeoCardsModelTests
    {
        [Test]
        public void Reset_ExplicitDeck_RestoresSnapshotOrderDuplicatesAndClearsDiscard()
        {
            CardData wolf = CardData.CreateCustom("wolf", "Wolf", 3, "Beast");
            CardData bear = CardData.CreateCustom("bear", "Bear", 8, "Beast");
            List<CardData> input = new() { wolf, bear, wolf };
            DeckModel deck = new();
            deck.Initialize(input, false);
            input.Clear();
            deck.Discard(deck.Draw().Value);
            int changes = 0;
            deck.OnDeckChanged += () => changes++;

            deck.Reset(false);

            CollectionAssert.AreEqual(new[] { wolf, bear, wolf }, deck.Cards);
            Assert.That(deck.DiscardPile, Is.Empty);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(deck.Draw(), Is.EqualTo(wolf));
            Assert.That(deck.Draw(), Is.EqualTo(bear));
            Assert.That(deck.Draw(), Is.EqualTo(wolf));
        }

        [TestCase(DeckType.Standard36, 36)]
        [TestCase(DeckType.Standard52, 52)]
        [TestCase(DeckType.Standard54, 54)]
        public void Reset_StandardInitializationReplacesPreviousExplicitSnapshot(DeckType type, int count)
        {
            DeckModel deck = new();
            deck.Initialize(new[] { CardData.CreateCustom("old") }, false);
            deck.Initialize(type, false);
            CardData[] expected = deck.Cards.ToArray();
            deck.Draw();

            deck.Reset(false);

            Assert.That(deck.Count, Is.EqualTo(count));
            Assert.That(deck.DeckType, Is.EqualTo(type));
            CollectionAssert.AreEqual(expected, deck.Cards);
        }

        [Test]
        public void Initialize_DeferredQueryOverOwnCards_PreservesSelectedCards()
        {
            DeckModel deck = new();
            CardData first = new(Suit.Clubs, Rank.Seven);
            CardData last = new(Suit.Spades, Rank.Ace);
            deck.Initialize(new[] { first, CardData.CreateJoker(true), last }, false);

            deck.Initialize(deck.Cards.Where(card => !card.IsJoker), false);

            CollectionAssert.AreEqual(new[] { first, last }, deck.Cards);
            deck.Draw();
            deck.Reset(false);
            CollectionAssert.AreEqual(new[] { first, last }, deck.Cards);
        }

        [Test]
        public void Initialize_FailingEnumeration_PreservesDeckDiscardAndResetSnapshot()
        {
            CardData first = new(Suit.Hearts, Rank.King);
            CardData last = new(Suit.Spades, Rank.Five);
            DeckModel deck = new();
            deck.Initialize(new[] { first, last }, false);
            deck.Discard(deck.Draw().Value);
            int changes = 0;
            deck.OnDeckChanged += () => changes++;

            Assert.Throws<InvalidOperationException>(() => deck.Initialize(FailingCards(), false));
            Assert.Throws<ArgumentNullException>(() => deck.Initialize((IEnumerable<CardData>)null, false));

            CollectionAssert.AreEqual(new[] { first }, deck.Cards);
            CollectionAssert.AreEqual(new[] { last }, deck.DiscardPile);
            Assert.That(changes, Is.Zero);
            deck.Reset(false);
            CollectionAssert.AreEqual(new[] { first, last }, deck.Cards);
        }

        [Test]
        public void Reset_ExplicitEmptyDeck_RemainsEmpty()
        {
            DeckModel deck = new();
            deck.Initialize(Array.Empty<CardData>(), false);
            deck.ReturnToTop(new CardData(Suit.Clubs, Rank.Ace));

            deck.Reset(false);

            Assert.That(deck.Count, Is.Zero);
            Assert.That(deck.Draw(), Is.Null);
        }

        [Test]
        public void AddRange_OwnHandCards_AddsOneSnapshotAndPublishesCompletedState()
        {
            CardData first = new(Suit.Hearts, Rank.Three);
            CardData second = new(Suit.Spades, Rank.Nine);
            HandModel hand = new() { Capacity = 4 };
            hand.AddRange(new[] { first, second });
            int additions = 0;
            int changes = 0;
            hand.OnCardAdded += card => additions++;
            hand.OnHandChanged += () => changes++;

            hand.AddRange(hand.Cards);

            CollectionAssert.AreEqual(new[] { first, second, first, second }, hand.Cards);
            Assert.That(additions, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(hand.IsFull, Is.True);
        }

        [Test]
        public void AddRange_ListenerClearsSourceList_StillAddsEntireSnapshot()
        {
            CardData first = CardData.CreateCustom("first");
            CardData second = CardData.CreateCustom("second");
            List<CardData> source = new() { first, second };
            HandModel hand = new();
            hand.OnCardAdded += card => source.Clear();

            hand.AddRange(source);

            CollectionAssert.AreEqual(new[] { first, second }, hand.Cards);
        }

        [Test]
        public void BoardModel_AddThroughContainer_EnforcesCapacityWithoutEventsOrMutationOnRejection()
        {
            ICardContainer board = new BoardModel(1);
            CardData first = CardData.CreateCustom("first");
            CardData second = CardData.CreateCustom("second");
            int changes = 0;
            board.OnChanged += () => changes++;
            board.Add(first);

            Assert.That(board.CanAdd(second), Is.False);
            Assert.Throws<InvalidOperationException>(() => board.Add(second));
            CollectionAssert.AreEqual(new[] { first }, board.Data);
            Assert.That(changes, Is.EqualTo(1));

            board.Remove(first);
            board.Add(second);
            CollectionAssert.AreEqual(new[] { second }, board.Data);
        }

        [Test]
        public void BoardModel_ZeroCapacityRejectsCardsAndNegativeCapacityIsInvalid()
        {
            BoardModel board = new(0);
            CardData card = CardData.CreateCustom("card");

            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardModel(-1));
            Assert.That(board.CanAdd(card), Is.False);
            Assert.Throws<InvalidOperationException>(() => board.Add(card));
            Assert.That(board.Count, Is.Zero);
        }

        [Test]
        public void Poker_CustomCards_AreRejectedInsteadOfBeingScoredAsAFlush()
        {
            CardData[] cards = Enumerable.Range(0, 5)
                .Select(index => CardData.CreateCustom("card_" + index, sortValue: index)).ToArray();

            ArgumentException exception = Assert.Throws<ArgumentException>(() => PokerHandEvaluator.Evaluate(cards));

            Assert.That(exception.ParamName, Is.EqualTo("cards"));
        }

        [TestCase(-1, 2)]
        [TestCase(4, 2)]
        [TestCase(0, 0)]
        [TestCase(0, 15)]
        public void Poker_InvalidStandardCard_IsRejected(int suit, int rank)
        {
            CardData[] cards =
            {
                new((Suit)suit, (Rank)rank),
                new(Suit.Clubs, Rank.Three), new(Suit.Hearts, Rank.Five),
                new(Suit.Diamonds, Rank.Seven), new(Suit.Spades, Rank.Nine)
            };

            ArgumentException exception = Assert.Throws<ArgumentException>(() => PokerHandEvaluator.Evaluate(cards));

            Assert.That(exception.ParamName, Is.EqualTo("cards"));
        }

        [Test]
        public void Poker_StandardRoyalFlushWithJoker_StillEvaluatesIgnoringJoker()
        {
            CardData[] cards =
            {
                new(Suit.Hearts, Rank.Ten), new(Suit.Hearts, Rank.Jack), new(Suit.Hearts, Rank.Queen),
                new(Suit.Hearts, Rank.King), new(Suit.Hearts, Rank.Ace), CardData.CreateJoker(false)
            };

            PokerHandResult result = PokerHandEvaluator.Evaluate(cards);

            Assert.That(result.Combination, Is.EqualTo(PokerCombination.RoyalFlush));
            Assert.That(result.CombinationRanks[0], Is.EqualTo(Rank.Ace));
            Assert.That(result.BestHand.Count, Is.EqualTo(5));
        }

        private static IEnumerable<CardData> FailingCards()
        {
            yield return CardData.CreateCustom("partial");
            throw new InvalidOperationException("Source failed before initialization completed.");
        }
    }
}
