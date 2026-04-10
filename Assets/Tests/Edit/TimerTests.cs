using Neo.Tools;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class TimerTests
    {
        [Test]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var timer = new Timer(10f, 0.1f, true, true);
            Assert.AreEqual(10f, timer.Duration);
            Assert.AreEqual(0.1f, timer.UpdateInterval);
            Assert.IsTrue(timer.IsLooping);
            Assert.IsTrue(timer.UseUnscaledTime);
            Assert.AreEqual(10f, timer.RemainingTime);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void Duration_NegativeValue_ClampedToZero()
        {
            var timer = new Timer(5f);
            timer.Duration = -10f;
            Assert.AreEqual(0f, timer.Duration);
        }

        [Test]
        public void UpdateInterval_NegativeValue_ClampedToZero()
        {
            var timer = new Timer(5f);
            timer.UpdateInterval = -1f;
            Assert.AreEqual(0f, timer.UpdateInterval);
        }

        [Test]
        public void Progress_AtStart_IsZero()
        {
            var timer = new Timer(10f);
            Assert.AreEqual(0f, timer.Progress, 0.001f);
        }

        [Test]
        public void SetProgress_SetsRemainingTime()
        {
            var timer = new Timer(10f);
            timer.SetProgress(0.5f);
            Assert.AreEqual(5f, timer.RemainingTime, 0.001f);
        }

        [Test]
        public void SetRemainingTime_ClampsToRange()
        {
            var timer = new Timer(10f);
            timer.SetRemainingTime(15f);
            Assert.AreEqual(10f, timer.RemainingTime, 0.001f);

            timer.SetRemainingTime(-5f);
            Assert.AreEqual(0f, timer.RemainingTime, 0.001f);
        }

        [Test]
        public void Reset_RestoresInitialState()
        {
            var timer = new Timer(5f);
            timer.SetProgress(0.8f);
            timer.Reset(10f);

            Assert.AreEqual(10f, timer.Duration);
            Assert.AreEqual(10f, timer.RemainingTime);
        }

        [Test]
        public void AddTime_IncreasesRemainingAndDuration()
        {
            var timer = new Timer(10f);
            timer.AddTime(5f);
            Assert.AreEqual(15f, timer.Duration, 0.001f);
            Assert.AreEqual(15f, timer.RemainingTime, 0.001f);
        }

        [Test]
        public void AddTime_NegativeReduces_ClampedToZero()
        {
            var timer = new Timer(5f);
            timer.AddTime(-100f);
            Assert.AreEqual(0f, timer.RemainingTime, 0.001f);
        }

        [Test]
        public void Dispose_StopsTimer()
        {
            var timer = new Timer(10f);
            timer.Dispose();
            Assert.IsFalse(timer.IsRunning);
        }
    }
}
