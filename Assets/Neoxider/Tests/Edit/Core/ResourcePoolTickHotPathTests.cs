using System;
using Neo.Core.Resources;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Hot-path contracts of <see cref="ResourcePoolModel.Tick" />: a warm tick does not allocate, and a pool removed
    ///     by a callback during the tick is skipped (the registration-version fallback).
    /// </summary>
    [TestFixture]
    public class ResourcePoolTickHotPathTests
    {
        [Test]
        public void Tick_WarmRegeneratingAndFullPools_DoesNotAllocate()
        {
            ResourcePoolModel model = CreateModel(8, 4);
            int changes = 0;
            model.OnResourceChanged += (id, current, max) => changes++;
            model.Tick(0.25f);
            int warmChanges = changes;
            Action ticks = () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    model.Tick(0.25f);
                }
            };

            GcAlloc.AssertNone(ticks, "A warm tick reuses its registration snapshot and raises changes without boxing.");

            Assert.That(changes - warmChanges, Is.GreaterThanOrEqualTo(4 * 1000),
                "Every regenerating pool changes on every tick; full pools stay silent.");
            Assert.That(model.GetCurrent("pool7"), Is.EqualTo(100f));
        }

        [Test]
        public void Tick_CallbackRemovesTheOtherSnapshotPool_SkipsItWithoutTouchingIt()
        {
            ResourcePoolModel model = new();
            model.AddPool("a", Regenerating(50f, 100f));
            model.AddPool("b", Regenerating(50f, 100f));
            string removed = null;
            int changes = 0;
            model.OnResourceChanged += (id, current, max) =>
            {
                changes++;
                if (removed == null)
                {
                    // WHY: pick the pool that has not ticked yet; dictionary enumeration order is irrelevant.
                    removed = string.Equals(id, "a", StringComparison.OrdinalIgnoreCase) ? "b" : "a";
                    model.RemovePool(removed);
                }
            };

            model.Tick(1f);

            Assert.That(changes, Is.EqualTo(1), "The removed pool must not regenerate from the old snapshot.");
            Assert.That(model.GetCurrent(removed), Is.Zero);
            Assert.That(model.GetMax(removed), Is.Zero);

            model.AddPool(removed, Regenerating(50f, 100f));
            model.Tick(1f);

            Assert.That(model.GetCurrent(removed), Is.EqualTo(60f),
                "A fresh registration starts with zeroed timers and regenerates normally on the next tick.");
        }

        [Test]
        public void Tick_ReAddedWithTheSameEntry_StartsFreshTimers()
        {
            ResourcePoolModel model = new();
            ResourcePoolEntry entry = Regenerating(10f, 100f);
            entry.RegenInterval = 1f;
            model.AddPool("hp", entry);
            model.Tick(0.75f);

            model.AddPool("HP", entry);
            model.Tick(0.5f);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(10f),
                "Re-registering resets the 0.75 s remainder, so 0.5 s more completes no cycle.");

            model.Tick(0.5f);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(20f));
        }

        [Test]
        public void Tick_PoolsAddedOrRemovedBetweenTicks_RefreshTheReusedSnapshot()
        {
            ResourcePoolModel model = new();
            ResourcePoolEntry first = Regenerating(50f, 100f);
            model.AddPool("a", first);
            model.Tick(1f);

            model.AddPool("b", Regenerating(50f, 100f));
            model.Tick(1f);

            Assert.That(model.GetCurrent("a"), Is.EqualTo(70f));
            Assert.That(model.GetCurrent("b"), Is.EqualTo(60f), "A pool added after a tick joins the next one.");

            model.RemovePool("a");
            model.Tick(1f);

            Assert.That(model.GetCurrent("b"), Is.EqualTo(70f));
            Assert.That(first.Current, Is.EqualTo(70f), "A removed pool is no longer ticked through the old snapshot.");

            model.AddPool("a", first);
            model.Tick(1f);

            Assert.That(model.GetCurrent("a"), Is.EqualTo(80f));
            Assert.That(model.GetCurrent("b"), Is.EqualTo(80f));
        }

        [Test]
        public void Tick_FramesShorterThanTheInterval_CompleteCyclesExactlyOnTheBoundary()
        {
            ResourcePoolModel model = new();
            ResourcePoolEntry entry = Regenerating(10f, 100f);
            entry.HealAmount = 1f;
            entry.HealDelay = 0.5f;
            model.AddPool("hp", entry);
            int changes = 0;
            model.OnResourceChanged += (id, current, max) => changes++;

            model.Tick(0.25f);
            model.Tick(0.125f);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(10f), "0.375 s completes neither the 0.5 s nor the 1 s timer.");
            Assert.That(changes, Is.Zero);

            model.Tick(0.125f);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(11f), "Reaching 0.5 s exactly completes one heal cycle.");

            model.Tick(0.5f);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(22f),
                "Reaching 1 s exactly completes the regen cycle, and the heal timer completes its second cycle.");
            Assert.That(changes, Is.EqualTo(3));

            model.Tick(0.25f);

            Assert.That(model.GetCurrent("hp"), Is.EqualTo(22f), "Both remainders restarted from zero.");
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(1)]
        [TestCase(8)]
        [TestCase(64)]
        public void Benchmark_Tick_HalfRegeneratingPools(int poolCount)
        {
            ResourcePoolModel model = CreateModel(poolCount, Math.Max(1, poolCount / 2));
            int changes = 0;
            model.OnResourceChanged += (id, current, max) => changes++;
            Action tick = () => model.Tick(0.016f);

            NeoPerf.MeasureSynchronous("core.resource_pool.tick_half_regen.pools" + poolCount, poolCount, 200,
                    NeoPerf.DefaultWarmupSamples, 60, tick)
                .Add("role", "current")
                .Emit();

            Assert.That(changes, Is.GreaterThan(0));
        }

        private static ResourcePoolModel CreateModel(int poolCount, int regeneratingCount)
        {
            ResourcePoolModel model = new();
            for (int i = 0; i < poolCount; i++)
            {
                bool regenerating = i < regeneratingCount;
                ResourcePoolEntry entry = Regenerating(regenerating ? 1f : 100f, regenerating ? 1e9f : 100f);
                entry.RegenInterval = 0.1f;
                entry.HealAmount = 1f;
                entry.HealDelay = 0.5f;
                model.AddPool("pool" + i, entry);
            }

            return model;
        }

        private static ResourcePoolEntry Regenerating(float current, float max)
        {
            return new ResourcePoolEntry
            {
                Current = current,
                Max = max,
                RegenPerSecond = 10f,
                RegenInterval = 1f,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f
            };
        }
    }
}
