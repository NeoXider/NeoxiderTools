using Neo.Bonus;
using NUnit.Framework;
using System.Reflection;
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
            LinesData.InnerArray[] lines = _spin.GetEffectiveLines(3, 3);
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[0].corY, Is.EqualTo(new[] { 0, 0, 0 }));
            Assert.That(lines[1].corY, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(lines[2].corY, Is.EqualTo(new[] { 2, 2, 2 }));
        }

        [Test]
        public void GetEffectiveLines_FallbackRowRange_SingleBottomRow()
        {
            typeof(CheckSpin).GetField("_fallbackWindowRowMin",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_spin, 0);
            typeof(CheckSpin).GetField("_fallbackWindowRowMax",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_spin, 0);

            LinesData.InnerArray[] lines = _spin.GetEffectiveLines(4, 3);
            Assert.That(lines.Length, Is.EqualTo(1));
            Assert.That(lines[0].corY, Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GetEffectiveLines_FallbackRowRange_InclusiveMiddleRows()
        {
            typeof(CheckSpin).GetField("_fallbackWindowRowMin",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_spin, 1);
            typeof(CheckSpin).GetField("_fallbackWindowRowMax",
                    BindingFlags.NonPublic | BindingFlags.Instance)
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

            int[] wins = _spin.GetWinningLines(ids, 1);
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

        [Test]
        public void SpinController_ChanseWinAlias_ClampsAndMirrorsChanceWin()
        {
            GameObject go = new("SpinControllerTest");
            try
            {
                SpinController controller = go.AddComponent<SpinController>();

                controller.ChanceWin = 1.5f;
                Assert.That(controller.ChanceWin, Is.EqualTo(1f));

#pragma warning disable CS0618
                controller.chanseWin = -1f;
                Assert.That(controller.ChanceWin, Is.EqualTo(0f));
                Assert.That(controller.chanseWin, Is.EqualTo(controller.ChanceWin));
#pragma warning restore CS0618
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpinController_PaidSpinWithoutMoneySpend_IsRejected()
        {
            GameObject go = new("SpinControllerPaidWithoutMoney");
            try
            {
                SpinController controller = go.AddComponent<SpinController>();
                SetSpinPrice(controller, 10);

                Assert.That(controller.TryPayForSpin(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpinController_PaidSpinWithMoneySpend_Spends()
        {
            GameObject go = new("SpinControllerPaidWithMoney");
            try
            {
                SpinController controller = go.AddComponent<SpinController>();
                var money = new FakeMoneySpend();
                controller.moneySpend = money;
                SetSpinPrice(controller, 15);

                Assert.That(controller.TryPayForSpin(), Is.True);
                Assert.That(money.SpendCalls, Is.EqualTo(1));
                Assert.That(money.LastAmount, Is.EqualTo(15f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpinController_FreeSpinWithoutMoneySpend_IsAllowed()
        {
            GameObject go = new("SpinControllerFreeWithoutMoney");
            try
            {
                SpinController controller = go.AddComponent<SpinController>();
                SetSpinPrice(controller, 0);

                Assert.That(controller.TryPayForSpin(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void SetSpinPrice(SpinController controller, int price)
        {
            typeof(SpinController)
                .GetField("price", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(controller, price);
        }

        private sealed class FakeMoneySpend : IMoneySpend
        {
            public int SpendCalls { get; private set; }
            public float LastAmount { get; private set; }

            public bool Spend(float count)
            {
                SpendCalls++;
                LastAmount = count;
                return true;
            }
        }
    }
}
