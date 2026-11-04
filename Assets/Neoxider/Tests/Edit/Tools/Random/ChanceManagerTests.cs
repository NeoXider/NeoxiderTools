using System;
using System.Collections.Generic;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ChanceManagerTests
    {
        [SetUp]
        public void CaptureRandomState() { _previousRandomState = UnityEngine.Random.state; }

        private UnityEngine.Random.State _previousRandomState;

        [Test]
        public void Constructor_WithWeights_CreatesEntries()
        {
            ChanceManager cm = new ChanceManager(0.3f, 0.7f);
            Assert.AreEqual(2, cm.Count);
        }

        [Test]
        public void GetId_WithDeterministicValue_ReturnsExpectedIndex()
        {
            // WHY: disable auto-normalize to keep raw weights
            ChanceManager cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(1f);
            cm.AddChance(1f);
            cm.AddChance(1f);

            // WHY: randomValue=0 should hit first entry (cumulative starts at 0)
            Assert.AreEqual(0, cm.GetId(0f));
            // WHY: randomValue=1 should hit last entry
            Assert.AreEqual(2, cm.GetId(1f));
        }

        [Test]
        public void GetId_EmptyList_ReturnsMinusOne()
        {
            ChanceManager cm = new ChanceManager();
            Assert.AreEqual(-1, cm.GetId(0.5f));
        }

        [Test]
        public void AddEntry_IncreasesCount()
        {
            ChanceManager cm = new ChanceManager();
            cm.AddEntry(0.5f, "Test");
            Assert.AreEqual(1, cm.Count);
            Assert.AreEqual("Test", cm.GetEntry(0).Label);
        }

        [Test]
        public void RemoveChance_DecreasesCount()
        {
            ChanceManager cm = new ChanceManager(0.5f, 0.5f);
            int initialCount = cm.Count;
            cm.RemoveChance(0);
            Assert.AreEqual(initialCount - 1, cm.Count);
        }

        [Test]
        public void Clear_RemovesAllEntries()
        {
            ChanceManager cm = new ChanceManager(0.2f, 0.3f, 0.5f);
            cm.Clear();
            Assert.AreEqual(0, cm.Count);
        }

        [Test]
        public void Clear_InvalidatesTotalWeightCache()
        {
            ChanceManager cm = new ChanceManager(0.2f, 0.3f, 0.5f);
            // WHY: force the TotalWeight cache to populate before clearing.
            Assert.Greater(cm.TotalWeight, 0f);
            cm.Clear();
            Assert.AreEqual(0f, cm.TotalWeight);
        }

        [Test]
        public void Normalize_SumsToTarget()
        {
            ChanceManager cm = new ChanceManager();
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
            ChanceManager cm = new ChanceManager();
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
            ChanceManager cm = new ChanceManager(0.5f, 0.5f);
            bool result = cm.TryEvaluate(0.3f, out int index, out ChanceManager.Entry entry);
            Assert.IsTrue(result);
            Assert.IsNotNull(entry);
            Assert.GreaterOrEqual(index, 0);
        }

        [Test]
        public void ValidateWeights_EmptyList_ReturnsIssue()
        {
            ChanceManager cm = new ChanceManager();
            List<string> issues = cm.ValidateWeights();
            Assert.IsTrue(issues.Count > 0);
        }

        [Test]
        public void GetNormalizedWeight_ReturnsCorrectProportion()
        {
            ChanceManager cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(1f);
            cm.AddChance(3f);

            float normalized0 = cm.GetNormalizedWeight(0);
            Assert.AreEqual(0.25f, normalized0, 0.001f);
        }

        [Test]
        public void CustomRandomProvider_IsUsed()
        {
            ChanceManager cm = new ChanceManager();
            cm.AutoNormalize = false;
            cm.AddChance(1f);
            cm.AddChance(1f);

            // WHY: always return 0 -> should always pick first entry
            cm.RandomProvider = () => 0f;
            int id = cm.GetChanceId();
            Assert.AreEqual(0, id);
        }

        [Test]
        public void WarmWeightChangesAndRolls_DoNotAllocate()
        {
            ChanceManager cm = new ChanceManager(1f, 2f, 3f, 4f);
            cm.AutoNormalize = true;
            float roll = 0f;
            int picks = 0;
            cm.RandomProvider = () => roll;
            Action cycle = () =>
            {
                for (int i = 0; i < 200; i++)
                {
                    cm.SetChanceValue(i & 3, 1f + (i & 7));
                    cm.SetLocked(1, (i & 1) == 0);
                    roll = i % 10 * 0.1f;
                    if (cm.GetChanceId() >= 0)
                    {
                        picks++;
                    }
                }
            };
            cycle();
            picks = 0;

            long allocationCalls = GcAlloc.TryCount(cycle, out string notAdmitted);

            Assert.That(picks, Is.EqualTo(200), "positive control: every roll picks an entry");
            Assert.That(cm.TotalWeight, Is.EqualTo(1f).Within(1e-4f), "positive control: auto-normalize ran");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "SetChanceValue/SetLocked (normalize plus id repair) and GetChanceId must reuse their buffers");
        }

        [TearDown]
        public void RestoreRandomState() { UnityEngine.Random.state = _previousRandomState; }

    }
}
