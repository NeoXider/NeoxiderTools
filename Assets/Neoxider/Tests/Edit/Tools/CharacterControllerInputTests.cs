using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Covers the Neoxider input layer that sits on top of the bundled CMF character controller:
    ///     the backend decision rule, the frame-delta to rate conversion, and the gating/injection contracts
    ///     of <see cref="NeoCharacterInput" /> and <see cref="NeoCameraInput" />.
    /// </summary>
    public sealed class CharacterControllerInputTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("CharacterUnderTest");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        // ---------------------------------------------------------------- backend resolver

        [Test]
        public void ExplicitLegacyBackend_NeverFallsBackToNewInput()
        {
            Assert.IsFalse(NeoInputBackendResolver.ShouldUseNewInput(
                NeoInputBackend.LegacyInputManager, true, true));
            Assert.IsFalse(NeoInputBackendResolver.ShouldUseNewInput(
                NeoInputBackend.LegacyInputManager, true, false));
        }

        [Test]
        public void AutoBackend_PrefersNewInputWhenAvailable()
        {
            Assert.IsTrue(NeoInputBackendResolver.ShouldUseNewInput(
                NeoInputBackend.AutoPreferNew, true, true));
            Assert.IsTrue(NeoInputBackendResolver.ShouldUseNewInput(
                NeoInputBackend.NewInputSystem, true, true));
        }

        [Test]
        public void AutoBackend_FallsBackToLegacyWhenNewInputMissing()
        {
            Assert.IsFalse(NeoInputBackendResolver.ShouldUseNewInput(
                NeoInputBackend.AutoPreferNew, false, true));
        }

        [Test]
        public void NewInputIsUsedWhenNeitherBackendIsAvailable()
        {
            // WHY: with legacy input disabled in Player Settings every Input call throws, so the legacy path
            // is not a survivable fallback even when the New Input System is also missing.
            Assert.IsTrue(NeoInputBackendResolver.ShouldUseNewInput(
                NeoInputBackend.AutoPreferNew, false, false));
        }

        // ---------------------------------------------------------------- look rate

        [Test]
        public void FrameDeltaBecomesRate()
        {
            Assert.AreEqual(100f, NeoLookRate.FromFrameDelta(2f, 0.02f, 1f), 1e-4f);
        }

        [Test]
        public void RateIsFrameRateIndependent()
        {
            // Same pointer travel over one second, sampled at two different frame rates, must produce the same
            // rotation once the camera controller multiplies the rate back by delta time.
            const float pixelsPerSecond = 600f;

            float slowDelta = 1f / 30f;
            float fastDelta = 1f / 240f;

            float slowRotation = NeoLookRate.FromFrameDelta(pixelsPerSecond * slowDelta, slowDelta, 1f) * slowDelta;
            float fastRotation = NeoLookRate.FromFrameDelta(pixelsPerSecond * fastDelta, fastDelta, 1f) * fastDelta;

            Assert.AreEqual(slowRotation / slowDelta, fastRotation / fastDelta, 1e-3f);
        }

        [Test]
        public void RateIsZeroWhenTimeIsStopped()
        {
            Assert.AreEqual(0f, NeoLookRate.FromFrameDelta(5f, 0.02f, 0f));
            Assert.AreEqual(0f, NeoLookRate.FromFrameDelta(5f, 0f, 1f));
        }

        [Test]
        public void RateScalesWithTimeScale()
        {
            Assert.AreEqual(50f, NeoLookRate.FromFrameDelta(2f, 0.02f, 0.5f), 1e-4f);
        }

        // ---------------------------------------------------------------- character input

        [Test]
        public void DisabledMovement_ReportsNoMovementAndNoSprint()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();
            input.SetMoveInput(new Vector2(1f, 1f));
            input.SetRunInput(true);

            input.SetMovementEnabled(false);

            Assert.IsFalse(input.MovementEnabled);
            Assert.AreEqual(0f, input.GetHorizontalMovementInput());
            Assert.AreEqual(0f, input.GetVerticalMovementInput());
            Assert.IsFalse(input.IsRunHeld);
        }

        [Test]
        public void ExternalMoveInputIsClampedToUnitLength()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();
            input.SetMoveInput(new Vector2(3f, 4f));

            var move = new Vector2(input.GetHorizontalMovementInput(), input.GetVerticalMovementInput());

            Assert.AreEqual(1f, move.magnitude, 1e-4f);
            Assert.AreEqual(0.6f, move.x, 1e-4f);
            Assert.AreEqual(0.8f, move.y, 1e-4f);
        }

        [Test]
        public void ClearingExternalMoveInputRevertsToDevice()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();
            input.SetMoveInput(new Vector2(1f, 0f));
            Assert.AreEqual(1f, input.GetHorizontalMovementInput(), 1e-4f);

            input.SetMoveInput(null);

            // No device is producing input in an EditMode test, so the device path reads as neutral.
            Assert.AreEqual(0f, input.GetHorizontalMovementInput(), 1e-4f);
        }

        [Test]
        public void ExternalJumpIsHeldUntilReleased()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();

            input.SetJumpInput(true);
            Assert.IsTrue(input.IsJumpKeyPressed(), "Jump must stay held across reads, not consume itself.");
            Assert.IsTrue(input.IsJumpKeyPressed());

            input.SetJumpInput(false);
            Assert.IsFalse(input.IsJumpKeyPressed());
        }

        [Test]
        public void DisabledJump_IgnoresExternalJump()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();
            input.SetJumpInput(true);

            input.SetJumpEnabled(false);

            Assert.IsFalse(input.JumpEnabled);
            Assert.IsFalse(input.IsJumpKeyPressed());
        }

        [Test]
        public void ExternalRunIsOnlyReadWhileExternalMoveIsActive()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();
            input.SetRunInput(true);

            Assert.IsFalse(input.IsRunHeld, "Without injected movement the device is the sprint source.");

            input.SetMoveInput(Vector2.up);
            Assert.IsTrue(input.IsRunHeld);
        }

        [Test]
        public void SprintIsBlockedWhenCanRunIsOff()
        {
            NeoCharacterInput input = _go.AddComponent<NeoCharacterInput>();
            SetPrivateField(input, "_canRun", false);
            input.SetMoveInput(Vector2.up);
            input.SetRunInput(true);

            Assert.IsFalse(input.IsRunHeld);
        }

        // ---------------------------------------------------------------- camera input

        [Test]
        public void DisabledLook_ReportsNoRotation()
        {
            NeoCameraInput input = _go.AddComponent<NeoCameraInput>();
            SetPrivateField(input, "_pauseLookWhenCursorVisible", false);
            input.SetLookInput(new Vector2(1f, 1f));

            input.SetLookEnabled(false);

            Assert.IsFalse(input.LookEnabled);
            Assert.IsFalse(input.IsLookActive);
            Assert.AreEqual(0f, input.GetHorizontalCameraInput());
            Assert.AreEqual(0f, input.GetVerticalCameraInput());
        }

        [Test]
        public void CursorGateCanBeTurnedOff()
        {
            NeoCameraInput input = _go.AddComponent<NeoCameraInput>();

            SetPrivateField(input, "_pauseLookWhenCursorVisible", false);
            input.SetLookEnabled(true);

            Assert.IsTrue(input.IsLookActive, "With the cursor gate off, look follows Look Enabled alone.");
        }

        [Test]
        public void CursorGateSuspendsLookWhileCursorIsVisible()
        {
            NeoCameraInput input = _go.AddComponent<NeoCameraInput>();
            SetPrivateField(input, "_pauseLookWhenCursorVisible", true);
            input.SetLookEnabled(true);

            Assert.AreEqual(!Cursor.visible, input.IsLookActive);
        }

        [Test]
        public void InjectedLookIsScaledBySensitivityAndInvertsPitch()
        {
            NeoCameraInput input = _go.AddComponent<NeoCameraInput>();
            SetPrivateField(input, "_pauseLookWhenCursorVisible", false);
            SetPrivateField(input, "_useGameSettingsMouseSensitivity", false);
            SetPrivateField(input, "_mouseSensitivity", 2f);
            SetPrivateField(input, "_stickInputMultiplier", 1f);
            input.SetLookEnabled(true);

            input.SetLookInput(new Vector2(0.5f, 0.25f));

            Assert.AreEqual(1f, input.GetHorizontalCameraInput(), 1e-4f);
            // CMF clamps pitch as "up is negative", so the vertical axis is inverted to match its own input scripts.
            Assert.AreEqual(-0.5f, input.GetVerticalCameraInput(), 1e-4f);
        }

        [Test]
        public void InvertFlagsFlipEachAxisIndependently()
        {
            NeoCameraInput input = _go.AddComponent<NeoCameraInput>();
            SetPrivateField(input, "_pauseLookWhenCursorVisible", false);
            SetPrivateField(input, "_useGameSettingsMouseSensitivity", false);
            SetPrivateField(input, "_mouseSensitivity", 1f);
            SetPrivateField(input, "_stickInputMultiplier", 1f);
            SetPrivateField(input, "_invertHorizontal", true);
            input.SetLookEnabled(true);

            input.SetLookInput(new Vector2(1f, 1f));

            Assert.AreEqual(-1f, input.GetHorizontalCameraInput(), 1e-4f);
            Assert.AreEqual(-1f, input.GetVerticalCameraInput(), 1e-4f);
        }
    }
}
