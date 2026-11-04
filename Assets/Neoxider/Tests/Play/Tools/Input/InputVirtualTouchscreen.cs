using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Owns one virtual Input System <c>Touchscreen</c> and queues single-touch <c>TouchState</c> events that the
    ///     real PlayerLoop update consumes. Reflection keeps the optional package optional for test compilation.
    ///     Create it after <see cref="Audit20261023NeoToolsInteractableObjectInputDriver" />, which configures
    ///     background/editor routing and selects the optional backend.
    /// </summary>
    internal sealed class InputVirtualTouchscreen : IDisposable
    {
        private readonly object _device;
        private readonly object _previous;
        private readonly Type _touchscreenType;
        private readonly Type _touchStateType;
        private readonly Type _phaseType;
        private readonly MethodInfo _queue;
        private readonly MethodInfo _removeDevice;
        private bool _disposed;

        internal InputVirtualTouchscreen()
        {
            Type inputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystem == null)
            {
                Assert.Ignore("Requires optional Unity.InputSystem virtual-device support.");
            }

            Assembly assembly = inputSystem.Assembly;
            _touchscreenType = assembly.GetType("UnityEngine.InputSystem.Touchscreen", true);
            _touchStateType = assembly.GetType("UnityEngine.InputSystem.LowLevel.TouchState", true);
            _phaseType = assembly.GetType("UnityEngine.InputSystem.TouchPhase", true);
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
            _queue = queue.MakeGenericMethod(_touchStateType);
            _previous = _touchscreenType.GetProperty("current").GetValue(null);
            MethodInfo addDevice =
                inputSystem.GetMethod("AddDevice", new[] { typeof(string), typeof(string), typeof(string) });
            Assert.That(addDevice, Is.Not.Null);
            _device = addDevice.Invoke(null, new object[] { "Touchscreen", "AuditInputTouchscreen", null });
            MakeCurrent(_device);
        }

        internal object Device => _device;

        /// <summary>Queues one touch event; it becomes visible after the next Input System update.</summary>
        internal void Queue(int touchId, string phase, Vector2 position)
        {
            object state = Activator.CreateInstance(_touchStateType);
            _touchStateType.GetField("touchId").SetValue(state, touchId);
            _touchStateType.GetField("position").SetValue(state, position);
            _touchStateType.GetProperty("phase").SetValue(state, Enum.Parse(_phaseType, phase));
            MakeCurrent(_device);
            _queue.Invoke(null, new[] { _device, state, (object)(-1d) });
        }

        /// <summary>Native primary-touch state read without the adapter, for oracle comparison.</summary>
        internal string DescribePrimaryTouch()
        {
            object touch = _touchscreenType.GetProperty("primaryTouch").GetValue(_device);
            Type touchType = touch.GetType();
            object phase = ReadValue(touchType.GetProperty("phase").GetValue(touch));
            object id = ReadValue(touchType.GetProperty("touchId").GetValue(touch));
            object position = ReadValue(touchType.GetProperty("position").GetValue(touch));
            object press = touchType.GetProperty("press").GetValue(touch);
            Type pressType = press.GetType();
            return $"native primaryTouch: phase={phase}, id={id}, position={position}, " +
                   $"pressed={pressType.GetProperty("isPressed").GetValue(press)}, " +
                   $"down={pressType.GetProperty("wasPressedThisFrame").GetValue(press)}, " +
                   $"up={pressType.GetProperty("wasReleasedThisFrame").GetValue(press)}, frame={Time.frameCount}";
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

        private static object ReadValue(object control)
        {
            return control.GetType().GetMethod("ReadValue", Type.EmptyTypes)?.Invoke(control, null);
        }

        private static void MakeCurrent(object device)
        {
            device?.GetType().GetMethod("MakeCurrent").Invoke(device, null);
        }
    }
}
