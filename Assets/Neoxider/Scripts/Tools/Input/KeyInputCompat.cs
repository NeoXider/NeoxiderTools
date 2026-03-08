using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Совместимость со старой (Input Manager) и новой (Input System) системой ввода.
    ///     Сначала пробует legacy Input; при отключённом старом вводе (InvalidOperationException)
    ///     использует новую систему через рефлексию, если пакет Unity.InputSystem присутствует.
    /// </summary>
    public static class KeyInputCompat
    {
        /// <summary>Клавиша нажата в этом кадре (down).</summary>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch (InvalidOperationException)
            {
                return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasPressedThisFrame", out bool state) && state;
            }
        }

        /// <summary>Клавиша отпущена в этом кадре (up).</summary>
        public static bool GetKeyUp(KeyCode keyCode)
        {
            try
            {
                return Input.GetKeyUp(keyCode);
            }
            catch (InvalidOperationException)
            {
                return OptionalInputSystemAdapter.TryReadKeyState(keyCode, "wasReleasedThisFrame", out bool state) && state;
            }
        }

        /// <summary>Клавиша удерживается.</summary>
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