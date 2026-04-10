using System;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class BonusSystemTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("BonusTestObject");
            SaveProvider.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            SaveProvider.DeleteAll();
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void TimeReward_GetClaimableCount_CalculatesCorrectlyBasedOnSave()
        {
            TimeReward reward = _go.AddComponent<TimeReward>();
            reward.secondsToWaitForReward = 10f; // 10 seconds for 1 reward

            // By default MaxRewardsPerTake is 1 (via private field). Let's set it via reflection to -1 (unlimited)
            typeof(TimeReward).GetField("_maxRewardsPerTake", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(reward, -1);

            // Set additional key
            reward.SetAdditionalKey("TestReward", false);

            // Set save data to exactly 25 seconds ago
            DateTime lastTime = DateTime.UtcNow.AddSeconds(-25);
            SaveProvider.SetString(reward.RewardTimeKey, lastTime.ToUniversalTime().ToString("O"));

            int count = reward.GetClaimableCount();

            // 25 / 10 = 2 rewards
            Assert.AreEqual(2, count, "Should have exactly 2 claimable rewards for 25 seconds elapsed");
        }

        [Test]
        public void TimeReward_TakeReward_UpdatesSaveKeyAndEvents()
        {
            TimeReward reward = _go.AddComponent<TimeReward>();
            reward.secondsToWaitForReward = 10f;
            reward.SetAdditionalKey("TestReward2", false);

            // Manually set old time to allow 1 claim
            DateTime lastTime = DateTime.UtcNow.AddSeconds(-15);
            SaveProvider.SetString(reward.RewardTimeKey, lastTime.ToUniversalTime().ToString("O"));

            bool claimFired = false;
            reward.OnRewardClaimed.AddListener(() => claimFired = true);

            bool result = reward.TakeReward();

            Assert.IsTrue(result, "TakeReward should succeed");
            Assert.IsTrue(claimFired, "OnRewardClaimed event should fire");

            // Ensure SaveProvider's key advanced
            string newSavedTimeStr = SaveProvider.GetString(reward.RewardTimeKey, "");
            DateTime newSavedTime;
            Assert.IsTrue(DateTime.TryParse(newSavedTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind,
                out newSavedTime));

            // The new saved time should be original + 10s. So roughly 5 seconds ago
            double elapsed = (DateTime.UtcNow - newSavedTime).TotalSeconds;
            Assert.IsTrue(elapsed >= 3 && elapsed <= 7,
                $"Save key was not advanced appropriately. Remaining pseudo elapsed: {elapsed}");
        }
    }
}
