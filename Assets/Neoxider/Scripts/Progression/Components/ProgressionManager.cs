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

        [Header("Definitions")]
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
        private bool _progressionInitialized;

        /// <summary>
        ///     Gets a backwards-compatible singleton alias.
        /// </summary>
        public static ProgressionManager Instance => I;

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
        /// </summary>
        public void EnsureInitialized()
        {
            if (!_progressionInitialized)
            {
                Init();
            }
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
            EnsureInitialized();
            if (amount <= 0)
            {
                return;
            }

            if (_levelProvider != null)
            {
                _levelProvider.AddXp(amount);
            }

            PersistAndNotify();
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

            _profile.AvailablePerkPoints += amount;
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
            
            if (_levelProvider != null)
            {
                _levelProvider.Reset();
                _levelProvider.Save();
            }

            ApplyProfile(new ProgressionProfileData(), true);
            if (_autoSave)
            {
                SaveProfile();
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

            _profile.HasPremium = true;
            if (_autoSave)
            {
                SaveProfile();
            }

            if (_levelProvider != null && _levelCurve != null)
            {
                for (int level = 1; level <= _levelProvider.Level; level++)
                {
                    if (_levelCurve.TryGetDefinition(level, out ProgressionLevelDefinition definition))
                    {
                        ProgressionRewardDispatcher.DispatchRewards(definition.Rewards, this, premiumOnly: true);
                    }
                }
            }

            PersistAndNotify();
        }

        /// <summary>
        ///     Loads the profile from the active save provider.
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
            _onProfileSaved?.Invoke();
        }

        protected override void Init()
        {
            base.Init();
            if (_progressionInitialized)
            {
                return;
            }

            _progressionInitialized = true;
            _saveKey = string.IsNullOrWhiteSpace(_saveKey) ? DefaultSaveKey : _saveKey.Trim();
            if (_levelProvider != null)
            {
                _levelProvider.OnLevelUp.AddListener(DispatchRewardsForLevel);
            }

            if (_loadOnAwake)
            {
                LoadProfileInternal(true);
            }
            else
            {
                ApplyProfile(new ProgressionProfileData(), true);
            }
        }

        private void LoadProfileInternal(bool invokeEvents)
        {
            string json = SaveProvider.GetString(_saveKey, string.Empty);
            ProgressionProfileData loadedProfile = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    loadedProfile = JsonUtility.FromJson<ProgressionProfileData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[ProgressionManager] Failed to deserialize profile '{_saveKey}': {exception.Message}");
                }
            }

            ApplyProfile(loadedProfile ?? new ProgressionProfileData(), invokeEvents);
            if (invokeEvents)
            {
                _onProfileLoaded?.Invoke();
            }
        }

        private void ApplyProfile(ProgressionProfileData profile, bool invokeEvents)
        {
            _profile = profile ?? new ProgressionProfileData();
            _profile.Version = ProfileVersion;
            _profile.Sanitize();

            _unlockedNodeIds.Clear();
            _purchasedPerkIds.Clear();
            ApplyDefaultStates();
            ImportProfileSets();
            SyncProfileListsFromSets();
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
            if (_levelCurve == null || !_levelCurve.TryGetDefinition(level, out ProgressionLevelDefinition definition))
            {
                return;
            }

            if (definition.GrantedPerkPoints > 0)
            {
                _profile.AvailablePerkPoints += definition.GrantedPerkPoints;
            }

            ProgressionRewardDispatcher.DispatchRewards(definition.Rewards, this);
            PersistAndNotify();
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
            SyncProfileListsFromSets();
            if (_autoSave)
            {
                SaveProfile();
            }
            RefreshRuntimeState(true);
        }

        private void RefreshRuntimeState(bool invokeEvents)
        {
            int level = CurrentLevel;
            int totalXp = TotalXp;
            int xpToNext = _levelProvider != null ? _levelProvider.XpToNextLevel : 0;
            XpState.SetValueWithoutNotify(totalXp);
            LevelState.SetValueWithoutNotify(level);
            PerkPointsState.SetValueWithoutNotify(_profile.AvailablePerkPoints);
            XpToNextLevelState.SetValueWithoutNotify(xpToNext);

            if (!invokeEvents)
            {
                return;
            }

            XpState.ForceNotify();
            LevelState.ForceNotify();
            PerkPointsState.ForceNotify();
            XpToNextLevelState.ForceNotify();
            _onXpChanged?.Invoke(totalXp);
            _onLevelChanged?.Invoke(level);
            _onPerkPointsChanged?.Invoke(_profile.AvailablePerkPoints);
        }
    }
}
