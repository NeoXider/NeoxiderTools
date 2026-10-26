using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    /// Owns virtual Input System devices. Queued native states are consumed by the real PlayerLoop;
    /// this does not invoke InteractiveObject.Update or replace the physics/query implementation.
    /// Reflection keeps the package's optional Input System dependency optional for test compilation.
    /// </summary>
    internal sealed class Audit20261023NeoToolsInteractableObjectInputDriver : IDisposable
    {
        private readonly object _keyboard;
        private readonly object _mouse;
        private readonly object _previousKeyboard;
        private readonly object _previousMouse;
        private readonly object _settings;
        private readonly object _previousUpdateMode;
        private readonly PropertyInfo _updateMode;
        private readonly PropertyInfo _backgroundBehavior;
        private readonly PropertyInfo _editorInputBehavior;
        private readonly object _previousBackgroundBehavior;
        private readonly object _previousEditorInputBehavior;
        private readonly bool _previousRunInBackground;
        private readonly string _initialContext;
        private readonly Type _keyboardType;
        private readonly Type _mouseType;
        private readonly Type _inputStateType;
        private readonly EventInfo _afterUpdateEvent;
        private readonly Action _afterUpdate;
        private readonly Queue<string> _snapshots = new();
        private readonly FieldInfo _legacyUnavailable;
        private readonly bool _previousLegacyUnavailable;
        private readonly MethodInfo _removeDevice;
        private readonly MethodInfo _queueKeyboard;
        private readonly MethodInfo _queueMouse;
        private readonly Type _keyboardStateType;
        private readonly Type _mouseStateType;
        private readonly Type _keyType;
        private int _lastDynamicFrame = -1;
        private int _dynamicUpdates;
        private int _keyboardQueueFrame = -1;
        private int _mouseQueueFrame = -1;
        private int _tapQueueFrame = -1;
        private bool _queuedKeyHeld;
        private bool _queuedMouseHeld;
        private bool _subscribed;
        private bool _disposed;

        internal Audit20261023NeoToolsInteractableObjectInputDriver()
        {
            Type inputSystem = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
            if (inputSystem == null)
            {
                Assert.Ignore("Requires optional Unity.InputSystem virtual-device support; no native input acceptance executed.");
            }

            Assembly assembly = inputSystem.Assembly;
            _keyboardType = assembly.GetType("UnityEngine.InputSystem.Keyboard", true);
            _mouseType = assembly.GetType("UnityEngine.InputSystem.Mouse", true);
            _inputStateType = assembly.GetType("UnityEngine.InputSystem.LowLevel.InputState", true);
            Type deviceType = assembly.GetType("UnityEngine.InputSystem.InputDevice", true);
            _keyboardStateType = assembly.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState", true);
            _mouseStateType = assembly.GetType("UnityEngine.InputSystem.LowLevel.MouseState", true);
            _keyType = assembly.GetType("UnityEngine.InputSystem.Key", true);
            _previousKeyboard = _keyboardType.GetProperty("current").GetValue(null);
            _previousMouse = _mouseType.GetProperty("current").GetValue(null);
            _settings = inputSystem.GetProperty("settings").GetValue(null);
            _updateMode = _settings.GetType().GetProperty("updateMode");
            _previousUpdateMode = _updateMode.GetValue(_settings);
            _backgroundBehavior = _settings.GetType().GetProperty("backgroundBehavior");
            _editorInputBehavior = _settings.GetType().GetProperty("editorInputBehaviorInPlayMode");
            Assert.That(_backgroundBehavior, Is.Not.Null);
            Assert.That(_editorInputBehavior, Is.Not.Null);
            _previousBackgroundBehavior = _backgroundBehavior.GetValue(_settings);
            _previousEditorInputBehavior = _editorInputBehavior.GetValue(_settings);
            _previousRunInBackground = Application.runInBackground;
            _initialContext = $"focus={Application.isFocused}, runInBackground={_previousRunInBackground}, " +
                $"update={_previousUpdateMode}, background={_previousBackgroundBehavior}, editor={_previousEditorInputBehavior}";
            _legacyUnavailable = typeof(InputBackendCompat).GetField("_legacyUnavailable",
                BindingFlags.Static | BindingFlags.NonPublic);
            _previousLegacyUnavailable = (bool)_legacyUnavailable.GetValue(null);
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
            _queueKeyboard = queue.MakeGenericMethod(_keyboardStateType);
            _queueMouse = queue.MakeGenericMethod(_mouseStateType);
            MethodInfo addDevice = inputSystem.GetMethod("AddDevice", new[] { typeof(string), typeof(string), typeof(string) });
            Assert.That(addDevice, Is.Not.Null);
            _afterUpdateEvent = inputSystem.GetEvent("onAfterUpdate");
            Assert.That(_afterUpdateEvent, Is.Not.Null);
            _afterUpdate = RecordAfterUpdate;
            try
            {
                // Queued test devices must reach the game buffers even in an unfocused/hidden Game View.
                // These public settings prevent InputManager's documented focus routing/flush paths;
                // the next native run must still prove actual raw and compat edges in the consumer frame.
                Application.runInBackground = true;
                _backgroundBehavior.SetValue(_settings, Enum.Parse(_backgroundBehavior.PropertyType, "IgnoreFocus"));
                _editorInputBehavior.SetValue(_settings,
                    Enum.Parse(_editorInputBehavior.PropertyType, "AllDeviceInputAlwaysGoesToGameView"));
                _updateMode.SetValue(_settings, Enum.Parse(_updateMode.PropertyType, "ProcessEventsInDynamicUpdate"));
                _legacyUnavailable.SetValue(null, true);
                _keyboard = addDevice.Invoke(null, new object[] { "Keyboard", "AuditInteractionKeyboard", null });
                _mouse = addDevice.Invoke(null, new object[] { "Mouse", "AuditInteractionMouse", null });
                _afterUpdateEvent.AddEventHandler(null, _afterUpdate);
                _subscribed = true;
                QueueKeyboard(false);
                QueueMouse(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void QueueKeyboard(bool held)
        {
            Array keys = Array.CreateInstance(_keyType, held ? 1 : 0);
            if (held)
            {
                keys.SetValue(Enum.Parse(_keyType, "E"), 0);
            }

            object state = Activator.CreateInstance(_keyboardStateType, new object[] { keys });
            _keyboardQueueFrame = Time.frameCount;
            _queuedKeyHeld = held;
            MakeCurrent(_keyboard);
            _queueKeyboard.Invoke(null, new[] { _keyboard, state, (object)(-1d) });
        }

        internal void QueueKeyboardTap()
        {
            QueueKeyboard(true);
            QueueKeyboard(false);
            _tapQueueFrame = Time.frameCount;
        }

        internal void QueueMouse(bool held)
        {
            object state = Activator.CreateInstance(_mouseStateType);
            FieldInfo buttons = _mouseStateType.GetField("buttons");
            buttons.SetValue(state, Convert.ChangeType(held ? 1 : 0, buttons.FieldType));
            _mouseStateType.GetField("position").SetValue(state, new Vector2(128f, 128f));
            _mouseQueueFrame = Time.frameCount;
            _queuedMouseHeld = held;
            MakeCurrent(_mouse);
            _queueMouse.Invoke(null, new[] { _mouse, state, (object)(-1d) });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_subscribed)
            {
                _afterUpdateEvent.RemoveEventHandler(null, _afterUpdate);
                _subscribed = false;
            }
            try
            {
                if (_mouse != null)
                {
                    _removeDevice.Invoke(null, new[] { _mouse });
                }
            }
            finally
            {
                try
                {
                    if (_keyboard != null)
                    {
                        _removeDevice.Invoke(null, new[] { _keyboard });
                    }
                }
                finally
                {
                    try
                    {
                        _updateMode.SetValue(_settings, _previousUpdateMode);
                        _editorInputBehavior.SetValue(_settings, _previousEditorInputBehavior);
                        _backgroundBehavior.SetValue(_settings, _previousBackgroundBehavior);
                    }
                    finally
                    {
                        Application.runInBackground = _previousRunInBackground;
                        _legacyUnavailable.SetValue(null, _previousLegacyUnavailable);
                        MakeCurrent(_previousKeyboard);
                        MakeCurrent(_previousMouse);
                    }
                }
            }
        }

        internal string Diagnostics
        {
            get
            {
                RecordSnapshot("consumer-frame assertion");
                return "Native input context before fixture: " + _initialContext + "\n" +
                       string.Join("\n", _snapshots);
            }
        }

        private void RecordAfterUpdate()
        {
            string update = Read(null, _inputStateType, "currentUpdateType")?.ToString();
            if (update == "Dynamic")
            {
                _lastDynamicFrame = Time.frameCount;
                _dynamicUpdates++;
            }

            RecordSnapshot("native onAfterUpdate");
        }

        private void RecordSnapshot(string phase)
        {
            object keyboard = Read(null, _keyboardType, "current");
            object mouse = Read(null, _mouseType, "current");
            string snapshot = $"{phase}: frame={Time.frameCount}, update={Read(null, _inputStateType, "currentUpdateType")}, " +
                $"lastDynamicFrame={_lastDynamicFrame}, dynamicUpdates={_dynamicUpdates}, legacy={InputBackendCompat.UsesLegacyInput}, " +
                $"focus={Application.isFocused}, runInBackground={Application.runInBackground}, " +
                $"configuredUpdate={_updateMode.GetValue(_settings)}, background={_backgroundBehavior.GetValue(_settings)}, " +
                $"editor={_editorInputBehavior.GetValue(_settings)}; " +
                $"queuedK(frame={_keyboardQueueFrame},held={_queuedKeyHeld},tapFrame={_tapQueueFrame}), " +
                $"queuedM(frame={_mouseQueueFrame},held={_queuedMouseHeld}); " +
                $"ownedK={DescribeDevice(_keyboard)}, currentK={DescribeDevice(keyboard)}, sameK={ReferenceEquals(keyboard, _keyboard)}, " +
                $"ownedE={DescribeButton(Read(_keyboard, _keyboardType, "eKey"))}, currentE={DescribeButton(Read(keyboard, _keyboardType, "eKey"))}, " +
                $"compatE(held/down/up)={KeyInputCompat.GetKey(KeyCode.E)}/{KeyInputCompat.GetKeyDown(KeyCode.E)}/{KeyInputCompat.GetKeyUp(KeyCode.E)}; " +
                $"ownedM={DescribeDevice(_mouse)}, currentM={DescribeDevice(mouse)}, sameM={ReferenceEquals(mouse, _mouse)}, " +
                $"ownedLeft={DescribeButton(Read(_mouse, _mouseType, "leftButton"))}, " +
                $"currentLeft={DescribeButton(Read(mouse, _mouseType, "leftButton"))}, " +
                $"compatM(available/held/down/up)={ReadCompatMouse()}";
            if (_snapshots.Count == 6)
            {
                _snapshots.Dequeue();
            }

            _snapshots.Enqueue(snapshot);
        }

        private static object Read(object instance, Type type, string property)
        {
            PropertyInfo info = type.GetProperty(property);
            return info == null || (instance == null && info.GetMethod?.IsStatic != true)
                ? null : info.GetValue(instance);
        }

        private static string DescribeDevice(object device)
        {
            return device == null ? "<none>" : $"id={Read(device, device.GetType(), "deviceId")}, " +
                $"added={Read(device, device.GetType(), "added")}, enabled={Read(device, device.GetType(), "enabled")}, " +
                $"background={Read(device, device.GetType(), "canRunInBackground")}, last={Read(device, device.GetType(), "lastUpdateTime")}";
        }

        private static string DescribeButton(object button)
        {
            return button == null ? "<none>" : $"{Read(button, button.GetType(), "isPressed")}/" +
                $"{Read(button, button.GetType(), "wasPressedThisFrame")}/{Read(button, button.GetType(), "wasReleasedThisFrame")}";
        }

        private static string ReadCompatMouse()
        {
            bool available = MouseInputCompat.TryGetButton(0, out bool held);
            MouseInputCompat.TryGetButtonDown(0, out bool down);
            MouseInputCompat.TryGetButtonUp(0, out bool up);
            return $"{available}/{held}/{down}/{up}";
        }

        private static void MakeCurrent(object device)
        {
            device?.GetType().GetMethod("MakeCurrent").Invoke(device, null);
        }
    }
}
