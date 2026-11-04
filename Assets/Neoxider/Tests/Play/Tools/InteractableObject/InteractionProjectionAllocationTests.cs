using System;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Neo.Tests.Play
{
    public partial class Audit20261023NeoToolsInteractableObjectLifecycleTests
    {
        /// <summary>
        ///     Warm indexed and complete projections run every frame for every target, so after their buffers grew
        ///     once they must not allocate (the 2026-11-02 captures measured zero AutomaticUpdate bytes).
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void Projection_WarmIndexedAndCompleteProjectionsAllocateNothing(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 9f, twoDimensions: twoDimensions);
            GameObject child = CreateObject("AllocationChild");
            child.transform.SetParent(target.transform, false);
            int foreignCount = InteractionHitProjector.DefaultIndexedMinimumHits + 4;
            for (int i = 0; i < foreignCount; i++)
            {
                CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * (2f + i * 0.5f));
            }

            SetField(target, "checkObstacles", false);
            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, foreignCount + 1);
            InteractionHitProjector projector = target.HitProjector;
            bool includeTriggers = ReadIndexedField<bool>(target, "includeTriggerCollidersInMouseRaycast");
            Action project = () =>
            {
                InteractionTargetScope scope = target.TargetScope;
                if (twoDimensions)
                {
                    projector.Project2D(lease.Hits2D, scope, false, includeTriggers, false, false);
                    projector.Project2D(lease.Hits2D, scope, false, includeTriggers, true, true);
                    projector.Project2D(lease.Hits2D, scope, true, true, true, true);
                }
                else
                {
                    projector.Project3D(lease.Hits3D, scope, false, false, false);
                    projector.Project3D(lease.Hits3D, scope, false, true, true);
                    projector.Project3D(lease.Hits3D, scope, true, true, true);
                }
            };

            project();
            project();
            int selected = ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out _);
            Assert.That(InteractionQueryMath.TrySelectTarget(projector.Hits, selected, false, out _), Is.True,
                "Positive control: the indexed projection finds the target behind the foreign colliders.");
            Assert.That(() =>
            {
                for (int i = 0; i < 64; i++)
                {
                    project();
                }
            }, Is.Not.AllocatingGCMemory());
            AssertIndexedLeaseStable(lease);
        }

        /// <summary>
        ///     Every frame starts a new epoch, so the shared collider index (dictionary clear and refill) and the
        ///     distance order (sort) are rebuilt per frame. Once their storage grew, that rebuild, the native re-query
        ///     and the clear-path walk must not allocate either.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void Projection_PerEpochIndexAndDistanceOrderRebuildAllocatesNothing(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 9f, twoDimensions: twoDimensions);
            int foreignCount = InteractionHitProjector.DefaultIndexedMinimumHits + 4;
            for (int i = 0; i < foreignCount; i++)
            {
                CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * (2f + i * 0.5f));
            }

            SetField(target, "checkObstacles", true);
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Camera source = _camera;
            InteractionHitProjector projector = target.HitProjector;
            bool includeTriggers = ReadIndexedField<bool>(target, "includeTriggerCollidersInMouseRaycast");
            int rawHits = 0;
            int blockedSelections = 0;
            Action rebuild = () =>
            {
                InteractionPhysicsQueries.Invalidate();
                InteractionTargetScope scope = target.TargetScope;
                int candidates;
                if (twoDimensions)
                {
                    InteractionPhysicsQueries.Hits2D hits = InteractionPhysicsQueries.GetRayIntersection2D(source, ray,
                        float.MaxValue, ~0, InteractionPhysicsQueries.QueryMode.Shared);
                    rawHits = hits.Count;
                    candidates = projector.Project2D(hits, scope, false, includeTriggers, true, false);
                    blockedSelections += InteractionQueryMath.TrySelectTarget(projector.Hits, candidates, true, out _)
                        ? 0
                        : 1;
                    projector.Project2D(hits, scope, false, includeTriggers, true, true);
                }
                else
                {
                    InteractionPhysicsQueries.Hits3D hits = InteractionPhysicsQueries.Raycast3D(source, ray,
                        float.MaxValue, ~0, QueryTriggerInteraction.Collide, InteractionPhysicsQueries.QueryMode.Shared);
                    rawHits = hits.Count;
                    candidates = projector.Project3D(hits, scope, false, true, false);
                    blockedSelections += InteractionQueryMath.TrySelectTarget(projector.Hits, candidates, true, out _)
                        ? 0
                        : 1;
                    projector.Project3D(hits, scope, false, true, true);
                }
            };

            rebuild();
            rebuild();
            Assert.That(rawHits, Is.EqualTo(foreignCount + 1), "Every foreign collider and the target are on the ray.");
            Assert.That(blockedSelections, Is.EqualTo(2), "The foreign colliders in front block the target.");
            InteractionPhysicsQueries.ResetCounters();
            rebuild();
            InteractionPhysicsQueries.Statistics counters = InteractionPhysicsQueries.Counters;
            Assert.That(counters.NativeCalls, Is.EqualTo(1), "Each epoch re-queries the physics world once.");
            Assert.That(counters.CacheHits, Is.Zero);
            // WHY: Target-only: index H + own entry 1 + distance order H + first blocker 1. Nearest-hit: own entry 1 +
            // first blocker 1, then the complete fallback H. Both builds are counted, so they ran in this epoch.
            Assert.That(counters.ClassifiedHits, Is.EqualTo(3L * rawHits + 4L));

            Assert.That(() =>
            {
                for (int i = 0; i < 64; i++)
                {
                    rebuild();
                }
            }, Is.Not.AllocatingGCMemory());
            Assert.That(blockedSelections, Is.EqualTo(3 + 64));
        }
    }
}
