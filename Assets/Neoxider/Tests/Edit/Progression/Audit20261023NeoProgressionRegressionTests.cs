using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Level;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Progression.Tests
{
    public sealed class Audit20261023NeoProgressionRegressionTests
    {
        private static readonly FieldInfo SingletonInstanceField = typeof(Singleton<ProgressionManager>)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo SingletonSearchField = typeof(Singleton<ProgressionManager>)
            .GetField("_searchFailed", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<Object> _ownedObjects = new();
        private ISaveProvider _previousProvider;
        private MemorySaveProvider _provider;
        private object _previousSingleton;
        private bool _previousSearchFailed;
        private bool _previousCreateInstance;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new MemorySaveProvider();
            SaveProvider.SetProvider(_provider);
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

        [TestCase(false)]
        [TestCase(true)]
        public void PerkPointGrants_AtIntegerBoundary_PreserveBalanceAndPublishedState(bool levelReward)
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 10));
            manager.AddPerkPoints(int.MaxValue - 1);
            int publishedPoints = -1;
            manager.OnPerkPointsChanged.AddListener(value => publishedPoints = value);

            if (levelReward)
            {
                manager.AddXp(100);
            }
            else
            {
                manager.AddPerkPoints(10);
            }

            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(int.MaxValue));
            Assert.That(manager.PerkPointsStateValue, Is.EqualTo(int.MaxValue));
            Assert.That(publishedPoints, Is.EqualTo(int.MaxValue));
            Assert.That(manager.GetProfileSnapshot().AvailablePerkPoints, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void ActivatePremium_XpRewardReachesNewLevel_GrantsNewRewardsOnce()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(
                CreateLevel(1, 0),
                CreateLevel(2, 0, CreateReward(ProgressionRewardType.Xp, 100, true)),
                CreateLevel(3, 3, CreateReward(ProgressionRewardType.PerkPoints, 7, true),
                    CreateReward(ProgressionRewardType.PerkPoints, 2)));
            manager.SetLevel(2);

            manager.ActivatePremium();

            Assert.That(manager.HasPremium, Is.True);
            Assert.That(manager.TotalXp, Is.EqualTo(200));
            Assert.That(manager.CurrentLevel, Is.EqualTo(3));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(12),
                "Level 3 grants 3 base points, 7 premium points, and 2 free points exactly once.");
            manager.ActivatePremium();
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(12));
            Assert.That(manager.TotalXp, Is.EqualTo(200));
        }

        [Test]
        public void ActivatePremium_SaveCallbackReachesNewLevel_DoesNotReplayNewLevel()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 0,
                CreateReward(ProgressionRewardType.PerkPoints, 7, true)), CreateLevel(3, 0,
                CreateReward(ProgressionRewardType.PerkPoints, 5, true)));
            manager.SetLevel(2);
            bool advanced = false;
            _provider.OnDataSaved += () =>
            {
                if (!advanced)
                {
                    advanced = true;
                    manager.SetLevel(3);
                }
            };
            manager.AutoSave = true;

            manager.ActivatePremium();

            Assert.That(advanced, Is.True);
            Assert.That(manager.CurrentLevel, Is.EqualTo(3));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(12),
                "The newly reached level grants 5 points normally; only the earlier level replays 7 points.");
        }

        [Test]
        public void ReassignSameProvider_AfterLevelDecrease_DoesNotReplayClaimedLevels()
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.SetLevel(3);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
            manager.SetLevel(1);

            manager.LevelProvider = level;
            manager.SetLevel(3);

            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
        }

        [Test]
        public void ReplaceProvider_RefreshesSnapshotsAndDetachesPreviousProvider()
        {
            LevelComponent first = CreateLevelProvider();
            ProgressionManager manager = CreateManager(first);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(3, 3), CreateLevel(4, 4));
            manager.AddXp(50);
            LevelComponent second = CreateLevelProvider();
            second.AddXp(100);

            manager.LevelProvider = second;

            Assert.That(manager.XpStateValue, Is.EqualTo(100));
            Assert.That(manager.LevelStateValue, Is.EqualTo(2));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(100));
            first.AddXp(500);
            Assert.That(manager.AvailablePerkPoints, Is.Zero, "The previous provider must no longer award rewards.");
            second.AddXp(100);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(3));

            manager.LevelProvider = null;
            second.AddXp(100);
            Assert.That(manager.XpStateValue, Is.Zero);
            Assert.That(manager.LevelStateValue, Is.EqualTo(1));
            Assert.That(manager.XpToNextLevelStateValue, Is.Zero);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(3));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ProfileSanitize_RepairsNullListAndKeepsValidIdentifiers(bool nullUnlocks)
        {
            ProgressionProfileData profile = new() { AvailablePerkPoints = 4, HasPremium = true };
            profile.UnlockedNodeIds.AddRange(new[] { "", "node", " " });
            profile.PurchasedPerkIds.AddRange(new[] { null, "perk" });
            SetField(profile, nullUnlocks ? "_unlockedNodeIds" : "_purchasedPerkIds", null);

            profile.Sanitize();

            Assert.That(profile.UnlockedNodeIds, Is.EqualTo(nullUnlocks ? Array.Empty<string>() : new[] { "node" }));
            Assert.That(profile.PurchasedPerkIds, Is.EqualTo(nullUnlocks ? new[] { "perk" } : Array.Empty<string>()));
            Assert.That(profile.AvailablePerkPoints, Is.EqualTo(4));
            Assert.That(profile.HasPremium, Is.True);
        }

        [Test]
        public void ClearDestroyedProvider_RefreshesCachedState()
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.AddXp(50);
            Object.DestroyImmediate(level.gameObject);

            manager.LevelProvider = null;

            Assert.That(manager.XpStateValue, Is.Zero);
            Assert.That(manager.LevelStateValue, Is.EqualTo(1));
            Assert.That(manager.XpToNextLevelStateValue, Is.Zero);
        }

        [Test]
        public void ProfileClone_WithNullList_CreatesIndependentUsableSnapshot()
        {
            ProgressionProfileData profile = new() { AvailablePerkPoints = 8, HasPremium = true };
            profile.PurchasedPerkIds.Add("perk");
            SetField(profile, "_unlockedNodeIds", null);

            ProgressionProfileData clone = profile.Clone();
            clone.UnlockedNodeIds.Add("node");
            clone.PurchasedPerkIds.Add("other");

            Assert.That(profile.PurchasedPerkIds, Is.EqualTo(new[] { "perk" }));
            Assert.That(clone.PurchasedPerkIds, Is.EqualTo(new[] { "perk", "other" }));
            Assert.That(clone.UnlockedNodeIds, Is.EqualTo(new[] { "node" }));
            Assert.That(clone.AvailablePerkPoints, Is.EqualTo(8));
            Assert.That(clone.HasPremium, Is.True);
        }

        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void RewardTrackNextLevel_WithNegativeXp_UsesZeroXp(int totalXp)
        {
            LevelCurveDefinition track = CreateRewardTrack(CreateLevel(1, 0), CreateLevel(2, 0));

            Assert.That(track.GetXpToNextLevel(totalXp), Is.EqualTo(100));
            Assert.That(track.EvaluateLevel(totalXp), Is.EqualTo(1));
        }

        [Test]
        public void Condition_WithExplicitUninitializedManager_ReadsPersistedPerkPoints()
        {
            ProgressionManager manager = CreateManager();
            SetField(manager, "_loadOnAwake", true);
            _provider.SetString(manager.SaveKey,
                JsonUtility.ToJson(new ProgressionProfileData { AvailablePerkPoints = 7 }));
            ProgressionConditionAdapter adapter = CreateObject().AddComponent<ProgressionConditionAdapter>();
            SetField(adapter, "_manager", manager);
            SetField(adapter, "_mode", ProgressionConditionEvaluationMode.PerkPointsAtLeast);
            SetField(adapter, "_threshold", 7);

            bool result = adapter.EvaluateCurrent();

            Assert.That(result, Is.True);
            Assert.That(adapter.LastResult, Is.True);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Condition_WithoutManager_LastResultMatchesReturnedResult(bool invert)
        {
            ProgressionConditionAdapter adapter = CreateObject().AddComponent<ProgressionConditionAdapter>();
            SetField(adapter, "_invert", invert);

            bool result = adapter.Evaluate(null);

            Assert.That(result, Is.EqualTo(invert));
            Assert.That(adapter.LastResult, Is.EqualTo(result));
        }

        [Test]
        public void DispatchRewards_FirstCall_LoadsPremiumEntitlementBeforeFiltering()
        {
            ProgressionManager manager = CreateManager();
            SetField(manager, "_loadOnAwake", true);
            _provider.SetString(manager.SaveKey,
                JsonUtility.ToJson(new ProgressionProfileData { HasPremium = true }));

            ProgressionRewardDispatcher.DispatchRewards(
                new[] { CreateReward(ProgressionRewardType.PerkPoints, 5, true) }, manager);

            Assert.That(manager.HasPremium, Is.True);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
        }

        [TestCase(0, "XP amount must be greater than zero.")]
        [TestCase(25, "Level provider is not assigned.")]
        public void NoCodeAddXp_WhenRejected_EmitsFailureWithoutSuccessOrSave(int amount, string expectedReason)
        {
            ProgressionManager manager = CreateManager();
            manager.AutoSave = true;
            ProgressionNoCodeAction action = CreateObject().AddComponent<ProgressionNoCodeAction>();
            action.Manager = manager;
            action.XpAmount = amount;
            int successes = 0;
            List<string> failures = new();
            List<string> messages = new();
            action.OnSuccess.AddListener(() => successes++);
            action.OnFailed.AddListener(failures.Add);
            action.OnResultMessage.AddListener(messages.Add);

            action.Execute();

            Assert.That(successes, Is.Zero);
            Assert.That(failures, Is.EqualTo(new[] { expectedReason }));
            Assert.That(messages, Is.EqualTo(failures));
            Assert.That(manager.TotalXp, Is.Zero);
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void NoCodeAddXp_WithProvider_ChangesXpAndEmitsOneSuccess()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.AutoSave = true;
            ProgressionNoCodeAction action = CreateObject().AddComponent<ProgressionNoCodeAction>();
            action.Manager = manager;
            action.XpAmount = 50;
            int successes = 0;
            List<string> failures = new();
            action.OnSuccess.AddListener(() => successes++);
            action.OnFailed.AddListener(failures.Add);

            action.Execute();

            Assert.That(manager.TotalXp, Is.EqualTo(50));
            Assert.That(manager.XpStateValue, Is.EqualTo(50));
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(failures, Is.Empty);
            Assert.That(_provider.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void NoCodeAction_WithUnknownType_ReportsFailureAndKeepsProfile()
        {
            ProgressionManager manager = CreateManager();
            manager.AddPerkPoints(4);
            manager.AutoSave = true;
            ProgressionNoCodeAction action = CreateObject().AddComponent<ProgressionNoCodeAction>();
            action.Manager = manager;
            action.ActionType = (ProgressionNoCodeActionType)int.MaxValue;
            int successes = 0;
            List<string> failures = new();
            action.OnSuccess.AddListener(() => successes++);
            action.OnFailed.AddListener(failures.Add);

            action.Execute();

            Assert.That(successes, Is.Zero);
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0], Does.Contain("Unsupported progression action"));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(4));
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void TryAddXp_WithNonPositiveAmount_ReturnsReasonAndLeavesState(int amount)
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.AddXp(20);
            manager.AutoSave = true;

            bool success = manager.TryAddXp(amount, out string reason);

            Assert.That(success, Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.That(manager.TotalXp, Is.EqualTo(20));
            Assert.That(manager.XpStateValue, Is.EqualTo(20));
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void ReloadSameProfile_AfterDowngrade_PreservesHighestRewardedLevel()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.SetLevel(3);
            manager.SaveProfile();

            for (int reload = 0; reload < 3; reload++)
            {
                manager.SetLevel(1);
                manager.LoadProfile();
                manager.SetLevel(3);
                Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
                Assert.That(manager.PerkPointsStateValue, Is.EqualTo(5));
            }

            Assert.That(manager.GetProfileSnapshot().Version, Is.EqualTo(2));
        }

        [Test]
        public void ResetProgression_AfterSameProfileReload_AllowsFreshLevelRewards()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.SetLevel(3);
            manager.SaveProfile();
            manager.SetLevel(1);
            manager.LoadProfile();

            manager.ResetProgression();

            Assert.That(manager.AvailablePerkPoints, Is.Zero);
            Assert.That(manager.CurrentLevel, Is.EqualTo(1));
            manager.SetLevel(3);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
        }

        [Test]
        public void LoadDifferentProfile_AfterDowngrade_UsesIndependentRewardBaseline()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.SetLevel(3);
            manager.SaveProfile();
            string firstKey = manager.SaveKey;
            manager.SetLevel(1);
            string secondKey = firstKey + ".other";
            _provider.SetString(secondKey,
                JsonUtility.ToJson(new ProgressionProfileData { AvailablePerkPoints = 10 }));

            manager.SaveKey = secondKey;
            manager.LoadProfile();
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(10));
            manager.SetLevel(3);

            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(15));
            ProgressionProfileData firstProfile =
                JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(firstKey));
            Assert.That(firstProfile.AvailablePerkPoints, Is.EqualTo(5));
        }

        [Test]
        public void AssignSaveKeyWithoutLoading_DoesNotDiscardActiveProfileRewardHistory()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.SetLevel(3);
            manager.SaveProfile();
            string activeKey = manager.SaveKey;

            manager.SaveKey = activeKey + ".not-loaded";
            manager.SetLevel(1);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
            manager.SaveKey = activeKey;
            manager.LoadProfile();
            manager.SetLevel(3);

            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
        }

        [Test]
        public void ReloadSameProfile_AfterCoreRestore_BaselinesIncomingHigherLevel()
        {
            LevelComponent level = CreateLevelProvider();
            string coreKey = "Audit20261023NeoProgression.Core." + Guid.NewGuid().ToString("N");
            SetField(level, "_saveKey", coreKey);
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.AddPerkPoints(4);
            manager.SaveProfile();
            _provider.SetString(coreKey, JsonUtility.ToJson(new LevelProfileData { TotalXp = 200, CurrentLevel = 3 }));

            level.Load();
            manager.LoadProfile();
            manager.SetLevel(1);
            manager.SetLevel(3);

            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(4));
            Assert.That(manager.LevelStateValue, Is.EqualTo(3));
        }

        [Test]
        public void ProviderXpChange_UpdatesMirrorsOnceWithoutSavingOrGrantingRewards()
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2));
            manager.AutoSave = true;
            manager.EnsureInitialized();
            List<int> xpNotifications = new();
            List<int> remainingAtNotification = new();
            manager.OnXpChanged.AddListener(value =>
            {
                xpNotifications.Add(value);
                remainingAtNotification.Add(manager.XpToNextLevelStateValue);
            });

            level.AddXp(25);
            level.XpState.ForceNotify();
            level.LevelState.ForceNotify();
            level.XpToNextLevelState.ForceNotify();

            Assert.That(manager.TotalXp, Is.EqualTo(25));
            Assert.That(manager.XpStateValue, Is.EqualTo(25));
            Assert.That(manager.LevelStateValue, Is.EqualTo(1));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(75));
            Assert.That(xpNotifications, Is.EqualTo(new[] { 25 }));
            Assert.That(remainingAtNotification, Is.EqualTo(new[] { 75 }));
            Assert.That(manager.AvailablePerkPoints, Is.Zero);
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void ManagerXpChangeAcrossLevel_PublishesOneSettledSnapshot()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2));
            manager.AutoSave = true;
            manager.EnsureInitialized();
            List<int> xpNotifications = new();
            List<int> pointsAtNotification = new();
            List<int> levelNotifications = new();
            manager.OnXpChanged.AddListener(value =>
            {
                xpNotifications.Add(value);
                pointsAtNotification.Add(manager.PerkPointsStateValue);
            });
            manager.OnLevelChanged.AddListener(levelNotifications.Add);

            manager.AddXp(100);

            Assert.That(xpNotifications, Is.EqualTo(new[] { 100 }));
            Assert.That(levelNotifications, Is.EqualTo(new[] { 2 }));
            Assert.That(pointsAtNotification, Is.EqualTo(new[] { 2 }));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(2));
            Assert.That(_provider.SaveCount, Is.EqualTo(2),
                "Keep the existing reward-save and command-save paths; reactive mirrors must add no saves.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProviderDecreaseOrReset_UpdatesMirrorsWithoutReplayingRewards(bool reset)
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.SetLevel(3);
            manager.AutoSave = true;
            List<int> xpNotifications = new();
            manager.OnXpChanged.AddListener(xpNotifications.Add);

            if (reset)
            {
                level.ResetProgress();
            }
            else
            {
                level.SetLevel(1);
            }

            Assert.That(manager.XpStateValue, Is.Zero);
            Assert.That(manager.LevelStateValue, Is.EqualTo(1));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(100));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(5));
            Assert.That(xpNotifications, Is.EqualTo(new[] { 0 }));
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void ProviderLoad_UpdatesAllMirrorsWithoutClaimingRestoredLevels()
        {
            LevelComponent level = CreateLevelProvider();
            string coreKey = "Audit20261023NeoProgression.Load." + Guid.NewGuid().ToString("N");
            SetField(level, "_saveKey", coreKey);
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            manager.AutoSave = true;
            manager.EnsureInitialized();
            _provider.SetString(coreKey, JsonUtility.ToJson(new LevelProfileData { TotalXp = 200, CurrentLevel = 3 }));

            level.Load();

            Assert.That(manager.XpStateValue, Is.EqualTo(200));
            Assert.That(manager.LevelStateValue, Is.EqualTo(3));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(100));
            Assert.That(manager.AvailablePerkPoints, Is.Zero);
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void ProviderSubscriptions_AreReleasedOnReplacementClearAndRebind()
        {
            LevelComponent first = CreateLevelProvider();
            first.EnsureInitialized();
            int firstXpListeners = first.XpState.ListenerCount;
            int firstLevelListeners = first.LevelState.ListenerCount;
            int firstRemainingListeners = first.XpToNextLevelState.ListenerCount;
            ProgressionManager manager = CreateManager(first);
            manager.EnsureInitialized();
            Assert.That(first.XpState.ListenerCount, Is.EqualTo(firstXpListeners + 1));
            Assert.That(first.LevelState.ListenerCount, Is.EqualTo(firstLevelListeners + 1));
            Assert.That(first.XpToNextLevelState.ListenerCount, Is.EqualTo(firstRemainingListeners + 1));
            manager.LevelProvider = first;
            Assert.That(first.XpState.ListenerCount, Is.EqualTo(firstXpListeners + 1));
            Assert.That(first.LevelState.ListenerCount, Is.EqualTo(firstLevelListeners + 1));
            Assert.That(first.XpToNextLevelState.ListenerCount, Is.EqualTo(firstRemainingListeners + 1));
            LevelComponent second = CreateLevelProvider();
            second.EnsureInitialized();
            int secondXpListeners = second.XpState.ListenerCount;
            int secondLevelListeners = second.LevelState.ListenerCount;
            int secondRemainingListeners = second.XpToNextLevelState.ListenerCount;

            manager.LevelProvider = second;
            Assert.That(first.XpState.ListenerCount, Is.EqualTo(firstXpListeners));
            Assert.That(first.LevelState.ListenerCount, Is.EqualTo(firstLevelListeners));
            Assert.That(first.XpToNextLevelState.ListenerCount, Is.EqualTo(firstRemainingListeners));
            Assert.That(second.XpState.ListenerCount, Is.EqualTo(secondXpListeners + 1));
            Assert.That(second.LevelState.ListenerCount, Is.EqualTo(secondLevelListeners + 1));
            Assert.That(second.XpToNextLevelState.ListenerCount, Is.EqualTo(secondRemainingListeners + 1));
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);
            first.AddXp(25);
            Assert.That(notifications, Is.Empty);
            second.AddXp(50);
            Assert.That(manager.XpStateValue, Is.EqualTo(50));
            Assert.That(notifications, Is.EqualTo(new[] { 50 }));

            // Pure public binding contract; native Awake/destruction requires separate PlayMode evidence.
            manager.LevelProvider = null;
            Assert.That(manager.LevelProvider, Is.Null);
            Assert.That(second.XpState.ListenerCount, Is.EqualTo(secondXpListeners));
            Assert.That(second.LevelState.ListenerCount, Is.EqualTo(secondLevelListeners));
            Assert.That(second.XpToNextLevelState.ListenerCount, Is.EqualTo(secondRemainingListeners));
            Assert.That(manager.XpStateValue, Is.Zero);
            Assert.That(manager.LevelStateValue, Is.EqualTo(1));
            Assert.That(manager.XpToNextLevelStateValue, Is.Zero);
            Assert.That(notifications, Is.EqualTo(new[] { 50, 0 }));
            second.AddXp(10);
            Assert.That(notifications, Is.EqualTo(new[] { 50, 0 }));

            manager.LevelProvider = second;
            manager.LevelProvider = second;
            manager.EnsureInitialized();
            Assert.That(first.XpState.ListenerCount, Is.EqualTo(firstXpListeners));
            Assert.That(first.LevelState.ListenerCount, Is.EqualTo(firstLevelListeners));
            Assert.That(first.XpToNextLevelState.ListenerCount, Is.EqualTo(firstRemainingListeners));
            Assert.That(second.XpState.ListenerCount, Is.EqualTo(secondXpListeners + 1));
            Assert.That(second.LevelState.ListenerCount, Is.EqualTo(secondLevelListeners + 1));
            Assert.That(second.XpToNextLevelState.ListenerCount, Is.EqualTo(secondRemainingListeners + 1));
            Assert.That(manager.XpStateValue, Is.EqualTo(60));
            Assert.That(notifications, Is.EqualTo(new[] { 50, 0, 60 }));
            second.AddXp(10);
            Assert.That(manager.XpStateValue, Is.EqualTo(70));
            Assert.That(notifications, Is.EqualTo(new[] { 50, 0, 60, 70 }));
            Assert.That(_provider.SaveCount, Is.Zero);
            manager.LevelProvider = null;
            Assert.That(second.XpState.ListenerCount, Is.EqualTo(secondXpListeners));
            Assert.That(second.LevelState.ListenerCount, Is.EqualTo(secondLevelListeners));
            Assert.That(second.XpToNextLevelState.ListenerCount, Is.EqualTo(secondRemainingListeners));
            Assert.That(notifications, Is.EqualTo(new[] { 50, 0, 60, 70, 0 }));
        }

        [Test]
        public void ProviderSwapInsideReactiveCallback_IgnoresOldCapturedSubscription()
        {
            LevelComponent first = CreateLevelProvider();
            first.EnsureInitialized();
            LevelComponent second = CreateLevelProvider();
            second.AddXp(60);
            ProgressionManager manager = null;
            bool redirected = false;
            using IDisposable redirect = first.XpState.Subscribe(value =>
            {
                if (!redirected)
                {
                    redirected = true;
                    manager.LevelProvider = second;
                }
            });
            manager = CreateManager(first);
            manager.EnsureInitialized();
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);

            first.AddXp(25);

            Assert.That(manager.LevelProvider, Is.SameAs(second));
            Assert.That(manager.XpStateValue, Is.EqualTo(60));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(40));
            Assert.That(notifications, Is.EqualTo(new[] { 60 }));
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void ProviderSwapInsideLevelEvent_DoesNotRewardDetachedProvider()
        {
            LevelComponent first = CreateLevelProvider();
            LevelComponent second = CreateLevelProvider();
            ProgressionManager manager = CreateManager(first);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2));
            first.OnLevelUp.AddListener(level => manager.LevelProvider = second);
            manager.EnsureInitialized();

            first.SetLevel(2);

            Assert.That(manager.LevelProvider, Is.SameAs(second));
            Assert.That(manager.XpStateValue, Is.Zero);
            Assert.That(manager.LevelStateValue, Is.EqualTo(1));
            Assert.That(manager.AvailablePerkPoints, Is.Zero);
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [Test]
        public void MirrorNotificationReentry_PublishesLatestStateWithoutTrailingOldPayload()
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.EnsureInitialized();
            using IDisposable subscription = manager.XpState.Subscribe(value =>
            {
                if (value == 25)
                {
                    manager.AddXp(10);
                }
            });
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);

            level.AddXp(25);

            Assert.That(manager.TotalXp, Is.EqualTo(35));
            Assert.That(manager.XpStateValue, Is.EqualTo(35));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(65));
            Assert.That(notifications, Is.EqualTo(new[] { 35 }));
        }

        [Test]
        public void NotificationFailure_DoesNotLeaveSubsequentProviderChangesBatched()
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.EnsureInitialized();
            UnityEngine.Events.UnityAction<int> fail = value => throw new InvalidOperationException("observer");
            manager.OnXpChanged.AddListener(fail);

            Assert.Throws<InvalidOperationException>(() => manager.AddXp(25));
            manager.OnXpChanged.RemoveListener(fail);
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);
            level.AddXp(10);

            Assert.That(manager.XpStateValue, Is.EqualTo(35));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(65));
            Assert.That(notifications, Is.EqualTo(new[] { 35 }));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void InitializationReadFailure_CorrectedRetryLoadsSavedPointsBeforeAutoSave(
            bool assignLevelProvider, bool replaceSaveProvider)
        {
            LevelComponent level = assignLevelProvider ? CreateLevelProvider() : null;
            ProgressionManager manager = CreateManager(level);
            string savedJson = EnableSavedProfile(manager);
            _provider.FailNextStringReadKey = manager.SaveKey;
            int loadedEvents = 0;
            manager.OnProfileLoaded.AddListener(() => loadedEvents++);

            Assert.Throws<InvalidOperationException>(manager.EnsureInitialized);

            Assert.That(_provider.StringReadCount, Is.EqualTo(1));
            Assert.That(_provider.SaveCount, Is.Zero);
            Assert.That(manager.AvailablePerkPoints, Is.Zero);
            Assert.That(loadedEvents, Is.Zero);
            if (assignLevelProvider)
            {
                AssertMirrorListenerCount(level, 0);
            }

            MemorySaveProvider correctedProvider = _provider;
            if (replaceSaveProvider)
            {
                correctedProvider = new MemorySaveProvider();
                correctedProvider.SetString(manager.SaveKey, savedJson);
                SaveProvider.SetProvider(correctedProvider);
            }

            // The one-shot read failure has ended; retry the public boundary explicitly.
            manager.EnsureInitialized();
            manager.EnsureInitialized();

            Assert.That(correctedProvider.StringReadCount, Is.EqualTo(replaceSaveProvider ? 1 : 2));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            Assert.That(manager.PerkPointsStateValue, Is.EqualTo(7));
            Assert.That(loadedEvents, Is.EqualTo(1));
            Assert.That(correctedProvider.SaveCount, Is.Zero);
            if (assignLevelProvider)
            {
                AssertMirrorListenerCount(level, 1);
            }

            manager.AddPerkPoints(1);

            ProgressionProfileData stored = JsonUtility.FromJson<ProgressionProfileData>(
                correctedProvider.GetString(manager.SaveKey));
            Assert.That(stored.AvailablePerkPoints, Is.EqualTo(8));
            Assert.That(stored.Version, Is.EqualTo(2));
            Assert.That(manager.PerkPointsStateValue, Is.EqualTo(8));
            Assert.That(correctedProvider.SaveCount, Is.EqualTo(1));
            if (replaceSaveProvider)
            {
                Assert.That(_provider.SaveCount, Is.Zero);
                Assert.That(_provider.GetString(manager.SaveKey), Is.EqualTo(savedJson));
            }
        }

        [Test]
        public void InitializationProviderReadFailure_CorrectedRetryBindsAndLoadsBothProfiles()
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            EnableSavedProfile(manager);
            string coreKey = manager.SaveKey + ".Core";
            SetField(level, "_saveKey", coreKey);
            SetField(level, "_loadOnAwake", true);
            _provider.SetString(coreKey, JsonUtility.ToJson(new Core.Level.LevelProfileData
            {
                TotalXp = 25,
                CurrentLevel = 1
            }));
            _provider.FailNextStringReadKey = coreKey;

            Assert.Throws<InvalidOperationException>(manager.EnsureInitialized);

            AssertMirrorListenerCount(level, 0);
            Assert.That(_provider.StringReadCount, Is.EqualTo(1));
            Assert.That(_provider.SaveCount, Is.Zero);
            manager.EnsureInitialized();
            manager.EnsureInitialized();

            AssertMirrorListenerCount(level, 1);
            Assert.That(_provider.StringReadCount, Is.EqualTo(3));
            Assert.That(manager.XpStateValue, Is.EqualTo(25));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(75));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            manager.AddPerkPoints(1);
            Assert.That(JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(manager.SaveKey))
                .AvailablePerkPoints, Is.EqualTo(8));
            Assert.That(_provider.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void InitializationSubscriptionFailure_ReleasesOnlyOwnedHandlesAndRetriesOnce()
        {
            LevelComponent level = CreateLevelProvider();
            level.EnsureInitialized();
            using IDisposable xpObserver = level.XpState.Subscribe(value => { });
            using IDisposable levelObserver = level.LevelState.Subscribe(value => { });
            using IDisposable remainingObserver = level.XpToNextLevelState.Subscribe(value => { });
            int externalLevelEvents = 0;
            level.OnLevelUp.AddListener(value => externalLevelEvents++);
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            EnableSavedProfile(manager);
            Neo.Reactive.ReactivePropertyInt originalLevelState = level.LevelState;
            level.LevelState = null;
            try
            {
                Assert.Throws<NullReferenceException>(manager.EnsureInitialized);
            }
            finally
            {
                level.LevelState = originalLevelState;
            }

            AssertMirrorListenerCount(level, 1);
            Assert.That(_provider.StringReadCount, Is.Zero, "Binding failed before the Progression read.");
            level.SetLevel(2);
            Assert.That(externalLevelEvents, Is.EqualTo(1));
            Assert.That(manager.AvailablePerkPoints, Is.Zero);
            Assert.That(_provider.SaveCount, Is.Zero);

            manager.EnsureInitialized();
            manager.EnsureInitialized();

            AssertMirrorListenerCount(level, 2);
            Assert.That(_provider.StringReadCount, Is.EqualTo(1));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            level.SetLevel(3);
            Assert.That(externalLevelEvents, Is.EqualTo(2));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(10));
            Assert.That(_provider.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void InitializationReentry_BeforeProfileCommit_RejectsUseWithoutReadingOrSavingDefaults()
        {
            ProgressionManager manager = CreateManager(CreateLevelProvider());
            EnableSavedProfile(manager);
            int readCallbacks = 0;
            _provider.BeforeStringRead = key =>
            {
                if (key != manager.SaveKey)
                {
                    return;
                }

                readCallbacks++;
                Assert.Throws<InvalidOperationException>(manager.EnsureInitialized);
                Assert.Throws<InvalidOperationException>(() => manager.AddPerkPoints(1));
                Assert.Throws<InvalidOperationException>(manager.SaveProfile);
            };

            manager.EnsureInitialized();
            _provider.BeforeStringRead = null;
            manager.EnsureInitialized();

            Assert.That(readCallbacks, Is.EqualTo(1));
            Assert.That(_provider.StringReadCount, Is.EqualTo(1));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            Assert.That(_provider.SaveCount, Is.Zero);
            AssertMirrorListenerCount(manager.LevelProvider, 1);
            manager.AddPerkPoints(1);
            Assert.That(JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(manager.SaveKey))
                .AvailablePerkPoints, Is.EqualTo(8));
        }

        [Test]
        public void InitializationReadFailure_AfterProviderSwap_PreservesNewerBindingUntilRetry()
        {
            LevelComponent first = CreateLevelProvider();
            LevelComponent second = CreateLevelProvider();
            second.AddXp(60);
            ProgressionManager manager = CreateManager(first);
            EnableSavedProfile(manager);
            _provider.FailNextStringReadKey = manager.SaveKey;
            _provider.BeforeStringRead = key =>
            {
                _provider.BeforeStringRead = null;
                manager.LevelProvider = second;
            };
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);

            Assert.Throws<InvalidOperationException>(manager.EnsureInitialized);

            Assert.That(manager.LevelProvider, Is.SameAs(second));
            AssertMirrorListenerCount(first, 0);
            AssertMirrorListenerCount(second, 1);
            second.AddXp(5);
            Assert.That(notifications, Is.Empty, "Uncommitted bindings must not publish profile state.");
            Assert.That(_provider.SaveCount, Is.Zero);

            manager.EnsureInitialized();
            manager.EnsureInitialized();

            Assert.That(_provider.StringReadCount, Is.EqualTo(2));
            AssertMirrorListenerCount(first, 0);
            AssertMirrorListenerCount(second, 1);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            Assert.That(manager.XpStateValue, Is.EqualTo(65));
            notifications.Clear();
            first.AddXp(25);
            second.AddXp(5);
            Assert.That(notifications, Is.EqualTo(new[] { 70 }));
            Assert.That(_provider.SaveCount, Is.Zero);
            manager.AddPerkPoints(1);
            Assert.That(JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(manager.SaveKey))
                .AvailablePerkPoints, Is.EqualTo(8));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InitializationObserverFailure_AfterCommit_DoesNotReloadOrReplayRewards(bool profileLoadedObserver)
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            manager.LevelCurve = CreateRewardTrack(CreateLevel(2, 2), CreateLevel(3, 3));
            EnableSavedProfile(manager);
            bool handled = false;
            Action completeAndFail = () =>
            {
                if (handled)
                {
                    return;
                }

                handled = true;
                manager.EnsureInitialized();
                manager.SetLevel(3);
                manager.SetLevel(1);
                throw new InvalidOperationException("postcommit observer");
            };
            UnityEngine.Events.UnityAction loadedObserver = () => completeAndFail();
            UnityEngine.Events.UnityAction<int> stateObserver = value => completeAndFail();
            if (profileLoadedObserver)
            {
                manager.OnProfileLoaded.AddListener(loadedObserver);
            }
            else
            {
                manager.OnXpChanged.AddListener(stateObserver);
            }

            Assert.Throws<InvalidOperationException>(manager.EnsureInitialized);
            manager.OnProfileLoaded.RemoveListener(loadedObserver);
            manager.OnXpChanged.RemoveListener(stateObserver);

            Assert.That(handled, Is.True);
            Assert.That(manager.CurrentLevel, Is.EqualTo(1));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(12));
            Assert.That(manager.PerkPointsStateValue, Is.EqualTo(12));
            Assert.That(_provider.SaveCount, Is.EqualTo(3));
            manager.EnsureInitialized();
            manager.EnsureInitialized();
            Assert.That(_provider.StringReadCount, Is.EqualTo(1));
            AssertMirrorListenerCount(level, 1);
            manager.SetLevel(3);
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(12), "The committed level-3 watermark survives.");
            Assert.That(_provider.SaveCount, Is.EqualTo(4));
            Assert.That(JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(manager.SaveKey))
                .AvailablePerkPoints, Is.EqualTo(12));
        }

        [Test]
        public void InitializationObserverFailure_AfterProviderSwap_KeepsCommittedProfileAndNewerSubscriptions()
        {
            LevelComponent first = CreateLevelProvider();
            LevelComponent second = CreateLevelProvider();
            second.AddXp(60);
            ProgressionManager manager = CreateManager(first);
            EnableSavedProfile(manager);
            UnityEngine.Events.UnityAction observer = () =>
            {
                manager.LevelProvider = second;
                throw new InvalidOperationException("postcommit provider swap");
            };
            manager.OnProfileLoaded.AddListener(observer);

            Assert.Throws<InvalidOperationException>(manager.EnsureInitialized);
            manager.OnProfileLoaded.RemoveListener(observer);
            manager.EnsureInitialized();

            Assert.That(_provider.StringReadCount, Is.EqualTo(1));
            Assert.That(manager.LevelProvider, Is.SameAs(second));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            AssertMirrorListenerCount(first, 0);
            AssertMirrorListenerCount(second, 1);
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);
            first.AddXp(25);
            second.AddXp(10);
            Assert.That(notifications, Is.EqualTo(new[] { 70 }));
            Assert.That(manager.XpToNextLevelStateValue, Is.EqualTo(30));
            Assert.That(_provider.SaveCount, Is.Zero);
            manager.AddPerkPoints(1);
            Assert.That(JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(manager.SaveKey))
                .AvailablePerkPoints, Is.EqualTo(8));
        }

        [Test]
        public void ProviderBindFailure_AfterReentrantSwap_DoesNotDetachTheNewerBinding()
        {
            LevelComponent first = CreateLevelProvider();
            LevelComponent failing = CreateLevelProvider();
            LevelComponent latest = CreateLevelProvider();
            latest.AddXp(60);
            ProgressionManager manager = CreateManager(first);
            EnableSavedProfile(manager);
            manager.EnsureInitialized();
            SetField(failing, "_saveKey", manager.SaveKey + ".Core");
            SetField(failing, "_loadOnAwake", true);
            UnityEngine.Events.UnityAction observer = () =>
            {
                manager.LevelProvider = latest;
                throw new InvalidOperationException("older provider setup");
            };
            failing.OnProfileLoaded.AddListener(observer);

            Assert.Throws<InvalidOperationException>(() => manager.LevelProvider = failing);
            failing.OnProfileLoaded.RemoveListener(observer);
            manager.EnsureInitialized();

            Assert.That(_provider.StringReadCount, Is.EqualTo(2));
            Assert.That(manager.LevelProvider, Is.SameAs(latest));
            AssertMirrorListenerCount(first, 0);
            AssertMirrorListenerCount(failing, 0);
            AssertMirrorListenerCount(latest, 1);
            Assert.That(manager.XpStateValue, Is.EqualTo(60));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(7));
            List<int> notifications = new();
            manager.OnXpChanged.AddListener(notifications.Add);
            first.AddXp(25);
            latest.AddXp(10);
            Assert.That(notifications, Is.EqualTo(new[] { 70 }));
            Assert.That(_provider.SaveCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Initialization_WithoutFailure_IsIdempotentWithOneBinding(bool loadProfile)
        {
            LevelComponent level = CreateLevelProvider();
            ProgressionManager manager = CreateManager(level);
            FieldInfo loadedEventField = typeof(ProgressionManager).GetField("_onProfileLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(loadedEventField, Is.Not.Null);
            Assert.That(manager.OnProfileLoaded, Is.SameAs(loadedEventField.GetValue(manager)),
                "C# subscribers and Inspector bindings must share the existing event instance.");
            EnableSavedProfile(manager);
            SetField(manager, "_loadOnAwake", loadProfile);
            int loadedEvents = 0;
            manager.OnProfileLoaded.AddListener(() => loadedEvents++);

            manager.EnsureInitialized();
            manager.EnsureInitialized();

            Assert.That(_provider.StringReadCount, Is.EqualTo(loadProfile ? 1 : 0));
            Assert.That(manager.AvailablePerkPoints, Is.EqualTo(loadProfile ? 7 : 0));
            Assert.That(loadedEvents, Is.EqualTo(loadProfile ? 1 : 0));
            AssertMirrorListenerCount(level, 1);
            Assert.That(_provider.SaveCount, Is.Zero);
            manager.AddPerkPoints(1);
            Assert.That(JsonUtility.FromJson<ProgressionProfileData>(_provider.GetString(manager.SaveKey))
                .AvailablePerkPoints, Is.EqualTo(loadProfile ? 8 : 1));
            Assert.That(_provider.SaveCount, Is.EqualTo(1));
        }

        private string EnableSavedProfile(ProgressionManager manager)
        {
            SetField(manager, "_loadOnAwake", true);
            manager.AutoSave = true;
            string json = JsonUtility.ToJson(new ProgressionProfileData { AvailablePerkPoints = 7 });
            _provider.SetString(manager.SaveKey, json);
            return json;
        }

        private static void AssertMirrorListenerCount(LevelComponent provider, int expected)
        {
            Assert.That(provider.XpState.ListenerCount, Is.EqualTo(expected), "XP subscriptions");
            Assert.That(provider.LevelState.ListenerCount, Is.EqualTo(expected), "Level subscriptions");
            Assert.That(provider.XpToNextLevelState.ListenerCount, Is.EqualTo(expected), "Remaining-XP subscriptions");
        }

        private GameObject CreateObject()
        {
            GameObject instance = new("Audit20261023NeoProgression");
            instance.SetActive(false);
            _ownedObjects.Add(instance);
            return instance;
        }

        private ProgressionManager CreateManager(LevelComponent provider = null)
        {
            ProgressionManager manager = CreateObject().AddComponent<ProgressionManager>();
            SetField(manager, "_loadOnAwake", false);
            manager.AutoSave = false;
            manager.SaveKey = "Audit20261023NeoProgression." + Guid.NewGuid().ToString("N");
            manager.LevelProvider = provider;
            return manager;
        }

        private LevelComponent CreateLevelProvider()
        {
            Core.Level.LevelCurveDefinition curve = ScriptableObject.CreateInstance<Core.Level.LevelCurveDefinition>();
            _ownedObjects.Add(curve);
            curve.SetLinear(100);
            LevelComponent provider = CreateObject().AddComponent<LevelComponent>();
            SetField(provider, "_loadOnAwake", false);
            SetField(provider, "_autoSave", false);
            provider.LevelCurveDefinition = curve;
            return provider;
        }

        private LevelCurveDefinition CreateRewardTrack(params ProgressionLevelDefinition[] levels)
        {
            LevelCurveDefinition track = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            _ownedObjects.Add(track);
            SetField(track, "_levels", new List<ProgressionLevelDefinition>(levels));
            return track;
        }

        private static ProgressionLevelDefinition CreateLevel(int level, int points, params ProgressionReward[] rewards)
        {
            ProgressionLevelDefinition definition = new();
            SetField(definition, "_level", level);
            SetField(definition, "_requiredXp", (level - 1) * 100);
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
            public int SaveCount { get; private set; }
            public int StringReadCount { get; private set; }
            public string FailNextStringReadKey { get; set; }
            public Action<string> BeforeStringRead { get; set; }
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0) =>
                _values.TryGetValue(key, out object value) && value is int result ? result : defaultValue;
            public float GetFloat(string key, float defaultValue = 0f) =>
                _values.TryGetValue(key, out object value) && value is float result ? result : defaultValue;
            public bool GetBool(string key, bool defaultValue = false) =>
                _values.TryGetValue(key, out object value) && value is bool result ? result : defaultValue;
            public string GetString(string key, string defaultValue = "")
            {
                StringReadCount++;
                BeforeStringRead?.Invoke(key);
                if (FailNextStringReadKey != null && string.Equals(FailNextStringReadKey, key, StringComparison.Ordinal))
                {
                    FailNextStringReadKey = null;
                    throw new InvalidOperationException("one-shot profile read failure");
                }

                return _values.TryGetValue(key, out object value) && value is string result ? result : defaultValue;
            }
            public bool HasKey(string key) => _values.ContainsKey(key);
            public void SetInt(string key, int value) => SetValue(key, value);
            public void SetFloat(string key, float value) => SetValue(key, value);
            public void SetBool(string key, bool value) => SetValue(key, value);
            public void SetString(string key, string value) => SetValue(key, value);
            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll() => _values.Clear();
            public void Save()
            {
                SaveCount++;
                OnDataSaved?.Invoke();
            }

            public void Load() => OnDataLoaded?.Invoke();
            private void SetValue(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
