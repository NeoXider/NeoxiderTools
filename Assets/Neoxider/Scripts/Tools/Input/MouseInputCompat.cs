using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Совместимость со старой и новой системой мышиного ввода.
    /// </summary>
    public static class MouseInputCompat
    {
        private static readonly Type MouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");

        public static bool TryGetPosition(out Vector3 position)
        {
            try
            {
                position = Input.mousePosition;
                return true;
            }
            catch (InvalidOperationException)
            {
                return TryGetPositionFromNewInputSystem(out position);
            }
        }

        public static bool TryGetButton(int buttonIndex, out bool isPressed)
        {
            try
            {
                isPressed = Input.GetMouseButton(buttonIndex);
                return true;
            }
            catch (InvalidOperationException)
            {
                return TryGetButtonState(buttonIndex, "isPressed", out isPressed);
            }
        }

        public static bool TryGetButtonDown(int buttonIndex, out bool wasPressedThisFrame)
        {
            try
            {
                wasPressedThisFrame = Input.GetMouseButtonDown(buttonIndex);
                return true;
            }
            catch (InvalidOperationException)
            {
                return TryGetButtonState(buttonIndex, "wasPressedThisFrame", out wasPressedThisFrame);
            }
        }

        public static bool TryGetButtonUp(int buttonIndex, out bool wasReleasedThisFrame)
        {
            try
            {
                wasReleasedThisFrame = Input.GetMouseButtonUp(buttonIndex);
                return true;
            }
            catch (InvalidOperationException)
            {
                return TryGetButtonState(buttonIndex, "wasReleasedThisFrame", out wasReleasedThisFrame);
            }
        }

        private static bool TryGetPositionFromNewInputSystem(out Vector3 position)
        {
            position = Vector3.zero;
            if (!TryGetCurrentMouse(out object mouse))
            {
                return false;
            }

            PropertyInfo positionProperty =
                MouseType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
            object positionControl = positionProperty?.GetValue(mouse);
            if (positionControl == null)
            {
                return false;
            }

            MethodInfo readValueMethod = positionControl.GetType().GetMethod("ReadValue", Type.EmptyTypes);
            if (readValueMethod == null)
            {
                return false;
            }

            object value = readValueMethod.Invoke(positionControl, null);
            if (value is Vector2 vector)
            {
                position = new Vector3(vector.x, vector.y, 0f);
                return true;
            }

            return false;
        }

        private static bool TryGetButtonState(int buttonIndex, string statePropertyName, out bool state)
        {
            state = false;
            if (!TryGetCurrentMouse(out object mouse))
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

            PropertyInfo buttonProperty =
                MouseType.GetProperty(buttonName, BindingFlags.Public | BindingFlags.Instance);
            object buttonControl = buttonProperty?.GetValue(mouse);
            if (buttonControl == null)
            {
                return false;
            }

            PropertyInfo stateProperty = buttonControl.GetType()
                .GetProperty(statePropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (stateProperty?.PropertyType != typeof(bool))
            {
                return false;
            }

            state = (bool)stateProperty.GetValue(buttonControl);
            return true;
        }

        private static bool TryGetCurrentMouse(out object mouse)
        {
            mouse = null;
            if (MouseType == null)
            {
                return false;
            }

            PropertyInfo currentProperty = MouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            mouse = currentProperty?.GetValue(null);
            return mouse != null;
        }
    }
}