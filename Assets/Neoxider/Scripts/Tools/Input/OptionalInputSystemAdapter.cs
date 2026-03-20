using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Neo.Tools
{
    public static class OptionalInputSystemAdapter
    {
        private static readonly Type KeyboardType = ResolveType("UnityEngine.InputSystem.Keyboard");
        private static readonly Type MouseType = ResolveType("UnityEngine.InputSystem.Mouse");
        private static readonly Type GamepadType = ResolveType("UnityEngine.InputSystem.Gamepad");
        private static readonly Dictionary<string, PropertyInfo> PropertyCache = new();
        private static readonly Dictionary<Type, MethodInfo> ReadValueMethodCache = new();

        public static bool IsAvailable => KeyboardType != null || MouseType != null || GamepadType != null;

        public static Vector2 ReadMove()
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

        public static Vector2 ReadLookDelta(float mouseScale)
        {
            Vector2 delta = Vector2.zero;

            object mouse = GetCurrentDevice(MouseType);
            if (mouse != null)
            {
                delta += ReadControlVector2(mouse, "delta") * mouseScale;
            }

            object gamepad = GetCurrentDevice(GamepadType);
            if (gamepad != null)
            {
                delta += ReadControlVector2(gamepad, "rightStick");
            }

            return delta;
        }

        public static bool ReadJumpPressed()
        {
            object keyboard = GetCurrentDevice(KeyboardType);
            bool keyboardJump = keyboard != null && GetControlBool(keyboard, "spaceKey", "wasPressedThisFrame");

            object gamepad = GetCurrentDevice(GamepadType);
            bool gamepadJump = gamepad != null && GetControlBool(gamepad, "buttonSouth", "wasPressedThisFrame");

            return keyboardJump || gamepadJump;
        }

        public static bool ReadRunHeld()
        {
            object keyboard = GetCurrentDevice(KeyboardType);
            bool keyboardRun = keyboard != null &&
                               (GetControlBool(keyboard, "leftShiftKey", "isPressed") ||
                                GetControlBool(keyboard, "rightShiftKey", "isPressed"));

            object gamepad = GetCurrentDevice(GamepadType);
            bool gamepadRun = gamepad != null && GetControlBool(gamepad, "leftStickButton", "isPressed");

            return keyboardRun || gamepadRun;
        }

        public static bool TryReadKeyState(KeyCode keyCode, string statePropertyName, out bool state)
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

            state = GetControlBool(keyboard, keyPropertyName, statePropertyName);
            return true;
        }

        public static bool TryGetMousePosition(out Vector3 position)
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

        public static bool TryGetMouseButtonState(int buttonIndex, string statePropertyName, out bool state)
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

            state = GetControlBool(mouse, buttonName, statePropertyName);
            return true;
        }

        public static string GetInputSystemKeyPropertyName(KeyCode keyCode)
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
                case KeyCode.Return:
                case KeyCode.KeypadEnter: return "enterKey";
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

        private static Type ResolveType(string fullTypeName)
        {
            TryLoadAssembly("Unity.InputSystem");
            TryLoadAssembly("Unity.InputSystem.ForUI");

            var directType = Type.GetType($"{fullTypeName}, Unity.InputSystem", false);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullTypeName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void TryLoadAssembly(string assemblyName)
        {
            try
            {
                Assembly.Load(assemblyName);
            }
            catch
            {
            }
        }

        private static bool GetControlBool(object device, string controlPropertyName, string statePropertyName)
        {
            if (device == null)
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

            return (bool)stateProperty.GetValue(control);
        }

        private static Vector2 ReadControlVector2(object device, string controlPropertyName)
        {
            if (device == null)
            {
                return Vector2.zero;
            }

            object control = GetControl(device, controlPropertyName);
            if (control == null)
            {
                return Vector2.zero;
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
                return Vector2.zero;
            }

            object value = readValueMethod.Invoke(control, null);
            return value is Vector2 vector ? vector : Vector2.zero;
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
    }
}
