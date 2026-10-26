using System;
using System.Collections.Generic;
using Neo.Extensions;
using NUnit.Framework;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NumericRandomAndPrimitiveTests
    {
        private Random.State _randomState;
        private bool _errorsEnabled;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _errorsEnabled = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: false);
        }

        [TearDown]
        public void TearDown()
        {
            Random.state = _randomState;
            NeoDiagnostics.Configure(errors: _errorsEnabled);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void WeightedChoice_NonFiniteWeightRejectsEntireDistribution(float invalidWeight)
        {
            float[] weights = { 1f, invalidWeight };
            string[] items = { "valid", "invalid" };
            Assert.AreEqual(-1, weights.GetRandomWeightedIndex());
            Assert.IsNull(items.GetRandomWeighted(weights));
            Assert.IsNull(items.GetRandomWeighted(item => item == "valid" ? 1f : invalidWeight));
        }

        [Test]
        public void WeightedChoice_FloatOverflowCannotCollapseBothBucketsIntoLastBucket()
        {
            Random.InitState(314159);
            float[] weights = { float.MaxValue, float.MaxValue, 0f };
            HashSet<int> selected = new();
            for (int i = 0; i < 256; i++)
            {
                selected.Add(weights.GetRandomWeightedIndex());
            }

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, selected);
        }

        [Test]
        public void Chance_NaNIsRejectedAsAnInvalidProbability()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => float.NaN.Chance());
        }

        [Test]
        public void RandomizeMinimumInteger_SamplesBothSignHalves()
        {
            Random.InitState(271828);
            bool positiveSeen = false;
            bool negativeSeen = false;
            for (int i = 0; i < 128; i++)
            {
                int result = int.MinValue.RandomizeBetween();
                positiveSeen |= result >= 0;
                negativeSeen |= result < 0;
            }

            Assert.IsTrue(positiveSeen, "Negating int.MinValue must not collapse the range to one negative value.");
            Assert.IsTrue(negativeSeen);
        }

        [Test]
        public void NormalizeAndDenormalize_FullFiniteFloatRangeRetainsMidpointAndEndpoints()
        {
            Assert.AreEqual(0.5f, 0f.NormalizeToUnit(-float.MaxValue, float.MaxValue));
            Assert.AreEqual(0f, 0f.NormalizeToRange(-float.MaxValue, float.MaxValue));
            Assert.AreEqual(0f, 0.5f.Denormalize(-float.MaxValue, float.MaxValue));
            Assert.AreEqual(-float.MaxValue, 0f.Denormalize(-float.MaxValue, float.MaxValue));
            Assert.AreEqual(float.MaxValue, 1f.Denormalize(-float.MaxValue, float.MaxValue));
            Assert.AreEqual(15f, 0f.Remap(-float.MaxValue, float.MaxValue, 10f, 20f));
        }

        [TestCase(1)]
        [TestCase(39)]
        [TestCase(100)]
        public void DecimalRounding_LargeFiniteFloatStaysFinite(int places)
        {
            Assert.AreEqual(float.MaxValue, float.MaxValue.RoundToDecimal(places));
        }

        [Test]
        public void DecimalRounding_SubnormalFloatWithSufficientPrecisionIsPreserved()
        {
            Assert.AreEqual(float.Epsilon, float.Epsilon.RoundToDecimal(46));
        }

        [TestCase(16777217, 1, 16777217)]
        [TestCase(int.MaxValue, 1, int.MaxValue)]
        [TestCase(int.MaxValue, 2, int.MaxValue)]
        [TestCase(int.MinValue, 3, int.MinValue)]
        [TestCase(5, 2, 4)]
        [TestCase(-5, 2, -4)]
        [TestCase(3, 2, 4)]
        [TestCase(-3, 2, -4)]
        public void IntegerSnap_RetainsPrecisionTiesAndRange(int value, int step, int expected)
        {
            Assert.AreEqual(expected, value.Snap(step));
        }

        [Test]
        public void FloatSnap_OverflowingIntermediateRetainsFiniteResult()
        {
            Assert.AreEqual(1f, 1f.Snap(float.Epsilon));
            Assert.AreEqual(float.MaxValue, float.MaxValue.Snap(2e38f));
        }
    }
}
