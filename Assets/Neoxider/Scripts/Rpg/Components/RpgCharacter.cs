using System;
using System.Collections.Generic;
using Neo.Core.Level;
using Neo.Core.Resources;
using Neo.Network;
using Neo.Reactive;
using Neo.Rpg.Runtime;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg.Components
{
    /// <summary>
    ///     Unified, per-instance RPG character facade. Supports any number of resources (HP / Mana /
    ///     Stamina / DarkMana / Rage / custom), any number of stats (Strength / Defense / FireResist /
    ///     custom), inline + SO buffs, status effects, Dota-style auto-growth, Dark-Souls-style manual
    ///     upgrade points, plus an optional transport-neutral network adapter.
    ///     <para>One <see cref="RpgCharacter"/> per character - both for players and NPCs. Drops the
    ///     legacy singleton profile pattern.</para>
    ///     <para>Public API is UnityEvent-friendly: every method takes one parameter or none and uses
    ///     primitive / GameObject / SO arguments. Wire it from <see cref="UnityEvent"/>,
    ///     <c>NetworkContextActionRelay.InvokeComponentMethod</c>, <c>NeoCondition</c>, etc.</para>
    /// </summary>
    [NeoDoc("Rpg/RpgCharacter.md")]
    [CreateFromMenu("Neoxider/RPG/RpgCharacter")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgCharacter))]
    [DisallowMultipleComponent]
    public sealed class RpgCharacter : MonoBehaviour, IRpgCombatReceiver, INeoOptionalNetworked
    {

        [Header("Template")]
        [Tooltip("Optional SO archetype. When set + applyTemplateOnAwake = true, resources/stats below " +
                 "are replaced with the template's at Awake. Leave template empty to fully drive setup " +
                 "from the inline arrays.")]
        [SerializeField]
        private RpgCharacterTemplate _template;

        [SerializeField] private bool _applyTemplateOnAwake = true;

        [Header("Resources")]
        [Tooltip("Resource pools (HP / Mana / Stamina / Shield / Rage / any custom). Auto-populated " +
                 "from Template when applyTemplateOnAwake is on.")]
        [SerializeField]
        private RpgResourceDefinition[] _resources = Array.Empty<RpgResourceDefinition>();

        [Header("Stats")]
        [Tooltip("Single-value stats (Strength / Defense / FireResist / custom). Auto-populated from " +
                 "Template when applyTemplateOnAwake is on.")]
        [SerializeField]
        private RpgStatDefinition[] _stats = Array.Empty<RpgStatDefinition>();

        [Header("Effects")] [Tooltip("Re-usable SO buff catalogue (applied via ApplyBuffById).")] [SerializeField]
        private BuffDefinition[] _knownBuffs = Array.Empty<BuffDefinition>();

        [Tooltip("One-off inline buffs (no SO required). Apply via ApplyInlineBuff(int) or by Id.")] [SerializeField]
        private InlineBuffEntry[] _inlineBuffs = Array.Empty<InlineBuffEntry>();

        [Tooltip("Re-usable SO status catalogue (applied via ApplyStatusById).")] [SerializeField]
        private StatusEffectDefinition[] _knownStatuses = Array.Empty<StatusEffectDefinition>();

        [Header("Progression (optional)")]
        [Tooltip("Optional level-up rules. Empty = level changes do not auto-modify stats.")]
        [SerializeField]
        private RpgProgressionDefinition _progression;

        [Tooltip("Optional level provider. When set, character level mirrors LevelComponent.")] [SerializeField]
        private LevelComponent _levelProvider;

        [Header("Persistence (optional)")] [SerializeField]
        private string _saveKey = string.Empty;

        [SerializeField] private bool _loadOnAwake;
        [SerializeField] private bool _autoSave;

        [Header("Networking")]
        [Tooltip("Enable the optional network adapter when one is installed on this GameObject.")]
        public bool isNetworked;

        [Header("Authority")]
        [Tooltip("Who is allowed to send networked commands for this character.")]
        [SerializeField]
        private NetworkAuthorityMode _authorityMode = NetworkAuthorityMode.OwnerOnly;

        [Tooltip(
            "Legacy opt-in. When disabled, remote clients cannot mutate trusted RPG state through Net* commands; " +
            "server gameplay must apply damage, XP, level, resources, stats, buffs, statuses, and invulnerability.")]
        [SerializeField]
        private bool _allowClientStateCommands;

        [Serializable]
        public sealed class StringFloatEvent : UnityEvent<string, float>
        {
        }

        [Header("Events")] [SerializeField] private UnityEventFloat _onDamaged = new();
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

        [Serializable]
        public sealed class StringEvent : UnityEvent<string>
        {
        }


        private readonly RpgCharacterResourceService _resourceService = new();
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
        private bool _resourceServiceSubscribed;
        private int _runtimeGeneration;
        private int _derivedGeneration;
        private int _appliedGrowthLevel = 1;
        private int _publicationDepth;
        private bool _initializing;
        private bool _pendingRebuild;
        private bool _snapshotPending;
        private bool _applyingProfile;
        private bool _applyingBuff;
        private LevelComponent _subscribedLevelProvider;
        private IRpgCharacterNetworkAdapter _networkAdapter;
        private readonly RpgCharacterProfileService _profileService = new();

        /// <summary>Raised after coherent runtime replacement, before optional auto-load. Rebind cached reactive states here.</summary>
        public event Action RuntimeRebuilt;

        public readonly ReactivePropertyInt LevelState = new(1);
        public readonly ReactivePropertyBool IsDeadState = new(false);
        public readonly ReactivePropertyBool InvulnerableState = new(false);
        public readonly ReactivePropertyInt UpgradePointsState = new(0);
        public readonly ReactivePropertyFloat XpState = new(0f);


        public RpgCharacterTemplate Template { get => _template; set => _template = value; }
        public NetworkAuthorityMode AuthorityMode { get => _authorityMode; set => _authorityMode = value; }

        public bool AllowClientStateCommands
        {
            get => _allowClientStateCommands;
            set => _allowClientStateCommands = value;
        }

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

        public float CurrentHp => HpValue;
        public float MaxHp => MaxHpValue;
        public int Level => _level;
        public bool IsDead => _isDead;
        public bool CanPerformActions => !_isDead && !HasBlockingStatus();
        bool INeoOptionalNetworked.IsNetworked => isNetworked;


        private void Awake()
        {
            EnsureResourceServiceSubscription();
            ResolveNetworkAdapter();
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            if (_pendingRebuild && !_initializing)
            {
                EnsureInitialized();
            }
            if (!_initialized)
            {
                return;
            }
            if (_networkAdapter != null && _networkAdapter.SuppressLocalSimulation)
            {
                return;
            }
            float dt = Time.deltaTime;
            int generation = _runtimeGeneration;
            _resourceService.TickRegen(dt, _isDead);
            if (generation != _runtimeGeneration)
            {
                return;
            }
            _effects.Tick(dt, HandleBuffExpired, HandleStatusExpired, HandleStatusTickDamage);
        }

        private void OnDisable()
        {
            if (_autoSave && _initialized && !string.IsNullOrWhiteSpace(_saveKey))
            {
                SaveProfile();
            }
        }

        private void OnDestroy()
        {
            _runtimeGeneration++;
            _resourceService.InvalidateOperations();
            _initialized = false;
            if (_resourceServiceSubscribed)
            {
                _resourceService.ResourceChanged -= NotifyResource;
                _resourceServiceSubscribed = false;
            }
            if (_subscribedLevelProvider != null)
            {
                _subscribedLevelProvider.LevelState.RemoveListener(HandleLevelProviderChanged);
            }
        }


        private void EnsureInitialized()
        {
            EnsureResourceServiceSubscription();
            if (_initialized || _initializing)
            {
                return;
            }
            _initializing = true;
            BeginPublication();
            try
            {
                // A callback may replace the template. Bound retries; persistent reentry retries next Update.
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    _pendingRebuild = false;
                    int generation = ++_runtimeGeneration;
                    if (_applyTemplateOnAwake && _template != null)
                    {
                        ImportTemplate(_template);
                    }
                    _upgradeInvestments.Clear();
                    _effects.ClearAllBuffs();
                    _effects.ClearAllStatuses();
                    BuildRuntimes();
                    BuildUpgradeLookup();
                    _effects.RegisterBuffLibrary(GatherBuffs());
                    _effects.RegisterStatusLibrary(GatherStatuses());
                    _effects.RegisterInlineBuffs(_inlineBuffs);
                    if (_subscribedLevelProvider != null)
                    {
                        _subscribedLevelProvider.LevelState.RemoveListener(HandleLevelProviderChanged);
                    }
                    _subscribedLevelProvider = _levelProvider;
                    if (_subscribedLevelProvider != null)
                    {
                        _level = Mathf.Max(1, _subscribedLevelProvider.Level);
                        _subscribedLevelProvider.LevelState.AddListener(HandleLevelProviderChanged);
                    }
                    _appliedGrowthLevel = UsesAutomaticGrowth() ? _level : 1;
                    if (!RefreshAllDerivedCore(true, null, generation))
                    {
                        continue;
                    }
                    _initialized = true;
                    RuntimeRebuilt?.Invoke();
                    if (!_initialized || _pendingRebuild)
                    {
                        continue;
                    }
                    // RuntimeRebuilt intentionally precedes auto-load; transport publication follows both.
                    if (_loadOnAwake && !string.IsNullOrWhiteSpace(_saveKey))
                    {
                        LoadProfile();
                    }
                    if (!_initialized || _pendingRebuild)
                    {
                        continue;
                    }
                    PushSnapshotIfServer();
                    return;
                }
                _initialized = false;
                _pendingRebuild = true;
            }
            finally
            {
                _initializing = false;
                EndPublication();
            }
        }

        private void ImportTemplate(RpgCharacterTemplate t)
        {
            if (t == null)
            {
                return;
            }

            if (t.resources != null && t.resources.Length > 0)
            {
                _resources = t.resources;
            }

            if (t.stats != null && t.stats.Length > 0)
            {
                _stats = t.stats;
            }

            if (t.knownBuffs != null && t.knownBuffs.Length > 0)
            {
                _knownBuffs = MergeBuffs(_knownBuffs, t.knownBuffs);
            }

            if (t.knownStatuses != null && t.knownStatuses.Length > 0)
            {
                _knownStatuses = MergeStatuses(_knownStatuses, t.knownStatuses);
            }

            if (t.progression != null)
            {
                _progression = t.progression;
            }
        }

        private void BuildRuntimes()
        {
            _resourceService.Build(_resources);

            _statRuntime.Clear();
            for (int i = 0; i < _stats.Length; i++)
            {
                RpgStatDefinition def = _stats[i];
                if (def == null || !def.id.IsValid)
                {
                    continue;
                }

                _statRuntime[def.id.Value] = new RpgStatRuntime(def);
            }
        }

        private void BuildUpgradeLookup()
        {
            _upgradeRuleLookup.Clear();
            if (_progression == null || _progression.upgradeRules == null)
            {
                return;
            }

            for (int i = 0; i < _progression.upgradeRules.Length; i++)
            {
                RpgStatUpgradeRule rule = _progression.upgradeRules[i];
                if (rule == null || !rule.statId.IsValid)
                {
                    continue;
                }

                _upgradeRuleLookup[rule.statId.Value] = rule;
            }
        }

        private IEnumerable<BuffDefinition> GatherBuffs()
        {
            for (int i = 0; i < _knownBuffs.Length; i++)
            {
                if (_knownBuffs[i] != null)
                {
                    yield return _knownBuffs[i];
                }
            }
        }

        private IEnumerable<StatusEffectDefinition> GatherStatuses()
        {
            for (int i = 0; i < _knownStatuses.Length; i++)
            {
                if (_knownStatuses[i] != null)
                {
                    yield return _knownStatuses[i];
                }
            }
        }

        private static BuffDefinition[] MergeBuffs(BuffDefinition[] a, BuffDefinition[] b)
        {
            HashSet<BuffDefinition> set = new();
            if (a != null)
            {
                foreach (BuffDefinition x in a)
                {
                    if (x != null)
                    {
                        set.Add(x);
                    }
                }
            }

            if (b != null)
            {
                foreach (BuffDefinition x in b)
                {
                    if (x != null)
                    {
                        set.Add(x);
                    }
                }
            }

            BuffDefinition[] result = new BuffDefinition[set.Count];
            set.CopyTo(result);
            return result;
        }

        private static StatusEffectDefinition[] MergeStatuses(StatusEffectDefinition[] a,
            StatusEffectDefinition[] b)
        {
            HashSet<StatusEffectDefinition> set = new();
            if (a != null)
            {
                foreach (StatusEffectDefinition x in a)
                {
                    if (x != null)
                    {
                        set.Add(x);
                    }
                }
            }

            if (b != null)
            {
                foreach (StatusEffectDefinition x in b)
                {
                    if (x != null)
                    {
                        set.Add(x);
                    }
                }
            }

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
            BeginPublication();
            try
            {
                _runtimeGeneration++;
                _initialized = false;
                _resourceService.InvalidateOperations();
                if (_initializing)
                {
                    _pendingRebuild = true;
                    return;
                }
                _resourceService.Clear();
                _statRuntime.Clear();
                _upgradeRuleLookup.Clear();
                EnsureInitialized();
            }
            finally
            {
                EndPublication();
            }
        }

        /// <summary>Assigns a template and rebuilds this character immediately.</summary>
        public void ApplyTemplate(RpgCharacterTemplate template)
        {
            _template = template;
            RebuildRuntime();
        }


        /// <summary>Damage HP using buff/status incoming-damage multipliers.</summary>
        public float Damage(float amount)
        {
            return DamageType(string.Empty, amount);
        }

        /// <summary>Typed damage (Fire/Cold/...): applies type-specific resist + global incoming damage modifier.</summary>
        public float DamageType(string damageType, float amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, damageType, amount)))
            {
                return 0f;
            }
            if (!_initialized)
            {
                EnsureInitialized();
            }

            if (amount <= 0f)
            {
                return 0f;
            }

            if (IsInvulnerable)
            {
                return 0f;
            }

            float multiplier = GetIncomingDamageMultiplier(damageType);
            float final = Mathf.Max(0f, amount * multiplier);
            int generation = _runtimeGeneration;
            float applied = Decrease(RpgResourceId.Hp, final);
            if (applied > 0f && generation == _runtimeGeneration)
            {
                _onDamaged?.Invoke(applied);
                if (generation != _runtimeGeneration)
                {
                    return applied;
                }
                NotifyRegenPauseOnDamage();
                if (HpValue <= 0f && !_isDead)
                {
                    HandleDeath();
                }
            }

            return applied;
        }

        /// <summary>Heal HP (positive amount).</summary>
        public float Heal(float amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Heal, number: amount)))
            {
                return 0f;
            }
            if (!_initialized)
            {
                EnsureInitialized();
            }

            if (amount <= 0f)
            {
                return 0f;
            }

            int generation = _runtimeGeneration;
            float applied = Increase(RpgResourceId.Hp, amount);
            if (applied > 0f && generation == _runtimeGeneration)
            {
                _onHealed?.Invoke(applied);
                if (generation != _runtimeGeneration)
                {
                    return applied;
                }
                if (_isDead && HpValue > 0f)
                {
                    Revive();
                }
            }

            return applied;
        }

        /// <summary>Spend resource (e.g. Mana, Stamina). Returns true if the cost was paid.</summary>
        public bool Spend(string resourceId, float amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Spend, resourceId, amount)))
            {
                return true;
            }
            if (!_initialized)
            {
                EnsureInitialized();
            }

            if (amount <= 0f)
            {
                return true;
            }

            return _resourceService.Spend(resourceId, amount);
        }

        /// <summary>Refill resource (clamped to Max unless canOverfill). Returns the actual amount added.</summary>
        public float Refill(string resourceId, float amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Refill, resourceId, amount)))
            {
                return 0f;
            }
            return Increase(resourceId, amount);
        }

        public float Increase(string resourceId, float amount)
        {
            if (!_initialized)
            {
                EnsureInitialized();
            }

            return _resourceService.Increase(resourceId, amount);
        }

        public float Decrease(string resourceId, float amount)
        {
            if (!_initialized)
            {
                EnsureInitialized();
            }

            return _resourceService.Decrease(resourceId, amount);
        }

        /// <summary>Restore one resource to its Max.</summary>
        public void RestoreResource(string resourceId)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.RestoreResource, resourceId)))
            {
                return;
            }
            _resourceService.Restore(resourceId);
        }

        /// <summary>Restore ALL resources to their respective Max.</summary>
        public void Restore()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.RestoreAll)))
            {
                return;
            }
            _resourceService.RestoreAll();

            if (_isDead && HpValue > 0f)
            {
                Revive();
            }
        }

        /// <summary>Sets the Max of a resource (used by buffs / upgrades). Current is clamped to new Max
        /// unless the resource has canOverfill = true.</summary>
        public void SetMaxResource(string resourceId, float newMax)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetMaxResource, resourceId, newMax)))
            {
                return;
            }
            _resourceService.SetMax(resourceId, newMax);
        }

        public void AddMaxResource(string resourceId, float delta)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddMaxResource, resourceId, delta)))
            {
                return;
            }
            _resourceService.AddMax(resourceId, delta);
        }


        public float GetStat(string statId)
        {
            return _statRuntime.TryGetValue(statId ?? string.Empty, out RpgStatRuntime s) ? s.CurrentValue : 0f;
        }

        public void AddStatBase(string statId, float delta)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddStatBase, statId, delta)))
            {
                return;
            }
            if (!_statRuntime.TryGetValue(statId ?? string.Empty, out RpgStatRuntime s))
            {
                return;
            }

            s.BaseValue += delta;
            RefreshAllDerived(false);
        }

        public void SetStatBase(string statId, float value)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetStatBase, statId, value)))
            {
                return;
            }
            if (!_statRuntime.TryGetValue(statId ?? string.Empty, out RpgStatRuntime s))
            {
                return;
            }

            s.BaseValue = value;
            RefreshAllDerived(false);
        }


        public bool ApplyBuff(BuffDefinition def)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyBuff, def != null ? def.Id : string.Empty)))
            {
                return true;
            }
            if (def == null)
            {
                return false;
            }

            return ApplyBuffEntry(def.Id, def.Modifiers, () => _effects.ApplyBuff(def));
        }

        public bool ApplyBuffById(string id)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyBuff, id)))
            {
                return true;
            }
            if (_effects.TryGetBuff(id, out BuffDefinition def))
            {
                return ApplyBuff(def);
            }

            if (_effects.TryGetInlineBuff(id, out InlineBuffEntry inline))
            {
                return ApplyInlineEntry(inline);
            }

            return false;
        }

        /// <summary>Apply an inline buff from <c>_inlineBuffs[index]</c>.</summary>
        public bool ApplyInlineBuff(int index)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyInlineBuff, integer: index)))
            {
                return true;
            }
            if (_inlineBuffs == null || index < 0 || index >= _inlineBuffs.Length)
            {
                return false;
            }

            return ApplyInlineEntry(_inlineBuffs[index]);
        }

        public bool ApplyInlineBuffByName(string name)
        {
            return ApplyBuffById(name);
        }

        private bool ApplyInlineEntry(InlineBuffEntry inline)
        {
            if (inline == null)
            {
                return false;
            }

            return ApplyBuffEntry(inline.Id, inline.Modifiers, () => _effects.ApplyInlineBuff(inline));
        }

        private readonly struct ResourceGrant
        {
            public ResourceGrant(string id, float amount)
            {
                Id = id;
                Amount = amount;
            }
            public string Id { get; }
            public float Amount { get; }
        }

        private bool ApplyBuffEntry(string id, BuffStatModifier[] modifiers,
            Func<RpgEffectShelf.ApplyResult<ActiveBuffEntry>> apply)
        {
            if (_applyingBuff || _applyingProfile)
            {
                return false;
            }
            EnsureInitialized();
            if (!_initialized)
            {
                return false;
            }
            _applyingBuff = true;
            BeginPublication();
            try
            {
                int generation = _runtimeGeneration;
                int previousStacks = _effects.GetBuffStackCount(id);
                RpgEffectShelf.ApplyResult<ActiveBuffEntry> result = apply();
                if (!result.Success)
                {
                    return false;
                }
                int stacks = _effects.GetBuffStackCount(id);
                int addedStacks = Mathf.Max(0, stacks - previousStacks);
                List<ResourceGrant> grants = new();
                for (int i = 0; i < (modifiers?.Length ?? 0); i++)
                {
                    BuffStatModifier modifier = modifiers[i];
                    if (addedStacks > 0 && modifier != null &&
                        modifier.StatType == BuffStatType.AddResourceCurrentFlat &&
                        !string.IsNullOrWhiteSpace(modifier.TargetIdValue) &&
                        RpgTickUtility.TryAmount((double)modifier.Value * addedStacks, out float amount))
                    {
                        grants.Add(new ResourceGrant(modifier.TargetIdValue, amount));
                    }
                }
                // Commit first; derive limits before one-shot current changes and settled observers.
                if (!RefreshAllDerivedCore(false, null, generation) || !_effects.IsCurrentBuff(result.Entry, stacks))
                {
                    return true;
                }
                for (int i = 0; i < grants.Count; i++)
                {
                    ResourceGrant grant = grants[i];
                    if (grant.Amount > 0f)
                    {
                        _resourceService.Increase(grant.Id, grant.Amount);
                    }
                    else if (grant.Amount < 0f)
                    {
                        _resourceService.Decrease(grant.Id, -grant.Amount);
                    }
                    if (generation != _runtimeGeneration || !_effects.IsCurrentBuff(result.Entry, stacks))
                    {
                        return true;
                    }
                }
                _onBuffApplied?.Invoke(id);
                PushSnapshotIfServer();
                return true;
            }
            finally
            {
                _applyingBuff = false;
                EndPublication();
            }
        }

        public bool RemoveBuff(string id)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.RemoveBuff, id)))
            {
                return true;
            }
            if (_effects.RemoveBuff(id))
            {
                int generation = _runtimeGeneration;
                _onBuffExpired?.Invoke(id);
                if (generation == _runtimeGeneration)
                {
                    RefreshDerivedFromBuffs();
                }
                return true;
            }

            return false;
        }

        public void ClearAllBuffs()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ClearBuffs)))
            {
                return;
            }
            _effects.ClearAllBuffs();
            RefreshDerivedFromBuffs();
        }

        public bool ApplyStatus(StatusEffectDefinition def)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyStatus, def != null ? def.Id : string.Empty)))
            {
                return true;
            }
            if (def == null)
            {
                return false;
            }

            RpgEffectShelf.ApplyResult<ActiveStatusEntry> r = _effects.ApplyStatus(def);
            if (r.Success)
            {
                _onStatusApplied?.Invoke(def.Id);
                PushSnapshotIfServer();
            }

            return r.Success;
        }

        public bool ApplyStatusById(string id)
        {
            return _effects.TryGetStatus(id, out StatusEffectDefinition def) && ApplyStatus(def);
        }

        public bool RemoveStatus(string id)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.RemoveStatus, id)))
            {
                return true;
            }
            if (_effects.RemoveStatus(id))
            {
                _onStatusExpired?.Invoke(id);
                PushSnapshotIfServer();
                return true;
            }

            return false;
        }

        public void ClearAllStatuses()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ClearStatuses)))
            {
                return;
            }
            _effects.ClearAllStatuses();
            PushSnapshotIfServer();
        }

        public bool HasBuff(string id)
        {
            return _effects.HasBuff(id);
        }

        public bool HasStatus(string id)
        {
            return _effects.HasStatus(id);
        }


        public void LockInvulnerable()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetInvulnerable, flag: true)))
            {
                return;
            }
            _invulnerabilityLocks++;
            InvulnerableState.Value = true;
            PushSnapshotIfServer();
        }

        public void UnlockInvulnerable()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetInvulnerable, flag: false)))
            {
                return;
            }
            _invulnerabilityLocks = Mathf.Max(0, _invulnerabilityLocks - 1);
            InvulnerableState.Value = _invulnerabilityLocks > 0;
            PushSnapshotIfServer();
        }

        public void SetInvulnerable(bool on)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetInvulnerable, flag: on)))
            {
                return;
            }
            if (on)
            {
                LockInvulnerable();
                return;
            }

            _invulnerabilityLocks = 0;
            InvulnerableState.Value = false;
            PushSnapshotIfServer();
        }

        float IRpgCombatReceiver.TakeDamage(RpgDamageInfo info)
        {
            return DamageType(info.DamageType, info.Amount);
        }

        public bool TrySpendResource(string resourceId, float amount, out string failReason)
        {
            failReason = null;
            if (!_resourceService.Contains(resourceId))
            {
                failReason = "Unknown resource id.";
                return false;
            }

            if (!Spend(resourceId, amount))
            {
                failReason = "Not enough resource.";
                return false;
            }

            return true;
        }

        public bool TryApplyBuff(string buffId, out string failReason)
        {
            failReason = null;
            if (ApplyBuffById(buffId))
            {
                return true;
            }

            failReason = "Buff not found.";
            return false;
        }

        public bool TryApplyStatus(string statusId, out string failReason)
        {
            failReason = null;
            if (ApplyStatusById(statusId))
            {
                return true;
            }

            failReason = "Status not found.";
            return false;
        }

        public void AddInvulnerabilityLock()
        {
            LockInvulnerable();
        }

        public void RemoveInvulnerabilityLock()
        {
            UnlockInvulnerable();
        }

        public float GetOutgoingDamageMultiplier()
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            float percent = 0f;
            for (int i = 0; i < _modifierBuffer.Count; i++)
            {
                BuffStatModifierApplication m = _modifierBuffer[i];
                if (m.Type == BuffStatType.OutgoingDamagePercent)
                {
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
                else if (m.Type == BuffStatType.DamagePercent)
                {
                    // WHY: DamagePercent is a legacy alias kept for backward compatibility with older buff data.
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
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
                if (m.Type == BuffStatType.MoveSpeedPercent)
                {
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
                else if (m.Type == BuffStatType.MovementSpeedPercent)
                {
                    // WHY: MovementSpeedPercent is a legacy alias kept for backward compatibility with older buff data.
                    percent += m.Value * Mathf.Max(1, m.Stacks);
                }
            }

            float statusMultiplier = 1f;
            foreach (ActiveStatusEntry entry in _effects.ActiveStatuses)
            {
                if (_effects.TryGetStatus(entry.StatusId, out StatusEffectDefinition definition) && definition != null)
                {
                    statusMultiplier *= Mathf.Max(0f, definition.MovementSpeedMultiplier);
                }
            }

            return Mathf.Max(0f, (1f + percent / 100f) * statusMultiplier);
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
                if (m.Type == BuffStatType.IncomingDamagePercent)
                {
                    percent += m.Value * stacks;
                }
                else if (m.Type == BuffStatType.DefensePercent)
                {
                    // WHY: DefensePercent is a legacy alias kept for backward compatibility with older buff data.
                    percent -= m.Value * stacks;
                }
                else if (m.Type == BuffStatType.DamageTypeResistPercent &&
                         !string.IsNullOrEmpty(damageType) && m.DamageType == damageType)
                {
                    resist += m.Value * stacks;
                }
                else if (m.Type == BuffStatType.SpecificDefensePercent && m.DamageType == damageType)
                {
                    resist += m.Value * stacks;
                }
            }

            float mult = 1f + percent / 100f - resist / 100f;
            return Mathf.Max(0f, mult);
        }


        public void SetLevel(int level)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetLevel, integer: level)))
            {
                return;
            }
            int clamped = Mathf.Max(1, level);
            if (clamped == _level)
            {
                return;
            }

            int oldLevel = _level;
            int generation = _runtimeGeneration;
            _level = clamped;
            if (UsesAutomaticGrowth())
            {
                _appliedGrowthLevel = clamped;
            }
            LevelState.Value = _level;
            if (generation != _runtimeGeneration || _level != clamped)
            {
                return;
            }
            _onLevelChanged?.Invoke(_level);
            if (generation != _runtimeGeneration || _level != clamped)
            {
                return;
            }

            if (_progression != null && clamped > oldLevel)
            {
                int gained = (clamped - oldLevel) * Mathf.Max(0, _progression.upgradePointsPerLevel);
                if (_progression.growthMode is RpgLevelGrowthMode.ManualUpgradePoints or RpgLevelGrowthMode.Hybrid)
                {
                    AddUpgradePoints(gained);
                    if (generation != _runtimeGeneration || _level != clamped)
                    {
                        return;
                    }
                }

            }

            if (UsesAutomaticGrowth())
            {
                RefreshAllDerived(false);
            }

            PushSnapshotIfServer();
        }

        private bool UsesAutomaticGrowth()
        {
            return _progression != null && _progression.autoApplyGrowthOnLevelUp &&
                   _progression.growthMode is RpgLevelGrowthMode.AllStatsEveryLevel or RpgLevelGrowthMode.Hybrid;
        }

        /// <summary>Explicitly applies configured per-level growth at the current level, including deferred/manual modes.</summary>
        public void ApplyLevelGrowth()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyLevelGrowth)))
            {
                return;
            }
            EnsureInitialized();
            if (!_initialized)
            {
                return;
            }
            _appliedGrowthLevel = _level;
            RefreshAllDerived(false);
        }

        public void AddLevel(int delta)
        {
            SetLevel(_level + delta);
        }

        public void AddXp(float amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddXp, number: amount)))
            {
                return;
            }
            if (amount <= 0f)
            {
                return;
            }

            _xp += amount;
            XpState.Value = _xp;
            PushSnapshotIfServer();
        }

        public void AddUpgradePoints(int amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddUpgradePoints, integer: amount)))
            {
                return;
            }
            _upgradePoints = Mathf.Max(0, _upgradePoints + amount);
            UpgradePointsState.Value = _upgradePoints;
            PushSnapshotIfServer();
        }

        public bool CanUpgradeStat(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId) || !_statRuntime.ContainsKey(statId) ||
                !_upgradeRuleLookup.TryGetValue(statId, out RpgStatUpgradeRule rule) || rule == null ||
                rule.costPerUpgrade <= 0)
            {
                return false;
            }

            if (_upgradePoints < rule.costPerUpgrade)
            {
                return false;
            }

            if (rule.maxUpgradeCount >= 0 && GetUpgradeLevel(statId) >= rule.maxUpgradeCount)
            {
                return false;
            }

            return true;
        }

        /// <summary>Local execution returns true once the point debit is accepted. If a debit observer replaces the runtime,
        /// the superseded upgrade stops without investing in or refunding into that replacement.</summary>
        public bool UpgradeStat(string statId)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.UpgradeStat, statId)))
            {
                return true;
            }
            if (!CanUpgradeStat(statId))
            {
                return false;
            }

            RpgStatUpgradeRule rule = _upgradeRuleLookup[statId];
            int generation = _runtimeGeneration;
            _upgradePoints -= rule.costPerUpgrade;
            UpgradePointsState.Value = _upgradePoints;
            if (generation != _runtimeGeneration)
            {
                return true;
            }

            _upgradeInvestments.TryGetValue(statId, out int count);
            _upgradeInvestments[statId] = count + 1;
            if (_statRuntime.TryGetValue(statId, out RpgStatRuntime stat))
            {
                stat.UpgradeCount = _upgradeInvestments[statId];
            }

            RefreshAllDerived(false);
            PushSnapshotIfServer();
            return true;
        }

        public int GetUpgradeLevel(string statId)
        {
            return _upgradeInvestments.TryGetValue(statId ?? string.Empty, out int v) ? v : 0;
        }

        public void UpgradeStrength()
        {
            UpgradeStat(nameof(RpgStatPreset.Strength));
        }

        public void UpgradeDexterity()
        {
            UpgradeStat(nameof(RpgStatPreset.Dexterity));
        }

        public void UpgradeVitality()
        {
            UpgradeStat(nameof(RpgStatPreset.Vitality));
        }

        public void UpgradeIntelligence()
        {
            UpgradeStat(nameof(RpgStatPreset.Intelligence));
        }

        public void UpgradeEndurance()
        {
            UpgradeStat(nameof(RpgStatPreset.Endurance));
        }

        public void SpendMana(float amount)
        {
            Spend(RpgResourceId.Mana, amount);
        }

        public void RefillMana(float amount)
        {
            Refill(RpgResourceId.Mana, amount);
        }

        public void SpendStamina(float amount)
        {
            Spend(RpgResourceId.Stamina, amount);
        }

        public void RefillStamina(float amount)
        {
            Refill(RpgResourceId.Stamina, amount);
        }

        public void SpendShield(float amount)
        {
            Spend(RpgResourceId.Shield, amount);
        }


        public ReactivePropertyFloat GetResourceCurrentState(string id)
        {
            return _resourceService.GetCurrentState(id);
        }

        public ReactivePropertyFloat GetResourceMaxState(string id)
        {
            return _resourceService.GetMaxState(id);
        }

        public ReactivePropertyFloat GetResourcePercentState(string id)
        {
            return _resourceService.GetPercentState(id);
        }

        public ReactivePropertyFloat GetStatState(string id)
        {
            return _statRuntime.TryGetValue(id ?? string.Empty, out RpgStatRuntime s) ? s.ValueState : null;
        }

        public float GetResource(string id)
        {
            return _resourceService.GetCurrent(id);
        }

        public float GetResourceMax(string id)
        {
            return _resourceService.GetMax(id);
        }

        public float GetResourcePercent(string id)
        {
            return _resourceService.GetPercent(id);
        }

        public IReadOnlyDictionary<string, RpgResourceRuntime> Resources => _resourceService.Resources;
        public IReadOnlyDictionary<string, RpgStatRuntime> Stats => _statRuntime;

        /// <summary>Recalculates max values, regen rates, stat values, and pushes reactive properties.</summary>
        public void RefreshAllDerived(bool initial)
        {
            RefreshAllDerivedCore(initial, null, _runtimeGeneration);
        }

        private bool RefreshAllDerivedCore(bool initial,
            IReadOnlyDictionary<string, RpgResourceSaveEntry> restoreRows, int generation)
        {
            int derivedGeneration = ++_derivedGeneration;
            Func<bool> isCurrent = () => generation == _runtimeGeneration && derivedGeneration == _derivedGeneration;
            _effects.BuildModifierApplications(_modifierBuffer);

            // WHY: refresh stats before resources — resource regen may scale off stat values.
            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statRuntime)
            {
                if (!RefreshStat(kv.Value, isCurrent))
                {
                    return false;
                }
            }

            _resourceService.RefreshDerived(
                (RpgResourceRuntime resource) => restoreRows != null &&
                    restoreRows.TryGetValue(resource.Id, out RpgResourceSaveEntry saved)
                        ? saved.Max
                        : RpgStatResolver.ResolveResourceMax(resource, _statRuntime, _upgradeRuleLookup, _modifierBuffer),
                (RpgResourceRuntime resource) => RpgStatResolver.ResolveRegen(resource, _statRuntime,
                    _modifierBuffer, _upgradeRuleLookup), initial, restoreRows, isCurrent);

            if (!isCurrent())
            {
                return false;
            }
            IsDeadState.Value = _isDead;
            if (!isCurrent())
            {
                return false;
            }
            LevelState.Value = _level;
            return isCurrent();
        }

        private void RefreshDerivedFromBuffs()
        {
            RefreshAllDerived(false);
        }

        private bool RefreshStat(RpgStatRuntime s, Func<bool> isCurrent)
        {
            float value = RpgStatResolver.ResolveStat(s, _appliedGrowthLevel, _modifierBuffer, _upgradeRuleLookup);
            s.SetCurrent(value, false);
            if (!isCurrent())
            {
                return false;
            }
            _onStatChanged?.Invoke(s.Id, value);
            if (!isCurrent())
            {
                return false;
            }
            PushSnapshotIfServer();
            return isCurrent();
        }

        private void NotifyResource(string id, RpgResourceRuntime r)
        {
            int generation = _runtimeGeneration;
            _onResourceChanged?.Invoke(id, r.Current);
            if (generation != _runtimeGeneration || !_resourceService.TryGet(id, out RpgResourceRuntime current) ||
                !ReferenceEquals(current, r))
            {
                return;
            }
            if (id == RpgResourceId.Hp)
            {
                if (r.Current <= 0f && !_isDead)
                {
                    HandleDeath();
                }
                else if (r.Current > 0f && _isDead)
                {
                    Revive();
                }
            }

            if (generation == _runtimeGeneration)
            {
                PushSnapshotIfServer();
            }
        }

        private void NotifyRegenPauseOnDamage()
        {
            _resourceService.PauseAfterDamage();
        }


        private void HandleBuffExpired(string id)
        {
            int generation = _runtimeGeneration;
            _onBuffExpired?.Invoke(id);
            if (generation == _runtimeGeneration)
            {
                RefreshDerivedFromBuffs();
            }
        }

        private void HandleStatusExpired(string id)
        {
            _onStatusExpired?.Invoke(id);
            PushSnapshotIfServer();
        }

        private void HandleStatusTickDamage(StatusEffectDefinition def, float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

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

        private void HandleLevelProviderChanged(int newLevel)
        {
            SetLevel(newLevel);
        }

        private void HandleDeath()
        {
            int generation = _runtimeGeneration;
            _isDead = true;
            IsDeadState.Value = true;
            if (generation != _runtimeGeneration)
            {
                return;
            }
            _onDeath?.Invoke();
            if (generation == _runtimeGeneration)
            {
                PushSnapshotIfServer();
            }
        }

        private void Revive()
        {
            int generation = _runtimeGeneration;
            _isDead = false;
            IsDeadState.Value = false;
            if (generation != _runtimeGeneration)
            {
                return;
            }
            _onRevived?.Invoke();
            if (generation == _runtimeGeneration)
            {
                PushSnapshotIfServer();
            }
        }

        private void PushSnapshotIfServer()
        {
            if (_publicationDepth > 0 || _initializing || !_initialized)
            {
                _snapshotPending = true;
                return;
            }
            _snapshotPending = false;
            _networkAdapter?.NotifyStateChanged();
        }

        private void BeginPublication()
        {
            _publicationDepth++;
        }

        private void EndPublication()
        {
            _publicationDepth--;
            if (_publicationDepth == 0 && !_initializing && _initialized && _snapshotPending)
            {
                PushSnapshotIfServer();
            }
        }

        private void ResolveNetworkAdapter()
        {
            _networkAdapter = null;
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IRpgCharacterNetworkAdapter adapter)
                {
                    _networkAdapter = adapter;
                    return;
                }
            }
        }

        private void EnsureResourceServiceSubscription()
        {
            if (_resourceServiceSubscribed)
            {
                return;
            }

            _resourceService.ResourceChanged += NotifyResource;
            _resourceServiceSubscribed = true;
        }

        private bool TryRouteNetworkCommand(RpgCharacterNetworkCommand command)
        {
            if (!isNetworked)
            {
                return false;
            }

            if (_networkAdapter == null)
            {
                ResolveNetworkAdapter();
            }

            return _networkAdapter != null && _networkAdapter.TryRoute(command);
        }


        public void SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            string json = _profileService.Serialize(CaptureProfile());
            SaveProvider.SetString(_saveKey, json);
            SaveProvider.Save();
            _onProfileSaved?.Invoke();
        }

        /// <summary>Captures all persistent and replicated character state into a transport-neutral payload.</summary>
        public RpgCharacterProfileData CaptureProfile()
        {
            EnsureInitialized();
            RpgCharacterProfileData data = new()
            {
                Level = _level,
                Xp = _xp,
                UpgradePoints = _upgradePoints,
                IsDead = _isDead,
                InvulnerabilityLocks = _invulnerabilityLocks
            };

            foreach (KeyValuePair<string, RpgResourceRuntime> kv in _resourceService.Resources)
            {
                data.Resources.Add(new RpgResourceSaveEntry
                {
                    Id = kv.Key, Current = kv.Value.Current, Max = kv.Value.Max
                });
            }

            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statRuntime)
            {
                data.Stats.Add(new RpgStatSaveEntry { Id = kv.Key, Base = kv.Value.BaseValue });
            }

            foreach (KeyValuePair<string, int> kv in _upgradeInvestments)
            {
                data.Upgrades.Add(new RpgUpgradeSaveEntry { StatId = kv.Key, Count = kv.Value });
            }

            _effects.CopyActiveEffectsTo(data.ActiveBuffs, data.ActiveStatuses);
            return data;
        }

        public void LoadProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey) || !SaveProvider.HasKey(_saveKey))
            {
                return;
            }

            string json = SaveProvider.GetString(_saveKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                RpgCharacterProfileData data = _profileService.Deserialize(json);
                if (data == null)
                {
                    return;
                }

                if (ApplyProfileCore(data))
                {
                    _onProfileLoaded?.Invoke();
                }
            }
            catch (Exception ex)
            {
                NeoDiagnostics.LogException(ex, this);
            }
        }

        /// <summary>Applies a previously captured profile without touching the configured save provider.</summary>
        public void ApplyProfile(RpgCharacterProfileData data)
        {
            ApplyProfileCore(data);
        }

        private bool ApplyProfileCore(RpgCharacterProfileData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_applyingProfile)
            {
                return false;
            }
            EnsureInitialized();
            if (!_initialized)
            {
                return false;
            }
            _applyingProfile = true;
            BeginPublication();
            try
            {
                int generation = ++_runtimeGeneration;
                _resourceService.InvalidateOperations();
                data.Sanitize();
                _level = data.Level;
                // v2 has no applied-growth anchor. Deferred/manual profiles require an explicit apply after load.
                _appliedGrowthLevel = UsesAutomaticGrowth() ? _level : 1;
                _xp = data.Xp;
                _upgradePoints = data.UpgradePoints;
                _isDead = data.IsDead;
                _invulnerabilityLocks = data.InvulnerabilityLocks;
                Dictionary<string, RpgResourceSaveEntry> restoreRows = new();
                foreach (RpgResourceSaveEntry entry in data.Resources)
                {
                    if (_resourceService.TryGet(entry.Id, out RpgResourceRuntime resource))
                    {
                        restoreRows[resource.Id] = entry;
                    }
                }
                foreach (RpgStatSaveEntry entry in data.Stats)
                {
                    if (_statRuntime.TryGetValue(entry.Id, out RpgStatRuntime stat))
                    {
                        stat.BaseValue = entry.Base;
                    }
                }
                _upgradeInvestments.Clear();
                foreach (RpgStatRuntime stat in _statRuntime.Values)
                {
                    stat.UpgradeCount = 0;
                }
                foreach (RpgUpgradeSaveEntry entry in data.Upgrades)
                {
                    _upgradeInvestments[entry.StatId] = entry.Count;
                    if (_statRuntime.TryGetValue(entry.StatId, out RpgStatRuntime stat))
                    {
                        stat.UpgradeCount = entry.Count;
                    }
                }
                _effects.RestoreActiveEffects(data.ActiveBuffs, data.ActiveStatuses);
                // Restore-call-only maxima: never copy effective Max into BaseMax or retain the lookup in regen.
                if (!RefreshAllDerivedCore(false, restoreRows, generation))
                {
                    return false;
                }
                XpState.Value = _xp;
                if (generation != _runtimeGeneration)
                {
                    return false;
                }
                UpgradePointsState.Value = _upgradePoints;
                if (generation != _runtimeGeneration)
                {
                    return false;
                }
                InvulnerableState.Value = _invulnerabilityLocks > 0;
                if (generation != _runtimeGeneration)
                {
                    return false;
                }
                PushSnapshotIfServer();
                return true;
            }
            finally
            {
                _applyingProfile = false;
                EndPublication();
            }
        }

        public void ResetProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            SaveProvider.DeleteKey(_saveKey);
            SaveProvider.Save();
        }

        // Compatibility wrappers retained on the local facade. The Mirror transport itself is supplied
        // by IRpgCharacterNetworkAdapter; without an adapter these remain ordinary local calls.
        public void NetDamage(float amount)
        {
            Damage(amount);
        }

        public void NetDamageType(string damageType, float amount)
        {
            DamageType(damageType, amount);
        }

        public void NetHeal(float amount)
        {
            Heal(amount);
        }

        public void NetSpend(string resourceId, float amount)
        {
            Spend(resourceId, amount);
        }

        public void NetRefill(string resourceId, float amount)
        {
            Refill(resourceId, amount);
        }

        public void NetApplyBuffById(string id)
        {
            ApplyBuffById(id);
        }

        public void NetApplyInlineBuff(int index)
        {
            ApplyInlineBuff(index);
        }

        public void NetApplyStatusById(string id)
        {
            ApplyStatusById(id);
        }

        public void NetAddLevel(int delta)
        {
            AddLevel(delta);
        }

    }
}
