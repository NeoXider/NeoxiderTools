using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoToolsRandomRegressionTests
    {
        [Test]
        public void Normalize_RefreshesCachedTotalProbabilitiesAndSelection()
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChances(new[] { 2f, 3f });
            Assert.That(manager.TotalWeight, Is.EqualTo(5f));

            manager.Normalize(1f);

            Assert.That(manager.TotalWeight, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(manager.GetNormalizedWeight(0), Is.EqualTo(0.4f).Within(0.00001f));
            Assert.That(manager.GetId(0.1f), Is.EqualTo(0));
        }

        [Test]
        public void Normalize_LockedWeightExceedsTarget_RefreshesTotalAndDisablesUnlockedEntry()
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddEntry(2f, locked: true);
            manager.AddEntry(3f);
            Assert.That(manager.TotalWeight, Is.EqualTo(5f));

            manager.Normalize(1f);

            Assert.That(manager.GetChanceValue(0), Is.EqualTo(2f));
            Assert.That(manager.GetChanceValue(1), Is.Zero);
            Assert.That(manager.TotalWeight, Is.EqualTo(2f));
            Assert.That(manager.GetId(0.9f), Is.EqualTo(0));
        }

        [Test]
        public void Normalize_ZeroWeightsBecomeSelectableAfterEvenRedistribution()
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChances(new[] { 0f, 0f });
            Assert.That(manager.TotalWeight, Is.Zero);

            manager.Normalize(2f);

            Assert.That(manager.TotalWeight, Is.EqualTo(2f));
            Assert.That(manager.GetNormalizedWeight(0), Is.EqualTo(0.5f));
            Assert.That(manager.GetId(0.75f), Is.EqualTo(1));
        }

        [Test]
        public void AutoNormalize_EnablingItRefreshesCachedTotal()
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChances(new[] { 2f, 3f });
            Assert.That(manager.TotalWeight, Is.EqualTo(5f));

            manager.AutoNormalize = true;

            Assert.That(manager.TotalWeight, Is.EqualTo(1f).Within(0.00001f));
            Assert.That(manager.GetNormalizedWeight(1), Is.EqualTo(0.6f).Within(0.00001f));
        }

        [Test]
        public void NormalizeTarget_ChangingItRefreshesCachedTotal()
        {
            ChanceManager manager = new(1f, 1f);
            Assert.That(manager.TotalWeight, Is.EqualTo(1f));

            manager.NormalizeTarget = 4f;

            Assert.That(manager.TotalWeight, Is.EqualTo(4f));
            Assert.That(manager.GetNormalizedWeight(0), Is.EqualTo(0.5f));
        }

        [TestCase(-1f, 1)]
        [TestCase(0f, 1)]
        [TestCase(0.5f, 3)]
        [TestCase(1f, 3)]
        [TestCase(2f, 3)]
        public void GetId_ClampedEndpointsAndBucketBoundariesSkipZeroWeights(float roll, int expectedIndex)
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChances(new[] { 0f, 1f, 0f, 1f, 0f });

            Assert.That(manager.GetId(roll), Is.EqualTo(expectedIndex));
        }

        [Test]
        public void RandomProvider_ZeroNeverSelectsDisabledLeadingEntry()
        {
            ChanceManager manager = new(0f, 1f, 0f) { RandomProvider = () => 0f };

            Assert.That(manager.TryEvaluate(out int index, out ChanceManager.Entry entry), Is.True);
            Assert.That(index, Is.EqualTo(1));
            Assert.That(entry, Is.SameAs(manager.GetEntry(1)));
        }

        [Test]
        public void GetId_NaNReturnsNoResultThroughAllEvaluationPaths()
        {
            ChanceManager manager = new(1f) { RandomProvider = () => float.NaN };

            Assert.That(manager.GetId(float.NaN), Is.EqualTo(-1));
            Assert.That(manager.Evaluate(float.NaN), Is.Null);
            Assert.That(manager.TryEvaluate(out int index, out ChanceManager.Entry entry), Is.False);
            Assert.That(index, Is.EqualTo(-1));
            Assert.That(entry, Is.Null);
        }

        [Test]
        public void Normalize_LargeFiniteWeightsKeepTheirProportions()
        {
            ChanceManager manager = new(float.MaxValue, float.MaxValue);

            Assert.That(manager.GetChanceValue(0), Is.EqualTo(0.5f));
            Assert.That(manager.GetChanceValue(1), Is.EqualTo(0.5f));
            Assert.That(manager.TotalWeight, Is.EqualTo(1f));
            Assert.That(manager.GetId(0.75f), Is.EqualTo(1));
        }

        [Test]
        public void Normalize_SubnormalWeightCanReachLargeFiniteTarget()
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChance(float.Epsilon);

            manager.Normalize(float.MaxValue);

            Assert.That(manager.GetChanceValue(0), Is.EqualTo(float.MaxValue));
            Assert.That(manager.TotalWeight, Is.EqualTo(float.MaxValue));
            Assert.That(manager.GetNormalizedWeight(0), Is.EqualTo(1f));
            Assert.That(manager.GetId(0.5f), Is.EqualTo(0));
        }

        [Test]
        public void RawLargeFiniteWeightsHaveValidProbabilitiesAndDeterministicRolls()
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChances(new[] { float.MaxValue, float.MaxValue, 0f });

            Assert.That(manager.TotalWeight, Is.EqualTo(float.PositiveInfinity));
            Assert.That(manager.GetNormalizedWeight(0), Is.EqualTo(0.5f));
            Assert.That(manager.GetNormalizedWeight(1), Is.EqualTo(0.5f));
            Assert.That(manager.GetId(0f), Is.EqualTo(0));
            Assert.That(manager.GetId(0.25f), Is.EqualTo(0));
            Assert.That(manager.GetId(0.75f), Is.EqualTo(1));
            Assert.That(manager.GetId(1f), Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidWeightsAreDisabledAndDoNotPoisonValidEntries(float weight)
        {
            ChanceManager manager = new() { AutoNormalize = false };
            manager.AddChances(new[] { weight, 2f });
            manager.SetChanceValue(0, weight);

            Assert.That(manager.GetChanceValue(0), Is.Zero);
            Assert.That(manager.TotalWeight, Is.EqualTo(2f));
            Assert.That(manager.GetNormalizedWeight(1), Is.EqualTo(1f));
            Assert.That(manager.GetId(0.25f), Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Normalize_InvalidTargetDisablesUnlockedWeightsWithoutNaN(float target)
        {
            ChanceManager manager = new(1f, 3f);
            Assert.That(manager.TotalWeight, Is.EqualTo(1f));

            manager.Normalize(target);

            Assert.That(manager.GetChanceValue(0), Is.Zero);
            Assert.That(manager.GetChanceValue(1), Is.Zero);
            Assert.That(manager.TotalWeight, Is.Zero);
            Assert.That(manager.GetId(0.5f), Is.EqualTo(-1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void NormalizeTarget_InvalidValueUsesFiniteMinimum(float target)
        {
            ChanceManager manager = new(1f, 1f);
            manager.NormalizeTarget = target;

            Assert.That(manager.NormalizeTarget, Is.EqualTo(0.001f));
            Assert.That(manager.TotalWeight, Is.EqualTo(0.001f).Within(0.000001f));
            Assert.That(manager.GetNormalizedWeight(0), Is.EqualTo(0.5f));
        }

        [Test]
        public void CopyFrom_SelfPreservesEntriesMetadataAndSampling()
        {
            ChanceManager manager = new() { AutoNormalize = false, RandomProvider = () => 0.75f };
            ChanceManager.Entry first = manager.AddEntry(1f, "First", true, 17);
            ChanceManager.Entry second = manager.AddEntry(3f, "Second", customId: 23);
            first.Notes = "Keep this configuration";
            Assert.That(manager.TotalWeight, Is.EqualTo(4f));

            manager.CopyFrom(manager);

            Assert.That(manager.Count, Is.EqualTo(2));
            Assert.That(manager.GetEntry(0), Is.SameAs(first));
            Assert.That(manager.GetEntry(1), Is.SameAs(second));
            Assert.That(first.Label, Is.EqualTo("First"));
            Assert.That(first.Locked, Is.True);
            Assert.That(first.CustomId, Is.EqualTo(17));
            Assert.That(first.Notes, Is.EqualTo("Keep this configuration"));
            Assert.That(manager.TotalWeight, Is.EqualTo(4f));
            Assert.That(manager.GetChanceId(), Is.EqualTo(1));
        }

        [Test]
        public void EvaluateAndNotify_ReentrantListenerDoesNotReplaceReturnedRoll()
        {
            GameObject owner = new("Random audit regression");
            try
            {
                ChanceSystemBehaviour behaviour = owner.AddComponent<ChanceSystemBehaviour>();
                behaviour.Manager.AddChances(new[] { 1f, 1f });
                behaviour.Manager.RandomProvider = () => 0f;
                ChanceManager.Entry original = behaviour.Manager.GetEntry(0);
                bool nested = false;
                int notifications = 0;
                int completions = 0;
                behaviour.OnRollComplete.AddListener(() => completions++);
                behaviour.OnIndexAndWeightSelected.AddListener((index, weight) =>
                {
                    notifications++;
                    if (!nested)
                    {
                        nested = true;
                        behaviour.SetResultAndNotify(1);
                    }
                });

                ChanceManager.Entry result = behaviour.EvaluateAndNotify();

                Assert.That(result, Is.SameAs(original));
                Assert.That(behaviour.LastSelectedIndex, Is.EqualTo(1));
                Assert.That(behaviour.LastSelectedEntry, Is.SameAs(behaviour.Manager.GetEntry(1)));
                Assert.That(notifications, Is.EqualTo(2));
                Assert.That(completions, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
