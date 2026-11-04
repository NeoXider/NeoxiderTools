using System;
using System.Collections.Generic;
using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Explicit benchmarks for the scene hub's radius queries at survivor-scale unit counts: the linear scan, the
    ///     warm per-frame spatial grid, the grid rebuilt before every query (the worst case after invalidation) and
    ///     the grid patched by a <c>TryMoveUnit</c> knockback before every query (what a full rebuild cost before
    ///     in-place moves). Every mode runs the same queries and must find the same number of hits.
    /// </summary>
    [TestFixture]
    public sealed class AbilityQueryBenchmarks
    {
        private const int QueriesPerSample = 64;
        private const int Samples = 60;
        private const float QueryRadius = 3f;
        private const float Spacing = 1.5f;

        [OneTimeSetUp]
        public void EmitCalibration()
        {
            NeoPerf.EmitCalibration("abilities.hub_query", 16);
        }

        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        [TestCase(64)]
        [TestCase(256)]
        [TestCase(1024)]
        public void Benchmark_HubRadiusQuery_LinearGridAndRebuild(int unitCount)
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                int side = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
                for (int i = 0; i < unitCount; i++)
                {
                    rig.AddUnit("BenchUnit" + i, 2, new Vector3(i % side * Spacing, 0f, i / side * Spacing));
                }

                float extent = side * Spacing;
                Vector3[] centers = new Vector3[QueriesPerSample];
                XorShiftRandom random = new XorShiftRandom(7u);
                for (int i = 0; i < centers.Length; i++)
                {
                    centers[i] = new Vector3(random.NextFloat() * extent, 0f, random.NextFloat() * extent);
                }

                List<UnitId> results = new List<UnitId>(unitCount);
                long hits = 0;
                Action query = () =>
                {
                    for (int q = 0; q < QueriesPerSample; q++)
                    {
                        results.Clear();
                        rig.Hub.QueryUnitsInRadius(centers[q], QueryRadius, results);
                        hits += results.Count;
                    }
                };
                Action rebuildAndQuery = () =>
                {
                    for (int q = 0; q < QueriesPerSample; q++)
                    {
                        rig.Hub.InvalidateSpatialCache();
                        results.Clear();
                        rig.Hub.QueryUnitsInRadius(centers[q], QueryRadius, results);
                        hits += results.Count;
                    }
                };

                rig.Hub.SpatialCellSize = 0f;
                NeoPerf.MeasureSamples("abilities.hub_query.linear.n" + unitCount, unitCount, QueriesPerSample,
                        NeoPerf.DefaultWarmupSamples, Samples, query)
                    .Add("role", "linear")
                    .Add("radius", QueryRadius)
                    .Emit();
                long linearHits = hits;

                rig.Hub.SpatialCellSize = QueryRadius;
                hits = 0;
                NeoPerf.MeasureSamples("abilities.hub_query.grid.n" + unitCount, unitCount, QueriesPerSample,
                        NeoPerf.DefaultWarmupSamples, Samples, query)
                    .Add("role", "grid")
                    .Add("cell", QueryRadius)
                    .Emit();
                long gridHits = hits;

                hits = 0;
                NeoPerf.MeasureSamples("abilities.hub_query.grid_rebuild.n" + unitCount, unitCount, QueriesPerSample,
                        NeoPerf.DefaultWarmupSamples, Samples, rebuildAndQuery)
                    .Add("role", "grid_rebuild_every_query")
                    .Add("cell", QueryRadius)
                    .Emit();
                long rebuildHits = hits;

                // WHY: the mover sits far outside every query circle and moves between two far points, so the
                // hit counts stay comparable while each query follows a real cross-cell TryMoveUnit.
                UnitId mover = rig.AddUnit("BenchMover", 2, new Vector3(-1000f, 0f, -1000f)).UnitId;
                Vector3 farA = new Vector3(-1000f, 0f, -1000f);
                Vector3 farB = new Vector3(-1000f, 0f, 1000f);
                Action moveAndQuery = () =>
                {
                    for (int q = 0; q < QueriesPerSample; q++)
                    {
                        rig.Hub.TryMoveUnit(mover, (q & 1) == 0 ? farB : farA);
                        results.Clear();
                        rig.Hub.QueryUnitsInRadius(centers[q], QueryRadius, results);
                        hits += results.Count;
                    }
                };

                hits = 0;
                NeoPerf.MeasureSamples("abilities.hub_query.grid_move.n" + unitCount, unitCount, QueriesPerSample,
                        NeoPerf.DefaultWarmupSamples, Samples, moveAndQuery)
                    .Add("role", "grid_trymoveunit_every_query")
                    .Add("cell", QueryRadius)
                    .Emit();

                Assert.That(linearHits, Is.GreaterThan(0L));
                Assert.That(gridHits, Is.EqualTo(linearHits), "grid and linear scan must find the same units");
                Assert.That(rebuildHits, Is.EqualTo(linearHits), "a rebuilt grid must find the same units");
                Assert.That(hits, Is.EqualTo(linearHits), "a patched grid must find the same units");
            }
        }
    }
}
