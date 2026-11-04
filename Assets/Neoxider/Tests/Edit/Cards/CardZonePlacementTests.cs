using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Cards
{
    /// <summary>
    ///     Contracts of the 2026-11 Cards closure pass: batch board placement and removal with one layout pass,
    ///     allocation-free immediate layout, one-time capacity growth in the restore helper, the bounded initial-card
    ///     debug list, the single-pass visual stack and UI cards staying under their Canvas when a zone releases them.
    /// </summary>
    public sealed class CardZonePlacementTests
    {
        private const int Iterations = 50;
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
        public void PlaceCardsImmediate_MatchesSequentialLayoutAndPublishesAfterTheWholeBatch()
        {
            BoardComponent batchBoard = CreateLineBoard("Batch", 8);
            BoardComponent sequentialBoard = CreateLineBoard("Sequential", 8);
            CardComponent[] batchCards = CreateCards("Batch", 5);
            CardComponent[] sequentialCards = CreateCards("Sequential", 5);
            List<CardComponent> published = new();
            List<int> countsAtPublish = new();
            batchBoard.OnCardPlaced = new UnityEvent<CardComponent>();
            batchBoard.OnCardPlaced.AddListener(card =>
            {
                published.Add(card);
                countsAtPublish.Add(batchBoard.Count);
            });

            int placed = batchBoard.PlaceCardsImmediate(batchCards, false);
            foreach (CardComponent card in sequentialCards)
            {
                Complete(sequentialBoard.PlaceCardAsync(card, false, false));
            }

            Assert.That(placed, Is.EqualTo(5));
            CollectionAssert.AreEqual(batchCards, batchBoard.Cards);
            CollectionAssert.AreEqual(batchCards, published, "One placement event per card, in input order.");
            Assert.That(countsAtPublish.All(count => count == 5), Is.True,
                "Batch events are raised after every card of the batch is on the board.");
            for (int i = 0; i < batchCards.Length; i++)
            {
                Assert.That(batchCards[i].transform.parent, Is.SameAs(batchBoard.transform));
                Assert.That(batchCards[i].transform.localPosition,
                    Is.EqualTo(sequentialCards[i].transform.localPosition), "Card " + i);
                Assert.That(batchCards[i].transform.GetSiblingIndex(),
                    Is.EqualTo(sequentialCards[i].transform.GetSiblingIndex()), "Card " + i);
            }
        }

        [Test]
        public void PlaceCardsImmediate_SkipsNullDuplicatesAndMembers_StopsWhenFull_RaisesFullOnce()
        {
            BoardComponent board = CreateLineBoard("Board", 3);
            CardComponent[] cards = CreateCards("Card", 5);
            Complete(board.PlaceCardAsync(cards[0], false, false));
            int placements = 0;
            int fullEvents = 0;
            board.OnCardPlaced = new UnityEvent<CardComponent>();
            board.OnCardPlaced.AddListener(card => placements++);
            board.OnBoardFull = new UnityEvent();
            board.OnBoardFull.AddListener(() => fullEvents++);

            int placed = board.PlaceCardsImmediate(
                new[] { null, cards[1], cards[1], cards[0], cards[2], cards[3], cards[4] }, false);

            Assert.That(placed, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { cards[0], cards[1], cards[2] }, board.Cards);
            Assert.That(placements, Is.EqualTo(2));
            Assert.That(fullEvents, Is.EqualTo(1));
            Assert.That(cards[3].transform.parent, Is.Not.SameAs(board.transform));
            Assert.That(board.PlaceCardsImmediate(new[] { cards[3] }, false), Is.Zero, "A full board admits nothing.");
            Assert.That(fullEvents, Is.EqualTo(1));
        }

        [Test]
        public void PlaceCardsImmediate_NullList_Throws()
        {
            BoardComponent board = CreateLineBoard("Board", 3);

            Assert.Throws<ArgumentNullException>(() => board.PlaceCardsImmediate(null));
        }

        [Test]
        public void PlaceCardsImmediate_ClearFromAPlacementListener_StopsTheRemainingEvents()
        {
            BoardComponent board = CreateLineBoard("Board", 8);
            CardComponent[] cards = CreateCards("Card", 4);
            List<CardComponent> returned = null;
            int placements = 0;
            board.OnCardPlaced = new UnityEvent<CardComponent>();
            board.OnCardPlaced.AddListener(card =>
            {
                placements++;
                returned ??= board.ClearAndReturn();
            });

            int placed = board.PlaceCardsImmediate(cards, false);

            Assert.That(placed, Is.EqualTo(1));
            Assert.That(placements, Is.EqualTo(1));
            Assert.That(board.Count, Is.Zero);
            CollectionAssert.AreEqual(cards, returned);
        }

        [Test]
        public void PlaceCardsImmediate_ClearAndRefillFromAPlacementListener_RaisesOnlyTheRefillsFullEvent()
        {
            BoardComponent board = CreateLineBoard("Board", 3);
            CardComponent[] superseded = CreateCards("Superseded", 3);
            CardComponent[] refill = CreateCards("Refill", 3);
            List<CardComponent> published = new();
            List<CardComponent> returned = null;
            int fullEvents = 0;
            int refillPlaced = -1;
            board.OnBoardFull = new UnityEvent();
            board.OnBoardFull.AddListener(() => fullEvents++);
            board.OnCardPlaced = new UnityEvent<CardComponent>();
            board.OnCardPlaced.AddListener(card =>
            {
                published.Add(card);
                if (returned != null)
                {
                    return;
                }

                // WHY: models a deck reset or game restart started by the first placement event.
                returned = board.ClearAndReturn();
                refillPlaced = board.PlaceCardsImmediate(refill, false);
            });

            int placed = board.PlaceCardsImmediate(superseded, false);

            Assert.That(placed, Is.EqualTo(1), "The superseded batch stops publishing after the clear.");
            Assert.That(refillPlaced, Is.EqualTo(3));
            CollectionAssert.AreEqual(superseded, returned);
            CollectionAssert.AreEqual(refill, board.Cards);
            CollectionAssert.AreEqual(new[] { superseded[0], refill[0], refill[1], refill[2] }, published);
            Assert.That(fullEvents, Is.EqualTo(1), "Only the refill reports the full board.");
            Assert.That(refill.All(card => card.transform.parent == board.transform), Is.True);
        }

        [Test]
        public void ImmediateLayout_WarmRemoveAndPlace_DoesNotAllocate()
        {
            BoardComponent board = CreateLineBoard("Board", 64);
            CardComponent[] cards = CreateCards("Card", 32);
            board.PlaceCardsImmediate(cards, false);
            CardComponent last = cards[cards.Length - 1];
            board.RemoveCard(last);
            board.PlaceCard(last);
            Vector3 expected = last.transform.localPosition;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    board.RemoveCard(last);
                    board.PlaceCard(last);
                }
            }, out string notAdmitted);

            Assert.That(board.Count, Is.EqualTo(cards.Length));
            Assert.That(board.Cards[cards.Length - 1], Is.SameAs(last));
            Assert.That(last.transform.localPosition, Is.EqualTo(expected));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "An immediate layout pass reuses the board's member, position and rotation buffers.");
        }

        [Test]
        public void PlaceCardsImmediate_WarmBatch_DoesNotAllocate()
        {
            BoardComponent board = CreateLineBoard("Board", 64);
            CardComponent[] cards = CreateCards("Card", 32);
            Action cycle = () =>
            {
                for (int i = cards.Length - 1; i >= 0; i--)
                {
                    board.RemoveCard(cards[i]);
                }

                board.PlaceCardsImmediate(cards, false);
            };
            cycle();
            cycle();

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    cycle();
                }
            }, out string notAdmitted);

            CollectionAssert.AreEqual(cards, board.Cards);
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "A warm batch reuses its admission list and the layout buffers.");
        }

        [Test]
        public void RemoveCards_MatchesSequentialRemovalAndSkipsNullsStrangersAndRepeats()
        {
            BoardComponent batchBoard = CreateLineBoard("BatchRemove", 8);
            BoardComponent sequentialBoard = CreateLineBoard("SequentialRemove", 8);
            CardComponent[] batchCards = CreateCards("BatchRemove", 6);
            CardComponent[] sequentialCards = CreateCards("SequentialRemove", 6);
            CardComponent stranger = CreateCards("Stranger", 1)[0];
            batchBoard.PlaceCardsImmediate(batchCards, false);
            sequentialBoard.PlaceCardsImmediate(sequentialCards, false);
            int events = 0;
            batchBoard.OnBoardCleared = new UnityEvent();
            batchBoard.OnBoardCleared.AddListener(() => events++);
            batchBoard.OnCardPlaced = new UnityEvent<CardComponent>();
            batchBoard.OnCardPlaced.AddListener(card => events++);

            int removed = batchBoard.RemoveCards(new[] { batchCards[4], null, stranger, batchCards[1], batchCards[4] });
            sequentialBoard.RemoveCard(sequentialCards[4]);
            sequentialBoard.RemoveCard(sequentialCards[1]);

            Assert.That(removed, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { batchCards[0], batchCards[2], batchCards[3], batchCards[5] },
                batchBoard.Cards);
            Assert.That(events, Is.Zero, "Batch removal raises no board event, like RemoveCard.");
            Assert.That(batchCards[1].transform.parent, Is.Null);
            Assert.That(batchCards[4].transform.parent, Is.Null);
            Assert.That(batchBoard.CanPlaceCard(batchCards[1]), Is.True, "A removed card can be placed again.");
            for (int i = 0; i < batchBoard.Count; i++)
            {
                Assert.That(batchBoard.Cards[i].transform.localPosition,
                    Is.EqualTo(sequentialBoard.Cards[i].transform.localPosition), "Card " + i);
                Assert.That(batchBoard.Cards[i].transform.GetSiblingIndex(),
                    Is.EqualTo(sequentialBoard.Cards[i].transform.GetSiblingIndex()), "Card " + i);
            }
        }

        [Test]
        public void RemoveCards_PassingCardsItself_ReleasesEveryMemberWithoutAClearEvent()
        {
            BoardComponent board = CreateLineBoard("SelfRemove", 4);
            CardComponent[] cards = CreateCards("SelfRemove", 4);
            board.PlaceCardsImmediate(cards, false);
            int clears = 0;
            board.OnBoardCleared = new UnityEvent();
            board.OnBoardCleared.AddListener(() => clears++);

            int removed = board.RemoveCards(board.Cards);

            Assert.That(removed, Is.EqualTo(4));
            Assert.That(board.IsEmpty, Is.True);
            Assert.That(clears, Is.Zero);
            Assert.That(cards.All(card => card.transform.parent == null), Is.True);
            Assert.That(board.RemoveCards(cards), Is.Zero, "Cards that left the board are skipped.");
        }

        [Test]
        public void RemoveCards_NullList_Throws()
        {
            BoardComponent board = CreateLineBoard("Board", 3);

            Assert.Throws<ArgumentNullException>(() => board.RemoveCards(null));
        }

        [Test]
        public void RemoveCards_WarmBatch_DoesNotAllocate()
        {
            BoardComponent board = CreateLineBoard("Board", 64);
            CardComponent[] cards = CreateCards("Card", 32);
            CardComponent[] leaving = { cards[3], cards[7], cards[20], cards[31] };
            Action cycle = () =>
            {
                board.RemoveCards(leaving);
                board.PlaceCardsImmediate(leaving, false);
            };
            board.PlaceCardsImmediate(cards, false);
            cycle();
            cycle();
            int removed = 0;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    removed += board.RemoveCards(leaving);
                    board.PlaceCardsImmediate(leaving, false);
                }
            }, out string notAdmitted);

            Assert.That(removed, Is.EqualTo(40));
            Assert.That(board.Count, Is.EqualTo(cards.Length));
            CollectionAssert.AreEquivalent(cards, board.Cards);
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero, "A warm batch removal reuses its copy list and the layout buffers.");
        }

        [Test]
        public void RestoreAllSourcesToBoard_GrowsCapacityOnceAndRaisesFullOnce()
        {
            BoardComponent board = CreateLineBoard("Board", 2);
            SetField(board, "_autoExpandCapacity", true);
            GameObject pile = Own(new GameObject("Pile"));
            CardComponent[] cards = CreateCards("Pile", 5);
            foreach (CardComponent card in cards)
            {
                card.transform.SetParent(pile.transform, false);
            }

            SetField(board, "_extraRoots", new List<Transform> { pile.transform });
            int fullEvents = 0;
            int placements = 0;
            board.OnBoardFull = new UnityEvent();
            board.OnBoardFull.AddListener(() => fullEvents++);
            board.OnCardPlaced = new UnityEvent<CardComponent>();
            board.OnCardPlaced.AddListener(card => placements++);

            Complete(board.RestoreAllSourcesToBoardAsync());

            Assert.That(board.MaxCards, Is.EqualTo(5));
            CollectionAssert.AreEquivalent(cards, board.Cards);
            Assert.That(placements, Is.EqualTo(5));
            Assert.That(fullEvents, Is.EqualTo(1), "Capacity grows once, so the board reports full once.");
        }

        [Test]
        public void RegisterInitialCard_DropsDestroyedEntriesAndKeepsOneEntryPerCard()
        {
            BoardComponent board = CreateLineBoard("Board", 4);
            CardComponent[] cards = CreateCards("Initial", 4);
            board.RegisterInitialCard(cards[0]);
            board.RegisterInitialCard(cards[1]);
            board.RegisterInitialCard(cards[2]);
            board.RegisterInitialCard(cards[0]);
            Object.DestroyImmediate(cards[1].gameObject);
            Object.DestroyImmediate(cards[2].gameObject);

            board.RegisterInitialCard(cards[3]);

            List<CardComponent> registered = GetField<List<CardComponent>>(board, "_initialSpawnedCards");
            CollectionAssert.AreEqual(new[] { cards[0], cards[3] }, registered);
        }

        [Test]
        public void DeckComponent_BuildVisualStack_PlacesTheWholeStackInModelOrderBeforePublishing()
        {
            DeckComponent deck = CreateComponent<DeckComponent>("Deck");
            BoardComponent stack = CreateLineBoard("Stack", 64);
            CardComponent prefab = CreateComponent<CardComponent>("Prefab");
            SetField(deck, "_config", CreateCustomConfig(6));
            SetField(deck, "_cardPrefab", prefab);
            SetField(deck, "_visualStackBoard", stack);
            SetField(deck, "_shuffleOnStart", false);
            deck.OnVisualStackBuilt = new UnityEvent();
            int built = 0;
            deck.OnVisualStackBuilt.AddListener(() => built++);
            List<int> countsAtPublish = new();
            stack.OnCardPlaced = new UnityEvent<CardComponent>();
            stack.OnCardPlaced.AddListener(card => countsAtPublish.Add(stack.Count));
            deck.Initialize();
            CardData[] expected = deck.Model.Cards.ToArray();

            try
            {
                Complete(deck.BuildVisualStackAsync());

                CollectionAssert.AreEqual(expected, stack.Cards.Select(card => card.Data).ToArray());
                Assert.That(countsAtPublish, Has.Count.EqualTo(6));
                Assert.That(countsAtPublish.All(count => count == 6), Is.True);
                Assert.That(built, Is.EqualTo(1));
                Assert.That(stack.Cards.All(card => card.transform.parent == stack.transform), Is.True);
            }
            finally
            {
                foreach (CardComponent card in stack.Cards)
                {
                    if (card != null)
                    {
                        Own(card.gameObject);
                    }
                }
            }
        }

        [Test]
        public void RemoveCard_UiCard_StaysUnderTheRootCanvasAndWorldCardGoesToTheSceneRoot()
        {
            GameObject canvas = Own(new GameObject("Canvas", typeof(Canvas)));
            GameObject panel = UiChild(canvas.transform, "Panel");
            GameObject nested = UiChild(panel.transform, "Nested");
            nested.AddComponent<Canvas>();
            BoardComponent uiBoard = UiChild(nested.transform, "UiBoard").AddComponent<BoardComponent>();
            uiBoard.SetCapacity(4, false);
            CardComponent uiCard = UiChild(canvas.transform, "UiCard").AddComponent<CardComponent>();
            CardComponent uiReturned = UiChild(canvas.transform, "UiReturned").AddComponent<CardComponent>();
            BoardComponent worldBoard = CreateLineBoard("WorldBoard", 4);
            CardComponent worldCard = CreateCards("World", 1)[0];
            Complete(uiBoard.PlaceCardAsync(uiCard, false, false));
            Complete(uiBoard.PlaceCardAsync(uiReturned, false, false));
            Complete(worldBoard.PlaceCardAsync(worldCard, false, false));
            Vector3 uiPosition = uiCard.transform.position;

            Assert.That(uiBoard.RemoveCard(uiCard), Is.True);
            List<CardComponent> returned = uiBoard.ClearAndReturn();
            Assert.That(worldBoard.RemoveCard(worldCard), Is.True);

            Assert.That(uiCard.transform.parent, Is.SameAs(canvas.transform),
                "A released UI card stays under the root Canvas, so it keeps rendering during a transfer.");
            Assert.That(Vector3.Distance(uiCard.transform.position, uiPosition), Is.LessThan(0.001f));
            CollectionAssert.AreEqual(new[] { uiReturned }, returned);
            Assert.That(uiReturned.transform.parent, Is.SameAs(canvas.transform));
            Assert.That(worldCard.transform.parent, Is.Null, "World cards still detach to the scene root.");
        }

        [Test]
        public void RemoveCard_FromAZoneThatIsTheRootCanvas_ReleasesTheCardToTheZoneParent()
        {
            GameObject holder = Own(new GameObject("Holder", typeof(RectTransform)));
            GameObject canvas = UiChild(holder.transform, "BoardCanvas");
            canvas.AddComponent<Canvas>();
            BoardComponent board = canvas.AddComponent<BoardComponent>();
            board.SetCapacity(2, false);
            CardComponent card = UiChild(canvas.transform, "Card").AddComponent<CardComponent>();
            Complete(board.PlaceCardAsync(card, false, false));

            Assert.That(board.RemoveCard(card), Is.True);

            Assert.That(card.transform.parent, Is.SameAs(holder.transform));
        }

        [Test]
        public void HandRemoveCard_UiCard_StaysUnderTheRootCanvas()
        {
            GameObject canvas = Own(new GameObject("Canvas", typeof(Canvas)));
            HandComponent hand = UiChild(canvas.transform, "Hand").AddComponent<HandComponent>();
            SetField(hand, "_arrangeDuration", 0f);
            CardComponent card = UiChild(canvas.transform, "Card").AddComponent<CardComponent>();
            card.SetData(CardData.CreateCustom("ui-hand"));
            Assert.That(Complete(hand.TryAddCardAsync(card, false)), Is.True);

            Complete(hand.RemoveCardAsync(card, false));

            Assert.That(hand.Count, Is.Zero);
            Assert.That(card.transform.parent, Is.SameAs(canvas.transform));
        }

        [Test]
        public void HandImmediateArrange_WarmCall_DoesNotAllocate()
        {
            HandComponent hand = CreateComponent<HandComponent>("Hand");
            SetField(hand, "_arrangeDuration", 0f);
            CardComponent[] cards = CreateCards("Hand", 16);
            foreach (CardComponent card in cards)
            {
                Assert.That(Complete(hand.TryAddCardAsync(card, false)), Is.True);
            }

            Complete(hand.ArrangeCardsAsync(false));
            Vector3 expected = cards[cards.Length - 1].transform.localPosition;

            long calls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    hand.ArrangeCardsAsync(false).Forget();
                }
            }, out string notAdmitted);

            Assert.That(hand.Count, Is.EqualTo(cards.Length));
            Assert.That(cards[cards.Length - 1].transform.localPosition, Is.EqualTo(expected));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "An immediate hand layout reuses its member and pose buffers and creates no cancellation source.");
        }

        [Test]
        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        public void Performance_StackPlacement_BatchVersusSequential()
        {
            const int samples = 60;
            NeoPerf.EmitCalibration("cards.board", samples);
            int[] counts = { 13, 54, 128 };
            foreach (int count in counts)
            {
                BoardComponent board = CreateLineBoard("PerfBoard" + count, count);
                CardComponent[] cards = CreateCards("Perf" + count, count);

                // WHY: both rows release the stack through ClearAndReturn (O(n), one returned list per sample), so the
                // difference between the rows is the placement cost alone.
                NeoPerf.MeasureSamples("cards.board.place_stack.sequential.n" + count, count, count,
                        NeoPerf.DefaultWarmupSamples, samples, () =>
                        {
                            for (int i = 0; i < cards.Length; i++)
                            {
                                board.PlaceCard(cards[i]);
                            }

                            board.ClearAndReturn();
                        })
                    .Add("role", "before")
                    .Add("release", "clear_and_return_1_alloc")
                    .Emit();
                NeoPerf.MeasureSamples("cards.board.place_stack.batch.n" + count, count, count,
                        NeoPerf.DefaultWarmupSamples, samples, () =>
                        {
                            board.PlaceCardsImmediate(cards, false);
                            board.ClearAndReturn();
                        })
                    .Add("role", "current")
                    .Add("release", "clear_and_return_1_alloc")
                    .Emit();

                board.PlaceCardsImmediate(cards, false);
                CollectionAssert.AreEqual(cards, board.Cards);
            }
        }

        private BoardComponent CreateLineBoard(string name, int capacity)
        {
            BoardComponent board = CreateComponent<BoardComponent>(name);
            SetField(board, "_autoGenerateSlots", false);
            SetField(board, "_layoutType", CardLayoutType.Line);
            board.SetCapacity(capacity, false);
            return board;
        }

        private CardComponent[] CreateCards(string prefix, int count)
        {
            CardComponent[] cards = new CardComponent[count];
            for (int i = 0; i < count; i++)
            {
                cards[i] = CreateComponent<CardComponent>(prefix + i);
                cards[i].SetData(CardData.CreateCustom(prefix + i), false);
            }

            return cards;
        }

        private DeckConfig CreateCustomConfig(int count)
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());
            SerializedObject serialized = new(config);
            serialized.FindProperty("_useCustomDeck").boolValue = true;
            SerializedProperty entries = serialized.FindProperty("_customCards");
            entries.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue = "stack" + i;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static GameObject UiChild(Transform parent, string name)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
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

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }

        private static void Complete(UniTask task)
        {
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "This scenario uses immediate operations in Edit Mode.");
            awaiter.GetResult();
        }

        private static T Complete<T>(UniTask<T> task)
        {
            UniTask<T>.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "This scenario uses immediate operations in Edit Mode.");
            return awaiter.GetResult();
        }
    }
}
