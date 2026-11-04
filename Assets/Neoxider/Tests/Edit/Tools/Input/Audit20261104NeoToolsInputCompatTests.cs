using System;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Compatibility reads on the legacy Input Manager branch and <see cref="MoveInputCompat" /> backend selection:
    ///     while the legacy Input Manager is configured and readable the key, mouse and move readers forward to it
    ///     without allocating; after a rejected legacy read ReadMove uses the optional adapter and ignores the axis
    ///     names.
    /// </summary>
    public sealed class Audit20261104NeoToolsInputCompatTests
    {
        private const int Iterations = 1024;
        private const string MissingAxis = "Neo Missing Move Axis";

        private static readonly FieldInfo UnavailableField = typeof(InputBackendCompat).GetField(
            "_legacyUnavailable", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo FailureCountField = typeof(InputBackendCompat).GetField(
            "_legacyReadFailureCount", BindingFlags.Static | BindingFlags.NonPublic);

        private bool _savedUnavailable;
        private int _savedFailures;

        [SetUp]
        public void SetUp()
        {
            _savedUnavailable = (bool)UnavailableField.GetValue(null);
            _savedFailures = (int)FailureCountField.GetValue(null);
            InputBackendCompat.ResetBackendSelection();
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [TearDown]
        public void TearDown()
        {
            UnavailableField.SetValue(null, _savedUnavailable);
            FailureCountField.SetValue(null, _savedFailures);
            OptionalInputSystemAdapter.ResetStaticState();
        }

        [Test]
        public void DefaultAxes_AreTheInputManagerDefaults()
        {
            Assert.That(MoveInputCompat.DefaultHorizontalAxis, Is.EqualTo("Horizontal"));
            Assert.That(MoveInputCompat.DefaultVerticalAxis, Is.EqualTo("Vertical"));
        }

        [Test]
        public void LegacyBackend_ForwardsTheNamedRawAxes()
        {
            IgnoreWithoutLegacyInput();
            Vector2 expected = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            Assert.That(MoveInputCompat.ReadMove(), Is.EqualTo(expected));
            Assert.That(MoveInputCompat.ReadMove("Horizontal", "Vertical"), Is.EqualTo(expected));
            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.Zero, "The legacy branch served the reads.");
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.True);
        }

        [Test]
        public void LegacyBackend_UnknownAxisThrowsLikeTheInputManagerWithoutRejectingTheBackend()
        {
            IgnoreWithoutLegacyInput();

            Assert.That(() => MoveInputCompat.ReadMove(MissingAxis, MoveInputCompat.DefaultVerticalAxis),
                Throws.InstanceOf<ArgumentException>());
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.True,
                "A missing axis is a configuration error, not a rejected legacy backend.");
            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.Zero);
        }

        [Test]
        public void RejectedLegacyBackend_UsesTheAdapterAndIgnoresAxisNames()
        {
            // WHY: simulate the one caught native rejection, as the shared backend tests do.
            InputBackendCompat.MarkLegacyUnavailable();
            Vector2 expected = OptionalInputSystemAdapter.ReadMove();

            for (int i = 0; i < 16; i++)
            {
                Assert.That(MoveInputCompat.ReadMove(MissingAxis, MissingAxis), Is.EqualTo(expected));
                Assert.That(MoveInputCompat.ReadMove(), Is.EqualTo(expected));
            }

            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.EqualTo(1),
                "The rejected backend is not retried.");
        }

        /// <summary>
        ///     The documented legacy branch: warmed key, mouse, scroll and pointer-delta reads forward to
        ///     <c>UnityEngine.Input</c> and add no managed allocation of their own.
        /// </summary>
        [Test]
        public void LegacyBackend_WarmKeyAndMouseReadsAllocateNothing()
        {
            IgnoreWithoutLegacyInput();
            Assert.That(PollLegacyKeyAndMouse(64), Is.EqualTo(64));

            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                int served = PollLegacyKeyAndMouse(Iterations);
                long allocationCount = counter.End();
                Assert.That(served, Is.EqualTo(Iterations), "Every read must be served by the legacy branch.");
                Assert.That(allocationCount, Is.Zero,
                    "Exactly zero native GC.Alloc calls for warmed legacy key, mouse, scroll and delta reads.");
            }

            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.Zero);
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.True);
        }

        private static int PollLegacyKeyAndMouse(int count)
        {
            int served = 0;
            for (int i = 0; i < count; i++)
            {
                _ = KeyInputCompat.GetKey(KeyCode.E);
                _ = KeyInputCompat.GetKeyDown(KeyCode.E);
                _ = KeyInputCompat.GetKeyUp(KeyCode.E);
                if (MouseInputCompat.TryGetPosition(out Vector3 _) &&
                    MouseInputCompat.TryGetButtonDown(0, out bool _) &&
                    MouseInputCompat.TryGetButton(0, out bool _) &&
                    MouseInputCompat.TryGetButtonUp(0, out bool _) &&
                    MouseInputCompat.TryGetScrollDelta(out Vector2 _) &&
                    MouseInputCompat.TryGetPointerDelta(out Vector2 _))
                {
                    served++;
                }
            }

            return served;
        }

        private static void IgnoreWithoutLegacyInput()
        {
            if (!InputBackendCompat.IsLegacyInputConfigured)
            {
                Assert.Ignore("Requires the legacy Input Manager (Active Input Handling: Input Manager or Both).");
            }
        }
    }
}
