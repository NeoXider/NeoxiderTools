using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Guards the pure sector-resolution geometry (<see cref="WheelFortune.ResolveSectorIndex"/>)
    ///     against known angles and the out-of-range safety of <see cref="WheelFortune.GetPrize"/>.
    /// </summary>
    [TestFixture]
    public class WheelFortuneTests
    {
        [Test]
        public void ResolveSectorIndex_ZeroAngles_ReturnsFirstSector()
        {
            Assert.AreEqual(0, WheelFortune.ResolveSectorIndex(0f, 0f, 0f, 8));
        }

        [Test]
        public void ResolveSectorIndex_RotatesThroughSectors()
        {
            // WHY: 8 sectors -> 45 deg each; the arrow points at sector floor((angle+22.5)/45).
            Assert.AreEqual(1, WheelFortune.ResolveSectorIndex(45f, 0f, 0f, 8));
            Assert.AreEqual(2, WheelFortune.ResolveSectorIndex(90f, 0f, 0f, 8));
            Assert.AreEqual(0, WheelFortune.ResolveSectorIndex(360f, 0f, 0f, 8), "Full turn wraps to 0.");
        }

        [Test]
        public void ResolveSectorIndex_HalfSectorBoundary_RoundsToNext()
        {
            // WHY: exactly on the +half-sector edge rounds up into the next sector.
            Assert.AreEqual(1, WheelFortune.ResolveSectorIndex(22.5f, 0f, 0f, 8));
        }

        [Test]
        public void ResolveSectorIndex_ArrowOffsetShiftsResult()
        {
            // WHY: rotating the arrow by one sector cancels a one-sector wheel rotation.
            Assert.AreEqual(0, WheelFortune.ResolveSectorIndex(45f, 45f, 0f, 8));
        }

        [Test]
        public void ResolveSectorIndex_NonPositiveItemCount_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, WheelFortune.ResolveSectorIndex(0f, 0f, 0f, 0));
            Assert.AreEqual(-1, WheelFortune.ResolveSectorIndex(0f, 0f, 0f, -3));
        }

        [Test]
        public void GetPrize_OutOfRangeOrEmpty_ReturnsNullInsteadOfThrowing()
        {
            var go = new GameObject("Wheel");
            try
            {
                WheelFortune wheel = go.AddComponent<WheelFortune>();

                // items default to null/empty -> must not throw.
                Assert.IsNull(wheel.GetPrize(0));
                Assert.IsNull(wheel.GetPrize(-1));
                Assert.IsNull(wheel.GetPrize(999));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
