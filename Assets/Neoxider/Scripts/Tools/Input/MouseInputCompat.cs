using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Compatibility between legacy and new mouse input.
    /// </summary>
    public static class MouseInputCompat
    {
        public static bool TryGetPosition(out Vector3 position)
        {
            try
            {
                position = Input.mousePosition;
                return true;
            }
            catch (InvalidOperationException)
            {
                return OptionalInputSystemAdapter.TryGetMousePosition(out position);
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
                return OptionalInputSystemAdapter.TryGetMouseButtonState(buttonIndex, "isPressed", out isPressed);
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
                return OptionalInputSystemAdapter.TryGetMouseButtonState(buttonIndex, "wasPressedThisFrame",
                    out wasPressedThisFrame);
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
                return OptionalInputSystemAdapter.TryGetMouseButtonState(buttonIndex, "wasReleasedThisFrame",
                    out wasReleasedThisFrame);
            }
        }
    }
}
