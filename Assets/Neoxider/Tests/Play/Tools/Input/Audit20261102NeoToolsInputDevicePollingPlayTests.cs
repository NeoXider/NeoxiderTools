using System.Collections;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Mouse, gamepad and touch polling through real virtual Input System devices and the real PlayerLoop update:
    ///     bound Vector2/integer/touch-phase getters report the native state for every phase and allocate nothing
    ///     once warm. The driver forces the optional backend temporarily; it is not a New-only project.
    /// </summary>
    public sealed class Audit20261102NeoToolsInputDevicePollingPlayTests
    {
        private const int Iterations = 4096;
        private static readonly Vector2 BeganPosition = new(100f, 100f);
        private static readonly Vector2 MovedPosition = new(220f, 160f);
        private static readonly Vector2 LeftStickInput = new(0.6f, -0.45f);
        private static readonly Vector2 RightStickInput = new(-0.3f, 0.7f);

        [SetUp]
        public void SetUp()
        {
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [UnityTest]
        public IEnumerator WarmMousePolling_RealVirtualMouseAllocatesNothing()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                input.QueueMouse(true);
                yield return null;

                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False, input.Diagnostics);
                Assert.That(PollMouse(64, true), Is.EqualTo(64), input.Diagnostics);
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = PollMouse(Iterations, true);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), input.Diagnostics);
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls for the MouseInputManager-style position/down/held/up pass.");
                }

                input.QueueMouse(false);
                yield return null;
                Assert.That(PollMouse(64, false), Is.EqualTo(64), "Release must be read live: " + input.Diagnostics);
            }
        }

        [UnityTest]
        public IEnumerator PrimaryTouchLifecycle_RealVirtualTouchscreenReadsEveryPhase()
        {
            using (new Audit20261023NeoToolsInteractableObjectInputDriver())
            using (InputVirtualTouchscreen touch = new())
            {
                yield return null;
                yield return null;
                Assert.That(OptionalInputSystemAdapter.TryGetPrimaryTouch(out _, out _, out _), Is.False,
                    "An idle touchscreen has no contact. " + touch.DescribePrimaryTouch());

                touch.Queue(1, "Began", BeganPosition);
                yield return null;
                AssertTouch(touch, TouchPhase.Began, BeganPosition, "began frame");

                yield return null;
                AssertTouch(touch, TouchPhase.Stationary, BeganPosition, "persisting raw Began without a new edge");

                touch.Queue(1, "Moved", MovedPosition);
                yield return null;
                AssertTouch(touch, TouchPhase.Moved, MovedPosition, "moved frame");

                touch.Queue(1, "Ended", MovedPosition);
                yield return null;
                AssertTouch(touch, TouchPhase.Ended, MovedPosition, "ended frame");

                yield return null;
                Assert.That(OptionalInputSystemAdapter.TryGetPrimaryTouch(out _, out _, out _), Is.False,
                    "A persisting raw Ended phase without a release edge is not a contact. " + touch.DescribePrimaryTouch());
            }
        }

        [UnityTest]
        public IEnumerator WarmPrimaryTouch_RealVirtualTouchscreenAllocatesNothing()
        {
            using (new Audit20261023NeoToolsInteractableObjectInputDriver())
            using (InputVirtualTouchscreen touch = new())
            {
                yield return null;
                yield return null;
                touch.Queue(1, "Began", BeganPosition);
                yield return null;
                touch.Queue(1, "Moved", MovedPosition);
                yield return null;
                AssertTouch(touch, TouchPhase.Moved, MovedPosition, "warm-up");
                Assert.That(PollTouch(64), Is.EqualTo(64), touch.DescribePrimaryTouch());
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = PollTouch(Iterations);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), touch.DescribePrimaryTouch());
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls for warmed primary-touch reads (phase, id, position, edges).");
                }

                touch.Queue(1, "Ended", MovedPosition);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator WarmGamepadPolling_RealVirtualGamepadReadsLiveAndAllocatesNothing()
        {
            using (new Audit20261023NeoToolsInteractableObjectInputDriver())
            using (InputVirtualGamepad gamepad = new())
            {
                yield return null;
                yield return null;
                gamepad.Queue(LeftStickInput, RightStickInput, true);
                yield return null;
                GamepadExpectation expected = new(gamepad.ReadNativeStick("leftStick"),
                    gamepad.ReadNativeStick("rightStick"), OptionalInputSystemAdapter.ReadPointerDelta());
                Assert.That(expected.LeftStick.sqrMagnitude, Is.GreaterThan(0.01f), gamepad.Describe());
                Assert.That(expected.RightStick.sqrMagnitude, Is.GreaterThan(0.01f), gamepad.Describe());
                Assert.That(OptionalInputSystemAdapter.ReadJumpPressed(), Is.True,
                    "South press edge in its event frame. " + gamepad.Describe());
                Assert.That(PollGamepad(expected, 64), Is.EqualTo(64), gamepad.Describe());
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = PollGamepad(expected, Iterations);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), gamepad.Describe());
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls for warmed ReadMove/ReadLookStick/ReadLookDelta/jump/run reads.");
                }

                gamepad.Queue(Vector2.zero, Vector2.zero, false);
                yield return null;
                Assert.That(OptionalInputSystemAdapter.ReadMove(), Is.EqualTo(Vector2.zero),
                    "A released stick must be read live. " + gamepad.Describe());
                Assert.That(OptionalInputSystemAdapter.ReadJumpHeld(), Is.False, gamepad.Describe());
            }
        }

        private static int PollGamepad(GamepadExpectation expected, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 move = OptionalInputSystemAdapter.ReadMove();
                Vector2 lookStick = OptionalInputSystemAdapter.ReadLookStick();
                Vector2 lookDelta = OptionalInputSystemAdapter.ReadLookDelta(1f);
                bool jumpPressed = OptionalInputSystemAdapter.ReadJumpPressed();
                bool jumpHeld = OptionalInputSystemAdapter.ReadJumpHeld();
                bool run = OptionalInputSystemAdapter.ReadRunHeld();
                if (move.x == expected.LeftStick.x && move.y == expected.LeftStick.y &&
                    lookStick.x == expected.RightStick.x && lookStick.y == expected.RightStick.y &&
                    lookDelta == expected.PointerDelta + expected.RightStick && jumpPressed && jumpHeld && !run)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static void AssertTouch(InputVirtualTouchscreen touch, TouchPhase expectedPhase, Vector2 expectedPosition,
            string step)
        {
            string native = touch.DescribePrimaryTouch();
            Assert.That(OptionalInputSystemAdapter.TryGetPrimaryTouch(out int fingerId, out Vector2 position,
                out TouchPhase phase), Is.True, step + ": " + native);
            Assert.That(phase, Is.EqualTo(expectedPhase), step + ": " + native);
            Assert.That(fingerId, Is.EqualTo(1), step + ": " + native);
            Assert.That(position, Is.EqualTo(expectedPosition), step + ": " + native);
        }

        private static int PollMouse(int count, bool held)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                bool positionAvailable = MouseInputCompat.TryGetPosition(out Vector3 position);
                bool heldAvailable = MouseInputCompat.TryGetButton(0, out bool isHeld);
                bool downAvailable = MouseInputCompat.TryGetButtonDown(0, out bool _);
                bool upAvailable = MouseInputCompat.TryGetButtonUp(0, out bool _);
                Vector2 pointerDelta = OptionalInputSystemAdapter.ReadPointerDelta();
                if (positionAvailable && heldAvailable && downAvailable && upAvailable && isHeld == held &&
                    position.x == 128f && position.y == 128f && position.z == 0f &&
                    !float.IsNaN(pointerDelta.x) && !float.IsNaN(pointerDelta.y))
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int PollTouch(int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (OptionalInputSystemAdapter.TryGetPrimaryTouch(out int fingerId, out Vector2 position,
                        out TouchPhase phase) && fingerId == 1 && phase == TouchPhase.Moved && position == MovedPosition)
                {
                    correct++;
                }
            }

            return correct;
        }

        private readonly struct GamepadExpectation
        {
            public readonly Vector2 LeftStick;
            public readonly Vector2 RightStick;
            public readonly Vector2 PointerDelta;

            public GamepadExpectation(Vector2 leftStick, Vector2 rightStick, Vector2 pointerDelta)
            {
                LeftStick = leftStick;
                RightStick = rightStick;
                PointerDelta = pointerDelta;
            }
        }
    }
}
