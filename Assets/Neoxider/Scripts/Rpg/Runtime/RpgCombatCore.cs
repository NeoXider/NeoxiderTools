using System;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// Runtime hit behavior for RPG attacks.
    /// </summary>
    public enum RpgHitMode
    {
        Damage,
        Heal
    }

    /// <summary>
    /// Delivery type for RPG attacks.
    /// </summary>
    public enum RpgAttackDeliveryType
    {
        Direct,
        Area,
        Projectile
    }

    /// <summary>
    /// Input source used by built-in RPG runtime controls.
    /// </summary>
    public enum RpgInputTriggerType
    {
        MouseButton,
        KeyCode
    }

    /// <summary>
    /// Mouse button ids used by built-in RPG runtime controls.
    /// </summary>
    public enum RpgMouseButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    /// <summary>
    /// Common combat receiver contract used by RPG attacks, projectiles, and abilities.
    /// </summary>
    public interface IRpgCombatReceiver
    {
        float CurrentHp { get; }
        float MaxHp { get; }
        int Level { get; }
        bool IsDead { get; }
        bool IsInvulnerable { get; }
        bool CanPerformActions { get; }
        float TakeDamage(float amount);
        float Heal(float amount);
        bool TryApplyBuff(string buffId, out string failReason);
        bool TryApplyStatus(string statusId, out string failReason);
        void AddInvulnerabilityLock();
        void RemoveInvulnerabilityLock();
        float GetOutgoingDamageMultiplier();
        float GetMovementSpeedMultiplier();
    }

    /// <summary>
    /// Reusable event carrying an attack identifier.
    /// </summary>
    [Serializable]
    public sealed class RpgAttackEvent : UnityEvent<string>
    {
    }

    /// <summary>
    /// Runtime-targeted buff references for attacks.
    /// </summary>
    [Serializable]
    public sealed class RpgAttackEffectRefs
    {
        [SerializeField] private string[] _targetBuffIds = Array.Empty<string>();
        [SerializeField] private string[] _targetStatusIds = Array.Empty<string>();
        [SerializeField] private string[] _selfBuffIds = Array.Empty<string>();

        /// <summary>
        /// Gets buffs applied to the hit target.
        /// </summary>
        public IReadOnlyList<string> TargetBuffIds => _targetBuffIds;

        /// <summary>
        /// Gets status effects applied to the hit target.
        /// </summary>
        public IReadOnlyList<string> TargetStatusIds => _targetStatusIds;

        /// <summary>
        /// Gets buffs applied to the source actor.
        /// </summary>
        public IReadOnlyList<string> SelfBuffIds => _selfBuffIds;
    }

    /// <summary>
    /// Inspector-friendly button binding used by the built-in RPG input flow.
    /// </summary>
    [Serializable]
    public sealed class RpgButtonBinding
    {
        [SerializeField] private RpgInputTriggerType _triggerType = RpgInputTriggerType.MouseButton;
        [SerializeField] private RpgMouseButton _mouseButton = RpgMouseButton.Left;
        [SerializeField] private KeyCode _keyCode = KeyCode.None;

        /// <summary>
        /// Creates a default primary-attack binding.
        /// </summary>
        public static RpgButtonBinding CreatePrimaryAttackDefault()
        {
            return new RpgButtonBinding
            {
                _triggerType = RpgInputTriggerType.MouseButton,
                _mouseButton = RpgMouseButton.Left
            };
        }

        /// <summary>
        /// Creates a default evade binding.
        /// </summary>
        public static RpgButtonBinding CreateEvadeDefault()
        {
            return new RpgButtonBinding
            {
                _triggerType = RpgInputTriggerType.KeyCode,
                _keyCode = KeyCode.LeftShift
            };
        }

        /// <summary>
        /// Returns true when the binding was pressed this frame.
        /// </summary>
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

    internal static class RpgCombatMath
    {
        internal static float GetOutgoingDamageMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff)
        {
            return 1f + GetPercentSum(activeBuffs, resolveBuff, BuffStatType.DamagePercent) / 100f;
        }

        internal static float GetIncomingDamageMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff)
        {
            float defensePercent = GetPercentSum(activeBuffs, resolveBuff, BuffStatType.DefensePercent);
            return Mathf.Clamp01(1f - defensePercent / 100f);
        }

        internal static float GetRegenPerSecond(float baseRegen, IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff)
        {
            return Mathf.Max(0f, baseRegen + GetFlatSum(activeBuffs, resolveBuff, BuffStatType.HpRegenPerSecond));
        }

        internal static float GetMovementSpeedMultiplier(IEnumerable<ActiveBuffEntry> activeBuffs,
            IEnumerable<ActiveStatusEntry> activeStatuses,
            Func<string, BuffDefinition> resolveBuff,
            Func<string, StatusEffectDefinition> resolveStatus)
        {
            float buffMultiplier = 1f + GetPercentSum(activeBuffs, resolveBuff, BuffStatType.MovementSpeedPercent) / 100f;
            float statusMultiplier = 1f;
            if (activeStatuses != null)
            {
                foreach (ActiveStatusEntry entry in activeStatuses)
                {
                    StatusEffectDefinition definition = resolveStatus(entry.StatusId);
                    if (definition == null)
                    {
                        continue;
                    }

                    statusMultiplier *= Mathf.Max(0f, definition.MovementSpeedMultiplier);
                }
            }

            return Mathf.Max(0f, buffMultiplier * statusMultiplier);
        }

        internal static bool HasBlockingStatus(IEnumerable<ActiveStatusEntry> activeStatuses, Func<string, StatusEffectDefinition> resolveStatus)
        {
            if (activeStatuses == null)
            {
                return false;
            }

            foreach (ActiveStatusEntry entry in activeStatuses)
            {
                StatusEffectDefinition definition = resolveStatus(entry.StatusId);
                if (definition != null && definition.BlocksActions)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetPercentSum(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff, BuffStatType statType)
        {
            return GetFlatSum(activeBuffs, resolveBuff, statType);
        }

        private static float GetFlatSum(IEnumerable<ActiveBuffEntry> activeBuffs, Func<string, BuffDefinition> resolveBuff, BuffStatType statType)
        {
            float total = 0f;
            if (activeBuffs == null)
            {
                return total;
            }

            foreach (ActiveBuffEntry entry in activeBuffs)
            {
                BuffDefinition definition = resolveBuff(entry.BuffId);
                if (definition == null)
                {
                    continue;
                }

                BuffStatModifier[] modifiers = definition.Modifiers;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    BuffStatModifier modifier = modifiers[i];
                    if (modifier != null && modifier.StatType == statType)
                    {
                        total += modifier.Value;
                    }
                }
            }

            return total;
        }
    }
}
