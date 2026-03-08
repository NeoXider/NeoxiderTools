using System;
using System.Collections.Generic;
using Neo;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// Scene-local RPG combat receiver for enemies, NPCs, destructibles, or non-persistent actors.
    /// </summary>
    [NeoDoc("Rpg/RpgCombatant.md")]
    [CreateFromMenu("Neoxider/RPG/RpgCombatant")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgCombatant))]
    public sealed class RpgCombatant : MonoBehaviour, IRpgCombatReceiver
    {
        [Header("Base Stats")]
        [SerializeField] [Min(1f)] private float _maxHp = 100f;
        [SerializeField] [Min(0f)] private float _currentHp = 100f;
        [SerializeField] [Min(1)] private int _level = 1;

        [Header("Definitions")]
        [SerializeField] private BuffDefinition[] _buffDefinitions = Array.Empty<BuffDefinition>();
        [SerializeField] private StatusEffectDefinition[] _statusDefinitions = Array.Empty<StatusEffectDefinition>();

        [Header("Runtime")]
        [SerializeField] private bool _restoreOnAwake = true;
        [SerializeField] private float _hpRegenPerSecond;
        [SerializeField] [Min(0.05f)] private float _regenInterval = 0.2f;

        [Header("Reactive State")]
        public ReactivePropertyFloat HpState = new(100f);
        public ReactivePropertyFloat HpPercentState = new(1f);
        public ReactivePropertyInt LevelState = new(1);
        public ReactivePropertyBool InvulnerableState = new(false);

        [Header("Events")]
        [SerializeField] private UnityEventFloat _onDamaged = new();
        [SerializeField] private UnityEventFloat _onHealed = new();
        [SerializeField] private UnityEvent _onDeath = new();
        [SerializeField] private RpgStringEvent _onBuffApplied = new();
        [SerializeField] private RpgStringEvent _onBuffExpired = new();
        [SerializeField] private RpgStringEvent _onStatusApplied = new();
        [SerializeField] private RpgStringEvent _onStatusExpired = new();

        private readonly List<ActiveBuffEntry> _activeBuffs = new();
        private readonly List<ActiveStatusEntry> _activeStatuses = new();
        private int _invulnerabilityLocks;
        private float _regenAccumulator;

        /// <inheritdoc />
        public float CurrentHp => _currentHp;

        /// <inheritdoc />
        public float MaxHp => _maxHp;

        /// <inheritdoc />
        public int Level => _level;

        /// <inheritdoc />
        public bool IsDead => _currentHp <= 0f;

        /// <inheritdoc />
        public bool IsInvulnerable => _invulnerabilityLocks > 0;

        /// <inheritdoc />
        public bool CanPerformActions => !IsDead && !RpgCombatMath.HasBlockingStatus(_activeStatuses, ResolveStatusDefinition);

        /// <summary>
        /// Gets the active buff ids.
        /// </summary>
        public IReadOnlyList<ActiveBuffEntry> ActiveBuffs => _activeBuffs;

        /// <summary>
        /// Gets the active status ids.
        /// </summary>
        public IReadOnlyList<ActiveStatusEntry> ActiveStatuses => _activeStatuses;

        private void Awake()
        {
            _maxHp = Mathf.Max(1f, _maxHp);
            if (_restoreOnAwake)
            {
                _currentHp = _maxHp;
            }
            else
            {
                _currentHp = Mathf.Clamp(_currentHp, 0f, _maxHp);
            }

            RefreshRuntimeState(true);
        }

        private void Update()
        {
            if (IsDead)
            {
                return;
            }

            _regenAccumulator += Time.deltaTime;
            if (_regenAccumulator < _regenInterval)
            {
                return;
            }

            float deltaTime = _regenAccumulator;
            _regenAccumulator = 0f;

            float regen = RpgCombatMath.GetRegenPerSecond(_hpRegenPerSecond, _activeBuffs, ResolveBuffDefinition);
            if (regen > 0f)
            {
                Heal(regen * deltaTime);
            }

            ProcessStatusTickDamage(deltaTime);
            ExpireEffects();
        }

        /// <inheritdoc />
        public float TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead || IsInvulnerable)
            {
                return 0f;
            }

            float adjustedAmount = amount * RpgCombatMath.GetIncomingDamageMultiplier(_activeBuffs, ResolveBuffDefinition);
            float actualDamage = Mathf.Min(adjustedAmount, _currentHp);
            _currentHp -= actualDamage;
            RefreshRuntimeState(true);
            _onDamaged?.Invoke(actualDamage);

            if (IsDead)
            {
                _onDeath?.Invoke();
            }

            return actualDamage;
        }

        /// <inheritdoc />
        public float Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            float previousHp = _currentHp;
            _currentHp = Mathf.Min(_currentHp + amount, _maxHp);
            float actualHeal = _currentHp - previousHp;
            RefreshRuntimeState(true);
            if (actualHeal > 0f)
            {
                _onHealed?.Invoke(actualHeal);
            }

            return actualHeal;
        }

        /// <summary>
        /// Restores HP to the maximum value.
        /// </summary>
        [Button]
        public void Restore()
        {
            _currentHp = _maxHp;
            RefreshRuntimeState(true);
        }

        /// <summary>
        /// Sets max HP and optionally clamps current HP.
        /// </summary>
        public void SetMaxHp(float maxHp, bool clampCurrent = true)
        {
            _maxHp = Mathf.Max(1f, maxHp);
            if (clampCurrent)
            {
                _currentHp = Mathf.Min(_currentHp, _maxHp);
            }

            RefreshRuntimeState(true);
        }

        /// <summary>
        /// Sets the combatant level.
        /// </summary>
        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            RefreshRuntimeState(true);
        }

        /// <inheritdoc />
        public bool TryApplyBuff(string buffId, out string failReason)
        {
            failReason = null;
            BuffDefinition definition = ResolveBuffDefinition(buffId);
            if (definition == null)
            {
                failReason = $"Buff definition not found: {buffId}";
                return false;
            }

            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + definition.Duration;
            ActiveBuffEntry existing = FindBuffEntry(definition.Id);
            if (existing != null && definition.Stackable)
            {
                existing.ExpiresAtUtc = Math.Max(existing.ExpiresAtUtc, expiresAt);
            }
            else
            {
                RemoveBuff(definition.Id, false);
                _activeBuffs.Add(new ActiveBuffEntry
                {
                    BuffId = definition.Id,
                    ExpiresAtUtc = expiresAt
                });
            }

            RefreshRuntimeState(true);
            _onBuffApplied?.Invoke(definition.Id);
            return true;
        }

        /// <inheritdoc />
        public bool TryApplyStatus(string statusId, out string failReason)
        {
            failReason = null;
            StatusEffectDefinition definition = ResolveStatusDefinition(statusId);
            if (definition == null)
            {
                failReason = $"Status effect definition not found: {statusId}";
                return false;
            }

            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + definition.Duration;
            ActiveStatusEntry existing = FindStatusEntry(definition.Id);
            if (existing != null)
            {
                existing.ExpiresAtUtc = expiresAt;
                if (definition.Stackable)
                {
                    existing.Stacks = Mathf.Min(existing.Stacks + 1, definition.MaxStacks);
                }
            }
            else
            {
                _activeStatuses.Add(new ActiveStatusEntry
                {
                    StatusId = definition.Id,
                    ExpiresAtUtc = expiresAt,
                    Stacks = 1
                });
            }

            RefreshRuntimeState(true);
            _onStatusApplied?.Invoke(definition.Id);
            return true;
        }

        /// <summary>
        /// Removes a buff by id.
        /// </summary>
        public void RemoveBuff(string buffId)
        {
            RemoveBuff(buffId, true);
        }

        /// <summary>
        /// Removes a status effect by id.
        /// </summary>
        public void RemoveStatus(string statusId)
        {
            for (int i = _activeStatuses.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(_activeStatuses[i].StatusId, statusId, StringComparison.Ordinal))
                {
                    continue;
                }

                _activeStatuses.RemoveAt(i);
                RefreshRuntimeState(true);
                _onStatusExpired?.Invoke(statusId);
                return;
            }
        }

        /// <summary>
        /// Returns whether the combatant has a buff.
        /// </summary>
        public bool HasBuff(string buffId)
        {
            return FindBuffEntry(buffId) != null;
        }

        /// <summary>
        /// Returns whether the combatant has a status effect.
        /// </summary>
        public bool HasStatus(string statusId)
        {
            return FindStatusEntry(statusId) != null;
        }

        /// <inheritdoc />
        [Button]
        public void AddInvulnerabilityLock()
        {
            _invulnerabilityLocks++;
            RefreshRuntimeState(true);
        }

        /// <inheritdoc />
        [Button]
        public void RemoveInvulnerabilityLock()
        {
            _invulnerabilityLocks = Mathf.Max(0, _invulnerabilityLocks - 1);
            RefreshRuntimeState(true);
        }

        /// <inheritdoc />
        public float GetOutgoingDamageMultiplier()
        {
            return RpgCombatMath.GetOutgoingDamageMultiplier(_activeBuffs, ResolveBuffDefinition);
        }

        /// <inheritdoc />
        public float GetMovementSpeedMultiplier()
        {
            return RpgCombatMath.GetMovementSpeedMultiplier(_activeBuffs, _activeStatuses, ResolveBuffDefinition, ResolveStatusDefinition);
        }

        private void RemoveBuff(string buffId, bool invokeEvent)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(_activeBuffs[i].BuffId, buffId, StringComparison.Ordinal))
                {
                    continue;
                }

                _activeBuffs.RemoveAt(i);
                RefreshRuntimeState(true);
                if (invokeEvent)
                {
                    _onBuffExpired?.Invoke(buffId);
                }

                return;
            }
        }

        private void ProcessStatusTickDamage(float deltaTime)
        {
            for (int i = 0; i < _activeStatuses.Count; i++)
            {
                ActiveStatusEntry entry = _activeStatuses[i];
                StatusEffectDefinition definition = ResolveStatusDefinition(entry.StatusId);
                if (definition == null || definition.TickDamagePerSecond <= 0f)
                {
                    continue;
                }

                TakeDamage(definition.TickDamagePerSecond * deltaTime * entry.Stacks);
            }
        }

        private void ExpireEffects()
        {
            double now = RpgTimeUtility.GetCurrentUnixTimestamp();
            bool changed = false;

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i].ExpiresAtUtc > now)
                {
                    continue;
                }

                string id = _activeBuffs[i].BuffId;
                _activeBuffs.RemoveAt(i);
                changed = true;
                _onBuffExpired?.Invoke(id);
            }

            for (int i = _activeStatuses.Count - 1; i >= 0; i--)
            {
                if (_activeStatuses[i].ExpiresAtUtc > now)
                {
                    continue;
                }

                string id = _activeStatuses[i].StatusId;
                _activeStatuses.RemoveAt(i);
                changed = true;
                _onStatusExpired?.Invoke(id);
            }

            if (changed)
            {
                RefreshRuntimeState(true);
            }
        }

        private ActiveBuffEntry FindBuffEntry(string buffId)
        {
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (string.Equals(_activeBuffs[i].BuffId, buffId, StringComparison.Ordinal))
                {
                    return _activeBuffs[i];
                }
            }

            return null;
        }

        private ActiveStatusEntry FindStatusEntry(string statusId)
        {
            for (int i = 0; i < _activeStatuses.Count; i++)
            {
                if (string.Equals(_activeStatuses[i].StatusId, statusId, StringComparison.Ordinal))
                {
                    return _activeStatuses[i];
                }
            }

            return null;
        }

        private BuffDefinition ResolveBuffDefinition(string buffId)
        {
            if (string.IsNullOrWhiteSpace(buffId))
            {
                return null;
            }

            for (int i = 0; i < _buffDefinitions.Length; i++)
            {
                BuffDefinition definition = _buffDefinitions[i];
                if (definition != null && string.Equals(definition.Id, buffId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private StatusEffectDefinition ResolveStatusDefinition(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                return null;
            }

            for (int i = 0; i < _statusDefinitions.Length; i++)
            {
                StatusEffectDefinition definition = _statusDefinitions[i];
                if (definition != null && string.Equals(definition.Id, statusId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private void RefreshRuntimeState(bool invokeEvents)
        {
            HpState.SetValueWithoutNotify(_currentHp);
            HpPercentState.SetValueWithoutNotify(_maxHp > 0f ? _currentHp / _maxHp : 0f);
            LevelState.SetValueWithoutNotify(_level);
            InvulnerableState.SetValueWithoutNotify(IsInvulnerable);

            if (!invokeEvents)
            {
                return;
            }

            HpState.ForceNotify();
            HpPercentState.ForceNotify();
            LevelState.ForceNotify();
            InvulnerableState.ForceNotify();
        }
    }
}
