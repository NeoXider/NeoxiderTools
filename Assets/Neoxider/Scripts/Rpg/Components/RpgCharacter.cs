using System;
using System.Collections.Generic;
using Neo.Core.Level;
using Neo.Core.Resources;
using Neo.Network;
using Neo.Reactive;
using Neo.Rpg.Runtime;
using UnityEngine;
using UnityEngine.Events;

#if MIRROR
using Mirror;
#endif

namespace Neo.Rpg.Components
{
    /// <summary>
    ///     Unified, per-instance RPG character facade. Supports any number of resources (HP / Mana /
    ///     Stamina / DarkMana / Rage / custom), any number of stats (Strength / Defense / FireResist /
    ///     custom), inline + SO buffs, status effects, Dota-style auto-growth, Dark-Souls-style manual
    ///     upgrade points, and Mirror multiplayer.
    ///     <para>One <see cref="RpgCharacter"/> per character - both for players and NPCs. Drops the
    ///     legacy singleton profile pattern.</para>
    ///     <para>Public API is UnityEvent-friendly: every method takes one parameter or none and uses
    ///     primitive / GameObject / SO arguments. Wire it from <see cref="UnityEvent"/>,
    ///     <c>NetworkContextActionRelay.InvokeComponentMethod</c>, <c>NeoCondition</c>, etc.</para>
    /// </summary>
    [NeoDoc("Rpg/RpgCharacter.md")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgCharacter))]
    [DisallowMultipleComponent]
    public sealed class RpgCharacter : NeoNetworkComponent, IRpgCombatReceiver
    {
        // ────────────────────── Serialized config ──────────────────────

        [Header("Template")]
        [Tooltip("Optional SO archetype. When set + applyTemplateOnAwake = true, resources/stats below " +
                 "are replaced with the template's at Awake. Leave template empty to fully drive setup " +
                 "from the inline arrays.")]
        [SerializeField] private RpgCharacterTemplate _template;

        [SerializeField] private bool _applyTemplateOnAwake = true;

        [Header("Resources")]
        [Tooltip("Resource pools (HP / Mana / Stamina / Shield / Rage / any custom). Auto-populated " +
                 "from Template when applyTemplateOnAwake is on.")]
        [SerializeField] private RpgResourceDefinition[] _resources = Array.Empty<RpgResourceDefinition>();

        [Header("Stats")]
        [Tooltip("Single-value stats (Strength / Defense / FireResist / custom). Auto-populated from " +
                 "Template when applyTemplateOnAwake is on.")]
        [SerializeField] private RpgStatDefinition[] _stats = Array.Empty<RpgStatDefinition>();

        [Header("Effects")]
        [Tooltip("Re-usable SO buff catalogue (applied via ApplyBuffById).")]
        [SerializeField] private BuffDefinition[] _knownBuffs = Array.Empty<BuffDefinition>();

        [Tooltip("One-off inline buffs (no SO required). Apply via ApplyInlineBuff(int) or by Id.")]
        [SerializeField] private InlineBuffEntry[] _inlineBuffs = Array.Empty<InlineBuffEntry>();

        [Tooltip("Re-usable SO status catalogue (applied via ApplyStatusById).")]
        [SerializeField] private StatusEffectDefinition[] _knownStatuses = Array.Empty<StatusEffectDefinition>();

        [Header("Progression (optional)")]
        [Tooltip("Optional level-up rules. Empty = level changes do not auto-modify stats.")]
        [SerializeField] private RpgProgressionDefinition _progression;

        [Tooltip("Optional level provider. When set, character level mirrors LevelComponent.")]
        [SerializeField] private LevelComponent _levelProvider;

        [Header("Persistence (optional)")]
        [SerializeField] private string _saveKey = string.Empty;
        [SerializeField] private bool _loadOnAwake;
        [SerializeField] private bool _autoSave;

        [Header("Authority")]
        [Tooltip("Who is allowed to send networked commands for this character.")]
        [SerializeField] private NetworkAuthorityMode _authorityMode = NetworkAuthorityMode.OwnerOnly;

#if MIRROR
        // Single SyncVar snapshot string covers every resource/stat/buff/status/level + flags.
        // Format: "L=lvl;X=xp;U=upgradePts;D=isDead;I=invulLocks;R:id=cur/max;...;S:id=base;...;B:id=expires;...;Z:id=expires|stacks"
        // Stable, weaver-friendly, no per-resource SyncVar explosion.
        [SyncVar(hook = nameof(OnSnapshotSynced))]
        private string _syncSnapshot = string.Empty;
#endif

        // ────────────────────── Events ──────────────────────

        [Serializable] public sealed class StringFloatEvent : UnityEvent<string, float> { }

        [Header("Events")]
        [SerializeField] private UnityEventFloat _onDamaged = new();
        [SerializeField] private UnityEventFloat _onHealed = new();
        [SerializeField] private UnityEvent _onDeath = new();
        [SerializeField] private UnityEvent _onRevived = new();
        [SerializeField] private StringEvent _onBuffApplied = new();
        [SerializeField] private StringEvent _onBuffExpired = new();
        [SerializeField] private StringEvent _onStatusApplied = new();
        [SerializeField] private StringEvent _onStatusExpired = new();
        [SerializeField] private UnityEventInt _onLevelChanged = new();
        [SerializeField] private StringFloatEvent _onResourceChanged = new();
        [SerializeField] private StringFloatEvent _onStatChanged = new();
        [SerializeField] private UnityEvent _onProfileSaved = new();
        [SerializeField] private UnityEvent _onProfileLoaded = new();

        [Serializable] public sealed class StringEvent : UnityEvent<string> { }

        // ────────────────────── Runtime state ──────────────────────

        private readonly Dictionary<string, RpgResourceRuntime> _resourceRuntime = new();
        private readonly Dictionary<string, RpgStatRuntime> _statRuntime = new();
        private readonly Dictionary<string, RpgStatUpgradeRule> _upgradeRuleLookup = new();
        private readonly Dictionary<string, int> _upgradeInvestments = new();
        private readonly RpgEffectShelf _effects = new();
        private readonly List<BuffStatModifierApplication> _modifierBuffer = new();

        private int _level = 1;
        private float _xp;
        private int _upgradePoints;
        private int _invulnerabilityLocks;
        private bool _initialized;
        private bool _isDead;

        // ── Reactive shortcuts for common pools ──
        public readonly ReactivePropertyInt LevelState = new(1);
        public readonly ReactivePropertyBool IsDeadState = new(false);
        public readonly ReactivePropertyBool InvulnerableState = new(false);
        public readonly ReactivePropertyInt UpgradePointsState = new(0);
        public readonly ReactivePropertyFloat XpState = new(0f);

        // ────────────────────── Public accessors ──────────────────────

        public RpgCharacterTemplate Template { get => _template; set => _template = value; }
        public NetworkAuthorityMode AuthorityMode { get => _authorityMode; set => _authorityMode = value; }

        public UnityEventFloat OnDamagedEvent => _onDamaged;
        public UnityEventFloat OnHealedEvent => _onHealed;
        public UnityEvent OnDeathEvent => _onDeath;
        public UnityEvent OnRevivedEvent => _onRevived;
        public StringEvent OnBuffAppliedEvent => _onBuffApplied;
        public StringEvent OnBuffExpiredEvent => _onBuffExpired;
        public StringEvent OnStatusAppliedEvent => _onStatusApplied;
        public StringEvent OnStatusExpiredEvent => _onStatusExpired;
        public UnityEventInt OnLevelChangedEvent => _onLevelChanged;
        public StringFloatEvent OnResourceChangedEvent => _onResourceChanged;
        public StringFloatEvent OnStatChangedEvent => _onStatChanged;
        public UnityEvent OnProfileSavedEvent => _onProfileSaved;
        public UnityEvent OnProfileLoadedEvent => _onProfileLoaded;

        // ── Common resource shortcuts (NeoCondition / NoCodeBindText friendly) ──
        public ReactivePropertyFloat HpState => GetResourceCurrentState(RpgResourceId.Hp);
        public ReactivePropertyFloat HpPercentState => GetResourcePercentState(RpgResourceId.Hp);
        public ReactivePropertyFloat MaxHpState => GetResourceMaxState(RpgResourceId.Hp);
        public ReactivePropertyFloat ManaState => GetResourceCurrentState(RpgResourceId.Mana);
        public ReactivePropertyFloat ManaPercentState => GetResourcePercentState(RpgResourceId.Mana);
        public ReactivePropertyFloat StaminaState => GetResourceCurrentState(RpgResourceId.Stamina);
        public ReactivePropertyFloat StaminaPercentState => GetResourcePercentState(RpgResourceId.Stamina);

        public float HpValue => GetResource(RpgResourceId.Hp);
        public float HpPercentValue => GetResourcePercent(RpgResourceId.Hp);
        public float MaxHpValue => GetResourceMax(RpgResourceId.Hp);
        public float ManaValue => GetResource(RpgResourceId.Mana);
        public float ManaPercentValue => GetResourcePercent(RpgResourceId.Mana);
        public float StaminaValue => GetResource(RpgResourceId.Stamina);
        public float StaminaPercentValue => GetResourcePercent(RpgResourceId.Stamina);
        public int LevelValue => _level;
        public int UpgradePointsValue => _upgradePoints;
        public float XpValue => _xp;
        public bool IsInvulnerable => _invulnerabilityLocks > 0;

        // IRpgCombatReceiver
        public float CurrentHp => HpValue;
        public float MaxHp => MaxHpValue;
        public int Level => _level;
        public bool IsDead => _isDead;
        public bool CanPerformActions => !_isDead && !HasBlockingStatus();

        // ────────────────────── Unity lifecycle ──────────────────────

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (!_initialized) return;
#if MIRROR
            if (isNetworked && NeoNetworkState.IsClientOnly) return;
#endif
            float dt = Time.deltaTime;
            TickRegen(dt);
            _effects.Tick(dt, HandleBuffExpired, HandleStatusExpired, HandleStatusTickDamage);
        }

        private void OnDisable()
        {
            if (_autoSave && _initialized && !string.IsNullOrWhiteSpace(_saveKey))
            {
                SaveProfile();
            }
        }

        // ────────────────────── Initialization ──────────────────────

        private void EnsureInitialized()
        {
            if (_initialized) return;

            if (_applyTemplateOnAwake && _template != null)
            {
                ImportTemplate(_template);
            }

            BuildRuntimes();
            BuildUpgradeLookup();
            _effects.RegisterBuffLibrary(GatherBuffs());
            _effects.RegisterStatusLibrary(GatherStatuses());
            _effects.RegisterInlineBuffs(_inlineBuffs);

            if (_levelProvider != null)
            {
                _level = Mathf.Max(1, _levelProvider.Level);
                _levelProvider.LevelState.AddListener(HandleLevelProviderChanged);
            }

            RefreshAllDerived(initial: true);
            _initialized = true;

            if (_loadOnAwake && !string.IsNullOrWhiteSpace(_saveKey))
            {
                LoadProfile();
            }
        }

        private void ImportTemplate(RpgCharacterTemplate t)
        {
            if (t == null) return;
            if (t.resources != null && t.resources.Length > 0) _resources = t.resources;
            if (t.stats != null && t.stats.Length > 0) _stats = t.stats;
            if (t.knownBuffs != null && t.knownBuffs.Length > 0) _knownBuffs = MergeBuffs(_knownBuffs, t.knownBuffs);
            if (t.knownStatuses != null && t.knownStatuses.Length > 0)
                _knownStatuses = MergeStatuses(_knownStatuses, t.knownStatuses);
            if (t.progression != null) _progression = t.progression;
        }

        private void BuildRuntimes()
        {
            _resourceRuntime.Clear();
            for (int i = 0; i < _resources.Length; i++)
            {
                RpgResourceDefinition def = _resources[i];
                if (def == null || !def.id.IsValid) continue;
                _resourceRuntime[def.id.Value] = new RpgResourceRuntime(def);
            }

            _statRuntime.Clear();
            for (int i = 0; i < _stats.Length; i++)
            {
                RpgStatDefinition def = _stats[i];
                if (def == null || !def.id.IsValid) continue;
                _statRuntime[def.id.Value] = new RpgStatRuntime(def);
            }
        }

        private void BuildUpgradeLookup()
        {
            _upgradeRuleLookup.Clear();
            if (_progression == null || _progression.upgradeRules == null) return;
            for (int i = 0; i < _progression.upgradeRules.Length; i++)
            {
                RpgStatUpgradeRule rule = _progression.upgradeRules[i];
                if (rule == null || !rule.statId.IsValid) continue;
                _upgradeRuleLookup[rule.statId.Value] = rule;
            }
        }

        private IEnumerable<BuffDefinition> GatherBuffs()
        {
            for (int i = 0; i < _knownBuffs.Length; i++)
                if (_knownBuffs[i] != null) yield return _knownBuffs[i];
        }

        private IEnumerable<StatusEffectDefinition> GatherStatuses()
        {
            for (int i = 0; i < _knownStatuses.Length; i++)
                if (_knownStatuses[i] != null) yield return _knownStatuses[i];
        }

        private static BuffDefinition[] MergeBuffs(BuffDefinition[] a, BuffDefinition[] b)
        {
            HashSet<BuffDefinition> set = new();
            if (a != null) foreach (BuffDefinition x in a) if (x != null) set.Add(x);
            if (b != null) foreach (BuffDefinition x in b) if (x != null) set.Add(x);
            BuffDefinition[] result = new BuffDefinition[set.Count];
            set.CopyTo(result);
            return result;
        }

        private static StatusEffectDefinition[] MergeStatuses(StatusEffectDefinition[] a,
            StatusEffectDefinition[] b)
        {
            HashSet<StatusEffectDefinition> set = new();
            if (a != null) foreach (StatusEffectDefinition x in a) if (x != null) set.Add(x);
            if (b != null) foreach (StatusEffectDefinition x in b) if (x != null) set.Add(x);
            StatusEffectDefinition[] result = new StatusEffectDefinition[set.Count];
            set.CopyTo(result);
            return result;
        }

        /// <summary>
        ///     Rebuilds runtime dictionaries from the currently assigned template/inline arrays.
        ///     Useful from tests, editor tools, and runtime character swaps.
        /// </summary>
        public void RebuildRuntime()
        {
            _initialized = false;
            _resourceRuntime.Clear();
            _statRuntime.Clear();
            _upgradeRuleLookup.Clear();
            _upgradeInvestments.Clear();
            _effects.ClearAllBuffs();
            _effects.ClearAllStatuses();
            EnsureInitialized();
            PushSnapshotIfServer();
        }

        /// <summary>Assigns a template and rebuilds this character immediately.</summary>
        public void ApplyTemplate(RpgCharacterTemplate template)
        {
            _template = template;
            RebuildRuntime();
        }

        // ────────────────────── Public API: resources ──────────────────────

        /// <summary>Damage HP using buff/status incoming-damage multipliers.</summary>
        public float Damage(float amount) => DamageType(string.Empty, amount);

        /// <summary>Typed damage (Fire/Cold/...): applies type-specific resist + global incoming damage modifier.</summary>
        public float DamageType(string damageType, float amount)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdDamage(damageType ?? string.Empty, amount);
                return 0f;
            }
#endif
            if (!_initialized) EnsureInitialized();
            if (amount <= 0f) return 0f;
            if (IsInvulnerable) return 0f;

            float multiplier = GetIncomingDamageMultiplier(damageType);
            float final = Mathf.Max(0f, amount * multiplier);
            float applied = Decrease(RpgResourceId.Hp, final);
            if (applied > 0f)
            {
                _onDamaged?.Invoke(applied);
                NotifyRegenPauseOnDamage();
                if (HpValue <= 0f && !_isDead) HandleDeath();
            }

            return applied;
        }

        /// <summary>Heal HP (positive amount).</summary>
        public float Heal(float amount)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdHeal(amount);
                return 0f;
            }
#endif
            if (!_initialized) EnsureInitialized();
            if (amount <= 0f) return 0f;
            float applied = Increase(RpgResourceId.Hp, amount);
            if (applied > 0f)
            {
                _onHealed?.Invoke(applied);
                if (_isDead && HpValue > 0f) Revive();
            }
            return applied;
        }

        /// <summary>Spend resource (e.g. Mana, Stamina). Returns true if the cost was paid.</summary>
        public bool Spend(string resourceId, float amount)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSpend(resourceId, amount);
                return true;
            }
#endif
            if (!_initialized) EnsureInitialized();
            if (amount <= 0f) return true;
            if (!_resourceRuntime.TryGetValue(resourceId, out RpgResourceRuntime r)) return false;
            if (r.Current < amount && !r.Definition.canGoBelowZero) return false;

            r.SetCurrent(r.Current - amount);
            NotifyResource(resourceId, r);
            NotifyRegenPauseOnSpend(r);
            return true;
        }

        /// <summary>Refill resource (clamped to Max unless canOverfill). Returns the actual amount added.</summary>
        public float Refill(string resourceId, float amount)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdRefill(resourceId, amount);
                return 0f;
            }
#endif
            return Increase(resourceId, amount);
        }

        public float Increase(string resourceId, float amount)
        {
            if (!_initialized) EnsureInitialized();
            if (!_resourceRuntime.TryGetValue(resourceId, out RpgResourceRuntime r)) return 0f;
            if (amount <= 0f) return 0f;
            float before = r.Current;
            r.SetCurrent(before + amount);
            float applied = r.Current - before;
            if (applied > 0f) NotifyResource(resourceId, r);
            return applied;
        }

        public float Decrease(string resourceId, float amount)
        {
            if (!_initialized) EnsureInitialized();
            if (!_resourceRuntime.TryGetValue(resourceId, out RpgResourceRuntime r)) return 0f;
            if (amount <= 0f) return 0f;
            float before = r.Current;
            r.SetCurrent(before - amount);
            float applied = before - r.Current;
            if (applied > 0f) NotifyResource(resourceId, r);
            return applied;
        }

        /// <summary>Restore one resource to its Max.</summary>
        public void RestoreResource(string resourceId)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdRestoreResource(resourceId);
                return;
            }
#endif
            if (!_resourceRuntime.TryGetValue(resourceId, out RpgResourceRuntime r)) return;
            r.SetCurrent(r.Max);
            NotifyResource(resourceId, r);
        }

        /// <summary>Restore ALL resources to their respective Max.</summary>
        public void Restore()
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdRestoreAll();
                return;
            }
#endif
            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceRuntime)
            {
                kv.Value.SetCurrent(kv.Value.Max);
                NotifyResource(kv.Key, kv.Value);
            }
            if (_isDead && HpValue > 0f) Revive();
        }

        /// <summary>Sets the Max of a resource (used by buffs / upgrades). Current is clamped to new Max
        /// unless the resource has canOverfill = true.</summary>
        public void SetMaxResource(string resourceId, float newMax)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSetMaxResource(resourceId, newMax);
                return;
            }
#endif
            if (!_resourceRuntime.TryGetValue(resourceId, out RpgResourceRuntime r)) return;
            r.SetMax(newMax, clampCurrentToMax: true);
            NotifyResource(resourceId, r);
        }

        public void AddMaxResource(string resourceId, float delta)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdAddMaxResource(resourceId, delta);
                return;
            }
#endif
            if (!_resourceRuntime.TryGetValue(resourceId, out RpgResourceRuntime r)) return;
            r.SetMax(r.Max + delta, clampCurrentToMax: true);
            NotifyResource(resourceId, r);
        }

        // ────────────────────── Public API: stats ──────────────────────

        public float GetStat(string statId)
        {
            return _statRuntime.TryGetValue(statId, out RpgStatRuntime s) ? s.CurrentValue : 0f;
        }

        public void AddStatBase(string statId, float delta)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdAddStatBase(statId, delta);
                return;
            }
#endif
            if (!_statRuntime.TryGetValue(statId, out RpgStatRuntime s)) return;
            s.BaseValue += delta;
            RefreshStat(s);
        }

        public void SetStatBase(string statId, float value)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSetStatBase(statId, value);
                return;
            }
#endif
            if (!_statRuntime.TryGetValue(statId, out RpgStatRuntime s)) return;
            s.BaseValue = value;
            RefreshStat(s);
        }

        // ────────────────────── Public API: buffs / statuses ──────────────────────

        public bool ApplyBuff(BuffDefinition def)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdApplyBuffById(def != null ? def.Id : string.Empty);
                return true;
            }
#endif
            if (def == null) return false;
            RpgEffectShelf.ApplyResult<ActiveBuffEntry> r = _effects.ApplyBuff(def);
            if (r.Success) { _onBuffApplied?.Invoke(def.Id); RefreshDerivedFromBuffs(); }
            return r.Success;
        }

        public bool ApplyBuffById(string id)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdApplyBuffById(id);
                return true;
            }
#endif
            if (_effects.TryGetBuff(id, out BuffDefinition def)) return ApplyBuff(def);
            // fall-through: maybe inline
            if (_effects.TryGetInlineBuff(id, out InlineBuffEntry inline)) return ApplyInlineEntry(inline);
            return false;
        }

        /// <summary>Apply an inline buff from <c>_inlineBuffs[index]</c>.</summary>
        public bool ApplyInlineBuff(int index)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdApplyInlineBuff(index);
                return true;
            }
#endif
            if (_inlineBuffs == null || index < 0 || index >= _inlineBuffs.Length) return false;
            return ApplyInlineEntry(_inlineBuffs[index]);
        }

        public bool ApplyInlineBuffByName(string name) => ApplyBuffById(name);

        private bool ApplyInlineEntry(InlineBuffEntry inline)
        {
            if (inline == null) return false;
            RpgEffectShelf.ApplyResult<ActiveBuffEntry> r = _effects.ApplyInlineBuff(inline);
            if (r.Success) { _onBuffApplied?.Invoke(inline.Id); RefreshDerivedFromBuffs(); }
            return r.Success;
        }

        public bool RemoveBuff(string id)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdRemoveBuff(id);
                return true;
            }
#endif
            if (_effects.RemoveBuff(id))
            {
                _onBuffExpired?.Invoke(id);
                RefreshDerivedFromBuffs();
                return true;
            }
            return false;
        }

        public void ClearAllBuffs()
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdClearAllBuffs();
                return;
            }
#endif
            _effects.ClearAllBuffs();
            RefreshDerivedFromBuffs();
        }

        public bool ApplyStatus(StatusEffectDefinition def)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdApplyStatusById(def != null ? def.Id : string.Empty);
                return true;
            }
#endif
            if (def == null) return false;
            RpgEffectShelf.ApplyResult<ActiveStatusEntry> r = _effects.ApplyStatus(def);
            if (r.Success)
            {
                _onStatusApplied?.Invoke(def.Id);
                PushSnapshotIfServer();
            }
            return r.Success;
        }

        public bool ApplyStatusById(string id) =>
            _effects.TryGetStatus(id, out StatusEffectDefinition def) && ApplyStatus(def);

        public bool RemoveStatus(string id)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdRemoveStatus(id);
                return true;
            }
#endif
            if (_effects.RemoveStatus(id)) { _onStatusExpired?.Invoke(id); PushSnapshotIfServer(); return true; }
            return false;
        }

        public void ClearAllStatuses()
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdClearAllStatuses();
                return;
            }
#endif
            _effects.ClearAllStatuses();
            PushSnapshotIfServer();
        }

        public bool HasBuff(string id) => _effects.HasBuff(id);
        public bool HasStatus(string id) => _effects.HasStatus(id);

        // ────────────────────── Public API: invulnerability ──────────────────────

        public void LockInvulnerable()
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSetInvulnerable(true);
                return;
            }
#endif
            _invulnerabilityLocks++;
            InvulnerableState.Value = true;
            PushSnapshotIfServer();
        }

        public void UnlockInvulnerable()
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSetInvulnerable(false);
                return;
            }
#endif
            _invulnerabilityLocks = Mathf.Max(0, _invulnerabilityLocks - 1);
            InvulnerableState.Value = _invulnerabilityLocks > 0;
            PushSnapshotIfServer();
        }

        public void SetInvulnerable(bool on)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSetInvulnerable(on);
                return;
            }
#endif
            if (on) { LockInvulnerable(); return; }
            _invulnerabilityLocks = 0;
            InvulnerableState.Value = false;
            PushSnapshotIfServer();
        }

        // ── IRpgCombatReceiver ──
        float IRpgCombatReceiver.TakeDamage(RpgDamageInfo info) => DamageType(info.DamageType, info.Amount);

        public bool TrySpendResource(string resourceId, float amount, out string failReason)
        {
            failReason = null;
            if (!_resourceRuntime.ContainsKey(resourceId)) { failReason = "Unknown resource id."; return false; }
            if (!Spend(resourceId, amount)) { failReason = "Not enough resource."; return false; }
            return true;
        }

        public bool TryApplyBuff(string buffId, out string failReason)
        {
            failReason = null;
            if (ApplyBuffById(buffId)) return true;
            failReason = "Buff not found.";
            return false;
        }

        public bool TryApplyStatus(string statusId, out string failReason)
        {
            failReason = null;
            if (ApplyStatusById(statusId)) return true;
            failReason = "Status not found.";
            return false;
        }

        public void AddInvulnerabilityLock() => LockInvulnerable();
        public void RemoveInvulnerabilityLock() => UnlockInvulnerable();

        public float GetOutgoingDamageMultiplier()
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            float percent = 0f;
            for (int i = 0; i < _modifierBuffer.Count; i++)
            {
                BuffStatModifierApplication m = _modifierBuffer[i];
                if (m.Type == BuffStatType.OutgoingDamagePercent) percent += m.Value * Mathf.Max(1, m.Stacks);
                else if (m.Type == BuffStatType.DamagePercent) percent += m.Value; // legacy
            }
            return Mathf.Max(0f, 1f + percent / 100f);
        }

        public float GetMovementSpeedMultiplier()
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            float percent = 0f;
            for (int i = 0; i < _modifierBuffer.Count; i++)
            {
                BuffStatModifierApplication m = _modifierBuffer[i];
                if (m.Type == BuffStatType.MoveSpeedPercent) percent += m.Value * Mathf.Max(1, m.Stacks);
                else if (m.Type == BuffStatType.MovementSpeedPercent) percent += m.Value; // legacy
            }
            return Mathf.Max(0f, 1f + percent / 100f);
        }

        public float GetIncomingDamageMultiplier(string damageType = null)
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            float percent = 0f;
            float resist = 0f;
            for (int i = 0; i < _modifierBuffer.Count; i++)
            {
                BuffStatModifierApplication m = _modifierBuffer[i];
                int stacks = Mathf.Max(1, m.Stacks);
                if (m.Type == BuffStatType.IncomingDamagePercent) percent += m.Value * stacks;
                else if (m.Type == BuffStatType.DefensePercent) percent -= m.Value; // legacy
                else if (m.Type == BuffStatType.DamageTypeResistPercent &&
                         !string.IsNullOrEmpty(damageType) && m.DamageType == damageType)
                    resist += m.Value * stacks;
                else if (m.Type == BuffStatType.SpecificDefensePercent && m.DamageType == damageType)
                    resist += m.Value;
            }
            float mult = 1f + percent / 100f - resist / 100f;
            return Mathf.Max(0f, mult);
        }

        // ────────────────────── Level / progression ──────────────────────

        public void SetLevel(int level)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdSetLevel(level);
                return;
            }
#endif
            int clamped = Mathf.Max(1, level);
            if (clamped == _level) return;
            int oldLevel = _level;
            _level = clamped;
            LevelState.Value = _level;
            _onLevelChanged?.Invoke(_level);

            if (_progression != null && clamped > oldLevel)
            {
                int gained = (clamped - oldLevel) * Mathf.Max(0, _progression.upgradePointsPerLevel);
                if (_progression.growthMode is RpgLevelGrowthMode.ManualUpgradePoints or RpgLevelGrowthMode.Hybrid)
                {
                    AddUpgradePoints(gained);
                }
                if (_progression.autoApplyGrowthOnLevelUp &&
                    _progression.growthMode is RpgLevelGrowthMode.AllStatsEveryLevel or RpgLevelGrowthMode.Hybrid)
                {
                    RefreshAllDerived(initial: false);
                }
            }
            PushSnapshotIfServer();
        }

        public void AddLevel(int delta) => SetLevel(_level + delta);

        public void AddXp(float amount)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdAddXp(amount);
                return;
            }
#endif
            if (amount <= 0f) return;
            _xp += amount;
            XpState.Value = _xp;
            PushSnapshotIfServer();
        }

        public void AddUpgradePoints(int amount)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdAddUpgradePoints(amount);
                return;
            }
#endif
            _upgradePoints = Mathf.Max(0, _upgradePoints + amount);
            UpgradePointsState.Value = _upgradePoints;
            PushSnapshotIfServer();
        }

        public bool CanUpgradeStat(string statId)
        {
            if (!_upgradeRuleLookup.TryGetValue(statId, out RpgStatUpgradeRule rule) || rule == null) return false;
            if (_upgradePoints < rule.costPerUpgrade) return false;
            if (rule.maxUpgradeCount >= 0 && GetUpgradeLevel(statId) >= rule.maxUpgradeCount) return false;
            return true;
        }

        public bool UpgradeStat(string statId)
        {
#if MIRROR
            if (ShouldRouteToServer)
            {
                CmdUpgradeStat(statId);
                return true;
            }
#endif
            if (!CanUpgradeStat(statId)) return false;
            RpgStatUpgradeRule rule = _upgradeRuleLookup[statId];
            _upgradePoints -= rule.costPerUpgrade;
            UpgradePointsState.Value = _upgradePoints;

            _upgradeInvestments.TryGetValue(statId, out int count);
            _upgradeInvestments[statId] = count + 1;
            if (_statRuntime.TryGetValue(statId, out RpgStatRuntime stat))
            {
                stat.UpgradeCount = _upgradeInvestments[statId];
            }
            RefreshAllDerived(initial: false);
            PushSnapshotIfServer();
            return true;
        }

        public int GetUpgradeLevel(string statId) =>
            _upgradeInvestments.TryGetValue(statId, out int v) ? v : 0;

        // ── NoCode-friendly shorthands ──
        public void UpgradeStrength() => UpgradeStat(nameof(RpgStatPreset.Strength));
        public void UpgradeDexterity() => UpgradeStat(nameof(RpgStatPreset.Dexterity));
        public void UpgradeVitality() => UpgradeStat(nameof(RpgStatPreset.Vitality));
        public void UpgradeIntelligence() => UpgradeStat(nameof(RpgStatPreset.Intelligence));
        public void UpgradeEndurance() => UpgradeStat(nameof(RpgStatPreset.Endurance));

        public void SpendMana(float amount) => Spend(RpgResourceId.Mana, amount);
        public void RefillMana(float amount) => Refill(RpgResourceId.Mana, amount);
        public void SpendStamina(float amount) => Spend(RpgResourceId.Stamina, amount);
        public void RefillStamina(float amount) => Refill(RpgResourceId.Stamina, amount);
        public void SpendShield(float amount) => Spend(RpgResourceId.Shield, amount);

        // ────────────────────── Reactive accessors ──────────────────────

        public ReactivePropertyFloat GetResourceCurrentState(string id) =>
            _resourceRuntime.TryGetValue(id, out RpgResourceRuntime r) ? r.CurrentState : null;

        public ReactivePropertyFloat GetResourceMaxState(string id) =>
            _resourceRuntime.TryGetValue(id, out RpgResourceRuntime r) ? r.MaxState : null;

        public ReactivePropertyFloat GetResourcePercentState(string id) =>
            _resourceRuntime.TryGetValue(id, out RpgResourceRuntime r) ? r.PercentState : null;

        public ReactivePropertyFloat GetStatState(string id) =>
            _statRuntime.TryGetValue(id, out RpgStatRuntime s) ? s.ValueState : null;

        public float GetResource(string id) =>
            _resourceRuntime.TryGetValue(id, out RpgResourceRuntime r) ? r.Current : 0f;

        public float GetResourceMax(string id) =>
            _resourceRuntime.TryGetValue(id, out RpgResourceRuntime r) ? r.Max : 0f;

        public float GetResourcePercent(string id) =>
            _resourceRuntime.TryGetValue(id, out RpgResourceRuntime r)
                ? (r.Max > 0f ? Mathf.Clamp01(r.Current / r.Max) : 0f) : 0f;

        public IReadOnlyDictionary<string, RpgResourceRuntime> Resources => _resourceRuntime;
        public IReadOnlyDictionary<string, RpgStatRuntime> Stats => _statRuntime;

        // ────────────────────── Internal: regen / refresh ──────────────────────

        private void TickRegen(float dt)
        {
            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceRuntime)
            {
                RpgResourceRuntime r = kv.Value;
                RpgRegenDefinition def = r.Definition?.regen;
                if (def == null || !def.enabled) continue;
                if (def.onlyWhenAlive && _isDead) continue;
                if (def.onlyWhenNotFull && r.Current >= r.Max) continue;
                if (r.RegenPauseRemaining > 0f) { r.RegenPauseRemaining -= dt; continue; }

                switch (def.mode)
                {
                    case RpgRegenMode.FlatPerSecond:
                    case RpgRegenMode.PercentMaxPerSecond:
                    case RpgRegenMode.FromStat:
                        if (r.ResolvedRegenPerSecond > 0f) Increase(kv.Key, r.ResolvedRegenPerSecond * dt);
                        break;
                    case RpgRegenMode.FlatPerTick:
                    case RpgRegenMode.PercentMaxPerTick:
                        r.TickAccumulator += dt;
                        while (r.TickAccumulator >= def.tickInterval)
                        {
                            float per = def.mode == RpgRegenMode.FlatPerTick
                                ? def.value
                                : r.Max * (def.value / 100f);
                            if (per > 0f) Increase(kv.Key, per);
                            r.TickAccumulator -= def.tickInterval;
                        }
                        break;
                }
            }
        }

        /// <summary>Recalculates max values, regen rates, stat values, and pushes reactive properties.</summary>
        public void RefreshAllDerived(bool initial)
        {
            _effects.BuildModifierApplications(_modifierBuffer);

            // Stats first (resources may scale regen from stats).
            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statRuntime)
                RefreshStat(kv.Value);

            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceRuntime)
                RefreshResource(kv.Value, initial);

            IsDeadState.Value = _isDead;
            LevelState.Value = _level;
        }

        private void RefreshDerivedFromBuffs() => RefreshAllDerived(initial: false);

        private void RefreshStat(RpgStatRuntime s)
        {
            float value = RpgStatResolver.ResolveStat(s, _level, _modifierBuffer, _upgradeRuleLookup);
            s.SetCurrent(value, forceNotify: false);
            _onStatChanged?.Invoke(s.Id, value);
            PushSnapshotIfServer();
        }

        private void RefreshResource(RpgResourceRuntime r, bool initial)
        {
            float newMax = RpgStatResolver.ResolveResourceMax(r, _statRuntime, _upgradeRuleLookup, _modifierBuffer);
            r.SetMax(newMax, clampCurrentToMax: !r.Definition.canOverfill);

            float regen = RpgStatResolver.ResolveRegen(r, _statRuntime, _modifierBuffer);
            r.ResolvedRegenPerSecond = regen;

            if (initial && r.Definition.restoreOnAwake && r.Definition.restoreToFull)
            {
                r.SetCurrent(r.Max);
            }

            NotifyResource(r.Id, r);
        }

        private void NotifyResource(string id, RpgResourceRuntime r)
        {
            _onResourceChanged?.Invoke(id, r.Current);
            if (id == RpgResourceId.Hp)
            {
                if (r.Current <= 0f && !_isDead) HandleDeath();
            }
            PushSnapshotIfServer();
        }

        private void NotifyRegenPauseOnSpend(RpgResourceRuntime r)
        {
            if (r.Definition.regen == null || !r.Definition.regen.pauseAfterSpend) return;
            r.RegenPauseRemaining = Mathf.Max(r.RegenPauseRemaining, r.Definition.regen.pauseAfterSpendSeconds);
        }

        private void NotifyRegenPauseOnDamage()
        {
            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceRuntime)
            {
                RpgResourceRuntime r = kv.Value;
                if (r.Definition.regen == null || !r.Definition.regen.pauseAfterDamage) continue;
                r.RegenPauseRemaining = Mathf.Max(r.RegenPauseRemaining,
                    r.Definition.regen.pauseAfterDamageSeconds);
            }
        }

        // ────────────────────── Buff/status callbacks ──────────────────────

        private void HandleBuffExpired(string id)
        {
            _onBuffExpired?.Invoke(id);
            RefreshDerivedFromBuffs();
        }

        private void HandleStatusExpired(string id)
        {
            _onStatusExpired?.Invoke(id);
            PushSnapshotIfServer();
        }

        private void HandleStatusTickDamage(StatusEffectDefinition def, float amount)
        {
            if (amount <= 0f) return;
            Damage(amount);
        }

        private bool HasBlockingStatus()
        {
            for (int i = 0; i < _effects.ActiveStatuses.Count; i++)
            {
                ActiveStatusEntry e = _effects.ActiveStatuses[i];
                if (_effects.TryGetStatus(e.StatusId, out StatusEffectDefinition def) &&
                    def != null && def.BlocksActions)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleLevelProviderChanged(int newLevel) => SetLevel(newLevel);

        private void HandleDeath()
        {
            _isDead = true;
            IsDeadState.Value = true;
            _onDeath?.Invoke();
            PushSnapshotIfServer();
        }

        private void Revive()
        {
            _isDead = false;
            IsDeadState.Value = false;
            _onRevived?.Invoke();
            PushSnapshotIfServer();
        }

        private void PushSnapshotIfServer()
        {
#if MIRROR
            PushSnapshot();
#endif
        }

        // ────────────────────── Save / Load ──────────────────────

        public void SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey)) return;
            RpgCharacterProfileData data = new() { Level = _level, Xp = _xp, UpgradePoints = _upgradePoints };

            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceRuntime)
                data.Resources.Add(new RpgResourceSaveEntry
                {
                    Id = kv.Key, Current = kv.Value.Current, Max = kv.Value.Max
                });

            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statRuntime)
                data.Stats.Add(new RpgStatSaveEntry { Id = kv.Key, Base = kv.Value.BaseValue });

            foreach (KeyValuePair<string, int> kv in _upgradeInvestments)
                data.Upgrades.Add(new RpgUpgradeSaveEntry { StatId = kv.Key, Count = kv.Value });

            _effects.CopyTo(new RpgProfileData());
            foreach (ActiveBuffEntry e in _effects.ActiveBuffs)
                data.ActiveBuffs.Add(new ActiveBuffEntry { BuffId = e.BuffId, ExpiresAtUtc = e.ExpiresAtUtc });
            foreach (ActiveStatusEntry e in _effects.ActiveStatuses)
                data.ActiveStatuses.Add(new ActiveStatusEntry
                {
                    StatusId = e.StatusId, ExpiresAtUtc = e.ExpiresAtUtc, Stacks = e.Stacks
                });

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(_saveKey, json);
            PlayerPrefs.Save();
            _onProfileSaved?.Invoke();
        }

        public void LoadProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey) || !PlayerPrefs.HasKey(_saveKey)) return;
            string json = PlayerPrefs.GetString(_saveKey);
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                RpgCharacterProfileData data = JsonUtility.FromJson<RpgCharacterProfileData>(json);
                if (data == null) return;
                data.Sanitize();

                _level = data.Level;
                _xp = data.Xp;
                _upgradePoints = data.UpgradePoints;
                LevelState.Value = _level;
                XpState.Value = _xp;
                UpgradePointsState.Value = _upgradePoints;

                foreach (RpgResourceSaveEntry e in data.Resources)
                {
                    if (!_resourceRuntime.TryGetValue(e.Id, out RpgResourceRuntime r)) continue;
                    r.SetMax(e.Max, clampCurrentToMax: false);
                    r.SetCurrent(e.Current);
                }

                foreach (RpgStatSaveEntry e in data.Stats)
                {
                    if (!_statRuntime.TryGetValue(e.Id, out RpgStatRuntime s)) continue;
                    s.BaseValue = e.Base;
                }

                _upgradeInvestments.Clear();
                foreach (RpgUpgradeSaveEntry e in data.Upgrades)
                {
                    _upgradeInvestments[e.StatId] = e.Count;
                    if (_statRuntime.TryGetValue(e.StatId, out RpgStatRuntime s)) s.UpgradeCount = e.Count;
                }

                _effects.ClearAllBuffs();
                _effects.ClearAllStatuses();
                RpgProfileData proxy = new();
                foreach (ActiveBuffEntry e in data.ActiveBuffs) proxy.ActiveBuffs.Add(e);
                foreach (ActiveStatusEntry e in data.ActiveStatuses) proxy.ActiveStatusEffects.Add(e);
                _effects.RestoreFrom(proxy);

                RefreshAllDerived(initial: false);
                _onProfileLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        public void ResetProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey)) return;
            PlayerPrefs.DeleteKey(_saveKey);
        }

#if MIRROR
        // ────────────────────── Network dispatch ──────────────────────

        /// <summary>True when this client should send a Cmd to the server instead of mutating locally.</summary>
        private bool ShouldRouteToServer => isNetworked && NeoNetworkState.IsClientOnly && !NeoNetworkState.IsServer;

        // Wrapper public methods that route to the server when networked.
        // The local mutators above (DamageInternal*) are what actually change state on the authority.

        public void NetDamage(float amount)
        {
            if (ShouldRouteToServer) { CmdDamage(string.Empty, amount); return; }
            Damage(amount);
            PushSnapshot();
        }

        public void NetDamageType(string damageType, float amount)
        {
            if (ShouldRouteToServer) { CmdDamage(damageType ?? string.Empty, amount); return; }
            DamageType(damageType, amount);
            PushSnapshot();
        }

        public void NetHeal(float amount)
        {
            if (ShouldRouteToServer) { CmdHeal(amount); return; }
            Heal(amount);
            PushSnapshot();
        }

        public void NetSpend(string resourceId, float amount)
        {
            if (ShouldRouteToServer) { CmdSpend(resourceId, amount); return; }
            Spend(resourceId, amount);
            PushSnapshot();
        }

        public void NetRefill(string resourceId, float amount)
        {
            if (ShouldRouteToServer) { CmdRefill(resourceId, amount); return; }
            Refill(resourceId, amount);
            PushSnapshot();
        }

        public void NetApplyBuffById(string id)
        {
            if (ShouldRouteToServer) { CmdApplyBuffById(id); return; }
            ApplyBuffById(id);
            PushSnapshot();
        }

        public void NetApplyInlineBuff(int index)
        {
            if (ShouldRouteToServer) { CmdApplyInlineBuff(index); return; }
            ApplyInlineBuff(index);
            PushSnapshot();
        }

        public void NetApplyStatusById(string id)
        {
            if (ShouldRouteToServer) { CmdApplyStatusById(id); return; }
            ApplyStatusById(id);
            PushSnapshot();
        }

        public void NetAddLevel(int delta)
        {
            if (ShouldRouteToServer) { CmdAddLevel(delta); return; }
            AddLevel(delta);
            PushSnapshot();
        }

        // ── Commands ──

        [Command(requiresAuthority = false)]
        private void CmdDamage(string damageType, float amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            DamageType(damageType, amount);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdHeal(float amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            Heal(amount);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdSpend(string resourceId, float amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            Spend(resourceId, amount);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdRefill(string resourceId, float amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            Refill(resourceId, amount);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdRestoreResource(string resourceId, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            RestoreResource(resourceId);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdRestoreAll(NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            Restore();
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdSetMaxResource(string resourceId, float newMax, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            SetMaxResource(resourceId, newMax);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdAddMaxResource(string resourceId, float delta, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            AddMaxResource(resourceId, delta);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdAddStatBase(string statId, float delta, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            AddStatBase(statId, delta);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdSetStatBase(string statId, float value, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            SetStatBase(statId, value);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdApplyBuffById(string id, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            ApplyBuffById(id);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdApplyInlineBuff(int index, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            ApplyInlineBuff(index);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdRemoveBuff(string id, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            RemoveBuff(id);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdClearAllBuffs(NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            ClearAllBuffs();
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdApplyStatusById(string id, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            ApplyStatusById(id);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdRemoveStatus(string id, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            RemoveStatus(id);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdClearAllStatuses(NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            ClearAllStatuses();
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdAddLevel(int delta, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            AddLevel(delta);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdSetLevel(int level, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            SetLevel(level);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdAddXp(float amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            AddXp(amount);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdAddUpgradePoints(int amount, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            AddUpgradePoints(amount);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdUpgradeStat(string statId, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            UpgradeStat(statId);
            PushSnapshot();
        }

        [Command(requiresAuthority = false)]
        private void CmdSetInvulnerable(bool on, NetworkConnectionToClient sender = null)
        {
            if (RateLimitCheck()) return;
            if (!NeoNetworkState.IsAuthorized(gameObject, sender, _authorityMode)) return;
            if (on)
            {
                _invulnerabilityLocks++;
                InvulnerableState.Value = true;
            }
            else
            {
                _invulnerabilityLocks = Mathf.Max(0, _invulnerabilityLocks - 1);
                InvulnerableState.Value = _invulnerabilityLocks > 0;
            }
            PushSnapshot();
        }

        // ── Snapshot encode / decode ──

        public override void OnStartServer()
        {
            base.OnStartServer();
            EnsureInitialized();
            PushSnapshot();
        }

        protected override void ApplyNetworkState()
        {
            base.ApplyNetworkState();
            if (!string.IsNullOrEmpty(_syncSnapshot)) ApplySnapshot(_syncSnapshot);
        }

        private void OnSnapshotSynced(string _, string snapshot)
        {
            if (NeoNetworkState.IsServer) return; // server is the authority
            ApplySnapshot(snapshot);
        }

        private void PushSnapshot()
        {
            if (!isNetworked || !NeoNetworkState.IsServer) return;
            _syncSnapshot = BuildSnapshot();
        }

        private string BuildSnapshot()
        {
            System.Text.StringBuilder sb = new();
            sb.Append("L=").Append(_level).Append(';');
            sb.Append("X=").Append(_xp.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';');
            sb.Append("U=").Append(_upgradePoints).Append(';');
            sb.Append("D=").Append(_isDead ? 1 : 0).Append(';');
            sb.Append("I=").Append(_invulnerabilityLocks).Append(';');

            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceRuntime)
            {
                sb.Append("R:").Append(kv.Key).Append('=')
                    .Append(kv.Value.Current.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append('/')
                    .Append(kv.Value.Max.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append(';');
            }

            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statRuntime)
            {
                sb.Append("S:").Append(kv.Key).Append('=')
                    .Append(kv.Value.BaseValue.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append(';');
            }

            foreach (KeyValuePair<string, int> kv in _upgradeInvestments)
            {
                if (kv.Value <= 0) continue;
                sb.Append("G:").Append(kv.Key).Append('=').Append(kv.Value).Append(';');
            }

            for (int i = 0; i < _effects.ActiveBuffs.Count; i++)
            {
                ActiveBuffEntry e = _effects.ActiveBuffs[i];
                sb.Append("B:").Append(e.BuffId).Append('=')
                    .Append(e.ExpiresAtUtc.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(';');
            }

            for (int i = 0; i < _effects.ActiveStatuses.Count; i++)
            {
                ActiveStatusEntry e = _effects.ActiveStatuses[i];
                sb.Append("Z:").Append(e.StatusId).Append('=')
                    .Append(e.ExpiresAtUtc.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                    .Append('|').Append(e.Stacks).Append(';');
            }

            return sb.ToString();
        }

        private void ApplySnapshot(string snapshot)
        {
            if (string.IsNullOrEmpty(snapshot)) return;
            EnsureInitialized();

            _upgradeInvestments.Clear();
            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statRuntime)
                kv.Value.UpgradeCount = 0;
            _effects.ClearAllBuffs();
            _effects.ClearAllStatuses();

            string[] parts = snapshot.Split(';');
            foreach (string p in parts)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                int colon = p.IndexOf(':');
                int eq = p.IndexOf('=');
                if (eq < 0) continue;

                if (colon < 0 || colon > eq)
                {
                    // Header (L= / X= / U= / D= / I=)
                    string key = p.Substring(0, eq);
                    string value = p.Substring(eq + 1);
                    ApplyHeader(key, value);
                    continue;
                }

                string tag = p.Substring(0, colon);
                string idValue = p.Substring(colon + 1);
                int innerEq = idValue.IndexOf('=');
                if (innerEq < 0) continue;
                string id = idValue.Substring(0, innerEq);
                string rhs = idValue.Substring(innerEq + 1);

                switch (tag)
                {
                    case "R": ApplyResourceSnap(id, rhs); break;
                    case "S": ApplyStatSnap(id, rhs); break;
                    case "G": ApplyUpgradeSnap(id, rhs); break;
                    case "B": ApplyBuffSnap(id, rhs); break;
                    case "Z": ApplyStatusSnap(id, rhs); break;
                }
            }

            RefreshAllDerived(initial: false);
        }

        private static float ParseFloat(string s) =>
            float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float r) ? r : 0f;

        private static double ParseDouble(string s) =>
            double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double r) ? r : 0d;

        private static int ParseInt(string s) => int.TryParse(s, out int r) ? r : 0;

        private void ApplyHeader(string key, string value)
        {
            switch (key)
            {
                case "L":
                    _level = Mathf.Max(1, ParseInt(value));
                    LevelState.Value = _level;
                    break;
                case "X":
                    _xp = ParseFloat(value);
                    XpState.Value = _xp;
                    break;
                case "U":
                    _upgradePoints = Mathf.Max(0, ParseInt(value));
                    UpgradePointsState.Value = _upgradePoints;
                    break;
                case "D":
                    _isDead = ParseInt(value) != 0;
                    IsDeadState.Value = _isDead;
                    break;
                case "I":
                    _invulnerabilityLocks = Mathf.Max(0, ParseInt(value));
                    InvulnerableState.Value = _invulnerabilityLocks > 0;
                    break;
            }
        }

        private void ApplyResourceSnap(string id, string currentSlashMax)
        {
            if (!_resourceRuntime.TryGetValue(id, out RpgResourceRuntime r)) return;
            int slash = currentSlashMax.IndexOf('/');
            if (slash < 0) return;
            float cur = ParseFloat(currentSlashMax.Substring(0, slash));
            float max = ParseFloat(currentSlashMax.Substring(slash + 1));
            r.SetMax(max, clampCurrentToMax: false);
            r.SetCurrent(cur);
        }

        private void ApplyStatSnap(string id, string baseValue)
        {
            if (!_statRuntime.TryGetValue(id, out RpgStatRuntime s)) return;
            s.BaseValue = ParseFloat(baseValue);
        }

        private void ApplyUpgradeSnap(string id, string count)
        {
            int c = Mathf.Max(0, ParseInt(count));
            _upgradeInvestments[id] = c;
            if (_statRuntime.TryGetValue(id, out RpgStatRuntime s)) s.UpgradeCount = c;
        }

        private void ApplyBuffSnap(string id, string expiresAt)
        {
            if (string.IsNullOrEmpty(id)) return;
            double expires = ParseDouble(expiresAt);

            if (_effects.TryGetBuff(id, out BuffDefinition def) && def != null) _effects.ApplyBuff(def);
            else if (_effects.TryGetInlineBuff(id, out InlineBuffEntry inline) && inline != null)
                _effects.ApplyInlineBuff(inline);

            foreach (ActiveBuffEntry e in _effects.ActiveBuffs)
            {
                if (e.BuffId == id) { e.ExpiresAtUtc = expires; break; }
            }
        }

        private void ApplyStatusSnap(string id, string rhs)
        {
            if (string.IsNullOrEmpty(id)) return;
            int pipe = rhs.IndexOf('|');
            double expires = ParseDouble(pipe >= 0 ? rhs.Substring(0, pipe) : rhs);
            int stacks = pipe >= 0 ? Mathf.Max(1, ParseInt(rhs.Substring(pipe + 1))) : 1;

            if (_effects.TryGetStatus(id, out StatusEffectDefinition def) && def != null)
            {
                _effects.ApplyStatus(def);
                foreach (ActiveStatusEntry e in _effects.ActiveStatuses)
                {
                    if (e.StatusId == id) { e.ExpiresAtUtc = expires; e.Stacks = stacks; break; }
                }
            }
        }
#endif
    }
}
