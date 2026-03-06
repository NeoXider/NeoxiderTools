using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Neo.Tools
{
    internal static class OptionalInputSystemBridge
    {
        private static readonly Type KeyboardType = ResolveType("UnityEngine.InputSystem.Keyboard");
        private static readonly Type MouseType = ResolveType("UnityEngine.InputSystem.Mouse");
        private static readonly Type GamepadType = ResolveType("UnityEngine.InputSystem.Gamepad");

        public static bool IsAvailable => KeyboardType != null || MouseType != null || GamepadType != null;

        public static Vector2 ReadMove()
        {
            float horizontal = 0f;
            float vertical = 0f;

            object keyboard = GetCurrentDevice(KeyboardType);
            if (keyboard != null)
            {
                if (GetKeyPressed(keyboard, "aKey") || GetKeyPressed(keyboard, "leftArrowKey"))
                {
                    horizontal -= 1f;
                }

                if (GetKeyPressed(keyboard, "dKey") || GetKeyPressed(keyboard, "rightArrowKey"))
                {
                    horizontal += 1f;
                }

                if (GetKeyPressed(keyboard, "wKey") || GetKeyPressed(keyboard, "upArrowKey"))
                {
                    vertical += 1f;
                }

                if (GetKeyPressed(keyboard, "sKey") || GetKeyPressed(keyboard, "downArrowKey"))
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
            bool keyboardJump = keyboard != null && GetKeyWasPressedThisFrame(keyboard, "spaceKey");

            object gamepad = GetCurrentDevice(GamepadType);
            bool gamepadJump = gamepad != null && GetControlBool(gamepad, "buttonSouth", "wasPressedThisFrame");

            return keyboardJump || gamepadJump;
        }

        public static bool ReadRunHeld()
        {
            object keyboard = GetCurrentDevice(KeyboardType);
            bool keyboardRun = keyboard != null &&
                               (GetKeyPressed(keyboard, "leftShiftKey") || GetKeyPressed(keyboard, "rightShiftKey"));

            object gamepad = GetCurrentDevice(GamepadType);
            bool gamepadRun = gamepad != null && GetControlBool(gamepad, "leftStickButton", "isPressed");

            return keyboardRun || gamepadRun;
        }

        public static bool ReadKeyDown(KeyCode keyCode)
        {
            return ReadKeyState(keyCode, "wasPressedThisFrame");
        }

        public static bool ReadKeyUp(KeyCode keyCode)
        {
            return ReadKeyState(keyCode, "wasReleasedThisFrame");
        }

        public static bool ReadKeyHeld(KeyCode keyCode)
        {
            return ReadKeyState(keyCode, "isPressed");
        }

        private static object GetCurrentDevice(Type deviceType)
        {
            if (deviceType == null)
            {
                return null;
            }

            PropertyInfo currentProperty = deviceType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            return currentProperty?.GetValue(null);
        }

        private static Type ResolveType(string fullTypeName)
        {
            TryLoadAssembly("Unity.InputSystem");
            TryLoadAssembly("Unity.InputSystem.ForUI");

            Type directType = Type.GetType(fullTypeName, false);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Select(assembly => assembly.GetType(fullTypeName, false)).FirstOrDefault(type => type != null);
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

        private static bool GetKeyPressed(object keyboard, string keyPropertyName)
        {
            return GetControlBool(keyboard, keyPropertyName, "isPressed");
        }

        private static bool GetKeyWasPressedThisFrame(object keyboard, string keyPropertyName)
        {
            return GetControlBool(keyboard, keyPropertyName, "wasPressedThisFrame");
        }

        private static bool ReadKeyState(KeyCode keyCode, string statePropertyName)
        {
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

            return GetControlBool(keyboard, keyPropertyName, statePropertyName);
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
                case KeyCode.Space:
                    return "spaceKey";
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    return "enterKey";
                case KeyCode.Escape:
                    return "escapeKey";
                case KeyCode.Tab:
                    return "tabKey";
                case KeyCode.Backspace:
                    return "backspaceKey";
                case KeyCode.LeftShift:
                    return "leftShiftKey";
                case KeyCode.RightShift:
                    return "rightShiftKey";
                case KeyCode.LeftControl:
                    return "leftCtrlKey";
                case KeyCode.RightControl:
                    return "rightCtrlKey";
                case KeyCode.LeftAlt:
                    return "leftAltKey";
                case KeyCode.RightAlt:
                    return "rightAltKey";
                case KeyCode.UpArrow:
                    return "upArrowKey";
                case KeyCode.DownArrow:
                    return "downArrowKey";
                case KeyCode.LeftArrow:
                    return "leftArrowKey";
                case KeyCode.RightArrow:
                    return "rightArrowKey";
                default:
                    return null;
            }
        }

        private static bool GetControlBool(object device, string controlPropertyName, string statePropertyName)
        {
            if (device == null)
            {
                return false;
            }

            PropertyInfo controlProperty = device.GetType().GetProperty(controlPropertyName,
                BindingFlags.Public | BindingFlags.Instance);
            object control = controlProperty?.GetValue(device);
            if (control == null)
            {
                return false;
            }

            PropertyInfo stateProperty = control.GetType().GetProperty(statePropertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (stateProperty == null || stateProperty.PropertyType != typeof(bool))
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

            PropertyInfo controlProperty = device.GetType().GetProperty(controlPropertyName,
                BindingFlags.Public | BindingFlags.Instance);
            object control = controlProperty?.GetValue(device);
            if (control == null)
            {
                return Vector2.zero;
            }

            MethodInfo readValueMethod = control.GetType().GetMethod("ReadValue",
                BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            if (readValueMethod == null)
            {
                return Vector2.zero;
            }

            object value = readValueMethod.Invoke(control, null);
            return value is Vector2 vector ? vector : Vector2.zero;
        }
    }
}