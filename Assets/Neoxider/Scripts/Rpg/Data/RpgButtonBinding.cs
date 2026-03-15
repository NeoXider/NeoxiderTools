using System;
using Neo.Tools;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Inspector-friendly button binding used by the built-in RPG input flow.
    /// </summary>
    [Serializable]
    public sealed class RpgButtonBinding
    {
        [SerializeField] private RpgInputTriggerType _triggerType = RpgInputTriggerType.MouseButton;
        [SerializeField] private RpgMouseButton _mouseButton = RpgMouseButton.Left;
        [SerializeField] private KeyCode _keyCode = KeyCode.None;

        public static RpgButtonBinding CreatePrimaryAttackDefault()
        {
            return new RpgButtonBinding
            {
                _triggerType = RpgInputTriggerType.MouseButton,
                _mouseButton = RpgMouseButton.Left
            };
        }

        public static RpgButtonBinding CreateEvadeDefault()
        {
            return new RpgButtonBinding
            {
                _triggerType = RpgInputTriggerType.KeyCode,
                _keyCode = KeyCode.LeftShift
            };
        }

        public bool IsPressedThisFrame()
        {
            switch (_triggerType)
            {
                case RpgInputTriggerType.MouseButton:
                    return MouseInputCompat.TryGetButtonDown((int)_mouseButton, out bool mousePressed) && mousePressed;
                case RpgInputTriggerType.KeyCode:
                    return _keyCode != KeyCode.None && KeyInputCompat.GetKeyDown(_keyCode);
                default:
                    return false;
            }
        }
    }
}
