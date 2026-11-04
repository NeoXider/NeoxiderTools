using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using ButtonDevice = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.ButtonDevice;
using ButtonState = Neo.Tests.Edit.Audit20261023NeoToolsInputAdapterTests.ButtonState;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Warmed keyboard/button polling of <see cref="OptionalInputSystemAdapter" /> allocates nothing while every
    ///     read stays live. Windows count native GC.Alloc events after a calibrated admission
    ///     (<see cref="GcAllocCounter" />); correctness is asserted before the allocation budget.
    /// </summary>
    public sealed class Audit20261102NeoToolsInputAllocationTests
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
        public void AllocationCounter_ObservesRetainedCallerThreadPayload()
        {
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                Assert.That(counter.RetainedPayloadLength, Is.EqualTo(GcAllocCounter.CalibrationPayloadLength));
                Assert.That(counter.PayloadAllocationCount, Is.EqualTo(1));
                Assert.That(counter.CalibrationPasses, Is.EqualTo(2));
                TestContext.WriteLine("Native GC.Alloc admission: retained byte[4096] is one allocation call per window; " +
                                      "two positive calibrations plus empty/excluded/stale-window controls.");
            }
        }

        [Test]
        public void WarmKeyMapping_AllSupportedNamesAndUnsupportedKeysAllocateNothing()
        {
            KeyValuePair<KeyCode, string>[] cases = KeyCases();
            CheckMappings(cases, 64);
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = CheckMappings(cases, Iterations);
                long allocationCount = counter.End();
                Assert.That(correct, Is.EqualTo((cases.Length + 2) * Iterations));
                Assert.That(allocationCount, Is.Zero, "Exactly zero native GC.Alloc calls in warmed mapping.");
            }
        }

        [Test]
        public void WarmBoolPolling_LiveHeldDownUpPositiveAndNegativeAllocateNothing()
        {
            ButtonDevice device = new();
            PollChangingState(device, 64);
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = PollChangingState(device, Iterations);
                long allocationCount = counter.End();
                Assert.That(correct, Is.EqualTo(Iterations), "Every held/down/up bit changes repeatedly within one frame.");
                Assert.That(allocationCount, Is.Zero, "Exactly zero native GC.Alloc calls in warmed bool polling.");
            }
        }

        [Test]
        public void WarmMissingMetadata_RejectsWithoutAllocatingOrInventingState()
        {
            ButtonDevice device = new();
            PollMissing(device, 64);
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int correct = PollMissing(device, Iterations);
                long allocationCount = counter.End();
                Assert.That(correct, Is.EqualTo(Iterations));
                Assert.That(allocationCount, Is.Zero, "Exactly zero native GC.Alloc calls for cached missing metadata.");
            }
        }

        [Test]
        public void Controls_ReplacementDifferentDeviceDisappearanceAndResetRemainLive()
        {
            ButtonState first = new() { isPressed = true };
            ReplaceableDevice device = new() { leftButton = first };
            AssertRead(device, "isPressed", true, true);
            ButtonState replacement = new() { wasPressedThisFrame = true };
            device.leftButton = replacement;
            AssertRead(device, "isPressed", true, false);
            AssertRead(device, "wasPressedThisFrame", true, true);
            replacement.wasPressedThisFrame = false;
            replacement.wasReleasedThisFrame = true;
            AssertRead(device, "wasPressedThisFrame", true, false);
            AssertRead(device, "wasReleasedThisFrame", true, true);
            AssertRead(new ReplaceableDevice { leftButton = first }, "isPressed", true, true);
            device.leftButton = null;
            AssertRead(device, "isPressed", false, false);
            AssertRead(null, "isPressed", false, false);
            device.leftButton = new WrongState();
            AssertRead(device, "isPressed", false, false);
            device.leftButton = replacement;
            OptionalInputSystemAdapter.ResetStaticState();
            AssertRead(device, "wasReleasedThisFrame", true, true);
            replacement.wasReleasedThisFrame = false;
            AssertRead(device, "wasReleasedThisFrame", true, false);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BoolGetterFault_PreservesReflectionEnvelopeOnColdAndWarmRead(bool warm)
        {
            ThrowingState control = new();
            ReplaceableDevice device = new() { leftButton = control };
            if (warm)
            {
                AssertRead(device, "isPressed", true, true);
            }

            control.Throw = true;
            TargetInvocationException fault = Assert.Throws<TargetInvocationException>(() =>
                OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed", out _));
            Assert.That(fault.InnerException, Is.SameAs(control.Failure));
        }

        [Test]
        public void InvalidState_DoesNotReadDeviceAndControlGetterFaultStillPropagates()
        {
            ThrowingDevice device = new();
            AssertRead(device, "synthetic", false, false);
            Assert.That(device.Reads, Is.Zero);
            TargetInvocationException fault = Assert.Throws<TargetInvocationException>(() =>
                OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed", out _));
            Assert.That(device.Reads, Is.EqualTo(1));
            Assert.That(fault.InnerException, Is.SameAs(device.Failure));
        }

        [Test]
        public void UnbindableIndexedBoolGetter_RetainsReflectedFailureAndDoesNotBecomeFalse()
        {
            IndexedState control = new();
            ReplaceableDevice device = new() { leftButton = control };
            Exception expected = Assert.Catch(() => control.GetType().GetProperty("isPressed").GetValue(control));
            for (int i = 0; i < 2; i++)
            {
                Exception actual = Assert.Catch(() =>
                    OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed", out _));
                Assert.That(actual.GetType(), Is.EqualTo(expected.GetType()));
            }
        }

        [Test]
        public void WarmReaders_DoNotKeepReleasedDeviceOrControlAliveWithoutReset()
        {
            WeakReference[] released = InputLifetimeProbe.WarmOnFinishedThread(WarmAndRelease);
            InputLifetimeProbe.Collect();
            Assert.That(released[0].IsAlive, Is.False, "No reset is used to conceal static ownership.");
            Assert.That(released[1].IsAlive, Is.False, "A closed getter value must not root its weak-table key.");
        }

        private static int PollChangingState(ButtonDevice device, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                int expected = i & 7;
                device.leftButton.isPressed = (expected & 1) != 0;
                device.leftButton.wasPressedThisFrame = (expected & 2) != 0;
                device.leftButton.wasReleasedThisFrame = (expected & 4) != 0;
                bool heldAvailable =
                    OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed", out bool held);
                bool downAvailable =
                    OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "wasPressedThisFrame", out bool down);
                bool upAvailable =
                    OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "wasReleasedThisFrame", out bool up);
                int actual = (held ? 1 : 0) | (down ? 2 : 0) | (up ? 4 : 0);
                if (heldAvailable && downAvailable && upAvailable && actual == expected)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int PollMissing(ButtonDevice device, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                bool missing =
                    OptionalInputSystemAdapter.TryGetControlBool(device, "missingButton", "isPressed", out bool state);
                bool invalid =
                    OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "missingState", out bool invalidState);
                if (!missing && !state && !invalid && !invalidState)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static void AssertRead(object device, string property, bool available, bool expected)
        {
            bool result = OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", property, out bool state);
            Assert.That(result, Is.EqualTo(available));
            Assert.That(state, Is.EqualTo(expected));
        }

        private static int CheckMappings(KeyValuePair<KeyCode, string>[] cases, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                for (int key = 0; key < cases.Length; key++)
                {
                    if (OptionalInputSystemAdapter.GetInputSystemKeyPropertyName(cases[key].Key) == cases[key].Value)
                    {
                        correct++;
                    }
                }

                if (OptionalInputSystemAdapter.GetInputSystemKeyPropertyName(KeyCode.None) == null)
                {
                    correct++;
                }

                if (OptionalInputSystemAdapter.GetInputSystemKeyPropertyName(KeyCode.JoystickButton0) == null)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static KeyValuePair<KeyCode, string>[] KeyCases()
        {
            List<KeyValuePair<KeyCode, string>> cases = new();
            for (int i = 0; i < 26; i++)
            {
                cases.Add(new KeyValuePair<KeyCode, string>((KeyCode)((int)KeyCode.A + i), (char)('a' + i) + "Key"));
            }

            for (int i = 0; i < 10; i++)
            {
                cases.Add(new KeyValuePair<KeyCode, string>((KeyCode)((int)KeyCode.Alpha0 + i), "digit" + i + "Key"));
            }

            KeyCode[] special =
            {
                KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Escape, KeyCode.Tab, KeyCode.Backspace,
                KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt,
                KeyCode.RightAlt, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
                KeyCode.Insert, KeyCode.Delete, KeyCode.Home, KeyCode.End, KeyCode.PageUp, KeyCode.PageDown,
                KeyCode.Numlock, KeyCode.CapsLock, KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4,
                KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
            };
            string[] names =
            {
                "spaceKey", "enterKey", "numpadEnterKey", "escapeKey", "tabKey", "backspaceKey", "leftShiftKey",
                "rightShiftKey", "leftCtrlKey", "rightCtrlKey", "leftAltKey", "rightAltKey", "upArrowKey",
                "downArrowKey", "leftArrowKey", "rightArrowKey", "insertKey", "deleteKey", "homeKey", "endKey",
                "pageUpKey", "pageDownKey", "numLockKey", "capsLockKey", "f1Key", "f2Key", "f3Key", "f4Key",
                "f5Key", "f6Key", "f7Key", "f8Key", "f9Key", "f10Key", "f11Key", "f12Key"
            };
            for (int i = 0; i < special.Length; i++)
            {
                cases.Add(new KeyValuePair<KeyCode, string>(special[i], names[i]));
            }

            return cases.ToArray();
        }

        private static WeakReference[] WarmAndRelease()
        {
            ButtonState control = new() { isPressed = true };
            ReplaceableDevice device = new() { leftButton = control };
            bool available = OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed", out bool held);
            if (!available || !held)
            {
                throw new InvalidOperationException("The warm-up read must observe the held control.");
            }

            return new[] { new WeakReference(device), new WeakReference(control) };
        }

        public sealed class ReplaceableDevice
        {
            public object leftButton { get; set; }
        }

        public sealed class WrongState
        {
            public int isPressed => 1;
        }

        public sealed class ThrowingState
        {
            public readonly InvalidOperationException Failure = new("input getter fault");
            public bool Throw;
            public bool isPressed => Throw ? throw Failure : true;
        }

        public sealed class ThrowingDevice
        {
            public readonly InvalidOperationException Failure = new("control getter fault");
            public int Reads;

            public object leftButton
            {
                get
                {
                    Reads++;
                    throw Failure;
                }
            }
        }

        public sealed class IndexedState
        {
            [IndexerName("isPressed")]
            public bool this[int index] => true;
        }
    }
}
