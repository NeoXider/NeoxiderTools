using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ChanceManagerTests
    {
        [Test]
        public void Constructor_WithWeights_CreatesEntries()
        {
            var cm = new ChanceManager(0.3f, 0.7f);
            Assert.AreEqual(2, cm.Count);
        }

        [Test]
        public void GetId_WithDeterministicValue_ReturnsExpectedIndex()
        {
            // Disable auto-normalize to keep raw weights
            var cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(1f);
            cm.AddChance(1f);
            cm.AddChance(1f);

            // randomValue=0 should hit first entry (cumulative starts at 0)
            Assert.AreEqual(0, cm.GetId(0f));
            // randomValue=1 should hit last entry
            Assert.AreEqual(2, cm.GetId(1f));
        }

        [Test]
        public void GetId_EmptyList_ReturnsMinusOne()
        {
            var cm = new ChanceManager();
            Assert.AreEqual(-1, cm.GetId(0.5f));
        }

        [Test]
        public void AddEntry_IncreasesCount()
        {
            var cm = new ChanceManager();
            cm.AddEntry(0.5f, "Test");
            Assert.AreEqual(1, cm.Count);
            Assert.AreEqual("Test", cm.GetEntry(0).Label);
        }

        [Test]
        public void RemoveChance_DecreasesCount()
        {
            var cm = new ChanceManager(0.5f, 0.5f);
            int initialCount = cm.Count;
            cm.RemoveChance(0);
            Assert.AreEqual(initialCount - 1, cm.Count);
        }

        [Test]
        public void Clear_RemovesAllEntries()
        {
            var cm = new ChanceManager(0.2f, 0.3f, 0.5f);
            cm.Clear();
            Assert.AreEqual(0, cm.Count);
        }

        [Test]
        public void Normalize_SumsToTarget()
        {
            var cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(2f);
            cm.AddChance(3f);
            cm.Normalize(1f);

            float total = 0f;
            for (int i = 0; i < cm.Count; i++)
            {
                total += cm.GetChanceValue(i);
            }

            Assert.AreEqual(1f, total, 0.001f);
        }

        [Test]
        public void SetLocked_PreservesWeightDuringNormalize()
        {
            var cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(0.5f);
            cm.AddChance(0.5f);
            cm.SetLocked(0, true);

            float lockedWeight = cm.GetChanceValue(0);
            cm.Normalize(1f);

            Assert.AreEqual(lockedWeight, cm.GetChanceValue(0), 0.001f, "Locked entry weight should be preserved.");
        }

        [Test]
        public void TryEvaluate_WithValidEntries_ReturnsTrue()
        {
            var cm = new ChanceManager(0.5f, 0.5f);
            bool result = cm.TryEvaluate(0.3f, out int index, out ChanceManager.Entry entry);
            Assert.IsTrue(result);
            Assert.IsNotNull(entry);
            Assert.GreaterOrEqual(index, 0);
        }

        [Test]
        public void ValidateWeights_EmptyList_ReturnsIssue()
        {
            var cm = new ChanceManager();
            List<string> issues = cm.ValidateWeights();
            Assert.IsTrue(issues.Count > 0);
        }

        [Test]
        public void GetNormalizedWeight_ReturnsCorrectProportion()
        {
            var cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(1f);
            cm.AddChance(3f);

            float normalized0 = cm.GetNormalizedWeight(0);
            Assert.AreEqual(0.25f, normalized0, 0.001f);
        }

        [Test]
        public void CustomRandomProvider_IsUsed()
        {
            var cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(1f);
            cm.AddChance(1f);

            // Always return 0 -> should always pick first entry
            cm.RandomProvider = () => 0f;
            int id = cm.GetChanceId();
            Assert.AreEqual(0, id);
        }
    }
}
