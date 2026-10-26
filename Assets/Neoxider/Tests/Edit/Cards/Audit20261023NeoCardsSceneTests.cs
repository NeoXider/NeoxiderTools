using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Cards
{
    public sealed class Audit20261023NeoCardsSceneTests
    {
        private readonly List<Object> _owned = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                {
                    Object.DestroyImmediate(_owned[index]);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void DeckConfig_TrimmedId_ResolvesGeneratedCardSpriteAndKeepsCaseDistinct()
        {
            DeckConfig config = CreateCustomConfig(" wolf ", "Wolf");
            Sprite face = CreateSprite();
            SerializedObject serialized = new(config);
            serialized.FindProperty("_customCards").GetArrayElementAtIndex(0)
                .FindPropertyRelative("_sprite").objectReferenceValue = face;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            List<CardData> cards = config.GenerateDeck();

            Assert.That(cards[0].CustomId, Is.EqualTo("wolf"));
            Assert.That(cards[1].CustomId, Is.EqualTo("Wolf"));
            Assert.That(config.GetSprite(cards[0]), Is.SameAs(face));
            Assert.That(config.GetSprite(cards[1]), Is.Null);
            Assert.That(config.Validate(out List<string> errors), Is.True);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void DeckConfig_IdsEqualAfterTrimming_ReportDuplicateIdentity()
        {
            DeckConfig config = CreateCustomConfig("wolf", " wolf ");

            bool valid = config.Validate(out List<string> errors);

            Assert.That(valid, Is.False);
            Assert.That(errors.Any(error => error.Contains("duplicated")), Is.True);
            Assert.That(config.GenerateDeck()[0], Is.EqualTo(config.GenerateDeck()[1]));
        }

        [Test]
        public void DeckPresenter_InitializeAndReset_UseConfiguredCustomCards()
        {
            DeckConfig config = CreateCustomConfig("wolf", "bear");
            DeckModel model = new();
            DeckView view = CreateComponent<DeckView>("DeckView");
            view.Config = config;
            CardView prefab = CreateComponent<CardView>("CardPrefab");
            DeckPresenter presenter = new(model, view, config, prefab);
            try
            {
                presenter.Initialize(false);
                CollectionAssert.AreEqual(config.GenerateDeck(), model.Cards);
                model.Draw();

                presenter.Reset(false);

                CollectionAssert.AreEqual(config.GenerateDeck(), model.Cards);
                Assert.That(model.Cards.All(card => card.IsCustom), Is.True);
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void DeckComponent_Reset_PreservesConfiguredExplicitDeck()
        {
            DeckConfig config = CreateCustomConfig("wolf", "bear");
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            SetReference(deck, "_config", config);
            SetBool(deck, "_shuffleOnStart", false);
            deck.Initialize();
            deck.Model.Draw();

            deck.Reset();

            CollectionAssert.AreEqual(config.GenerateDeck(), deck.Model.Cards);
            Assert.That(deck.RemainingCount, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeckDeal_FullHand_DoesNotConsumeLogicalOrVisualCard(bool useVisualStack)
        {
            DeckConfig config = CreateCustomConfig("wolf", "bear");
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            SetReference(deck, "_config", config);
            SetReference(deck, "_cardPrefab", CreateComponent<CardComponent>("CardPrefab"));
            SetBool(deck, "_shuffleOnStart", false);
            BoardComponent board = null;
            if (useVisualStack)
            {
                board = CreateComponent<BoardComponent>("Stack");
                SetReference(deck, "_visualStackBoard", board);
            }

            deck.Initialize();
            if (useVisualStack)
            {
                Complete(deck.BuildVisualStackAsync());
                Assert.That(board.Count, Is.EqualTo(2));
            }

            HandComponent hand = CreateComponent<HandComponent>("Hand");
            SetInt(hand, "_maxCards", 1);
            CardComponent existing = CreateCard("Existing", CardData.CreateCustom("existing"));
            Complete(hand.AddCardAsync(existing, false));
            int dealtEvents = 0;
            deck.OnCardDealt = new UnityEvent<CardComponent, HandComponent>();
            deck.OnCardDealt.AddListener((card, target) => dealtEvents++);
            CardData[] before = deck.Model.Cards.ToArray();

            CardComponent result = deck.DealToHandAsync(hand, true, 0f).GetAwaiter().GetResult();

            Assert.That(result, Is.Null);
            CollectionAssert.AreEqual(before, deck.Model.Cards);
            CollectionAssert.AreEqual(new[] { existing }, hand.Cards);
            Assert.That(dealtEvents, Is.Zero);
            if (useVisualStack)
            {
                Assert.That(board.Count, Is.EqualTo(2));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DeckInitialize_HidesPriorTrumpWhenDeckIsEmptyOrDisplayDisabled(bool emptyDeck)
        {
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            CardComponent display = CreateComponent<CardComponent>("Trump");
            SetReference(deck, "_config", CreateCustomConfig("wolf", "bear"));
            SetReference(deck, "_trumpCardDisplay", display);
            SetBool(deck, "_shuffleOnStart", false);
            deck.Initialize();
            Assert.That(display.gameObject.activeSelf, Is.True);
            Assert.That(display.Data.CustomId, Is.EqualTo("wolf"));

            if (emptyDeck)
            {
                SetReference(deck, "_config", CreateCustomConfig());
            }
            else
            {
                SetBool(deck, "_showTrumpCard", false);
            }

            deck.Initialize();

            Assert.That(deck.RemainingCount, Is.EqualTo(emptyDeck ? 0 : 2));
            Assert.That(display.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void HandTryAdd_ReportsAdmissionAndRejectsFullOrDuplicateWithoutMutation()
        {
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            SetInt(hand, "_maxCards", 1);
            CardComponent first = CreateCard("First", CardData.CreateCustom("same"));
            CardComponent other = CreateCard("Other", first.Data);
            Transform originalParent = other.transform.parent;
            int additions = 0;
            hand.OnCardAdded.AddListener(card => additions++);

            Assert.That(hand.TryAddCardAsync(first, false).GetAwaiter().GetResult(), Is.True);
            Assert.That(hand.TryAddCardAsync(first, false).GetAwaiter().GetResult(), Is.False);
            Assert.That(hand.TryAddCardAsync(other, false).GetAwaiter().GetResult(), Is.False);

            CollectionAssert.AreEqual(new[] { first }, hand.Cards);
            CollectionAssert.AreEqual(new[] { first.Data }, hand.Model.Cards);
            Assert.That(other.transform.parent, Is.SameAs(originalParent));
            Assert.That(additions, Is.EqualTo(1));
        }

        [Test]
        public void DeckDeal_DrawObserverAlreadyAdmittedTheCard_DoesNotRollbackOrDuplicateIt()
        {
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            SetReference(deck, "_config", CreateCustomConfig("card"));
            SetReference(deck, "_cardPrefab", CreateComponent<CardComponent>("Prefab"));
            SetBool(deck, "_shuffleOnStart", false);
            deck.Initialize();
            deck.OnCardDrawn = new UnityEvent<CardComponent>();
            deck.OnCardDrawn.AddListener(card =>
            {
                Own(card.gameObject);
                hand.AddCard(card);
            });
            int successes = 0;
            deck.OnCardDealt = new UnityEvent<CardComponent, HandComponent>();
            deck.OnCardDealt.AddListener((card, target) => successes++);

            CardComponent result = deck.DealToHandAsync(hand, true, 0f).GetAwaiter().GetResult();

            Assert.That(result, Is.Not.Null);
            Assert.That(deck.RemainingCount, Is.Zero);
            CollectionAssert.AreEqual(new[] { result }, hand.Cards);
            Assert.That(hand.Model.Count, Is.EqualTo(1));
            Assert.That(successes, Is.EqualTo(1));
        }

        [TestCase(CardDisplayMode.AlwaysFaceUp)]
        [TestCase(CardDisplayMode.AlwaysFaceDown)]
        public void CardPresenter_FixedFaceView_ReportsViewStateAfterSetAndFlip(CardDisplayMode mode)
        {
            DeckConfig config = CreateCustomConfig("card");
            CardViewUniversal view = CreateComponent<CardViewUniversal>("Card");
            SetInt(view, "_displayMode", (int)mode);
            CardPresenter presenter = new(view, config);
            try
            {
                bool expected = mode == CardDisplayMode.AlwaysFaceUp;
                presenter.SetData(CardData.CreateCustom("card"), !expected);
                Assert.That(presenter.IsFaceUp, Is.EqualTo(expected));

                presenter.Flip();
                Assert.That(presenter.IsFaceUp, Is.EqualTo(expected));
                Complete(presenter.FlipAsync(0f));
                Assert.That(presenter.IsFaceUp, Is.EqualTo(expected));
                Assert.That(presenter.IsFaceUp, Is.EqualTo(view.IsFaceUp));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void CardPresenter_ViewFlippedDirectly_ReportsCurrentViewState()
        {
            CardView view = CreateComponent<CardView>("Card");
            CardPresenter presenter = new(view, CreateCustomConfig("card"));
            try
            {
                presenter.SetData(CardData.CreateCustom("card"), true);

                view.Flip();

                Assert.That(presenter.IsFaceUp, Is.False);
                Assert.That(presenter.IsFaceUp, Is.EqualTo(view.IsFaceUp));
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void HandComponent_DuplicateInstanceIsIgnoredButEqualDataInstancesRemainIndependent()
        {
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            CardData data = CardData.CreateCustom("duplicate");
            CardComponent first = CreateCard("First", data);
            CardComponent second = CreateCard("Second", data);
            int additions = 0;
            hand.OnCardAdded.AddListener(card => additions++);

            Complete(hand.AddCardAsync(first, false));
            Complete(hand.AddCardAsync(first, false));
            Complete(hand.AddCardAsync(second, false));

            CollectionAssert.AreEqual(new[] { first, second }, hand.Cards);
            Assert.That(hand.Model.Count, Is.EqualTo(2));
            Assert.That(additions, Is.EqualTo(2));
            Complete(hand.RemoveCardAsync(first, false));
            CollectionAssert.AreEqual(new[] { second }, hand.Cards);
            CollectionAssert.AreEqual(new[] { data }, hand.Model.Cards);
        }

        [Test]
        public void BoardComponent_DuplicateInstanceDoesNotOccupyAnotherSlotOrRaisePlacement()
        {
            BoardComponent board = CreateComponent<BoardComponent>("Board");
            board.SetCapacity(2, false);
            CardData data = CardData.CreateCustom("duplicate");
            CardComponent first = CreateCard("First", data);
            CardComponent second = CreateCard("Second", data);
            int placements = 0;
            board.OnCardPlaced = new UnityEvent<CardComponent>();
            board.OnCardPlaced.AddListener(card => placements++);

            Complete(board.PlaceCardAsync(first, false));
            Assert.That(board.CanPlaceCard(first), Is.False);
            Complete(board.PlaceCardAsync(first, false));
            Complete(board.PlaceCardAsync(second, false));

            CollectionAssert.AreEqual(new[] { first, second }, board.Cards);
            Assert.That(placements, Is.EqualTo(2));
            Assert.That(board.RemoveCard(first), Is.True);
            CollectionAssert.AreEqual(new[] { second }, board.Cards);
        }

        [Test]
        public void HandView_DuplicateInstanceIsIgnoredAndSeparateEqualCardCanBeRemoved()
        {
            HandView view = CreateComponent<HandView>("Hand");
            CardView first = CreateComponent<CardView>("First");
            CardView second = CreateComponent<CardView>("Second");
            CardData data = CardData.CreateCustom("duplicate");
            first.SetData(data);
            second.SetData(data);

            Complete(view.AddCardAsync(first, false));
            Complete(view.AddCardAsync(first, false));
            Complete(view.AddCardAsync(second, false));

            CollectionAssert.AreEqual(new ICardView[] { first, second }, view.CardViews);
            Complete(view.RemoveCardAsync(first, false));
            CollectionAssert.AreEqual(new ICardView[] { second }, view.CardViews);
        }

        [Test]
        public void HandPresenter_DuplicatePresenterDoesNotDuplicateModelViewOrEvents()
        {
            HandModel model = new();
            HandView view = CreateComponent<HandView>("Hand");
            HandPresenter hand = new(model, view);
            CardView cardView = CreateComponent<CardView>("Card");
            CardPresenter card = new(cardView, CreateCustomConfig("card"));
            card.SetData(CardData.CreateCustom("card"));
            int additions = 0;
            hand.OnCardAdded += added => additions++;
            try
            {
                Complete(hand.AddCardAsync(card, false));
                Complete(hand.AddCardAsync(card, false));

                Assert.That(model.Count, Is.EqualTo(1));
                CollectionAssert.AreEqual(new[] { card }, hand.CardPresenters);
                CollectionAssert.AreEqual(new ICardView[] { cardView }, view.CardViews);
                Assert.That(additions, Is.EqualTo(1));
            }
            finally
            {
                hand.Dispose();
                card.Dispose();
            }
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void HandPresenter_Sort_SynchronizesModelViewPositionsAndSubsequentRemoval(bool byRank, bool ascending)
        {
            HandModel model = new();
            HandView view = CreateComponent<HandView>("Hand");
            SetInt(view, "_layoutType", (int)CardLayoutType.Line);
            HandPresenter hand = new(model, view);
            DeckConfig config = CreateCustomConfig("unused");
            CardData[] input =
            {
                new(Suit.Clubs, Rank.King), new(Suit.Hearts, Rank.Ace), new(Suit.Spades, Rank.Two)
            };
            List<CardPresenter> cards = new();
            try
            {
                foreach (CardData data in input)
                {
                    CardView cardView = CreateComponent<CardView>(data.ToString());
                    CardPresenter card = new(cardView, config);
                    card.SetData(data);
                    cards.Add(card);
                    Complete(hand.AddCardAsync(card, false));
                }

                CardPresenter[] expected = byRank
                    ? new[] { cards[2], cards[0], cards[1] }
                    : new[] { cards[1], cards[0], cards[2] };
                if (!ascending)
                {
                    Array.Reverse(expected);
                }

                Complete(byRank ? hand.SortByRankAsync(ascending, false) : hand.SortBySuitAsync(ascending, false));

                CollectionAssert.AreEqual(expected, hand.CardPresenters);
                CollectionAssert.AreEqual(expected.Select(card => card.Data).ToArray(), model.Cards);
                CollectionAssert.AreEqual(expected.Select(card => card.View).ToArray(), view.CardViews);
                for (int index = 0; index < expected.Length; index++)
                {
                    Assert.That(expected[index].Transform.localPosition.x, Is.EqualTo((index - 1) * 60f).Within(0.001f));
                }

                CardPresenter removed = hand.RemoveAtAsync(1, false).GetAwaiter().GetResult();
                Assert.That(removed, Is.SameAs(expected[1]));
                CollectionAssert.AreEqual(new[] { expected[0].Data, expected[2].Data }, model.Cards);
                CollectionAssert.AreEqual(new[] { expected[0].View, expected[2].View }, view.CardViews);
            }
            finally
            {
                hand.Dispose();
                foreach (CardPresenter card in cards)
                {
                    card.Dispose();
                }
            }
        }

        [TestCase(0, true)]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(2, false)]
        public void CardRenderers_MissingBackAndClearedOverride_ClearPreviouslyVisibleFace(int kind, bool useImage)
        {
            GameObject root = Own(new GameObject("Card", typeof(RectTransform)));
            Image image = useImage ? root.AddComponent<Image>() : null;
            SpriteRenderer renderer = useImage ? null : root.AddComponent<SpriteRenderer>();
            Sprite face = CreateSprite();
            MonoBehaviour component;
            Action flip;
            Action clear;
            if (kind == 0)
            {
                CardComponent card = root.AddComponent<CardComponent>();
                card.SetData(CardData.CreateCustom("card"));
                component = card;
                flip = card.Flip;
                clear = () => card.ClearSpriteOverrides();
                SetReference(component, useImage ? "_cardImage" : "_spriteRenderer", useImage ? (Object)image : renderer);
                card.SetSpriteOverrides(face);
            }
            else if (kind == 1)
            {
                CardView card = root.AddComponent<CardView>();
                component = card;
                flip = card.Flip;
                clear = () => card.ClearSpriteOverrides();
                SetReference(component, useImage ? "_cardImage" : "_spriteRenderer", useImage ? (Object)image : renderer);
                card.SetSpriteOverrides(face);
            }
            else
            {
                CardViewUniversal card = root.AddComponent<CardViewUniversal>();
                component = card;
                flip = card.Flip;
                clear = () => card.ClearSpriteOverrides();
                SetReference(component, useImage ? "_cardImage" : "_spriteRenderer", useImage ? (Object)image : renderer);
                card.SetSpriteOverrides(face);
            }

            Func<Sprite> visible = () => useImage ? image.sprite : renderer.sprite;
            Assert.That(visible(), Is.SameAs(face));

            flip();
            Assert.That(visible(), Is.Null, "A missing back must not retain the exposed face.");
            flip();
            Assert.That(visible(), Is.SameAs(face));
            clear();
            Assert.That(visible(), Is.Null, "Clearing overrides must remove stale artwork when no config supplies it.");
        }

        private DeckConfig CreateCustomConfig(params string[] ids)
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());
            SerializedObject serialized = new(config);
            serialized.FindProperty("_useCustomDeck").boolValue = true;
            SerializedProperty cards = serialized.FindProperty("_customCards");
            cards.arraySize = ids.Length;
            for (int index = 0; index < ids.Length; index++)
            {
                cards.GetArrayElementAtIndex(index).FindPropertyRelative("_id").stringValue = ids[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private CardComponent CreateCard(string name, CardData data)
        {
            CardComponent card = CreateComponent<CardComponent>(name);
            card.SetData(data);
            return card;
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            return Own(new GameObject(name)).AddComponent<T>();
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = Own(new Texture2D(2, 2));
            return Own(Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero));
        }

        private T Own<T>(T value) where T : Object
        {
            _owned.Add(value);
            return value;
        }

        private static void Complete(UniTask task)
        {
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "This scenario uses immediate operations in Edit Mode.");
            awaiter.GetResult();
        }

        private static void SetReference(Object target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
