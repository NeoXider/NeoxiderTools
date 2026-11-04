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
    ///     Warmed public keyboard polling through real virtual Input System devices: direct adapter reads and
    ///     <see cref="KeyInputCompat" /> agree with the native key state and allocate nothing. The driver forces the
    ///     optional backend temporarily; this is not a New-only project configuration.
    /// </summary>
    public sealed class Audit20261102NeoToolsInputAllocationPlayTests
    {
        private const int Iterations = 4096;

        [UnityTest]
        public IEnumerator WarmPublicPolling_ReleasedWithoutEdgesAllocatesNothing()
        {
            return WarmPublicPolling(0);
        }

        [UnityTest]
        public IEnumerator WarmPublicPolling_PressedWithDownEdgeAllocatesNothing()
        {
            return WarmPublicPolling(3);
        }

        [UnityTest]
        public IEnumerator WarmPublicPolling_ReleasedWithUpEdgeAllocatesNothing()
        {
            return WarmPublicPolling(4);
        }

        [UnityTest]
        public IEnumerator RepeatedInputUpdatesWithinOneUnityFrame_ReadLiveEdges()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                Type inputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem", true);
                MethodInfo update = inputSystem.GetMethod("Update", Type.EmptyTypes);
                Assert.That(update, Is.Not.Null);
                int frame = Time.frameCount;
                input.QueueKeyboard(true);
                update.Invoke(null, null);
                Assert.That(KeyInputCompat.GetKey(KeyCode.E), Is.True, input.Diagnostics);
                Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, input.Diagnostics);
                input.QueueKeyboard(false);
                update.Invoke(null, null);
                Assert.That(KeyInputCompat.GetKey(KeyCode.E), Is.False, input.Diagnostics);
                Assert.That(KeyInputCompat.GetKeyUp(KeyCode.E), Is.True, input.Diagnostics);
                Assert.That(Time.frameCount, Is.EqualTo(frame));
            }
        }

        [UnityTest]
        public IEnumerator CurrentKeyboardReplacementAndReset_DoNotReuseOldControlState()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                input.QueueKeyboard(true);
                yield return null;
                Type inputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem", true);
                Type keyboardType = inputSystem.Assembly.GetType("UnityEngine.InputSystem.Keyboard", true);
                Type deviceType = inputSystem.Assembly.GetType("UnityEngine.InputSystem.InputDevice", true);
                object original = keyboardType.GetProperty("current").GetValue(null);
                Assert.That(KeyInputCompat.GetKey(KeyCode.E), Is.True, input.Diagnostics);
                MethodInfo add = inputSystem.GetMethod("AddDevice",
                    new[] { typeof(string), typeof(string), typeof(string) });
                MethodInfo remove = inputSystem.GetMethod("RemoveDevice", new[] { deviceType });
                object replacement = add.Invoke(null, new object[] { "Keyboard", "AuditInputGcReplacement", null });
                try
                {
                    keyboardType.GetMethod("MakeCurrent").Invoke(replacement, null);
                    Assert.That(ReadNativeMask(), Is.Zero);
                    Assert.That(PollPublic(64, 0), Is.EqualTo(64));
                    OptionalInputSystemAdapter.ResetStaticState();
                    Assert.That(PollPublic(64, 0), Is.EqualTo(64));
                    keyboardType.GetMethod("MakeCurrent").Invoke(original, null);
                    Assert.That(KeyInputCompat.GetKey(KeyCode.E), Is.True);
                }
                finally
                {
                    try
                    {
                        remove.Invoke(null, new[] { replacement });
                    }
                    finally
                    {
                        keyboardType.GetMethod("MakeCurrent").Invoke(original, null);
                    }
                }
            }
        }

        private static IEnumerator WarmPublicPolling(int expectedMask)
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                yield return null;
                yield return null;
                if (expectedMask != 0)
                {
                    input.QueueKeyboard(true);
                    yield return null;
                }

                if (expectedMask == 4)
                {
                    input.QueueKeyboard(false);
                    yield return null;
                }

                Assert.That(ReadNativeMask(), Is.EqualTo(expectedMask), input.Diagnostics);
                Assert.That(OptionalInputSystemAdapter.TryReadKeyState(KeyCode.None, "isPressed", out bool unsupported),
                    Is.False);
                Assert.That(unsupported, Is.False);
                Assert.That(OptionalInputSystemAdapter.TryReadKeyState(KeyCode.E, "missingState", out bool invalid),
                    Is.False);
                Assert.That(invalid, Is.False);
                Assert.That(PollPublic(64, expectedMask), Is.EqualTo(64), input.Diagnostics);
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = PollPublic(Iterations, expectedMask);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations), input.Diagnostics);
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls in warmed public polling; no queue/update/setup in the window.");
                }

                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False,
                    "The driver selects optional-backend coverage temporarily; it is not New-only configuration proof.");
            }
        }

        private static int PollPublic(int count, int expectedMask)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                bool heldAvailable = OptionalInputSystemAdapter.TryReadKeyState(KeyCode.E, "isPressed", out bool held);
                bool downAvailable =
                    OptionalInputSystemAdapter.TryReadKeyState(KeyCode.E, "wasPressedThisFrame", out bool down);
                bool upAvailable =
                    OptionalInputSystemAdapter.TryReadKeyState(KeyCode.E, "wasReleasedThisFrame", out bool up);
                int direct = (held ? 1 : 0) | (down ? 2 : 0) | (up ? 4 : 0);
                int compat = (KeyInputCompat.GetKey(KeyCode.E) ? 1 : 0) |
                             (KeyInputCompat.GetKeyDown(KeyCode.E) ? 2 : 0) |
                             (KeyInputCompat.GetKeyUp(KeyCode.E) ? 4 : 0);
                if (heldAvailable && downAvailable && upAvailable && direct == expectedMask && compat == expectedMask)
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int ReadNativeMask()
        {
            Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem", true);
            object keyboard = keyboardType.GetProperty("current").GetValue(null);
            Assert.That(keyboard, Is.Not.Null);
            object control = keyboardType.GetProperty("eKey").GetValue(keyboard);
            Type type = control.GetType();
            bool held = (bool)type.GetProperty("isPressed").GetValue(control);
            bool down = (bool)type.GetProperty("wasPressedThisFrame").GetValue(control);
            bool up = (bool)type.GetProperty("wasReleasedThisFrame").GetValue(control);
            return (held ? 1 : 0) | (down ? 2 : 0) | (up ? 4 : 0);
        }
    }
}
