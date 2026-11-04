using System;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using AdapterTouchPhase = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.AdapterTouchPhase;
using ButtonState = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.ButtonState;
using TouchControls = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.TouchControls;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Vector2, integer and touch-phase reads of <see cref="OptionalInputSystemAdapter" />: bound getters keep the
    ///     reflected value semantics, stay live, keep the reflection fault envelope, do not retain removed controls and
    ///     allocate nothing once warm. Shapes that cannot be bound keep the reflected (allocating) fallback.
    /// </summary>
    public sealed class Audit20261102NeoToolsInputValueReaderTests
    {
        private const int Iterations = 4096;

        private static readonly AdapterTouchPhase[] RawPhases =
        {
            AdapterTouchPhase.Moved, AdapterTouchPhase.Stationary, AdapterTouchPhase.Began, AdapterTouchPhase.Ended,
            AdapterTouchPhase.Canceled, AdapterTouchPhase.None
        };

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
        public void WarmTouchReads_LiveIdPositionAndEveryPhaseAllocateNothing()
        {
            TouchControls touch = new();
            PollTouch(touch, 64);
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = PollTouch(touch, Iterations);
                long allocationCount = counter.End();
                Assert.That(correct, Is.EqualTo(Iterations), "Every phase, edge, id and position must be read live.");
                Assert.That(allocationCount, Is.Zero, "Exactly zero native GC.Alloc calls in warmed touch reads.");
            }
        }

        [Test]
        public void ReflectedFallbackShapes_KeepValueSemantics()
        {
            FlexibleTouch touch = new()
            {
                touchId = new SampleControl<int> { Value = 5 },
                position = new ObjectControl { Value = new Vector2(3f, 4f) },
                phase = new SampleControl<ByteTouchPhase> { Value = ByteTouchPhase.Moved }
            };

            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out int fingerId, out Vector2 position,
                out TouchPhase phase), Is.True);
            Assert.That(fingerId, Is.EqualTo(5));
            Assert.That(position, Is.EqualTo(new Vector2(3f, 4f)));
            Assert.That(phase, Is.EqualTo(TouchPhase.Moved));

            ((ObjectControl)touch.position).Value = 12f;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out position, out _), Is.False,
                "A non-Vector2 value is rejected exactly like the reflected read.");
            Assert.That(position, Is.EqualTo(Vector2.zero));

            ((ObjectControl)touch.position).Value = new Vector2(1f, 1f);
            touch.touchId = new SampleControl<AdapterTouchPhase> { Value = AdapterTouchPhase.Moved };
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out fingerId, out _, out _), Is.False,
                "An enum-typed id is not an Int32 id, as with the reflected 'is int' check.");
            Assert.That(fingerId, Is.Zero);

            touch.touchId = new SampleControl<int> { Value = 9 };
            ((SampleControl<ByteTouchPhase>)touch.phase).Value = ByteTouchPhase.None;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out _), Is.False);
            touch.phase = new SampleControl<string> { Value = "Ended" };
            touch.press.wasReleasedThisFrame = true;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out fingerId, out _, out phase), Is.True,
                "Any ReadValue whose text is a phase name keeps working through the reflected path.");
            Assert.That(phase, Is.EqualTo(TouchPhase.Ended));
            Assert.That(fingerId, Is.EqualTo(9));
        }

        [Test]
        public void ReflectedFallback_StillAllocatesAndIsDetected()
        {
            FlexibleTouch touch = new()
            {
                touchId = new SampleControl<int> { Value = 1 },
                position = new ObjectControl { Value = new Vector2(2f, 2f) },
                phase = new SampleControl<ByteTouchPhase> { Value = ByteTouchPhase.Moved }
            };
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out _), Is.True);
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = 0;
                for (int i = 0; i < 64; i++)
                {
                    if (OptionalInputSystemAdapter.TryReadTouch(touch, out int id, out Vector2 position,
                            out TouchPhase phase) && id == 1 && position.x == 2f && phase == TouchPhase.Moved)
                    {
                        correct++;
                    }
                }

                long allocationCount = counter.End();
                Assert.That(correct, Is.EqualTo(64));
                Assert.That(allocationCount, Is.GreaterThan(0),
                    "Positive control: boxed/reflected reads are visible to the counter, so a zero elsewhere is real.");
            }
        }

        [Test]
        public void BoundAndReflectedValueFaults_KeepTargetInvocationEnvelope()
        {
            ThrowingVectorControl bound = new();
            FlexibleTouch touch = new()
            {
                touchId = new SampleControl<int> { Value = 1 },
                position = bound,
                phase = new SampleControl<AdapterTouchPhase> { Value = AdapterTouchPhase.Moved }
            };
            for (int i = 0; i < 2; i++)
            {
                TargetInvocationException fault = Assert.Throws<TargetInvocationException>(() =>
                    OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out _));
                Assert.That(fault.InnerException, Is.SameAs(bound.Failure));
            }

            ThrowingObjectControl reflected = new();
            touch.position = reflected;
            TargetInvocationException reflectedFault = Assert.Throws<TargetInvocationException>(() =>
                OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out _));
            Assert.That(reflectedFault.InnerException, Is.SameAs(reflected.Failure));
        }

        [Test]
        public void ReplacedValueControls_AreReadLiveWithoutReset()
        {
            TouchControls touch = new();
            touch.touchId.Value = 3;
            touch.phase.Value = AdapterTouchPhase.Moved;
            touch.position.Value = new Vector2(1f, 1f);
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out Vector2 position, out _), Is.True);
            Assert.That(position, Is.EqualTo(new Vector2(1f, 1f)));

            touch.position = new Audit20261023NeoToolsInputAdapterTests.SampleControl<Vector2> { Value = new Vector2(7f, 8f) };
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out position, out _), Is.True);
            Assert.That(position, Is.EqualTo(new Vector2(7f, 8f)), "A replaced control must not reuse the old binding.");

            touch.position = null;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out position, out _), Is.False);
            Assert.That(position, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RuntimeBuiltNames_ResolveTheSameLiveControls()
        {
            Audit20261023NeoToolsInputAdapterTests.ButtonDevice device = new();
            device.leftButton.wasPressedThisFrame = true;
            string control = new string("leftButton".ToCharArray());
            string state = new string("wasPressedThisFrame".ToCharArray());
            for (int i = 0; i < 4; i++)
            {
                Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, control, state, out bool down), Is.True);
                Assert.That(down, Is.True);
                Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "wasPressedThisFrame",
                    out down), Is.True);
                Assert.That(down, Is.True);
            }

            device.leftButton.wasPressedThisFrame = false;
            Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, control, state, out bool released), Is.True);
            Assert.That(released, Is.False);
        }

        [Test]
        public void WarmValueReaders_DoNotKeepReleasedControlsAliveWithoutReset()
        {
            WeakReference[] released = InputLifetimeProbe.WarmOnFinishedThread(WarmAndRelease);
            InputLifetimeProbe.Collect();
            Assert.That(released[0].IsAlive, Is.False, "Touch owner must be collectable.");
            Assert.That(released[1].IsAlive, Is.False, "Vector2 control with a bound getter must be collectable.");
            Assert.That(released[2].IsAlive, Is.False, "Touch-phase control with a bound getter must be collectable.");
        }

        private static int PollTouch(TouchControls touch, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                AdapterTouchPhase raw = RawPhases[i % RawPhases.Length];
                bool down = (i & 8) != 0;
                bool up = (i & 16) != 0;
                touch.touchId.Value = i;
                touch.position.Value = new Vector2(i, -i);
                touch.phase.Value = raw;
                touch.press.wasPressedThisFrame = down;
                touch.press.wasReleasedThisFrame = up;
                bool expectedAvailable = ExpectedPhase(raw, down, up, out TouchPhase expectedPhase);
                bool available = OptionalInputSystemAdapter.TryReadTouch(touch, out int id, out Vector2 position,
                    out TouchPhase phase);
                if (available != expectedAvailable)
                {
                    continue;
                }

                if (!available || (id == i && position.x == i && position.y == -i && phase == expectedPhase))
                {
                    correct++;
                }
            }

            return correct;
        }

        private static bool ExpectedPhase(AdapterTouchPhase raw, bool down, bool up, out TouchPhase phase)
        {
            phase = default;
            switch (raw)
            {
                case AdapterTouchPhase.Began:
                    phase = down ? TouchPhase.Began : TouchPhase.Stationary;
                    return true;
                case AdapterTouchPhase.Moved:
                    phase = TouchPhase.Moved;
                    return true;
                case AdapterTouchPhase.Stationary:
                    phase = TouchPhase.Stationary;
                    return true;
                case AdapterTouchPhase.Ended:
                    phase = TouchPhase.Ended;
                    return up;
                case AdapterTouchPhase.Canceled:
                    phase = TouchPhase.Canceled;
                    return up;
                default:
                    return false;
            }
        }

        private static WeakReference[] WarmAndRelease()
        {
            TouchControls touch = new();
            touch.touchId.Value = 1;
            touch.phase.Value = AdapterTouchPhase.Moved;
            if (!OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out TouchPhase phase) ||
                phase != TouchPhase.Moved)
            {
                throw new InvalidOperationException("The warm-up read must bind the touch controls.");
            }

            return new[] { new WeakReference(touch), new WeakReference(touch.position), new WeakReference(touch.phase) };
        }

        public enum ByteTouchPhase : byte
        {
            None,
            Began,
            Moved,
            Stationary,
            Ended,
            Canceled
        }

        public sealed class SampleControl<T>
        {
            public T Value;

            public T ReadValue()
            {
                return Value;
            }
        }

        public sealed class ObjectControl
        {
            public object Value;

            public object ReadValue()
            {
                return Value;
            }
        }

        public sealed class ThrowingVectorControl
        {
            public readonly InvalidOperationException Failure = new("vector getter fault");

            public Vector2 ReadValue()
            {
                throw Failure;
            }
        }

        public sealed class ThrowingObjectControl
        {
            public readonly InvalidOperationException Failure = new("reflected getter fault");

            public object ReadValue()
            {
                throw Failure;
            }
        }

        public sealed class FlexibleTouch
        {
            public object touchId { get; set; }
            public object position { get; set; }
            public object phase { get; set; }
            public ButtonState press { get; } = new();
        }
    }
}
