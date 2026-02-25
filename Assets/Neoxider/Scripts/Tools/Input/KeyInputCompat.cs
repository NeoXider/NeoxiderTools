using System;
using System.Reflection;
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
        private static readonly Type KeyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");

        /// <summary>Клавиша нажата в этом кадре (down).</summary>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch (InvalidOperationException)
            {
                return ReadNewInputKeyState(keyCode, "wasPressedThisFrame");
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
                return ReadNewInputKeyState(keyCode, "wasReleasedThisFrame");
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
                return ReadNewInputKeyState(keyCode, "isPressed");
            }
        }

        private static bool ReadNewInputKeyState(KeyCode keyCode, string statePropertyName)
        {
            if (KeyboardType == null)
                return false;

            string keyProperty = GetInputSystemKeyPropertyName(keyCode);
            if (string.IsNullOrEmpty(keyProperty))
                return false;

            PropertyInfo currentProperty = KeyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            object keyboard = currentProperty?.GetValue(null);
            if (keyboard == null)
                return false;

            PropertyInfo keyControlProperty = KeyboardType.GetProperty(keyProperty, BindingFlags.Public | BindingFlags.Instance);
            object keyControl = keyControlProperty?.GetValue(keyboard);
            if (keyControl == null)
                return false;

            PropertyInfo stateProperty = keyControl.GetType().GetProperty(statePropertyName, BindingFlags.Public | BindingFlags.Instance);
            return stateProperty != null && stateProperty.PropertyType == typeof(bool) && (bool)stateProperty.GetValue(keyControl);
        }

        private static string GetInputSystemKeyPropertyName(KeyCode keyCode)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
                return char.ToLowerInvariant((char)('a' + (keyCode - KeyCode.A))) + "Key";

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
                return "digit" + (keyCode - KeyCode.Alpha0) + "Key";

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
    }
}
