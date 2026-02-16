using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Tools
{
    internal static class OptionalInputSystemBridge
    {
        private static readonly Type KeyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        private static readonly Type MouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
        private static readonly Type GamepadType = Type.GetType("UnityEngine.InputSystem.Gamepad, Unity.InputSystem");

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

        private static object GetCurrentDevice(Type deviceType)
        {
            if (deviceType == null)
            {
                return null;
            }

            PropertyInfo currentProperty = deviceType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            return currentProperty?.GetValue(null);
        }

        private static bool GetKeyPressed(object keyboard, string keyPropertyName)
        {
            return GetControlBool(keyboard, keyPropertyName, "isPressed");
        }

        private static bool GetKeyWasPressedThisFrame(object keyboard, string keyPropertyName)
        {
            return GetControlBool(keyboard, keyPropertyName, "wasPressedThisFrame");
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