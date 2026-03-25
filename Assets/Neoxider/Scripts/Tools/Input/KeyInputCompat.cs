using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Compatibility between legacy (Input Manager) and new (Input System) input.
    ///     Tries legacy Input first; on InvalidOperationException uses the new system via reflection if Unity.InputSystem is present.
    /// </summary>
    public static class KeyInputCompat
    {
        /// <summary>Key pressed this frame (down).</summary>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch (InvalidOperationException)
            {
                return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasPressedThisFrame", out bool state) &&
                       state;
            }
        }

        /// <summary>Key released this frame (up).</summary>
        public static bool GetKeyUp(KeyCode keyCode)
        {
            try
            {
                return Input.GetKeyUp(keyCode);
            }
            catch (InvalidOperationException)
            {
                return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasReleasedThisFrame", out bool state) &&
                       state;
            }
        }

        /// <summary>Key held.</summary>
        public static bool GetKey(KeyCode keyCode)
        {
            try
            {
                return Input.GetKey(keyCode);
            }
            catch (InvalidOperationException)
            {
                return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "isPressed", out bool state) && state;
            }
        }
    }
}
