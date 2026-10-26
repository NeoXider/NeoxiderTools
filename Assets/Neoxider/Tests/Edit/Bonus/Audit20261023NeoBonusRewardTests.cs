using System;
using System.Globalization;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

#pragma warning disable CS0618
namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoBonusRewardTests
    {
        private Audit20261023NeoEditorTestsClockScope _clockScope;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _clockScope = new Audit20261023NeoEditorTestsClockScope();
            _root = new GameObject(nameof(Audit20261023NeoBonusRewardTests));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_root);
            }
            finally
            {
                _clockScope.Dispose();
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TakeReward_ConsumesBeforeCallbacks_AndRejectsReentrantClaims(bool legacy, bool saveOnTake)
        {
            RewardAccess reward = NewReward(legacy, saveOnTake);
            reward.MakeAvailable();
            int grants = 0;
            int countDuringGrant = -1;
            bool nestedClaim = false;
            reward.Claimed.AddListener(() =>
            {
                grants++;
                countDuringGrant = reward.Count();
                if (grants == 1)
                {
                    nestedClaim = reward.Take();
                }
            });

            Assert.That(reward.Take(), Is.True);
            Assert.That(grants, Is.EqualTo(1));
            Assert.That(countDuringGrant, Is.Zero);
            Assert.That(nestedClaim, Is.False);
            Assert.That(reward.CanTake(), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ManualRestartMode_RejectsRepeatedDirectClaims_UntilExplicitlyRearmed(bool legacy)
        {
            RewardAccess reward = NewReward(legacy, false);
            int grants = 0;
            reward.Claimed.AddListener(() => grants++);
            reward.MakeAvailable();

            Assert.That(reward.Take(), Is.True);
            Assert.That(reward.Take(), Is.False);
            Assert.That(reward.Count(), Is.Zero);
            Assert.That(grants, Is.EqualTo(1));
            reward.MakeAvailable();
            Assert.That(reward.Take(), Is.True);
            Assert.That(grants, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ThrowingGrantListener_DoesNotRestoreTheConsumedClaim_OrLeaveTheGuardLocked(bool legacy)
        {
            RewardAccess reward = NewReward(legacy, true);
            int grants = 0;
            reward.Claimed.AddListener(() =>
            {
                if (++grants == 1)
                {
                    throw new InvalidOperationException("Grant callback failed");
                }
            });
            reward.MakeAvailable();

            Assert.Throws<InvalidOperationException>(() => reward.Take());
            Assert.That(reward.Count(), Is.Zero);
            reward.MakeAvailable();
            Assert.That(reward.Take(), Is.True);
            Assert.That(grants, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NewAvailableReward_WithZeroClaimCap_GrantsNothing(bool legacy)
        {
            RewardAccess reward = NewReward(legacy, true, 0, true);
            SaveProvider.DeleteKey(reward.TimestampKey);

            Assert.That(reward.Count(), Is.Zero);
            Assert.That(reward.Take(), Is.False);
        }

        [Test]
        public void LegacyClaimCount_TinyCooldownAndLongAbsence_SaturatesInsteadOfOverflowing()
        {
            TimeReward reward = _root.AddComponent<TimeReward>();
            reward.secondsToWaitForReward = 0.000001f;
            reward.SetAdditionalKey("Audit20261023NeoBonusLegacyCount", false);
            SetField(reward, "_maxRewardsPerTake", -1);
            SaveProvider.SetString(reward.RewardTimeKey, NeoTrustedTime.UtcNow.AddDays(-1).ToString("o"));

            Assert.That(reward.GetClaimableCount(), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void CooldownClaim_UsesTrustedDeadline_AndRejectsSequentialClaimsUntilExpiry()
        {
            RewardAccess reward = NewReward(false, true);
            CooldownReward cooldown = _root.GetComponent<CooldownReward>();
            DateTime firstDeadline = _clockScope.UtcNow.AddSeconds(60d);
            int grants = 0;
            reward.Claimed.AddListener(() => grants++);
            reward.MakeAvailable();

            Assert.That(reward.Take(), Is.True);
            Assert.That(ReadTimestamp(reward.TimestampKey), Is.EqualTo(firstDeadline));
            Assert.That(reward.Take(), Is.False);
            Assert.That(reward.Count(), Is.Zero);
            Assert.That(grants, Is.EqualTo(1));

            _clockScope.Advance(59d);
            cooldown.Tick(59f);
            Assert.That(reward.Take(), Is.False);
            Assert.That(cooldown.GetSecondsUntilReward(), Is.EqualTo(1f));
            _clockScope.Advance(1d);
            cooldown.Tick(1f);

            Assert.That(reward.Take(), Is.True);
            Assert.That(ReadTimestamp(reward.TimestampKey), Is.EqualTo(firstDeadline.AddSeconds(60d)));
            Assert.That(reward.Take(), Is.False);
            Assert.That(grants, Is.EqualTo(2));
        }

        [Test]
        public void LegacyTake_FractionalCooldown_PersistsPreciseConsumedCyclesBeforeGranting()
        {
            TimeReward reward = _root.AddComponent<TimeReward>();
            reward.secondsToWaitForReward = 1.4f;
            reward.SaveTimeOnTakeReward = true;
            reward.SetAdditionalKey("Audit20261023NeoBonusFractionalAdvance", false);
            SetField(reward, "_maxRewardsPerTake", 3);
            DateTime lastClaim = _clockScope.UtcNow.AddSeconds(-4.5d);
            // 3 * (double)1.4f is 4.199999928474426 seconds: truncate to 41,999,999 ticks.
            DateTime expectedLastClaim = lastClaim.AddTicks(41_999_999L);
            SaveProvider.SetString(reward.RewardTimeKey, lastClaim.ToString("o"));
            int grants = 0;
            int batch = 0;
            reward.OnRewardClaimed.AddListener(() =>
            {
                Assert.That(ReadTimestamp(reward.RewardTimeKey), Is.EqualTo(expectedLastClaim));
                grants++;
            });
            reward.OnRewardsClaimed.AddListener(count => batch = count);

            Assert.That(reward.GetClaimableCount(), Is.EqualTo(3));
            Assert.That(reward.TakeReward(), Is.True);
            Assert.That(ReadTimestamp(reward.RewardTimeKey), Is.EqualTo(expectedLastClaim));
            Assert.That(grants, Is.EqualTo(3));
            Assert.That(batch, Is.EqualTo(3));
            Assert.That(reward.TakeReward(), Is.False);
        }

        private static DateTime ReadTimestamp(string key)
        {
            return DateTime.Parse(SaveProvider.GetString(key), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        }

        private RewardAccess NewReward(bool legacy, bool saveOnTake, int cap = 1, bool availableOnStart = false)
        {
            if (legacy)
            {
                TimeReward reward = _root.AddComponent<TimeReward>();
                reward.secondsToWaitForReward = 60f;
                reward.SaveTimeOnTakeReward = saveOnTake;
                reward.SetAdditionalKey("Audit20261023NeoBonusLegacyClaim", false);
                SetField(reward, "_maxRewardsPerTake", cap);
                SetField(reward, "_rewardAvailableOnStart", availableOnStart);
                return new RewardAccess
                {
                    Take = reward.TakeReward, Count = reward.GetClaimableCount,
                    CanTake = reward.CanTakeReward, MakeAvailable = reward.SetRewardAvailableNow,
                    Claimed = reward.OnRewardClaimed, TimestampKey = reward.RewardTimeKey
                };
            }

            CooldownReward cooldown = _root.AddComponent<CooldownReward>();
            cooldown.CooldownSeconds = 60f;
            cooldown.SaveTimeOnTakeReward = saveOnTake;
            cooldown.MaxRewardsPerTake = cap;
            cooldown.SetAdditionalKey("Audit20261023NeoBonusCooldownClaim", false);
            SetField(cooldown, "_rewardAvailableOnStart", availableOnStart);
            return new RewardAccess
            {
                Take = cooldown.TakeReward, Count = cooldown.GetClaimableCount,
                CanTake = cooldown.CanTakeReward, MakeAvailable = cooldown.SetRewardAvailableNow,
                Claimed = cooldown.OnRewardClaimed, TimestampKey = cooldown.RewardTimeKey + "_rt"
            };
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class RewardAccess
        {
            public Func<bool> Take;
            public Func<int> Count;
            public Func<bool> CanTake;
            public Action MakeAvailable;
            public UnityEvent Claimed;
            public string TimestampKey;
        }
    }
}
#pragma warning restore CS0618
