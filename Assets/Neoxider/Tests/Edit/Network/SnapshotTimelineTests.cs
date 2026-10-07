using System;
using Neo.Network.Realtime;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     The client render clock must glide between frames instead of stepping with them, converge on a lag that
    ///     follows the snapshot interval and the arrival jitter, and snap rather than fast-forward after a stall.
    /// </summary>
    [TestFixture]
    public sealed class SnapshotTimelineTests
    {
        private const float Interval = 0.05f; // 20 Hz
        private const float Frame = 1f / 60f; // a 60 fps client

        private static SnapshotTimeline NewTimeline()
        {
            return new SnapshotTimeline(SnapshotTimelineSettings.Default);
        }

        // Feeds a perfectly regular link for the given number of seconds and returns the local clock at the end.
        private static float RunSteady(SnapshotTimeline timeline, float seconds, float startLocal = 0f,
            float serverOffset = 100f)
        {
            float local = startLocal;
            float nextFrameAt = local;
            float end = local + seconds;
            while (local < end)
            {
                if (local >= nextFrameAt)
                {
                    timeline.OnFrame(local + serverOffset, local);
                    nextFrameAt += Interval;
                }

                timeline.Advance(Frame);
                local += Frame;
            }

            return local;
        }

        // A link whose authority clock runs at clockRate(t) of real time (hit-stop, slowed simulation).
        private static void RunWithServerRate(SnapshotTimeline timeline, float seconds, float measureAfter,
            Func<float, float> clockRate, out float lowest, out float mean)
        {
            float local = 0f;
            double server = 100.0;
            float nextFrameAt = 0f;
            float lowestLag = float.MaxValue;
            double lagSum = 0d;
            int lagCount = 0;
            while (local < seconds)
            {
                if (local >= nextFrameAt)
                {
                    timeline.OnFrame(server, local);
                    nextFrameAt += Interval;
                }

                timeline.Advance(Frame);
                local += Frame;
                server += Frame * clockRate(local);
                if (local > measureAfter)
                {
                    if (timeline.LagSeconds < lowestLag)
                    {
                        lowestLag = timeline.LagSeconds;
                    }

                    lagSum += timeline.LagSeconds;
                    lagCount++;
                }
            }

            lowest = lowestLag;
            mean = (float)(lagSum / lagCount);
        }

        [Test]
        public void ASlowedAuthorityClock_DoesNotPushTheRenderClockAheadOfTheFrames()
        {
            // The first host-loopback run of the game this was extracted from: hit-stop made the host's clock run at
            // ~70% of real time, the render clock assumed 1:1, slid 0.2-0.4 s in FRONT of the newest frame and
            // interpolation degraded to "hold the newest position".
            SnapshotTimeline timeline = NewTimeline();
            RunWithServerRate(timeline, 16f, 8f, _ => 0.7f, out float lowest, out float mean);

            Assert.That(timeline.ServerClockRate, Is.EqualTo(0.7f).Within(0.03f));
            Assert.That(lowest, Is.GreaterThanOrEqualTo(-0.02f), "the clock must stay behind the newest frame");
            Assert.That(mean, Is.InRange(0.02f, 0.2f));
        }

        [Test]
        public void RepeatedHitStops_AreAbsorbed()
        {
            SnapshotTimeline timeline = NewTimeline();
            RunWithServerRate(timeline, 16f, 6f, t => t % 1f < 0.7f ? 1f : 0.08f, out float lowest, out float mean);

            Assert.That(timeline.ServerClockRate, Is.InRange(0.6f, 0.95f));
            Assert.That(lowest, Is.GreaterThanOrEqualTo(-0.3f));
            Assert.That(mean, Is.GreaterThanOrEqualTo(0f), "on average the clock still trails the frames");
            Assert.That(timeline.BufferSeconds, Is.LessThan(0.3f),
                "stop-and-go must not read as jitter and balloon the buffer");
        }

        [Test]
        public void ANormalLink_MeasuresARateOfOne()
        {
            SnapshotTimeline timeline = NewTimeline();
            RunSteady(timeline, 6f);
            Assert.That(timeline.ServerClockRate, Is.EqualTo(1f).Within(0.03f));
        }

        [Test]
        public void BeforeTheFirstFrame_ThereIsNoTimeline()
        {
            SnapshotTimeline timeline = NewTimeline();
            Assert.That(timeline.HasTimeline, Is.False);
            timeline.Advance(0.1f);
            Assert.That(timeline.HasTimeline, Is.False);
        }

        [Test]
        public void FirstFrame_PlacesTheClockOneBufferBehindIt()
        {
            SnapshotTimeline timeline = NewTimeline();
            timeline.OnFrame(50f, 1f);
            Assert.That(timeline.HasTimeline, Is.True);
            Assert.That(timeline.RenderTime, Is.EqualTo(50f - timeline.BufferSeconds).Within(0.0001f));
            Assert.That(timeline.LagSeconds, Is.EqualTo(timeline.BufferSeconds).Within(0.0001f));
        }

        [Test]
        public void RenderTime_GlidesBetweenFrames_NotInSteps()
        {
            SnapshotTimeline timeline = NewTimeline();
            RunSteady(timeline, 1f);
            float before = timeline.RenderTime;
            timeline.Advance(Frame);
            float after = timeline.RenderTime;
            Assert.That(after, Is.GreaterThan(before),
                "with no frame arriving the clock still moves: what the staircase clock could not do");
            Assert.That(after - before, Is.EqualTo(Frame).Within(Frame * 0.2f),
                "about real time, steered a few percent at most");
        }

        [Test]
        public void SteadyLink_ConvergesOnTheTargetLag()
        {
            SnapshotTimeline timeline = NewTimeline();
            RunSteady(timeline, 6f);

            float target = timeline.BufferSeconds;
            Assert.That(timeline.SendIntervalSeconds, Is.EqualTo(Interval).Within(0.002f));
            Assert.That(target, Is.EqualTo(Interval * 1.5f).Within(0.01f), "1.5 intervals of lag on a clean link");
            Assert.That(timeline.LagSeconds, Is.InRange(target - Interval - 0.01f, target + Interval + 0.01f),
                "the lag saw-tooths by one interval around its target as frames arrive");
        }

        [Test]
        public void RenderTime_NeverRunsAheadOfTheNewestFrame()
        {
            SnapshotTimeline timeline = NewTimeline();
            float local = 0f;
            float nextFrameAt = 0f;
            for (int i = 0; i < 600; i++)
            {
                if (local >= nextFrameAt)
                {
                    timeline.OnFrame(local + 10f, local);
                    nextFrameAt += Interval;
                }

                timeline.Advance(Frame);
                local += Frame;
                Assert.That(timeline.LagSeconds, Is.GreaterThanOrEqualTo(0f),
                    "the clock must trail the newest frame, never lead it");
            }
        }

        [Test]
        public void ARoughLink_WidensTheBuffer()
        {
            SnapshotTimeline clean = NewTimeline();
            RunSteady(clean, 4f);

            SnapshotTimeline rough = NewTimeline();
            float local = 0f;
            float server = 100f;
            Random rng = new Random(7);
            for (int i = 0; i < 120; i++)
            {
                server += Interval;
                local += Interval + ((float)rng.NextDouble() - 0.5f) * 0.08f;
                rough.OnFrame(server, local);
            }

            Assert.That(rough.JitterSeconds, Is.GreaterThan(clean.JitterSeconds + 0.005f));
            Assert.That(rough.BufferSeconds, Is.GreaterThan(clean.BufferSeconds),
                "smoothness is bought with a little latency");
            Assert.That(rough.BufferSeconds, Is.LessThanOrEqualTo(SnapshotTimelineSettings.Default.MaxBufferSeconds));
        }

        [Test]
        public void ALongStall_SnapsTheClockInsteadOfFastForwarding()
        {
            SnapshotTimeline timeline = NewTimeline();
            RunSteady(timeline, 2f);

            timeline.OnFrame(timeline.ServerNow + 10f, 12f);
            timeline.Advance(Frame);
            Assert.That(timeline.LagSeconds, Is.EqualTo(timeline.BufferSeconds).Within(0.02f),
                "snapped onto the target lag in one step");
        }

        [Test]
        public void SpeedAdjustment_StaysWithinItsBand()
        {
            SnapshotTimeline timeline = NewTimeline();
            timeline.OnFrame(100f, 0f);
            float before = timeline.RenderTime;
            timeline.Advance(1f);
            float advanced = timeline.RenderTime - before;
            float band = SnapshotTimelineSettings.Default.MaxSpeedAdjust;
            Assert.That(advanced, Is.InRange(1f - band - 0.001f, 1f + band + 0.001f));
        }

        [Test]
        public void OutOfOrderStamps_AreIgnoredByTheClock()
        {
            SnapshotTimeline timeline = NewTimeline();
            timeline.OnFrame(10f, 0f);
            timeline.OnFrame(10.05f, 0.05f);
            float interval = timeline.SendIntervalSeconds;
            timeline.OnFrame(10.01f, 0.06f);
            Assert.That(timeline.SendIntervalSeconds, Is.EqualTo(interval).Within(0.0001f));
        }

        [Test]
        public void Reset_ForgetsTheTimeline()
        {
            SnapshotTimeline timeline = NewTimeline();
            RunSteady(timeline, 1f);
            timeline.Reset();
            Assert.That(timeline.HasTimeline, Is.False);
            Assert.That(timeline.RenderTime, Is.EqualTo(0f));
        }

        [Test]
        public void DoublePrecision_KeepsAMillisecondAfterTenHours()
        {
            SnapshotTimeline timeline = NewTimeline();
            const double start = 36000d;
            double local = 0d;
            double nextFrameAt = 0d;
            for (int i = 0; i < 600; i++)
            {
                if (local >= nextFrameAt)
                {
                    timeline.OnFrame(start + local, local);
                    nextFrameAt += Interval;
                }

                timeline.Advance(Frame);
                local += Frame;
            }

            Assert.That(timeline.LagSeconds, Is.InRange(0.03f, 0.2f));
            double stepBefore = timeline.RenderTimeExact;
            timeline.Advance(Frame);
            Assert.That(timeline.RenderTimeExact - stepBefore, Is.EqualTo(Frame).Within(Frame * 0.25),
                "a float clock at 36000 s has 4 ms resolution and would quantise this step");
        }

        [Test]
        public void ForSendRate_UsesThatRateAsTheInitialIntervalAssumption()
        {
            SnapshotTimeline timeline = new SnapshotTimeline(SnapshotTimelineSettings.ForSendRate(30f));
            Assert.That(timeline.SendIntervalSeconds, Is.EqualTo(1f / 30f).Within(0.0001f));
        }

        [Test]
        public void DrivingASnapshotBuffer_WithTheTimeline_ProducesSmoothMotion()
        {
            // Sampled once per render frame against the free-running clock, a body at constant speed must advance
            // on every rendered frame, not only on the frames a snapshot landed.
            SnapshotTimeline timeline = NewTimeline();
            SnapshotBuffer<float> buffer = new SnapshotBuffer<float>(8);
            const float speed = 10f;
            float local = 0f;
            float nextFrameAt = 0f;
            int movedFrames = 0;
            int sampledFrames = 0;
            float lastX = float.NaN;

            for (int i = 0; i < 360; i++)
            {
                if (local >= nextFrameAt)
                {
                    float serverTime = local + 5f;
                    buffer.TryAdd(serverTime, speed * serverTime);
                    timeline.OnFrame(serverTime, local);
                    nextFrameAt += Interval;
                }

                timeline.Advance(Frame);
                local += Frame;

                if (local > 3f
                    && buffer.TrySample(timeline.RenderTimeExact, out float from, out float to, out float alpha)
                    != SnapshotSampleKind.None)
                {
                    float x = NetInterpolation.Lerp(from, to, alpha);
                    sampledFrames++;
                    if (!float.IsNaN(lastX) && x > lastX + 0.0001f)
                    {
                        movedFrames++;
                    }

                    lastX = x;
                }
            }

            Assert.That(sampledFrames, Is.GreaterThan(100));
            Assert.That(movedFrames / (float)(sampledFrames - 1), Is.GreaterThan(0.95f),
                "the body must move on (almost) every rendered frame");
        }
    }
}
