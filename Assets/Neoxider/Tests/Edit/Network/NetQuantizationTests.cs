using System;
using Neo.Network.Realtime;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class NetQuantizationTests
    {
        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(-1f)]
        [TestCase(123.456f)]
        [TestCase(-987.654f)]
        [TestCase(1023.9f)]
        public void Position_RoundTripsWithinHalfAStep(float value)
        {
            float back = NetQuantization.UnpackPosition(NetQuantization.PackPosition(value));
            Assert.That(back, Is.EqualTo(value).Within(0.5f / NetQuantization.PositionStepsPerUnit + 1e-5f));
        }

        [Test]
        public void Position_SaturatesInsteadOfWrapping()
        {
            Assert.That(NetQuantization.PackPosition(5000f), Is.EqualTo(short.MaxValue));
            Assert.That(NetQuantization.PackPosition(-5000f), Is.EqualTo(short.MinValue));
            Assert.That(NetQuantization.PackPosition(float.PositiveInfinity), Is.EqualTo(short.MaxValue));
            Assert.That(NetQuantization.UnpackPosition(NetQuantization.PackPosition(5000f)),
                Is.EqualTo(NetQuantization.MaxPackedPosition).Within(0.04f), "pins to the edge, never teleports across the map");
        }

        [Test]
        public void Position_NaNPacksToZero()
        {
            Assert.That(NetQuantization.PackPosition(float.NaN), Is.EqualTo((short)0));
        }

        [Test]
        public void FitsPosition_FlagsAWorldThatOutgrewTheRange()
        {
            Assert.That(NetQuantization.FitsPosition(1000f), Is.True);
            Assert.That(NetQuantization.FitsPosition(-1000f), Is.True);
            Assert.That(NetQuantization.FitsPosition(1100f), Is.False);
            Assert.That(NetQuantization.FitsPosition(-1100f), Is.False);
        }

        [Test]
        public void ExplicitDensity_ScalesTheRange()
        {
            Assert.That(NetQuantization.MaxShort(32f), Is.EqualTo(NetQuantization.MaxPackedPosition));
            Assert.That(NetQuantization.MaxShort(8f), Is.EqualTo(short.MaxValue / 8f));
            Assert.That(NetQuantization.FitsShort(3000f, 8f), Is.True);
            Assert.That(NetQuantization.FitsShort(5000f, 8f), Is.False);
            short packed = NetQuantization.PackShort(12.34f, 100f);
            Assert.That(NetQuantization.UnpackShort(packed, 100f), Is.EqualTo(12.34f).Within(0.005f));
            Assert.That(NetQuantization.MaxShort(0f), Is.EqualTo(0f), "a non-positive density is not a divide by zero");
            Assert.That(NetQuantization.UnpackShort(100, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void UnsignedPackers_ClampNegativesToZeroAndSaturateAtTheTop()
        {
            Assert.That(NetQuantization.PackScale(-1f), Is.EqualTo((ushort)0));
            Assert.That(NetQuantization.PackScale(1e6f), Is.EqualTo(ushort.MaxValue));
            Assert.That(NetQuantization.UnpackScale(NetQuantization.PackScale(1.5f)), Is.EqualTo(1.5f).Within(0.01f));
            Assert.That(NetQuantization.UnpackSpeed(NetQuantization.PackSpeed(6.25f)), Is.EqualTo(6.25f).Within(0.003f));
            Assert.That(NetQuantization.UnpackSeconds(NetQuantization.PackSeconds(3.3f)), Is.EqualTo(3.3f).Within(0.02f));
        }

        [Test]
        public void Fraction_UsesTheFullByte()
        {
            Assert.That(NetQuantization.PackFraction(0f), Is.EqualTo((byte)0));
            Assert.That(NetQuantization.PackFraction(1f), Is.EqualTo((byte)255));
            Assert.That(NetQuantization.PackFraction(2f), Is.EqualTo((byte)255));
            Assert.That(NetQuantization.PackFraction(-1f), Is.EqualTo((byte)0));
            Assert.That(NetQuantization.UnpackFraction(NetQuantization.PackFraction(0.5f)), Is.EqualTo(0.5f).Within(0.003f));
        }

        [Test]
        public void SignedUnit_HasAnExactZero()
        {
            Assert.That(NetQuantization.PackSignedUnit(0f), Is.EqualTo((byte)128));
            Assert.That(NetQuantization.UnpackSignedUnit(128), Is.EqualTo(0f));
            Assert.That(NetQuantization.UnpackSignedUnit(NetQuantization.PackSignedUnit(1f)), Is.EqualTo(1f).Within(0.01f));
            Assert.That(NetQuantization.UnpackSignedUnit(NetQuantization.PackSignedUnit(-1f)), Is.EqualTo(-1f).Within(0.01f));
            Assert.That(NetQuantization.UnpackSignedUnit(NetQuantization.PackSignedUnit(0.5f)), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(NetQuantization.PackSignedUnit(50f), Is.EqualTo((byte)255), "saturates");
        }

        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(3.14159f)]
        [TestCase(6f)]
        [TestCase(-1f)]
        [TestCase(20f)]
        public void Angle_RoundTripsOnTheCircle(float radians)
        {
            float twoPi = (float)(Math.PI * 2d);
            float expected = radians - (float)Math.Floor(radians / twoPi) * twoPi;
            float back = NetQuantization.UnpackAngle(NetQuantization.PackAngle(radians));
            float diff = Math.Abs(back - expected);
            diff = Math.Min(diff, twoPi - diff);
            Assert.That(diff, Is.LessThan(twoPi / 65536f * 1.01f));
        }

        [Test]
        public void AngleByte_WrapsAndStaysOnTheCircle()
        {
            float twoPi = (float)(Math.PI * 2d);
            Assert.That(NetQuantization.PackAngleByte(0f), Is.EqualTo((byte)0));
            Assert.That(NetQuantization.PackAngleByte(twoPi), Is.EqualTo((byte)0), "a full turn is zero");
            Assert.That(NetQuantization.PackAngleByte(twoPi * 0.5f), Is.EqualTo((byte)128));
            Assert.That(NetQuantization.UnpackAngleByte(64), Is.EqualTo(twoPi * 0.25f).Within(1e-4f));
            Assert.That(NetQuantization.PackAngleByte(float.NaN), Is.EqualTo((byte)0));
            Assert.That(NetQuantization.PackAngle(float.PositiveInfinity), Is.EqualTo((ushort)0));
        }

        [Test]
        public void HitPoints_RoundUp_SoALiveEntityNeverReadsAsDead()
        {
            Assert.That(NetQuantization.PackHitPoints(0.3f), Is.EqualTo(1u));
            Assert.That(NetQuantization.PackHitPoints(7.0f), Is.EqualTo(7u));
            Assert.That(NetQuantization.PackHitPoints(7.01f), Is.EqualTo(8u));
            Assert.That(NetQuantization.PackHitPoints(0f), Is.EqualTo(0u));
            Assert.That(NetQuantization.PackHitPoints(-5f), Is.EqualTo(0u));
            Assert.That(NetQuantization.PackHitPoints(float.NaN), Is.EqualTo(0u));
            Assert.That(NetQuantization.PackHitPoints(1e20f), Is.EqualTo(uint.MaxValue));
        }

        [Test]
        public void MaxHitPoints_RoundToNearest_AtLeastOneForARealEntity()
        {
            Assert.That(NetQuantization.PackMaxHitPoints(0.2f), Is.EqualTo(1u));
            Assert.That(NetQuantization.PackMaxHitPoints(10.4f), Is.EqualTo(10u));
            Assert.That(NetQuantization.PackMaxHitPoints(10.5f), Is.EqualTo(11u));
            Assert.That(NetQuantization.PackMaxHitPoints(0f), Is.EqualTo(0u));
            Assert.That(NetQuantization.PackMaxHitPoints(float.NaN), Is.EqualTo(0u));
        }
    }
}
