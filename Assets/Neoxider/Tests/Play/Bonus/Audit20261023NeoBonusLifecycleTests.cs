using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoBonusLifecycleTests
    {
        private readonly List<Object> _assets = new();
        private GameObject _root;
        private ISaveProvider _previousProvider;
        private object _previousClock;
        private object _previousClockRestored;
        private object _previousClockHighWater;
        private ManualClock _clock;
        private string _temporaryRoot;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _previousClock = ClockField("_clock").GetValue(null);
            _previousClockRestored = ClockField("_restored").GetValue(null);
            _previousClockHighWater = ClockField("_persistedHighWater").GetValue(null);
            _previousProvider = SaveProvider.CurrentProvider;
            _temporaryRoot = Path.Combine(Path.GetTempPath(), "Audit20261023NeoBonus-" + Guid.NewGuid().ToString("N"));
            FileSaveProviderOptions options = new() { PersistenceRoot = _temporaryRoot };
            SaveProvider.SetProvider(new FileSaveProvider("save.json", options));
            _clock = new ManualClock();
            NeoTrustedTime.SetClockForTests(new TrustedClock(_clock));
            _root = new GameObject(nameof(Audit20261023NeoBonusLifecycleTests));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_root);
                foreach (Object asset in _assets)
                {
                    Object.DestroyImmediate(asset);
                }

                _assets.Clear();
            }
            finally
            {
                Time.timeScale = _previousTimeScale;
                ClockField("_clock").SetValue(null, _previousClock);
                ClockField("_restored").SetValue(null, _previousClockRestored);
                ClockField("_persistedHighWater").SetValue(null, _previousClockHighWater);
                SaveProvider.SetProvider(_previousProvider);
                if (Directory.Exists(_temporaryRoot))
                {
                    Directory.Delete(_temporaryRoot, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator Roulette_DisableDuringFullSpeed_CancelsMotionAndGrant_AndAllowsOneNewRoll()
        {
            yield return AssertRouletteCancellation(1f);
        }

        [UnityTest]
        public IEnumerator Roulette_DisableDuringDeceleration_CancelsTheNestedPhaseAndGrant()
        {
            yield return AssertRouletteCancellation(0f);
        }

        [UnityTest]
        public IEnumerator Roulette_WinListenerStartsANewRoll_WhichRemainsOwnedAndCancellable()
        {
            LineRoulett roulette = _root.AddComponent<LineRoulett>();
            Image first = NewChild("First", true).AddComponent<Image>();
            Image second = NewChild("Second", true).AddComponent<Image>();
            second.transform.position = Vector3.right * 10f;
            SetField(roulette, "images", new[] { first, second });
            SetField(roulette, "arrow", NewChild("Arrow").transform);
            SetField(roulette, "speed", 5f);
            SetField(roulette, "timeRoll", 0f);
            SetField(roulette, "slowDownTime", 0.03f);
            roulette.sprites = new Sprite[1];
            roulette.OnWin = new UnityEvent<int>();
            int grants = 0;
            roulette.OnWin.AddListener(_ =>
            {
                if (++grants == 1)
                {
                    SetField(roulette, "timeRoll", 1f);
                    roulette.StartRolling();
                }
            });
            yield return null;
            roulette.StartRolling();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (grants == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(grants, Is.EqualTo(1));
            roulette.enabled = false;
            Vector3 stoppedPosition = first.transform.position;
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(first.transform.position, Is.EqualTo(stoppedPosition));
            Assert.That(grants, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Wheel_ManualStop_CancelsTheOldAutoStopBeforeTheNextSpin()
        {
            WheelFortune wheel = _root.AddComponent<WheelFortune>();
            RectTransform wheelTransform = NewChild("Wheel", true).GetComponent<RectTransform>();
            SetField(wheel, "_wheelTransform", wheelTransform);
            SetField(wheel, "items", new[] { NewChild("Prize") });
            SetField(wheel, "_singleUse", false);
            SetField(wheel, "_initialAngularVelocity", 100f);
            SetField(wheel, "_angularDeceleration", 10000f);
            SetField(wheel, "_autoStopTime", 0.4f);
            SetField(wheel, "_extraSpinTime", 0f);
            int grants = 0;
            wheel.OnWinIdVariant.AddListener(_ => grants++);

            wheel.Spin();
            wheel.Stop();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (wheel.State != WheelFortune.SpinState.Idle && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(grants, Is.EqualTo(1));
            SetField(wheel, "_autoStopTime", 0f);
            wheel.Spin();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Spinning));
            Assert.That(grants, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Wheel_ZeroDecelerationAndAlignmentDuration_CompleteOneGrantEvenAtZeroTimeScale()
        {
            WheelFortune wheel = _root.AddComponent<WheelFortune>();
            SetField(wheel, "_wheelTransform", NewChild("Wheel", true).GetComponent<RectTransform>());
            SetField(wheel, "items", new[] { NewChild("Prize") });
            SetField(wheel, "_angularDeceleration", 0f);
            SetField(wheel, "_enableAlignment", true);
            SetField(wheel, "_alignmentDuration", 0f);
            int grants = 0;
            wheel.OnWinIdVariant.AddListener(_ => grants++);
            Time.timeScale = 0f;

            wheel.Spin();
            wheel.Stop();
            yield return null;
            yield return null;
            yield return null;

            Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Idle));
            Assert.That(grants, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CooldownInitialization_LoadsItsRealTimeStateBeforePublishingTheCountdown()
        {
            _root.SetActive(false);
            CooldownReward reward = _root.AddComponent<CooldownReward>();
            reward.CooldownSeconds = 60f;
            reward.SetAdditionalKey("Audit20261023NeoBonusRestore", false);
            SetField(reward, "_startTimerOnStart", false);
            SaveProvider.SetString(reward.RewardTimeKey + "_rt", _clock.UtcNow.AddSeconds(20).ToString("o"));
            SaveProvider.SetBool(reward.RewardTimeKey + "_a", true);

            _root.SetActive(true);
            yield return null;

            Assert.That(reward.GetSecondsUntilReward(), Is.InRange(18f, 21f));
            Assert.That(reward.RemainingTimeValue, Is.InRange(18f, 21f));
            Assert.That(reward.GetClaimableCount(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator CooldownDefaultStartup_PreservesTheRestoredRemainingTime()
        {
            _root.SetActive(false);
            CooldownReward reward = _root.AddComponent<CooldownReward>();
            reward.CooldownSeconds = 60f;
            reward.SetAdditionalKey("Audit20261023NeoBonusAutoRestore", false);
            SaveProvider.SetString(reward.RewardTimeKey + "_rt", _clock.UtcNow.AddSeconds(20).ToString("o"));
            SaveProvider.SetBool(reward.RewardTimeKey + "_a", true);

            _root.SetActive(true);
            yield return null;

            Assert.That(reward.IsRunning, Is.True);
            Assert.That(reward.GetSecondsUntilReward(), Is.InRange(18f, 21f));
            Assert.That(reward.RemainingTimeValue, Is.InRange(18f, 21f));
        }

        [UnityTest]
        public IEnumerator CooldownFirstSession_AutoClaimsMultipleCyclesWithoutANeedToDisableOrReload()
        {
            _root.SetActive(false);
            CooldownReward reward = _root.AddComponent<CooldownReward>();
            reward.CooldownSeconds = 1f;
            reward.SetAdditionalKey("Audit20261023NeoBonusFirstSession", false);
            SetField(reward, "_updateInterval", 0.02f);
            reward.AutoClaim = true;
            int grants = 0;
            reward.OnRewardClaimed.AddListener(() => grants++);
            _root.SetActive(true);
            yield return null; // Real Start wires completion and persists the first trusted deadline.

            Assert.That(ReadSavedDeadline(reward), Is.EqualTo(_clock.UtcNow.AddSeconds(1d)));
            _clock.Advance(1d);
            reward.Tick(1f);
            Assert.That(grants, Is.EqualTo(1));
            Assert.That(reward.TakeReward(), Is.False);
            _clock.Advance(1d);
            reward.Tick(1f);

            Assert.That(grants, Is.EqualTo(2));
            Assert.That(reward.IsRunning, Is.True);
            Assert.That(SaveProvider.HasKey(reward.RewardTimeKey + "_rt"), Is.True);
            Assert.That(SaveProvider.GetBool(reward.RewardTimeKey + "_a"), Is.True);
            Assert.That(ReadSavedDeadline(reward), Is.EqualTo(_clock.UtcNow.AddSeconds(1d)));
        }

        [UnityTest]
        public IEnumerator InitiallyAvailableCooldown_ClaimAndRestartBothArmTheFullDuration()
        {
            _root.SetActive(false);
            CooldownReward reward = _root.AddComponent<CooldownReward>();
            reward.CooldownSeconds = 60f;
            reward.SetAdditionalKey("Audit20261023NeoBonusInitialAvailability", false);
            SetField(reward, "_rewardAvailableOnStart", true);
            SetField(reward, "_startTimerOnStart", false);
            _root.SetActive(true);
            yield return null;

            Assert.That(reward.TakeReward(), Is.True);
            Assert.That(reward.IsRunning, Is.True);
            Assert.That(reward.GetSecondsUntilReward(), Is.InRange(59f, 60f));
            Assert.That(reward.GetClaimableCount(), Is.Zero);
            Assert.That(ReadSavedDeadline(reward), Is.EqualTo(_clock.UtcNow.AddSeconds(60d)));
            reward.StopTime();
            reward.RestartTime();
            Assert.That(reward.IsRunning, Is.True);
            Assert.That(reward.GetSecondsUntilReward(), Is.InRange(59f, 60f));
            Assert.That(SaveProvider.GetBool(reward.RewardTimeKey + "_a"), Is.True);
            Assert.That(ReadSavedDeadline(reward), Is.EqualTo(_clock.UtcNow.AddSeconds(60d)));
        }

        [UnityTest]
        public IEnumerator CooldownClaim_DisableSaveAndRecreate_RestoresTheTrustedDeadlineAndRemainingTime()
        {
            const string key = "Audit20261023NeoBonusTrustedRoundTrip";
            GameObject original = NewChild("OriginalReward");
            original.SetActive(false);
            CooldownReward reward = original.AddComponent<CooldownReward>();
            reward.CooldownSeconds = 60f;
            reward.SetAdditionalKey(key, false);
            SetField(reward, "_rewardAvailableOnStart", true);
            SetField(reward, "_startTimerOnStart", false);
            int grants = 0;
            reward.OnRewardClaimed.AddListener(() => grants++);
            original.SetActive(true);
            yield return null;

            DateTime firstDeadline = _clock.UtcNow.AddSeconds(60d);
            Assert.That(reward.TakeReward(), Is.True);
            Assert.That(reward.TakeReward(), Is.False);
            Assert.That(ReadSavedDeadline(reward), Is.EqualTo(firstDeadline));
            _clock.Advance(20d);
            reward.Tick(20f);
            original.SetActive(false); // Real OnDisable persists through the trusted provider hook.
            Assert.That(ReadSavedDeadline(reward), Is.EqualTo(firstDeadline));
            SaveProvider.Save();
            Object.DestroyImmediate(original);

            _clock.Advance(10d);
            FileSaveProviderOptions options = new() { PersistenceRoot = _temporaryRoot };
            SaveProvider.SetProvider(new FileSaveProvider("save.json", options));
            GameObject restoredObject = NewChild("RestoredReward");
            restoredObject.SetActive(false);
            CooldownReward restored = restoredObject.AddComponent<CooldownReward>();
            restored.CooldownSeconds = 60f;
            restored.SetAdditionalKey(key, false);
            SetField(restored, "_startTimerOnStart", false);
            restored.OnRewardClaimed.AddListener(() => grants++);
            restoredObject.SetActive(true); // Real Awake/Init/OnEnable restore from the reopened store.
            yield return null;

            Assert.That(restored.IsRunning, Is.True);
            Assert.That(ReadSavedDeadline(restored), Is.EqualTo(firstDeadline));
            Assert.That(restored.GetSecondsUntilReward(), Is.InRange(28f, 30f));
            Assert.That(restored.RemainingTimeValue, Is.InRange(28f, 30f));
            Assert.That(restored.TakeReward(), Is.False);
            Assert.That(grants, Is.EqualTo(1));

            _clock.Advance(29d);
            restored.Tick(29f);
            Assert.That(restored.GetSecondsUntilReward(), Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
            Assert.That(restored.GetClaimableCount(), Is.Zero);
            Assert.That(restored.TakeReward(), Is.False);
            _clock.Advance(1d);
            restored.Tick(1f);
            Assert.That(restored.TakeReward(), Is.True);
            Assert.That(restored.TakeReward(), Is.False);
            Assert.That(ReadSavedDeadline(restored), Is.EqualTo(firstDeadline.AddSeconds(60d)));
            Assert.That(grants, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Spin_ReentrantPriceOrWalletCallback_ChargesAndCompletesOnlyOnce(bool walletCallback)
        {
            SpinController spin = NewSpin();
            CountingWallet wallet = new();
            spin.moneySpend = wallet;
            spin.moneyAdd = wallet;
            if (walletCallback)
            {
                wallet.OnSpend = () =>
                {
                    if (wallet.SpendCalls == 1)
                    {
                        spin.StartSpin();
                    }
                };
            }
            else
            {
                int notifications = 0;
                spin.OnChangeBet = new UnityEvent<string>();
                spin.OnChangeBet.AddListener(_ =>
                {
                    if (++notifications == 1)
                    {
                        spin.StartSpin();
                    }
                });
            }

            int completed = 0;
            spin.OnEnd.AddListener(_ => completed++);
            spin.ForceNextOutcome(new[,] { { 0 }, { 0 }, { 0 } });
            spin.StartSpin();

            Assert.That(wallet.SpendCalls, Is.EqualTo(1));
            Assert.That(wallet.Balance, Is.EqualTo(90f));
            Assert.That(spin.CompleteActiveSpinImmediately(), Is.True);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(wallet.Balance, Is.EqualTo(100f));
        }

        [Test]
        public void Spin_PriceCallbackDisablesTheController_DoesNotChargeOrStart()
        {
            SpinController spin = NewSpin();
            CountingWallet wallet = new();
            spin.moneySpend = wallet;
            spin.OnChangeBet = new UnityEvent<string>();
            spin.OnChangeBet.AddListener(_ => spin.enabled = false);

            spin.StartSpin();

            Assert.That(wallet.SpendCalls, Is.Zero);
            Assert.That(wallet.Balance, Is.EqualTo(100f));
            Assert.That(spin.IsSpinInProgress, Is.False);
        }

        [Test]
        public void Spin_IncompleteVisualTable_DoesNotChargeOrConsumeTheQueuedOutcome()
        {
            SpinController spin = NewSpin();
            CountingWallet wallet = new();
            spin.moneySpend = wallet;
            spin.ForceNextOutcome(new[,] { { 0 }, { 0 }, { 0 } });
            spin.allSpritesData.visuals[1] = null;

            spin.StartSpin();

            Assert.That(wallet.SpendCalls, Is.Zero);
            Assert.That(wallet.Balance, Is.EqualTo(100f));
            Assert.That(spin.HasForcedNextOutcome, Is.True);
            Assert.That(spin.IsSpinInProgress, Is.False);
        }

        [Test]
        public void Spin_ConfigurationRemainsLockedBetweenStaggeredColumns()
        {
            SpinController spin = NewSpin();
            CountingWallet wallet = new();
            spin.moneySpend = wallet;
            spin.moneyAdd = wallet;
            spin.DelayBetweenColumnSpins = 10f;
            Row[] originalRows = spin.Rows;
            spin.ForceNextOutcome(new[,] { { 0 }, { 0 }, { 0 } });
            spin.StartSpin();
            originalRows[0].Stop(false);
            Assert.That(spin.IsStop(), Is.True, "All reels can be idle during a stagger gap.");
            Assert.That(spin.IsSpinInProgress, Is.True);

            spin.VisibleWindowRows = 4;
            spin.Rows = Array.Empty<Row>();
            spin.ActivePaylineCount = 3;
            spin.BetSelectionIndex = 1;
            spin.AddBet();
            spin.AddLine();
            spin.ConfigureSlotRuntime(5, 3);

            Assert.That(spin.Rows, Is.SameAs(originalRows));
            Assert.That(spin.VisibleWindowRows, Is.EqualTo(1));
            Assert.That(spin.ActivePaylineCount, Is.EqualTo(1));
            Assert.That(spin.BetSelectionIndex, Is.Zero);
            Assert.That(spin.CompleteActiveSpinImmediately(), Is.True);
            CollectionAssert.AreEqual(new[,] { { 0 }, { 0 }, { 0 } }, spin.FinalElementIDs);
        }

        [Test]
        public void Spin_MaximumIntegerPayout_IsCreditedInsteadOfOverflowingToZero()
        {
            SpinController spin = NewSpin();
            SetField(spin.betsData, "_bets", new[] { int.MaxValue });
            spin.BetSelectionIndex = 0;
            CountingWallet wallet = new() { Balance = float.MaxValue };
            spin.moneySpend = wallet;
            spin.moneyAdd = wallet;
            int announced = -1;
            spin.OnWin = new UnityEvent<int>();
            spin.OnWin.AddListener(amount => announced = amount);
            spin.ForceNextOutcome(new[,] { { 0 }, { 0 }, { 0 } });

            spin.StartSpin();
            Assert.That(spin.CompleteActiveSpinImmediately(), Is.True);

            Assert.That(spin.LastPayout, Is.EqualTo(int.MaxValue));
            Assert.That(announced, Is.EqualTo(int.MaxValue));
            Assert.That(wallet.AddCalls, Is.EqualTo(1));
            Assert.That(wallet.Credited, Is.EqualTo((float)int.MaxValue));
        }

        private IEnumerator AssertRouletteCancellation(float fullSpeedSeconds)
        {
            LineRoulett roulette = _root.AddComponent<LineRoulett>();
            Image first = NewChild("First", true).AddComponent<Image>();
            Image second = NewChild("Second", true).AddComponent<Image>();
            second.transform.position = Vector3.right * 10f;
            SetField(roulette, "images", new[] { first, second });
            SetField(roulette, "arrow", NewChild("Arrow").transform);
            roulette.sprites = new Sprite[1];
            SetField(roulette, "speed", 5f);
            SetField(roulette, "timeRoll", fullSpeedSeconds);
            SetField(roulette, "slowDownTime", 0.25f);
            roulette.OnWin = new UnityEvent<int>();
            int grants = 0;
            roulette.OnWin.AddListener(_ => grants++);
            yield return null; // Let the real Start callback initialize its layout.

            roulette.StartRolling();
            yield return null;
            yield return null;
            roulette.enabled = false;
            Vector3 stoppedPosition = first.transform.position;
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(first.transform.position, Is.EqualTo(stoppedPosition));
            Assert.That(grants, Is.Zero);
            roulette.enabled = true;
            SetField(roulette, "timeRoll", 0f);
            SetField(roulette, "slowDownTime", 0.03f);
            roulette.StartRolling();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (grants == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(grants, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(grants, Is.EqualTo(1));
        }

        private SpinController NewSpin()
        {
            SpinController spin = _root.AddComponent<SpinController>();
            Row[] rows = new Row[3];
            for (int x = 0; x < rows.Length; x++)
            {
                GameObject column = NewChild("Row" + x);
                Row row = column.AddComponent<Row>();
                row.countSlotElement = 1;
                row.spaceY = 10f;
                row.offsetY = 0f;
                row.speedControll = new SpeedControll { speed = 100f, timeSpin = 10f };
                for (int i = 0; i < 2; i++)
                {
                    GameObject cell = new("Cell" + i);
                    cell.transform.SetParent(column.transform, false);
                    cell.AddComponent<SlotElement>();
                }

                row.ApplyLayout();
                rows[x] = row;
            }

            spin.Rows = rows;
            spin.VisibleWindowRows = 1;
            spin.DelayBetweenColumnSpins = 0f;
            spin.checkSpin = new CheckSpin();
            spin.betsData = NewAsset<BetsData>();
            spin.BetSelectionIndex = 0;
            spin.allSpritesData = NewAsset<SpritesData>();
            SetField(spin.allSpritesData, "_visuals", new[]
            {
                new SlotVisualData { id = 0 }, new SlotVisualData { id = 1 }
            });
            spin.OnEnd = new UnityEvent<bool>();
            return spin;
        }

        private GameObject NewChild(string name, bool ui = false)
        {
            GameObject child = ui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            child.transform.SetParent(_root.transform, false);
            return child;
        }

        private T NewAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static DateTime ReadSavedDeadline(CooldownReward reward)
        {
            return DateTime.Parse(SaveProvider.GetString(reward.RewardTimeKey + "_rt"),
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static FieldInfo ClockField(string name)
        {
            return typeof(NeoTrustedTime).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(NeoTrustedTime).FullName, name);
        }

        private sealed class ManualClock : INeoClock
        {
            public DateTime UtcNow { get; private set; } = new(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            public double MonotonicSeconds { get; private set; }

            public void Advance(double seconds)
            {
                UtcNow = UtcNow.AddSeconds(seconds);
                MonotonicSeconds += seconds;
            }
        }

        private sealed class CountingWallet : IMoneySpend, IMoneyAdd
        {
            public float Balance = 100f;
            public float Credited;
            public int SpendCalls;
            public int AddCalls;
            public Action OnSpend;

            public bool Spend(float count)
            {
                SpendCalls++;
                OnSpend?.Invoke();
                if (Balance < count)
                {
                    return false;
                }

                Balance -= count;
                return true;
            }

            public void Add(float count)
            {
                AddCalls++;
                Credited += count;
                Balance += count;
            }
        }
    }
}
