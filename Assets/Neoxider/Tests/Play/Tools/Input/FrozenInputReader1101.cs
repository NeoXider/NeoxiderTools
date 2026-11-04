using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Frozen copy of the <c>OptionalInputSystemAdapter</c> read path shipped in 11.0.1 (unchanged up to commit
    ///     3e795e8c): every lookup builds a string cache key from <see cref="Type.AssemblyQualifiedName" />, letter and
    ///     digit key names are concatenated per read, states are read through <see cref="PropertyInfo.GetValue(object)" />
    ///     and values through a boxing <see cref="MethodBase.Invoke(object, object[])" />.
    /// </summary>
    /// <remarks>
    ///     WHY: the performance fixtures run it next to the current adapter on the same virtual devices, so the
    ///     before/after comparison is reproducible in one host instead of relying on numbers from another harness.
    ///     Only type resolution differs (it is not on the read path). Never change the read algorithm.
    /// </remarks>
    internal static class FrozenInputReader1101
    {
        private static readonly Type KeyboardType = ResolveType("UnityEngine.InputSystem.Keyboard");
        private static readonly Type MouseType = ResolveType("UnityEngine.InputSystem.Mouse");
        private static readonly Type GamepadType = ResolveType("UnityEngine.InputSystem.Gamepad");
        private static readonly Type TouchscreenType = ResolveType("UnityEngine.InputSystem.Touchscreen");
        private static readonly Dictionary<string, PropertyInfo> PropertyCache = new();
        private static readonly Dictionary<Type, MethodInfo> ReadValueMethodCache = new();

        internal static void Reset()
        {
            PropertyCache.Clear();
            ReadValueMethodCache.Clear();
        }

        internal static Vector2 ReadMove()
        {
            float horizontal = 0f;
            float vertical = 0f;

            object keyboard = GetCurrentDevice(KeyboardType);
            if (keyboard != null)
            {
                if (GetControlBool(keyboard, "aKey", "isPressed") ||
                    GetControlBool(keyboard, "leftArrowKey", "isPressed"))
                {
                    horizontal -= 1f;
                }

                if (GetControlBool(keyboard, "dKey", "isPressed") ||
                    GetControlBool(keyboard, "rightArrowKey", "isPressed"))
                {
                    horizontal += 1f;
                }

                if (GetControlBool(keyboard, "wKey", "isPressed") ||
                    GetControlBool(keyboard, "upArrowKey", "isPressed"))
                {
                    vertical += 1f;
                }

                if (GetControlBool(keyboard, "sKey", "isPressed") ||
                    GetControlBool(keyboard, "downArrowKey", "isPressed"))
                {
                    vertical -= 1f;
                }
            }

            object gamepad = GetCurrentDevice(GamepadType);
            if (gamepad != null)
            {
                Vector2 leftStick = ReadControlVector2(gamepad, "leftStick");
                horizontal = Mathf.Abs(leftStick.x) > Mathf.Abs(horizontal) ? leftStick.x : horizontal;
                vertical = Mathf.Abs(leftStick.y) > Mathf.Abs(vertical) ? leftStick.y : vertical;
            }

            return new Vector2(horizontal, vertical);
        }

        internal static bool TryReadKeyState(KeyCode keyCode, string statePropertyName, out bool state)
        {
            state = false;
            object keyboard = GetCurrentDevice(KeyboardType);
            if (keyboard == null)
            {
                return false;
            }

            string keyPropertyName = GetInputSystemKeyPropertyName(keyCode);
            if (string.IsNullOrEmpty(keyPropertyName))
            {
                return false;
            }

            return TryGetControlBool(keyboard, keyPropertyName, statePropertyName, out state);
        }

        internal static bool TryGetMousePosition(out Vector3 position)
        {
            position = Vector3.zero;
            object mouse = GetCurrentDevice(MouseType);
            if (mouse == null)
            {
                return false;
            }

            Vector2 value = ReadControlVector2(mouse, "position");
            position = new Vector3(value.x, value.y, 0f);
            return true;
        }

        internal static bool TryGetMouseButtonState(int buttonIndex, string statePropertyName, out bool state)
        {
            state = false;
            object mouse = GetCurrentDevice(MouseType);
            if (mouse == null)
            {
                return false;
            }

            string buttonName = buttonIndex switch
            {
                0 => "leftButton",
                1 => "rightButton",
                2 => "middleButton",
                _ => null
            };

            if (buttonName == null)
            {
                return false;
            }

            return TryGetControlBool(mouse, buttonName, statePropertyName, out state);
        }

        internal static bool TryGetPrimaryTouch(out int fingerId, out Vector2 position, out TouchPhase phase)
        {
            object touchscreen = GetCurrentDevice(TouchscreenType);
            object touch = touchscreen == null ? null : GetControl(touchscreen, "primaryTouch");
            return TryReadTouch(touch, out fingerId, out position, out phase);
        }

        private static bool TryReadTouch(object touch, out int fingerId, out Vector2 position, out TouchPhase phase)
        {
            fingerId = default;
            position = default;
            phase = default;
            if (touch == null)
            {
                return false;
            }

            object phaseValue = ReadControlValue(touch, "phase");
            switch (phaseValue?.ToString())
            {
                case "Began": phase = TouchPhase.Began; break;
                case "Moved": phase = TouchPhase.Moved; break;
                case "Stationary": phase = TouchPhase.Stationary; break;
                case "Ended": phase = TouchPhase.Ended; break;
                case "Canceled": phase = TouchPhase.Canceled; break;
                default: return false;
            }

            if (phase == TouchPhase.Began && !GetControlBool(touch, "press", "wasPressedThisFrame"))
            {
                phase = TouchPhase.Stationary;
            }
            else if ((phase == TouchPhase.Ended || phase == TouchPhase.Canceled) &&
                     !GetControlBool(touch, "press", "wasReleasedThisFrame"))
            {
                return false;
            }

            if (!(ReadControlValue(touch, "touchId") is int touchId) ||
                !(ReadControlValue(touch, "position") is Vector2 touchPosition))
            {
                return false;
            }

            fingerId = touchId;
            position = touchPosition;
            return true;
        }

        private static string GetInputSystemKeyPropertyName(KeyCode keyCode)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                return char.ToLowerInvariant((char)('a' + (keyCode - KeyCode.A))) + "Key";
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                int digit = keyCode - KeyCode.Alpha0;
                return "digit" + digit + "Key";
            }

            switch (keyCode)
            {
                case KeyCode.Space: return "spaceKey";
                case KeyCode.Return: return "enterKey";
                case KeyCode.KeypadEnter: return "numpadEnterKey";
                case KeyCode.Escape: return "escapeKey";
                case KeyCode.Tab: return "tabKey";
                case KeyCode.Backspace: return "backspaceKey";
                case KeyCode.LeftShift: return "leftShiftKey";
                case KeyCode.RightShift: return "rightShiftKey";
                case KeyCode.LeftControl: return "leftCtrlKey";
                case KeyCode.RightControl: return "rightCtrlKey";
                case KeyCode.LeftAlt: return "leftAltKey";
                case KeyCode.RightAlt: return "rightAltKey";
                case KeyCode.UpArrow: return "upArrowKey";
                case KeyCode.DownArrow: return "downArrowKey";
                case KeyCode.LeftArrow: return "leftArrowKey";
                case KeyCode.RightArrow: return "rightArrowKey";
                case KeyCode.Insert: return "insertKey";
                case KeyCode.Delete: return "deleteKey";
                case KeyCode.Home: return "homeKey";
                case KeyCode.End: return "endKey";
                case KeyCode.PageUp: return "pageUpKey";
                case KeyCode.PageDown: return "pageDownKey";
                case KeyCode.Numlock: return "numLockKey";
                case KeyCode.CapsLock: return "capsLockKey";
                case KeyCode.F1: return "f1Key";
                case KeyCode.F2: return "f2Key";
                case KeyCode.F3: return "f3Key";
                case KeyCode.F4: return "f4Key";
                case KeyCode.F5: return "f5Key";
                case KeyCode.F6: return "f6Key";
                case KeyCode.F7: return "f7Key";
                case KeyCode.F8: return "f8Key";
                case KeyCode.F9: return "f9Key";
                case KeyCode.F10: return "f10Key";
                case KeyCode.F11: return "f11Key";
                case KeyCode.F12: return "f12Key";
                default: return null;
            }
        }

        private static object GetCurrentDevice(Type deviceType)
        {
            if (deviceType == null)
            {
                return null;
            }

            PropertyInfo currentProperty =
                GetCachedProperty(deviceType, "current", BindingFlags.Public | BindingFlags.Static);
            return currentProperty?.GetValue(null);
        }

        private static bool GetControlBool(object device, string controlPropertyName, string statePropertyName)
        {
            return TryGetControlBool(device, controlPropertyName, statePropertyName, out bool state) && state;
        }

        private static bool TryGetControlBool(object device, string controlPropertyName, string statePropertyName,
            out bool state)
        {
            state = false;
            if (device == null ||
                (statePropertyName != "isPressed" && statePropertyName != "wasPressedThisFrame" &&
                 statePropertyName != "wasReleasedThisFrame"))
            {
                return false;
            }

            object control = GetControl(device, controlPropertyName);
            if (control == null)
            {
                return false;
            }

            PropertyInfo stateProperty =
                GetCachedProperty(control.GetType(), statePropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (stateProperty?.PropertyType != typeof(bool))
            {
                return false;
            }

            state = (bool)stateProperty.GetValue(control);
            return true;
        }

        private static Vector2 ReadControlVector2(object device, string controlPropertyName)
        {
            object value = ReadControlValue(device, controlPropertyName);
            return value is Vector2 vector ? vector : Vector2.zero;
        }

        private static object ReadControlValue(object device, string controlPropertyName)
        {
            if (device == null)
            {
                return null;
            }

            object control = GetControl(device, controlPropertyName);
            if (control == null)
            {
                return null;
            }

            Type controlType = control.GetType();
            if (!ReadValueMethodCache.TryGetValue(controlType, out MethodInfo readValueMethod))
            {
                readValueMethod = controlType.GetMethod("ReadValue",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                ReadValueMethodCache[controlType] = readValueMethod;
            }

            if (readValueMethod == null)
            {
                return null;
            }

            return readValueMethod.Invoke(control, null);
        }

        private static object GetControl(object device, string controlPropertyName)
        {
            PropertyInfo controlProperty =
                GetCachedProperty(device.GetType(), controlPropertyName, BindingFlags.Public | BindingFlags.Instance);
            return controlProperty?.GetValue(device);
        }

        private static PropertyInfo GetCachedProperty(Type targetType, string propertyName, BindingFlags flags)
        {
            if (targetType == null || string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            string cacheKey = $"{targetType.AssemblyQualifiedName}|{propertyName}|{(int)flags}";
            if (!PropertyCache.TryGetValue(cacheKey, out PropertyInfo propertyInfo))
            {
                propertyInfo = targetType.GetProperty(propertyName, flags);
                PropertyCache[cacheKey] = propertyInfo;
            }

            return propertyInfo;
        }

        private static Type ResolveType(string fullTypeName)
        {
            return Type.GetType(fullTypeName + ", Unity.InputSystem", false);
        }
    }
}
