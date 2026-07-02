using System;
using Neo.Bonus;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the 9.6.1/9.6.2 auto-claim regen fix: claiming must re-arm the underlying
    ///     non-looping timer (previously the timer stayed stopped, so continuous regen fired once
    ///     and the countdown froze).
    /// </summary>
    [TestFixture]
    public class CooldownRewardAutoClaimTests
    {
        private const string TestKey = "AutoClaimReArmTest";

        private GameObject _go;
        private CooldownReward _reward;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("CooldownRewardAutoClaimTests");
            _reward = _go.AddComponent<CooldownReward>();
            _reward.CooldownSeconds = 60f;
            _reward.SetAdditionalKey(TestKey, false);
            DeleteSaveKeys();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteSaveKeys();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        private void DeleteSaveKeys()
        {
            SaveProvider.DeleteKey(_reward.RewardTimeKey);
            SaveProvider.DeleteKey(_reward.RewardTimeKey + "_rt");
            SaveProvider.DeleteKey(_reward.RewardTimeKey + "_a");
        }

        private void MakeClaimableNow()
        {
            // Saved end time in the past => exactly one accumulated claim is available.
            SaveProvider.SetString(_reward.RewardTimeKey + "_rt",
                DateTime.UtcNow.AddSeconds(-1).ToString("o"));
        }

        [Test]
        public void TakeReward_RestartsCooldown_AndRearmsTimer()
        {
            MakeClaimableNow();
            Assert.IsTrue(_reward.CanTakeReward(), "arranged state must be claimable");

            Assert.IsTrue(_reward.TakeReward());

            Assert.IsFalse(_reward.CanTakeReward(), "cooldown must restart after a claim");
            Assert.IsTrue(_reward.IsRunning, "timer must re-arm after a claim (continuous regen)");
        }

        [Test]
        public void TakeReward_CanClaimAgain_AfterNextCooldownElapses()
        {
            MakeClaimableNow();
            Assert.IsTrue(_reward.TakeReward());

            // Simulate the next cooldown elapsing by rewriting the saved end time into the past.
            MakeClaimableNow();

            Assert.IsTrue(_reward.CanTakeReward(), "a re-armed reward must become claimable again");
            Assert.IsTrue(_reward.TakeReward(), "second claim must succeed (regen loop)");
        }
    }
}
