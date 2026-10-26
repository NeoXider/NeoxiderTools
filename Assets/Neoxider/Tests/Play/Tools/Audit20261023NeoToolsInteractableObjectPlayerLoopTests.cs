using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public partial class Audit20261023NeoToolsInteractableObjectLifecycleTests
    {
        [UnityTest] public IEnumerator PlayerLoop_Camera3D_1() => AutomaticQueries(1, false, false);
        [UnityTest] public IEnumerator PlayerLoop_Camera3D_100() => AutomaticQueries(100, false, false);
        [UnityTest] public IEnumerator PlayerLoop_Camera3D_1000() => AutomaticQueries(1000, false, false);
        [UnityTest] public IEnumerator PlayerLoop_Camera2D_1() => AutomaticQueries(1, true, false);
        [UnityTest] public IEnumerator PlayerLoop_Camera2D_100() => AutomaticQueries(100, true, false);
        [UnityTest] public IEnumerator PlayerLoop_Camera2D_1000() => AutomaticQueries(1000, true, false);
        [UnityTest] public IEnumerator PlayerLoop_KeyboardLook3D_1() => AutomaticQueries(1, false, true);
        [UnityTest] public IEnumerator PlayerLoop_KeyboardLook3D_100() => AutomaticQueries(100, false, true);
        [UnityTest] public IEnumerator PlayerLoop_KeyboardLook3D_1000() => AutomaticQueries(1000, false, true);
        [UnityTest] public IEnumerator PlayerLoop_KeyboardLook2D_1() => AutomaticQueries(1, true, true);
        [UnityTest] public IEnumerator PlayerLoop_KeyboardLook2D_100() => AutomaticQueries(100, true, true);
        [UnityTest] public IEnumerator PlayerLoop_KeyboardLook2D_1000() => AutomaticQueries(1000, true, true);

        private IEnumerator AutomaticQueries(int targetCount, bool twoDimensions, bool keyboard)
        {
            if (keyboard)
            {
                EnsureInput();
            }

            Transform lookSource = _camera.transform;
            if (keyboard && twoDimensions)
            {
                lookSource = CreateObject("PlanarLookSource").transform;
                lookSource.position = Origin;
                lookSource.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            }

            Vector3 direction = keyboard && twoDimensions ? Vector3.right : Vector3.forward;
            List<InteractiveObject> targets = new();
            for (int i = 0; i < targetCount; i++)
            {
                // Distinct physical targets, all on the same raw ray. At N=1000 this also exercises N x H projection.
                InteractiveObject target = CreateTarget(Origin + direction * (2f + i * 0.005f),
                    keyboard: keyboard, hover: !keyboard, twoDimensions: twoDimensions);
                target.InteractionDistance = 10f; // Identical keyboard look length, independent of target distance.
                // A component JSON overwrite can recreate the intentionally null inline UnityEvents.
                SetField(target, "checkObstacles", false);
                SetField(target, "viewCheckPoint", lookSource);
                AssertNoObserverEvents(target);
                targets.Add(target);
            }

            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            if (keyboard)
            {
                _input.QueueKeyboard(true);
            }

            yield return null; // The actual PlayerLoop warms native and target-local buffers; no SendMessage/manual Update.
            if (keyboard)
            {
                Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, _input.Diagnostics);
            }

            AssertConsumerStates(targets, keyboard, true);
            if (keyboard)
            {
                Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, _input.Diagnostics);
                _input.QueueKeyboard(false);
                yield return null;
                _input.QueueKeyboard(true);
            }

            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            int beforeFrame = Time.frameCount;
            yield return null;
            Assert.That(Time.frameCount, Is.GreaterThan(beforeFrame));
            AssertConsumerStates(targets, keyboard, true);
            if (keyboard)
            {
                Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, _input.Diagnostics);
            }

            WriteCounters("positive", targetCount, twoDimensions, keyboard);
            AssertStableCounters(targetCount, twoDimensions, keyboard, targetCount);

            if (keyboard)
            {
                _input.QueueKeyboard(false);
                yield return null;
            }

            // Observe real hover-exit transitions before measuring the unchanged miss context.
            int[] hoverExits = new int[targetCount];
            for (int i = 0; i < targets.Count; i++)
            {
                int consumer = i;
                InteractiveObject target = targets[i];
                target.onHoverExit ??= new UnityEvent();
                target.onHoverExit.AddListener(() => hoverExits[consumer]++);
                target.transform.position += Vector3.up * 20f;
            }

            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            if (keyboard)
            {
                _input.QueueKeyboard(true);
            }

            yield return null;
            AssertConsumerStates(targets, keyboard, false);
            foreach (int exits in hoverExits)
            {
                Assert.That(exits, Is.EqualTo(keyboard ? 0 : 1));
            }

            WriteCounters("transition-miss", targetCount, twoDimensions, keyboard);
            if (keyboard)
            {
                _input.QueueKeyboard(false);
                yield return null; // Rearm the next real press without changing geometry.
                _input.QueueKeyboard(true);
            }

            InteractionPhysicsQueries.ResetCounters();
            beforeFrame = Time.frameCount;
            yield return null; // Camera: the very next frame, after all hover-exit observers settled.
            Assert.That(Time.frameCount, Is.GreaterThan(beforeFrame));
            AssertConsumerStates(targets, keyboard, false);
            foreach (int exits in hoverExits)
            {
                Assert.That(exits, Is.EqualTo(keyboard ? 0 : 1), "A stable miss must not repeat hover exit.");
            }

            WriteCounters("stable-miss", targetCount, twoDimensions, keyboard);
            AssertStableCounters(targetCount, twoDimensions, keyboard, 0);
        }

        private static void AssertNoObserverEvents(InteractiveObject target)
        {
            Assert.That(new object[]
            {
                target.onHoverEnter, target.onHoverExit, target.onHoverChanged,
                target.onEnterRange, target.onExitRange, target.onInteractDown, target.onInteractUp,
                target.onClick, target.onDoubleClick, target.onRightClick, target.onMiddleClick
            }, Is.All.Null, "Stable query setup requires null event objects; even an empty UnityEvent is a callback boundary.");
        }

        private void AssertConsumerStates(List<InteractiveObject> targets, bool keyboard, bool expected)
        {
            string input = keyboard ? _input.Diagnostics : "";
            foreach (InteractiveObject target in targets)
            {
                Assert.That(target.isActiveAndEnabled, Is.True);
                Assert.That(keyboard ? InteractedThisFrame(target) : target.IsHovered, Is.EqualTo(expected), input);
                if (expected)
                {
                    AssertNoObserverEvents(target); // Confirm the precondition again at the actual positive consumer frame.
                }
            }
        }

        private static void AssertStableCounters(int targetCount, bool twoDimensions, bool keyboard, int hitCount)
        {
            InteractionPhysicsQueries.Statistics counters = InteractionPhysicsQueries.Counters;
            Assert.That(counters.NativeCalls, Is.EqualTo(1), "Native work must be independent of consumer count after warm-up.");
            Assert.That(counters.CameraNativeCalls, Is.EqualTo(keyboard ? 0 : 1));
            Assert.That(counters.LookNativeCalls, Is.EqualTo(keyboard ? 1 : 0));
            Assert.That(counters.Raycasts3D, Is.EqualTo(twoDimensions ? 0 : 1));
            Assert.That(counters.RayIntersections2D, Is.EqualTo(twoDimensions && !keyboard ? 1 : 0));
            Assert.That(counters.Raycasts2D, Is.EqualTo(twoDimensions && keyboard ? 1 : 0));
            Assert.That(counters.CacheHits, Is.EqualTo(targetCount - 1));
            Assert.That(counters.OverflowFallbacks, Is.Zero);
            Assert.That(counters.RangeNativeCalls, Is.Zero, "Unique LOS is a separate scenario, not merged into the camera budget.");
            Assert.That(counters.CallbackBoundaries, Is.Zero, "Stable measurement has no observer-created epochs.");
            Assert.That(counters.NativeCallsInsideCallbacks, Is.Zero);
            Assert.That(counters.FreshQueries, Is.Zero);
            Assert.That(counters.FrameAdvances, Is.EqualTo(1));
            Assert.That(counters.ClassificationPasses, Is.EqualTo(targetCount));
            Assert.That(counters.ClassifiedHits, Is.EqualTo((long)targetCount * hitCount));
        }

        private static void WriteCounters(string control, int targetCount, bool twoDimensions, bool keyboard)
        {
            InteractionPhysicsQueries.Statistics counters = InteractionPhysicsQueries.Counters;
            TestContext.WriteLine($"PlayerLoop interaction {control}: N={targetCount}, dimension={(twoDimensions ? 2 : 3)}, " +
                $"route={(keyboard ? "keyboard" : "camera")}, native={counters.NativeCalls}, " +
                $"camera={counters.CameraNativeCalls}, look={counters.LookNativeCalls}, LOS={counters.RangeNativeCalls}, " +
                $"cache={counters.CacheHits}, fallback={counters.OverflowFallbacks}, frameEpochs={counters.FrameAdvances}, " +
                $"callbackEpochs={counters.CallbackBoundaries}, callbackNative={counters.NativeCallsInsideCallbacks}, " +
                $"classificationPasses={counters.ClassificationPasses}, classifiedHits={counters.ClassifiedHits}.");
        }
    }
}
