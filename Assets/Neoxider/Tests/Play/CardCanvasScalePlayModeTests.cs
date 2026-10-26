using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Neo.Tests.Play
{
    public sealed class CardCanvasScalePlayModeTests
    {
        private static readonly Vector3 AuthoredScale = Vector3.one * 0.7f;
        private static readonly Vector2 AuthoredSize = new Vector2(380f, 554f);

        [UnityTest]
        public IEnumerator SmallCanvas_DrawStackAndTransfersKeepAuthoredSize()
        {
            return UiScaleScenario(0.258723944f, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator RotatedNonuniformCanvas_DrawStackAndTransfersKeepAuthoredSize()
        {
            return UiScaleScenario(1.5f, true).ToCoroutine();
        }

        [Test]
        public void WorldCard_AtCanvasSpawnRetainsUnparentedWorldPoseAndScale()
        {
            using FixtureScene scene = new FixtureScene(false, true, 1.5f, true);
            AssertLegacySpawn(scene);
        }

        [Test]
        public void RectCard_WithoutCanvasRetainsUnparentedWorldPoseAndScale()
        {
            using FixtureScene scene = new FixtureScene(true, false, 1.5f, true);
            AssertLegacySpawn(scene);
        }

        [UnityTest]
        public IEnumerator UiDraw_BirthResetOrInitializeAbortsOnlyTheStaleClone()
        {
            return BirthReentryScenario(false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator UiStack_BirthResetOrInitializeAbortsOnlyTheStaleClone()
        {
            return BirthReentryScenario(true).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator UiStack_ClearObserverUsesThePostDrawOrShuffleComposition()
        {
            return ClearMutationScenario().ToCoroutine();
        }

        [UnityTest]
        public IEnumerator UiStack_SameModelBirthMutationAndLogicalDrawResetDoNotPublishStaleCards()
        {
            return SameModelMutationScenario().ToCoroutine();
        }

        private static async UniTask UiScaleScenario(float canvasScale, bool nonuniform)
        {
            using FixtureScene scene = new FixtureScene(true, true, canvasScale, nonuniform);
            CardComponent reference = scene.CreateUiReference();
            int drawnEvents = 0;
            scene.Deck.OnCardDrawn.AddListener(card =>
            {
                drawnEvents++;
                Assert.That(card.transform.parent, Is.SameAs(scene.Spawn));
                AssertVector(card.transform.localPosition, Vector3.zero);
                AssertUiShape(card, reference);
            });

            CardComponent drawn = scene.Deck.DrawCard(false);
            Assert.That(drawn != null, Is.True);
            Assert.That(drawnEvents, Is.EqualTo(1));
            Assert.That(scene.Deck.RemainingCount, Is.EqualTo(2));
            await HoverRoundTrip(drawn);

            Assert.That(await Within(scene.Hand.TryAddCardAsync(drawn, false)), Is.True);
            Assert.That(scene.Hand.Count, Is.EqualTo(1));
            Assert.That(drawn.transform.parent, Is.SameAs(scene.Hand.transform));
            AssertUiShape(drawn, reference);
            await HoverRoundTrip(drawn);

            Assert.That(scene.Hand.DrawFirst(), Is.SameAs(drawn));
            await Within(scene.TargetBoard.PlaceCardAsync(drawn, false, false));
            Assert.That(scene.Hand.Count, Is.Zero);
            Assert.That(scene.TargetBoard.Count, Is.EqualTo(1));
            Assert.That(drawn.transform.parent, Is.SameAs(scene.TargetBoard.transform));
            AssertUiShape(drawn, reference);
            await HoverRoundTrip(drawn);

            CardData[] expectedStack = scene.Deck.Model.Cards.ToArray();
            await Within(scene.Deck.BuildVisualStackAsync());
            Assert.That(scene.StackBoard.Count, Is.EqualTo(2));
            CollectionAssert.AreEqual(expectedStack, scene.StackBoard.Cards.Select(card => card.Data).ToArray());
            foreach (CardComponent card in scene.StackBoard.Cards)
            {
                Assert.That(card.transform.parent, Is.SameAs(scene.StackBoard.transform));
                AssertUiShape(card, reference);
            }

            await HoverRoundTrip(scene.StackBoard.Cards[0]);
            AssertUiShape(scene.StackBoard.Cards[0], reference);
        }

        private static void AssertLegacySpawn(FixtureScene scene)
        {
            Vector3 position = scene.Spawn.position;
            Vector3 scale = scene.Prefab.transform.localScale;
            CardComponent card = scene.Deck.DrawCard(false);
            Assert.That(card != null, Is.True);
            Assert.That(card.transform.parent == null, Is.True);
            AssertVector(card.transform.position, position);
            AssertVector(card.transform.localScale, scale);
            Assert.That(Quaternion.Angle(card.transform.rotation, Quaternion.identity), Is.LessThan(0.001f));
            Assert.That(scene.Deck.RemainingCount, Is.EqualTo(2));
        }

        private static async UniTask BirthReentryScenario(bool buildStack)
        {
            for (int variant = 0; variant < 2; variant++)
            {
                bool reset = variant == 0;
                using FixtureScene scene = new FixtureScene(true, true, 0.258723944f, false);
                int callbacks = 0;
                int drawnEvents = 0;
                int builtEvents = 0;
                int visualEvents = 0;
                int placements = 0;
                GameObject staleObject = null;
                DeckModel replacementModel = null;
                CardData[] replacementCards = null;
                scene.Deck.OnCardDrawn.AddListener(card => drawnEvents++);
                scene.Deck.OnVisualStackBuilt.AddListener(() => builtEvents++);
                scene.Deck.OnVisualStackChanged.AddListener(() => visualEvents++);
                scene.StackBoard.OnCardPlaced = new UnityEvent<CardComponent>();
                scene.StackBoard.OnCardPlaced.AddListener(card => placements++);
                Audit20261023NeoCardsHierarchyObserver observer =
                    scene.Spawn.gameObject.AddComponent<Audit20261023NeoCardsHierarchyObserver>();
                observer.ArmChildrenChanged(() =>
                {
                    callbacks++;
                    Assert.That(scene.Spawn.childCount, Is.EqualTo(1), "Exercise the real UI birth-parent callback.");
                    staleObject = scene.Spawn.GetChild(0).gameObject;
                    if (reset)
                    {
                        scene.Deck.Reset();
                    }
                    else
                    {
                        scene.Deck.Initialize();
                    }

                    replacementModel = scene.Deck.Model;
                    replacementCards = replacementModel.Cards.ToArray();
                });

                if (buildStack)
                {
                    await Within(scene.Deck.BuildVisualStackAsync());
                }
                else
                {
                    Assert.That(scene.Deck.DrawCard(false) == null, Is.True);
                }

                Assert.That(callbacks, Is.EqualTo(1), "Reset and initialize must each run from a native birth callback.");
                Assert.That(replacementModel, Is.Not.Null);
                Assert.That(scene.Deck.Model, Is.SameAs(replacementModel));
                CollectionAssert.AreEqual(replacementCards, scene.Deck.Model.Cards);
                Assert.That(scene.StackBoard.Count, Is.Zero);
                Assert.That(drawnEvents + builtEvents + visualEvents + placements, Is.Zero);
                await NativeFrames(2);
                Assert.That(staleObject == null, Is.True, "The uncommitted old clone must actually be destroyed.");
                Assert.That(scene.OwnedCloneCount(), Is.Zero, "No detached UI birth clone may survive.");
                Assert.That(scene.Deck.Model, Is.SameAs(replacementModel));
                CollectionAssert.AreEqual(replacementCards, scene.Deck.Model.Cards);
                Assert.That(drawnEvents + builtEvents + visualEvents + placements, Is.Zero);

                // Positive control: the one-shot observer must not reject later ordinary admission.
                scene.Deck.Model.Initialize(new[] { CardData.CreateCustom("accepted-after-reentry") }, false);
                CardComponent accepted = scene.Deck.DrawCard(false);
                Assert.That(accepted != null, Is.True);
                Assert.That(drawnEvents, Is.EqualTo(1));
                Assert.That(await Within(scene.Hand.TryAddCardAsync(accepted, false)), Is.True);
                scene.Deck.Reset();
                await NativeFrames(2);
                Assert.That(accepted != null, Is.True, "Hand admission releases deck cleanup ownership.");
                Assert.That(scene.Hand.Cards[0], Is.SameAs(accepted));
                Assert.That(accepted.transform.parent, Is.SameAs(scene.Hand.transform));
                AssertVector(accepted.transform.localScale, AuthoredScale);
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(builtEvents + visualEvents + placements, Is.Zero);
            }
        }

        private static async UniTask ClearMutationScenario()
        {
            for (int variant = 0; variant < 2; variant++)
            {
                using FixtureScene scene = new FixtureScene(true, true, 1.5f, true);
                await Within(scene.StackBoard.PlaceCardAsync(scene.CreateUiReference(), false, false));
                DeckModel source = scene.Deck.Model;
                CardData[] expected = null;
                int clearCalls = 0;
                int built = 0;
                int changed = 0;
                scene.StackBoard.OnBoardCleared = new UnityEvent();
                scene.StackBoard.OnBoardCleared.AddListener(() =>
                {
                    clearCalls++;
                    if (variant == 0)
                    {
                        Assert.That(source.Draw().HasValue, Is.True);
                    }
                    else
                    {
                        Random.InitState(526);
                        source.Shuffle();
                    }

                    expected = source.Cards.ToArray();
                });
                scene.Deck.OnVisualStackBuilt.AddListener(() => built++);
                scene.Deck.OnVisualStackChanged.AddListener(() => changed++);

                await Within(scene.Deck.BuildVisualStackAsync());

                Assert.That(clearCalls, Is.EqualTo(1));
                Assert.That(scene.Deck.Model, Is.SameAs(source));
                Assert.That(expected, Is.Not.Null.And.Not.Empty);
                CollectionAssert.AreEqual(expected, scene.StackBoard.Cards.Select(card => card.Data).ToArray());
                CollectionAssert.AreEqual(expected, source.Cards);
                Assert.That(built, Is.EqualTo(1));
                Assert.That(changed, Is.EqualTo(1));
                await NativeFrames(2);
                Assert.That(scene.OwnedCloneCount(), Is.EqualTo(expected.Length));
            }
        }

        private static async UniTask SameModelMutationScenario()
        {
            for (int variant = 0; variant < 2; variant++)
            {
                using FixtureScene scene = new FixtureScene(true, true, 0.258723944f, false);
                DeckModel source = scene.Deck.Model;
                CardData[] expected = null;
                int callbacks = 0;
                int modelChanges = 0;
                int published = 0;
                GameObject stale = null;
                source.OnDeckChanged += () => modelChanges++;
                scene.Deck.OnCardDrawn.AddListener(card => published++);
                scene.Deck.OnVisualStackBuilt.AddListener(() => published++);
                scene.Deck.OnVisualStackChanged.AddListener(() => published++);
                Audit20261023NeoCardsHierarchyObserver observer =
                    scene.Spawn.gameObject.AddComponent<Audit20261023NeoCardsHierarchyObserver>();
                observer.ArmChildrenChanged(() =>
                {
                    callbacks++;
                    stale = scene.Spawn.GetChild(0).gameObject;
                    if (variant == 0) Assert.That(source.Draw().HasValue, Is.True);
                    else source.Shuffle();
                    expected = source.Cards.ToArray();
                });

                await Within(scene.Deck.BuildVisualStackAsync());

                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(modelChanges, Is.EqualTo(1), "The same model must actually mutate without replacement.");
                Assert.That(scene.Deck.Model, Is.SameAs(source));
                CollectionAssert.AreEqual(expected, source.Cards);
                Assert.That(scene.StackBoard.Count, Is.Zero);
                Assert.That(published, Is.Zero);
                await NativeFrames(2);
                Assert.That(stale == null, Is.True);
                Assert.That(scene.OwnedCloneCount(), Is.Zero);
                Assert.That(published, Is.Zero);
            }

            using FixtureScene logicalScene = new FixtureScene(true, true, 1.5f, true);
            DeckModel logicalSource = logicalScene.Deck.Model;
            CardData[] initial = logicalSource.Cards.ToArray();
            bool resetPending = true;
            int resets = 0;
            int draws = 0;
            logicalScene.Deck.OnCardDrawn.AddListener(card => draws++);
            logicalSource.OnDeckChanged += () =>
            {
                if (!resetPending) return;
                resetPending = false;
                resets++;
                logicalScene.Deck.Reset();
            };

            Assert.That(logicalScene.Deck.DrawCard(false) == null, Is.True);
            Assert.That(resets, Is.EqualTo(1));
            Assert.That(draws, Is.Zero);
            CollectionAssert.AreEqual(initial, logicalScene.Deck.Model.Cards);
            await NativeFrames(2);
            Assert.That(logicalScene.OwnedCloneCount(), Is.Zero);
            Assert.That(draws, Is.Zero);
        }

        private static void AssertUiShape(CardComponent card, CardComponent reference)
        {
            RectTransform rect = card.transform as RectTransform;
            RectTransform referenceRect = reference.transform as RectTransform;
            Assert.That(rect != null && referenceRect != null, Is.True);
            AssertVector(card.transform.localScale, AuthoredScale);
            Assert.That(Vector2.Distance(rect.rect.size, AuthoredSize), Is.LessThan(0.001f));
            Vector3[] corners = new Vector3[4];
            Vector3[] referenceCorners = new Vector3[4];
            rect.GetWorldCorners(corners);
            referenceRect.GetWorldCorners(referenceCorners);
            Assert.That(Vector3.Distance(corners[0], corners[3]),
                Is.EqualTo(Vector3.Distance(referenceCorners[0], referenceCorners[3])).Within(0.01f));
            Assert.That(Vector3.Distance(corners[0], corners[1]),
                Is.EqualTo(Vector3.Distance(referenceCorners[0], referenceCorners[1])).Within(0.01f));
        }

        private static async UniTask HoverRoundTrip(CardComponent card)
        {
            PointerEventData data = new PointerEventData(null);
            ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.pointerEnterHandler);
            await WaitForScale(card, AuthoredScale * 1.25f);
            ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.pointerExitHandler);
            await WaitForScale(card, AuthoredScale);
        }

        private static async UniTask WaitForScale(CardComponent card, Vector3 expected)
        {
            Stopwatch clock = Stopwatch.StartNew();
            while (card != null && Vector3.Distance(card.transform.localScale, expected) > 0.000001f &&
                   clock.Elapsed.TotalSeconds < 3d)
            {
                await UniTask.Yield();
            }

            // A following layout must capture the settled rest scale, not a near-target
            // value from an exit tween that is still running in this player-loop turn.
            await NativeFrames(1);
            Assert.That(card != null, Is.True);
            AssertVector(card.transform.localScale, expected);
        }

        private static async UniTask NativeFrames(int count)
        {
            int first = Time.frameCount;
            Stopwatch clock = Stopwatch.StartNew();
            while (Time.frameCount - first < count && clock.Elapsed.TotalSeconds < 3d)
            {
                await UniTask.Yield();
            }

            Assert.That(Time.frameCount - first, Is.GreaterThanOrEqualTo(count));
        }

        private static async UniTask Within(UniTask task)
        {
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Stopwatch clock = Stopwatch.StartNew();
            while (!awaiter.IsCompleted && clock.Elapsed.TotalSeconds < 3d) await UniTask.Yield();
            Assert.That(awaiter.IsCompleted, Is.True, "Owned card operation exceeded its unscaled bound.");
            awaiter.GetResult();
        }

        private static async UniTask<T> Within<T>(UniTask<T> task)
        {
            UniTask<T>.Awaiter awaiter = task.GetAwaiter();
            Stopwatch clock = Stopwatch.StartNew();
            while (!awaiter.IsCompleted && clock.Elapsed.TotalSeconds < 3d) await UniTask.Yield();
            Assert.That(awaiter.IsCompleted, Is.True, "Owned card admission exceeded its unscaled bound.");
            return awaiter.GetResult();
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.0001f));
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class FixtureScene : IDisposable
        {
            private readonly float _previousTimeScale = Time.timeScale;
            private readonly Random.State _previousRandom = Random.state;
            private readonly string _cloneName;
            internal readonly GameObject Root;
            internal readonly DeckConfig Config;
            internal readonly Canvas UiCanvas;
            internal readonly Transform Spawn;
            internal readonly CardComponent Prefab;
            internal readonly DeckComponent Deck;
            internal readonly HandComponent Hand;
            internal readonly BoardComponent StackBoard;
            internal readonly BoardComponent TargetBoard;

            internal FixtureScene(bool uiPrefab, bool canvasSpawn, float scale, bool nonuniform)
            {
                Assert.That(Application.isPlaying, Is.True, "This fixture requires native Play Mode lifecycle.");
                string prefix = "AuditCardCanvas_" + Guid.NewGuid().ToString("N");
                _cloneName = prefix + "_Prefab(Clone)";
                Root = new GameObject(prefix);
                Config = ScriptableObject.CreateInstance<DeckConfig>();
                Root.SetActive(false);
                Time.timeScale = 1f;
                try
                {
                    Transform container = Root.transform;
                    if (canvasSpawn)
                    {
                        GameObject canvasObject = Child(Root.transform, "Canvas", true);
                        UiCanvas = canvasObject.AddComponent<Canvas>();
                        UiCanvas.renderMode = RenderMode.WorldSpace;
                        RectTransform canvasRect = (RectTransform)canvasObject.transform;
                        canvasRect.sizeDelta = new Vector2(1200f, 1000f);
                        canvasRect.localScale = new Vector3(scale, nonuniform ? scale * 1.25f : scale, scale);
                        canvasRect.localRotation = Quaternion.Euler(0f, 0f, 17f);
                        container = canvasRect;
                    }

                    Spawn = Child(container, "Spawn", canvasSpawn).transform;
                    Spawn.localPosition = new Vector3(-200f, 40f, 0f);
                    if (canvasSpawn)
                    {
                        ((RectTransform)Spawn).pivot = new Vector2(0f, 0.5f);
                    }
                    else
                    {
                        Spawn.localRotation = Quaternion.Euler(13f, 27f, 7f);
                        Spawn.localScale = new Vector3(2f, 0.5f, 1.3f);
                    }

                    Hand = Child(container, "Hand", true).AddComponent<HandComponent>();
                    SetField(Hand, "_arrangeDuration", 0f);
                    StackBoard = CreateBoard(container, "StackBoard");
                    TargetBoard = CreateBoard(container, "TargetBoard");
                    GameObject prefabObject = Child(Root.transform, prefix + "_Prefab", uiPrefab);
                    prefabObject.transform.localScale = uiPrefab ? AuthoredScale : new Vector3(0.7f, 0.9f, 1.1f);
                    Image image = null;
                    if (uiPrefab)
                    {
                        ((RectTransform)prefabObject.transform).sizeDelta = AuthoredSize;
                        image = prefabObject.AddComponent<Image>();
                    }

                    Prefab = prefabObject.AddComponent<CardComponent>();
                    SetField(Prefab, "_cardImage", image);
                    SetField(Prefab, "_hoverScale", 0.25f);
                    SetField(Prefab, "_hoverYOffset", 0f);
                    SetField(Prefab, "_hoverDuration", 0.01f);
                    Deck = Child(Root.transform, "Deck", false).AddComponent<DeckComponent>();
                    SetField(Deck, "_config", Config);
                    SetField(Deck, "_cardPrefab", Prefab);
                    SetField(Deck, "_spawnPoint", Spawn);
                    SetField(Deck, "_visualStackBoard", StackBoard);
                    SetField(Deck, "_initializeOnStart", false);
                    SetField(Deck, "_shuffleOnStart", false);
                    SetField(Deck, "_spawnVisualOnInitialize", false);
                    SetField(Deck, "_setAnimationConfigAsGlobal", false);
                    // Geometry is compared at the same local rotation: decorative stack jitter
                    // legitimately changes world edge lengths under a nonuniform ancestor.
                    SetField(Deck, "_stackRotationJitter", Vector3.zero);
                    SetField(Deck, "_stackPositionJitter", Vector3.zero);
                    Deck.OnCardDrawn = new UnityEvent<CardComponent>();
                    Deck.OnVisualStackBuilt = new UnityEvent();
                    Deck.OnVisualStackChanged = new UnityEvent();
                    Root.SetActive(true);
                    Deck.Initialize();
                    Deck.Model.Initialize(new[]
                    {
                        CardData.CreateCustom("first"), CardData.CreateCustom("second"), CardData.CreateCustom("third")
                    }, false);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal CardComponent CreateUiReference()
            {
                CardComponent card = Object.Instantiate(Prefab, UiCanvas.transform, false);
                card.transform.localPosition = Vector3.zero;
                card.UpdateOriginalTransform();
                return card;
            }

            internal int OwnedCloneCount()
            {
                return Resources.FindObjectsOfTypeAll<CardComponent>().Count(card =>
                    card != null && card.gameObject.scene.IsValid() && card.gameObject.name == _cloneName);
            }

            public void Dispose()
            {
                try
                {
                    if (Root != null)
                    {
                        foreach (Audit20261023NeoCardsHierarchyObserver observer in
                                 Root.GetComponentsInChildren<Audit20261023NeoCardsHierarchyObserver>(true))
                            observer.Disarm();
                    }

                    foreach (CardComponent card in Resources.FindObjectsOfTypeAll<CardComponent>())
                    {
                        if (card != null && card.gameObject.scene.IsValid() && card.gameObject.name == _cloneName)
                            Object.DestroyImmediate(card.gameObject);
                    }

                    if (Root != null) Object.DestroyImmediate(Root);
                    if (Config != null) Object.DestroyImmediate(Config);
                }
                finally
                {
                    Time.timeScale = _previousTimeScale;
                    Random.state = _previousRandom;
                }
            }

            private static BoardComponent CreateBoard(Transform parent, string name)
            {
                BoardComponent board = Child(parent, name, true).AddComponent<BoardComponent>();
                SetField(board, "_autoGenerateSlots", false);
                SetField(board, "_layoutType", CardLayoutType.Line);
                SetField(board, "_placeDuration", 0f);
                return board;
            }

            private static GameObject Child(Transform parent, string name, bool ui)
            {
                GameObject child = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                child.transform.SetParent(parent, false);
                return child;
            }
        }
    }
}
