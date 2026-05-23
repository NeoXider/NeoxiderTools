using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public class CheckSpinTests
    {
        private CheckSpin _spin;

        [SetUp]
        public void SetUp()
        {
            _spin = new CheckSpin();
        }

        [Test]
        public void GetEffectiveLines_WithoutLinesData_DefaultUsesFullWindowHorizontalLines()
        {
            LinesData.InnerArray[] lines = _spin.GetEffectiveLines(columnCount: 3, windowRowCount: 3);
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[0].corY, Is.EqualTo(new[] { 0, 0, 0 }));
            Assert.That(lines[1].corY, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(lines[2].corY, Is.EqualTo(new[] { 2, 2, 2 }));
        }

        [Test]
        public void GetEffectiveLines_FallbackRowRange_SingleBottomRow()
        {
            typeof(CheckSpin).GetField("_fallbackWindowRowMin",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_spin, 0);
            typeof(CheckSpin).GetField("_fallbackWindowRowMax",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_spin, 0);

            LinesData.InnerArray[] lines = _spin.GetEffectiveLines(4, 3);
            Assert.That(lines.Length, Is.EqualTo(1));
            Assert.That(lines[0].corY, Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GetEffectiveLines_FallbackRowRange_InclusiveMiddleRows()
        {
            typeof(CheckSpin).GetField("_fallbackWindowRowMin",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_spin, 1);
            typeof(CheckSpin).GetField("_fallbackWindowRowMax",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_spin, 2);

            LinesData.InnerArray[] lines = _spin.GetEffectiveLines(3, 4);
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0].corY, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(lines[1].corY, Is.EqualTo(new[] { 2, 2, 2 }));
        }

        [Test]
        public void GetWinningLines_DetectsFullHorizontalMatch()
        {
            int[,] ids =
            {
                { 5, 1, 2 },
                { 5, 0, 0 },
                { 5, 0, 0 }
            };

            int[] wins = _spin.GetWinningLines(ids, countLine: 1);
            Assert.That(wins.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(wins, Does.Contain(0));
        }

        [Test]
        public void WheelFortune_ResolveSectorIndex_FourSectors()
        {
            Assert.That(WheelFortune.ResolveSectorIndex(0f, 0f, 0f, 4), Is.EqualTo(0));
            float sector = 360f / 4f;
            Assert.That(WheelFortune.ResolveSectorIndex(sector, 0f, 0f, 4), Is.EqualTo(1));
        }
    }
}
