using System;
using System.Collections.Generic;
using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    /// Serializable UnityEvent wrapper for string payloads used by RPG runtime components.
    /// </summary>
    [Serializable]
    public sealed class RpgStringEvent : UnityEvent<string>
    {
    }

    /// <summary>
    /// Main entry point for the persistent RPG stats system (player profile, buffs, status effects).
    /// </summary>
    [NeoDoc("Rpg/RpgStatsManager.md")]
    [CreateFromMenu("Neoxider/RPG/RpgStatsManager")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgStatsManager))]
    public sealed class RpgStatsManager : Singleton<RpgStatsManager>, IRpgCombatReceiver
    {
        private const int ProfileVersion = 1;
        private const string DefaultSaveKey = "RpgV1.Profile";

        [Header("Definitions")] [SerializeField] private BuffDefinition[] _buffDefinitions = Array.Empty<BuffDefinition>();
        [SerializeField] private StatusEffectDefinition[] _statusDefinitions = Array.Empty<StatusEffectDefinition>();

        [Header("Persistence")] [SerializeField] private string _saveKey = DefaultSaveKey;
        [SerializeField] private bool _loadOnAwake = true;
        [SerializeField] private bool _autoSave;

        [Header("Regen")] [SerializeField] private float _hpRegenPerSecond;
        [SerializeField] private float _regenInterval = 1f;

        [Header("Reactive State")] public ReactivePropertyFloat HpState = new(100f);
        public ReactivePropertyFloat HpPercentState = new(1f);
        public ReactivePropertyInt LevelState = new(1);

        [Header("Events")] [SerializeField] private UnityEventFloat _onDamaged = new();
        [SerializeField] private UnityEventFloat _onHealed = new();
        [SerializeField] private UnityEvent _onDeath = new();
        [SerializeField] private RpgStringEvent _onBuffApplied = new();
        [SerializeField] private RpgStringEvent _onBuffExpired = new();
        [SerializeField] private RpgStringEvent _onStatusApplied = new();
        [SerializeField] private RpgStringEvent _onStatusExpired = new();
        [SerializeField] private UnityEvent _onProfileLoaded = new();
        [SerializeField] private UnityEvent _onProfileSaved = new();

        private RpgProfileData _profile = new();
        private bool _rpgInitialized;
        private float _regenAccumulator;
        private int _invulnerabilityLocks;

        /// <summary>
        /// Gets a backwards-compatible singleton alias.
        /// </summary>
        public static RpgStatsManager Instance => I;

        /// <summary>
        /// Gets or sets the save key used for the persistent profile payload.
        /// </summary>
        public string SaveKey
        {
            get => _saveKey;
            set => _saveKey = string.IsNullOrWhiteSpace(value) ? DefaultSaveKey : value.Trim();
        }

        /// <summary>
        /// Gets or sets whether profile changes are written automatically after runtime mutations.
        /// </summary>
        public bool AutoSave
        {
            get => _autoSave;
            set => _autoSave = value;
        }

        /// <summary>
        /// Gets the current HP.
        /// </summary>
        public float CurrentHp => _profile.CurrentHp;

        /// <summary>
        /// Gets the maximum HP.
        /// </summary>
        public float MaxHp => _profile.MaxHp;

        /// <summary>
        /// Gets the character level.
        /// </summary>
        public int Level => _profile.Level;

        /// <summary>
        /// Gets whether the character is dead (HP &lt;= 0).
        /// </summary>
        public bool IsDead => _profile.CurrentHp <= 0f;

        /// <inheritdoc />
        public bool IsInvulnerable => _invulnerabilityLocks > 0;

        /// <inheritdoc />
        public bool CanPerformActions => !IsDead && !RpgCombatMath.HasBlockingStatus(_profile.ActiveStatusEffects, ResolveStatusDefinition);

        /// <summary>
        /// Gets the UnityEvent raised when damage is taken.
        /// </summary>
        public UnityEventFloat OnDamaged => _onDamaged;

        /// <summary>
        /// Gets the UnityEvent raised when healed.
        /// </summary>
        public UnityEventFloat OnHealed => _onHealed;

        /// <summary>
        /// Gets the UnityEvent raised when HP reaches zero.
        /// </summary>
        public UnityEvent OnDeath => _onDeath;

        /// <summary>
        /// Gets the UnityEvent raised when a buff is applied.
        /// </summary>
        public RpgStringEvent OnBuffApplied => _onBuffApplied;

        /// <summary>
        /// Gets the UnityEvent raised when a buff expires.
        /// </summary>
        public RpgStringEvent OnBuffExpired => _onBuffExpired;

        /// <summary>
        /// Gets the UnityEvent raised when a status effect is applied.
        /// </summary>
        public RpgStringEvent OnStatusApplied => _onStatusApplied;

        /// <summary>
        /// Gets the UnityEvent raised when a status effect expires.
        /// </summary>
        public RpgStringEvent OnStatusExpired => _onStatusExpired;

        /// <summary>
        /// Ensures the manager is initialized.
        /// </summary>
        public void EnsureInitialized()
        {
            if (!_rpgInitialized)
            {
                Init();
            }
        }

        /// <summary>
        /// Returns a deep copy of the current profile.
        /// </summary>
        public RpgProfileData GetProfileSnapshot()
        {
            EnsureInitialized();
            return _profile.Clone();
        }

        /// <inheritdoc />
        public float GetOutgoingDamageMultiplier()
        {
            EnsureInitialized();
            return RpgCombatMath.GetOutgoingDamageMultiplier(_profile.ActiveBuffs, ResolveBuffDefinition);
        }

        /// <inheritdoc />
        public float GetMovementSpeedMultiplier()
        {
            EnsureInitialized();
            return RpgCombatMath.GetMovementSpeedMultiplier(_profile.ActiveBuffs, _profile.ActiveStatusEffects, ResolveBuffDefinition, ResolveStatusDefinition);
        }

        /// <inheritdoc />
        public void AddInvulnerabilityLock()
        {
            EnsureInitialized();
            _invulnerabilityLocks++;
            RefreshRuntimeState(true);
        }

        /// <inheritdoc />
        public void RemoveInvulnerabilityLock()
        {
            EnsureInitialized();
            _invulnerabilityLocks = Mathf.Max(0, _invulnerabilityLocks - 1);
            RefreshRuntimeState(true);
        }

        /// <summary>
        /// Applies damage to the character.
        /// </summary>
        /// <param name="amount">Damage amount (positive value).</param>
        /// <returns>Actual damage dealt after modifiers.</returns>
        public float TakeDamage(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f || IsDead || IsInvulnerable)
            {
                return 0f;
            }

            float actualDamage = Mathf.Min(amount * RpgCombatMath.GetIncomingDamageMultiplier(_profile.ActiveBuffs, ResolveBuffDefinition), _profile.CurrentHp);
            _profile.CurrentHp -= actualDamage;
            PersistAndNotify();
            _onDamaged?.Invoke(actualDamage);

            if (IsDead)
            {
                _onDeath?.Invoke();
            }

            return actualDamage;
        }

        /// <summary>
        /// Heals the character.
        /// </summary>
        /// <param name="amount">Heal amount (positive value).</param>
        /// <returns>Actual amount healed.</returns>
        public float Heal(float amount)
        {
            EnsureInitialized();
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            float before = _profile.CurrentHp;
            _profile.CurrentHp = Mathf.Min(_profile.CurrentHp + amount, _profile.MaxHp);
            float actualHeal = _profile.CurrentHp - before;
            PersistAndNotify();
            if (actualHeal > 0f)
            {
                _onHealed?.Invoke(actualHeal);
            }

            return actualHeal;
        }

        /// <summary>
        /// Sets the maximum HP and optionally clamps current HP.
        /// </summary>
        public void SetMaxHp(float maxHp, bool clampCurrent = true)
        {
            EnsureInitialized();
            _profile.MaxHp = Mathf.Max(1f, maxHp);
            if (clampCurrent)
            {
                _profile.CurrentHp = Mathf.Min(_profile.CurrentHp, _profile.MaxHp);
            }

            PersistAndNotify();
        }

        /// <summary>
        /// Sets the character level.
        /// </summary>
        public void SetLevel(int level)
        {
            EnsureInitialized();
            _profile.Level = Mathf.Max(1, level);
            PersistAndNotify();
        }

        /// <summary>
        /// Applies a buff by definition or id.
        /// </summary>
        public bool TryApplyBuff(string buffId, out string failReason)
        {
            EnsureInitialized();
            failReason = null;

            BuffDefinition definition = ResolveBuffDefinition(buffId);
            if (definition == null)
            {
                failReason = $"Buff definition not found: {buffId}";
                return false;
            }

            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + definition.Duration;
            if (definition.Stackable)
            {
                ActiveBuffEntry existing = FindBuffEntry(buffId);
                if (existing != null)
                {
                    existing.ExpiresAtUtc = Math.Max(existing.ExpiresAtUtc, expiresAt);
                }
                else
                {
                    _profile.ActiveBuffs.Add(new ActiveBuffEntry
                    {
                        BuffId = definition.Id,
                        ExpiresAtUtc = expiresAt
                    });
                }
            }
            else
            {
                RemoveBuff(buffId);
                _profile.ActiveBuffs.Add(new ActiveBuffEntry
                {
                    BuffId = definition.Id,
                    ExpiresAtUtc = expiresAt
                });
            }

            PersistAndNotify();
            _onBuffApplied?.Invoke(definition.Id);
            return true;
        }

        /// <summary>
        /// Applies a status effect by definition or id.
        /// </summary>
        public bool TryApplyStatus(string statusId, out string failReason)
        {
            EnsureInitialized();
            failReason = null;

            StatusEffectDefinition definition = ResolveStatusDefinition(statusId);
            if (definition == null)
            {
                failReason = $"Status effect definition not found: {statusId}";
                return false;
            }

            double expiresAt = RpgTimeUtility.GetCurrentUnixTimestamp() + definition.Duration;
            ActiveStatusEntry existing = FindStatusEntry(statusId);
            if (existing != null)
            {
                if (definition.Stackable)
                {
                    existing.ExpiresAtUtc = expiresAt;
                    existing.Stacks = Mathf.Min(existing.Stacks + 1, definition.MaxStacks);
                }
                else
                {
                    existing.ExpiresAtUtc = expiresAt;
                }
            }
            else
            {
                _profile.ActiveStatusEffects.Add(new ActiveStatusEntry
                {
                    StatusId = definition.Id,
                    ExpiresAtUtc = expiresAt,
                    Stacks = 1
                });
            }

            PersistAndNotify();
            _onStatusApplied?.Invoke(definition.Id);
            return true;
        }

        /// <summary>
        /// Removes a buff by id.
        /// </summary>
        public void RemoveBuff(string buffId)
        {
            EnsureInitialized();
            for (int i = _profile.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_profile.ActiveBuffs[i].BuffId, buffId, StringComparison.Ordinal))
                {
                    _profile.ActiveBuffs.RemoveAt(i);
                    PersistAndNotify();
                    _onBuffExpired?.Invoke(buffId);
                    return;
                }
            }
        }

        /// <summary>
        /// Removes a status effect by id.
        /// </summary>
        public void RemoveStatus(string statusId)
        {
            EnsureInitialized();
            for (int i = _profile.ActiveStatusEffects.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_profile.ActiveStatusEffects[i].StatusId, statusId, StringComparison.Ordinal))
                {
                    _profile.ActiveStatusEffects.RemoveAt(i);
                    PersistAndNotify();
                    _onStatusExpired?.Invoke(statusId);
                    return;
                }
            }
        }

        /// <summary>
        /// Returns true when the character has the specified buff active.
        /// </summary>
        public bool HasBuff(string buffId)
        {
            EnsureInitialized();
            return FindBuffEntry(buffId) != null;
        }

        /// <summary>
        /// Returns true when the character has the specified status effect active.
        /// </summary>
        public bool HasStatus(string statusId)
        {
            EnsureInitialized();
            return FindStatusEntry(statusId) != null;
        }

        /// <summary>
        /// Loads the profile from the active save provider.
        /// </summary>
        public void LoadProfile()
        {
            EnsureInitialized();
            LoadProfileInternal(true);
        }

        /// <summary>
        /// Saves the profile through the active save provider.
        /// </summary>
        public void SaveProfile()
        {
            SaveProfile(true);
        }

        /// <summary>
        /// Saves the profile and optionally flushes the active save provider.
        /// </summary>
        public void SaveProfile(bool flushToProvider)
        {
            EnsureInitialized();
            _profile.Version = ProfileVersion;
            _profile.Sanitize();
            string json = JsonUtility.ToJson(_profile, true);
            SaveProvider.SetString(_saveKey, json);
            if (flushToProvider)
            {
                SaveProvider.Save();
            }

            _onProfileSaved?.Invoke();
        }

        /// <summary>
        /// Resets the profile to defaults.
        /// </summary>
        public void ResetProfile()
        {
            EnsureInitialized();
            _profile = new RpgProfileData();
            ApplyProfile(_profile, true);
            SaveProfile();
        }

        protected override bool DontDestroyOnLoadEnabled => true;

        protected override void Init()
        {
            base.Init();
            if (_rpgInitialized)
            {
                return;
            }

            _rpgInitialized = true;
            _saveKey = string.IsNullOrWhiteSpace(_saveKey) ? DefaultSaveKey : _saveKey.Trim();
            if (_loadOnAwake)
            {
                LoadProfileInternal(true);
            }
            else
            {
                ApplyProfile(new RpgProfileData(), true);
            }
        }

        private void Update()
        {
            if (!_rpgInitialized || IsDead)
            {
                return;
            }

            _regenAccumulator += Time.deltaTime;
            if (_regenAccumulator >= _regenInterval)
            {
                float totalRegen = RpgCombatMath.GetRegenPerSecond(_hpRegenPerSecond, _profile.ActiveBuffs, ResolveBuffDefinition) * _regenAccumulator;
                if (totalRegen > 0f)
                {
                    Heal(totalRegen);
                }

                ProcessStatusTickDamage(_regenAccumulator);
                ExpireBuffsAndStatuses();
                _regenAccumulator = 0f;
            }
        }

        private void OnValidate()
        {
            _saveKey = string.IsNullOrWhiteSpace(_saveKey) ? DefaultSaveKey : _saveKey.Trim();
        }

        private void LoadProfileInternal(bool invokeEvents)
        {
            SaveProvider.Load();
            string json = SaveProvider.GetString(_saveKey, string.Empty);
            RpgProfileData loadedProfile = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    loadedProfile = JsonUtility.FromJson<RpgProfileData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[RpgStatsManager] Failed to deserialize profile '{_saveKey}': {exception.Message}");
                }
            }

            ApplyProfile(loadedProfile ?? new RpgProfileData(), invokeEvents);
            if (invokeEvents)
            {
                _onProfileLoaded?.Invoke();
            }
        }

        private void ApplyProfile(RpgProfileData profile, bool invokeEvents)
        {
            _profile = profile ?? new RpgProfileData();
            _profile.Version = ProfileVersion;
            _profile.Sanitize();
            RefreshRuntimeState(invokeEvents);
        }

        private void ProcessStatusTickDamage(float deltaTime)
        {
            for (int i = 0; i < _profile.ActiveStatusEffects.Count; i++)
            {
                ActiveStatusEntry entry = _profile.ActiveStatusEffects[i];
                StatusEffectDefinition def = ResolveStatusDefinition(entry.StatusId);
                if (def == null || def.TickDamagePerSecond <= 0f)
                {
                    continue;
                }

                float damage = def.TickDamagePerSecond * deltaTime * entry.Stacks;
                if (damage > 0f)
                {
                    TakeDamage(damage);
                }
            }
        }

        private void ExpireBuffsAndStatuses()
        {
            double now = RpgTimeUtility.GetCurrentUnixTimestamp();
            for (int i = _profile.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                if (_profile.ActiveBuffs[i].ExpiresAtUtc <= now)
                {
                    string id = _profile.ActiveBuffs[i].BuffId;
                    _profile.ActiveBuffs.RemoveAt(i);
                    _onBuffExpired?.Invoke(id);
                }
            }

            for (int i = _profile.ActiveStatusEffects.Count - 1; i >= 0; i--)
            {
                if (_profile.ActiveStatusEffects[i].ExpiresAtUtc <= now)
                {
                    string id = _profile.ActiveStatusEffects[i].StatusId;
                    _profile.ActiveStatusEffects.RemoveAt(i);
                    _onStatusExpired?.Invoke(id);
                }
            }

            PersistAndNotify();
        }

        private BuffDefinition ResolveBuffDefinition(string buffId)
        {
            if (string.IsNullOrWhiteSpace(buffId)) return null;
            for (int i = 0; i < _buffDefinitions.Length; i++)
            {
                if (_buffDefinitions[i] != null && string.Equals(_buffDefinitions[i].Id, buffId, StringComparison.Ordinal))
                {
                    return _buffDefinitions[i];
                }
            }

            return null;
        }

        private StatusEffectDefinition ResolveStatusDefinition(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId)) return null;
            for (int i = 0; i < _statusDefinitions.Length; i++)
            {
                if (_statusDefinitions[i] != null && string.Equals(_statusDefinitions[i].Id, statusId, StringComparison.Ordinal))
                {
                    return _statusDefinitions[i];
                }
            }

            return null;
        }

        private ActiveBuffEntry FindBuffEntry(string buffId)
        {
            for (int i = 0; i < _profile.ActiveBuffs.Count; i++)
            {
                if (string.Equals(_profile.ActiveBuffs[i].BuffId, buffId, StringComparison.Ordinal))
                {
                    return _profile.ActiveBuffs[i];
                }
            }

            return null;
        }

        private ActiveStatusEntry FindStatusEntry(string statusId)
        {
            for (int i = 0; i < _profile.ActiveStatusEffects.Count; i++)
            {
                if (string.Equals(_profile.ActiveStatusEffects[i].StatusId, statusId, StringComparison.Ordinal))
                {
                    return _profile.ActiveStatusEffects[i];
                }
            }

            return null;
        }

        private void PersistAndNotify()
        {
            if (_autoSave)
            {
                SaveProfile(false);
            }

            RefreshRuntimeState(true);
        }

        private void RefreshRuntimeState(bool invokeEvents)
        {
            HpState.SetValueWithoutNotify(_profile.CurrentHp);
            HpPercentState.SetValueWithoutNotify(_profile.MaxHp > 0f ? _profile.CurrentHp / _profile.MaxHp : 0f);
            LevelState.SetValueWithoutNotify(_profile.Level);

            if (!invokeEvents) return;

            HpState.ForceNotify();
            HpPercentState.ForceNotify();
            LevelState.ForceNotify();
        }
    }
}
