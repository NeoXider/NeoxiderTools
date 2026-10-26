using System;
using System.Collections.Generic;
using Neo.Condition;
using Neo.Core.Level;
using Neo.Reactive;
using Neo.Save;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Progression
{
    /// <summary>
    ///     Main entry point for the new meta-progression system.
    /// </summary>
    [NeoDoc("Progression/ProgressionManager.md")]
    [CreateFromMenu("Neoxider/Progression/ProgressionManager")]
    [AddComponentMenu("Neoxider/Progression/" + nameof(ProgressionManager))]
    public sealed class ProgressionManager : Singleton<ProgressionManager>
    {
        private const int ProfileVersion = 2;
        private const string DefaultSaveKey = "ProgressionV2.Profile";

        [Header("Level (from Core)")]
        [Tooltip("Level/XP source; when null, level and XP are not used.")]
        [SerializeField]
        private LevelComponent _levelProvider;

        [Header("Reward Track And Trees")]
        [Tooltip("Reward table: level -> perk points and rewards. Used when level increases.")]
        [SerializeField]
        private LevelCurveDefinition _levelCurve;

        [SerializeField] private UnlockTreeDefinition _unlockTree;
        [SerializeField] private PerkTreeDefinition _perkTree;

        [Header("Persistence")] [SerializeField]
        private string _saveKey = DefaultSaveKey;

        [SerializeField] private bool _loadOnAwake = true;
        [SerializeField] private bool _autoSave = true;

        [Header("Context")] [SerializeField] private GameObject _conditionContext;

        [Header("Reactive State")] public ReactivePropertyInt XpState = new();
        public ReactivePropertyInt LevelState = new(1);
        public ReactivePropertyInt PerkPointsState = new();
        public ReactivePropertyInt XpToNextLevelState = new();

        [Header("Events")] [SerializeField] private UnityEventInt _onXpChanged = new();
        [SerializeField] private UnityEventInt _onLevelChanged = new();
        [SerializeField] private UnityEventInt _onPerkPointsChanged = new();
        [SerializeField] private ProgressionStringEvent _onNodeUnlocked = new();
        [SerializeField] private ProgressionStringEvent _onPerkPurchased = new();
        [SerializeField] private UnityEvent _onProfileLoaded = new();
        [SerializeField] private UnityEvent _onProfileSaved = new();
        [SerializeField] private UnityEvent _onProfileReset = new();
        private readonly HashSet<string> _purchasedPerkIds = new(StringComparer.Ordinal);

        private readonly HashSet<string> _unlockedNodeIds = new(StringComparer.Ordinal);
        private ProgressionProfileData _profile = new();
        private string _activeProfileKey;
        private int _lastRewardedLevel = 1;
        private bool _progressionInitialized;
        private bool _progressionInitializing;
        private bool _warnedMissingLevelProvider;
        private LevelComponent _boundLevelProvider;
        private UnityAction<int> _providerLevelUpListener;
        private IDisposable _providerXpSubscription;
        private IDisposable _providerLevelSubscription;
        private IDisposable _providerXpToNextSubscription;
        private long _providerBindingVersion;
        private long _runtimeNotificationVersion;
        private int _runtimeUpdateDepth;
        private bool _runtimeNotificationPending;
        private bool _isDestroyed;

        /// <summary>
        ///     Gets a backwards-compatible singleton alias.
        /// </summary>
        public new static ProgressionManager Instance => I;

        /// <summary>
        ///     Gets or sets the level/XP provider. A runtime swap rebinds reward and mirror subscriptions;
        ///     the incoming provider's current level is treated as already rewarded.
        /// </summary>
        public LevelComponent LevelProvider
        {
            get => _levelProvider;
            set
            {
                if (ReferenceEquals(_levelProvider, value))
                {
                    return;
                }

                _levelProvider = value;
                if (_progressionInitialized || _progressionInitializing)
                {
                    BeginRuntimeUpdate();
                    try
                    {
                        BindLevelProvider(out _);
                        RefreshRuntimeState(_progressionInitialized);
                    }
                    finally
                    {
                        EndRuntimeUpdate();
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets the level curve definition.
        /// </summary>
        public LevelCurveDefinition LevelCurve
        {
            get => _levelCurve;
            set => _levelCurve = value;
        }

        /// <summary>
        ///     Gets or sets the unlock tree definition.
        /// </summary>
        public UnlockTreeDefinition UnlockTree
        {
            get => _unlockTree;
            set => _unlockTree = value;
        }

        /// <summary>
        ///     Gets or sets the perk tree definition.
        /// </summary>
        public PerkTreeDefinition PerkTree
        {
            get => _perkTree;
            set => _perkTree = value;
        }

        /// <summary>
        ///     Gets or sets the save key used for the persistent profile payload.
        /// </summary>
        public string SaveKey
        {
            get => _saveKey;
            set => _saveKey = string.IsNullOrWhiteSpace(value) ? DefaultSaveKey : value.Trim();
        }

        /// <summary>
        ///     Gets or sets whether the profile should be saved automatically on changes.
        /// </summary>
        public bool AutoSave
        {
            get => _autoSave;
            set => _autoSave = value;
        }

        /// <summary>
        ///     Gets or sets the default context used by condition checks.
        /// </summary>
        public GameObject ConditionContext
        {
            get => _conditionContext;
            set => _conditionContext = value;
        }

        /// <summary>
        ///     Gets the total accumulated XP (from level provider when set).
        /// </summary>
        public int TotalXp => _levelProvider != null ? _levelProvider.TotalXp : 0;

        /// <summary>
        ///     Gets the resolved current level (from level provider when set).
        /// </summary>
        public int CurrentLevel => _levelProvider != null ? _levelProvider.Level : 1;

        /// <summary>
        ///     Gets the currently unspent perk points.
        /// </summary>
        public int AvailablePerkPoints => _profile.AvailablePerkPoints;

        /// <summary>
        ///     Gets whether the active profile has the premium track enabled.
        /// </summary>
        public bool HasPremium => _profile.HasPremium;

        /// <summary>
        ///     Gets the unlocked node identifiers.
        /// </summary>
        public IReadOnlyCollection<string> UnlockedNodeIds => _unlockedNodeIds;

        /// <summary>
        ///     Gets the purchased perk identifiers.
        /// </summary>
        public IReadOnlyCollection<string> PurchasedPerkIds => _purchasedPerkIds;

        /// <summary>
        ///     Gets the UnityEvent raised when total XP changes.
        /// </summary>
        public UnityEventInt OnXpChanged => _onXpChanged;

        /// <summary>
        ///     Gets the UnityEvent raised when the resolved level changes.
        /// </summary>
        public UnityEventInt OnLevelChanged => _onLevelChanged;

        /// <summary>
        ///     Gets the UnityEvent raised when available perk points change.
        /// </summary>
        public UnityEventInt OnPerkPointsChanged => _onPerkPointsChanged;

        /// <summary>
        ///     Gets the UnityEvent raised when an unlock node is granted.
        /// </summary>
        public ProgressionStringEvent OnNodeUnlocked => _onNodeUnlocked;

        /// <summary>
        ///     Gets the UnityEvent raised when a perk is purchased.
        /// </summary>
        public ProgressionStringEvent OnPerkPurchased => _onPerkPurchased;

        /// <summary>
        ///     Gets the Inspector UnityEvent raised after loading and applying a profile.
        /// </summary>
        public UnityEvent OnProfileLoaded => _onProfileLoaded;

        /// <summary>Current XP value (for NeoCondition and reactive state binding).</summary>
        public int XpStateValue => XpState.CurrentValue;

        /// <summary>Current level value (for NeoCondition and reactive state binding).</summary>
        public int LevelStateValue => LevelState.CurrentValue;

        /// <summary>Current unspent perk points (for NeoCondition and reactive state binding).</summary>
        public int PerkPointsStateValue => PerkPointsState.CurrentValue;

        /// <summary>XP remaining to the next level (for NeoCondition and reactive state binding).</summary>
        public int XpToNextLevelStateValue => XpToNextLevelState.CurrentValue;


        private void OnValidate()
        {
            _saveKey = string.IsNullOrWhiteSpace(_saveKey) ? DefaultSaveKey : _saveKey.Trim();
        }

        /// <summary>
        ///     Ensures the manager is initialized even when accessed outside the normal scene lifecycle.
        ///     Failed setup can be retried after correcting its dependency; unfinished reentrant use is rejected.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_progressionInitialized)
            {
                return;
            }

            if (_progressionInitializing)
            {
                throw new InvalidOperationException("Progression initialization is already in progress.");
            }

            Init();
        }

        /// <summary>
        ///     Assigns the main definitions in one call.
        /// </summary>
        public void SetDefinitions(LevelCurveDefinition levelCurve,
            UnlockTreeDefinition unlockTree,
            PerkTreeDefinition perkTree)
        {
            _levelCurve = levelCurve;
            _unlockTree = unlockTree;
            _perkTree = perkTree;
        }

        /// <summary>
        ///     Returns a deep copy of the current profile.
        /// </summary>
        public ProgressionProfileData GetProfileSnapshot()
        {
            EnsureInitialized();
            return _profile.Clone();
        }

        /// <summary>
        ///     Adds XP via level provider; level-up rewards are dispatched via OnLevelUp subscription.
        /// </summary>
        public void AddXp(int amount)
        {
            TryAddXp(amount, out _);
        }

        /// <summary>
        ///     Tries to add positive XP through the configured provider and returns a failure reason when unavailable.
        /// </summary>
        public bool TryAddXp(int amount, out string failReason)
        {
            EnsureInitialized();
            if (amount <= 0)
            {
                failReason = "XP amount must be greater than zero.";
                return false;
            }

            if (_levelProvider == null)
            {
                failReason = "Level provider is not assigned.";
                WarnMissingLevelProvider("XP was not added.");
                return false;
            }

            failReason = null;
            BeginRuntimeUpdate();
            try
            {
                _levelProvider.AddXp(amount);
                PersistAndNotify();
            }
            finally
            {
                EndRuntimeUpdate();
            }

            return true;
        }

        /// <summary>
        ///     Sets the level through the configured level provider.
        /// </summary>
        public void SetLevel(int level)
        {
            TrySetLevel(level, out _);
        }

        /// <summary>
        ///     Tries to set the level through the configured level provider and returns a fail reason when unavailable.
        /// </summary>
        public bool TrySetLevel(int level, out string failReason)
        {
            EnsureInitialized();
            if (_levelProvider == null)
            {
                failReason = "Level provider is not assigned.";
                WarnMissingLevelProvider("Level was not changed.");
                return false;
            }

            failReason = null;
            BeginRuntimeUpdate();
            try
            {
                _levelProvider.SetLevel(level);
                PersistAndNotify();
            }
            finally
            {
                EndRuntimeUpdate();
            }

            return true;
        }

        /// <summary>
        ///     Adds unspent perk points.
        /// </summary>
        public void AddPerkPoints(int amount)
        {
            EnsureInitialized();
            if (amount <= 0)
            {
                return;
            }

            _profile.AvailablePerkPoints = (int)Math.Min(int.MaxValue, (long)_profile.AvailablePerkPoints + amount);
            PersistAndNotify();
        }

        /// <summary>
        ///     Returns true when the supplied node has already been unlocked.
        /// </summary>
        public bool HasUnlockedNode(string nodeId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(nodeId) && _unlockedNodeIds.Contains(nodeId);
        }

        /// <summary>
        ///     Returns true when the supplied perk has already been purchased.
        /// </summary>
        public bool HasPurchasedPerk(string perkId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(perkId) && _purchasedPerkIds.Contains(perkId);
        }

        /// <summary>
        ///     Tries to resolve an unlock node definition.
        /// </summary>
        public bool TryGetNodeDefinition(string nodeId, out UnlockNodeDefinition node)
        {
            EnsureInitialized();
            if (_unlockTree != null)
            {
                return _unlockTree.TryGetNode(nodeId, out node);
            }

            node = null;
            return false;
        }

        /// <summary>
        ///     Tries to resolve a perk definition.
        /// </summary>
        public bool TryGetPerkDefinition(string perkId, out PerkDefinition perk)
        {
            EnsureInitialized();
            if (_perkTree != null)
            {
                return _perkTree.TryGetPerk(perkId, out perk);
            }

            perk = null;
            return false;
        }

        /// <summary>
        ///     Evaluates whether the supplied node can be unlocked.
        /// </summary>
        public bool CanUnlockNode(string nodeId, out string failReason)
        {
            EnsureInitialized();
            failReason = null;

            if (!_unlockTree || !_unlockTree.TryGetNode(nodeId, out UnlockNodeDefinition node))
            {
                failReason = "Unlock node definition not found.";
                return false;
            }

            if (_unlockedNodeIds.Contains(node.Id))
            {
                failReason = "Unlock node is already unlocked.";
                return false;
            }

            if (CurrentLevel < node.RequiredLevel)
            {
                failReason = $"Unlock node requires level {node.RequiredLevel}.";
                return false;
            }

            IReadOnlyList<string> prerequisites = node.PrerequisiteNodeIds;
            for (int i = 0; i < prerequisites.Count; i++)
            {
                string prerequisiteId = prerequisites[i];
                if (!string.IsNullOrWhiteSpace(prerequisiteId) && !_unlockedNodeIds.Contains(prerequisiteId))
                {
                    failReason = $"Unlock node requires prerequisite '{prerequisiteId}'.";
                    return false;
                }
            }

            if (!EvaluateConditions(node.Conditions))
            {
                failReason = "Unlock node conditions are not met.";
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Unlocks a node when all requirements are met.
        /// </summary>
        public bool TryUnlockNode(string nodeId, out string failReason)
        {
            if (!CanUnlockNode(nodeId, out failReason))
            {
                return false;
            }

            _unlockedNodeIds.Add(nodeId);
            SyncProfileListsFromSets();

            if (_unlockTree != null && _unlockTree.TryGetNode(nodeId, out UnlockNodeDefinition node))
            {
                ProgressionRewardDispatcher.DispatchRewards(node.Rewards, this);
            }

            PersistAndNotify();
            _onNodeUnlocked?.Invoke(nodeId);
            return true;
        }

        /// <summary>
        ///     Evaluates whether the supplied perk can be purchased.
        /// </summary>
        public bool CanBuyPerk(string perkId, out string failReason)
        {
            EnsureInitialized();
            failReason = null;

            if (!_perkTree || !_perkTree.TryGetPerk(perkId, out PerkDefinition perk))
            {
                failReason = "Perk definition not found.";
                return false;
            }

            if (_purchasedPerkIds.Contains(perk.Id))
            {
                failReason = "Perk is already purchased.";
                return false;
            }

            if (CurrentLevel < perk.RequiredLevel)
            {
                failReason = $"Perk requires level {perk.RequiredLevel}.";
                return false;
            }

            if (_profile.AvailablePerkPoints < perk.Cost)
            {
                failReason = "Not enough perk points.";
                return false;
            }

            IReadOnlyList<string> prerequisitePerks = perk.PrerequisitePerkIds;
            for (int i = 0; i < prerequisitePerks.Count; i++)
            {
                string prerequisitePerkId = prerequisitePerks[i];
                if (!string.IsNullOrWhiteSpace(prerequisitePerkId) && !_purchasedPerkIds.Contains(prerequisitePerkId))
                {
                    failReason = $"Perk requires prerequisite '{prerequisitePerkId}'.";
                    return false;
                }
            }

            IReadOnlyList<string> requiredUnlockNodes = perk.RequiredUnlockNodeIds;
            for (int i = 0; i < requiredUnlockNodes.Count; i++)
            {
                string requiredNodeId = requiredUnlockNodes[i];
                if (!string.IsNullOrWhiteSpace(requiredNodeId) && !_unlockedNodeIds.Contains(requiredNodeId))
                {
                    failReason = $"Perk requires unlocked node '{requiredNodeId}'.";
                    return false;
                }
            }

            if (!EvaluateConditions(perk.Conditions))
            {
                failReason = "Perk conditions are not met.";
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Buys a perk when all requirements are met.
        /// </summary>
        public bool TryBuyPerk(string perkId, out string failReason)
        {
            if (!CanBuyPerk(perkId, out failReason))
            {
                return false;
            }

            PerkDefinition perk = null;
            if (_perkTree != null)
            {
                _perkTree.TryGetPerk(perkId, out perk);
            }

            if (perk == null)
            {
                failReason = "Perk definition not found.";
                return false;
            }

            _profile.AvailablePerkPoints -= perk.Cost;
            _purchasedPerkIds.Add(perkId);
            SyncProfileListsFromSets();
            ProgressionRewardDispatcher.DispatchRewards(perk.Rewards, this);
            PersistAndNotify();
            _onPerkPurchased?.Invoke(perkId);
            return true;
        }

        /// <summary>
        ///     Clears the current progression profile and reapplies defaults.
        /// </summary>
        public void ResetProgression()
        {
            EnsureInitialized();
            BeginRuntimeUpdate();
            try
            {
                if (_levelProvider != null)
                {
                    _levelProvider.ResetProgress();
                    _levelProvider.Save();
                }

                ApplyProfile(new ProgressionProfileData(), true, _saveKey, true);
                if (_autoSave)
                {
                    SaveProfile();
                }
            }
            finally
            {
                EndRuntimeUpdate();
            }

            _onProfileReset?.Invoke();
        }

        /// <summary>
        ///     Activates the premium track and retroactively grants premium rewards for already achieved levels.
        /// </summary>
        public void ActivatePremium()
        {
            EnsureInitialized();
            if (_profile.HasPremium)
            {
                return;
            }

            // Save callbacks can also advance the provider, so capture the replay boundary before publishing premium.
            int achievedLevel = CurrentLevel;
            BeginRuntimeUpdate();
            try
            {
                _profile.HasPremium = true;
                if (_autoSave)
                {
                    SaveProfile();
                }

                if (_levelProvider != null && _levelCurve != null)
                {
                    // XP rewards can reach new levels; those are already rewarded by the level-up listener.
                    for (long level = 1; level <= achievedLevel; level++)
                    {
                        if (_levelCurve.TryGetDefinition((int)level, out ProgressionLevelDefinition definition))
                        {
                            ProgressionRewardDispatcher.DispatchRewards(definition.Rewards, this, true);
                        }
                    }
                }

                PersistAndNotify();
            }
            finally
            {
                EndRuntimeUpdate();
            }
        }

        /// <summary>
        ///     Loads the selected profile from the save provider. Reloading the same active key preserves
        ///     this session's highest rewarded level; a different key establishes a new baseline.
        /// </summary>
        public void LoadProfile()
        {
            EnsureInitialized();
            LoadProfileInternal(true);
        }

        /// <summary>
        ///     Saves the profile through the active save provider.
        /// </summary>
        public void SaveProfile()
        {
            EnsureInitialized();
            SyncProfileListsFromSets();
            _profile.Version = ProfileVersion;
            string json = JsonUtility.ToJson(_profile, true);
            SaveProvider.SetString(_saveKey, json);
            SaveProvider.Save();
            _onProfileSaved?.Invoke();
        }

        protected override void Init()
        {
            if (_progressionInitialized || _progressionInitializing)
            {
                return;
            }

            _progressionInitializing = true;
            bool bindingStarted = false;
            long bindingVersion = _providerBindingVersion;
            try
            {
                _saveKey = string.IsNullOrWhiteSpace(_saveKey) ? DefaultSaveKey : _saveKey.Trim();
                if (_levelProvider == null)
                {
                    TryGetComponent(out _levelProvider);
                }

                bindingStarted = true;
                BindLevelProvider(out bindingVersion);

                if (_loadOnAwake)
                {
                    LoadProfileInternal(true);
                }
                else
                {
                    ApplyProfile(new ProgressionProfileData(), true, _saveKey);
                }
            }
            catch
            {
                // A completed profile and any newer reentrant binding belong to their successful callers.
                if (!_progressionInitialized && bindingStarted && bindingVersion == _providerBindingVersion)
                {
                    UnbindLevelProvider();
                }

                throw;
            }
            finally
            {
                _progressionInitializing = false;
            }
        }

        private void LoadProfileInternal(bool invokeEvents)
        {
            string profileKey = _saveKey;
            string json = SaveProvider.GetString(profileKey, string.Empty);
            ProgressionProfileData loadedProfile = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    loadedProfile = JsonUtility.FromJson<ProgressionProfileData>(json);
                }
                catch (Exception exception)
                {
                    NeoDiagnostics.LogWarning(
                        $"[ProgressionManager] Failed to deserialize profile '{profileKey}': {exception.Message}");
                }
            }

            ApplyProfile(loadedProfile ?? new ProgressionProfileData(), invokeEvents, profileKey);
            if (invokeEvents)
            {
                _onProfileLoaded?.Invoke();
            }
        }

        private void ApplyProfile(ProgressionProfileData profile, bool invokeEvents, string profileKey,
            bool resetRewardWatermark = false)
        {
            bool preserveRewardWatermark = !resetRewardWatermark &&
                                           string.Equals(_activeProfileKey, profileKey, StringComparison.Ordinal);
            _profile = profile ?? new ProgressionProfileData();
            _profile.Version = ProfileVersion;
            _profile.Sanitize();

            _unlockedNodeIds.Clear();
            _purchasedPerkIds.Clear();
            ApplyDefaultStates();
            ImportProfileSets();
            SyncProfileListsFromSets();
            int incomingLevel = Mathf.Max(1, CurrentLevel);
            _lastRewardedLevel = preserveRewardWatermark ? Mathf.Max(_lastRewardedLevel, incomingLevel) : incomingLevel;
            _activeProfileKey = profileKey;
            // Commit readiness before external notifications: their failure must not replay profile initialization.
            base.Init();
            _progressionInitialized = true;
            RefreshRuntimeState(invokeEvents);
        }

        private void ApplyDefaultStates()
        {
            if (_unlockTree != null)
            {
                IReadOnlyList<UnlockNodeDefinition> nodes = _unlockTree.Nodes;
                for (int i = 0; i < nodes.Count; i++)
                {
                    UnlockNodeDefinition node = nodes[i];
                    if (node != null && node.UnlockedByDefault && !string.IsNullOrWhiteSpace(node.Id))
                    {
                        _unlockedNodeIds.Add(node.Id);
                    }
                }
            }

            if (_perkTree != null)
            {
                IReadOnlyList<PerkDefinition> perks = _perkTree.Perks;
                for (int i = 0; i < perks.Count; i++)
                {
                    PerkDefinition perk = perks[i];
                    if (perk != null && perk.PurchasedByDefault && !string.IsNullOrWhiteSpace(perk.Id))
                    {
                        _purchasedPerkIds.Add(perk.Id);
                    }
                }
            }
        }

        private void ImportProfileSets()
        {
            for (int i = 0; i < _profile.UnlockedNodeIds.Count; i++)
            {
                string nodeId = _profile.UnlockedNodeIds[i];
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    _unlockedNodeIds.Add(nodeId);
                }
            }

            for (int i = 0; i < _profile.PurchasedPerkIds.Count; i++)
            {
                string perkId = _profile.PurchasedPerkIds[i];
                if (!string.IsNullOrWhiteSpace(perkId))
                {
                    _purchasedPerkIds.Add(perkId);
                }
            }
        }

        private void SyncProfileListsFromSets()
        {
            _profile.UnlockedNodeIds.Clear();
            foreach (string nodeId in _unlockedNodeIds)
            {
                _profile.UnlockedNodeIds.Add(nodeId);
            }

            _profile.PurchasedPerkIds.Clear();
            foreach (string perkId in _purchasedPerkIds)
            {
                _profile.PurchasedPerkIds.Add(perkId);
            }
        }

        private void DispatchRewardsForLevel(int level)
        {
            if (level <= _lastRewardedLevel)
            {
                return;
            }

            BeginRuntimeUpdate();
            try
            {
                // WHY: reward dispatch can add XP and re-enter this handler; re-reading _lastRewardedLevel
                // each iteration prevents double-dispatching levels the nested call already rewarded.
                while (_lastRewardedLevel < level)
                {
                    int nextLevel = _lastRewardedLevel + 1;
                    _lastRewardedLevel = nextLevel;
                    DispatchRewardsForSingleLevel(nextLevel);
                }

                PersistAndNotify();
            }
            finally
            {
                EndRuntimeUpdate();
            }
        }

        private void DispatchRewardsForSingleLevel(int level)
        {
            if (_levelCurve == null || !_levelCurve.TryGetDefinition(level, out ProgressionLevelDefinition definition))
            {
                return;
            }

            if (definition.GrantedPerkPoints > 0)
            {
                _profile.AvailablePerkPoints = (int)Math.Min(int.MaxValue,
                    (long)_profile.AvailablePerkPoints + definition.GrantedPerkPoints);
            }

            ProgressionRewardDispatcher.DispatchRewards(definition.Rewards, this);
        }

        private bool EvaluateConditions(IReadOnlyList<ConditionEntry> conditions)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            GameObject context = _conditionContext != null ? _conditionContext : gameObject;
            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionEntry condition = conditions[i];
                if (condition != null && !condition.Evaluate(context))
                {
                    return false;
                }
            }

            return true;
        }

        private void PersistAndNotify()
        {
            BeginRuntimeUpdate();
            try
            {
                SyncProfileListsFromSets();
                if (_autoSave)
                {
                    SaveProfile();
                }

                RefreshRuntimeState(true);
            }
            finally
            {
                EndRuntimeUpdate();
            }
        }

        private void RefreshRuntimeState(bool invokeEvents, bool onlyIfChanged = false)
        {
            if (_isDestroyed)
            {
                return;
            }

            int level = CurrentLevel;
            int totalXp = TotalXp;
            int xpToNext = _levelProvider != null ? _levelProvider.XpToNextLevel : 0;
            int perkPoints = _profile.AvailablePerkPoints;
            bool changed = XpState.CurrentValue != totalXp || LevelState.CurrentValue != level ||
                           PerkPointsState.CurrentValue != perkPoints || XpToNextLevelState.CurrentValue != xpToNext;
            XpState.SetValueWithoutNotify(totalXp);
            LevelState.SetValueWithoutNotify(level);
            PerkPointsState.SetValueWithoutNotify(perkPoints);
            XpToNextLevelState.SetValueWithoutNotify(xpToNext);

            if (!invokeEvents || (onlyIfChanged && !changed))
            {
                return;
            }

            if (_runtimeUpdateDepth > 0)
            {
                _runtimeNotificationPending = true;
                return;
            }

            _runtimeNotificationPending = false;
            long notificationVersion = ++_runtimeNotificationVersion;
            XpState.ForceNotify();
            if (notificationVersion != _runtimeNotificationVersion || _isDestroyed)
            {
                return;
            }

            LevelState.ForceNotify();
            if (notificationVersion != _runtimeNotificationVersion || _isDestroyed)
            {
                return;
            }

            PerkPointsState.ForceNotify();
            if (notificationVersion != _runtimeNotificationVersion || _isDestroyed)
            {
                return;
            }

            XpToNextLevelState.ForceNotify();
            if (notificationVersion != _runtimeNotificationVersion || _isDestroyed)
            {
                return;
            }

            _onXpChanged?.Invoke(totalXp);
            if (notificationVersion != _runtimeNotificationVersion || _isDestroyed)
            {
                return;
            }

            _onLevelChanged?.Invoke(level);
            if (notificationVersion != _runtimeNotificationVersion || _isDestroyed)
            {
                return;
            }

            _onPerkPointsChanged?.Invoke(perkPoints);
        }

        private void BeginRuntimeUpdate()
        {
            _runtimeUpdateDepth++;
        }

        private void EndRuntimeUpdate()
        {
            _runtimeUpdateDepth--;
            if (_runtimeUpdateDepth == 0 && _runtimeNotificationPending)
            {
                RefreshRuntimeState(true);
            }
        }

        private void BindLevelProvider(out long attemptedBindingVersion)
        {
            UnbindLevelProvider();
            long bindingVersion = _providerBindingVersion;
            attemptedBindingVersion = bindingVersion;
            LevelComponent provider = _levelProvider;
            if (provider == null)
            {
                return;
            }

            try
            {
                provider.EnsureInitialized();
                if (bindingVersion != _providerBindingVersion || !ReferenceEquals(_levelProvider, provider) || _isDestroyed)
                {
                    return;
                }

                _boundLevelProvider = provider;
                _lastRewardedLevel = Mathf.Max(1, provider.Level);
                UnityAction<int> mirrorListener = value =>
                {
                    if (_progressionInitialized && bindingVersion == _providerBindingVersion &&
                        ReferenceEquals(_levelProvider, provider))
                    {
                        // Mirror-only updates never persist a profile or change reward eligibility.
                        RefreshRuntimeState(true, true);
                    }
                };
                _providerLevelUpListener = level =>
                {
                    if (_progressionInitialized && bindingVersion == _providerBindingVersion &&
                        ReferenceEquals(_levelProvider, provider))
                    {
                        DispatchRewardsForLevel(level);
                    }
                };

                provider.OnLevelUp.AddListener(_providerLevelUpListener);
                _providerXpSubscription = provider.XpState.Subscribe(mirrorListener);
                _providerLevelSubscription = provider.LevelState.Subscribe(mirrorListener);
                _providerXpToNextSubscription = provider.XpToNextLevelState.Subscribe(mirrorListener);
            }
            catch
            {
                if (bindingVersion == _providerBindingVersion)
                {
                    UnbindLevelProvider();
                }

                throw;
            }
        }

        private void UnbindLevelProvider()
        {
            _providerBindingVersion++;
            if (!ReferenceEquals(_boundLevelProvider, null) && _providerLevelUpListener != null)
            {
                _boundLevelProvider.OnLevelUp.RemoveListener(_providerLevelUpListener);
            }

            _providerXpSubscription?.Dispose();
            _providerLevelSubscription?.Dispose();
            _providerXpToNextSubscription?.Dispose();
            _providerXpSubscription = null;
            _providerLevelSubscription = null;
            _providerXpToNextSubscription = null;
            _providerLevelUpListener = null;
            _boundLevelProvider = null;
        }

        private void WarnMissingLevelProvider(string suffix)
        {
            if (_warnedMissingLevelProvider)
            {
                return;
            }

            _warnedMissingLevelProvider = true;
            NeoDiagnostics.LogWarning($"[ProgressionManager] Level Provider is not assigned. {suffix}", this);
        }

        protected override void OnDestroy()
        {
            _isDestroyed = true;
            _runtimeNotificationPending = false;
            UnbindLevelProvider();
            base.OnDestroy();
        }
    }
}
