using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo.Cards;
using Neo.Cards.Poker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

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
        public void CardData_CustomCards_SupportGenericGames()
        {
            var wolf = CardData.CreateCustom("beast_wolf", "Wolf", 2, "Beast");
            var bear = CardData.CreateCustom("beast_bear", "Bear", 5, "Beast");
            var fireball = CardData.CreateCustom("spell_fireball", "Fireball", 5, "Spell");

            Assert.IsTrue(wolf.IsCustom);
            Assert.AreEqual("beast_wolf", wolf.CustomId);
            Assert.AreEqual("Wolf", wolf.ToString());
            Assert.IsTrue(bear.Beats(wolf, null), "Higher SortValue in same custom group should beat lower card.");
            Assert.IsFalse(fireball.Beats(wolf, null),
                "Different custom groups should not beat each other by default.");
            Assert.IsTrue(bear.HasSameRank(fireball), "SortValue is the generic rank equivalent for custom games.");
            Assert.IsFalse(bear.HasSameSuit(fireball), "Group is the generic suit/faction equivalent.");
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
        public void DeckModel_ExplicitCustomDeck_PreservesCardsAndEmptyDrawsSafely()
        {
            var cards = new List<CardData>
            {
                CardData.CreateCustom("hero_mage", "Mage", 10, "Hero"),
                CardData.CreateCustom("spell_arcane", "Arcane Bolt", 3, "Spell")
            };
            var deck = new DeckModel();

            deck.Initialize(cards, false);

            Assert.AreEqual(2, deck.RemainingCount);
            Assert.AreEqual(cards[1], deck.Draw());
            Assert.AreEqual(cards[0], deck.Draw());
            Assert.IsNull(deck.Draw());
            Assert.AreEqual(0, deck.Draw(3).Count);
        }

        [Test]
        public void CardViewApis_AllowStandaloneCustomCardsWithoutDeckConfig()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            var cardObject = new GameObject("StandaloneCard");
            var viewObject = new GameObject("StandaloneView");
            var universalObject = new GameObject("StandaloneUniversalView");

            try
            {
                CardComponent card = cardObject.AddComponent<CardComponent>();
                CardView view = viewObject.AddComponent<CardView>();
                CardViewUniversal universal = universalObject.AddComponent<CardViewUniversal>();
                var custom = CardData.CreateCustom("minion_custom", "Custom Minion", 4, "Neutral");

                Assert.DoesNotThrow(() => card.SetSpriteOverrides(sprite));
                Assert.DoesNotThrow(() => card.SetData(custom));
                Assert.DoesNotThrow(() => card.Flip());

                Assert.DoesNotThrow(() => view.SetSpriteOverrides(sprite));
                Assert.DoesNotThrow(() => view.SetData(custom));
                Assert.DoesNotThrow(() => view.Flip());

                Assert.DoesNotThrow(() => universal.SetSpriteOverrides(sprite));
                Assert.DoesNotThrow(() => universal.SetData(custom));
                Assert.DoesNotThrow(() => universal.Flip());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cardObject);
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(universalObject);
            }
        }

        [Test]
        public void BoardComponent_CustomCapacityAndFaceUp_ArePubliclyConfigurable()
        {
            var boardObject = new GameObject("Board");
            var cardObject = new GameObject("Card");

            try
            {
                BoardComponent board = boardObject.AddComponent<BoardComponent>();
                CardComponent card = cardObject.AddComponent<CardComponent>();

                board.SetCapacity(1, false);
                board.SetFaceUp(false);

                Assert.AreEqual(1, board.MaxCards);
                Assert.IsFalse(board.FaceUp);
                Assert.IsTrue(board.CanPlaceCard(card));
                Assert.IsFalse(board.CanPlaceCard(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
                UnityEngine.Object.DestroyImmediate(boardObject);
            }
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

        [Test]
        public void HandModel_RemoveAt_WithDuplicateData_RemovesIndexedCardAndKeepsOrder()
        {
            var duplicate = new CardData(Suit.Hearts, Rank.Ace);
            var middle = new CardData(Suit.Clubs, Rank.Seven);
            var hand = new HandModel();
            hand.Add(duplicate);
            hand.Add(middle);
            hand.Add(duplicate);

            CardData removed = hand.RemoveAt(2);

            Assert.AreEqual(duplicate, removed);
            CollectionAssert.AreEqual(new[] { duplicate, middle }, hand.Cards.ToList());
        }

        [Test]
        public void HandPresenter_RemoveAt_WithDuplicateData_RemovesIndexedPresenterAndKeepsOrder()
        {
            var duplicate = new CardData(Suit.Hearts, Rank.Ace);
            var middle = new CardData(Suit.Clubs, Rank.Seven);
            DeckConfig config = ScriptableObject.CreateInstance<DeckConfig>();
            var handViewObject = new GameObject("HandView");
            var firstObject = new GameObject("FirstPresenterView");
            var secondObject = new GameObject("MiddlePresenterView");
            var thirdObject = new GameObject("IndexedPresenterView");

            try
            {
                HandView handView = handViewObject.AddComponent<HandView>();
                var presenter = new HandPresenter(new HandModel(), handView);
                CardPresenter first = CreatePresenter(duplicate, config, firstObject);
                CardPresenter second = CreatePresenter(middle, config, secondObject);
                CardPresenter third = CreatePresenter(duplicate, config, thirdObject);

                InvokeUniTaskMethod(presenter, nameof(HandPresenter.AddCardAsync), first, false);
                InvokeUniTaskMethod(presenter, nameof(HandPresenter.AddCardAsync), second, false);
                InvokeUniTaskMethod(presenter, nameof(HandPresenter.AddCardAsync), third, false);

                InvokeUniTaskMethod(presenter, nameof(HandPresenter.RemoveAtAsync), 2, false);

                Assert.AreSame(first, presenter.CardPresenters[0]);
                Assert.AreSame(second, presenter.CardPresenters[1]);
                CollectionAssert.AreEqual(new[] { duplicate, middle }, presenter.Model.Cards.ToList());
                CollectionAssert.AreEqual(new[] { first.View, second.View }, handView.CardViews.ToList());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(thirdObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(handViewObject);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void HandComponent_RemoveAt_WithDuplicateData_RemovesIndexedComponentAndKeepsOrder()
        {
            var duplicate = new CardData(Suit.Hearts, Rank.Ace);
            var middle = new CardData(Suit.Clubs, Rank.Seven);
            var handObject = new GameObject("Hand");
            var firstObject = new GameObject("FirstDuplicate");
            var secondObject = new GameObject("Middle");
            var thirdObject = new GameObject("IndexedDuplicate");

            try
            {
                HandComponent hand = handObject.AddComponent<HandComponent>();
                SetPrivateField(hand, "_maxCards", 36);
                CardComponent first = firstObject.AddComponent<CardComponent>();
                CardComponent second = secondObject.AddComponent<CardComponent>();
                CardComponent third = thirdObject.AddComponent<CardComponent>();
                InitializeCardEvents(first);
                InitializeCardEvents(second);
                InitializeCardEvents(third);
                first.SetData(duplicate);
                second.SetData(middle);
                third.SetData(duplicate);

                InvokeUniTaskMethod(hand, nameof(HandComponent.AddCardAsync), first, false);
                InvokeUniTaskMethod(hand, nameof(HandComponent.AddCardAsync), second, false);
                InvokeUniTaskMethod(hand, nameof(HandComponent.AddCardAsync), third, false);

                Assert.AreEqual(3, hand.Count, "Test setup should add all cards before removing by index.");

                InvokeUniTaskMethod(hand, nameof(HandComponent.RemoveAtAsync), 2, false);

                Assert.AreSame(first, hand.Cards[0]);
                Assert.AreSame(second, hand.Cards[1]);
                CollectionAssert.AreEqual(new[] { duplicate, middle }, hand.Model.Cards.ToList());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(thirdObject);
                UnityEngine.Object.DestroyImmediate(secondObject);
                UnityEngine.Object.DestroyImmediate(firstObject);
                UnityEngine.Object.DestroyImmediate(handObject);
            }
        }

        [Test]
        public void HandComponent_RemoveCard_PreservesExternalCardClickListeners()
        {
            var handObject = new GameObject("Hand");
            var cardObject = new GameObject("CardWithExternalListener");

            try
            {
                HandComponent hand = handObject.AddComponent<HandComponent>();
                CardComponent card = cardObject.AddComponent<CardComponent>();
                InitializeCardEvents(card);
                card.SetData(new CardData(Suit.Spades, Rank.Queen));

                int externalClicks = 0;
                int handClicks = 0;
                card.OnClick.AddListener(() => externalClicks++);
                hand.OnCardClicked.AddListener(_ => handClicks++);

                InvokeUniTaskMethod(hand, nameof(HandComponent.AddCardAsync), card, false);
                card.OnClick.Invoke();

                Assert.AreEqual(1, externalClicks);
                Assert.AreEqual(1, handClicks);

                InvokeUniTaskMethod(hand, nameof(HandComponent.RemoveCardAsync), card, false);
                card.OnClick.Invoke();

                Assert.AreEqual(2, externalClicks, "Removing from a hand must not clear user-owned listeners.");
                Assert.AreEqual(1, handClicks, "The hand should remove only its own click listener.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cardObject);
                UnityEngine.Object.DestroyImmediate(handObject);
            }
        }

        [Test]
        public void DeckComponent_InitializeTwice_UnsubscribesOldModelEmptyEvent()
        {
            var deckObject = new GameObject("Deck");
            DeckConfig config = ScriptableObject.CreateInstance<DeckConfig>();

            try
            {
                DeckComponent deck = deckObject.AddComponent<DeckComponent>();
                SetPrivateField(deck, "_config", config);
                SetPrivateField(deck, "_shuffleOnStart", false);

                int emptyEvents = 0;
                deck.OnDeckEmpty = new UnityEvent();
                deck.OnDeckEmpty.AddListener(() => emptyEvents++);

                deck.Initialize();
                DeckModel oldModel = deck.Model;
                Assert.That(oldModel, Is.Not.Null);

                deck.Initialize();
                Assert.That(deck.Model, Is.Not.SameAs(oldModel));

                oldModel.Draw(oldModel.RemainingCount);

                Assert.That(emptyEvents, Is.EqualTo(0),
                    "Replacing DeckComponent.Model must detach events from the old model.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(deckObject);
            }
        }

        [Test]
        public void CardViews_HoverTweens_AreOwnedAndClearedOnExit()
        {
            var viewObject = new GameObject("CardView");
            var universalObject = new GameObject("CardViewUniversal");

            try
            {
                CardView view = viewObject.AddComponent<CardView>();
                CardViewUniversal universal = universalObject.AddComponent<CardViewUniversal>();
                var pointer = new PointerEventData(null);

                ((IPointerEnterHandler)view).OnPointerEnter(pointer);
                Assert.That(GetPrivateField<object>(view, "_hoverScaleTween"), Is.Not.Null);
                Assert.That(GetPrivateField<object>(view, "_hoverMoveTween"), Is.Not.Null);

                ((IPointerExitHandler)view).OnPointerExit(pointer);
                Assert.That(GetPrivateField<object>(view, "_hoverScaleTween"), Is.Not.Null);
                Assert.That(GetPrivateField<object>(view, "_hoverMoveTween"), Is.Not.Null);

                ((IPointerEnterHandler)universal).OnPointerEnter(pointer);
                Assert.That(GetPrivateField<object>(universal, "_hoverScaleTween"), Is.Not.Null);
                Assert.That(GetPrivateField<object>(universal, "_hoverMoveTween"), Is.Not.Null);

                ((IPointerExitHandler)universal).OnPointerExit(pointer);
                Assert.That(GetPrivateField<object>(universal, "_hoverScaleTween"), Is.Not.Null);
                Assert.That(GetPrivateField<object>(universal, "_hoverMoveTween"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(universalObject);
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        [Test]
        public void PokerEvaluator_RejectsHandsWithFewerThanFiveNonJokers()
        {
            CardData[] cards = new[]
            {
                new CardData(Suit.Hearts, Rank.Ace),
                new CardData(Suit.Clubs, Rank.King),
                new CardData(Suit.Diamonds, Rank.Queen),
                new CardData(Suit.Spades, Rank.Jack),
                CardData.CreateJoker(true)
            };

            Assert.Throws<ArgumentException>(() => PokerHandEvaluator.Evaluate(cards));
        }

        [Test]
        public void PokerEvaluator_WheelStraight_UsesFiveAsHighCard()
        {
            CardData[] cards = new[]
            {
                new CardData(Suit.Hearts, Rank.Ace),
                new CardData(Suit.Clubs, Rank.Two),
                new CardData(Suit.Diamonds, Rank.Three),
                new CardData(Suit.Spades, Rank.Four),
                new CardData(Suit.Hearts, Rank.Five)
            };

            PokerHandResult result = PokerHandEvaluator.Evaluate(cards);

            Assert.AreEqual(PokerCombination.Straight, result.Combination);
            Assert.AreEqual(Rank.Five, result.CombinationRanks[0]);
        }

        [Test]
        public void PokerRules_GetWinners_IgnoresNullHandsAndSupportsAllNull()
        {
            PokerHandResult pair = PokerHandEvaluator.Evaluate(new[]
            {
                new CardData(Suit.Hearts, Rank.Ace),
                new CardData(Suit.Clubs, Rank.Ace),
                new CardData(Suit.Diamonds, Rank.Queen),
                new CardData(Suit.Spades, Rank.Jack),
                new CardData(Suit.Hearts, Rank.Nine)
            });

            CollectionAssert.AreEqual(new[] { 1 }, PokerRules.GetWinners(new List<PokerHandResult> { null, pair }));
            Assert.AreEqual(0, PokerRules.GetWinners(new List<PokerHandResult> { null, null }).Count);
        }

        [Test]
        public void PokerRules_TexasHoldem_ReturnsSplitPotWinners()
        {
            CardData[] board = new[]
            {
                new CardData(Suit.Hearts, Rank.Ace),
                new CardData(Suit.Clubs, Rank.King),
                new CardData(Suit.Diamonds, Rank.Queen),
                new CardData(Suit.Spades, Rank.Jack),
                new CardData(Suit.Hearts, Rank.Ten)
            };
            var players = new List<IEnumerable<CardData>>
            {
                new[] { new CardData(Suit.Clubs, Rank.Two), new CardData(Suit.Diamonds, Rank.Three) },
                new[] { new CardData(Suit.Spades, Rank.Four), new CardData(Suit.Clubs, Rank.Five) }
            };

            CollectionAssert.AreEqual(new[] { 0, 1 }, PokerRules.GetWinnersTexasHoldem(board, players));
        }

        private static CardPresenter CreatePresenter(CardData data, DeckConfig config, GameObject viewObject)
        {
            CardView view = viewObject.AddComponent<CardView>();
            var presenter = new CardPresenter(view, config);
            presenter.SetData(data);
            return presenter;
        }

        private static void InvokeUniTaskMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName);
            Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            Assert.DoesNotThrow(() =>
            {
                object task = method.Invoke(target, arguments);
                object awaiter = task.GetType().GetMethod("GetAwaiter").Invoke(task, null);
                awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, null);
            });
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} should exist.");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} should exist.");
            field.SetValue(target, value);
        }

        private static void InitializeCardEvents(CardComponent card)
        {
            card.OnClick = new UnityEvent();
            card.OnFlip = new UnityEvent();
            card.OnMoveComplete = new UnityEvent();
            card.OnHoverEnter = new UnityEvent();
            card.OnHoverExit = new UnityEvent();
        }
    }
}
