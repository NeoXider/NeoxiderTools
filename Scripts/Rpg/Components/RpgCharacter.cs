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
    ///     <para>Public API is UnityEvent-friendly: the scene-facing methods take one parameter or none and use
    ///     primitive / GameObject / SO arguments. Wire it from <see cref="UnityEvent"/>,
    ///     <c>NetworkContextActionRelay.InvokeComponentMethod</c>, <c>NeoCondition</c>, etc.</para>
    ///     <para>State lives in plain C# services (<see cref="RpgCharacterResourceService"/>,
    ///     <see cref="RpgCharacterStatService"/>, <see cref="RpgCharacterProgressionService"/>,
    ///     <see cref="RpgEffectShelf"/>); this component orchestrates event order, re-entrancy guards,
    ///     persistence and network routing. Code-only extension points: <see cref="IRpgDamageModifier"/>,
    ///     <see cref="DamageApplied"/> and <see cref="RuntimeRebuilt"/>.</para>
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
        private readonly RpgCharacterStatService _statService = new();
        private readonly RpgCharacterProgressionService _progressionState = new();
        private readonly RpgEffectShelf _effects = new();
        private readonly List<BuffStatModifierApplication> _modifierBuffer = new();
        private readonly List<ResourceGrant> _grantBuffer = new();

        // WHY: cached once; method-group arguments would allocate three delegates on every Update.
        private Action<string> _buffExpiredHandler;
        private Action<string> _statusExpiredHandler;
        private Action<StatusEffectDefinition, float> _statusTickHandler;
        private Func<RpgResourceRuntime, float> _maxResolver;
        private Func<RpgResourceRuntime, float> _regenResolver;
        private IReadOnlyDictionary<string, RpgResourceSaveEntry> _restoreRows;
        private IRpgDamageModifier[] _damageModifiers = Array.Empty<IRpgDamageModifier>();

        private int _invulnerabilityLocks;
        private bool _initialized;
        private bool _isDead;
        private bool _resourceServiceSubscribed;
        private int _runtimeGeneration;
        private int _derivedGeneration;
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

        /// <summary>
        ///     Code-side damage notification raised after <see cref="OnDamagedEvent" /> with the original hit
        ///     (raw amount, damage type, optional source) and the HP actually removed.
        /// </summary>
        /// <remarks>
        ///     A lethal hit raises <see cref="OnDeathEvent" /> during the HP decrease, so for the killing blow
        ///     <see cref="IsDead" /> is already true when this event runs.
        /// </remarks>
        public event Action<RpgDamageInfo, float> DamageApplied;

        /// <summary>Current level (reactive).</summary>
        public readonly ReactivePropertyInt LevelState = new(1);
        /// <summary>True while HP is depleted (reactive).</summary>
        public readonly ReactivePropertyBool IsDeadState = new(false);
        /// <summary>True while at least one invulnerability lock is held (reactive).</summary>
        public readonly ReactivePropertyBool InvulnerableState = new(false);
        /// <summary>Unspent upgrade points (reactive).</summary>
        public readonly ReactivePropertyInt UpgradePointsState = new(0);
        /// <summary>Accumulated experience (reactive).</summary>
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
        public int LevelValue => _progressionState.Level;
        public int UpgradePointsValue => _progressionState.UpgradePoints;
        public float XpValue => _progressionState.Xp;
        public bool IsInvulnerable => _invulnerabilityLocks > 0;

        public float CurrentHp => HpValue;
        public float MaxHp => MaxHpValue;
        public int Level => _progressionState.Level;
        public bool IsDead => _isDead;
        public bool CanPerformActions => !_isDead && !_effects.HasBlockingStatus();
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
            IRpgCharacterNetworkAdapter adapter = LiveNetworkAdapter;
            if (adapter != null && adapter.SuppressLocalSimulation)
            {
                return;
            }
            TickSimulation(Time.deltaTime);
        }

        private void TickSimulation(float dt)
        {
            // WHY: one frame of regeneration and effect ticks publishes one network snapshot. Every changed
            // resource used to capture and serialize the whole profile on the server, once per resource per frame.
            BeginPublication();
            try
            {
                int generation = _runtimeGeneration;
                _resourceService.TickRegen(dt, _isDead);
                if (generation != _runtimeGeneration)
                {
                    return;
                }
                _effects.Tick(dt, _buffExpiredHandler ??= HandleBuffExpired,
                    _statusExpiredHandler ??= HandleStatusExpired, _statusTickHandler ??= HandleStatusTickDamage);
            }
            finally
            {
                EndPublication();
            }
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
                // WHY: a callback may replace the template. Bound retries; persistent reentry retries next Update.
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    _pendingRebuild = false;
                    int generation = ++_runtimeGeneration;
                    if (_applyTemplateOnAwake && _template != null)
                    {
                        ImportTemplate(_template);
                    }
                    _statService.ClearInvestments();
                    _effects.ClearAllBuffs();
                    _effects.ClearAllStatuses();
                    _resourceService.Build(_resources);
                    _statService.Build(_stats);
                    _statService.BuildUpgradeRules(_progression);
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
                        _progressionState.SetLevel(_subscribedLevelProvider.Level);
                        _subscribedLevelProvider.LevelState.AddListener(HandleLevelProviderChanged);
                    }
                    SyncedProgression.ResetGrowthAnchor();
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
                    // WHY: RuntimeRebuilt intentionally precedes auto-load; transport publication follows both.
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
                _statService.Clear();
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
            return ApplyDamage(new RpgDamageInfo(amount, damageType));
        }

        /// <summary>
        ///     Full incoming damage pipeline: network routing, invulnerability, buff/status multipliers for
        ///     <see cref="RpgDamageInfo.DamageType" />, registered <see cref="IRpgDamageModifier" />s, then HP.
        ///     The source reaches modifiers and <see cref="DamageApplied" /> but is not sent over the network.
        /// </summary>
        /// <returns>HP actually removed (0 when routed to the server, rejected or fully absorbed).</returns>
        public float ApplyDamage(RpgDamageInfo info)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, info.DamageType, info.Amount)))
            {
                return 0f;
            }

            // WHY: a lethal hit changes HP and then death; one snapshot covers both.
            BeginPublication();
            try
            {
                return ApplyDamageLocal(info);
            }
            finally
            {
                EndPublication();
            }
        }

        private float ApplyDamageLocal(RpgDamageInfo info)
        {
            float amount = info.Amount;
            if (!_initialized)
            {
                EnsureInitialized();
            }

            if (!RpgTickUtility.IsFinite(amount) || amount <= 0f)
            {
                return 0f;
            }

            if (IsInvulnerable)
            {
                return 0f;
            }

            float multiplier = GetIncomingDamageMultiplier(info.DamageType);
            int generation = _runtimeGeneration;
            float final = ApplyDamageModifiers(info, Mathf.Max(0f, amount * multiplier));
            if (generation != _runtimeGeneration)
            {
                // WHY: a modifier replaced the runtime; the hit belonged to the superseded one.
                return 0f;
            }

            float applied = Decrease(RpgResourceId.Hp, final);
            if (applied > 0f && generation == _runtimeGeneration)
            {
                _onDamaged?.Invoke(applied);
                if (generation != _runtimeGeneration)
                {
                    return applied;
                }
                DamageApplied?.Invoke(info, applied);
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

        /// <summary>Registers a damage hook; adding the same instance twice is ignored. See <see cref="IRpgDamageModifier" />.</summary>
        public void AddDamageModifier(IRpgDamageModifier modifier)
        {
            if (modifier == null || Array.IndexOf(_damageModifiers, modifier) >= 0)
            {
                return;
            }

            IRpgDamageModifier[] next = new IRpgDamageModifier[_damageModifiers.Length + 1];
            Array.Copy(_damageModifiers, next, _damageModifiers.Length);
            next[next.Length - 1] = modifier;
            _damageModifiers = next;
        }

        /// <summary>Unregisters a damage hook. Returns false when it was not registered.</summary>
        public bool RemoveDamageModifier(IRpgDamageModifier modifier)
        {
            int index = modifier == null ? -1 : Array.IndexOf(_damageModifiers, modifier);
            if (index < 0)
            {
                return false;
            }

            IRpgDamageModifier[] next = new IRpgDamageModifier[_damageModifiers.Length - 1];
            Array.Copy(_damageModifiers, 0, next, 0, index);
            Array.Copy(_damageModifiers, index + 1, next, index, next.Length - index);
            _damageModifiers = next;
            return true;
        }

        private float ApplyDamageModifiers(RpgDamageInfo info, float amount)
        {
            // WHY: copy-on-write array; a modifier may add/remove modifiers without disturbing this pass.
            IRpgDamageModifier[] modifiers = _damageModifiers;
            for (int i = 0; i < modifiers.Length; i++)
            {
                IRpgDamageModifier modifier = modifiers[i];
                if (modifier is UnityEngine.Object unityObject && unityObject == null)
                {
                    continue;
                }

                float next = modifier.ModifyIncomingDamage(this, info, amount);
                if (RpgTickUtility.IsFinite(next))
                {
                    amount = Mathf.Max(0f, next);
                }
            }

            return amount;
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

            // WHY: a revive-heal changes HP and then the dead flag; one snapshot covers both.
            BeginPublication();
            try
            {
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
            finally
            {
                EndPublication();
            }
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

        /// <summary>Adds to a resource locally (no network routing). Returns the amount actually added.</summary>
        public float Increase(string resourceId, float amount)
        {
            if (!_initialized)
            {
                EnsureInitialized();
            }

            return _resourceService.Increase(resourceId, amount);
        }

        /// <summary>Subtracts from a resource locally (no resist, no network routing). Returns the amount removed.</summary>
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

            // WHY: every refilled resource used to publish its own snapshot.
            BeginPublication();
            try
            {
                _resourceService.RestoreAll();

                if (_isDead && HpValue > 0f)
                {
                    Revive();
                }
            }
            finally
            {
                EndPublication();
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

        /// <summary>Adds <paramref name="delta" /> to a resource's effective Max (see <see cref="SetMaxResource" />).</summary>
        public void AddMaxResource(string resourceId, float delta)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddMaxResource, resourceId, delta)))
            {
                return;
            }
            _resourceService.AddMax(resourceId, delta);
        }


        /// <summary>Final stat value (base + level growth + upgrades + buffs), or 0 for unknown stats.</summary>
        public float GetStat(string statId)
        {
            return _statService.GetValue(statId);
        }

        /// <summary>Adds to a stat's base value and refreshes every derived value.</summary>
        public void AddStatBase(string statId, float delta)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddStatBase, statId, delta)))
            {
                return;
            }
            if (!_statService.AddBase(statId, delta))
            {
                return;
            }

            RefreshAllDerived(false);
        }

        /// <summary>Sets a stat's base value and refreshes every derived value.</summary>
        public void SetStatBase(string statId, float value)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetStatBase, statId, value)))
            {
                return;
            }
            if (!_statService.SetBase(statId, value))
            {
                return;
            }

            RefreshAllDerived(false);
        }


        /// <summary>Applies or stacks an SO buff. Returns false for a null definition or a rejected re-entrant apply.</summary>
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

            return ApplyBuffEntry(def.Id, def.Modifiers, def, null);
        }

        /// <summary>Applies a known SO buff or inline buff by id. Returns false when the id is unknown.</summary>
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

        /// <summary>Alias of <see cref="ApplyBuffById" /> for inline buffs addressed by name.</summary>
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

            return ApplyBuffEntry(inline.Id, inline.Modifiers, null, inline);
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

        private bool ApplyBuffEntry(string id, BuffStatModifier[] modifiers, BuffDefinition definition,
            InlineBuffEntry inline)
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
                RpgEffectShelf.ApplyResult<ActiveBuffEntry> result = definition != null
                    ? _effects.ApplyBuff(definition)
                    : _effects.ApplyInlineBuff(inline);
                if (!result.Success)
                {
                    return false;
                }
                int stacks = _effects.GetBuffStackCount(id);
                int addedStacks = Mathf.Max(0, stacks - previousStacks);
                // WHY: _applyingBuff forbids nesting, so one reusable grant list is safe.
                List<ResourceGrant> grants = _grantBuffer;
                grants.Clear();
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
                // WHY: commit first; derive limits before one-shot current changes and settled observers.
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
                _grantBuffer.Clear();
                _applyingBuff = false;
                EndPublication();
            }
        }

        /// <summary>Removes an active buff, raises <see cref="OnBuffExpiredEvent" /> and refreshes derived values.</summary>
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

        /// <summary>Removes every active buff and refreshes derived values (no per-buff expiry events).</summary>
        public void ClearAllBuffs()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ClearBuffs)))
            {
                return;
            }
            _effects.ClearAllBuffs();
            RefreshDerivedFromBuffs();
        }

        /// <summary>Applies or stacks a status effect and raises <see cref="OnStatusAppliedEvent" />.</summary>
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

        /// <summary>Applies a known status by id. Returns false when the id is unknown.</summary>
        public bool ApplyStatusById(string id)
        {
            return _effects.TryGetStatus(id, out StatusEffectDefinition def) && ApplyStatus(def);
        }

        /// <summary>Removes an active status and raises <see cref="OnStatusExpiredEvent" />.</summary>
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

        /// <summary>Removes every active status (no per-status expiry events).</summary>
        public void ClearAllStatuses()
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ClearStatuses)))
            {
                return;
            }
            _effects.ClearAllStatuses();
            PushSnapshotIfServer();
        }

        /// <summary>Whether a buff with this id is active.</summary>
        public bool HasBuff(string id)
        {
            return _effects.HasBuff(id);
        }

        /// <summary>Whether a status with this id is active.</summary>
        public bool HasStatus(string id)
        {
            return _effects.HasStatus(id);
        }


        /// <summary>Adds one invulnerability lock (stacking; pair with <see cref="UnlockInvulnerable" />).</summary>
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

        /// <summary>Releases one invulnerability lock.</summary>
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

        /// <summary><c>true</c> adds a lock; <c>false</c> clears every lock, including ones held by other systems.</summary>
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
            return ApplyDamage(info);
        }

        /// <summary>Spends a resource with a failure reason for unknown ids or insufficient amounts.</summary>
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

        /// <summary><see cref="ApplyBuffById" /> with a failure reason.</summary>
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

        /// <summary><see cref="ApplyStatusById" /> with a failure reason.</summary>
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

        /// <summary><see cref="IRpgCombatReceiver" /> alias of <see cref="LockInvulnerable" />.</summary>
        public void AddInvulnerabilityLock()
        {
            LockInvulnerable();
        }

        /// <summary><see cref="IRpgCombatReceiver" /> alias of <see cref="UnlockInvulnerable" />.</summary>
        public void RemoveInvulnerabilityLock()
        {
            UnlockInvulnerable();
        }

        /// <summary>Outgoing damage multiplier from active buffs (see <see cref="RpgModifierMath.GetOutgoingDamageMultiplier" />).</summary>
        public float GetOutgoingDamageMultiplier()
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            return RpgModifierMath.GetOutgoingDamageMultiplier(_modifierBuffer);
        }

        /// <summary>Movement multiplier: buff speed percent times every active status slow, clamped at 0.</summary>
        public float GetMovementSpeedMultiplier()
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            float buffMultiplier = RpgModifierMath.GetMovementSpeedBuffMultiplier(_modifierBuffer);
            return Mathf.Max(0f, buffMultiplier * _effects.GetStatusMovementMultiplier());
        }

        /// <summary>Incoming damage multiplier for a damage type (see <see cref="RpgModifierMath.GetIncomingDamageMultiplier" />).</summary>
        public float GetIncomingDamageMultiplier(string damageType = null)
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            return RpgModifierMath.GetIncomingDamageMultiplier(_modifierBuffer, damageType);
        }

        /// <summary>
        ///     Attack speed multiplier from <see cref="BuffStatType.AttackSpeedPercent" /> buffs (1 = normal).
        ///     Consumed by <see cref="RpgAttackController" /> only when its cooldown scaling option is enabled.
        /// </summary>
        public float GetAttackSpeedMultiplier()
        {
            _effects.BuildModifierApplications(_modifierBuffer);
            return RpgModifierMath.GetAttackSpeedMultiplier(_modifierBuffer);
        }


        /// <summary>
        ///     Sets the level (min 1). Automatic growth refreshes stats; manual/hybrid progression grants upgrade points
        ///     for levels gained. Raises <see cref="OnLevelChangedEvent" />.
        /// </summary>
        public void SetLevel(int level)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.SetLevel, integer: level)))
            {
                return;
            }

            // WHY: a level-up grants points, refreshes every stat and publishes; one snapshot covers all of it.
            BeginPublication();
            try
            {
                SetLevelLocal(level);
            }
            finally
            {
                EndPublication();
            }
        }

        private void SetLevelLocal(int level)
        {
            RpgCharacterProgressionService progression = SyncedProgression;
            int oldLevel = progression.Level;
            int generation = _runtimeGeneration;
            if (!progression.SetLevel(level))
            {
                return;
            }

            int clamped = progression.Level;
            LevelState.Value = clamped;
            if (generation != _runtimeGeneration || _progressionState.Level != clamped)
            {
                return;
            }
            _onLevelChanged?.Invoke(clamped);
            if (generation != _runtimeGeneration || _progressionState.Level != clamped)
            {
                return;
            }

            if (clamped > oldLevel && SyncedProgression.GrantsUpgradePoints)
            {
                AddUpgradePoints(_progressionState.GetUpgradePointsForLevelChange(oldLevel, clamped));
                if (generation != _runtimeGeneration || _progressionState.Level != clamped)
                {
                    return;
                }
            }

            if (SyncedProgression.UsesAutomaticGrowth)
            {
                RefreshAllDerived(false);
            }

            PushSnapshotIfServer();
        }

        /// <summary>Progression state with its rules synced to the serialized (or template) definition.</summary>
        private RpgCharacterProgressionService SyncedProgression
        {
            get
            {
                _progressionState.Definition = _progression;
                return _progressionState;
            }
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
            _progressionState.ApplyGrowthAtCurrentLevel();
            RefreshAllDerived(false);
        }

        /// <summary>Changes the level by <paramref name="delta" /> (saturating, min 1).</summary>
        public void AddLevel(int delta)
        {
            SetLevel(RpgCharacterProgressionService.ClampLevel((long)_progressionState.Level + delta));
        }

        /// <summary>Adds experience; non-positive and non-finite amounts are ignored.</summary>
        public void AddXp(float amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddXp, number: amount)))
            {
                return;
            }
            if (!_progressionState.AddXp(amount))
            {
                return;
            }

            XpState.Value = _progressionState.Xp;
            PushSnapshotIfServer();
        }

        /// <summary>Adds (or removes, when negative) upgrade points, saturating at 0 and int.MaxValue.</summary>
        public void AddUpgradePoints(int amount)
        {
            if (TryRouteNetworkCommand(new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.AddUpgradePoints, integer: amount)))
            {
                return;
            }
            _progressionState.AddUpgradePoints(amount);
            UpgradePointsState.Value = _progressionState.UpgradePoints;
            PushSnapshotIfServer();
        }

        /// <summary>Whether the stat has a valid upgrade rule, is below its cap and the points cover the cost.</summary>
        public bool CanUpgradeStat(string statId)
        {
            return _statService.CanUpgrade(statId, _progressionState.UpgradePoints);
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

            BeginPublication();
            try
            {
                _statService.TryGetUpgradeRule(statId, out RpgStatUpgradeRule rule);
                int generation = _runtimeGeneration;
                _progressionState.TrySpendUpgradePoints(rule.costPerUpgrade);
                UpgradePointsState.Value = _progressionState.UpgradePoints;
                if (generation != _runtimeGeneration)
                {
                    return true;
                }

                _statService.AddInvestment(statId);
                RefreshAllDerived(false);
                PushSnapshotIfServer();
                return true;
            }
            finally
            {
                EndPublication();
            }
        }

        /// <summary>Upgrade points invested into a stat.</summary>
        public int GetUpgradeLevel(string statId)
        {
            return _statService.GetUpgradeLevel(statId);
        }

        /// <summary>UnityEvent shortcut for <c>UpgradeStat("Strength")</c>.</summary>
        public void UpgradeStrength()
        {
            UpgradeStat(nameof(RpgStatPreset.Strength));
        }

        /// <summary>UnityEvent shortcut for <c>UpgradeStat("Dexterity")</c>.</summary>
        public void UpgradeDexterity()
        {
            UpgradeStat(nameof(RpgStatPreset.Dexterity));
        }

        /// <summary>UnityEvent shortcut for <c>UpgradeStat("Vitality")</c>.</summary>
        public void UpgradeVitality()
        {
            UpgradeStat(nameof(RpgStatPreset.Vitality));
        }

        /// <summary>UnityEvent shortcut for <c>UpgradeStat("Intelligence")</c>.</summary>
        public void UpgradeIntelligence()
        {
            UpgradeStat(nameof(RpgStatPreset.Intelligence));
        }

        /// <summary>UnityEvent shortcut for <c>UpgradeStat("Endurance")</c>.</summary>
        public void UpgradeEndurance()
        {
            UpgradeStat(nameof(RpgStatPreset.Endurance));
        }

        /// <summary>UnityEvent shortcut for <c>Spend("Mana", amount)</c>.</summary>
        public void SpendMana(float amount)
        {
            Spend(RpgResourceId.Mana, amount);
        }

        /// <summary>UnityEvent shortcut for <c>Refill("Mana", amount)</c>.</summary>
        public void RefillMana(float amount)
        {
            Refill(RpgResourceId.Mana, amount);
        }

        /// <summary>UnityEvent shortcut for <c>Spend("Stamina", amount)</c>.</summary>
        public void SpendStamina(float amount)
        {
            Spend(RpgResourceId.Stamina, amount);
        }

        /// <summary>UnityEvent shortcut for <c>Refill("Stamina", amount)</c>.</summary>
        public void RefillStamina(float amount)
        {
            Refill(RpgResourceId.Stamina, amount);
        }

        /// <summary>UnityEvent shortcut for <c>Spend("Shield", amount)</c>.</summary>
        public void SpendShield(float amount)
        {
            Spend(RpgResourceId.Shield, amount);
        }


        /// <summary>Reactive Current of a resource, or null. Replaced on rebuild; rebind on <see cref="RuntimeRebuilt" />.</summary>
        public ReactivePropertyFloat GetResourceCurrentState(string id)
        {
            return _resourceService.GetCurrentState(id);
        }

        /// <summary>Reactive Max of a resource, or null. Replaced on rebuild.</summary>
        public ReactivePropertyFloat GetResourceMaxState(string id)
        {
            return _resourceService.GetMaxState(id);
        }

        /// <summary>Reactive Current/Max (0-1) of a resource, or null. Replaced on rebuild.</summary>
        public ReactivePropertyFloat GetResourcePercentState(string id)
        {
            return _resourceService.GetPercentState(id);
        }

        /// <summary>Reactive final value of a stat, or null. Replaced on rebuild.</summary>
        public ReactivePropertyFloat GetStatState(string id)
        {
            return _statService.GetState(id);
        }

        /// <summary>Current value of a resource, or 0 for unknown ids.</summary>
        public float GetResource(string id)
        {
            return _resourceService.GetCurrent(id);
        }

        /// <summary>Effective Max of a resource, or 0 for unknown ids.</summary>
        public float GetResourceMax(string id)
        {
            return _resourceService.GetMax(id);
        }

        /// <summary>Current/Max clamped to 0-1, or 0 for unknown ids or zero Max.</summary>
        public float GetResourcePercent(string id)
        {
            return _resourceService.GetPercent(id);
        }

        public IReadOnlyDictionary<string, RpgResourceRuntime> Resources => _resourceService.Resources;
        public IReadOnlyDictionary<string, RpgStatRuntime> Stats => _statService.Stats;

        /// <summary>
        ///     Recalculates max values, regen rates, stat values, and pushes reactive properties. Publishes one
        ///     network snapshot of the finished refresh instead of one per refreshed stat.
        /// </summary>
        public void RefreshAllDerived(bool initial)
        {
            BeginPublication();
            try
            {
                RefreshAllDerivedCore(initial, null, _runtimeGeneration);
            }
            finally
            {
                EndPublication();
            }
        }

        private bool RefreshAllDerivedCore(bool initial,
            IReadOnlyDictionary<string, RpgResourceSaveEntry> restoreRows, int generation)
        {
            int derivedGeneration = ++_derivedGeneration;
            Func<bool> isCurrent = () => generation == _runtimeGeneration && derivedGeneration == _derivedGeneration;
            _effects.BuildModifierApplications(_modifierBuffer);

            // WHY: refresh stats before resources — resource regen may scale off stat values.
            foreach (KeyValuePair<string, RpgStatRuntime> kv in _statService.StatTable)
            {
                if (!RefreshStat(kv.Value, isCurrent))
                {
                    return false;
                }
            }

            IReadOnlyDictionary<string, RpgResourceSaveEntry> outerRestoreRows = _restoreRows;
            _restoreRows = restoreRows;
            try
            {
                _resourceService.RefreshDerived(_maxResolver ??= ResolveResourceMaxForRefresh,
                    _regenResolver ??= ResolveResourceRegen, initial, restoreRows, isCurrent);
            }
            finally
            {
                _restoreRows = outerRestoreRows;
            }

            if (!isCurrent())
            {
                return false;
            }
            IsDeadState.Value = _isDead;
            if (!isCurrent())
            {
                return false;
            }
            LevelState.Value = _progressionState.Level;
            return isCurrent();
        }

        // WHY: restore-call-only maxima; the saved effective Max wins for this pass and regen never reads restore rows.
        private float ResolveResourceMaxForRefresh(RpgResourceRuntime resource)
        {
            return _restoreRows != null && _restoreRows.TryGetValue(resource.Id, out RpgResourceSaveEntry saved)
                ? saved.Max
                : _statService.ResolveResourceMax(resource, _modifierBuffer);
        }

        private float ResolveResourceRegen(RpgResourceRuntime resource)
        {
            return _statService.ResolveRegen(resource, _modifierBuffer);
        }

        private void RefreshDerivedFromBuffs()
        {
            RefreshAllDerived(false);
        }

        private bool RefreshStat(RpgStatRuntime s, Func<bool> isCurrent)
        {
            float value = _statService.ResolveStat(s, _progressionState.AppliedGrowthLevel, _modifierBuffer);
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
            LiveNetworkAdapter?.NotifyStateChanged();
        }

        /// <summary>
        ///     Defers network snapshot publication until the returned scope is disposed. Nested scopes (including the
        ///     ones the character opens itself) publish one complete snapshot when the outermost scope ends, and only
        ///     when something changed. Wrap several mutations made in one step (a server command, a scripted combo)
        ///     so the adapter captures and serializes the final state once instead of after every mutation.
        /// </summary>
        /// <remarks>
        ///     Only the transport publication is deferred: UnityEvents, reactive states and C# events still fire
        ///     in place. Dispose each scope exactly once, preferably with <c>using</c>. Without a network adapter the
        ///     scope only counts nesting.
        /// </remarks>
        /// <example>
        ///     <code>
        /// using (character.DeferSnapshotPublication())
        /// {
        ///     character.Damage(25f);
        ///     character.AddXp(10f);
        /// }
        /// </code>
        /// </example>
        public SnapshotPublicationScope DeferSnapshotPublication()
        {
            BeginPublication();
            return new SnapshotPublicationScope(this);
        }

        /// <summary>
        ///     Scope returned by <see cref="DeferSnapshotPublication" />. Disposing it ends the deferral; a
        ///     <c>default</c> instance does nothing.
        /// </summary>
        public readonly struct SnapshotPublicationScope : IDisposable
        {
            private readonly RpgCharacter _owner;

            internal SnapshotPublicationScope(RpgCharacter owner)
            {
                _owner = owner;
            }

            /// <summary>Ends this deferral; the outermost one publishes the pending snapshot.</summary>
            public void Dispose()
            {
                // WHY: reference check, not Unity's null: a destroyed owner must still unwind its depth counter.
                if (!ReferenceEquals(_owner, null))
                {
                    _owner.EndPublication();
                }
            }
        }

        private void BeginPublication()
        {
            _publicationDepth++;
        }

        private void EndPublication()
        {
            // WHY: a public scope disposed twice must not drive the depth negative and disable publication.
            if (_publicationDepth <= 0)
            {
                return;
            }

            _publicationDepth--;
            if (_publicationDepth == 0 && !_initializing && _initialized && _snapshotPending)
            {
                PushSnapshotIfServer();
            }
        }

        private void ResolveNetworkAdapter()
        {
            // WHY: TryGetComponent with the interface type allocates neither an array nor an editor fake-null.
            _networkAdapter = TryGetComponent(out IRpgCharacterNetworkAdapter adapter) ? adapter : null;
        }

        /// <summary>The cached adapter, dropped once its component has been destroyed.</summary>
        private IRpgCharacterNetworkAdapter LiveNetworkAdapter
        {
            get
            {
                if (_networkAdapter is UnityEngine.Object adapterObject && adapterObject == null)
                {
                    _networkAdapter = null;
                }

                return _networkAdapter;
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

            if (LiveNetworkAdapter == null)
            {
                ResolveNetworkAdapter();
            }

            return _networkAdapter != null && _networkAdapter.TryRoute(command);
        }


        /// <summary>Serializes <see cref="CaptureProfile" /> to the save provider under the configured key.</summary>
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
                Level = _progressionState.Level,
                Xp = _progressionState.Xp,
                UpgradePoints = _progressionState.UpgradePoints,
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

            _statService.CaptureTo(data.Stats, data.Upgrades);

            _effects.CopyActiveEffectsTo(data.ActiveBuffs, data.ActiveStatuses);
            return data;
        }

        /// <summary>Loads and applies the profile stored under the configured key, then raises <see cref="OnProfileLoadedEvent" />.</summary>
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
                _progressionState.Restore(data.Level, data.Xp, data.UpgradePoints);
                // WHY: v2 has no applied-growth anchor. Deferred/manual profiles require an explicit apply after load.
                SyncedProgression.ResetGrowthAnchor();
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
                _statService.RestoreBases(data.Stats);
                _statService.RestoreInvestments(data.Upgrades);
                _effects.RestoreActiveEffects(data.ActiveBuffs, data.ActiveStatuses);
                // WHY: restore-call-only maxima; never copy effective Max into BaseMax or retain the lookup in regen.
                if (!RefreshAllDerivedCore(false, restoreRows, generation))
                {
                    return false;
                }
                XpState.Value = _progressionState.Xp;
                if (generation != _runtimeGeneration)
                {
                    return false;
                }
                UpgradePointsState.Value = _progressionState.UpgradePoints;
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

        /// <summary>Deletes the stored profile key; the live state is unchanged.</summary>
        public void ResetProfile()
        {
            if (string.IsNullOrWhiteSpace(_saveKey))
            {
                return;
            }

            SaveProvider.DeleteKey(_saveKey);
            SaveProvider.Save();
        }

        // WHY: compatibility wrappers retained on the local facade. The Mirror transport itself is supplied
        // by IRpgCharacterNetworkAdapter; without an adapter these remain ordinary local calls.
        /// <summary>Compatibility alias of <see cref="Damage" /> (routes through the network adapter when networked).</summary>
        public void NetDamage(float amount)
        {
            Damage(amount);
        }

        /// <summary>Compatibility alias of <see cref="DamageType" />.</summary>
        public void NetDamageType(string damageType, float amount)
        {
            DamageType(damageType, amount);
        }

        /// <summary>Compatibility alias of <see cref="Heal" />.</summary>
        public void NetHeal(float amount)
        {
            Heal(amount);
        }

        /// <summary>Compatibility alias of <see cref="Spend" />.</summary>
        public void NetSpend(string resourceId, float amount)
        {
            Spend(resourceId, amount);
        }

        /// <summary>Compatibility alias of <see cref="Refill" />.</summary>
        public void NetRefill(string resourceId, float amount)
        {
            Refill(resourceId, amount);
        }

        /// <summary>Compatibility alias of <see cref="ApplyBuffById" />.</summary>
        public void NetApplyBuffById(string id)
        {
            ApplyBuffById(id);
        }

        /// <summary>Compatibility alias of <see cref="ApplyInlineBuff" />.</summary>
        public void NetApplyInlineBuff(int index)
        {
            ApplyInlineBuff(index);
        }

        /// <summary>Compatibility alias of <see cref="ApplyStatusById" />.</summary>
        public void NetApplyStatusById(string id)
        {
            ApplyStatusById(id);
        }

        /// <summary>Compatibility alias of <see cref="AddLevel" />.</summary>
        public void NetAddLevel(int delta)
        {
            AddLevel(delta);
        }

    }
}
