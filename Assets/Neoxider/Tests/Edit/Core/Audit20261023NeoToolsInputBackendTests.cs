using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsInputBackendTests
    {
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
        }

        [TearDown]
        public void TearDown()
        {
            UnavailableField.SetValue(null, _savedUnavailable);
            FailureCountField.SetValue(null, _savedFailures);
        }

        [Test]
        public void FailedLegacySelection_IsSharedAcrossAllKeyAndMouseReads()
        {
            // Simulate the one caught native rejection; the following calls use real compat APIs.
            InputBackendCompat.MarkLegacyUnavailable();
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.False);
            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.EqualTo(1));
            bool expectedPositionAvailable = OptionalInputSystemAdapter.TryGetMousePosition(out Vector3 expectedPosition);

            for (int i = 0; i < 32; i++)
            {
                Assert.That(KeyInputCompat.GetKey(KeyCode.None), Is.False);
                Assert.That(KeyInputCompat.GetKeyDown(KeyCode.None), Is.False);
                Assert.That(KeyInputCompat.GetKeyUp(KeyCode.None), Is.False);
                Assert.That(MouseInputCompat.TryGetPosition(out Vector3 position), Is.EqualTo(expectedPositionAvailable));
                Assert.That(position, Is.EqualTo(expectedPosition));
                Assert.That(MouseInputCompat.TryGetButton(-1, out bool held), Is.False);
                Assert.That(held, Is.False);
                Assert.That(MouseInputCompat.TryGetButtonDown(-1, out bool down), Is.False);
                Assert.That(down, Is.False);
                Assert.That(MouseInputCompat.TryGetButtonUp(-1, out bool up), Is.False);
                Assert.That(up, Is.False);
            }

            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.EqualTo(1),
                "Warmed-up shared fallback must not retry the rejected backend.");
        }

        [Test]
        public void ResetBackendSelection_RestoresConfiguredPreferenceAndClearsFailureCount()
        {
            bool configuredPreference = InputBackendCompat.UsesLegacyInput;
            InputBackendCompat.MarkLegacyUnavailable();
            Assert.That(InputBackendCompat.UsesLegacyInput, Is.False);

            InputBackendCompat.ResetBackendSelection();

            Assert.That(InputBackendCompat.UsesLegacyInput, Is.EqualTo(configuredPreference));
            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.Zero);
        }

        [Test]
        public void ConfiguredNewOnlyReads_DoNotRequireALegacySelectionException()
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                Assert.Ignore("Run this acceptance case in an Input System Package (New) validation host.");
            }

            for (int i = 0; i < 32; i++)
            {
                Assert.That(KeyInputCompat.GetKey(KeyCode.None), Is.False);
                Assert.That(KeyInputCompat.GetKeyDown(KeyCode.None), Is.False);
                Assert.That(KeyInputCompat.GetKeyUp(KeyCode.None), Is.False);
                Assert.That(MouseInputCompat.TryGetButton(-1, out bool pressed), Is.False);
                Assert.That(pressed, Is.False);
            }

            Assert.That(InputBackendCompat.LegacyReadFailureCount, Is.Zero);
        }
    }
}
