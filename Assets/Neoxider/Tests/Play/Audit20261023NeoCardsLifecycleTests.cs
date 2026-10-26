using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    public sealed class Audit20261023NeoCardsLifecycleTests
    {
        [UnityTest]
        public IEnumerator CompetingLogicalDeals_LastSlot_ConserveCardsAndPublishOneSuccess()
        {
            return CompetingDeals(false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator CompetingVisualDeals_LastSlot_RestoreRejectedCardToTheStack()
        {
            return CompetingDeals(true).ToCoroutine();
        }

        private static async UniTask CompetingDeals(bool visual)
        {
            using Scene scene = new();
            scene.Deck.Initialize();
            CardData[] original = { CardData.CreateCustom("a"), CardData.CreateCustom("b"), CardData.CreateCustom("c") };
            scene.Deck.Model.Initialize(original, false);
            Set(scene.Player, "_maxCards", 1);
            if (visual)
            {
                Set(scene.Deck, "_visualStackBoard", scene.Board);
                await scene.Deck.BuildVisualStackAsync();
            }

            List<CardComponent> drawn = new();
            scene.Deck.OnCardDrawn = new UnityEvent<CardComponent>();
            scene.Deck.OnCardDrawn.AddListener(drawn.Add);
            int successes = 0;
            scene.Deck.OnCardDealt = new UnityEvent<CardComponent, HandComponent>();
            scene.Deck.OnCardDealt.AddListener((card, hand) => successes++);

            UniTask<CardComponent> first = scene.Deck.DealToHandAsync(scene.Player, true, 0.12f);
            UniTask<CardComponent> second = scene.Deck.DealToHandAsync(scene.Player, true, 0.12f);
            CardComponent[] results = await Within(UniTask.WhenAll(new[] { first, second }));

            Assert.That(results.Count(card => card != null), Is.EqualTo(1));
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(scene.Player.Count, Is.EqualTo(1));
            Assert.That(scene.Deck.RemainingCount, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(original, scene.Deck.Model.Cards.Concat(scene.Player.Model.Cards));
            await UniTask.Yield();
            if (visual)
            {
                Assert.That(scene.Board.Count, Is.EqualTo(2));
                CollectionAssert.AreEqual(scene.Deck.Model.Cards, scene.Board.Cards.Select(card => card.Data));
            }
            else
            {
                Assert.That(drawn.Count(card => card != null), Is.EqualTo(1), "Rejected detached visuals are released.");
            }

            CardComponent accepted = scene.Player.Cards[0];
            scene.Deck.Reset();
            await UniTask.Yield();
            Assert.That(accepted != null, Is.True, "Accepted transfers belong to the hand, not the deck's reset cleanup.");
        }

        [UnityTest]
        public IEnumerator ReinitializeDuringDeal_DoesNotRollbackIntoReplacementDeck()
        {
            return ReplaceSourceDuringDeal(false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator ResetDuringDeal_DoesNotDuplicateTheResetComposition()
        {
            return ReplaceSourceDuringDeal(true).ToCoroutine();
        }

        private static async UniTask ReplaceSourceDuringDeal(bool reset)
        {
            using Scene scene = new();
            scene.Deck.Initialize();
            scene.Deck.Model.Initialize(new[] { CardData.CreateCustom("a"), CardData.CreateCustom("b") }, false);
            List<CardComponent> drawn = new();
            scene.Deck.OnCardDrawn = new UnityEvent<CardComponent>();
            scene.Deck.OnCardDrawn.AddListener(drawn.Add);
            int successes = 0;
            scene.Deck.OnCardDealt = new UnityEvent<CardComponent, HandComponent>();
            scene.Deck.OnCardDealt.AddListener((card, hand) => successes++);
            UniTask<CardComponent> pending = scene.Deck.DealToHandAsync(scene.Player, true, 1f);
            await UniTask.Yield();

            if (reset)
            {
                scene.Deck.Reset();
            }
            else
            {
                scene.Deck.Initialize();
            }

            CardData[] replacement = scene.Deck.Model.Cards.ToArray();
            Assert.That(await Within(pending), Is.Null);
            await UniTask.Yield();
            CollectionAssert.AreEqual(replacement, scene.Deck.Model.Cards);
            Assert.That(scene.Player.Count, Is.Zero);
            Assert.That(successes, Is.Zero);
            Assert.That(drawn.All(card => card == null), Is.True);
        }

        [UnityTest]
        public IEnumerator BoardLayout_AddRemoveAndClearDuringMotion_KeepCurrentOrderAndRelinquishTransforms()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                BoardComponent board = scene.Board;
                Set(board, "_placeDuration", 0.12f);
                for (int i = 0; i < 3; i++)
                {
                    await board.PlaceCardAsync(scene.Card("Initial" + i), false, false);
                }

                UniTask pendingAdd = board.PlaceCardAsync(scene.Card("Fourth"), true, false);
                await UniTask.Yield();
                await board.PlaceCardAsync(scene.Card("Fifth"), false, false);
                await Within(pendingAdd);
                AssertBoardLine(board);

                UniTask pendingRemove = board.PlaceCardAsync(scene.Card("Sixth"), true, false);
                await UniTask.Yield();
                CardComponent transferred = board.Cards[0];
                Assert.That(board.RemoveCard(transferred), Is.True);
                Assert.That(await scene.Player.TryAddCardAsync(transferred, false), Is.True);
                Vector3 position = transferred.transform.position;
                Quaternion rotation = transferred.transform.rotation;
                await Within(pendingRemove);
                Assert.That(transferred.transform.position, Is.EqualTo(position));
                Assert.That(transferred.transform.rotation, Is.EqualTo(rotation));
                AssertBoardLine(board);

                int placements = 0;
                board.OnCardPlaced = new UnityEvent<CardComponent>();
                board.OnCardPlaced.AddListener(card => placements++);
                UniTask pendingClear = board.PlaceCardAsync(scene.Card("Seventh"), true, false);
                await UniTask.Yield();
                List<CardComponent> released = board.ClearAndReturn();
                for (int i = 0; i < released.Count; i++)
                {
                    released[i].transform.SetParent(scene.Root.transform, true);
                    released[i].transform.position = new Vector3(1000f + i, 800f, 0f);
                    released[i].transform.rotation = Quaternion.Euler(0f, 0f, 47f);
                }

                await Within(pendingClear);
                await Quiesce(180);
                Assert.That(board.Count, Is.Zero);
                Assert.That(placements, Is.Zero);
                for (int i = 0; i < released.Count; i++)
                {
                    Assert.That(released[i].transform.position, Is.EqualTo(new Vector3(1000f + i, 800f, 0f)));
                    Assert.That(Quaternion.Angle(released[i].transform.rotation, Quaternion.Euler(0f, 0f, 47f)),
                        Is.LessThan(0.001f));
                }
            });
        }

        [UnityTest]
        public IEnumerator HandLayout_TransferredOrClearedCard_IsNotMovedOrNotifiedByOldAdmission()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                await scene.Player.AddCardAsync(scene.Card("Existing"), false);
                Set(scene.Player, "_arrangeDuration", 0.2f);
                CardComponent moving = scene.Card("Moving");
                int staleAdditions = 0;
                scene.Player.OnCardAdded.AddListener(card => staleAdditions++);
                UniTask<bool> pending = scene.Player.TryAddCardAsync(moving, true);
                await UniTask.Yield();
                scene.Player.RemoveCard(moving);
                await scene.Opponent.AddCardAsync(moving, false);
                Vector3 destination = moving.transform.position;

                Assert.That(await Within(pending), Is.True, "Admission was committed before the transfer.");
                await Quiesce(240);
                Assert.That(moving.transform.parent, Is.SameAs(scene.Opponent.transform));
                Assert.That(moving.transform.position, Is.EqualTo(destination));
                Assert.That(staleAdditions, Is.Zero);

                UniTask<bool> clearedAdmission = scene.Player.TryAddCardAsync(scene.Card("Cleared"), true);
                await UniTask.Yield();
                scene.Player.Clear();
                Assert.That(await Within(clearedAdmission), Is.True);
                Assert.That(scene.Player.Count, Is.Zero);
                Assert.That(scene.Player.Model.Count, Is.Zero);
                Assert.That(staleAdditions, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator HandViewLayout_RemoveAndClear_StopMotionBeforeRelinquishingViews()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                HandView hand = Child(scene.Root, "HandView").AddComponent<HandView>();
                Set(hand, "_arrangeDuration", 0.2f);
                Set(hand, "_layoutType", CardLayoutType.Line);
                CardView first = Child(scene.Root, "FirstView").AddComponent<CardView>();
                CardView second = Child(scene.Root, "SecondView").AddComponent<CardView>();
                CardView third = Child(scene.Root, "ThirdView").AddComponent<CardView>();
                await hand.AddCardAsync(first, false);
                await hand.AddCardAsync(second, false);
                UniTask pending = hand.AddCardAsync(third, true);
                await UniTask.Yield();
                await hand.RemoveCardAsync(first, false);
                first.transform.SetParent(scene.Root.transform, true);
                first.transform.position = Vector3.one * 900f;
                await Within(pending);
                Assert.That(first.transform.position, Is.EqualTo(Vector3.one * 900f));

                UniTask pendingClear = hand.ArrangeCardsAsync(true);
                await UniTask.Yield();
                hand.Clear();
                second.transform.SetParent(scene.Root.transform, true);
                third.transform.SetParent(scene.Root.transform, true);
                second.transform.position = Vector3.one * 700f;
                third.transform.position = Vector3.one * 800f;
                await Within(pendingClear);
                await Quiesce(240);
                Assert.That(hand.Count, Is.Zero);
                Assert.That(second.transform.position, Is.EqualTo(Vector3.one * 700f));
                Assert.That(third.transform.position, Is.EqualTo(Vector3.one * 800f));
                Assert.That(first.transform.position, Is.EqualTo(Vector3.one * 900f));
            });
        }

        [UnityTest]
        public IEnumerator RestartDuringTurnDelay_OldFinallyCannotStopTheReplacementRound()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                scene.SeedHands(3);
                Set(scene.Game, "_turnDelay", 1f);
                UniTask oldRound = scene.Game.PlayRound();
                Assert.That(scene.Game.IsPlaying, Is.True);
                scene.Game.RestartGame();
                await WaitFor(() => scene.Restarts == 1);
                Set(scene.Game, "_turnDelay", 0.8f);
                UniTask newRound = scene.Game.PlayRound();

                await Within(oldRound);

                Assert.That(scene.Game.IsPlaying, Is.True);
                Assert.That(scene.RoundEnds, Is.Zero);
                Assert.That(scene.Winners, Is.Zero);
                scene.Game.RestartGame();
                await Within(newRound);
                Assert.That(scene.Game.PlayerCardCount + scene.Game.OpponentCardCount, Is.EqualTo(54));
                Assert.That(scene.Game.IsPlaying, Is.False);
                Assert.That(scene.RoundEnds, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator RestartDuringFlip_ReleasesOldViewsAndLeavesOnlyTheNewDeal()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                scene.SeedHands(3);
                Set(scene.Prefab, "_flipDuration", 0.4f);
                UniTask round = scene.Game.PlayRound();
                CardComponent[] oldViews = scene.Table.GetComponentsInChildren<CardComponent>();
                Assert.That(oldViews.Length, Is.EqualTo(1));
                await UniTask.Yield();
                scene.Game.RestartGame();
                await Within(round);
                await UniTask.Yield();

                Assert.That(oldViews.All(card => card == null), Is.True);
                Assert.That(scene.Table.GetComponentsInChildren<CardComponent>(), Is.Empty);
                Assert.That(scene.Game.PlayerCardCount, Is.EqualTo(27));
                Assert.That(scene.Game.OpponentCardCount, Is.EqualTo(27));
                Assert.That(scene.RoundEnds, Is.Zero);
                Assert.That(scene.Winners, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator RestartDuringWarDelay_CleansEveryWarViewAndSuppressesOldCompletion()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                scene.SeedHands(8);
                Set(scene.Game, "_warContinueDelay", 1f);
                UniTask round = scene.Game.PlayRound();
                await WaitFor(() => scene.Wars == 1 && scene.Table.GetComponentsInChildren<CardComponent>().Length == 4);
                CardComponent[] oldViews = scene.Table.GetComponentsInChildren<CardComponent>();
                scene.Game.RestartGame();
                await Within(round);
                await UniTask.Yield();

                Assert.That(oldViews.All(card => card == null), Is.True);
                Assert.That(scene.Table.GetComponentsInChildren<CardComponent>(), Is.Empty);
                Assert.That(scene.Game.PlayerCardCount + scene.Game.OpponentCardCount, Is.EqualTo(54));
                Assert.That(scene.WarEnds, Is.Zero);
                Assert.That(scene.RoundEnds, Is.Zero);
                Assert.That(scene.Winners, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator RoundStartedListener_RestartsWithoutDrawingFromTheNewGame()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                scene.SeedHands(1);
                scene.Game.OnRoundStarted.AddListener(scene.Game.RestartGame);

                await Within(scene.Game.PlayRound());

                Assert.That(scene.Restarts, Is.EqualTo(1));
                Assert.That(scene.Game.PlayerCardCount, Is.EqualTo(27));
                Assert.That(scene.Game.OpponentCardCount, Is.EqualTo(27));
                Assert.That(scene.Game.GameStarted, Is.False);
                Assert.That(scene.Game.IsPlaying, Is.False);
                Assert.That(scene.Winners, Is.Zero);
                Assert.That(scene.RoundEnds, Is.Zero);
            });
        }

        [UnityTest]
        public IEnumerator CardComponent_CancelledFlipAndSupersededMove_KeepOnlyTheNewCompletion()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                CardComponent card = scene.Card("Cancellable");
                int flips = 0;
                int moves = 0;
                card.OnFlip = new UnityEvent();
                card.OnFlip.AddListener(() => flips++);
                card.OnMoveComplete = new UnityEvent();
                card.OnMoveComplete.AddListener(() => moves++);
                using CancellationTokenSource cancellation = new();
                UniTask<bool> flip = card.FlipAsync(0.4f, cancellation.Token).SuppressCancellationThrow();
                await WaitFor(() => card.transform.localScale.x < 0.98f);
                cancellation.Cancel();
                Assert.That(await Within(flip), Is.True);
                Assert.That(card.IsFaceUp, Is.True);
                Assert.That(card.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(flips, Is.Zero);

                UniTask<bool> first = card.MoveToAsync(Vector3.right * 100f, 1f).SuppressCancellationThrow();
                await UniTask.Yield();
                UniTask second = card.MoveToAsync(Vector3.up * 80f, 0.08f);
                Assert.That(await Within(first), Is.True);
                await Within(second);
                Assert.That(Vector3.Distance(card.transform.position, Vector3.up * 80f), Is.LessThan(0.001f));
                Assert.That(moves, Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator ConcreteViews_CallerCancellationAndReplacement_StopTheirOwnTweens()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                foreach (bool universal in new[] { false, true })
                {
                    GameObject root = Child(scene.Root, "ConcreteView");
                    CardView ordinary = universal ? null : root.AddComponent<CardView>();
                    CardViewUniversal enhanced = universal ? root.AddComponent<CardViewUniversal>() : null;
                    ICardView view = universal ? (ICardView)enhanced : ordinary;
                    using CancellationTokenSource cancellation = new();
                    UniTask<bool> flip = (universal ? enhanced.FlipAsync(0.4f, cancellation.Token) :
                        ordinary.FlipAsync(0.4f, cancellation.Token)).SuppressCancellationThrow();
                    await WaitFor(() => root.transform.localScale.x < 0.98f);
                    cancellation.Cancel();
                    Assert.That(await Within(flip), Is.True);
                    Assert.That(view.IsFaceUp, Is.True);
                    Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));

                    UniTask<bool> first = view.MoveToAsync(Vector3.right * 100f, 1f).SuppressCancellationThrow();
                    await UniTask.Yield();
                    UniTask second = view.MoveToAsync(Vector3.up * 70f, 0.08f);
                    Assert.That(await Within(first), Is.True);
                    await Within(second);
                    Assert.That(Vector3.Distance(root.transform.position, Vector3.up * 70f), Is.LessThan(0.001f));
                }
            });
        }

        [UnityTest]
        public IEnumerator UniversalOneShot_DestructionCompletesItsOwnedWaitWithoutLateTransformAccess()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using Scene scene = new();
                CardViewUniversal view = Child(scene.Root, "OneShot").AddComponent<CardViewUniversal>();
                UniTask pending = view.PlayOneShotAsync(CardViewAnimationType.Pulse, 1f);
                await UniTask.Yield();

                Object.Destroy(view.gameObject);

                await Within(pending);
                Assert.That(view == null, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator PausedEntry_FixtureRunsNativePartialFlipAndRestoresTheBorrowedTimeScale()
        {
            return UniTask.ToCoroutine(async () =>
            {
                float originalTimeScale = Time.timeScale;
                try
                {
                    Time.timeScale = 0f;
                    using (Scene scene = new())
                    {
                        Assert.That(Time.timeScale, Is.EqualTo(1f));
                        CardComponent card = scene.Card("PausedEntry");
                        int flips = 0;
                        card.OnFlip = new UnityEvent();
                        card.OnFlip.AddListener(() => flips++);
                        UniTask pending = card.FlipAsync(0.6f);
                        await WaitFor(() => card.transform.localScale.x > 0f && card.transform.localScale.x < 0.98f);
                        Assert.That(pending.GetAwaiter().IsCompleted, Is.False, "The native flip must really be in progress.");
                        Assert.That(flips, Is.Zero);
                        await Within(pending);
                        Assert.That(card.IsFaceUp, Is.False);
                        Assert.That(card.transform.localScale, Is.EqualTo(Vector3.one));
                        Assert.That(flips, Is.EqualTo(1));
                    }

                    Assert.That(Time.timeScale, Is.Zero, "Scene disposal restores its captured paused entry state.");
                    await Quiesce(60);
                    Assert.That(Time.timeScale, Is.Zero, "Quiescence must finish without changing paused time.");
                }
                finally
                {
                    Time.timeScale = originalTimeScale;
                }

                Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));
            });
        }

        [UnityTest]
        public IEnumerator DealDrawObserver_AdmitsThenResets_AcceptedCardSurvivesNativeCleanup()
        {
            return ObserverHandoff(true, false, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator DealDrawObserver_AdmitsThenReinitializes_AcceptedCardSurvivesNativeCleanup()
        {
            return ObserverHandoff(false, false, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator DirectDraw_AdmissionEventResetsBeforeReturn_AcceptedCardSurvivesNativeCleanup()
        {
            return ObserverHandoff(true, true, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator Deal_ModelAdmissionResetsBeforeHandCallReturns_AcceptedCardSurvivesNativeCleanup()
        {
            return ObserverHandoff(true, false, true).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator NativeParentChanged_ResetAfterRegularAdmission_PreservesAcceptedCard()
        {
            return NativeHierarchyHandoff(true, true, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator NativeParentChanged_ReinitializeAfterBottomAdmission_PreservesAcceptedCard()
        {
            return NativeHierarchyHandoff(true, false, true).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator NativeChildrenChanged_ResetAfterBottomAdmission_PreservesAcceptedCard()
        {
            return NativeHierarchyHandoff(false, true, true).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator NativeChildrenChanged_ReinitializeAfterRegularAdmission_PreservesAcceptedCard()
        {
            return NativeHierarchyHandoff(false, false, false).ToCoroutine();
        }

        private static async UniTask NativeHierarchyHandoff(bool parentObserver, bool reset, bool bottom)
        {
            using Scene scene = new();
            CardComponent existing = scene.Card("ExistingHandMember");
            await scene.Player.AddCardAsync(existing, false);
            Set(scene.Player, "_addToBottom", bottom);
            scene.Deck.Initialize();
            CardData[] original = { CardData.CreateCustom("first"), CardData.CreateCustom("accepted") };
            scene.Deck.Model.Initialize(original, false);
            HandModel acceptedModel = scene.Player.Model;
            CardComponent accepted = null;
            CardComponent[] expectedMembers = null;
            CardData[] expectedData = null;
            Audit20261023NeoCardsHierarchyObserver observer = null;
            int hierarchyCallbacks = 0;
            int modelAdmissions = 0;
            int admissions = 0;
            int drawnEvents = 0;
            int dealtEvents = 0;
            int visualEvents = 0;
            int clicks = 0;
            bool armNextDraw = true;
            scene.Deck.OnCardDrawn = new UnityEvent<CardComponent>();
            scene.Deck.OnCardDealt = new UnityEvent<CardComponent, HandComponent>();
            scene.Deck.OnVisualStackChanged = new UnityEvent();
            scene.Deck.OnCardDealt.AddListener((card, hand) => dealtEvents++);
            scene.Deck.OnVisualStackChanged.AddListener(() => visualEvents++);
            scene.Player.OnCardClicked.AddListener(card => clicks++);
            scene.Player.OnCardAdded.AddListener(card => admissions++);
            scene.Player.Model.OnCardAdded += data =>
            {
                modelAdmissions++;
                if (data.Equals(original[1]) && modelAdmissions == 1)
                {
                    Assert.That(accepted.transform.parent, Is.SameAs(scene.Player.transform),
                        "Public model observers still see the accepted card's final parent.");
                    CollectionAssert.AreEqual(expectedMembers, scene.Player.Cards);
                    CollectionAssert.AreEqual(expectedData, scene.Player.Model.Cards);
                }
            };
            Action hierarchyChanged = () =>
            {
                hierarchyCallbacks++;
                Assert.That(accepted != null, Is.True);
                Assert.That(accepted.transform.parent, Is.SameAs(scene.Player.transform));
                Assert.That(scene.Player.Model, Is.SameAs(acceptedModel));
                CollectionAssert.AreEqual(expectedMembers, scene.Player.Cards,
                    "Native hierarchy observers must see committed component membership.");
                CollectionAssert.AreEqual(expectedData, scene.Player.Model.Cards,
                    "Native hierarchy observers must see actual accepted model data.");
                Assert.That(modelAdmissions, Is.Zero, "Parenting precedes public model notifications.");
                if (reset)
                {
                    scene.Deck.Reset();
                }
                else
                {
                    scene.Deck.Initialize();
                }
            };
            scene.Deck.OnCardDrawn.AddListener(card =>
            {
                drawnEvents++;
                card.OnClick ??= new UnityEvent();
                if (!armNextDraw)
                {
                    return;
                }

                armNextDraw = false;
                accepted = card;
                expectedMembers = bottom ? new[] { accepted, existing } : new[] { existing, accepted };
                expectedData = expectedMembers.Select(member => member.Data).ToArray();
                if (parentObserver)
                {
                    observer = card.gameObject.AddComponent<Audit20261023NeoCardsHierarchyObserver>();
                    observer.ArmParentChanged(hierarchyChanged);
                }
            });
            if (!parentObserver)
            {
                observer = scene.Player.gameObject.AddComponent<Audit20261023NeoCardsHierarchyObserver>();
                observer.ArmChildrenChanged(hierarchyChanged);
            }

            try
            {
                CardComponent result = await Within(scene.Deck.DealToHandAsync(scene.Player, true, 0f));

                Assert.That(result, Is.Null, "The replaced source cannot publish old-generation success.");
                Assert.That(hierarchyCallbacks, Is.EqualTo(1), "The armed native message must really run once.");
                Assert.That(modelAdmissions, Is.EqualTo(1));
                Assert.That(admissions, Is.EqualTo(1));
                Assert.That(drawnEvents, Is.EqualTo(1));
                Assert.That(dealtEvents, Is.Zero);
                Assert.That(visualEvents, Is.Zero, "There must be no obsolete deal/rollback notification tail.");
                CardData[] replacement = reset ? original : scene.Config.GenerateDeck().ToArray();
                await Quiesce(120);
                await UniTask.Yield();

                Assert.That(accepted != null, Is.True,
                    "Cleanup release is proved by real survival after deferred destruction, not an ownership flag.");
                Assert.That(accepted.transform.parent, Is.SameAs(scene.Player.transform));
                Assert.That(scene.Player.Model, Is.SameAs(acceptedModel));
                CollectionAssert.AreEqual(expectedMembers, scene.Player.Cards);
                CollectionAssert.AreEqual(expectedData, scene.Player.Model.Cards);
                CollectionAssert.AreEqual(replacement, scene.Deck.Model.Cards);
                Vector3 settledPosition = accepted.transform.position;
                Quaternion settledRotation = accepted.transform.rotation;
                await Quiesce(160);
                Assert.That(accepted.transform.position, Is.EqualTo(settledPosition));
                Assert.That(Quaternion.Angle(accepted.transform.rotation, settledRotation), Is.LessThan(0.001f));
                CollectionAssert.AreEqual(replacement, scene.Deck.Model.Cards);
                Assert.That(hierarchyCallbacks, Is.EqualTo(1));
                Assert.That(modelAdmissions, Is.EqualTo(1));
                Assert.That(admissions, Is.EqualTo(1));
                Assert.That(drawnEvents, Is.EqualTo(1));
                Assert.That(dealtEvents, Is.Zero);
                Assert.That(visualEvents, Is.Zero);
                accepted.OnClick.Invoke();
                Assert.That(clicks, Is.EqualTo(1), "Accepted click ownership is installed once.");

                CardComponent ordinary = await Within(scene.Deck.DealToHandAsync(scene.Player, true, 0f));
                Assert.That(ordinary != null, Is.True, "An ordinary later deal must still succeed.");
                CardComponent[] ordinaryMembers = bottom ? new[] { ordinary, accepted, existing } :
                    new[] { existing, accepted, ordinary };
                CollectionAssert.AreEqual(ordinaryMembers, scene.Player.Cards);
                CollectionAssert.AreEqual(ordinaryMembers.Select(card => card.Data), scene.Player.Model.Cards);
                Assert.That(ordinary.transform.parent, Is.SameAs(scene.Player.transform));
                Assert.That(hierarchyCallbacks, Is.EqualTo(1), "Later hierarchy changes do not replay the observer.");
                Assert.That(modelAdmissions, Is.EqualTo(2));
                Assert.That(admissions, Is.EqualTo(2));
                Assert.That(drawnEvents, Is.EqualTo(2));
                Assert.That(dealtEvents, Is.EqualTo(1));
                Assert.That(visualEvents, Is.EqualTo(1));
                ordinary.OnClick.Invoke();
                Assert.That(clicks, Is.EqualTo(2));

                Set(scene.Player, "_maxCards", scene.Player.Count);
                CardData[] beforeRejection = scene.Deck.Model.Cards.ToArray();
                Assert.That(await Within(scene.Deck.DealToHandAsync(scene.Player, true, 0f)), Is.Null);
                await Quiesce(80);
                CollectionAssert.AreEqual(beforeRejection, scene.Deck.Model.Cards);
                CollectionAssert.AreEqual(ordinaryMembers, scene.Player.Cards);
                CollectionAssert.AreEqual(ordinaryMembers.Select(card => card.Data), scene.Player.Model.Cards);
                Assert.That(accepted != null && ordinary != null, Is.True);
                Assert.That(hierarchyCallbacks, Is.EqualTo(1));
                Assert.That(modelAdmissions, Is.EqualTo(2));
                Assert.That(admissions, Is.EqualTo(2));
                Assert.That(drawnEvents, Is.EqualTo(2));
                Assert.That(dealtEvents, Is.EqualTo(1));
                Assert.That(visualEvents, Is.EqualTo(1));
            }
            finally
            {
                if (observer != null)
                {
                    observer.Disarm();
                }
            }
        }

        private static async UniTask ObserverHandoff(bool reset, bool directDraw, bool modelObserver)
        {
            using Scene scene = new();
            scene.Deck.Initialize();
            CardData[] original = { CardData.CreateCustom("first"), CardData.CreateCustom("accepted") };
            scene.Deck.Model.Initialize(original, false);
            Set(scene.Player, "_addToBottom", !reset || directDraw);
            CardComponent accepted = null;
            HandModel acceptedModel = scene.Player.Model;
            int dealtEvents = 0;
            int drawnEvents = 0;
            int admissions = 0;
            int replacements = 0;
            scene.Deck.OnCardDealt = new UnityEvent<CardComponent, HandComponent>();
            scene.Deck.OnCardDealt.AddListener((card, hand) => dealtEvents++);
            scene.Player.OnCardAdded.AddListener(card => admissions++);
            Action replace = () =>
            {
                replacements++;
                if (reset)
                {
                    scene.Deck.Reset();
                }
                else
                {
                    scene.Deck.Initialize();
                }
            };
            if (modelObserver)
            {
                scene.Player.Model.OnCardAdded += data => replace();
            }
            else if (directDraw)
            {
                scene.Player.OnCardAdded.AddListener(card => replace());
            }

            scene.Deck.OnCardDrawn = new UnityEvent<CardComponent>();
            scene.Deck.OnCardDrawn.AddListener(card =>
            {
                drawnEvents++;
                accepted = card;
                if (modelObserver)
                {
                    return;
                }

                if (directDraw)
                {
                    scene.Player.AddCard(card);
                }
                else
                {
                    UniTask<bool> admission = scene.Player.TryAddCardAsync(card, false);
                    Assert.That(admission.GetAwaiter().IsCompleted, Is.True);
                    Assert.That(admission.GetAwaiter().GetResult(), Is.True);
                    replace();
                }
            });

            CardComponent result = directDraw ? scene.Deck.DrawCard(true) :
                await Within(scene.Deck.DealToHandAsync(scene.Player, true, 0f));
            if (directDraw)
            {
                Assert.That(result, Is.SameAs(accepted));
            }
            else
            {
                Assert.That(result, Is.Null, "A replaced source must not publish old-generation deal success.");
            }

            await Quiesce(120);
            await UniTask.Yield();

            Assert.That(accepted != null, Is.True, "Accepted ownership must survive actual deferred destruction.");
            Assert.That(accepted.transform.parent, Is.SameAs(scene.Player.transform));
            Assert.That(scene.Player.Model, Is.SameAs(acceptedModel));
            CollectionAssert.AreEqual(new[] { accepted }, scene.Player.Cards);
            CollectionAssert.AreEqual(new[] { original[1] }, scene.Player.Model.Cards);
            CollectionAssert.AreEqual(reset ? original : scene.Config.GenerateDeck().ToArray(), scene.Deck.Model.Cards);
            Assert.That(admissions, Is.EqualTo(1));
            Assert.That(drawnEvents, Is.EqualTo(1));
            Assert.That(replacements, Is.EqualTo(1));
            Assert.That(dealtEvents, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SerializedNullSlot_ImmediatePlacement_KeepsCommittedCardAndTransform()
        {
            return SlotPlacement(false, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator SerializedNullSlot_AnimatedPlacement_KeepsCommittedCardAndTransform()
        {
            return SlotPlacement(false, true).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator SerializedValidSlot_ImmediatePlacement_MovesCommittedCardToSlot()
        {
            return SlotPlacement(true, false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator SerializedValidSlot_AnimatedPlacement_MovesCommittedCardToSlot()
        {
            return SlotPlacement(true, true).ToCoroutine();
        }

        private static async UniTask SlotPlacement(bool validSlot, bool animate)
        {
            using Scene scene = new();
            CardComponent card = scene.Card("SlotCard");
            Vector3 initialPosition = new(17f, 23f, 11f);
            Quaternion initialRotation = Quaternion.Euler(10f, 20f, 30f);
            card.transform.position = initialPosition;
            card.transform.rotation = initialRotation;
            Transform slot = validSlot ? Child(scene.Root, "AssignedSlot").transform : null;
            Vector3 slotPosition = new(200f, 300f, -40f);
            if (slot != null)
            {
                slot.position = slotPosition;
            }

            Set(scene.Board, "_layoutType", CardLayoutType.Slots);
            Set(scene.Board, "_cardSlots", new[] { slot });
            Set(scene.Board, "_placeDuration", 0.08f);
            int placements = 0;
            int moves = 0;
            scene.Board.OnCardPlaced = new UnityEvent<CardComponent>();
            scene.Board.OnCardPlaced.AddListener(placed =>
            {
                Assert.That(placed, Is.SameAs(card));
                CollectionAssert.AreEqual(new[] { card }, scene.Board.Cards);
                Assert.That(card.transform.parent, Is.SameAs(scene.Board.transform));
                placements++;
            });
            card.OnMoveComplete = new UnityEvent();
            card.OnMoveComplete.AddListener(() => moves++);
            Assert.That(scene.Board.CardSlots.Length, Is.EqualTo(1));
            if (validSlot)
            {
                Assert.That(scene.Board.CardSlots[0], Is.SameAs(slot));
            }
            else
            {
                Assert.That(scene.Board.CardSlots[0], Is.Null, "This control uses a serialized empty element, not an invalid index.");
            }

            await Within(scene.Board.PlaceCardAsync(card, animate, false));

            CollectionAssert.AreEqual(new[] { card }, scene.Board.Cards);
            Assert.That(card.transform.parent, Is.SameAs(scene.Board.transform));
            Assert.That(Vector3.Distance(card.transform.position, validSlot ? slotPosition : initialPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(card.transform.rotation, initialRotation), Is.LessThan(0.001f));
            Assert.That(placements, Is.EqualTo(1));
            Assert.That(moves, Is.EqualTo(validSlot && animate ? 1 : 0));
            Vector3 settled = card.transform.position;
            await Quiesce(160);
            Assert.That(card.transform.position, Is.EqualTo(settled));
            Assert.That(Quaternion.Angle(card.transform.rotation, initialRotation), Is.LessThan(0.001f));
            Assert.That(placements, Is.EqualTo(1));
            Assert.That(moves, Is.EqualTo(validSlot && animate ? 1 : 0));
        }

        private static void AssertBoardLine(BoardComponent board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                Assert.That(board.Cards[i].transform.parent, Is.SameAs(board.transform));
                Assert.That(board.Cards[i].transform.localPosition.x,
                    Is.EqualTo((i - (board.Count - 1) * 0.5f) * 80f).Within(0.001f));
            }
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

        private static UniTask Quiesce(int milliseconds)
        {
            double until = Time.realtimeSinceStartupAsDouble + milliseconds / 1000d;
            return WaitFor(() => Time.realtimeSinceStartupAsDouble >= until);
        }

        private static GameObject Child(GameObject root, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        private static void Set(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private sealed class Scene : IDisposable
        {
            private readonly float _originalTimeScale = Time.timeScale;
            internal readonly GameObject Root = new("AuditCardsLifecycle");
            internal readonly DeckConfig Config = ScriptableObject.CreateInstance<DeckConfig>();
            internal readonly DeckComponent Deck;
            internal readonly CardComponent Prefab;
            internal readonly HandComponent Player;
            internal readonly HandComponent Opponent;
            internal readonly BoardComponent Board;
            internal readonly DrunkardGame Game;
            internal readonly GameObject Table;
            internal int Restarts;
            internal int RoundEnds;
            internal int Wars;
            internal int WarEnds;
            internal int Winners;

            internal Scene()
            {
                Time.timeScale = 1f;
                try
                {
                    Root.SetActive(false);
                    Player = Child(Root, "Player").AddComponent<HandComponent>();
                    Opponent = Child(Root, "Opponent").AddComponent<HandComponent>();
                    Player.transform.localPosition = Vector3.right * 300f;
                    Opponent.transform.localPosition = Vector3.left * 300f;
                    Set(Player, "_arrangeDuration", 0f);
                    Set(Opponent, "_arrangeDuration", 0f);
                    Deck = Child(Root, "Deck").AddComponent<DeckComponent>();
                    Prefab = Child(Root, "Prefab").AddComponent<CardComponent>();
                    Set(Prefab, "_flipDuration", 0f);
                    Set(Deck, "_config", Config);
                    Set(Deck, "_cardPrefab", Prefab);
                    Set(Deck, "_initializeOnStart", false);
                    Set(Deck, "_shuffleOnStart", false);
                    Set(Deck, "_setAnimationConfigAsGlobal", false);
                    Board = Child(Root, "Board").AddComponent<BoardComponent>();
                    Set(Board, "_autoGenerateSlots", false);
                    Set(Board, "_layoutType", CardLayoutType.Line);
                    Set(Board, "_maxCards", 64);
                    Table = Child(Root, "Table");
                    Game = Root.AddComponent<DrunkardGame>();
                    Set(Game, "_deckComponent", Deck);
                    Set(Game, "_initializeOnStart", false);
                    Set(Game, "_cardsParent", Table.transform);
                    Set(Game, "_playerDeckPosition", Player.transform);
                    Set(Game, "_opponentDeckPosition", Opponent.transform);
                    Set(Game, "_cardMoveDuration", 0f);
                    Set(Game, "_turnDelay", 0f);
                    Set(Game, "_roundDelay", 0f);
                    Set(Game, "_cardReturnDelay", 0f);
                    Set(Game, "_warContinueDelay", 0f);
                    Event(Game, "_onGameRestarted", () => Restarts++);
                    Event(Game, "_onRoundEnded", () => RoundEnds++);
                    Event(Game, "_onWarStarted", () => Wars++);
                    Event(Game, "_onWarEnded", () => WarEnds++);
                    Event(Game, "_onPlayerWin", () => Winners++);
                    Event(Game, "_onOpponentWin", () => Winners++);
                    Event(Game, "_onPlayerWonRound", () => Winners++);
                    Event(Game, "_onOpponentWonRound", () => Winners++);
                    Event(Game, "_onRoundStarted", () => { });
                    Root.SetActive(true);
                }
                catch
                {
                    ReleaseFixture();
                    throw;
                }
            }

            internal CardComponent Card(string name)
            {
                CardComponent card = Child(Root, name).AddComponent<CardComponent>();
                card.SetData(CardData.CreateCustom(name));
                return card;
            }

            internal void SeedHands(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    CardComponent player = Card("Player" + i);
                    CardComponent opponent = Card("Opponent" + i);
                    player.SetData(new CardData(Suit.Hearts, (Rank)(5 + i % 8)));
                    opponent.SetData(new CardData(Suit.Spades, (Rank)(5 + i % 8)));
                    Player.AddCard(player);
                    Opponent.AddCard(opponent);
                }
            }

            public void Dispose()
            {
                try
                {
                    Deck.Reset();
                }
                finally
                {
                    ReleaseFixture();
                }
            }

            private void ReleaseFixture()
            {
                try
                {
                    Object.Destroy(Root);
                    Object.Destroy(Config);
                }
                finally
                {
                    Time.timeScale = _originalTimeScale;
                }
            }

            private static void Event(object target, string field, UnityAction callback)
            {
                UnityEvent unityEvent = new();
                unityEvent.AddListener(callback);
                Set(target, field, unityEvent);
            }
        }
    }
}
