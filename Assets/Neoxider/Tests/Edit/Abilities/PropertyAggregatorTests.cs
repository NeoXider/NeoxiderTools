using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Pure property math: final = max((base + sum Add) * product Mul, all Max floors).
    /// </summary>
    public sealed class PropertyAggregatorTests
    {
        private static List<ResolvedContribution> Contributions(params ResolvedContribution[] items)
        {
            return new List<ResolvedContribution>(items);
        }

        private static ResolvedContribution Add(float v) => new ResolvedContribution(PropertyOp.Add, v);
        private static ResolvedContribution Mul(float v) => new ResolvedContribution(PropertyOp.Mul, v);
        private static ResolvedContribution Max(float v) => new ResolvedContribution(PropertyOp.Max, v);

        [Test]
        public void EmptyContributions_ReturnsBase()
        {
            Assert.That(PropertyAggregator.Compute(100f, Contributions()), Is.EqualTo(100f));
        }

        [Test]
        public void NullContributions_ReturnsBase()
        {
            Assert.That(PropertyAggregator.Compute(42f, null), Is.EqualTo(42f));
        }

        [Test]
        public void AddOnly_SumsWithBase()
        {
            float result = PropertyAggregator.Compute(100f, Contributions(Add(10f), Add(5f), Add(-3f)));
            Assert.That(result, Is.EqualTo(112f).Within(0.0001f));
        }

        [Test]
        public void MulOnly_MultipliesBase()
        {
            float result = PropertyAggregator.Compute(100f, Contributions(Mul(0.5f), Mul(2f)));
            Assert.That(result, Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void AddThenMul_AppliesAddBeforeMul()
        {
            // (100 + 20) * 1.5 = 180, NOT 100 + 20 * 1.5
            float result = PropertyAggregator.Compute(100f, Contributions(Add(20f), Mul(1.5f)));
            Assert.That(result, Is.EqualTo(180f).Within(0.0001f));
        }

        [Test]
        public void Max_RaisesResultToFloor()
        {
            float result = PropertyAggregator.Compute(100f, Contributions(Max(150f)));
            Assert.That(result, Is.EqualTo(150f).Within(0.0001f));
        }

        [Test]
        public void Max_NeverLowersResult()
        {
            // Floor below the computed value must not pull it down.
            float result = PropertyAggregator.Compute(100f, Contributions(Max(50f)));
            Assert.That(result, Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void Max_UsesHighestFloor()
        {
            float result = PropertyAggregator.Compute(0f, Contributions(Max(30f), Max(80f), Max(50f)));
            Assert.That(result, Is.EqualTo(80f).Within(0.0001f));
        }

        [Test]
        public void CombinedAddMulMax_AppliesInOrder()
        {
            // (10 + 5) * 2 = 30, then floor 40 raises it to 40.
            float result = PropertyAggregator.Compute(10f, Contributions(Add(5f), Mul(2f), Max(40f)));
            Assert.That(result, Is.EqualTo(40f).Within(0.0001f));
        }

        [Test]
        public void ValueForStacks_ScalesPerStackBeyondFirst()
        {
            var c = new PropertyContribution("armor", PropertyOp.Add, 5f, 3f);
            Assert.That(c.ValueForStacks(1), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(c.ValueForStacks(2), Is.EqualTo(8f).Within(0.0001f));
            Assert.That(c.ValueForStacks(3), Is.EqualTo(11f).Within(0.0001f));
        }

        [Test]
        public void ValueForStacks_ZeroOrOneStack_ReturnsBaseValue()
        {
            var c = new PropertyContribution("armor", PropertyOp.Add, 5f, 3f);
            Assert.That(c.ValueForStacks(0), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(c.ValueForStacks(1), Is.EqualTo(5f).Within(0.0001f));
        }
    }
}
