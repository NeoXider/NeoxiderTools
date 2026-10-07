using System;
using Neo.Network;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class NetTrafficMeterTests
    {
        private struct KindA { }

        private struct KindB { }

        private struct KindC { }

        [Test]
        public void Rates_AreComputedPerCompletedWindow()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            for (int i = 0; i < 20; i++)
            {
                meter.Record(typeof(KindA), 100);
            }

            meter.Tick(0.5d);
            Assert.That(meter.BytesPerSecond, Is.EqualTo(0f), "the window has not elapsed yet");

            meter.Tick(1.0d);
            Assert.That(meter.BytesPerSecond, Is.EqualTo(2000f).Within(0.01f));
            Assert.That(meter.MessagesPerSecond, Is.EqualTo(20f).Within(0.01f));
            Assert.That(meter.TotalBytes, Is.EqualTo(2000));
            Assert.That(meter.TotalMessages, Is.EqualTo(20));
        }

        [Test]
        public void ANewWindow_StartsFromZero_AndRatesFallWhenTrafficStops()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 500);
            meter.Tick(1d);
            Assert.That(meter.BytesPerSecond, Is.EqualTo(500f).Within(0.01f));

            meter.Tick(2d);
            Assert.That(meter.BytesPerSecond, Is.EqualTo(0f));
            Assert.That(meter.TotalBytes, Is.EqualTo(500), "totals never reset by themselves");
        }

        [Test]
        public void AWindowLongerThanTheConfiguredOne_NormalisesByTheRealElapsedTime()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 3000);
            meter.Tick(3d);
            Assert.That(meter.BytesPerSecond, Is.EqualTo(1000f).Within(0.01f), "a stalled frame must not inflate the rate");
        }

        [Test]
        public void SendingToSeveralConnections_CountsEveryCopy()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 200, 5);
            meter.Tick(1d);
            Assert.That(meter.BytesPerSecond, Is.EqualTo(1000f).Within(0.01f));
            Assert.That(meter.MessagesPerSecond, Is.EqualTo(5f).Within(0.01f));
        }

        [Test]
        public void PerKindStats_AreTrackedSeparately()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 100, 10);
            meter.Record(typeof(KindB), 50, 2);
            meter.Tick(1d);

            Assert.That(meter.KindCount, Is.EqualTo(2));
            Assert.That(meter.TryGetKind(typeof(KindA), out NetTrafficMeter.KindStat a), Is.True);
            Assert.That(a.BytesPerSecond, Is.EqualTo(1000f).Within(0.01f));
            Assert.That(a.TotalMessages, Is.EqualTo(10));
            Assert.That(meter.TryGetKind(typeof(KindB), out NetTrafficMeter.KindStat b), Is.True);
            Assert.That(b.BytesPerSecond, Is.EqualTo(100f).Within(0.01f));
            Assert.That(meter.TryGetKind(typeof(KindC), out _), Is.False);
            Assert.That(meter.TryGetKind(null, out _), Is.False);
        }

        [Test]
        public void TopKinds_AreSortedByRate_AndBoundedByTheBuffer()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 10);
            meter.Record(typeof(KindB), 900);
            meter.Record(typeof(KindC), 90);
            meter.Tick(1d);

            NetTrafficMeter.KindStat[] all = new NetTrafficMeter.KindStat[5];
            int count = meter.GetTopKinds(all);
            Assert.That(count, Is.EqualTo(3));
            Assert.That(all[0].Kind, Is.EqualTo(typeof(KindB)));
            Assert.That(all[1].Kind, Is.EqualTo(typeof(KindC)));
            Assert.That(all[2].Kind, Is.EqualTo(typeof(KindA)));

            NetTrafficMeter.KindStat[] two = new NetTrafficMeter.KindStat[2];
            Assert.That(meter.GetTopKinds(two), Is.EqualTo(2));
            Assert.That(two[0].Kind, Is.EqualTo(typeof(KindB)));
            Assert.That(two[1].Kind, Is.EqualTo(typeof(KindC)), "the smallest kind did not displace a busier one");

            Assert.That(meter.GetTopKinds(null), Is.EqualTo(0));
            Assert.That(meter.GetTopKinds(Array.Empty<NetTrafficMeter.KindStat>()), Is.EqualTo(0));
        }

        [Test]
        public void ABusierLateKind_DisplacesTheLeastBusyOne()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 10);
            meter.Record(typeof(KindB), 20);
            meter.Record(typeof(KindC), 30);
            meter.Tick(1d);

            NetTrafficMeter.KindStat[] two = new NetTrafficMeter.KindStat[2];
            Assert.That(meter.GetTopKinds(two), Is.EqualTo(2));
            Assert.That(two[0].Kind, Is.EqualTo(typeof(KindC)));
            Assert.That(two[1].Kind, Is.EqualTo(typeof(KindB)));
        }

        [Test]
        public void NullKindAndInvalidInput_OnlyAffectTheTotals()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(null, 100);
            meter.Record(typeof(KindA), -5);
            meter.Record(typeof(KindA), 10, 0);
            Assert.That(meter.KindCount, Is.EqualTo(0));
            Assert.That(meter.TotalBytes, Is.EqualTo(100));
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            meter.Record(typeof(KindA), 100);
            meter.Tick(1d);
            meter.Reset();
            Assert.That(meter.KindCount, Is.EqualTo(0));
            Assert.That(meter.TotalBytes, Is.EqualTo(0));
            Assert.That(meter.BytesPerSecond, Is.EqualTo(0f));
        }

        [Test]
        public void ThousandsOfRecords_DoNotGrowTheKindTable()
        {
            NetTrafficMeter meter = new NetTrafficMeter(1f);
            meter.Tick(0d);
            for (int i = 0; i < 5000; i++)
            {
                meter.Record(i % 2 == 0 ? typeof(KindA) : typeof(KindB), 64);
            }

            Assert.That(meter.KindCount, Is.EqualTo(2));
            Assert.That(meter.TotalMessages, Is.EqualTo(5000));
        }
    }
}
