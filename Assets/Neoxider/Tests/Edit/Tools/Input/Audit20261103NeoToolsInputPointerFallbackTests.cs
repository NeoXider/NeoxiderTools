using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using ButtonState = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.ButtonState;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Pointer reads of <see cref="OptionalInputSystemAdapter" /> on fake mouse and touchscreen controls: a mouse
    ///     always wins, without one the primary touch serves position, delta and the left button only, the scroll
    ///     wheel never comes from touch, and warm reads allocate nothing.
    /// </summary>
    public sealed class Audit20261103NeoToolsInputPointerFallbackTests
    {
        private const int Iterations = 4096;

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

        [Test]
        public void ConnectedMouse_WinsOverTouchForEveryPointerRead()
        {
            FakeMouse mouse = new();
            mouse.position.Value = new Vector2(10f, 20f);
            mouse.delta.Value = new Vector2(1f, -1f);
            mouse.scroll.Value = new Vector2(0f, 1f);
            mouse.rightButton.isPressed = true;
            FakeTouchscreen touch = new();
            touch.position.Value = new Vector2(99f, 99f);
            touch.delta.Value = new Vector2(7f, 7f);
            touch.press.isPressed = true;

            Assert.That(OptionalInputSystemAdapter.TryReadPointerPosition(mouse, touch, out Vector3 position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(10f, 20f, 0f)));
            Assert.That(OptionalInputSystemAdapter.TryReadPointerDelta(mouse, touch, out Vector2 delta), Is.True);
            Assert.That(delta, Is.EqualTo(new Vector2(1f, -1f)));
            Assert.That(OptionalInputSystemAdapter.TryReadMouseScroll(mouse, out Vector2 scroll), Is.True);
            Assert.That(scroll, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(mouse, touch, 0, "isPressed", out bool left),
                Is.True);
            Assert.That(left, Is.False, "The left button comes from the mouse, not from the pressed touch.");
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(mouse, touch, 1, "isPressed", out bool right),
                Is.True);
            Assert.That(right, Is.True);
        }

        [Test]
        public void WithoutMouse_PrimaryTouchServesPositionDeltaAndLeftButtonOnly()
        {
            FakeTouchscreen touch = new();
            touch.position.Value = new Vector2(40f, 50f);
            touch.delta.Value = new Vector2(3f, 4f);
            touch.press.isPressed = true;
            touch.press.wasPressedThisFrame = true;

            Assert.That(OptionalInputSystemAdapter.TryReadPointerPosition(null, touch, out Vector3 position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(40f, 50f, 0f)));
            Assert.That(OptionalInputSystemAdapter.TryReadPointerDelta(null, touch, out Vector2 delta), Is.True);
            Assert.That(delta, Is.EqualTo(new Vector2(3f, 4f)));
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, touch, 0, "isPressed", out bool held),
                Is.True);
            Assert.That(held, Is.True);
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, touch, 0, "wasPressedThisFrame",
                out bool down), Is.True);
            Assert.That(down, Is.True);
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, touch, 0, "wasReleasedThisFrame",
                out bool up), Is.True);
            Assert.That(up, Is.False);

            for (int button = 1; button <= 2; button++)
            {
                Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, touch, button, "isPressed",
                    out bool other), Is.False, "Touch has no right or middle button.");
                Assert.That(other, Is.False);
            }

            Assert.That(OptionalInputSystemAdapter.TryReadMouseScroll(null, out Vector2 scroll), Is.False,
                "The scroll wheel never comes from touch.");
            Assert.That(scroll, Is.EqualTo(Vector2.zero));

            touch.press.isPressed = false;
            touch.press.wasPressedThisFrame = false;
            touch.press.wasReleasedThisFrame = true;
            touch.position.Value = new Vector2(41f, 51f);
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, touch, 0, "wasReleasedThisFrame",
                out up), Is.True);
            Assert.That(up, Is.True, "Release is read live from the touch press.");
            Assert.That(OptionalInputSystemAdapter.TryReadPointerPosition(null, touch, out position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(41f, 51f, 0f)));
        }

        [Test]
        public void WithoutPointerDevices_EveryReadIsUnavailableAndNeutral()
        {
            Assert.That(OptionalInputSystemAdapter.TryReadPointerPosition(null, null, out Vector3 position), Is.False);
            Assert.That(position, Is.EqualTo(Vector3.zero));
            Assert.That(OptionalInputSystemAdapter.TryReadPointerDelta(null, null, out Vector2 delta), Is.False);
            Assert.That(delta, Is.EqualTo(Vector2.zero));
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, null, 0, "isPressed", out bool held),
                Is.False);
            Assert.That(held, Is.False);
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(new FakeMouse(), null, 3, "isPressed",
                out bool unknown), Is.False, "Only buttons 0, 1 and 2 exist.");
            Assert.That(unknown, Is.False);
            Assert.That(OptionalInputSystemAdapter.TryReadPointerButton(null, new FakeTouchscreen(), 0, "missingState",
                out bool missing), Is.False);
            Assert.That(missing, Is.False);
        }

        [Test]
        public void SimulateMouseWithTouches_IsOnByDefaultAndRestoredBySubsystemReset()
        {
            Assert.That(OptionalInputSystemAdapter.SimulateMouseWithTouches, Is.True);
            OptionalInputSystemAdapter.SimulateMouseWithTouches = false;
            Assert.That(OptionalInputSystemAdapter.SimulateMouseWithTouches, Is.False);
            OptionalInputSystemAdapter.ResetStaticState();
            Assert.That(OptionalInputSystemAdapter.SimulateMouseWithTouches, Is.True);
        }

        /// <summary>
        ///     A given mouse always suppresses the touch fallback, and the switch turned off suppresses it without one.
        ///     The positive case needs a current touchscreen and is pinned by the Play Mode pointer tests.
        /// </summary>
        [Test]
        public void SelectTouchFallback_MouseWinsAndSwitchOffSuppressesTouch()
        {
            FakeMouse mouse = new();
            Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(mouse), Is.Null);

            OptionalInputSystemAdapter.SimulateMouseWithTouches = false;
            Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(null), Is.Null);
            Assert.That(OptionalInputSystemAdapter.SelectTouchFallback(mouse), Is.Null);
        }

        [Test]
        public void WarmMouseAndTouchPointerReads_AllocateNothing()
        {
            FakeMouse mouse = new();
            FakeTouchscreen touch = new();
            Poll(mouse, touch, 64);
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = Poll(mouse, touch, Iterations);
                long allocationCount = counter.End();
                Assert.That(correct, Is.EqualTo(Iterations), "Every mouse and touch value must be read live.");
                Assert.That(allocationCount, Is.Zero,
                    "Exactly zero native GC.Alloc calls for warmed position, delta, scroll and button reads.");
            }
        }

        private static int Poll(FakeMouse mouse, FakeTouchscreen touch, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                bool pressed = (i & 1) == 0;
                mouse.position.Value = new Vector2(i, 1f);
                mouse.delta.Value = new Vector2(1f, i);
                mouse.scroll.Value = new Vector2(0f, i & 3);
                mouse.leftButton.isPressed = pressed;
                touch.position.Value = new Vector2(-i, 2f);
                touch.delta.Value = new Vector2(2f, -i);
                touch.press.wasPressedThisFrame = pressed;

                bool mouseRead = OptionalInputSystemAdapter.TryReadPointerPosition(mouse, touch, out Vector3 position) &&
                                 OptionalInputSystemAdapter.TryReadPointerDelta(mouse, touch, out Vector2 delta) &&
                                 OptionalInputSystemAdapter.TryReadMouseScroll(mouse, out Vector2 scroll) &&
                                 OptionalInputSystemAdapter.TryReadPointerButton(mouse, touch, 0, "isPressed",
                                     out bool held) &&
                                 position.x == i && delta.y == i && scroll.y == (i & 3) && held == pressed;
                bool touchRead = OptionalInputSystemAdapter.TryReadPointerPosition(null, touch, out position) &&
                                 OptionalInputSystemAdapter.TryReadPointerDelta(null, touch, out delta) &&
                                 OptionalInputSystemAdapter.TryReadPointerButton(null, touch, 0,
                                     "wasPressedThisFrame", out bool down) &&
                                 position.x == -i && delta.y == -i && down == pressed;
                if (mouseRead && touchRead)
                {
                    correct++;
                }
            }

            return correct;
        }

        public sealed class FakeMouse
        {
            public Audit20261023NeoToolsInputAdapterTests.SampleControl<Vector2> position { get; } = new();
            public Audit20261023NeoToolsInputAdapterTests.SampleControl<Vector2> delta { get; } = new();
            public Audit20261023NeoToolsInputAdapterTests.SampleControl<Vector2> scroll { get; } = new();
            public ButtonState leftButton { get; } = new();
            public ButtonState rightButton { get; } = new();
            public ButtonState middleButton { get; } = new();
        }

        public sealed class FakeTouchscreen
        {
            public Audit20261023NeoToolsInputAdapterTests.SampleControl<Vector2> position { get; } = new();
            public Audit20261023NeoToolsInputAdapterTests.SampleControl<Vector2> delta { get; } = new();
            public ButtonState press { get; } = new();
        }
    }
}
