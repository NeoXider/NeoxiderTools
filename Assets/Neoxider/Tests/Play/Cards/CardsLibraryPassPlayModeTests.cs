using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play Mode regressions for DeckComponent lifecycle fixes of the 2026-11 Cards quality pass.
    /// </summary>
    public sealed class CardsLibraryPassPlayModeTests
    {
        [UnityTest]
        public IEnumerator DeckReset_WithVisualStack_ClearsStaleStackAndKeepsDealing()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Fixture scene = new();
                scene.LoadCards("a", "b", "c");
                await Within(scene.Deck.BuildVisualStackAsync());
                Assert.That(scene.Stack.Count, Is.EqualTo(3));
                int visualEvents = 0;
                scene.Deck.OnVisualStackChanged.AddListener(() => visualEvents++);

                scene.Deck.Reset();

                Assert.That(scene.Stack.Count, Is.Zero, "Destroyed stack members must not stay on the board.");
                Assert.That(visualEvents, Is.EqualTo(1));
                CardComponent dealt = await Within(scene.Deck.DealToHandAsync(scene.Hand, true, 0f));
                Assert.That(dealt != null, Is.True, "The first deal after a reset must not pop a dead stack entry.");
                Assert.That(scene.Hand.Count, Is.EqualTo(1));
                Assert.That(scene.Deck.RemainingCount, Is.EqualTo(2));
            });
        }

        [UnityTest]
        public IEnumerator StackDeal_AfterModelReorder_DealsTheCardThatLeftTheModel()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Fixture scene = new();
                CardData a = CardData.CreateCustom("a");
                CardData b = CardData.CreateCustom("b");
                CardData c = CardData.CreateCustom("c");
                scene.LoadCards("a", "b", "c");
                await Within(scene.Deck.BuildVisualStackAsync());
                Assert.That(scene.Stack.Cards[scene.Stack.Count - 1].Data, Is.EqualTo(c));

                // WHY: a model-only reorder under an existing stack (Shuffle, ReturnCard, direct model use).
                scene.Deck.Model.Initialize(new[] { c, b, a }, false);
                CardComponent dealt = await Within(scene.Deck.DealToHandAsync(scene.Hand, true, 0f));

                Assert.That(dealt.Data, Is.EqualTo(a));
                CollectionAssert.AreEqual(new[] { a }, scene.Hand.Model.Cards);
                CollectionAssert.AreEqual(new[] { c, b }, scene.Deck.Model.Cards);
                CollectionAssert.AreEquivalent(new[] { a, b, c },
                    scene.Deck.Model.Cards.Concat(scene.Hand.Model.Cards), "No card may be duplicated or lost.");
            });
        }

        [UnityTest]
        public IEnumerator ReturnCard_FromHand_RemovesTheMemberBeforeDestroyingIt()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Fixture scene = new();
                scene.LoadCards("a", "b");
                CardComponent card = scene.Deck.DrawCard(true);
                Assert.That(await Within(scene.Hand.TryAddCardAsync(card, false)), Is.True);
                int removed = 0;
                scene.Hand.OnCardRemoved.AddListener(_ => removed++);

                scene.Deck.ReturnCard(card, true);
                await UniTask.Yield();

                Assert.That(scene.Hand.Count, Is.Zero);
                Assert.That(scene.Hand.Model.Count, Is.Zero);
                Assert.That(removed, Is.EqualTo(1));
                Assert.That(scene.Deck.Model.Peek(), Is.EqualTo(CardData.CreateCustom("b")));
                Assert.That(scene.Deck.RemainingCount, Is.EqualTo(2));
                await UniTask.Yield();
                Assert.That(card == null, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator ShuffleVisual_CardDealtDuringShake_IsNotPulledBackToTheStack()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Fixture scene = new();
                scene.LoadCards("a", "b", "c", "d");
                await Within(scene.Deck.BuildVisualStackAsync());

                UniTask shuffle = scene.Deck.ShuffleVisualAsync(ShuffleVisualType.Shake, 0.4f);
                await UniTask.Yield();
                await UniTask.Yield();
                CardComponent dealt = await Within(scene.Deck.DealToHandAsync(scene.Hand, true, 0f));
                await Within(shuffle);
                await UniTask.Yield();

                Assert.That(dealt.transform.parent, Is.SameAs(scene.Hand.transform));
                Assert.That(Vector3.Distance(dealt.transform.localPosition, Vector3.zero), Is.LessThan(0.0001f),
                    "The shake must not write stack-local offsets to a card that left the stack.");
                Assert.That(scene.Stack.Count, Is.EqualTo(3));
            });
        }

        [UnityTest]
        public IEnumerator ShuffleVisual_ResetDuringShake_DoesNotReportCompletion()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Fixture scene = new();
                scene.LoadCards("a", "b", "c");
                await Within(scene.Deck.BuildVisualStackAsync());
                int completed = 0;
                scene.Deck.OnShuffleVisualCompleted.AddListener(() => completed++);

                UniTask shuffle = scene.Deck.ShuffleVisualAsync(ShuffleVisualType.Shake, 0.5f);
                await UniTask.Yield();
                scene.Deck.Reset();
                await Within(shuffle);

                Assert.That(completed, Is.Zero);
                Assert.That(scene.Stack.Count, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator ShuffleVisual_NegativeDuration_CompletesInsteadOfThrowing()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Fixture scene = new();
                scene.LoadCards("a", "b", "c");
                await Within(scene.Deck.BuildVisualStackAsync());
                int completed = 0;
                scene.Deck.OnShuffleVisualCompleted.AddListener(() => completed++);

                await Within(scene.Deck.ShuffleVisualAsync(ShuffleVisualType.Cut, -1f));
                await Within(scene.Deck.ShuffleVisualAsync(ShuffleVisualType.Shake, -1f));

                Assert.That(completed, Is.EqualTo(2));
                Assert.That(scene.Stack.Count, Is.EqualTo(3));
                CollectionAssert.AreEqual(scene.Deck.Model.Cards, scene.Stack.Cards.Select(card => card.Data),
                    "The stack follows the shuffled model order.");
            });
        }

        private static async UniTask<T> Within<T>(UniTask<T> operation)
        {
            Task<T> task = operation.AsTask();
            await WaitFor(() => task.IsCompleted);
            return await task;
        }

        private static async UniTask Within(UniTask operation)
        {
            Task task = operation.AsTask();
            await WaitFor(() => task.IsCompleted);
            await task;
        }

        private static async UniTask WaitFor(Func<bool> predicate)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 4d;
            while (!predicate())
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Cards operation timed out.");
                await UniTask.Yield();
            }
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly float _originalTimeScale = Time.timeScale;
            private readonly GameObject _root = new("CardsLibraryPassFixture");
            private readonly DeckConfig _config = ScriptableObject.CreateInstance<DeckConfig>();
            internal readonly DeckComponent Deck;
            internal readonly HandComponent Hand;
            internal readonly BoardComponent Stack;

            internal Fixture()
            {
                Time.timeScale = 1f;
                try
                {
                    _root.SetActive(false);
                    Hand = Child("Hand").AddComponent<HandComponent>();
                    Hand.transform.localPosition = Vector3.right * 5f;
                    Set(Hand, "_arrangeDuration", 0f);
                    Set(Hand, "_layoutType", CardLayoutType.Line);
                    CardComponent prefab = Child("Prefab").AddComponent<CardComponent>();
                    Set(prefab, "_flipDuration", 0f);
                    Set(prefab, "_enableHoverEffect", false);
                    Stack = Child("Stack").AddComponent<BoardComponent>();
                    Set(Stack, "_autoGenerateSlots", false);
                    Set(Stack, "_layoutType", CardLayoutType.Stack);
                    Set(Stack, "_placeDuration", 0f);
                    Set(Stack, "_maxCards", 64);
                    Deck = Child("Deck").AddComponent<DeckComponent>();
                    Set(Deck, "_config", _config);
                    Set(Deck, "_cardPrefab", prefab);
                    Set(Deck, "_visualStackBoard", Stack);
                    Set(Deck, "_initializeOnStart", false);
                    Set(Deck, "_shuffleOnStart", false);
                    Set(Deck, "_spawnVisualOnInitialize", false);
                    Set(Deck, "_setAnimationConfigAsGlobal", false);
                    Set(Deck, "_stackPositionJitter", new Vector3(0.05f, 0.05f, 0f));
                    Deck.OnVisualStackChanged = new UnityEvent();
                    Deck.OnShuffleVisualCompleted = new UnityEvent();
                    _root.SetActive(true);
                    Deck.Initialize();
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal void LoadCards(params string[] ids)
            {
                CardData[] cards = ids.Select(id => CardData.CreateCustom(id)).ToArray();
                Deck.Model.Initialize(cards, false);
            }

            public void Dispose()
            {
                Time.timeScale = _originalTimeScale;
                foreach (CardComponent card in Object.FindObjectsByType<CardComponent>(FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (card != null && card.name.StartsWith("Prefab", StringComparison.Ordinal))
                    {
                        Object.Destroy(card.gameObject);
                    }
                }

                Object.Destroy(_root);
                Object.Destroy(_config);
            }

            private GameObject Child(string name)
            {
                GameObject child = new(name);
                child.transform.SetParent(_root.transform, false);
                return child;
            }
        }
    }
}
