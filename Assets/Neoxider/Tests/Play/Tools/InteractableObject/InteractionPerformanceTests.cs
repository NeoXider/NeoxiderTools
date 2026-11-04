using System.Collections;
using System.Collections.Generic;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     NEO_PERF measurements of automatic interaction on the real PlayerLoop. The default cases mirror the
    ///     functional PlayerLoop matrix (N distinct collinear targets on one shared ray). Every algorithm claim has a
    ///     same-instrument control on an identical scene: <c>*_complete</c> forces the complete projection for every
    ///     target, which is the pre-index N x H cost, so one run of this suite gives before and after.
    /// </summary>
    /// <remarks>
    ///     Compare NEO_PERF lines only with NEO_PERF lines of the same machine, Unity version and
    ///     <c>profiler_enabled</c> value. The 2026-11-02 audit captures ran with the profiler and a binary log on,
    ///     whose per-marker overhead is part of their numbers, so they are not a baseline for this suite.
    /// </remarks>
    public partial class Audit20261023NeoToolsInteractableObjectLifecycleTests
    {
        private const int PerfWarmFrames = 8;
        private const int PerfSampleFrames = 120;
        private const int PerfSlowSampleFrames = 24;
        private const int PerfLargeTargets = 20;
        private const int PerfLargeTargetChildColliders = 200;
        private const int PerfLargeRayHits = 12;

        private enum PerfRoute
        {
            Camera3D,
            Camera2D,
            Keyboard3D,
            Keyboard2D
        }

        private enum PerfStrategy
        {
            /// <summary>Product defaults: the index from 8 raw hits, bounded by the target-size hint.</summary>
            Default,

            /// <summary>Complete projection for every target: the pre-index N x H control.</summary>
            Complete,

            /// <summary>Index without the target-size hint: the unbounded target-enumeration control.</summary>
            ForcedIndex,

            /// <summary>Index from the first raw hit: the small-H control for tuning the index threshold.</summary>
            AlwaysIndex
        }

        private struct PerfFrameCounters
        {
            public long NativeCalls;
            public long ClassificationPasses;
            public long ClassifiedHits;
            public long CallbackBoundaries;
        }

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_1() => MeasureInteractionRoute(PerfRoute.Camera3D, 1);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_100() => MeasureInteractionRoute(PerfRoute.Camera3D, 100);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_1000() => MeasureInteractionRoute(PerfRoute.Camera3D, 1000);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera2D_1() => MeasureInteractionRoute(PerfRoute.Camera2D, 1);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera2D_100() => MeasureInteractionRoute(PerfRoute.Camera2D, 100);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera2D_1000() => MeasureInteractionRoute(PerfRoute.Camera2D, 1000);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard3D_1() => MeasureInteractionRoute(PerfRoute.Keyboard3D, 1);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard3D_100() => MeasureInteractionRoute(PerfRoute.Keyboard3D, 100);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard3D_1000() => MeasureInteractionRoute(PerfRoute.Keyboard3D, 1000);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard2D_1() => MeasureInteractionRoute(PerfRoute.Keyboard2D, 1);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard2D_100() => MeasureInteractionRoute(PerfRoute.Keyboard2D, 100);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard2D_1000() => MeasureInteractionRoute(PerfRoute.Keyboard2D, 1000);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_PositiveComplete_100() =>
            MeasureInteractionRoute(PerfRoute.Camera3D, 100, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_PositiveComplete_1000() =>
            MeasureInteractionRoute(PerfRoute.Camera3D, 1000, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera2D_PositiveComplete_100() =>
            MeasureInteractionRoute(PerfRoute.Camera2D, 100, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera2D_PositiveComplete_1000() =>
            MeasureInteractionRoute(PerfRoute.Camera2D, 1000, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard3D_PositiveComplete_100() =>
            MeasureInteractionRoute(PerfRoute.Keyboard3D, 100, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard3D_PositiveComplete_1000() =>
            MeasureInteractionRoute(PerfRoute.Keyboard3D, 1000, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard2D_PositiveComplete_100() =>
            MeasureInteractionRoute(PerfRoute.Keyboard2D, 100, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Keyboard2D_PositiveComplete_1000() =>
            MeasureInteractionRoute(PerfRoute.Keyboard2D, 1000, PerfStrategy.Complete);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_PartialMiss_1000() => MeasurePartialMiss(false);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera2D_PartialMiss_1000() => MeasurePartialMiss(true);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_PartialMissComplete_1000() =>
            MeasurePartialMiss(false, PerfStrategy.Complete);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_ObstaclesOn_100() => MeasureObstaclesOn(100);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_ObstaclesOn_1000() => MeasureObstaclesOn(1000);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_ObstaclesOnComplete_100() =>
            MeasureObstaclesOn(100, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_ObstaclesOnComplete_1000() =>
            MeasureObstaclesOn(1000, PerfStrategy.Complete);

        // WHY: the small-H sweep brackets InteractionHitProjector.DefaultIndexedMinimumHits. Each case puts N = H
        // targets on one ray, so the complete projection inspects H x H entries and the index H + H; compare the two
        // strategies at every H and move the threshold to where the index starts to win.
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsComplete_2() => MeasureSmallHits(2, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsIndex_2() => MeasureSmallHits(2, PerfStrategy.AlwaysIndex);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsComplete_4() => MeasureSmallHits(4, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsIndex_4() => MeasureSmallHits(4, PerfStrategy.AlwaysIndex);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsComplete_6() => MeasureSmallHits(6, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsIndex_6() => MeasureSmallHits(6, PerfStrategy.AlwaysIndex);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsComplete_8() => MeasureSmallHits(8, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsIndex_8() => MeasureSmallHits(8, PerfStrategy.AlwaysIndex);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsComplete_12() =>
            MeasureSmallHits(12, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsIndex_12() =>
            MeasureSmallHits(12, PerfStrategy.AlwaysIndex);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsComplete_16() =>
            MeasureSmallHits(16, PerfStrategy.Complete);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_SmallHitsIndex_16() =>
            MeasureSmallHits(16, PerfStrategy.AlwaysIndex);

        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_LargeHierarchy_20() => MeasureLargeHierarchy(PerfStrategy.Default);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_LargeHierarchyForcedIndex_20() =>
            MeasureLargeHierarchy(PerfStrategy.ForcedIndex);
        [UnityTest, Explicit(NeoPerf.ExplicitReason), Category(NeoPerf.Category)]
        public IEnumerator Perf_Interaction_Camera3D_LargeHierarchyComplete_20() =>
            MeasureLargeHierarchy(PerfStrategy.Complete);

        private IEnumerator MeasureInteractionRoute(PerfRoute route, int targetCount,
            PerfStrategy strategy = PerfStrategy.Default)
        {
            bool keyboard = route == PerfRoute.Keyboard3D || route == PerfRoute.Keyboard2D;
            bool twoDimensions = route == PerfRoute.Camera2D || route == PerfRoute.Keyboard2D;
            if (keyboard)
            {
                EnsureInput();
            }

            Transform source = _camera.transform;
            if (keyboard && twoDimensions)
            {
                source = CreateObject("PerfPlanarLookSource").transform;
                source.position = Origin;
                source.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            }

            Vector3 direction = keyboard && twoDimensions ? Vector3.right : Vector3.forward;
            List<InteractiveObject> targets = new(targetCount);
            for (int i = 0; i < targetCount; i++)
            {
                InteractiveObject target = CreateTarget(Origin + direction * (2f + i * 0.005f),
                    keyboard: keyboard, hover: !keyboard, twoDimensions: twoDimensions);
                target.InteractionDistance = 10f;
                SetField(target, "checkObstacles", false);
                SetField(target, "viewCheckPoint", source);
                ApplyPerfStrategy(target, strategy);
                AssertNoObserverEvents(target);
                targets.Add(target);
            }

            CreatePerfWindowRig();
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            for (int i = 0; i < PerfWarmFrames; i++)
            {
                QueuePerfEdge(keyboard, i);
                yield return null;
            }

            QueuePerfEdge(keyboard, 0);
            yield return null;
            AssertConsumerStates(targets, keyboard, true);
            // WHY: Release first, so the first measured frame is a real press edge.
            QueuePerfEdge(keyboard, 1);
            yield return null;

            string routeId = "interaction." + route.ToString().ToLowerInvariant();
            long expectedPositive;
            switch (strategy)
            {
                case PerfStrategy.Complete:
                    expectedPositive = (long)targetCount * targetCount;
                    break;
                case PerfStrategy.AlwaysIndex:
                    expectedPositive = (long)targetCount + targetCount;
                    break;
                default:
                    expectedPositive = ExpectedDenseInspection(targetCount, targetCount);
                    break;
            }

            yield return MeasureInteractionFrames(routeId + ".positive" + StrategySuffix(strategy) + ".n" + targetCount,
                strategy, targetCount, keyboard, SampleFrames(strategy, targetCount), expectedPositive);
            if (strategy != PerfStrategy.Default)
            {
                // WHY: A stable miss has no raw hits, so every strategy inspects nothing; only the default is reported.
                yield break;
            }

            foreach (InteractiveObject target in targets)
            {
                target.transform.position += Vector3.up * 20f;
            }

            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.Invalidate();
            for (int i = 0; i < PerfWarmFrames; i++)
            {
                QueuePerfEdge(keyboard, i);
                yield return null;
            }

            yield return MeasureInteractionFrames(routeId + ".stable_miss.n" + targetCount, strategy, targetCount,
                keyboard, PerfSampleFrames, 0L);
            AssertConsumerStates(targets, keyboard, false);
        }

        private IEnumerator MeasureSmallHits(int hitCount, PerfStrategy strategy)
        {
            return MeasureInteractionRoute(PerfRoute.Camera3D, hitCount, strategy);
        }

        private IEnumerator MeasurePartialMiss(bool twoDimensions, PerfStrategy strategy = PerfStrategy.Default)
        {
            const int targetCount = 1000;
            const int onRay = 100;
            List<InteractiveObject> hit = new(onRay);
            for (int i = 0; i < targetCount; i++)
            {
                bool onTheRay = i < onRay;
                Vector3 position = Origin + Vector3.forward * (2f + i * 0.005f) +
                                   (onTheRay ? Vector3.zero : Vector3.right * 3f);
                InteractiveObject target = CreateTarget(position, hover: true, twoDimensions: twoDimensions);
                SetField(target, "checkObstacles", false);
                ApplyPerfStrategy(target, strategy);
                AssertNoObserverEvents(target);
                if (onTheRay)
                {
                    hit.Add(target);
                }
            }

            CreatePerfWindowRig();
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            for (int i = 0; i < PerfWarmFrames; i++)
            {
                yield return null;
            }

            AssertConsumerStates(hit, false, true);
            string caseId = "interaction.camera" + (twoDimensions ? "2d" : "3d") + ".partial_miss" +
                            StrategySuffix(strategy) + ".n" + targetCount;
            long expected = strategy == PerfStrategy.Complete ? (long)targetCount * onRay : (long)onRay + onRay;
            yield return MeasureInteractionFrames(caseId, strategy, targetCount, false, PerfSampleFrames, expected);
        }

        private IEnumerator MeasureObstaclesOn(int targetCount, PerfStrategy strategy = PerfStrategy.Default)
        {
            // WHY: Default checkObstacles=true exercises the clear-path walk: every target behind the nearest one
            // stops at the first blocker in the shared distance order instead of projecting all H hits.
            List<InteractiveObject> targets = new(targetCount);
            for (int i = 0; i < targetCount; i++)
            {
                InteractiveObject target = CreateTarget(Origin + Vector3.forward * (2f + i * 0.005f), hover: true);
                ApplyPerfStrategy(target, strategy);
                AssertNoObserverEvents(target);
                targets.Add(target);
            }

            CreatePerfWindowRig();
            Physics.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            for (int i = 0; i < PerfWarmFrames; i++)
            {
                yield return null;
            }

            int hovered = 0;
            foreach (InteractiveObject target in targets)
            {
                hovered += target.IsHovered ? 1 : 0;
            }

            Assert.That(targets[0].IsHovered, Is.True, "The nearest collinear target is the only clear one.");
            Assert.That(hovered, Is.EqualTo(1));
            long expected = strategy == PerfStrategy.Complete
                ? (long)targetCount * targetCount
                : ExpectedClearPathInspection(targetCount);
            yield return MeasureInteractionFrames(
                "interaction.camera3d.obstacles_on" + StrategySuffix(strategy) + ".n" + targetCount, strategy,
                targetCount, false, SampleFrames(strategy, targetCount), expected);
        }

        private IEnumerator MeasureLargeHierarchy(PerfStrategy strategy)
        {
            // WHY: Consumers with hundreds of child colliders on a moderately dense ray are where enumerating the
            // target costs more than checking the H raw hits; the default must stay at the complete control's cost.
            for (int i = 0; i < PerfLargeRayHits; i++)
            {
                GameObject rayCollider = CreateObject("PerfRayCollider");
                rayCollider.transform.position = Origin + Vector3.forward * (2f + i * 0.5f);
                rayCollider.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            }

            List<InteractiveObject> targets = new(PerfLargeTargets);
            for (int i = 0; i < PerfLargeTargets; i++)
            {
                InteractiveObject target = CreateTarget(Origin + Vector3.right * 3f + Vector3.forward * (2f + i * 0.5f),
                    hover: true);
                SetField(target, "checkObstacles", false);
                for (int c = 0; c < PerfLargeTargetChildColliders; c++)
                {
                    GameObject part = new("PerfTargetPart");
                    part.transform.SetParent(target.transform, false);
                    part.transform.localPosition = new Vector3(c % 10 * 0.05f, c / 10 * 0.05f, 0f);
                    part.AddComponent<BoxCollider>().size = Vector3.one * 0.04f;
                }

                ApplyPerfStrategy(target, strategy);
                AssertNoObserverEvents(target);
                targets.Add(target);
            }

            CreatePerfWindowRig();
            Physics.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            for (int i = 0; i < PerfWarmFrames; i++)
            {
                yield return null;
            }

            AssertConsumerStates(targets, false, false);
            const long rayHits = PerfLargeRayHits;
            long complete = rayHits * PerfLargeTargets;
            long expected = strategy == PerfStrategy.ForcedIndex ? rayHits : complete;
            // WHY: Every TargetSizeHintLifetime frames each default target enumerates again (index build, no hit).
            long refresh = strategy == PerfStrategy.Default ? rayHits : -1L;
            yield return MeasureInteractionFrames(
                "interaction.camera3d.large_hierarchy" + StrategySuffix(strategy) + ".n" + PerfLargeTargets, strategy,
                PerfLargeTargets, false, PerfSampleFrames, expected, refresh,
                (long)PerfLargeTargetChildColliders + 1);
        }

        private IEnumerator MeasureInteractionFrames(string caseId, PerfStrategy strategy, int targetCount,
            bool keyboard, int frames, long expectedInspectedPerFrame, long refreshInspectedPerFrame = -1L,
            long collidersPerTarget = 1L)
        {
            PerfFrameCounters[] counters = new PerfFrameCounters[frames];
            double[] automatic = new double[frames];
            int automaticCount = 0;
            NeoPerfUpdateWindow window = NeoPerfUpdateWindow.Start(frames);
            // WHY: Default options sum every AutomaticUpdate sample of a frame: the CPU of all N targets together.
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts,
                "Neo.Interaction.AutomaticUpdate");
            try
            {
                // WHY: The first resumed frame only exposes the partial frame in which recording started.
                QueuePerfEdge(keyboard, 0);
                InteractionPhysicsQueries.ResetCounters();
                yield return null;
                for (int i = 0; i < frames; i++)
                {
                    InteractionPhysicsQueries.Statistics frameCounters = InteractionPhysicsQueries.Counters;
                    counters[i] = new PerfFrameCounters
                    {
                        NativeCalls = frameCounters.NativeCalls,
                        ClassificationPasses = frameCounters.ClassificationPasses,
                        ClassifiedHits = frameCounters.ClassifiedHits,
                        CallbackBoundaries = frameCounters.CallbackBoundaries
                    };
                    QueuePerfEdge(keyboard, i + 1);
                    InteractionPhysicsQueries.ResetCounters();
                    yield return null;
                    if (recorder.Valid)
                    {
                        automatic[automaticCount++] = recorder.LastValue / 1000000d;
                    }
                }
            }
            finally
            {
                window.Stop();
                recorder.Dispose();
            }

            Assert.That(window.Count, Is.EqualTo(frames), "Every measured frame must close its Update window.");
            long allocationMax = 0;
            double[] allocationsPerFrame = new double[window.Count];
            for (int i = 0; i < window.Count; i++)
            {
                allocationMax = System.Math.Max(allocationMax, window.AllocationCounts[i]);
                allocationsPerFrame[i] = window.AllocationCounts[i];
            }

            // WHY: Workload identity is checked after recording so assertions stay outside the measured frames.
            long inspectedMin = long.MaxValue;
            long inspectedMax = long.MinValue;
            for (int i = 0; i < frames; i++)
            {
                long inspected = counters[i].ClassifiedHits;
                inspectedMin = System.Math.Min(inspectedMin, inspected);
                inspectedMax = System.Math.Max(inspectedMax, inspected);
                Assert.That(counters[i].NativeCalls, Is.EqualTo(1), caseId + " frame " + i);
                Assert.That(counters[i].ClassificationPasses, Is.EqualTo(targetCount), caseId + " frame " + i);
                Assert.That(inspected == expectedInspectedPerFrame || inspected == refreshInspectedPerFrame, Is.True,
                    caseId + " frame " + i + ": inspected " + inspected + ", expected " + expectedInspectedPerFrame +
                    (refreshInspectedPerFrame >= 0 ? " or " + refreshInspectedPerFrame : string.Empty));
                Assert.That(counters[i].CallbackBoundaries, Is.Zero, caseId + " frame " + i);
            }

            NeoPerf.Line line = new NeoPerf.Line(caseId)
                .Add("n", targetCount)
                .Add("frames", frames)
                .Add("strategy", StrategyKey(strategy))
                .Add("colliders_per_target", collidersPerTarget);
            if (automaticCount > 0)
            {
                line.AddMilliseconds("auto", NeoPerf.Summarize(automatic, automaticCount));
            }
            else
            {
                line.Add("auto", "unavailable");
            }

            line.AddMilliseconds("update", NeoPerf.Summarize(window.Milliseconds, window.Count))
                .Add("native_per_frame", counters[0].NativeCalls)
                .Add("inspected_per_frame", expectedInspectedPerFrame);
            if (inspectedMin != inspectedMax)
            {
                line.Add("inspected_min", inspectedMin).Add("inspected_max", inspectedMax);
            }

            if (window.AllocationsAvailable)
            {
                line.Add("gc_allocs_per_frame_p50", NeoPerf.Summarize(allocationsPerFrame, window.Count).P50)
                    .Add("gc_allocs_per_frame_max", allocationMax);
            }
            else
            {
                line.Add("gc_allocs_per_frame", "unavailable");
            }

            line.Emit();
        }

        private static void ApplyPerfStrategy(InteractiveObject target, PerfStrategy strategy)
        {
            if (strategy == PerfStrategy.Complete)
            {
                target.HitProjector.IndexedMinimumHits = int.MaxValue;
            }
            else if (strategy == PerfStrategy.ForcedIndex)
            {
                target.HitProjector.UseTargetSizeHint = false;
            }
            else if (strategy == PerfStrategy.AlwaysIndex)
            {
                target.HitProjector.IndexedMinimumHits = 1;
            }
        }

        private static int SampleFrames(PerfStrategy strategy, int targetCount)
        {
            // WHY: A complete N = 1000 frame costs about N x H = 1M hierarchy checks; fewer frames keep it bounded.
            return strategy == PerfStrategy.Complete && targetCount >= 1000 ? PerfSlowSampleFrames : PerfSampleFrames;
        }

        private static string StrategySuffix(PerfStrategy strategy)
        {
            return strategy == PerfStrategy.Default ? string.Empty : "_" + StrategyKey(strategy);
        }

        private static string StrategyKey(PerfStrategy strategy)
        {
            switch (strategy)
            {
                case PerfStrategy.Complete:
                    return "complete";
                case PerfStrategy.ForcedIndex:
                    return "forced_index";
                case PerfStrategy.AlwaysIndex:
                    return "index_always";
                default:
                    return "default";
            }
        }

        private void CreatePerfWindowRig()
        {
            GameObject rig = CreateObject("NeoPerfUpdateWindow");
            rig.AddComponent<NeoPerfUpdateWindowBegin>();
            rig.AddComponent<NeoPerfUpdateWindowEnd>();
        }

        private void QueuePerfEdge(bool keyboard, int frame)
        {
            // WHY: Keyboard look queries run only on press/release edges, so alternate them to make every
            // measured frame query-bearing.
            if (keyboard)
            {
                _input.QueueKeyboard((frame & 1) == 0);
            }
        }
    }
}
