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
    /// How a target is selected from available candidates.
    /// </summary>
    public enum RpgTargetSelectionMode
    {
        Nearest,
        Farthest,
        LowestCurrentHp,
        HighestCurrentHp,
        LowestHpPercent,
        HighestLevel,
        Random
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
    /// Reusable event carrying a GameObject payload.
    /// </summary>
    [Serializable]
    public sealed class RpgGameObjectEvent : UnityEvent<GameObject>
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

    /// <summary>
    /// Query used to locate a target for AI, skills, and spell presets.
    /// </summary>
    [Serializable]
    public sealed class RpgTargetQuery
    {
        [SerializeField] [Min(0.1f)] private float _range = 10f;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private bool _use2D = true;
        [SerializeField] private bool _use3D = true;
        [SerializeField] private bool _ignoreSelf = true;
        [SerializeField] private bool _includeDeadTargets;
        [SerializeField] private bool _requireCanPerformActions;
        [SerializeField] private RpgTargetSelectionMode _selectionMode = RpgTargetSelectionMode.Nearest;

        /// <summary>
        /// Gets the target search range.
        /// </summary>
        public float Range => Mathf.Max(0.1f, _range);

        /// <summary>
        /// Gets the target layer filter.
        /// </summary>
        public LayerMask TargetLayers => _targetLayers;

        /// <summary>
        /// Gets whether 2D physics queries are enabled.
        /// </summary>
        public bool Use2D => _use2D;

        /// <summary>
        /// Gets whether 3D physics queries are enabled.
        /// </summary>
        public bool Use3D => _use3D;

        /// <summary>
        /// Gets whether the source actor is ignored.
        /// </summary>
        public bool IgnoreSelf => _ignoreSelf;

        /// <summary>
        /// Gets whether dead targets are allowed.
        /// </summary>
        public bool IncludeDeadTargets => _includeDeadTargets;

        /// <summary>
        /// Gets whether the target must be able to perform actions.
        /// </summary>
        public bool RequireCanPerformActions => _requireCanPerformActions;

        /// <summary>
        /// Gets the sorting strategy used to select the final target.
        /// </summary>
        public RpgTargetSelectionMode SelectionMode => _selectionMode;
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

    internal static class RpgTargetingUtility
    {
        internal static GameObject SelectTarget(Transform sourceTransform, RpgTargetQuery query, Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            if (sourceTransform == null || query == null || resolveReceiver == null)
            {
                return null;
            }

            List<GameObject> candidates = new();
            Vector3 position = sourceTransform.position;
            float range = query.Range;

            if (query.Use3D)
            {
                Collider[] colliders = Physics.OverlapSphere(position, range, query.TargetLayers);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        AddCandidate(candidates, colliders[i].gameObject, sourceTransform, query, resolveReceiver);
                    }
                }
            }

            if (query.Use2D)
            {
                Collider2D[] colliders2D = Physics2D.OverlapCircleAll(position, range, query.TargetLayers);
                for (int i = 0; i < colliders2D.Length; i++)
                {
                    if (colliders2D[i] != null)
                    {
                        AddCandidate(candidates, colliders2D[i].gameObject, sourceTransform, query, resolveReceiver);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            if (query.SelectionMode == RpgTargetSelectionMode.Random)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            GameObject best = candidates[0];
            float bestScore = Score(candidates[0], sourceTransform.position, query.SelectionMode, resolveReceiver);
            for (int i = 1; i < candidates.Count; i++)
            {
                float score = Score(candidates[i], sourceTransform.position, query.SelectionMode, resolveReceiver);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidates[i];
                }
            }

            return best;
        }

        private static void AddCandidate(List<GameObject> candidates,
            GameObject candidate,
            Transform sourceTransform,
            RpgTargetQuery query,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            if (candidate == null || candidates.Contains(candidate))
            {
                return;
            }

            if (query.IgnoreSelf && candidate == sourceTransform.gameObject)
            {
                return;
            }

            IRpgCombatReceiver receiver = resolveReceiver(candidate);
            if (receiver == null)
            {
                return;
            }

            if (!query.IncludeDeadTargets && receiver.IsDead)
            {
                return;
            }

            if (query.RequireCanPerformActions && !receiver.CanPerformActions)
            {
                return;
            }

            candidates.Add(candidate);
        }

        private static float Score(GameObject candidate,
            Vector3 sourcePosition,
            RpgTargetSelectionMode selectionMode,
            Func<GameObject, IRpgCombatReceiver> resolveReceiver)
        {
            IRpgCombatReceiver receiver = resolveReceiver(candidate);
            if (receiver == null)
            {
                return float.MinValue;
            }

            float distance = Vector3.Distance(sourcePosition, candidate.transform.position);
            return selectionMode switch
            {
                RpgTargetSelectionMode.Nearest => -distance,
                RpgTargetSelectionMode.Farthest => distance,
                RpgTargetSelectionMode.LowestCurrentHp => -receiver.CurrentHp,
                RpgTargetSelectionMode.HighestCurrentHp => receiver.CurrentHp,
                RpgTargetSelectionMode.LowestHpPercent => -(receiver.MaxHp > 0f ? receiver.CurrentHp / receiver.MaxHp : 0f),
                RpgTargetSelectionMode.HighestLevel => receiver.Level,
                _ => -distance
            };
        }
    }
}
