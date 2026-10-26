using System;
using System.Collections.Generic;
using Neo.Network;
using Neo.Network.Realtime;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoNetworkRealtimeTests
    {
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void SnapshotBuffer_InvalidStampDoesNotTakeOwnershipOrEvict(double stamp)
        {
            SnapshotBuffer<int> buffer = new SnapshotBuffer<int>(2);
            List<int> evicted = new List<int>();
            buffer.OnEvicted = evicted.Add;

            Assert.That(buffer.TryAdd(stamp, 999), Is.False);
            Assert.That(buffer.Count, Is.Zero);
            Assert.That(buffer.TryAdd(10d, 10), Is.True);
            Assert.That(buffer.TryAdd(11d, 11), Is.True);
            Assert.That(buffer.TryAdd(stamp, 999), Is.False);
            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.NewestTime, Is.EqualTo(11d));
            Assert.That(evicted, Is.Empty);

            Assert.That(buffer.TryAdd(12d, 12), Is.True);
            Assert.That(evicted, Is.EqualTo(new[] { 10 }));
            Assert.That(buffer.TrySample(11.5d, out int from, out int to, out float alpha),
                Is.EqualTo(SnapshotSampleKind.Between));
            Assert.That(from, Is.EqualTo(11));
            Assert.That(to, Is.EqualTo(12));
            Assert.That(alpha, Is.EqualTo(0.5f));
        }

        [TestCase(double.NaN, true)]
        [TestCase(double.PositiveInfinity, true)]
        [TestCase(double.NegativeInfinity, true)]
        [TestCase(double.NaN, false)]
        [TestCase(double.PositiveInfinity, false)]
        [TestCase(double.NegativeInfinity, false)]
        public void Timeline_InvalidObservationLeavesClockAndEstimatesIntact(double stamp, bool remote)
        {
            SnapshotTimeline timeline = new SnapshotTimeline(SnapshotTimelineSettings.Default);
            SnapshotTimeline control = new SnapshotTimeline(SnapshotTimelineSettings.Default);
            timeline.OnFrame(remote ? stamp : 10d, remote ? 0d : stamp);
            Assert.That(timeline.HasTimeline, Is.False);

            timeline.OnFrame(10d, 0d);
            control.OnFrame(10d, 0d);
            timeline.OnFrame(remote ? stamp : 10.1d, remote ? 0.1d : stamp);
            timeline.OnFrame(10.2d, 0.2d);
            control.OnFrame(10.2d, 0.2d);
            timeline.Advance(0.02f);
            control.Advance(0.02f);

            Assert.That(timeline.RenderTimeExact, Is.EqualTo(control.RenderTimeExact));
            Assert.That(timeline.SendIntervalSeconds, Is.EqualTo(control.SendIntervalSeconds));
            Assert.That(timeline.JitterSeconds, Is.EqualTo(control.JitterSeconds));
            Assert.That(timeline.ServerClockRate, Is.EqualTo(control.ServerClockRate));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Timeline_InvalidDeltaDoesNotPoisonFollowingFrames(float delta)
        {
            SnapshotTimeline timeline = new SnapshotTimeline(SnapshotTimelineSettings.Default);
            timeline.OnFrame(10d, 0d);
            double before = timeline.RenderTimeExact;
            timeline.Advance(delta);
            Assert.That(timeline.RenderTimeExact, Is.EqualTo(before));
            timeline.Advance(0.02f);
            Assert.That(timeline.RenderTimeExact, Is.GreaterThan(before));
        }

        [TestCase(int.MaxValue, int.MaxValue, 1)]
        [TestCase(int.MaxValue, 2, 1073741824)]
        [TestCase(int.MaxValue, 65535, 32769)]
        [TestCase(1025, 256, 5)]
        [TestCase(0, 256, 0)]
        public void FragmentCount_LargeLengthsDoNotWrap(int length, int chunk, int expected)
        {
            Assert.That(NetFragmentation.FragmentCount(length, chunk), Is.EqualTo(expected));
        }

        [TestCase(int.MinValue, 256)]
        [TestCase(0, 256)]
        [TestCase(280, 256)]
        [TestCase(281, 257)]
        [TestCase(int.MaxValue, int.MaxValue - NetFragmentation.OverheadBytes)]
        public void FragmentThreshold_DoesNotWrap(int threshold, int expected)
        {
            Assert.That(NetFragmentation.ChunkSizeForThreshold(threshold), Is.EqualTo(expected));
        }

        [TestCase(int.MaxValue, int.MaxValue)]
        [TestCase(10000000, int.MaxValue)]
        [TestCase(256, 65280)]
        [TestCase(-1, 0)]
        public void FrameCapacity_SaturatesAtRepresentableLength(int chunk, int expected)
        {
            Assert.That(NetFragmentation.MaxFrameBytes(chunk), Is.EqualTo(expected));
        }

        [Test]
        public void StartupDefaults_PublicDefaultContainsTheDocumentedAddress()
        {
            NeoStartupDefaults defaults = NeoStartupDefaults.Default;
            NeoStartupIntent intent = NeoStartupCommandLine.Parse(Array.Empty<string>(), defaults);
            Assert.That(defaults.Address, Is.EqualTo(NeoStartupIntent.DefaultAddress));
            Assert.That(defaults.DisplayName, Is.EqualTo(string.Empty));
            Assert.That(intent.Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(intent.Address, Is.EqualTo(defaults.Address));
        }
    }
}
