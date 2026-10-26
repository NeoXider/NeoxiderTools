using System;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public class Audit20261023NeoToolsInputAdapterTests
    {
        [Test]
        public void ReturnAndKeypadEnter_ResolveDifferentPhysicalControls()
        {
            Assert.That(OptionalInputSystemAdapter.GetInputSystemKeyPropertyName(KeyCode.Return),
                Is.EqualTo("enterKey"));
            Assert.That(OptionalInputSystemAdapter.GetInputSystemKeyPropertyName(KeyCode.KeypadEnter),
                Is.EqualTo("numpadEnterKey"));
            Assert.That(OptionalInputSystemAdapter.GetInputSystemKeyPropertyName(KeyCode.None), Is.Null);
        }

        [Test]
        public void PublicKeyRead_WithKeyboard_RejectsUnsupportedState()
        {
            Type inputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem", false);
            if (inputSystem == null)
            {
                Assert.Ignore("The Input System package is optional.");
            }

            Type keyboard = inputSystem.Assembly.GetType("UnityEngine.InputSystem.Keyboard", true);
            Type inputDevice = inputSystem.Assembly.GetType("UnityEngine.InputSystem.InputDevice", true);
            object previousKeyboard = keyboard.GetProperty("current").GetValue(null);
            MethodInfo addDevice = inputSystem.GetMethod("AddDevice", new[] { typeof(string), typeof(string), typeof(string) });
            MethodInfo removeDevice = inputSystem.GetMethod("RemoveDevice", new[] { inputDevice });
            object device = addDevice.Invoke(null, new object[] { "Keyboard", null, null });
            try
            {
                Assert.That(OptionalInputSystemAdapter.TryReadKeyState(KeyCode.Space, "isPressed", out bool state), Is.True);
                Assert.That(state, Is.False);
                Assert.That(OptionalInputSystemAdapter.TryReadKeyState(KeyCode.Space, "missingState", out state), Is.False);
                Assert.That(state, Is.False);
            }
            finally
            {
                removeDevice.Invoke(null, new[] { device });
                if (previousKeyboard != null)
                {
                    keyboard.GetMethod("MakeCurrent").Invoke(previousKeyboard, null);
                }
            }
        }

        [Test]
        public void ButtonRead_DistinguishesUnpressedControlFromUnsupportedState()
        {
            ButtonDevice device = new();
            device.leftButton.isPressed = false;
            Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "isPressed",
                out bool state), Is.True);
            Assert.That(state, Is.False);

            device.leftButton.wasPressedThisFrame = true;
            Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", "wasPressedThisFrame",
                out state), Is.True);
            Assert.That(state, Is.True);

            Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, "missingButton", "isPressed",
                out state), Is.False);
            Assert.That(state, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("missingState")]
        [TestCase("synthetic")]
        public void ButtonRead_RejectsUnsupportedStateAndClearsOutput(string propertyName)
        {
            ButtonDevice device = new();
            Assert.That(OptionalInputSystemAdapter.TryGetControlBool(device, "leftButton", propertyName,
                out bool state), Is.False);
            Assert.That(state, Is.False);
        }

        [TestCase(AdapterTouchPhase.Began, TouchPhase.Began)]
        [TestCase(AdapterTouchPhase.Moved, TouchPhase.Moved)]
        [TestCase(AdapterTouchPhase.Stationary, TouchPhase.Stationary)]
        [TestCase(AdapterTouchPhase.Ended, TouchPhase.Ended)]
        [TestCase(AdapterTouchPhase.Canceled, TouchPhase.Canceled)]
        public void TouchRead_PreservesIdentityPositionAndPhase(AdapterTouchPhase sourcePhase, TouchPhase expectedPhase)
        {
            TouchControls touch = new();
            touch.touchId.Value = 42;
            touch.position.Value = new Vector2(120f, 340f);
            touch.phase.Value = sourcePhase;
            touch.press.wasPressedThisFrame = true;
            touch.press.wasReleasedThisFrame = true;

            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out int fingerId, out Vector2 position,
                out TouchPhase phase), Is.True);
            Assert.That(fingerId, Is.EqualTo(42));
            Assert.That(position, Is.EqualTo(new Vector2(120f, 340f)));
            Assert.That(phase, Is.EqualTo(expectedPhase));
        }

        [Test]
        public void TouchRead_IdleAndMissingControlsDoNotCreateContact()
        {
            TouchControls touch = new();
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out int fingerId, out Vector2 position,
                out _), Is.False);
            Assert.That(fingerId, Is.Zero);
            Assert.That(position, Is.EqualTo(Vector2.zero));

            touch.phase.Value = AdapterTouchPhase.Began;
            touch.position = null;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out fingerId, out position, out _), Is.False);
            Assert.That(fingerId, Is.Zero);
            Assert.That(position, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TouchRead_PersistentRawPhasesDoNotRestartOrBlockMouseForever()
        {
            TouchControls touch = new();
            touch.touchId.Value = 7;
            touch.phase.Value = AdapterTouchPhase.Began;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out TouchPhase phase), Is.True);
            Assert.That(phase, Is.EqualTo(TouchPhase.Stationary));

            touch.phase.Value = AdapterTouchPhase.Ended;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out _, out _, out _), Is.False);
            touch.press.wasReleasedThisFrame = true;
            Assert.That(OptionalInputSystemAdapter.TryReadTouch(touch, out int fingerId, out _, out phase), Is.True);
            Assert.That(phase, Is.EqualTo(TouchPhase.Ended));
            Assert.That(fingerId, Is.EqualTo(7));
        }

        // Deliberately different enum values: the two input backends do not share a TouchPhase enum.
        public enum AdapterTouchPhase
        {
            None = 0,
            Began = 10,
            Moved = 20,
            Stationary = 30,
            Ended = 40,
            Canceled = 50
        }

        public sealed class SampleControl<T>
        {
            public T Value;
            public T ReadValue() => Value;
        }

        public sealed class TouchControls
        {
            public SampleControl<int> touchId { get; } = new();
            public SampleControl<Vector2> position { get; set; } = new();
            public SampleControl<AdapterTouchPhase> phase { get; } = new();
            public ButtonState press { get; } = new();
        }

        public sealed class ButtonDevice
        {
            public ButtonState leftButton { get; } = new();
        }

        public sealed class ButtonState
        {
            public bool isPressed { get; set; }
            public bool wasPressedThisFrame { get; set; }
            public bool wasReleasedThisFrame { get; set; }
            public bool synthetic => true;
        }
    }
}
