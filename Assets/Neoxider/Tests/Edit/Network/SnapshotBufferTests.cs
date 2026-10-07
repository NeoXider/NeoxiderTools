using System.Collections.Generic;
using Neo.Network.Realtime;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class SnapshotBufferTests
    {
        private static SnapshotBuffer<float> Filled(int capacity, params double[] times)
        {
            SnapshotBuffer<float> buffer = new SnapshotBuffer<float>(capacity);
            for (int i = 0; i < times.Length; i++)
            {
                Assert.That(buffer.TryAdd(times[i], (float)times[i] * 10f), Is.True);
            }

            return buffer;
        }

        [Test]
        public void Empty_SamplesNothing()
        {
            SnapshotBuffer<float> buffer = new SnapshotBuffer<float>(4);
            SnapshotSampleKind kind = buffer.TrySample(1d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.None));
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.TryGetNewest(out _), Is.False);
            Assert.That(buffer.NewestTime, Is.EqualTo(double.NegativeInfinity));
        }

        [Test]
        public void CapacityBelowTwo_IsRaisedToTwo()
        {
            Assert.That(new SnapshotBuffer<int>(0).Capacity, Is.EqualTo(2));
        }

        [Test]
        public void BetweenTwoFrames_BlendsByAlpha()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d, 3d);
            SnapshotSampleKind kind = buffer.TrySample(2.25d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.Between));
            Assert.That(from, Is.EqualTo(20f));
            Assert.That(to, Is.EqualTo(30f));
            Assert.That(alpha, Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void ExactlyOnAFrame_ReturnsThatFrameAtAlphaZero()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d, 3d);
            SnapshotSampleKind kind = buffer.TrySample(2d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.Between));
            Assert.That(from, Is.EqualTo(20f));
            Assert.That(to, Is.EqualTo(30f));
            Assert.That(alpha, Is.EqualTo(0f));
        }

        [Test]
        public void ExactlyOnTheOldestFrame_BlendsTowardTheSecond()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d);
            SnapshotSampleKind kind = buffer.TrySample(1d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.Between));
            Assert.That(from, Is.EqualTo(10f));
            Assert.That(to, Is.EqualTo(20f));
            Assert.That(alpha, Is.EqualTo(0f));
        }

        [Test]
        public void BeforeTheOldest_HoldsTheOldest()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d);
            SnapshotSampleKind kind = buffer.TrySample(0.5d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.BeforeOldest));
            Assert.That(from, Is.EqualTo(10f));
            Assert.That(to, Is.EqualTo(10f));
            Assert.That(alpha, Is.EqualTo(0f));
        }

        [Test]
        public void PastTheNewest_HoldsTheNewest_NoExtrapolation()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d);
            SnapshotSampleKind kind = buffer.TrySample(9d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.AfterNewest));
            Assert.That(from, Is.EqualTo(20f));
            Assert.That(to, Is.EqualTo(20f));
            Assert.That(alpha, Is.EqualTo(1f));
        }

        [Test]
        public void SingleFrame_IsHeldFromBothSides()
        {
            SnapshotBuffer<float> buffer = Filled(4, 5d);
            Assert.That(buffer.TrySample(4d, out _, out _, out _), Is.EqualTo(SnapshotSampleKind.BeforeOldest));
            Assert.That(buffer.TrySample(5d, out _, out _, out _), Is.EqualTo(SnapshotSampleKind.AfterNewest));
            Assert.That(buffer.TrySample(6d, out _, out _, out _), Is.EqualTo(SnapshotSampleKind.AfterNewest));
        }

        [Test]
        public void StaleOrDuplicateStamps_AreRejected()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d);
            Assert.That(buffer.TryAdd(2d, 99f), Is.False, "same stamp");
            Assert.That(buffer.TryAdd(1.5d, 99f), Is.False, "older stamp (reordered packet)");
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.TryGetNewest(out float newest), Is.True);
            Assert.That(newest, Is.EqualTo(20f));
        }

        [Test]
        public void WhenFull_TheOldestIsEvicted_AndReported()
        {
            SnapshotBuffer<float> buffer = new SnapshotBuffer<float>(3);
            List<float> evicted = new List<float>();
            buffer.OnEvicted = evicted.Add;
            for (int i = 1; i <= 5; i++)
            {
                buffer.TryAdd(i, i * 10f);
            }

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(evicted, Is.EqualTo(new[] { 10f, 20f }));
            Assert.That(buffer.OldestTime, Is.EqualTo(3d));
            Assert.That(buffer.NewestTime, Is.EqualTo(5d));
            Assert.That(buffer.GetFrame(0), Is.EqualTo(30f));
            Assert.That(buffer.GetFrame(2), Is.EqualTo(50f));
            Assert.That(buffer.GetTime(1), Is.EqualTo(4d));
        }

        [Test]
        public void Wraparound_KeepsSamplingCorrect()
        {
            SnapshotBuffer<float> buffer = new SnapshotBuffer<float>(4);
            for (int i = 1; i <= 11; i++)
            {
                buffer.TryAdd(i, i * 10f);
            }

            SnapshotSampleKind kind = buffer.TrySample(9.5d, out float from, out float to, out float alpha);
            Assert.That(kind, Is.EqualTo(SnapshotSampleKind.Between));
            Assert.That(from, Is.EqualTo(90f));
            Assert.That(to, Is.EqualTo(100f));
            Assert.That(alpha, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Clear_ReportsEveryFrameAndEmptiesTheBuffer()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d, 2d, 3d);
            List<float> evicted = new List<float>();
            buffer.OnEvicted = evicted.Add;
            buffer.Clear();
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(evicted, Is.EqualTo(new[] { 10f, 20f, 30f }));
            Assert.That(buffer.TryAdd(0.5d, 5f), Is.True, "after Clear any stamp is accepted again");
        }

        [Test]
        public void GetFrame_OutOfRange_Throws()
        {
            SnapshotBuffer<float> buffer = Filled(4, 1d);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => buffer.GetFrame(1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => buffer.GetTime(-1));
        }

        [Test]
        public void LerpAngle_TakesTheShortWayAround()
        {
            Assert.That(NetInterpolation.LerpAngleDegrees(350f, 10f, 0.5f), Is.EqualTo(360f).Within(1e-3f));
            Assert.That(NetInterpolation.LerpAngleDegrees(10f, 350f, 0.5f), Is.EqualTo(0f).Within(1e-3f));
            Assert.That(NetInterpolation.LerpAngleRadians(0.1f, 6.2f, 0.5f), Is.EqualTo(0.0084f).Within(2e-3f));
            Assert.That(NetInterpolation.Lerp(0f, 10f, 2f), Is.EqualTo(10f), "alpha is clamped");
            Assert.That(NetInterpolation.Lerp(0f, 10f, -1f), Is.EqualTo(0f));
        }
    }
}
