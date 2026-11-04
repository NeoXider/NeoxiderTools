using System;
using System.Collections;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Scroll wheel, pointer delta and the touch-as-mouse fallback on real virtual Input System devices and the
    ///     real PlayerLoop update: compatible reads match the native controls in the frame they change, stay live
    ///     on the next frame and allocate nothing once warm. The driver forces the optional backend temporarily; the
    ///     legacy case runs without it.
    /// </summary>
    public sealed class Audit20261103NeoToolsInputPointerPlayTests
    {
        private const int Iterations = 1024;
        private static readonly Vector2 QueuedDelta = new(6f, -4f);
        private static readonly Vector2 QueuedScroll = new(0f, 2f);
        private static readonly Vector2 BeganPosition = new(140f, 90f);
        private static readonly Vector2 MovedPosition = new(200f, 120f);
        private static readonly Vector3 DriverMousePosition = new(128f, 128f, 0f);

        [SetUp]
        public void SetUp()
        {
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [TearDown]
        public void TearDown()
        {
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [UnityTest]
        public IEnumerator ScrollAndPointerDelta_RealVirtualMouse_MatchNativeReadLiveAndAllocateNothing()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                input.QueueMouseMotion(QueuedDelta, QueuedScroll);
                yield return null;

                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False, input.Diagnostics);
                Vector2 nativeDelta = input.ReadNativeMouseVector("delta");
                Vector2 nativeScroll = input.ReadNativeMouseVector("scroll");
                Assert.That(nativeDelta, Is.Not.EqualTo(Vector2.zero),
                    "Positive control: the queued delta reached the virtual mouse. " + input.Diagnostics);
                Assert.That(nativeScroll, Is.Not.EqualTo(Vector2.zero),
                    "Positive control: the queued scroll reached the virtual mouse. " + input.Diagnostics);
                Assert.That(MouseInputCompat.TryGetPointerDelta(out Vector2 delta), Is.True, input.Diagnostics);
                Assert.That(delta, Is.EqualTo(nativeDelta));
                Assert.That(MouseInputCompat.TryGetScrollDelta(out Vector2 scroll), Is.True, input.Diagnostics);
                Assert.That(scroll, Is.EqualTo(nativeScroll));

                Assert.That(PollMotion(64, nativeDelta, nativeScroll), Is.EqualTo(64));
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = PollMotion(Iterations, nativeDelta, nativeScroll);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), input.Diagnostics);
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls for warmed scroll and pointer-delta reads.");
                }

                yield return null;
                Assert.That(MouseInputCompat.TryGetPointerDelta(out delta), Is.True);
                Assert.That(delta, Is.EqualTo(input.ReadNativeMouseVector("delta")), "Deltas are read live.");
                Assert.That(MouseInputCompat.TryGetScrollDelta(out scroll), Is.True);
                Assert.That(scroll, Is.EqualTo(input.ReadNativeMouseVector("scroll")), "Scroll is read live.");
            }
        }

        [UnityTest]
        public IEnumerator TouchFallback_RealVirtualTouchscreen_ServesPositionDeltaAndLeftButton()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            using (InputVirtualTouchscreen touch = new())
            {
                object device = touch.Device;
                yield return null;
                yield return null;
                AssertMatchesNative(device, "idle", touch);
                Assert.That(ReadNativePress(device, "isPressed"), Is.False, "An idle touchscreen has no contact.");

                touch.Queue(1, "Began", BeganPosition);
                yield return null;
                Assert.That(ReadNativePress(device, "isPressed"), Is.True,
                    "Positive control: the began touch is a contact. " + touch.DescribePrimaryTouch());
                Assert.That(ReadNativePress(device, "wasPressedThisFrame"), Is.True,
                    "Positive control: the began touch is a press edge, so a touch can raise OnPress. " +
                    touch.DescribePrimaryTouch());
                AssertMatchesNative(device, "began frame", touch);
                Assert.That(ReadPosition(device), Is.EqualTo(new Vector3(BeganPosition.x, BeganPosition.y, 0f)));
                Assert.That(OptionalInputSystemAdapter.TryGetMousePosition(out Vector3 mousePosition), Is.True);
                Assert.That(mousePosition, Is.EqualTo(DriverMousePosition),
                    "A connected mouse wins over an active touch. " + input.Diagnostics);

                yield return null;
                AssertMatchesNative(device, "held frame", touch);

                touch.Queue(1, "Moved", MovedPosition);
                yield return null;
                AssertMatchesNative(device, "moved frame", touch);
                Assert.That(ReadPosition(device), Is.EqualTo(new Vector3(MovedPosition.x, MovedPosition.y, 0f)));
                Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, device, 1, "isPressed", out _),
                    Is.False, "Touch has no right button.");
                Assert.That(OptionalInputSystemAdapter.TryReadMouseScroll(null, out _), Is.False,
                    "The scroll wheel never comes from touch.");

                bool nativeHeld = ReadNativePress(device, "isPressed");
                Assert.That(PollTouch(device, nativeHeld, 64), Is.EqualTo(64), touch.DescribePrimaryTouch());
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = PollTouch(device, nativeHeld, Iterations);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), touch.DescribePrimaryTouch());
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls for warmed touch-as-mouse reads.");
                }

                touch.Queue(1, "Ended", MovedPosition);
                yield return null;
                Assert.That(ReadNativePress(device, "wasReleasedThisFrame"), Is.True,
                    "Positive control: the ended touch releases the contact. " + touch.DescribePrimaryTouch());
                AssertMatchesNative(device, "ended frame", touch);

                yield return null;
                AssertMatchesNative(device, "after release", touch);
            }
        }

        /// <summary>
        ///     The public pointer reads take their touch device from
        ///     <see cref="OptionalInputSystemAdapter.SelectTouchFallback" />: the current touchscreen while no mouse is
        ///     given and the switch is on, never otherwise.
        /// </summary>
        [UnityTest]
        public IEnumerator TouchFallbackSelection_RealVirtualTouchscreen_FollowsSimulateMouseWithTouches()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            using (InputVirtualTouchscreen touch = new())
            {
                yield return null;
                object device = touch.Device;
                Assert.That(ReadNativeCurrentTouchscreen(device), Is.SameAs(device),
                    "Positive control: the virtual touchscreen is Touchscreen.current. " + input.Diagnostics);
                Assert.That(OptionalInputSystemAdapter.SimulateMouseWithTouches, Is.True, "On by default.");
                Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(null), Is.SameAs(device),
                    "Without a mouse the current touchscreen serves the mouse reads.");
                Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(new object()), Is.Null,
                    "A connected mouse always wins.");

                OptionalInputSystemAdapter.SimulateMouseWithTouches = false;
                Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(null), Is.Null,
                    "Switched off, touch stays out of every mouse read.");
                Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(new object()), Is.Null);

                OptionalInputSystemAdapter.SimulateMouseWithTouches = true;
                Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(null), Is.SameAs(device),
                    "Switching it back on is seen by the next read.");
            }
        }

        /// <summary>
        ///     In a project with the legacy Input Manager (Input Manager or Both) the scroll and pointer-delta reads
        ///     forward to <c>Input.mouseScrollDelta</c> and <c>Input.mousePositionDelta</c> without a rejected read.
        /// </summary>
        [UnityTest]
        public IEnumerator ScrollAndPointerDelta_LegacyBackend_ForwardToTheInputManager()
        {
            if (!InputBackendCompat.IsLegacyInputConfigured)
            {
                Assert.Ignore("Requires the legacy Input Manager (Active Input Handling: Input Manager or Both).");
            }

            yield return null;
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.True,
                "No earlier test may leave the optional backend forced.");
            int failures = InputBackendCompat.LegacyReadFailureCount;

            Assert.That(MouseInputCompat.TryGetScrollDelta(out Vector2 scroll), Is.True);
            Assert.That(scroll, Is.EqualTo(Input.mouseScrollDelta));
            Assert.That(MouseInputCompat.TryGetPointerDelta(out Vector2 delta), Is.True);
            Vector3 legacyDelta = Input.mousePositionDelta;
            Assert.That(delta, Is.EqualTo(new Vector2(legacyDelta.x, legacyDelta.y)));
            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.EqualTo(failures),
                "The legacy branch served both reads.");
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.True);
        }

        private static int PollMotion(int count, Vector2 expectedDelta, Vector2 expectedScroll)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (MouseInputCompat.TryGetPointerDelta(out Vector2 delta) &&
                    MouseInputCompat.TryGetScrollDelta(out Vector2 scroll) &&
                    delta == expectedDelta && scroll == expectedScroll)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int PollTouch(object device, bool expectedHeld, int count)
        {
            Vector3 expectedPosition = new(MovedPosition.x, MovedPosition.y, 0f);
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (OptionalInputSystemAdapter.TryReadPointerPosition(null, device, out Vector3 position) &&
                    OptionalInputSystemAdapter.TryReadPointerDelta(null, device, out Vector2 _) &&
                    OptionalInputSystemAdapter.TryReadPointerButton(null, device, 0, "isPressed", out bool held) &&
                    OptionalInputSystemAdapter.TryReadPointerButton(null, device, 0, "wasPressedThisFrame",
                        out bool _) &&
                    OptionalInputSystemAdapter.TryReadPointerButton(null, device, 0, "wasReleasedThisFrame",
                        out bool _) &&
                    position == expectedPosition && held == expectedHeld)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static void AssertMatchesNative(object device, string step, InputVirtualTouchscreen touch)
        {
            string native = step + ": " + touch.DescribePrimaryTouch();
            Vector2 nativePosition = ReadNativeVector(device, "position");
            Assert.That(ReadPosition(device), Is.EqualTo(new Vector3(nativePosition.x, nativePosition.y, 0f)), native);
            Assert.That(OptionalInputSystemAdapter.TryReadPointerDelta(null, device, out Vector2 delta), Is.True,
                native);
            Assert.That(delta, Is.EqualTo(ReadNativeVector(device, "delta")), native);
            string[] states = { "isPressed", "wasPressedThisFrame", "wasReleasedThisFrame" };
            foreach (string state in states)
            {
                Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, device, 0, state, out bool value),
                    Is.True, native);
                Assert.That(value, Is.EqualTo(ReadNativePress(device, state)), state + " of " + native);
            }
        }

        private static Vector3 ReadPosition(object device)
        {
            Assert.That(OptionalInputSystemAdapter.TryReadPointerPosition(null, device, out Vector3 position), Is.True);
            return position;
        }

        private static Vector2 ReadNativeVector(object device, string controlName)
        {
            object control = device.GetType().GetProperty(controlName).GetValue(device);
            return (Vector2)control.GetType().GetMethod("ReadValue", Type.EmptyTypes).Invoke(control, null);
        }

        private static object ReadNativeCurrentTouchscreen(object device)
        {
            Assert.That(device, Is.Not.Null, "The virtual touchscreen was not created.");
            // WHY: reflection does not inherit static members, so Touchscreen.current is read from Touchscreen itself
            // rather than from the runtime device type, which a layout may derive from it.
            Type touchscreen = device.GetType().Assembly.GetType("UnityEngine.InputSystem.Touchscreen", true);
            PropertyInfo current = touchscreen.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            Assert.That(current, Is.Not.Null, "Input System must expose Touchscreen.current.");
            return current.GetValue(null);
        }

        private static bool ReadNativePress(object device, string state)
        {
            object press = device.GetType().GetProperty("press").GetValue(device);
            PropertyInfo property = press.GetType().GetProperty(state, BindingFlags.Public | BindingFlags.Instance);
            return (bool)property.GetValue(press);
        }
    }
}
