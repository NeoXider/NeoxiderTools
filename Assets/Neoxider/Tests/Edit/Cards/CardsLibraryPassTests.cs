using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using Neo.Cards.Poker;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Neo.Editor.Tests.Cards
{
    /// <summary>
    ///     Regression and performance guards for the 2026-11 Cards quality pass: deterministic shuffling, stable
    ///     sorting, allocation-free hot paths, deterministic layout and the allocation-free poker evaluator.
    /// </summary>
    public sealed class CardsLibraryPassTests
    {
        private const int Iterations = 200;
        private readonly List<Object> _owned = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void DeckModel_SameSeed_ProducesIdenticalShuffleSequence()
        {
            DeckModel first = new DeckModel(42);
            DeckModel second = new DeckModel(new Random(42));
            DeckModel other = new DeckModel(43);

            first.Initialize(DeckType.Standard52);
            second.Initialize(DeckType.Standard52);
            other.Initialize(DeckType.Standard52);

            CollectionAssert.AreEqual(first.Cards, second.Cards);
            CollectionAssert.AreNotEqual(first.Cards, other.Cards);

            first.Shuffle();
            second.Shuffle();
            CollectionAssert.AreEqual(first.Cards, second.Cards, "The injected source drives every later shuffle.");
        }

        [Test]
        public void DeckModel_OneOffRandom_DoesNotAdvanceTheInjectedSource()
        {
            DeckModel first = new DeckModel(7);
            DeckModel second = new DeckModel(7);
            first.Initialize(DeckType.Standard36, false);
            second.Initialize(DeckType.Standard36, false);

            first.Shuffle(new Random(1000));
            first.Initialize(DeckType.Standard36, false);
            first.Shuffle();
            second.Shuffle();

            CollectionAssert.AreEqual(second.Cards, first.Cards);
        }

        [Test]
        public void DeckModel_NullRandomSources_AreRejected()
        {
            DeckModel deck = new DeckModel();

            Assert.Throws<ArgumentNullException>(() => deck.ShuffleRandom = null);
            Assert.Throws<ArgumentNullException>(() => deck.Shuffle(null));
            Assert.That(deck.ShuffleRandom, Is.Not.Null);
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void DeckModel_DrawNonPositiveCount_ReturnsEmptyWithoutThrowing(int count)
        {
            DeckModel deck = new DeckModel(1);
            deck.Initialize(DeckType.Standard36);

            List<CardData> drawn = deck.Draw(count);

            Assert.That(drawn, Is.Empty);
            Assert.That(deck.RemainingCount, Is.EqualTo(36));
        }

        [Test]
        public void DeckModel_Shuffle_AllocatesNothingWhenWarm()
        {
            DeckModel deck = new DeckModel(3);
            deck.Initialize(DeckType.Standard54);
            deck.Shuffle();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                deck.Shuffle();
            }

            Assert.That(allocationCounter.End(), Is.EqualTo(0L));
        }

        [Test]
        public void CardData_StandardConstruction_AllocatesNothingAndKeepsSuitGroup()
        {
            Assert.That(new CardData(Suit.Spades, Rank.Ace).Group, Is.EqualTo("Spades"));
            Assert.That(new CardData(Suit.Hearts, Rank.Two).Group, Is.EqualTo("Hearts"));
            int checksum = 0;
            checksum += new CardData(Suit.Clubs, Rank.Ten).SortValue;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                CardData card = new CardData((Suit)(i % 4), (Rank)(2 + i % 13));
                checksum += card.SortValue + card.CompareTo(new CardData(Suit.Hearts, Rank.Seven));
            }

            Assert.That(allocationCounter.End(), Is.EqualTo(0L));
            Assert.That(checksum, Is.Not.EqualTo(int.MinValue));
        }

        [Test]
        public void HandModel_SortByRank_IsStableForEqualRanks()
        {
            CardData sevenHearts = new CardData(Suit.Hearts, Rank.Seven);
            CardData two = new CardData(Suit.Spades, Rank.Two);
            CardData sevenClubs = new CardData(Suit.Clubs, Rank.Seven);
            CardData sevenDiamonds = new CardData(Suit.Diamonds, Rank.Seven);
            HandModel hand = new HandModel();
            hand.AddRange(new[] { sevenHearts, two, sevenClubs, sevenDiamonds });
            int changes = 0;
            hand.OnHandChanged += () => changes++;

            hand.SortByRank();

            CollectionAssert.AreEqual(new[] { two, sevenHearts, sevenClubs, sevenDiamonds }, hand.Cards);
            hand.SortByRank(false);
            CollectionAssert.AreEqual(new[] { sevenHearts, sevenClubs, sevenDiamonds, two }, hand.Cards);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void HandModel_SortWithComparer_OrdersCustomCardsAndRejectsNull()
        {
            CardData wolf = CardData.CreateCustom("wolf", "Wolf", 2, "Beast");
            CardData bear = CardData.CreateCustom("bear", "Bear", 5, "Beast");
            CardData fireball = CardData.CreateCustom("fireball", "Fireball", 3, "Spell");
            CardData imp = CardData.CreateCustom("imp", "Imp", 1, "Spell");
            HandModel hand = new HandModel();
            hand.AddRange(new[] { wolf, bear, fireball, imp });

            hand.Sort(CardComparer.ByValueDescending);
            CollectionAssert.AreEqual(new[] { bear, fireball, wolf, imp }, hand.Cards);

            hand.Sort(CardComparer.ByGroupThenValueAscending);
            CollectionAssert.AreEqual(new[] { wolf, bear, imp, fireball }, hand.Cards);

            Assert.Throws<ArgumentNullException>(() => hand.Sort((IComparer<CardData>)null));
            Assert.Throws<ArgumentNullException>(() => hand.Sort((Comparison<CardData>)null));
        }

        [Test]
        public void HandModel_StableSort_HandlesLargeCollections()
        {
            HandModel hand = new HandModel();
            List<CardData> input = new();
            for (int i = 0; i < 120; i++)
            {
                input.Add(CardData.CreateCustom("c" + i, null, i % 5));
            }

            hand.AddRange(input);
            // WHY: comparing SortValue only leaves many ties, so this exercises the stable merge path (> 32 items).
            hand.Sort((a, b) => a.SortValue.CompareTo(b.SortValue));

            List<CardData> expected = input.OrderBy(card => card.SortValue).ToList();
            CollectionAssert.AreEqual(expected, hand.Cards);
        }

        [Test]
        public void HandModel_LowestAndHighest_UseTrumpPreferFirstAndIgnoreJokers()
        {
            CardData sixHearts = new CardData(Suit.Hearts, Rank.Six);
            CardData sixClubs = new CardData(Suit.Clubs, Rank.Six);
            CardData aceSpades = new CardData(Suit.Spades, Rank.Ace);
            CardData sevenDiamonds = new CardData(Suit.Diamonds, Rank.Seven);
            HandModel hand = new HandModel();
            hand.AddRange(new[] { CardData.CreateJoker(true), sixHearts, sixClubs, aceSpades, sevenDiamonds });

            Assert.That(hand.GetLowestCard(), Is.EqualTo(sixHearts));
            Assert.That(hand.GetLowestCard(Suit.Hearts), Is.EqualTo(sixClubs));
            Assert.That(hand.GetHighestCard(), Is.EqualTo(aceSpades));
            Assert.That(hand.GetHighestCard(Suit.Diamonds), Is.EqualTo(sevenDiamonds));

            HandModel jokers = new HandModel();
            jokers.Add(CardData.CreateJoker(false));
            Assert.That(jokers.GetLowestCard().HasValue, Is.False);
            Assert.That(jokers.GetHighestCard().HasValue, Is.False);
        }

        [Test]
        public void HandModel_Queries_AllocateNothingWhenWarm()
        {
            HandModel hand = new HandModel();
            CardData probe = new CardData(Suit.Clubs, Rank.King);
            hand.AddRange(new[]
            {
                new CardData(Suit.Hearts, Rank.Six), new CardData(Suit.Spades, Rank.Ace), probe,
                CardData.CreateJoker(true)
            });
            int hits = 0;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                hits += hand.ContainsRank(Rank.Ace) ? 1 : 0;
                hits += hand.ContainsSuit(Suit.Diamonds) ? 1 : 0;
                hits += hand.Contains(probe) ? 1 : 0;
                hits += hand.IndexOf(probe);
                hits += hand.GetLowestCard(Suit.Hearts).HasValue ? 1 : 0;
                hits += hand.GetHighestCard().HasValue ? 1 : 0;
            }

            Assert.That(allocationCounter.End(), Is.EqualTo(0L));
            Assert.That(hits, Is.EqualTo(Iterations * 6));
        }

        [Test]
        public void HandModel_BuiltInSortsAndComparers_AllocateNothingWhenWarm()
        {
            HandModel hand = new HandModel();
            for (int i = 0; i < 12; i++)
            {
                hand.Add(new CardData((Suit)(i % 4), (Rank)(2 + i * 5 % 13)));
            }

            Comparison<CardData> byValue = CardComparer.ByValueDescending.Compare;
            hand.SortByRank();
            hand.SortBySuit(false);
            hand.Sort(byValue);
            CardData first = hand.Cards[0];
            CardData last = hand.Cards[hand.Count - 1];
            int order = 0;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                hand.SortByRank(i % 2 == 0);
                hand.SortBySuit(i % 3 == 0);
                hand.Sort(byValue);
                order += CardComparer.ByGroupThenValueAscending.Compare(first, last);
                order += CardComparer.BySuitThenRankAscending.Compare(last, first);
            }

            long calls = allocationCounter.End();
            Assert.That(hand.Cards[0], Is.EqualTo(first), "The last sort is the same stable value order.");
            Assert.That(order, Is.Not.EqualTo(int.MinValue));
            Assert.That(calls, Is.EqualTo(0L),
                "Built-in sorts of hands up to 32 cards run an in-place insertion sort with cached comparisons.");
        }

        [Test]
        public void Layout_RandomizedLayouts_AreStableAndLeaveUnityRandomUntouched()
        {
            CardLayoutSettings settings = CardLayoutSettings.Default;
            settings.PositionJitter = new Vector3(0.5f, 0.25f, 0.1f);
            settings.RotationJitter = new Vector3(0f, 0f, 5f);
            UnityEngine.Random.State previous = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(1234);
                List<Vector3> first = CardLayoutCalculator.CalculatePositions(CardLayoutType.Scattered, 5, settings);
                List<Quaternion> rotations =
                    CardLayoutCalculator.CalculateRotations(CardLayoutType.Scattered, 5, settings);
                float afterLayout = UnityEngine.Random.value;
                UnityEngine.Random.InitState(1234);
                Assert.That(afterLayout, Is.EqualTo(UnityEngine.Random.value),
                    "Layout must not consume the global UnityEngine.Random sequence.");

                List<Vector3> again = CardLayoutCalculator.CalculatePositions(CardLayoutType.Scattered, 5, settings);
                List<Vector3> grown = CardLayoutCalculator.CalculatePositions(CardLayoutType.Scattered, 8, settings);
                CollectionAssert.AreEqual(first, again);
                CollectionAssert.AreEqual(first, grown.Take(5).ToList(), "Adding cards must not move earlier ones.");
                CollectionAssert.AreEqual(rotations,
                    CardLayoutCalculator.CalculateRotations(CardLayoutType.Scattered, 5, settings));

                List<Vector3> seeded = new();
                CardLayoutCalculator.CalculatePositions(CardLayoutType.Scattered, 5, settings, seeded, 99);
                CollectionAssert.AreNotEqual(first, seeded);
                foreach (Vector3 position in first)
                {
                    Vector3 planar = new(position.x, position.y, 0f);
                    Assert.That(planar.magnitude, Is.LessThanOrEqualTo(settings.ScatteredRadius + 1f));
                }
            }
            finally
            {
                UnityEngine.Random.state = previous;
            }
        }

        [Test]
        public void Layout_NonPositiveCount_ReturnsEmptyLists()
        {
            CardLayoutSettings settings = CardLayoutSettings.Default;

            Assert.That(CardLayoutCalculator.CalculatePositions(CardLayoutType.Fan, -3, settings), Is.Empty);
            Assert.That(CardLayoutCalculator.CalculateRotations(CardLayoutType.Grid, -1, settings), Is.Empty);
            Assert.Throws<ArgumentNullException>(() =>
                CardLayoutCalculator.CalculatePositions(CardLayoutType.Line, 2, settings, null));
        }

        [TestCase(CardLayoutType.Fan)]
        [TestCase(CardLayoutType.Scattered)]
        [TestCase(CardLayoutType.Grid)]
        public void Layout_FillOverloads_AllocateNothingWhenWarm(CardLayoutType layoutType)
        {
            CardLayoutSettings settings = CardLayoutSettings.Default;
            settings.PositionJitter = Vector3.one;
            settings.RotationJitter = Vector3.one;
            List<Vector3> positions = new();
            List<Quaternion> rotations = new();
            CardLayoutCalculator.CalculatePositions(layoutType, 12, settings, positions);
            CardLayoutCalculator.CalculateRotations(layoutType, 12, settings, rotations);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                CardLayoutCalculator.CalculatePositions(layoutType, 12, settings, positions);
                CardLayoutCalculator.CalculateRotations(layoutType, 12, settings, rotations);
            }

            Assert.That(allocationCounter.End(), Is.EqualTo(0L));
            Assert.That(positions, Has.Count.EqualTo(12));
            Assert.That(rotations, Has.Count.EqualTo(12));
        }

        [Test]
        public void CardSettingsRuntime_PlaySessionReset_ClearsGlobalAnimationConfig()
        {
            CardAnimationConfig previous = CardSettingsRuntime.GlobalAnimationConfig;
            MethodInfo reset = typeof(CardSettingsRuntime).GetMethod("ResetStatics",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null);
            RuntimeInitializeOnLoadMethodAttribute attribute =
                reset.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.SubsystemRegistration));
            try
            {
                CardSettingsRuntime.GlobalAnimationConfig = Own(ScriptableObject.CreateInstance<CardAnimationConfig>());

                reset.Invoke(null, null);

                Assert.That(CardSettingsRuntime.GlobalAnimationConfig, Is.Null);
            }
            finally
            {
                CardSettingsRuntime.GlobalAnimationConfig = previous;
            }
        }

        [Test]
        public void DeckConfig_CustomSpriteLookup_FollowsSerializedEditsAfterCaching()
        {
            DeckConfig config = CreateCustomConfig("wolf", "bear");
            Sprite wolfFace = CreateSprite();
            Sprite bearFace = CreateSprite();
            Sprite replacement = CreateSprite();
            SetCustomSprite(config, 0, wolfFace);
            SetCustomSprite(config, 1, bearFace);
            CardData wolf = CardData.CreateCustom("wolf");
            CardData bear = CardData.CreateCustom("bear");
            Assert.That(config.GetSprite(wolf), Is.SameAs(wolfFace));
            Assert.That(config.GetSprite(bear), Is.SameAs(bearFace));

            SetCustomSprite(config, 0, replacement);
            Assert.That(config.GetSprite(wolf), Is.SameAs(replacement));

            SerializedObject serialized = new(config);
            SerializedProperty cards = serialized.FindProperty("_customCards");
            cards.GetArrayElementAtIndex(0).FindPropertyRelative("_id").stringValue = "fox";
            cards.GetArrayElementAtIndex(1).FindPropertyRelative("_id").stringValue = "wolf";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(config.GetSprite(wolf), Is.SameAs(bearFace));
            Assert.That(config.GetSprite(CardData.CreateCustom("fox")), Is.SameAs(replacement));
            Assert.That(config.GetSprite(bear), Is.Null);
        }

        [Test]
        public void DeckConfig_CustomSpriteLookup_AllocatesNothingWhenWarm()
        {
            DeckConfig config = CreateCustomConfig("c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7");
            Sprite face = CreateSprite();
            SetCustomSprite(config, 7, face);
            CardData last = CardData.CreateCustom("c7");
            Assert.That(config.GetSprite(last), Is.SameAs(face));

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                config.GetSprite(last);
            }

            Assert.That(allocationCounter.End(), Is.EqualTo(0L));
        }

        [Test]
        public void DeckConfig_GenerateDeck_KeepsSuitMajorOrder()
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());

            List<CardData> cards = config.GenerateDeck(DeckType.Standard36);

            Assert.That(cards, Has.Count.EqualTo(36));
            Assert.That(cards[0], Is.EqualTo(new CardData(Suit.Hearts, Rank.Six)));
            Assert.That(cards[9], Is.EqualTo(new CardData(Suit.Diamonds, Rank.Six)));
            Assert.That(cards[35], Is.EqualTo(new CardData(Suit.Spades, Rank.Ace)));
        }

        [Test]
        public void DeckComponent_FixedSeed_ReproducesOrderOnInitializeAndReset()
        {
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            SetField(deck, "_config", CreateCustomConfig(Ids(20)));
            deck.UseFixedShuffleSeed = true;
            deck.ShuffleSeed = 2026;

            deck.Initialize();
            CardData[] first = deck.Model.Cards.ToArray();
            deck.Initialize();
            CollectionAssert.AreEqual(first, deck.Model.Cards);

            deck.Model.Draw(3);
            deck.Reset();
            CollectionAssert.AreEqual(first, deck.Model.Cards);

            deck.ShuffleSeed = 2027;
            deck.Initialize();
            CollectionAssert.AreNotEqual(first, deck.Model.Cards);
            CollectionAssert.AreEquivalent(first, deck.Model.Cards);
        }

        [Test]
        public void DeckComponent_TrumpDisplay_FollowsShuffleAndBottomReturns()
        {
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            CardComponent display = CreateComponent<CardComponent>("Trump");
            SetField(deck, "_config", CreateCustomConfig(Ids(12)));
            SetField(deck, "_trumpCardDisplay", display);
            deck.UseFixedShuffleSeed = true;
            deck.ShuffleSeed = 5;
            deck.Initialize();
            CardData initialTrump = deck.TrumpCard.Value;
            Assert.That(display.Data, Is.EqualTo(initialTrump));

            for (int i = 0; i < 10 && deck.TrumpCard.Value.Equals(initialTrump); i++)
            {
                deck.Shuffle();
            }

            Assert.That(deck.TrumpCard.Value, Is.Not.EqualTo(initialTrump), "Seeded shuffles must move the bottom.");
            Assert.That(display.Data, Is.EqualTo(deck.TrumpCard.Value));

            CardData returned = CardData.CreateCustom("returned");
            deck.Model.ReturnToBottom(returned);
            Assert.That(display.Data, Is.EqualTo(returned));
            Assert.That(display.gameObject.activeSelf, Is.True);

            deck.Model.Draw(deck.RemainingCount);
            Assert.That(display.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DeckPresenter_TrumpPreview_FollowsModelChanges()
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());
            SerializedObject serialized = new(config);
            serialized.FindProperty("_gameDeckType").intValue = (int)DeckType.Standard36;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RecordingDeckView view = new(Own(new GameObject("Spawn")).transform);
            DeckModel model = new DeckModel(9);
            DeckPresenter presenter = new(model, view, config, CreateComponent<CardView>("Prefab"));
            try
            {
                presenter.Initialize(false);
                Assert.That(view.TopCard, Is.EqualTo(model.PeekBottom()));

                CardData returned = new CardData(Suit.Clubs, Rank.Ace);
                model.ReturnToBottom(returned);
                Assert.That(view.TopCard, Is.EqualTo(returned));

                model.Draw(model.RemainingCount);
                Assert.That(view.TopCard.HasValue, Is.False);
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void HandComponent_StableSort_KeepsModelAndViewAlignedForIndexedRemoval()
        {
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            CardData[] data =
            {
                new CardData(Suit.Hearts, Rank.Seven), new CardData(Suit.Spades, Rank.Two),
                new CardData(Suit.Clubs, Rank.Seven), new CardData(Suit.Diamonds, Rank.Seven),
                new CardData(Suit.Hearts, Rank.Ace)
            };
            foreach (CardData card in data)
            {
                Complete(hand.AddCardAsync(CreateCard(card), false));
            }

            Complete(hand.SortByRankAsync(true, false));

            CollectionAssert.AreEqual(new[] { data[1], data[0], data[2], data[3], data[4] },
                hand.Cards.Select(card => card.Data));
            CollectionAssert.AreEqual(hand.Cards.Select(card => card.Data), hand.Model.Cards);

            CardComponent removed = hand.Cards[2];
            Assert.That(hand.RemoveAtAsync(2, false).GetAwaiter().GetResult(), Is.SameAs(removed));
            CollectionAssert.AreEqual(hand.Cards.Select(card => card.Data), hand.Model.Cards);
            Assert.That(hand.Contains(removed), Is.False);
        }

        [Test]
        public void HandComponent_SortAsyncWithComparer_SortsCustomCards()
        {
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            CardData low = CardData.CreateCustom("low", null, 1);
            CardData high = CardData.CreateCustom("high", null, 9);
            CardData mid = CardData.CreateCustom("mid", null, 5);
            foreach (CardData card in new[] { low, high, mid })
            {
                Complete(hand.AddCardAsync(CreateCard(card), false));
            }

            Complete(hand.SortAsync(CardComparer.ByValueDescending, false));

            CollectionAssert.AreEqual(new[] { high, mid, low }, hand.Cards.Select(card => card.Data));
            CollectionAssert.AreEqual(new[] { high, mid, low }, hand.Model.Cards);
            Assert.Throws<ArgumentNullException>(() => hand.SortAsync((IComparer<CardData>)null));
        }

        [Test]
        public void HandComponent_DrawRandomWithSeed_IsReproducible()
        {
            HandComponent first = CreateComponent<HandComponent>("First");
            HandComponent second = CreateComponent<HandComponent>("Second");
            for (int i = 0; i < 6; i++)
            {
                Complete(first.AddCardAsync(CreateCard(CardData.CreateCustom("a" + i)), false));
                Complete(second.AddCardAsync(CreateCard(CardData.CreateCustom("a" + i)), false));
            }

            CardComponent fromFirst = first.DrawRandom(new Random(11));
            CardComponent fromSecond = second.DrawRandom(new Random(11));

            Assert.That(fromFirst.Data, Is.EqualTo(fromSecond.Data));
            Assert.That(first.Count, Is.EqualTo(5));
            Assert.That(first.Contains(fromFirst), Is.False);
            Assert.Throws<ArgumentNullException>(() => first.DrawRandom(null));
        }

        [Test]
        public void HandComponent_ClearAfterExternalDestroy_DropsClickHandlerEntries()
        {
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            CardComponent survivor = CreateCard(CardData.CreateCustom("survivor"));
            CardComponent destroyed = CreateCard(CardData.CreateCustom("destroyed"));
            survivor.OnClick = new UnityEngine.Events.UnityEvent();
            destroyed.OnClick = new UnityEngine.Events.UnityEvent();
            Complete(hand.AddCardAsync(survivor, false));
            Complete(hand.AddCardAsync(destroyed, false));
            Object.DestroyImmediate(destroyed.gameObject);
            Object.DestroyImmediate(survivor.gameObject);

            hand.Clear();

            FieldInfo field = typeof(HandComponent).GetField("_cardClickHandlers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            System.Collections.IDictionary handlers = (System.Collections.IDictionary)field.GetValue(hand);
            Assert.That(handlers.Count, Is.Zero);
            Assert.That(hand.Count, Is.Zero);
        }

        [Test]
        public void HandPresenter_SortAsyncWithComparer_AlignsModelPresentersAndView()
        {
            DeckConfig config = CreateCustomConfig("a", "b", "c");
            HandView view = CreateComponent<HandView>("HandView");
            HandPresenter hand = new(new HandModel(), view);
            CardData[] data =
            {
                CardData.CreateCustom("a", null, 3), CardData.CreateCustom("b", null, 1),
                CardData.CreateCustom("c", null, 2)
            };
            foreach (CardData card in data)
            {
                CardViewUniversal cardView = CreateComponent<CardViewUniversal>("Card_" + card.CustomId);
                CardPresenter presenter = new(cardView, config);
                presenter.SetData(card);
                Complete(hand.AddCardAsync(presenter, false));
            }

            Complete(hand.SortAsync(CardComparer.ByValueAscending, false));

            CollectionAssert.AreEqual(new[] { data[1], data[2], data[0] }, hand.Model.Cards);
            CollectionAssert.AreEqual(hand.Model.Cards, hand.CardPresenters.Select(card => card.Data));
            CollectionAssert.AreEqual(hand.CardPresenters.Select(card => card.View), view.CardViews);
            Assert.That(hand.FindCard(data[2]), Is.SameAs(hand.CardPresenters[1]));
        }

        [Test]
        public void CardComparer_ValueComparers_HandleStandardJokersAndCustomCards()
        {
            List<CardData> cards = new()
            {
                CardData.CreateJoker(true), new CardData(Suit.Spades, Rank.King), new CardData(Suit.Hearts, Rank.Two)
            };

            cards.Sort(CardComparer.ByValueAscending);
            Assert.That(cards[0].Rank, Is.EqualTo(Rank.Two));
            Assert.That(cards[2].IsJoker, Is.True);

            cards.Sort(CardComparer.ByValueDescending);
            Assert.That(cards[0].IsJoker, Is.True);
            Assert.That(cards[2].Rank, Is.EqualTo(Rank.Two));
        }

        [Test]
        public void PokerEvaluate_MatchesReferenceEvaluatorOnCraftedAndSeededHands()
        {
            foreach (CardData[] hand in CraftedHands())
            {
                AssertSameResult(ReferencePokerEvaluator.Evaluate(hand), PokerHandEvaluator.Evaluate(hand), hand);
            }

            Random random = new(20261102);
            List<CardData> deck = StandardDeck();
            for (int iteration = 0; iteration < 1500; iteration++)
            {
                CardData[] hand = Deal(deck, 5 + random.Next(3), random);
                AssertSameResult(ReferencePokerEvaluator.Evaluate(hand), PokerHandEvaluator.Evaluate(hand), hand);
            }
        }

        [Test]
        public void PokerEvaluateStrength_OrdersHandsExactlyLikeCompareTo()
        {
            Random random = new(77);
            List<CardData> deck = StandardDeck();
            for (int iteration = 0; iteration < 800; iteration++)
            {
                CardData[] first = Deal(deck, 5 + random.Next(3), random);
                CardData[] second = Deal(deck, 5 + random.Next(3), random);
                PokerHandResult firstResult = PokerHandEvaluator.Evaluate(first);
                PokerHandResult secondResult = PokerHandEvaluator.Evaluate(second);
                int firstStrength = PokerHandEvaluator.EvaluateStrength(first);
                int secondStrength = PokerHandEvaluator.EvaluateStrength(second);

                Assert.That(Math.Sign(firstStrength.CompareTo(secondStrength)),
                    Is.EqualTo(Math.Sign(firstResult.CompareTo(secondResult))));
                Assert.That(PokerHandEvaluator.GetCombination(firstStrength), Is.EqualTo(firstResult.Combination));
            }
        }

        [Test]
        public void PokerEvaluateStrength_AllocatesNothingWhenWarm()
        {
            CardData[] hand =
            {
                new CardData(Suit.Hearts, Rank.Ace), new CardData(Suit.Hearts, Rank.King),
                new CardData(Suit.Clubs, Rank.Seven), new CardData(Suit.Hearts, Rank.Queen),
                new CardData(Suit.Spades, Rank.Two), new CardData(Suit.Hearts, Rank.Jack),
                new CardData(Suit.Hearts, Rank.Ten)
            };
            int strength = PokerHandEvaluator.EvaluateStrength(hand);
            Assert.That(PokerHandEvaluator.GetCombination(strength), Is.EqualTo(PokerCombination.RoyalFlush));

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                strength ^= PokerHandEvaluator.EvaluateStrength(hand);
            }

            Assert.That(allocationCounter.End(), Is.EqualTo(0L));
        }

        [Test]
        public void PokerEvaluateStrength_RejectsInvalidInputLikeEvaluate()
        {
            CardData[] four =
            {
                new CardData(Suit.Hearts, Rank.Ace), new CardData(Suit.Hearts, Rank.King),
                new CardData(Suit.Clubs, Rank.Seven), CardData.CreateJoker(true), CardData.CreateJoker(false)
            };
            CardData[] custom =
            {
                CardData.CreateCustom("a"), CardData.CreateCustom("b"), CardData.CreateCustom("c"),
                CardData.CreateCustom("d"), CardData.CreateCustom("e")
            };

            Assert.Throws<ArgumentException>(() => PokerHandEvaluator.EvaluateStrength(four));
            Assert.Throws<ArgumentException>(() => PokerHandEvaluator.EvaluateStrength(custom));
            Assert.Throws<ArgumentNullException>(() => PokerHandEvaluator.EvaluateStrength(null));
        }

        [Test]
        public void PokerRules_CountOuts_MatchesFullEvaluation()
        {
            CardData[] current =
            {
                new CardData(Suit.Hearts, Rank.Nine), new CardData(Suit.Hearts, Rank.Ten),
                new CardData(Suit.Clubs, Rank.Jack), new CardData(Suit.Hearts, Rank.Queen),
                new CardData(Suit.Spades, Rank.Two), new CardData(Suit.Hearts, Rank.Four)
            };
            List<CardData> remaining = StandardDeck().Where(card => !current.Contains(card)).ToList();

            foreach (PokerCombination target in new[]
                     {
                         PokerCombination.Pair, PokerCombination.Straight, PokerCombination.Flush,
                         PokerCombination.StraightFlush
                     })
            {
                int expected = remaining.Count(card =>
                    PokerHandEvaluator.Evaluate(current.Append(card)).Combination >= target);
                Assert.That(PokerRules.CountOuts(current, target, remaining), Is.EqualTo(expected), target.ToString());
            }
        }

        private static void AssertSameResult(PokerHandResult expected, PokerHandResult actual,
            IEnumerable<CardData> hand)
        {
            string label = string.Join(" ", hand.Select(card => card.ToString()));
            Assert.That(actual.Combination, Is.EqualTo(expected.Combination), label);
            CollectionAssert.AreEqual(expected.CombinationRanks, actual.CombinationRanks, label);
            CollectionAssert.AreEqual(expected.Kickers, actual.Kickers, label);
            CollectionAssert.AreEqual(expected.BestHand, actual.BestHand, label);
            Assert.That(actual.CompareTo(expected), Is.Zero, label);
        }

        private static IEnumerable<CardData[]> CraftedHands()
        {
            yield return Hand("AH KH QH JH TH 2C 3D");
            yield return Hand("5S 4S 3S 2S AS KD KC");
            yield return Hand("9C 9D 9H 9S 2C 3C 4C");
            yield return Hand("8C 8D 8H 4S 4C 4D 2H");
            yield return Hand("2H 7H 9H JH KH AS AD");
            yield return Hand("6C 5D 4H 3S 2C AD KH");
            yield return Hand("AH 2D 3C 4S 5H 9C 9D");
            yield return Hand("QC QD QH 7S 3C 2D 9H");
            yield return Hand("JC JD 4H 4S 9C 9D AH");
            yield return Hand("TC TD 4H 8S 3C 2D AH");
            yield return Hand("AC KD 9H 7S 5C 3D 2H");
            yield return Hand("AS AS KD KD QH 2C 3C");
            yield return Hand("2C 3C 4C 5C 6C 7C 8C");
            yield return Hand("KH KD KS 2C 2D");
        }

        private static CardData[] Hand(string notation)
        {
            string[] tokens = notation.Split(' ');
            CardData[] cards = new CardData[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                Rank rank = tokens[i][0] switch
                {
                    'A' => Rank.Ace,
                    'K' => Rank.King,
                    'Q' => Rank.Queen,
                    'J' => Rank.Jack,
                    'T' => Rank.Ten,
                    _ => (Rank)(tokens[i][0] - '0')
                };
                Suit suit = tokens[i][1] switch
                {
                    'H' => Suit.Hearts,
                    'D' => Suit.Diamonds,
                    'C' => Suit.Clubs,
                    _ => Suit.Spades
                };
                cards[i] = new CardData(suit, rank);
            }

            return cards;
        }

        private static List<CardData> StandardDeck()
        {
            DeckModel deck = new DeckModel(1);
            deck.Initialize(DeckType.Standard52, false);
            return deck.Cards.ToList();
        }

        private static CardData[] Deal(List<CardData> deck, int count, Random random)
        {
            List<CardData> pool = new(deck);
            CardData[] hand = new CardData[count];
            for (int i = 0; i < count; i++)
            {
                int index = random.Next(pool.Count);
                hand[i] = pool[index];
                pool.RemoveAt(index);
            }

            return hand;
        }

        private static string[] Ids(int count)
        {
            string[] ids = new string[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = "card" + i;
            }

            return ids;
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

        private static void SetCustomSprite(DeckConfig config, int index, Sprite sprite)
        {
            SerializedObject serialized = new(config);
            serialized.FindProperty("_customCards").GetArrayElementAtIndex(index).FindPropertyRelative("_sprite")
                .objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = Own(new Texture2D(2, 2));
            return Own(Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero));
        }

        private CardComponent CreateCard(CardData data)
        {
            CardComponent card = CreateComponent<CardComponent>("Card_" + data);
            card.SetData(data);
            return card;
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            return Own(new GameObject(name)).AddComponent<T>();
        }

        private T Own<T>(T value) where T : Object
        {
            _owned.Add(value);
            return value;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void Complete(UniTask task)
        {
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "This scenario uses immediate operations in Edit Mode.");
            awaiter.GetResult();
        }

        private sealed class RecordingDeckView : IDeckView
        {
            internal RecordingDeckView(Transform spawnPoint)
            {
                SpawnPoint = spawnPoint;
            }

            internal CardData? TopCard { get; private set; }

            public Transform SpawnPoint { get; }

            public int VisibleCardCount { get; set; } = 1;

            public void UpdateVisual(int remainingCount)
            {
            }

            public void ShowTopCard(CardData card)
            {
                TopCard = card;
            }

            public void HideTopCard()
            {
                TopCard = null;
            }
        }

        /// <summary>
        ///     The pre-2026-11 LINQ evaluator, kept verbatim in behaviour as the oracle for the rewrite.
        /// </summary>
        private static class ReferencePokerEvaluator
        {
            internal static PokerHandResult Evaluate(IEnumerable<CardData> cards)
            {
                List<CardData> cardList = cards.Where(c => !c.IsJoker).ToList();
                if (cardList.Count == 5)
                {
                    return EvaluateFiveCards(cardList);
                }

                PokerHandResult best = null;
                foreach (IEnumerable<CardData> combination in GetCombinations(cardList, 5))
                {
                    PokerHandResult result = EvaluateFiveCards(combination.ToList());
                    if (best == null || result.CompareTo(best) > 0)
                    {
                        best = result;
                    }
                }

                return best;
            }

            private static PokerHandResult EvaluateFiveCards(List<CardData> cards)
            {
                List<CardData> sorted = cards.OrderByDescending(c => c.Rank).ToList();
                Dictionary<int, List<Rank>> groups = GetRankGroups(sorted);
                bool isFlush = sorted.All(c => c.Suit == sorted[0].Suit);
                bool isStraight = IsStraight(sorted, out Rank high);

                if (isFlush && isStraight)
                {
                    return new PokerHandResult(high == Rank.Ace ? PokerCombination.RoyalFlush : PokerCombination.StraightFlush,
                        new[] { high == Rank.Ace ? Rank.Ace : high }, Array.Empty<Rank>(), sorted);
                }

                if (groups.ContainsKey(4))
                {
                    Rank four = groups[4][0];
                    return new PokerHandResult(PokerCombination.FourOfAKind, new[] { four },
                        sorted.Where(c => c.Rank != four).Select(c => c.Rank).Take(1).ToList(), sorted);
                }

                if (groups.ContainsKey(3) && groups.ContainsKey(2))
                {
                    return new PokerHandResult(PokerCombination.FullHouse, new[] { groups[3][0], groups[2][0] },
                        Array.Empty<Rank>(), sorted);
                }

                if (isFlush)
                {
                    return new PokerHandResult(PokerCombination.Flush, sorted.Select(c => c.Rank).ToList(),
                        Array.Empty<Rank>(), sorted);
                }

                if (isStraight)
                {
                    return new PokerHandResult(PokerCombination.Straight, new[] { high }, Array.Empty<Rank>(), sorted);
                }

                if (groups.ContainsKey(3))
                {
                    Rank three = groups[3][0];
                    return new PokerHandResult(PokerCombination.ThreeOfAKind, new[] { three },
                        sorted.Where(c => c.Rank != three).Select(c => c.Rank).Take(2).ToList(), sorted);
                }

                if (groups.ContainsKey(2))
                {
                    List<Rank> pairs = groups[2].OrderByDescending(r => r).ToList();
                    if (pairs.Count >= 2)
                    {
                        return new PokerHandResult(PokerCombination.TwoPair, new[] { pairs[0], pairs[1] },
                            sorted.Where(c => c.Rank != pairs[0] && c.Rank != pairs[1]).Select(c => c.Rank).Take(1)
                                .ToList(), sorted);
                    }

                    return new PokerHandResult(PokerCombination.Pair, new[] { pairs[0] },
                        sorted.Where(c => c.Rank != pairs[0]).Select(c => c.Rank).Take(3).ToList(), sorted);
                }

                List<Rank> ranks = sorted.Select(c => c.Rank).ToList();
                return new PokerHandResult(PokerCombination.HighCard, new[] { ranks[0] }, ranks.Skip(1).ToList(),
                    sorted);
            }

            private static Dictionary<int, List<Rank>> GetRankGroups(List<CardData> cards)
            {
                Dictionary<int, List<Rank>> result = new();
                foreach (IGrouping<Rank, CardData> group in cards.GroupBy(c => c.Rank))
                {
                    int count = group.Count();
                    if (!result.ContainsKey(count))
                    {
                        result[count] = new List<Rank>();
                    }

                    result[count].Add(group.Key);
                }

                foreach (List<Rank> list in result.Values)
                {
                    list.Sort((a, b) => b.CompareTo(a));
                }

                return result;
            }

            private static bool IsStraight(List<CardData> cards, out Rank high)
            {
                List<int> ranks = cards.Select(c => (int)c.Rank).Distinct().OrderByDescending(r => r).ToList();
                high = (Rank)ranks[0];
                if (ranks.Count < 5)
                {
                    return false;
                }

                bool consecutive = true;
                for (int i = 0; i < ranks.Count - 1; i++)
                {
                    consecutive &= ranks[i] - ranks[i + 1] == 1;
                }

                if (consecutive)
                {
                    return true;
                }

                if (ranks.Contains(14) && ranks.Contains(5) && ranks.Contains(4) && ranks.Contains(3) &&
                    ranks.Contains(2))
                {
                    high = Rank.Five;
                    return true;
                }

                return false;
            }

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
}
