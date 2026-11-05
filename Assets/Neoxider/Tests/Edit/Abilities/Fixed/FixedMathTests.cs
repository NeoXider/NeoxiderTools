using Neo.Abilities.Fixed;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>Integer arithmetic of the fixed core: rounding, saturation, 128-bit MulDiv, parsing, cones.</summary>
    public sealed class FixedMathTests
    {
        [TestCase(5L, 2L, 3L)]
        [TestCase(-5L, 2L, -3L)]
        [TestCase(7L, 3L, 2L)]
        [TestCase(-7L, 3L, -2L)]
        [TestCase(8L, 3L, 3L)]
        [TestCase(5L, -2L, -3L)]
        [TestCase(0L, 7L, 0L)]
        public void DivRound_RoundsHalfAwayFromZero(long value, long divisor, long expected)
        {
            Assert.That(FixedMath.DivRoundHalfAwayFromZero(value, divisor), Is.EqualTo(expected));
        }

        [Test]
        public void MulDiv_IsExactBeyond64BitProducts_AndSaturates()
        {
            long big = long.MaxValue / 3L;
            Assert.That(FixedMath.MulDiv(big, 6L, 6L), Is.EqualTo(big), "the 128-bit product divides back exactly");
            Assert.That(FixedMath.MulDiv(big, -6L, 6L), Is.EqualTo(-big));
            Assert.That(FixedMath.MulDiv(long.MaxValue, long.MaxValue, 1L), Is.EqualTo(long.MaxValue));
            Assert.That(FixedMath.MulDiv(long.MaxValue, -long.MaxValue, 1L), Is.EqualTo(long.MinValue));
            Assert.That(FixedMath.MulDiv(3L, 5L, 2L), Is.EqualTo(8L), "7.5 rounds away from zero");
            Assert.That(FixedMath.MulDivToInt(int.MaxValue, 4L, 1L), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void SaturatingOperations_ClampInsteadOfWrapping()
        {
            Assert.That(FixedMath.SaturatingAdd(int.MaxValue, 1), Is.EqualTo(int.MaxValue));
            Assert.That(FixedMath.SaturatingAdd(int.MinValue, -1), Is.EqualTo(int.MinValue));
            Assert.That(FixedMath.SaturatingAdd(long.MaxValue, 1L), Is.EqualTo(long.MaxValue));
            Assert.That(FixedMath.SaturatingNegate(int.MinValue), Is.EqualTo(int.MaxValue));
            Assert.That(FixedMath.SaturatingSubtract(int.MinValue, 1), Is.EqualTo(int.MinValue));
            Assert.That(FixedMath.SaturatingMultiply(1 << 20, 1 << 20), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void SqrtFloor_AndGcd()
        {
            Assert.That(FixedMath.SqrtFloor(99L), Is.EqualTo(9L));
            Assert.That(FixedMath.SqrtFloor(100L), Is.EqualTo(10L));
            Assert.That(FixedMath.SqrtFloor(ulong.MaxValue), Is.EqualTo(4294967295UL));
            Assert.That(FixedMath.Gcd(84L, -36L), Is.EqualTo(12L));
        }

        [Test]
        public void Format_ConvertsAndMultipliesInRaw()
        {
            FixedFormat q8 = FixedFormat.Q8;
            Assert.That(q8.One, Is.EqualTo(256));
            Assert.That(q8.FromInt(3), Is.EqualTo(768));
            Assert.That(q8.FromRatio(1, 3), Is.EqualTo(85));
            Assert.That(q8.FromPercent(25), Is.EqualTo(64));
            Assert.That(q8.Mul(384, 384), Is.EqualTo(576), "1.5 x 1.5 = 2.25");
            Assert.That(q8.Div(256, 768), Is.EqualTo(85));
            Assert.That(q8.Div(5, 0), Is.EqualTo(int.MaxValue));
            Assert.That(q8.ToIntRounded(640), Is.EqualTo(3), "2.5 rounds away from zero");
            Assert.That(FixedFormat.Q16.One, Is.EqualTo(65536));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new FixedFormat(17));
        }

        [TestCase("0.5", 128)]
        [TestCase("-1.25", -320)]
        [TestCase("+3", 768)]
        [TestCase("0.001", 0)]
        [TestCase("0.002", 1)]
        [TestCase(".75", 192)]
        public void Format_ParsesDecimalsWithoutFloatingPoint(string text, int expected)
        {
            Assert.That(FixedFormat.Q8.TryParse(text, out int raw), Is.True);
            Assert.That(raw, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("1.2.3")]
        [TestCase("-")]
        public void Format_RejectsNonDecimals(string text)
        {
            Assert.That(FixedFormat.Q8.TryParse(text, out _), Is.False);
        }

        [Test]
        public void FixedValue_WrapsRawWithSaturatingOperators()
        {
            FixedValue a = FixedValue.FromInt(2, FixedFormat.Q8);
            FixedValue b = FixedValue.FromRatio(1, 2, FixedFormat.Q8);
            Assert.That((a + b).Raw, Is.EqualTo(640));
            Assert.That((a - b).Raw, Is.EqualTo(384));
            Assert.That(a.Mul(b, FixedFormat.Q8).Raw, Is.EqualTo(256));
            Assert.That(a.Div(b, FixedFormat.Q8).Raw, Is.EqualTo(1024));
            Assert.That(a > b, Is.True);
            Assert.That((FixedValue.FromRaw(int.MaxValue) + a).Raw, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Vectors_UseSaturatingSquaredDistances()
        {
            FixedVec3 a = new FixedVec3(int.MinValue, int.MinValue, int.MinValue);
            FixedVec3 b = new FixedVec3(int.MaxValue, int.MaxValue, int.MaxValue);
            Assert.That(FixedVec3.SquaredDistance(a, b), Is.EqualTo(ulong.MaxValue));
            Assert.That(FixedVec3.IsWithinRadius(new FixedVec3(768, 0, 1024), FixedVec3.Zero, 1280), Is.True, "3-4-5");
            Assert.That(FixedVec3.IsWithinRadius(new FixedVec3(768, 0, 1025), FixedVec3.Zero, 1280), Is.False);
            FixedVec3 scaled = new FixedVec3(768, 0, 1024).WithLength(2560);
            Assert.That(scaled, Is.EqualTo(new FixedVec3(1536, 0, 2048)));
        }

        [Test]
        public void Cone_UsesTheIntegerCosineTable()
        {
            Assert.That(FixedCone.CosHalfQ30(0), Is.EqualTo(1L << 30));
            Assert.That(FixedCone.CosHalfQ30(180), Is.EqualTo(0L));
            Assert.That(FixedCone.CosHalfQ30(360), Is.EqualTo(-(1L << 30)));
            long cos45 = FixedCone.CosHalfQ30(90);
            FixedVec3 aim = new FixedVec3(256, 0, 0);
            Assert.That(FixedCone.Contains(new FixedVec3(768, 0, 512), aim, cos45), Is.True, "33.7 degrees");
            Assert.That(FixedCone.Contains(new FixedVec3(768, 0, 896), aim, cos45), Is.False, "49.4 degrees");
            Assert.That(FixedCone.Contains(new FixedVec3(256, 0, 256), aim, cos45), Is.True, "exactly 45 degrees");
            Assert.That(FixedCone.Contains(FixedVec3.Zero, aim, cos45), Is.True, "the apex");
        }

        [Test]
        public void XorShift_MatchesTheFloatGeneratorStream()
        {
            Neo.Abilities.XorShiftRandom floatRandom = new Neo.Abilities.XorShiftRandom(12345u);
            FixedXorShiftRandom fixedRandom = new FixedXorShiftRandom(12345u);
            for (int i = 0; i < 100; i++)
            {
                float roll = floatRandom.NextFloat();
                int raw = fixedRandom.NextRaw(256);
                Assert.That(raw, Is.EqualTo((int)(roll * 256f)), "same 24 bits, floored to the grid");
                Assert.That(roll < 0.25f, Is.EqualTo(raw < 64), "a chance on the grid rolls the same");
            }
        }
    }
}
