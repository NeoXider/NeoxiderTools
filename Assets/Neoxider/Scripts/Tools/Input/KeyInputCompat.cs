using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Compatibility between legacy (Input Manager) and new (Input System) input.
    ///     Uses the shared legacy-first backend selection and optional Input System reflection fallback.
    /// </summary>
    public static class KeyInputCompat
    {
        /// <summary>Key pressed this frame (down).</summary>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    return Input.GetKeyDown(keyCode);
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasPressedThisFrame", out bool state) && state;
        }

        /// <summary>Key released this frame (up).</summary>
        public static bool GetKeyUp(KeyCode keyCode)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    return Input.GetKeyUp(keyCode);
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasReleasedThisFrame", out bool state) && state;
        }

        /// <summary>Key held.</summary>
        public static bool GetKey(KeyCode keyCode)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    return Input.GetKey(keyCode);
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "isPressed", out bool state) && state;
        }
    }
}
