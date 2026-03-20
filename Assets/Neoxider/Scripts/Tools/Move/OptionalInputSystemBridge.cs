using UnityEngine;

namespace Neo.Tools
{
    internal static class OptionalInputSystemBridge
    {
        public static bool IsAvailable => OptionalInputSystemAdapter.IsAvailable;

        public static Vector2 ReadMove()
        {
            return OptionalInputSystemAdapter.ReadMove();
        }

        public static Vector2 ReadLookDelta(float mouseScale)
        {
            return OptionalInputSystemAdapter.ReadLookDelta(mouseScale);
        }

        public static bool ReadJumpPressed()
        {
            return OptionalInputSystemAdapter.ReadJumpPressed();
        }

        public static bool ReadRunHeld()
        {
            return OptionalInputSystemAdapter.ReadRunHeld();
        }

        public static bool ReadKeyDown(KeyCode keyCode)
        {
            return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasPressedThisFrame", out bool state) && state;
        }

        public static bool ReadKeyUp(KeyCode keyCode)
        {
            return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasReleasedThisFrame", out bool state) && state;
        }

        public static bool ReadKeyHeld(KeyCode keyCode)
        {
            return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "isPressed", out bool state) && state;
        }
    }
}
