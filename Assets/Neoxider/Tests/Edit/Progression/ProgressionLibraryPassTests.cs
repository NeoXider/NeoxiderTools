using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Neo.Condition;
using Neo.Core.Level;
using Neo.Editor.Tests;
using Neo.Save;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Progression.Tests
{
    /// <summary>
    ///     Library pass 2026-11: re-entrant unlock/purchase claims, settled notifications for multi-reward claims,
    ///     sparse reward-track walking, allocation-free reward dispatch and the premium NoCode/condition paths.
    /// </summary>
    public sealed class ProgressionLibraryPassTests
    {
        private const int Iterations = 200;

        private static readonly FieldInfo SingletonInstanceField = typeof(Singleton<ProgressionManager>)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo SingletonSearchField = typeof(Singleton<ProgressionManager>)
            .GetField("_searchFailed", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<Object> _ownedObjects = new();
        private ISaveProvider _previousProvider;
        private object _previousSingleton;
        private bool _previousSearchFailed;
        private bool _previousCreateInstance;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(new MemorySaveProvider());
            _previousSingleton = SingletonInstanceField.GetValue(null);
            _previousSearchFailed = (bool)SingletonSearchField.GetValue(null);
            _previousCreateInstance = Singleton<ProgressionManager>.CreateInstance;
            SingletonInstanceField.SetValue(null, null);
            SingletonSearchField.SetValue(null, true);
            Singleton<ProgressionManager>.CreateInstance = false;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _ownedObjects.Count - 1; i >= 0; i--)
                {
                    if (_ownedObjects[i] != null)
                    {
                        Object.DestroyImmediate(_ownedObjects[i]);
                    }
                }
            }
            finally
            {
                _ownedObjects.Clear();
                SingletonInstanceField.SetValue(null, _previousSingleton);
                SingletonSearchField.SetValue(null, _previousSearchFailed);
                Singleton<ProgressionManager>.CreateInstance = _previousCreateInstance;
                SaveProvider.SetProvider(_previousProvider);
            }
        }

        [Test]
        public void TryBuyPerk_ConditionBuysSamePerkReentrantly_ChargesCostAndRewardsOnce()
        {
            ProgressionManager manager = CreateManager();
            Audit20261023NeoQuestConditionProbe probe = CreateProbe();
            manager.PerkTree = CreatePerkTree(CreatePerk("dash", 2, ProbeCondition(probe),
                CreateReward(ProgressionRewardType.PerkPoints, 1)));
            manager.AddPerkPoints(5);
            bool nestedBought = false;
            string nestedReason = "not run";
            probe.OnRead = () => nestedBought = manager.TryBuyPerk("dash", out nestedReason);
            List<string> purchased = new();
            int typedPurchases = 0;
            manager.OnPerkPurchased.AddListener(purchased.Add);
            manager.PerkPurchased += id => typedPurchases++;

            bool outerBought = manager.TryBuyPerk("dash", out string outerReason);

            Assert.That(nestedBought, Is.True, nestedReason);
            Assert.That(outerBought, Is.False);
            Assert.That(outerReason, Is.EqualTo("Perk is already purchased."));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(4), "5 - cost 2 + reward 1, charged and granted once");
            Assert.That(purchased, Is.EqualTo(new[] { "dash" }));
            Assert.That(typedPurchases, Is.EqualTo(1));
            Assert.That(manager.HasPurchasedPerk("dash"), Is.True);
        }

        [Test]
        public void TryUnlockNode_ConditionUnlocksSameNodeReentrantly_DispatchesRewardsOnce()
        {
            ProgressionManager manager = CreateManager();
            Audit20261023NeoQuestConditionProbe probe = CreateProbe();
            manager.UnlockTree = CreateUnlockTree(CreateNode("gate", ProbeCondition(probe),
                CreateReward(ProgressionRewardType.PerkPoints, 3)));
            bool nestedUnlocked = false;
            probe.OnRead = () => nestedUnlocked = manager.TryUnlockNode("gate", out _);
            List<string> unlocked = new();
            int typedUnlocks = 0;
            manager.OnNodeUnlocked.AddListener(unlocked.Add);
            manager.NodeUnlocked += id => typedUnlocks++;

            bool outerUnlocked = manager.TryUnlockNode("gate", out string outerReason);

            Assert.That(nestedUnlocked, Is.True);
            Assert.That(outerUnlocked, Is.False);
            Assert.That(outerReason, Is.EqualTo("Unlock node is already unlocked."));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(3));
            Assert.That(unlocked, Is.EqualTo(new[] { "gate" }));
            Assert.That(typedUnlocks, Is.EqualTo(1));
        }

        [Test]
        public void TryUnlockNode_WithSeveralRewards_PublishesOneSettledSnapshotBeforeNodeEvent()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider(true));
            manager.UnlockTree = CreateUnlockTree(CreateNode("vault", null,
                CreateReward(ProgressionRewardType.PerkPoints, 1),
                CreateReward(ProgressionRewardType.PerkPoints, 2),
                CreateReward(ProgressionRewardType.Xp, 50)));
            manager.EnsureInitialized();
            List<string> sequence = new();
            manager.OnPerkPointsChanged.AddListener(points => sequence.Add("points:" + points));
            manager.OnXpChanged.AddListener(xp => sequence.Add("xp:" + xp));
            manager.NodeUnlocked += id => sequence.Add("node:" + id);

            Assert.That(manager.TryUnlockNode("vault", out string reason), Is.True, reason);

            Assert.That(sequence, Is.EqualTo(new[] { "xp:50", "points:3", "node:vault" }));
            Assert.That(manager.PerkPointsStateValue, Is.EqualTo(3));
            Assert.That(manager.XpStateValue, Is.EqualTo(50));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnlockAndPurchase_SnapshotAndSavedProfile_ListTheNewIds(bool autoSave)
        {
            ProgressionManager manager = CreateManager();
            manager.UnlockTree = CreateUnlockTree(CreateNode("gate", null,
                CreateReward(ProgressionRewardType.PerkPoints, 2)));
            manager.PerkTree = CreatePerkTree(CreatePerk("dash", 2, null));
            manager.AutoSave = autoSave;
            manager.EnsureInitialized();
            List<string> nodesSeenByEvent = null;
            manager.NodeUnlocked += id => nodesSeenByEvent = manager.GetProfileSnapshot().UnlockedNodeIds;

            Assert.That(manager.TryUnlockNode("gate", out string unlockReason), Is.True, unlockReason);
            Assert.That(manager.TryBuyPerk("dash", out string buyReason), Is.True, buyReason);

            Assert.That(nodesSeenByEvent, Is.EqualTo(new[] { "gate" }), "a snapshot inside the event lists the node");
            ProgressionProfileData snapshot = manager.GetProfileSnapshot();
            Assert.That(snapshot.UnlockedNodeIds, Is.EqualTo(new[] { "gate" }));
            Assert.That(snapshot.PurchasedPerkIds, Is.EqualTo(new[] { "dash" }));
            Assert.That(SaveProvider.HasKey(manager.SaveKey), Is.EqualTo(autoSave));

            manager.SaveProfile();

            ProgressionProfileData stored =
                JsonUtility.FromJson<ProgressionProfileData>(SaveProvider.GetString(manager.SaveKey));
            Assert.That(stored.UnlockedNodeIds, Is.EqualTo(new[] { "gate" }));
            Assert.That(stored.PurchasedPerkIds, Is.EqualTo(new[] { "dash" }));
        }

        [Test]
        public void LevelJumpAndPremiumReplay_OnSparseTrack_VisitOnlyDefinedLevels()
        {
            LevelComponent provider = CreateLevelProvider(false);
            ProgressionManager manager = CreateManager(provider);
            manager.LevelCurve = CreateRewardTrack(
                CreateLevel(2, 1),
                CreateLevel(int.MaxValue, 10, CreateReward(ProgressionRewardType.PerkPoints, 5, true)));
            manager.EnsureInitialized();
            Stopwatch stopwatch = Stopwatch.StartNew();

            manager.SetLevel(int.MaxValue);
            manager.ActivatePremium();

            stopwatch.Stop();
            Assert.That(manager.CurrentLevel, Is.EqualTo(int.MaxValue));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(16), "1 + 10 on level-up, 5 premium on replay");
            // WHY: walking every numeric level up to int.MaxValue took seconds; walking defined entries is instant.
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LevelJump_AscendingAndUnsortedTracks_DispatchFirstDuplicatesInLevelOrder(bool ascending)
        {
            LevelComponent provider = CreateLevelProvider(false);
            ProgressionManager manager = CreateManager(provider);
            ProgressionLevelDefinition two = CreateLevel(2, 0, CreateReward(ProgressionRewardType.PerkPoints, 1));
            ProgressionLevelDefinition twoDuplicate =
                CreateLevel(2, 0, CreateReward(ProgressionRewardType.PerkPoints, 5000));
            ProgressionLevelDefinition four = CreateLevel(4, 0, CreateReward(ProgressionRewardType.PerkPoints, 10));
            ProgressionLevelDefinition seven = CreateLevel(7, 0, CreateReward(ProgressionRewardType.PerkPoints, 100));
            ProgressionLevelDefinition nine = CreateLevel(9, 0, CreateReward(ProgressionRewardType.PerkPoints, 1000));
            manager.LevelCurve = ascending
                ? CreateRewardTrack(two, twoDuplicate, four, null, seven, nine)
                : CreateRewardTrack(nine, null, four, two, seven, twoDuplicate);
            manager.AutoSave = true;
            manager.EnsureInitialized();
            // WHY: every perk-point reward saves the profile, so the sequence of saved balances shows the order in
            // which the entries were dispatched (each entry grants a different power of ten).
            List<int> balances = new();
            manager.OnProfileSaved.AddListener(() =>
            {
                int balance = manager.AvailablePerkPoints;
                if (balances.Count == 0 || balances[balances.Count - 1] != balance)
                {
                    balances.Add(balance);
                }
            });

            manager.SetLevel(8);

            Assert.That(manager.LevelCurve.IsAscending(), Is.EqualTo(ascending));
            Assert.That(balances, Is.EqualTo(new[] { 1, 11, 111 }),
                "levels 2, 4 and 7 in order; the first level-2 entry wins; level 9 is above the jump");

            balances.Clear();
            manager.SetLevel(20);

            Assert.That(balances, Is.EqualTo(new[] { 1111 }));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(1111), "the duplicate level-2 entry never pays");
        }

        [Test]
        public void LevelJumpAndPremiumReplay_OnLargeAscendingTrack_WalkEveryEntryOnce()
        {
            const int entryCount = 100000;
            FieldInfo levelField = typeof(ProgressionLevelDefinition).GetField("_level",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo pointsField = typeof(ProgressionLevelDefinition).GetField("_grantedPerkPoints",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(levelField, Is.Not.Null);
            Assert.That(pointsField, Is.Not.Null);
            ProgressionLevelDefinition[] entries = new ProgressionLevelDefinition[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                if (i % 1000 == 0)
                {
                    entries[i] = CreateLevel(i + 2, 1, CreateReward(ProgressionRewardType.PerkPoints, 1, true));
                    continue;
                }

                ProgressionLevelDefinition entry = new();
                levelField.SetValue(entry, i + 2);
                pointsField.SetValue(entry, 1);
                entries[i] = entry;
            }

            LevelComponent provider = CreateLevelProvider(false);
            ProgressionManager manager = CreateManager(provider);
            manager.LevelCurve = CreateRewardTrack(entries);
            manager.EnsureInitialized();
            Assert.That(manager.LevelCurve.IsAscending(), Is.True);
            Stopwatch stopwatch = Stopwatch.StartNew();

            manager.SetLevel(int.MaxValue);
            manager.ActivatePremium();

            stopwatch.Stop();
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(entryCount + entryCount / 1000),
                "one point per entry on level-up plus one premium point per 1000 entries on replay");
            // WHY: rescanning the track per step costs ~5e9 entry visits here (seconds); one pass is milliseconds.
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
        }

        [Test]
        public void TryGetNextDefinition_ReturnsSmallestLevelInRangeAndFirstDuplicate()
        {
            ProgressionLevelDefinition first = CreateLevel(3, 1);
            ProgressionLevelDefinition duplicate = CreateLevel(3, 2);
            LevelCurveDefinition track = CreateRewardTrack(CreateLevel(7, 0), first, null, duplicate, CreateLevel(5, 0));

            Assert.That(track.TryGetNextDefinition(0, 10, out ProgressionLevelDefinition next), Is.True);
            Assert.That(next, Is.SameAs(first));
            Assert.That(track.TryGetNextDefinition(3, 10, out next), Is.True);
            Assert.That(next.Level, Is.EqualTo(5));
            Assert.That(track.TryGetNextDefinition(5, 6, out next), Is.False);
            Assert.That(next, Is.Null);
            Assert.That(track.TryGetDefinition(3, out ProgressionLevelDefinition direct), Is.True);
            Assert.That(direct, Is.SameAs(first));
        }

        [Test]
        public void DispatchRewards_IndexedAndEnumerableOverloads_AllocateNothingWhenWarm()
        {
            ProgressionManager manager = CreateManager();
            manager.EnsureInitialized();
            List<ProgressionReward> rewards = new()
            {
                CreateReward(ProgressionRewardType.None, 1),
                null,
                CreateReward(ProgressionRewardType.PerkPoints, 1, true)
            };
            IEnumerable<ProgressionReward> enumerable = rewards;
            ProgressionRewardDispatcher.DispatchRewards(rewards, manager);
            ProgressionRewardDispatcher.DispatchRewards(enumerable, manager);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                ProgressionRewardDispatcher.DispatchRewards(rewards, manager);
                ProgressionRewardDispatcher.DispatchRewards(enumerable, manager);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
            Assert.That(manager.AvailablePerkPoints, Is.Zero, "premium rewards stay filtered without premium");
        }

        [Test]
        public void NoCodeActivatePremium_SucceedsOnceAndConditionAdapterTracksEntitlement()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider(true));
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 0, CreateReward(ProgressionRewardType.PerkPoints, 4, true)));
            manager.SetLevel(2);
            ProgressionConditionAdapter adapter = CreateObject().AddComponent<ProgressionConditionAdapter>();
            adapter.Manager = manager;
            adapter.Mode = ProgressionConditionEvaluationMode.HasPremium;
            ProgressionNoCodeAction action = CreateObject().AddComponent<ProgressionNoCodeAction>();
            action.Manager = manager;
            action.ActionType = ProgressionNoCodeActionType.ActivatePremium;
            int successes = 0;
            List<string> failures = new();
            int premiumEvents = 0;
            int typedPremiumEvents = 0;
            action.OnSuccess.AddListener(() => successes++);
            action.OnFailed.AddListener(failures.Add);
            manager.OnPremiumActivated.AddListener(() => premiumEvents++);
            manager.PremiumActivated += () => typedPremiumEvents++;
            Assert.That(adapter.EvaluateCurrent(), Is.False);

            action.Execute();
            action.Execute();

            Assert.That(successes, Is.EqualTo(1));
            Assert.That(failures, Is.EqualTo(new[] { "Premium track is already active." }));
            Assert.That(premiumEvents, Is.EqualTo(1));
            Assert.That(typedPremiumEvents, Is.EqualTo(1));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(4));
            Assert.That(adapter.EvaluateCurrent(), Is.True);
            adapter.Invert = true;
            Assert.That(adapter.EvaluateCurrent(), Is.False);
        }

        [Test]
        public void ProfileEventAccessors_ExposeSerializedSaveAndResetEvents()
        {
            ProgressionManager manager = CreateManager();
            int saved = 0;
            int reset = 0;
            manager.OnProfileSaved.AddListener(() => saved++);
            manager.OnProfileReset.AddListener(() => reset++);

            manager.SaveProfile();
            manager.ResetProgression();

            Assert.That(saved, Is.EqualTo(1));
            Assert.That(reset, Is.EqualTo(1));
        }

        private GameObject CreateObject()
        {
            GameObject instance = new("ProgressionLibraryPass");
            instance.SetActive(false);
            _ownedObjects.Add(instance);
            return instance;
        }

        private ProgressionManager CreateManager(LevelComponent provider = null)
        {
            ProgressionManager manager = CreateObject().AddComponent<ProgressionManager>();
            SetField(manager, "_loadOnAwake", false);
            manager.AutoSave = false;
            manager.SaveKey = "ProgressionLibraryPass." + Guid.NewGuid().ToString("N");
            manager.LevelProvider = provider;
            return manager;
        }

        private LevelComponent CreateLevelProvider(bool useXp)
        {
            LevelComponent provider = CreateObject().AddComponent<LevelComponent>();
            SetField(provider, "_loadOnAwake", false);
            SetField(provider, "_autoSave", false);
            SetField(provider, "_useXp", useXp);
            if (useXp)
            {
                Core.Level.LevelCurveDefinition curve = ScriptableObject.CreateInstance<Core.Level.LevelCurveDefinition>();
                _ownedObjects.Add(curve);
                curve.SetLinear(100);
                provider.LevelCurveDefinition = curve;
            }

            return provider;
        }

        private Audit20261023NeoQuestConditionProbe CreateProbe()
        {
            return CreateObject().AddComponent<Audit20261023NeoQuestConditionProbe>();
        }

        private LevelCurveDefinition CreateRewardTrack(params ProgressionLevelDefinition[] levels)
        {
            LevelCurveDefinition track = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            _ownedObjects.Add(track);
            SetField(track, "_levels", new List<ProgressionLevelDefinition>(levels));
            return track;
        }

        private PerkTreeDefinition CreatePerkTree(params PerkDefinition[] perks)
        {
            PerkTreeDefinition tree = ScriptableObject.CreateInstance<PerkTreeDefinition>();
            _ownedObjects.Add(tree);
            SetField(tree, "_perks", new List<PerkDefinition>(perks));
            return tree;
        }

        private UnlockTreeDefinition CreateUnlockTree(params UnlockNodeDefinition[] nodes)
        {
            UnlockTreeDefinition tree = ScriptableObject.CreateInstance<UnlockTreeDefinition>();
            _ownedObjects.Add(tree);
            SetField(tree, "_nodes", new List<UnlockNodeDefinition>(nodes));
            return tree;
        }

        private static PerkDefinition CreatePerk(string id, int cost, ConditionEntry condition,
            params ProgressionReward[] rewards)
        {
            PerkDefinition perk = new();
            SetField(perk, "_id", id);
            SetField(perk, "_cost", cost);
            SetField(perk, "_conditions",
                condition != null ? new List<ConditionEntry> { condition } : new List<ConditionEntry>());
            SetField(perk, "_rewards", new List<ProgressionReward>(rewards));
            return perk;
        }

        private static UnlockNodeDefinition CreateNode(string id, ConditionEntry condition,
            params ProgressionReward[] rewards)
        {
            UnlockNodeDefinition node = new();
            SetField(node, "_id", id);
            SetField(node, "_conditions",
                condition != null ? new List<ConditionEntry> { condition } : new List<ConditionEntry>());
            SetField(node, "_rewards", new List<ProgressionReward>(rewards));
            return node;
        }

        private static ProgressionLevelDefinition CreateLevel(int level, int points, params ProgressionReward[] rewards)
        {
            ProgressionLevelDefinition definition = new();
            SetField(definition, "_level", level);
            SetField(definition, "_grantedPerkPoints", points);
            SetField(definition, "_rewards", new List<ProgressionReward>(rewards));
            return definition;
        }

        private static ProgressionReward CreateReward(ProgressionRewardType type, int amount, bool premium = false)
        {
            ProgressionReward reward = new();
            SetField(reward, "_rewardType", type);
            SetField(reward, "_amount", amount);
            SetField(reward, "_isPremium", premium);
            return reward;
        }

        private static ConditionEntry ProbeCondition(Audit20261023NeoQuestConditionProbe probe)
        {
            return new ConditionEntry
            {
                Source = SourceMode.Component,
                SourceObject = probe.gameObject,
                ComponentTypeName = typeof(Audit20261023NeoQuestConditionProbe).FullName,
                PropertyName = nameof(Audit20261023NeoQuestConditionProbe.Value),
                CurrentValueType = Neo.Condition.ValueType.Bool,
                Compare = CompareOp.Equal,
                ThresholdBool = true
            };
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Serialized field {name} is required by this fixture.");
            field.SetValue(target, value);
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _values.TryGetValue(key, out object value) && value is int result ? result : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                SetValue(key, value);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _values.TryGetValue(key, out object value) && value is float result ? result : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                SetValue(key, value);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string result ? result : defaultValue;
            }

            public void SetString(string key, string value)
            {
                SetValue(key, value);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _values.TryGetValue(key, out object value) && value is bool result ? result : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                SetValue(key, value);
            }

            public bool HasKey(string key)
            {
                return _values.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll()
            {
                _values.Clear();
            }

            public void Save()
            {
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }

            private void SetValue(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
