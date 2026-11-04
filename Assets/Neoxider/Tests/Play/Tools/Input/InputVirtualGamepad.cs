using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Owns one virtual Input System <c>Gamepad</c> and queues <c>GamepadState</c> events that the real PlayerLoop
    ///     update consumes. Reflection keeps the optional package optional for test compilation. Create it after
    ///     <see cref="Audit20261023NeoToolsInteractableObjectInputDriver" />, which configures background/editor
    ///     routing and selects the optional backend.
    /// </summary>
    internal sealed class InputVirtualGamepad : IDisposable
    {
        private readonly object _device;
        private readonly object _previous;
        private readonly Type _gamepadType;
        private readonly Type _stateType;
        private readonly Type _buttonType;
        private readonly MethodInfo _queue;
        private readonly MethodInfo _removeDevice;
        private bool _disposed;

        internal InputVirtualGamepad()
        {
            Type inputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystem == null)
            {
                Assert.Ignore("Requires optional Unity.InputSystem virtual-device support.");
            }

            Assembly assembly = inputSystem.Assembly;
            _gamepadType = assembly.GetType("UnityEngine.InputSystem.Gamepad", true);
            _stateType = assembly.GetType("UnityEngine.InputSystem.LowLevel.GamepadState", true);
            _buttonType = assembly.GetType("UnityEngine.InputSystem.LowLevel.GamepadButton", true);
            Type deviceType = assembly.GetType("UnityEngine.InputSystem.InputDevice", true);
            _removeDevice = inputSystem.GetMethod("RemoveDevice", new[] { deviceType });
            MethodInfo queue = null;
            foreach (MethodInfo candidate in inputSystem.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (candidate.Name == "QueueStateEvent" && candidate.IsGenericMethodDefinition &&
                    candidate.GetParameters().Length == 3)
                {
                    queue = candidate;
                    break;
                }
            }

            Assert.That(queue, Is.Not.Null, "Input System must expose QueueStateEvent<TState>.");
            _queue = queue.MakeGenericMethod(_stateType);
            _previous = _gamepadType.GetProperty("current").GetValue(null);
            MethodInfo addDevice =
                inputSystem.GetMethod("AddDevice", new[] { typeof(string), typeof(string), typeof(string) });
            Assert.That(addDevice, Is.Not.Null);
            _device = addDevice.Invoke(null, new object[] { "Gamepad", "AuditInputGamepad", null });
            MakeCurrent(_device);
        }

        /// <summary>Queues one full gamepad state; it becomes visible after the next Input System update.</summary>
        internal void Queue(Vector2 leftStick, Vector2 rightStick, bool buttonSouth)
        {
            object state = Activator.CreateInstance(_stateType);
            _stateType.GetField("leftStick").SetValue(state, leftStick);
            _stateType.GetField("rightStick").SetValue(state, rightStick);
            if (buttonSouth)
            {
                state = _stateType.GetMethod("WithButton")
                    .Invoke(state, new[] { Enum.Parse(_buttonType, "South"), (object)true });
            }

            MakeCurrent(_device);
            _queue.Invoke(null, new[] { _device, state, (object)(-1d) });
        }

        /// <summary>Processed stick value read natively, without the adapter, for oracle comparison.</summary>
        internal Vector2 ReadNativeStick(string stickName)
        {
            object control = _gamepadType.GetProperty(stickName).GetValue(_device);
            return (Vector2)control.GetType().GetMethod("ReadValue", Type.EmptyTypes).Invoke(control, null);
        }

        internal string Describe()
        {
            object south = _gamepadType.GetProperty("buttonSouth").GetValue(_device);
            Type buttonType = south.GetType();
            return $"native gamepad: current={ReferenceEquals(_gamepadType.GetProperty("current").GetValue(null), _device)}, " +
                   $"leftStick={ReadNativeStick("leftStick")}, rightStick={ReadNativeStick("rightStick")}, " +
                   $"south={buttonType.GetProperty("isPressed").GetValue(south)}/" +
                   $"{buttonType.GetProperty("wasPressedThisFrame").GetValue(south)}, frame={Time.frameCount}";
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _removeDevice.Invoke(null, new[] { _device });
            }
            finally
            {
                MakeCurrent(_previous);
            }
        }

        private static void MakeCurrent(object device)
        {
            device?.GetType().GetMethod("MakeCurrent").Invoke(device, null);
        }
    }
}
