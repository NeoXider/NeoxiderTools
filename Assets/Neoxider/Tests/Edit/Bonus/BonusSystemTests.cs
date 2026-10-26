using System;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

#pragma warning disable CS0618
namespace Neo.Editor.Tests
{
    [TestFixture]
    public class BonusSystemTests
    {
        private GameObject _go;
        private Audit20261023NeoEditorTestsClockScope _clockScope;

        [SetUp]
        public void SetUp()
        {
            _clockScope = new Audit20261023NeoEditorTestsClockScope();
            _go = new GameObject("BonusTestObject");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_go != null)
                {
                    UnityEngine.Object.DestroyImmediate(_go);
                }
            }
            finally
            {
                _clockScope?.Dispose();
                _clockScope = null;
            }
        }

        [Test]
        public void TimeReward_GetClaimableCount_CalculatesCorrectlyBasedOnSave()
        {
            TimeReward reward = _go.AddComponent<TimeReward>();
            reward.secondsToWaitForReward = 10f;

            // WHY: By default MaxRewardsPerTake is 1 (via private field). Let's set it via reflection to -1 (unlimited)
            FieldInfo cap = typeof(TimeReward).GetField("_maxRewardsPerTake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(cap, Is.Not.Null);
            cap.SetValue(reward, -1);

            reward.SetAdditionalKey("TestReward", false);

            DateTime lastTime = _clockScope.UtcNow.AddSeconds(-25);
            SaveProvider.SetString(reward.RewardTimeKey, lastTime.ToUniversalTime().ToString("O"));

            int count = reward.GetClaimableCount();

            // WHY: 25 / 10 = 2 rewards
            Assert.AreEqual(2, count, "Should have exactly 2 claimable rewards for 25 seconds elapsed");
        }

        [Test]
        public void TimeReward_TakeReward_UpdatesSaveKeyAndEvents()
        {
            TimeReward reward = _go.AddComponent<TimeReward>();
            reward.secondsToWaitForReward = 10f;
            reward.SetAdditionalKey("TestReward2", false);

            // WHY: Manually set old time to allow 1 claim
            DateTime lastTime = _clockScope.UtcNow.AddSeconds(-15);
            SaveProvider.SetString(reward.RewardTimeKey, lastTime.ToUniversalTime().ToString("O"));

            bool claimFired = false;
            reward.OnRewardClaimed.AddListener(() => claimFired = true);

            bool result = reward.TakeReward();

            Assert.IsTrue(result, "TakeReward should succeed");
            Assert.IsTrue(claimFired, "OnRewardClaimed event should fire");

            string newSavedTimeStr = SaveProvider.GetString(reward.RewardTimeKey, "");
            DateTime newSavedTime;
            Assert.IsTrue(DateTime.TryParse(newSavedTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind,
                out newSavedTime));

            Assert.That(newSavedTime, Is.EqualTo(lastTime.AddSeconds(10)));
            Assert.That((_clockScope.UtcNow - newSavedTime).TotalSeconds, Is.EqualTo(5d));
            Assert.That(reward.GetClaimableCount(), Is.Zero);
            _clockScope.Advance(5d);
            Assert.That(reward.GetClaimableCount(), Is.EqualTo(1));
        }

        [Test]
        public void TimeReward_IsLegacyButNotUserCreationMenuEntry()
        {
            Type type = typeof(TimeReward);

            Assert.That(type.GetCustomAttribute<ObsoleteAttribute>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<LegacyComponentAttribute>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<CreateFromMenuAttribute>(), Is.Null);
            Assert.That(type.GetCustomAttribute<AddComponentMenu>(), Is.Null);
        }

        [Test]
        public void VisualSlotLines_LineActiv_IgnoresMissingLineReferences()
        {
            VisualSlotLines lines = new VisualSlotLines();

            Assert.DoesNotThrow(() => lines.LineActiv(false));
            Assert.DoesNotThrow(() => lines.LineActiv((int[])null));

            lines.lines = new GameObject[2];

            Assert.DoesNotThrow(() => lines.LineActiv(true));
            Assert.DoesNotThrow(() => lines.LineActiv(new[] { 0, 1 }));
        }
    }
}
#pragma warning restore CS0618
