using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public partial class Audit20261023NeoToolsInputBehaviorTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void EarlierPressDisable_FencesLateSubscriberInAndLaterPhases(bool reenableInsideCallback)
        {
            bool interrupt = true;
            int latePress = 0;
            int pressIn = 0;
            int hold = 0;
            int released = 0;
            int clicked = 0;
            int canceled = 0;
            _manager.enableHold = true;
            _manager.OnPress += data =>
            {
                if (interrupt)
                {
                    interrupt = false;
                    _manager.enabled = false;
                    if (reenableInsideCallback)
                    {
                        _manager.enabled = true;
                    }
                }
            };
            MouseEffect effect = CreateEffect();
            int starts = 0;
            effect.onStartFollow.AddListener(() => starts++);
            _manager.OnPress += data => latePress++;
            _manager.OnPressIn += (in MouseInputManager.MouseEventData data) => pressIn++;
            _manager.OnHold += data => hold++;
            _manager.OnRelease += data => released++;
            _manager.OnClick += data => clicked++;
            _manager.OnCanceled += () => canceled++;

            _manager.ProcessMouseState(_screenPosition, true, true, true);

            Assert.That(starts, Is.Zero);
            Assert.That(latePress, Is.Zero);
            Assert.That(pressIn, Is.Zero);
            Assert.That(hold, Is.Zero);
            Assert.That(released, Is.Zero);
            Assert.That(clicked, Is.Zero);
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);

            _manager.enabled = true;
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(latePress, Is.EqualTo(1));
            Assert.That(pressIn, Is.EqualTo(1));
            Assert.That(hold, Is.EqualTo(1));
            Assert.That(released, Is.EqualTo(1));
            Assert.That(clicked, Is.EqualTo(1));
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EarlierCancelOrReleaseReplacement_PreservesNewHitPressAndFollow(bool replaceDuringRelease)
        {
            GameObject replacement = CreateReplacementTarget(out Vector2 replacementScreen);
            bool replaced = false;
            Action replace = () =>
            {
                if (!replaced)
                {
                    replaced = true;
                    _manager.ProcessMouseState(replacementScreen, true, false, false);
                }
            };
            if (replaceDuringRelease)
            {
                _manager.OnRelease += data => replace();
            }
            else
            {
                _manager.OnCanceled += replace;
            }

            MouseEffect effect = CreateEffect();
            int starts = 0;
            int stops = 0;
            int lateCancel = 0;
            List<GameObject> releaseHits = new();
            List<GameObject> holdHits = new();
            List<GameObject> clickHits = new();
            List<GameObject> clickInHits = new();
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            _manager.OnCanceled += () => lateCancel++;
            _manager.OnRelease += data => releaseHits.Add(data.HitObject);
            _manager.OnHold += data => holdHits.Add(data.HitObject);
            _manager.OnClick += data => clickHits.Add(data.HitObject);
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) => clickInHits.Add(data.HitObject);
            _manager.enableHold = true;
            _manager.ProcessMouseState(_screenPosition, true, false, false);

            if (replaceDuringRelease)
            {
                _manager.ProcessMouseState(_screenPosition, false, false, true);
            }
            else
            {
                _manager.SendMessage("OnApplicationFocus", false);
            }

            Assert.That(starts, Is.EqualTo(2));
            Assert.That(stops, Is.Zero, "Old cancellation/release tails must not stop the replacement.");
            Assert.That(lateCancel, Is.Zero);
            Assert.That(releaseHits, Is.Empty);
            Assert.That(clickHits, Is.Empty);
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.SameAs(replacement));
            Assert.That(MouseInputManager.LastEventData.ScreenPosition, Is.EqualTo(replacementScreen));
            Assert.That(effect.followObject.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
            Assert.That(effect.followObject.activeSelf, Is.True);
            Assert.That(effect.trail.emitting, Is.True);

            _manager.ProcessMouseState(replacementScreen, false, true, false);
            _manager.ProcessMouseState(replacementScreen, false, false, true);
            CollectionAssert.AreEqual(new[] { replacement }, holdHits);
            CollectionAssert.AreEqual(new[] { replacement }, releaseHits);
            CollectionAssert.AreEqual(new[] { replacement }, clickHits);
            CollectionAssert.AreEqual(new[] { replacement }, clickInHits);
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
        }

        [Test]
        public void EarlierRepeatedCancellation_DoesNotSkipCleanupInTheSameCancellationBatch()
        {
            int cancellations = 0;
            _manager.OnCanceled += () =>
            {
                cancellations++;
                _manager.SendMessage("OnApplicationFocus", false);
            };
            MouseEffect effect = CreateEffect();
            int stops = 0;
            effect.onStopFollow.AddListener(() => stops++);

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.SendMessage("OnApplicationFocus", false);
            Assert.That(cancellations, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.SendMessage("OnApplicationFocus", false);
            Assert.That(cancellations, Is.EqualTo(2));
            Assert.That(stops, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisableDuringRelease_CancelsPendingVisualOwnershipAndSuppressesClick(bool disableInEvent)
        {
            bool interrupt = true;
            Action disable = () =>
            {
                if (interrupt)
                {
                    interrupt = false;
                    _manager.enabled = false;
                }
            };
            if (disableInEvent)
            {
                _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => disable();
            }
            else
            {
                _manager.OnRelease += data => disable();
            }

            MouseEffect effect = CreateEffect();
            int stopped = 0;
            int cancellations = 0;
            int clicked = 0;
            int lateRelease = 0;
            int lateIn = 0;
            effect.onStopFollow.AddListener(() => stopped++);
            _manager.OnCanceled += () => cancellations++;
            _manager.OnRelease += data => lateRelease++;
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => lateIn++;
            _manager.OnClick += data => clicked++;

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(cancellations, Is.EqualTo(1));
            Assert.That(clicked, Is.Zero);
            Assert.That(lateRelease, Is.EqualTo(disableInEvent ? 1 : 0));
            Assert.That(lateIn, Is.Zero);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);

            _manager.enabled = true;
            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(stopped, Is.EqualTo(2));
            Assert.That(cancellations, Is.EqualTo(1));
            Assert.That(clicked, Is.EqualTo(1));
        }

        [TestCase("PressIn")]
        [TestCase("HoldIn")]
        [TestCase("ReleaseIn")]
        [TestCase("ClickIn")]
        public void ReplacementInsideInEvent_FencesItsTailAndOldLaterPhases(string eventName)
        {
            GameObject replacement = CreateReplacementTarget(out Vector2 replacementScreen);
            bool replaced = false;
            SubscribeIn(eventName, (in MouseInputManager.MouseEventData data) =>
            {
                if (!replaced)
                {
                    replaced = true;
                    _manager.ProcessMouseState(replacementScreen, true, false, false);
                }
            });
            MouseEffect effect = CreateEffect();
            int starts = 0;
            int stops = 0;
            List<GameObject> lateHits = new();
            List<GameObject> clickHits = new();
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            SubscribeIn(eventName, (in MouseInputManager.MouseEventData data) => lateHits.Add(data.HitObject));
            _manager.OnClick += data => clickHits.Add(data.HitObject);
            _manager.enableHold = true;

            _manager.ProcessMouseState(_screenPosition, true, eventName == "PressIn", eventName == "PressIn");
            if (eventName == "HoldIn")
            {
                _manager.ProcessMouseState(_screenPosition, false, true, true);
            }
            else if (eventName == "ReleaseIn" || eventName == "ClickIn")
            {
                _manager.ProcessMouseState(_screenPosition, false, false, true);
            }

            Assert.That(starts, Is.EqualTo(2));
            Assert.That(stops, Is.EqualTo(eventName == "ReleaseIn" || eventName == "ClickIn" ? 1 : 0));
            Assert.That(lateHits.Exists(hit => hit == _target), Is.False);
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.SameAs(replacement));
            Assert.That(effect.followObject.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
            Assert.That(effect.followObject.activeSelf, Is.True);

            _manager.ProcessMouseState(replacementScreen, false, true, false);
            _manager.ProcessMouseState(replacementScreen, false, false, true);
            CollectionAssert.AreEqual(new[] { replacement }, lateHits);
            if (eventName == "ClickIn")
            {
                CollectionAssert.AreEqual(new[] { _target, replacement }, clickHits);
            }
            else
            {
                CollectionAssert.AreEqual(new[] { replacement }, clickHits);
            }

            Assert.That(stops, Is.EqualTo(eventName == "ReleaseIn" || eventName == "ClickIn" ? 2 : 1));
        }

        [TestCase(true, true, true, true, "press,press-in,hold,hold-in,release,release-in,click,click-in")]
        [TestCase(false, true, true, true, "hold,hold-in,release,release-in,click,click-in")]
        [TestCase(true, false, true, true, "press,press-in,release,release-in,click,click-in")]
        [TestCase(true, true, false, true, "press,press-in,hold,hold-in,click,click-in")]
        [TestCase(true, true, true, false, "press,press-in,hold,hold-in,release,release-in")]
        public void NormalDispatch_PreservesIndependentModesAndOrdinaryThenInOrder(bool press, bool hold,
            bool release, bool click, string expected)
        {
            List<string> order = new();
            _manager.enablePress = press;
            _manager.enableHold = hold;
            _manager.enableRelease = release;
            _manager.enableClick = click;
            _manager.OnPress += data => RecordEvent(order, "press", in data);
            _manager.OnHold += data => RecordEvent(order, "hold", in data);
            _manager.OnRelease += data => RecordEvent(order, "release", in data);
            _manager.OnClick += data => RecordEvent(order, "click", in data);
            _manager.OnPressIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "press-in", in data);
            _manager.OnHoldIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "hold-in", in data);
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "release-in", in data);
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "click-in", in data);
            _manager.OnCanceled += () => order.Add("canceled");

            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);

            Assert.That(string.Join(",", order), Is.EqualTo(expected));
        }

        [Test]
        public void ReleaseWithoutPress_PreservesOrdinaryAndInNotificationsWithoutClickOrCancellation()
        {
            List<string> order = new();
            _manager.OnRelease += data => RecordEvent(order, "release", in data);
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "release-in", in data);
            _manager.OnClick += data => order.Add("click");
            _manager.OnCanceled += () => order.Add("canceled");

            _manager.ProcessMouseState(_screenPosition, false, false, true);
            _manager.SendMessage("OnApplicationFocus", false);

            CollectionAssert.AreEqual(new[] { "release", "release-in" }, order);
        }

        [Test]
        public void SubscriptionMutation_KeepsActiveBatchAndCapturesInEventAfterOrdinaryCallbacks()
        {
            List<string> order = new();
            bool changed = false;
            Action<MouseInputManager.MouseEventData> second = data => order.Add("B");
            Action<MouseInputManager.MouseEventData> added = data => order.Add("C");
            MouseInputManager.MouseEventHandler addedIn = (in MouseInputManager.MouseEventData data) => order.Add("I2");
            _manager.OnPress += data =>
            {
                order.Add("A");
                if (!changed)
                {
                    changed = true;
                    _manager.OnPress -= second;
                    _manager.OnPress += added;
                    _manager.OnPressIn += addedIn;
                }
            };
            _manager.OnPress += second;
            _manager.OnPressIn += (in MouseInputManager.MouseEventData data) => order.Add("I1");

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.ProcessMouseState(_screenPosition, true, false, false);

            CollectionAssert.AreEqual(new[] { "A", "B", "I1", "I2", "A", "C", "I1", "I2" }, order);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EarlierHoldOrClickSubscriber_DisablesEffectWithoutCapturedSpawnAndRecovers(bool clickMode)
        {
            MouseEffect effect = null;
            bool disable = false;
            Action<MouseInputManager.MouseEventData> earlier = data =>
            {
                if (disable)
                {
                    disable = false;
                    effect.enabled = false;
                }
            };
            if (clickMode)
            {
                _manager.OnClick += earlier;
            }
            else
            {
                _manager.OnHold += earlier;
            }

            effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Captured Spawn Source");
            effect.spawnTrigger = clickMode ? MouseEffect.SpawnTrigger.Click : MouseEffect.SpawnTrigger.Hold;
            effect.spawnDuringHold = false;
            _manager.enableHold = true;
            int spawned = 0;
            int starts = 0;
            int stops = 0;
            effect.onSpawn.AddListener(() => spawned++);
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            DriveSpawnGesture(clickMode);
            GameObject retainedInstance = effect.transform.GetChild(0).gameObject;
            Assert.That(spawned, Is.EqualTo(1));

            disable = true;
            DriveSpawnGesture(clickMode);
            Assert.That(spawned, Is.EqualTo(1));
            Assert.That(effect.transform.childCount, Is.EqualTo(1));
            Assert.That(effect.enabled, Is.False);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            Assert.That(retainedInstance != null, Is.True);

            effect.enabled = true;
            DriveSpawnGesture(clickMode);
            Assert.That(spawned, Is.EqualTo(2));
            Assert.That(effect.transform.childCount, Is.EqualTo(2));
            Assert.That(starts, Is.EqualTo(3));
            Assert.That(stops, Is.EqualTo(3));
            Assert.That(retainedInstance, Is.SameAs(effect.transform.GetChild(0).gameObject));
            Assert.That(effect.transform.GetChild(1).position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
        }

        [Test]
        public void WarmHoldDispatch_ReusesBothSnapshotsWithoutSubscriptionChanges()
        {
            int ordinary = 0;
            int byReference = 0;
            _manager.enableHold = true;
            _manager.OnHold += data => ordinary++;
            _manager.OnHoldIn += (in MouseInputManager.MouseEventData data) => byReference++;
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            Delegate[] ordinarySnapshot = GetCachedSnapshot("_holdSubscribers");
            Delegate[] inSnapshot = GetCachedSnapshot("_holdInSubscribers");
            Assert.That(ordinarySnapshot.Length, Is.EqualTo(1));
            Assert.That(inSnapshot.Length, Is.EqualTo(1));

            for (int i = 0; i < 32; i++)
            {
                _manager.ProcessMouseState(_screenPosition, false, true, false);
                Assert.That(GetCachedSnapshot("_holdSubscribers"), Is.SameAs(ordinarySnapshot));
                Assert.That(GetCachedSnapshot("_holdInSubscribers"), Is.SameAs(inSnapshot));
            }

            Assert.That(ordinary, Is.EqualTo(33));
            Assert.That(byReference, Is.EqualTo(33));
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.SameAs(_target));
        }

        private static readonly string[] SnapshotEventNames =
        {
            "Press", "Hold", "Release", "Click", "Canceled", "PressIn", "HoldIn", "ReleaseIn", "ClickIn"
        };

        [UnityTest]
        public IEnumerator LastUnsubscribe_ReleasesWarmedTargetWithoutWaitingForNextEvent(
            [ValueSource(nameof(SnapshotEventNames))] string eventName)
        {
            int warmedFrame = Time.frameCount;
            WeakReference target = SubscribeAndWarmProbe(eventName);
            // The completed Release left no pending gesture. Prevent native Update publications
            // during either frame boundary without removing this positive retention subscription.
            _manager.enabled = false;
            yield return null;
            int retainedFrame = Time.frameCount;
            Assert.That(retainedFrame, Is.GreaterThan(warmedFrame), "The subscribed GC control requires a new native frame.");
            CollectProbeGarbage();
            Assert.That(target.IsAlive, Is.True, "A subscribed warmed target must remain manager-owned across GC.");

            RemoveProbeAndAssertImmediateRelease(eventName, target);
            TestContext.WriteLine($"Unsubscribed {eventName} in frame {retainedFrame}: manager delegate=null, snapshot=null; native Update disabled.");
            // Only the WeakReference enters this iterator's fields. Unsubscribe delegates and the
            // strong probe local live in a separate non-inlined helper, in the previous native frame.
            yield return null;
            int releasedFrame = Time.frameCount;
            Assert.That(releasedFrame, Is.GreaterThan(retainedFrame), "Collection must occur after the unsubscribe native frame returns.");
            CollectProbeGarbage();

            TestContext.WriteLine($"GC probe {eventName}: warmed={warmedFrame}, retained={retainedFrame}, released={releasedFrame}, alive={target.IsAlive}.");
            Assert.That(target.IsAlive, Is.False, "No next publication may be needed to drop an unsubscribed target.");
        }

        [Test]
        public void SubscriberException_StopsBatchAndInPhaseButDoesNotPoisonLaterCancellationOrInput()
        {
            Action<MouseInputManager.MouseEventData> throwing = data => throw new InvalidOperationException("Audit observer");
            _manager.OnPress += throwing;
            int late = 0;
            int inCalls = 0;
            int canceled = 0;
            _manager.OnPress += data => late++;
            _manager.OnPressIn += (in MouseInputManager.MouseEventData data) => inCalls++;
            _manager.OnCanceled += () => canceled++;

            Assert.Throws<InvalidOperationException>(() => _manager.ProcessMouseState(_screenPosition, true, true, true));
            Assert.That(late, Is.Zero);
            Assert.That(inCalls, Is.Zero);
            _manager.OnPress -= throwing;
            _manager.SendMessage("OnApplicationFocus", false);
            _manager.ProcessMouseState(_screenPosition, true, false, false);

            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(late, Is.EqualTo(1));
            Assert.That(inCalls, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReleaseObserverFault_RetainsIncompleteOwnershipForManagerOnlyCancellation(bool faultInEvent)
        {
            InvalidOperationException fault = new("Audit Release observer");
            int faults = 0;
            Action<MouseInputManager.MouseEventData> throwing = data =>
            {
                faults++;
                throw fault;
            };
            MouseInputManager.MouseEventHandler throwingIn = (in MouseInputManager.MouseEventData data) => throwing(data);
            if (faultInEvent)
            {
                _manager.OnReleaseIn += throwingIn;
            }
            else
            {
                // The real effect must subscribe after the failing ordinary observer.
                _manager.OnRelease += throwing;
            }

            MouseEffect effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Faulted Release Spawn Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Release;
            int starts = 0;
            int stops = 0;
            int spawned = 0;
            int lateRelease = 0;
            int lateReleaseIn = 0;
            int clicked = 0;
            int clickedIn = 0;
            int canceled = 0;
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            effect.onSpawn.AddListener(() => spawned++);
            _manager.OnRelease += data => lateRelease++;
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => lateReleaseIn++;
            _manager.OnClick += data => clicked++;
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) => clickedIn++;
            _manager.OnCanceled += () => canceled++;

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            Vector3 pressWorld = MouseInputManager.LastEventData.WorldPosition;
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.SameAs(_target));
            Assert.That(MouseInputManager.LastEventData.ScreenPosition, Is.EqualTo(_screenPosition));
            Assert.That(Vector3.Distance(pressWorld, _target.transform.position + Vector3.back * 0.5f), Is.LessThan(0.001f));
            Assert.That(effect.followObject.transform.position, Is.EqualTo(pressWorld));
            Assert.That(effect.trail.transform.position, Is.EqualTo(pressWorld));
            Assert.That(effect.followObject.activeSelf, Is.True);
            Assert.That(effect.trail.emitting, Is.True);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.Zero);

            InvalidOperationException observed = Assert.Throws<InvalidOperationException>(() =>
                _manager.ProcessMouseState(_screenPosition, false, false, true));
            int completedOrdinaryRelease = faultInEvent ? 1 : 0;
            Assert.That(observed, Is.SameAs(fault));
            Assert.That(faults, Is.EqualTo(1));
            Assert.That(lateRelease, Is.EqualTo(completedOrdinaryRelease));
            Assert.That(lateReleaseIn, Is.Zero);
            Assert.That(clicked, Is.Zero);
            Assert.That(clickedIn, Is.Zero);
            Assert.That(canceled, Is.Zero);
            Assert.That(stops, Is.EqualTo(completedOrdinaryRelease));
            Assert.That(spawned, Is.EqualTo(completedOrdinaryRelease));
            Assert.That(effect.followObject.activeSelf, Is.EqualTo(!faultInEvent));
            Assert.That(effect.trail.emitting, Is.EqualTo(!faultInEvent));

            if (faultInEvent)
            {
                _manager.OnReleaseIn -= throwingIn;
            }
            else
            {
                _manager.OnRelease -= throwing;
            }

            // Only the publisher loses its native lifecycle; the effect stays enabled.
            _manager.enabled = false;
            Assert.That(effect.isActiveAndEnabled, Is.True);
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            _manager.SendMessage("OnApplicationFocus", false);
            _manager.SendMessage("OnApplicationPause", true);
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(lateRelease, Is.EqualTo(completedOrdinaryRelease));
            Assert.That(lateReleaseIn, Is.Zero);
            Assert.That(clicked, Is.Zero);
            Assert.That(clickedIn, Is.Zero);
            Assert.That(spawned, Is.EqualTo(completedOrdinaryRelease));
            Assert.That(effect.transform.childCount, Is.EqualTo(completedOrdinaryRelease));

            _manager.enabled = true;
            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            _manager.enabled = false;
            Assert.That(starts, Is.EqualTo(2));
            Assert.That(stops, Is.EqualTo(2));
            Assert.That(lateRelease, Is.EqualTo(completedOrdinaryRelease + 1));
            Assert.That(lateReleaseIn, Is.EqualTo(1));
            Assert.That(clicked, Is.EqualTo(1));
            Assert.That(clickedIn, Is.EqualTo(1));
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(faults, Is.EqualTo(1));
            Assert.That(spawned, Is.EqualTo(completedOrdinaryRelease + 1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
        }

        [Test]
        public void SuccessfulRelease_StopsVisualsAndConsumesOwnershipBeforeLaterLifecycleLoss()
        {
            MouseEffect effect = CreateEffect();
            int starts = 0;
            int stops = 0;
            int canceled = 0;
            List<string> order = new();
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            _manager.OnRelease += data => RecordEvent(order, "release", in data);
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "release-in", in data);
            _manager.OnClick += data => RecordEvent(order, "click", in data);
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) => RecordEvent(order, "click-in", in data);
            _manager.OnCanceled += () => canceled++;

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.Zero);
            Assert.That(effect.followObject.activeSelf, Is.True);
            Assert.That(effect.trail.emitting, Is.True);
            Assert.That(effect.followObject.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));

            _manager.ProcessMouseState(_screenPosition, false, false, true);
            _manager.enabled = false;
            _manager.SendMessage("OnApplicationFocus", false);
            _manager.SendMessage("OnApplicationPause", true);
            CollectionAssert.AreEqual(new[] { "release", "release-in", "click", "click-in" }, order);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(canceled, Is.Zero);
            Assert.That(effect.isActiveAndEnabled, Is.True);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
        }

        [Test]
        public void OldSuccessfulReleaseFinally_CannotConsumeReplacementFaultedReleaseOwnership()
        {
            GameObject replacement = CreateReplacementTarget(out Vector2 replacementScreen);
            InvalidOperationException fault = new("Audit replacement Release observer");
            Action<MouseInputManager.MouseEventData> throwing = data =>
            {
                if (data.HitObject == replacement)
                {
                    throw fault;
                }
            };
            _manager.OnRelease += throwing;
            List<GameObject> earlyClicks = new();
            _manager.OnClick += data =>
            {
                earlyClicks.Add(data.HitObject);
                if (data.HitObject == _target)
                {
                    _manager.ProcessMouseState(replacementScreen, true, false, false);
                    _manager.ProcessMouseState(replacementScreen, false, false, true);
                }
            };
            MouseEffect effect = CreateEffect();
            int starts = 0;
            int stops = 0;
            int canceled = 0;
            List<GameObject> releases = new();
            List<GameObject> releasesIn = new();
            List<GameObject> holds = new();
            List<GameObject> lateClicks = new();
            List<GameObject> clicksIn = new();
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            _manager.OnCanceled += () => canceled++;
            _manager.OnRelease += data => releases.Add(data.HitObject);
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => releasesIn.Add(data.HitObject);
            _manager.OnHold += data => holds.Add(data.HitObject);
            _manager.OnClick += data => lateClicks.Add(data.HitObject);
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) => clicksIn.Add(data.HitObject);
            _manager.enableHold = true;

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            InvalidOperationException observed = Assert.Throws<InvalidOperationException>(() =>
                _manager.ProcessMouseState(_screenPosition, false, false, true));
            Assert.That(observed, Is.SameAs(fault));
            Assert.That(starts, Is.EqualTo(2));
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(canceled, Is.Zero);
            CollectionAssert.AreEqual(new[] { _target }, releases);
            CollectionAssert.AreEqual(new[] { _target }, releasesIn);
            CollectionAssert.AreEqual(new[] { _target }, earlyClicks);
            Assert.That(holds, Is.Empty);
            Assert.That(lateClicks, Is.Empty);
            Assert.That(clicksIn, Is.Empty);
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.SameAs(replacement));
            Assert.That(MouseInputManager.LastEventData.ScreenPosition, Is.EqualTo(replacementScreen));
            Assert.That(effect.followObject.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
            Assert.That(effect.trail.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
            Assert.That(effect.followObject.activeSelf, Is.True);
            Assert.That(effect.trail.emitting, Is.True);

            _manager.OnRelease -= throwing;
            _manager.enabled = false;
            _manager.SendMessage("OnApplicationFocus", false);
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(2));
            Assert.That(effect.isActiveAndEnabled, Is.True);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);

            _manager.enabled = true;
            _manager.ProcessMouseState(replacementScreen, true, false, false);
            _manager.ProcessMouseState(replacementScreen, false, true, false);
            _manager.ProcessMouseState(replacementScreen, false, false, true);
            _manager.enabled = false;
            CollectionAssert.AreEqual(new[] { replacement }, holds);
            CollectionAssert.AreEqual(new[] { _target, replacement }, releases);
            CollectionAssert.AreEqual(new[] { _target, replacement }, releasesIn);
            CollectionAssert.AreEqual(new[] { _target, replacement }, earlyClicks);
            CollectionAssert.AreEqual(new[] { replacement }, lateClicks);
            CollectionAssert.AreEqual(new[] { replacement }, clicksIn);
            Assert.That(starts, Is.EqualTo(3));
            Assert.That(stops, Is.EqualTo(3));
            Assert.That(canceled, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateSubscription_RemovesOneMatchAndNullAccessDoesNotInvalidateSnapshot()
        {
            int calls = 0;
            Action<MouseInputManager.MouseEventData> handler = data => calls++;
            _manager.enableHold = true;
            _manager.OnHold += handler;
            _manager.OnHold += handler;
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            Assert.That(calls, Is.EqualTo(2));
            Delegate[] initial = GetCachedSnapshot("_holdSubscribers");
            _manager.OnHold += null;
            _manager.OnHold -= null;
            Assert.That(GetCachedSnapshot("_holdSubscribers"), Is.SameAs(initial));
            _manager.OnHold -= handler;
            Assert.That(GetCachedSnapshot("_holdSubscribers"), Is.Null);
            _manager.ProcessMouseState(_screenPosition, false, true, false);
            Assert.That(calls, Is.EqualTo(3));
        }

        private void RecordEvent(List<string> order, string name, in MouseInputManager.MouseEventData data)
        {
            Assert.That(data.HitObject, Is.SameAs(_target));
            Assert.That(data.ScreenPosition, Is.EqualTo(_screenPosition));
            Assert.That(data.WorldPosition, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
            order.Add(name);
        }

        private GameObject CreateReplacementTarget(out Vector2 screenPosition)
        {
            GameObject replacement = CreateObject("Audit Replacement Target");
            replacement.transform.position = _target.transform.position + Vector3.right * 2f;
            replacement.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            screenPosition = _camera.WorldToScreenPoint(replacement.transform.position);
            return replacement;
        }

        private void DriveSpawnGesture(bool clickMode)
        {
            _manager.ProcessMouseState(_screenPosition, true, false, false);
            if (!clickMode)
            {
                _manager.ProcessMouseState(_screenPosition, false, true, false);
            }

            _manager.ProcessMouseState(_screenPosition, false, false, true);
        }

        private void SubscribeIn(string eventName, MouseInputManager.MouseEventHandler handler)
        {
            switch (eventName)
            {
                case "PressIn": _manager.OnPressIn += handler; break;
                case "HoldIn": _manager.OnHoldIn += handler; break;
                case "ReleaseIn": _manager.OnReleaseIn += handler; break;
                case "ClickIn": _manager.OnClickIn += handler; break;
                default: throw new ArgumentOutOfRangeException(nameof(eventName));
            }
        }

        private Delegate[] GetCachedSnapshot(string subscribersField)
        {
            FieldInfo field = typeof(MouseInputManager).GetField(subscribersField, BindingFlags.Instance | BindingFlags.NonPublic);
            object subscribers = field.GetValue(_manager);
            return (Delegate[])subscribers.GetType().GetField("_snapshot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(subscribers);
        }

        private Delegate GetCombinedSubscribers(string subscribersField)
        {
            FieldInfo field = typeof(MouseInputManager).GetField(subscribersField, BindingFlags.Instance | BindingFlags.NonPublic);
            object subscribers = field.GetValue(_manager);
            return (Delegate)subscribers.GetType().GetField("_handlers", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(subscribers);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private WeakReference SubscribeAndWarmProbe(string eventName)
        {
            SubscriberProbe probe = new();
            switch (eventName)
            {
                case "Press": _manager.OnPress += probe.OnData; break;
                case "Hold": _manager.OnHold += probe.OnData; break;
                case "Release": _manager.OnRelease += probe.OnData; break;
                case "Click": _manager.OnClick += probe.OnData; break;
                case "Canceled": _manager.OnCanceled += probe.OnCanceled; break;
                default: SubscribeIn(eventName, probe.OnIn); break;
            }

            _manager.enableHold = true;
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.SendMessage("OnApplicationFocus", false);
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(probe.Calls, Is.EqualTo(eventName == "Press" || eventName == "Hold" ||
                eventName == "PressIn" || eventName == "HoldIn" ? 2 : 1));
            string field = "_" + char.ToLowerInvariant(eventName[0]) + eventName.Substring(1) + "Subscribers";
            Assert.That(ReferenceEquals(GetCombinedSubscribers(field).Target, probe), Is.True);
            Delegate[] snapshot = GetCachedSnapshot(field);
            Assert.That(snapshot.Length, Is.EqualTo(1));
            Assert.That(ReferenceEquals(snapshot[0].Target, probe), Is.True);
            return new WeakReference(probe);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RemoveProbeAndAssertImmediateRelease(string eventName, WeakReference target)
        {
            SubscriberProbe probe = (SubscriberProbe)target.Target;
            Assert.That(probe != null, Is.True);
            Assert.That(probe.Calls, Is.EqualTo(eventName == "Press" || eventName == "Hold" ||
                eventName == "PressIn" || eventName == "HoldIn" ? 2 : 1), "No intervening publication is allowed.");
            switch (eventName)
            {
                case "Press": _manager.OnPress -= probe.OnData; break;
                case "Hold": _manager.OnHold -= probe.OnData; break;
                case "Release": _manager.OnRelease -= probe.OnData; break;
                case "Click": _manager.OnClick -= probe.OnData; break;
                case "Canceled": _manager.OnCanceled -= probe.OnCanceled; break;
                case "PressIn": _manager.OnPressIn -= probe.OnIn; break;
                case "HoldIn": _manager.OnHoldIn -= probe.OnIn; break;
                case "ReleaseIn": _manager.OnReleaseIn -= probe.OnIn; break;
                case "ClickIn": _manager.OnClickIn -= probe.OnIn; break;
            }

            string field = "_" + char.ToLowerInvariant(eventName[0]) + eventName.Substring(1) + "Subscribers";
            Assert.That(GetCombinedSubscribers(field), Is.Null, "The manager must drop its combined delegate immediately.");
            Assert.That(GetCachedSnapshot(field), Is.Null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CollectProbeGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private sealed class SubscriberProbe
        {
            public int Calls;
            public void OnData(MouseInputManager.MouseEventData data) => Calls++;
            public void OnIn(in MouseInputManager.MouseEventData data) => Calls++;
            public void OnCanceled() => Calls++;
        }
    }
}
