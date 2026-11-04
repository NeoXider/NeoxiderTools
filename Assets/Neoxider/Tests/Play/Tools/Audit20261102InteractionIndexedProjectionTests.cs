using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Parity of the indexed and complete projections on one borrowed lease. Reuses the lifecycle fixture's
    ///     camera rig, null-observer targets, owned objects and teardown.
    /// </summary>
    public partial class Audit20261023NeoToolsInteractableObjectLifecycleTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void IndexedProjection_SameLeaseNestedChildRigAndExplicitOverride(bool twoDimensions)
        {
            GameObject level = CreateObject("IndexedCommonLevel");
            InteractiveObject target = CreateTarget(Origin + new Vector3(2f, 0f, 6f), twoDimensions: twoDimensions);
            ForceIndexedProjection(target);
            target.transform.SetParent(level.transform, true);
            Component primary = IndexedPrimary(target, twoDimensions);
            GameObject rig = CreateObject("IndexedRig");
            rig.transform.SetParent(level.transform, true);
            GameObject checkpoint = CreateObject("IndexedCheckpoint");
            checkpoint.transform.SetParent(rig.transform, true);
            checkpoint.transform.position = Origin;
            _camera.transform.SetParent(checkpoint.transform, true);
            target.DistanceCheckPoint = checkpoint.transform;
            SetField(target, "viewCheckPoint", checkpoint.transform);
            SetField(target, "checkObstacles", false);

            GameObject ancestor = CreateObject("IndexedMovingAncestor");
            ancestor.transform.SetParent(level.transform, true);
            GameObject inner = CreateObject("IndexedUnchangedImmediateParent");
            inner.transform.SetParent(ancestor.transform, true);
            Component child = CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * 3f);
            child.transform.SetParent(inner.transform, true);
            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, 1);

            AssertIndexedParity(lease, target, false, out long coldFallbackWork);
            Assert.That(coldFallbackWork, Is.EqualTo(2), "H=1 index build + H=1 complete miss fallback.");
            AssertIndexedHover(lease, target, false);
            ancestor.transform.SetParent(target.transform, true);
            AssertIndexedParity(lease, target, true, out long reparentWork);
            Assert.That(reparentWork, Is.EqualTo(1), "Existing index + one live reparented target projection.");
            AssertIndexedHover(lease, target, true);

            string explicitField = twoDimensions ? "targetCollider2D" : "targetCollider3D";
            SetField(target, explicitField, primary);
            AssertIndexedParity(lease, target, false, out _);
            AssertIndexedHover(lease, target, false);
            SetField(target, explicitField, child);
            AssertIndexedParity(lease, target, true, out _);
            AssertIndexedHover(lease, target, true);

            // WHY: The collider's immediate parent and level root stay fixed; only its deeper ancestry moves.
            ancestor.transform.SetParent(checkpoint.transform, true);
            Assert.That(child.transform.parent, Is.SameAs(inner.transform));
            Assert.That(child.transform.root, Is.SameAs(level.transform));
            // WHY: An explicit target collider must never be ignored as part of the source rig.
            AssertIndexedParity(lease, target, true, out _);
            AssertIndexedHover(lease, target, true);
            SetField(target, explicitField, null);
            AssertIndexedParity(lease, target, false, out _);
            AssertIndexedHover(lease, target, false);
            AssertIndexedNativeControl(lease, 1, true);
            AssertIndexedLeaseStable(lease);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IndexedProjection_SameLeaseDestroyedDisabledAndLiveTriggers(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 6f, twoDimensions: twoDimensions);
            ForceIndexedProjection(target);
            Component primary = IndexedPrimary(target, twoDimensions);
            Component wall = CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * 3f);
            SetField(target, "checkObstacles", false);
            // WHY: 3D Ignore is fixed for the whole lease; the complete projection never re-filters a later live
            // trigger change of a cached 3D hit.
            SetField(target, "includeTriggerCollidersInMouseRaycast", twoDimensions);
            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, twoDimensions, 2);
            AssertIndexedParity(lease, target, true, out _);
            // WHY: Build the index before any mutation, so every later step reads the same warm entry.
            AssertIndexedHover(lease, target, true);

            SetField(target, "checkObstacles", true);
            SetIndexedEnabled(wall, false);
            AssertIndexedParity(lease, target, false, out _);
            // WHY: A disabled collider is still in the borrowed snapshot, while a fresh native query excludes it.
            AssertIndexedHover(lease, target, false);
            AssertIndexedNativeControl(lease, 1, false);
            SetIndexedEnabled(wall, true);
            wall.gameObject.SetActive(false);
            AssertIndexedParity(lease, target, false, out _);
            AssertIndexedHover(lease, target, false);
            wall.gameObject.SetActive(true);

            SetIndexedTrigger(wall, true);
            AssertIndexedParity(lease, target, true, out _);
            // WHY: A foreign collider that became a trigger no longer blocks.
            AssertIndexedHover(lease, target, true);
            SetField(target, "checkObstacles", false);
            SetIndexedTrigger(primary, true);
            if (twoDimensions)
            {
                SetField(target, "includeTriggerCollidersInMouseRaycast", false);
                AssertIndexedParity(lease, target, false, out long excludedWork);
                Assert.That(excludedWork, Is.EqualTo(3), "One rejected 2D target projection + H=2 full fallback.");
                // WHY: The live 2D trigger inclusion gate must stay effective on a cached hit.
                AssertIndexedHover(lease, target, false);
                SetField(target, "includeTriggerCollidersInMouseRaycast", true);
            }

            AssertIndexedParity(lease, target, true, out _);
            // WHY: 3D keeps its cached hit despite the collider's current trigger flag.
            AssertIndexedHover(lease, target, true);
            SetIndexedTrigger(primary, false);
            SetIndexedTrigger(wall, false);
            SetField(target, "checkObstacles", true);
            AssertIndexedHover(lease, target, false);
            Object.DestroyImmediate(wall.gameObject);
            AssertIndexedParity(lease, target, true, out _);
            // WHY: A destroyed collider reads as null and is skipped, unlike the disabled control above.
            AssertIndexedHover(lease, target, true);
            SetField(target, "checkObstacles", false);
            AssertIndexedParity(lease, target, true, out _);
            AssertIndexedHover(lease, target, true);
            AssertIndexedNativeControl(lease, 1, true);
            AssertIndexedLeaseStable(lease);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IndexedProjection_CompoundNativeOrderPointRangeAndHonestWork(bool twoDimensions)
        {
            // WHY: A slanted ray gives different XY hit points in 2D too (RaycastHit2D.point has no Z).
            Vector3 direction = new Vector3(0.25f, 0f, 1f).normalized;
            _camera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            InteractiveObject target = CreateTarget(Origin + direction * 6f, twoDimensions: twoDimensions);
            ForceIndexedProjection(target);
            Component primary = IndexedPrimary(target, twoDimensions);
            Component child = CreateIndexedCollider(twoDimensions, Origin + direction * 3f);
            child.transform.SetParent(target.transform, true);
            Component equalChild = CreateIndexedCollider(twoDimensions, child.transform.position);
            equalChild.transform.SetParent(target.transform, true);
            CreateIndexedCollider(twoDimensions, Origin + direction);
            Component foreignTrigger = CreateIndexedCollider(twoDimensions, Origin + direction * 2f);
            SetIndexedTrigger(foreignTrigger, true);
            SetField(target, "checkObstacles", false);
            target.DistanceCheckPoint = _camera.transform;
            target.InteractionDistance = twoDimensions ? 1f : 3.2f;
            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, 5);
            float childDistance = float.NaN;
            int tiedChildren = 0;
            for (int i = 0; i < lease.Count; i++)
            {
                Component collider = twoDimensions ? (Component)lease.Hits2D[i].collider : lease.Hits3D[i].collider;
                if (collider == child || collider == equalChild)
                {
                    float distance = twoDimensions ? lease.Hits2D[i].distance : lease.Hits3D[i].distance;
                    if (tiedChildren > 0) Assert.That(distance, Is.EqualTo(childDistance));
                    childDistance = distance;
                    tiedChildren++;
                }
            }

            Assert.That(tiedChildren, Is.EqualTo(2), "Actual native equal-distance compound admission.");

            InteractionRayHit selected = AssertIndexedParity(lease, target, true, out long coldWork);
            Assert.That(coldWork, Is.EqualTo(8), "H=5 complete index build + T=3 selected raw projections.");
            Assert.That(InteractionQueryMath.IsWithinRange(_camera.transform.position, selected.Point,
                target.InteractionDistance), Is.True, "Actual nearest child point is inside range.");
            Assert.That(Vector3.Distance(_camera.transform.position, selected.Point), Is.GreaterThan(0.5f));
            AssertIndexedParity(lease, target, true, out long warmWork);
            Assert.That(warmWork, Is.EqualTo(3), "Same-lease reuse inspects only the three target raw entries.");
            AssertIndexedHover(lease, target, true);

            SetField(target, twoDimensions ? "targetCollider2D" : "targetCollider3D", primary);
            InteractionRayHit explicitHit = AssertIndexedParity(lease, target, true, out long explicitWork);
            Assert.That(explicitWork, Is.EqualTo(1));
            Assert.That(InteractionQueryMath.IsWithinRange(_camera.transform.position, explicitHit.Point,
                target.InteractionDistance), Is.False, "Explicit root point is outside range.");
            // WHY: The explicit root point is outside range; descendant colliders cannot widen it.
            AssertIndexedHover(lease, target, false);
            AssertIndexedNativeControl(lease, 5, true);
            AssertIndexedLeaseStable(lease);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Projection_SmallRawSetUsesLinearPassWithIndexedParity(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 6f, twoDimensions: twoDimensions);
            CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * 3f);
            SetField(target, "checkObstacles", false);
            Assert.That(target.HitProjector.IndexedMinimumHits,
                Is.EqualTo(InteractionHitProjector.DefaultIndexedMinimumHits));
            Assert.That(InteractionHitProjector.DefaultIndexedMinimumHits, Is.GreaterThan(2));
            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, 2);

            int linearCount = ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out long linearWork);
            Assert.That(linearWork, Is.EqualTo(2), "A raw set below the threshold is inspected once, without an index.");
            Assert.That(InteractionQueryMath.TrySelectTarget(target.HitProjector.Hits, linearCount, false,
                out InteractionRayHit linearHit), Is.True);

            ForceIndexedProjection(target);
            int indexedCount = ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out long indexedWork);
            Assert.That(indexedWork, Is.EqualTo(3), "Forced index: H=2 build + one selected target entry.");
            Assert.That(InteractionQueryMath.TrySelectTarget(target.HitProjector.Hits, indexedCount, false,
                out InteractionRayHit indexedHit), Is.True);
            Assert.That(indexedHit.Distance, Is.EqualTo(linearHit.Distance));
            Assert.That(indexedHit.Point, Is.EqualTo(linearHit.Point));
            AssertIndexedHover(lease, target, true);
            AssertIndexedLeaseStable(lease);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Projection_TargetOnlyMissSkipsCompleteFallbackUnlessNearestIsNeeded(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + new Vector3(3f, 0f, 6f), twoDimensions: twoDimensions);
            SetField(target, "checkObstacles", false);
            int foreignCount = InteractionHitProjector.DefaultIndexedMinimumHits + 2;
            for (int i = 0; i < foreignCount; i++)
            {
                CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * (2f + i * 0.5f));
            }

            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, foreignCount);
            int missCount = ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out long missWork);
            Assert.That(missCount, Is.Zero, "Target-only selection returns no foreign candidates.");
            Assert.That(missWork, Is.EqualTo(foreignCount), "Index build only; no N x H fallback for a camera miss.");
            Assert.That(InteractionQueryMath.TrySelectTarget(target.HitProjector.Hits, missCount, false, out _), Is.False);

            int nearestCount = ProjectForTest(lease, target, keepNonTargetsOnMiss: true, out long nearestWork);
            Assert.That(nearestWork, Is.EqualTo(foreignCount), "Index reused; the complete fallback inspects H entries.");
            Assert.That(nearestCount, Is.EqualTo(foreignCount));
            Assert.That(InteractionQueryMath.TryGetNearestHit(target.HitProjector.Hits, nearestCount,
                out InteractionRayHit nearest), Is.True);
            Assert.That(nearest.IsTarget, Is.False);
            Assert.That(nearest.Distance, Is.LessThan(3f), "The nearest foreign collider is the debug-ray end.");
            AssertIndexedHover(lease, target, false);
            AssertIndexedLeaseStable(lease);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IndexedProjection_ClearPathWalksOnlyTheHitsInFrontOfTheTarget(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 6f, twoDimensions: twoDimensions);
            ForceIndexedProjection(target);
            SetField(target, "checkObstacles", true);
            Component trigger = CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * 2f);
            SetIndexedTrigger(trigger, true);
            GameObject rig = CreateObject("IndexedViewRig");
            rig.transform.position = Origin;
            Component rigCollider = CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * 3f);
            rigCollider.transform.SetParent(rig.transform, true);
            SetField(target, "viewCheckPoint", rig.transform);
            Component wall = CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * 4f);
            for (int i = 0; i < 3; i++)
            {
                CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * (7f + i));
            }

            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, 7);
            AssertIndexedParity(lease, target, false, out long blockedWork);
            Assert.That(blockedWork, Is.EqualTo(25),
                "Index 7 + target 1 + distance order 7 + walk trigger/rig/wall 3, then the complete nearest fallback 7.");
            AssertIndexedHover(lease, target, false);

            SetIndexedTrigger(wall, true);
            AssertIndexedParity(lease, target, true, out long clearWork);
            Assert.That(clearWork, Is.EqualTo(4), "Target 1 + walk 3; the hits behind the target are never visited.");
            AssertIndexedHover(lease, target, true);

            SetField(target, "viewCheckPoint", null);
            AssertIndexedParity(lease, target, false, out long rigWork);
            Assert.That(rigWork, Is.EqualTo(10), "Target 1 + walk 2 (the rig now blocks) + complete fallback 7.");
            AssertIndexedHover(lease, target, false);
            AssertIndexedNativeControl(lease, 7, true);
            AssertIndexedLeaseStable(lease);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Projection_LargeTargetHierarchyUsesCompleteProjectionUntilTheHintExpires(bool twoDimensions)
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 9f, twoDimensions: twoDimensions);
            SetField(target, "checkObstacles", false);
            int foreignCount = InteractionHitProjector.DefaultIndexedMinimumHits + 1;
            for (int i = 0; i < foreignCount; i++)
            {
                CreateIndexedCollider(twoDimensions, Origin + Vector3.forward * (2f + i * 0.5f));
            }

            int rawHits = foreignCount + 1;
            List<GameObject> parts = new List<GameObject>();
            for (int i = 0; i < rawHits + 4; i++)
            {
                // WHY: Off-ray parts make the target hierarchy larger than the raw hit set without changing the lease.
                Component part = CreateIndexedCollider(twoDimensions, Origin + new Vector3(3f, 0f, 9f + i * 0.1f));
                part.transform.SetParent(target.transform, true);
                parts.Add(part.gameObject);
            }

            IndexedProjectionLease lease = WarmIndexedLease(twoDimensions, true, rawHits);
            AssertIndexedParity(lease, target, true, out long firstWork);
            Assert.That(firstWork, Is.EqualTo(rawHits + 1), "Unknown target size: index build H + one own entry.");
            AssertIndexedParity(lease, target, true, out long hintedWork);
            Assert.That(hintedWork, Is.EqualTo(rawHits), "More target colliders than raw hits: complete H checks.");
            AssertIndexedHover(lease, target, true);

            target.HitProjector.UseTargetSizeHint = false;
            ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out long forcedWork);
            Assert.That(forcedWork, Is.EqualTo(1), "Without the hint the existing index serves the own entry.");
            target.HitProjector.UseTargetSizeHint = true;

            foreach (GameObject part in parts)
            {
                Object.DestroyImmediate(part);
            }

            int completes = 0;
            int firstIndexedCall = -1;
            for (int i = 0; i < InteractionHitProjector.TargetSizeHintLifetime && firstIndexedCall < 0; i++)
            {
                ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out long work);
                if (work == 1)
                {
                    firstIndexedCall = i;
                }
                else
                {
                    Assert.That(work, Is.EqualTo(rawHits), "Until the hint expires the stale size picks the complete scan.");
                    completes++;
                }
            }

            Assert.That(firstIndexedCall, Is.EqualTo(InteractionHitProjector.TargetSizeHintLifetime - 1),
                "The hint is measured again after its lifetime, so a target that lost colliders returns to the index.");
            Assert.That(completes, Is.EqualTo(InteractionHitProjector.TargetSizeHintLifetime - 1));
            AssertIndexedParity(lease, target, true, out long recoveredWork);
            Assert.That(recoveredWork, Is.EqualTo(1));
            AssertIndexedHover(lease, target, true);
            AssertIndexedNativeControl(lease, rawHits, true);
            AssertIndexedLeaseStable(lease);
        }

        [Test]
        public void KeyboardLookMiss_DebugRayStillEndsAtNearestForeignHit()
        {
            InteractiveObject target = CreateTarget(Origin + new Vector3(3f, 0f, 6f), keyboard: true, hover: false);
            SetField(target, "checkObstacles", false);
            SetField(target, "viewCheckPoint", _camera.transform);
            target.InteractionDistance = 20f;
            int foreignCount = InteractionHitProjector.DefaultIndexedMinimumHits + 2;
            for (int i = 0; i < foreignCount; i++)
            {
                CreateIndexedCollider(false, Origin + Vector3.forward * (2f + i * 0.5f));
            }

            Physics.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            MethodInfo inView = typeof(InteractiveObject).GetMethod("IsInViewForKeyboardInteraction",
                BindingFlags.Instance | BindingFlags.NonPublic);

            InteractionPhysicsQueries.ResetCounters();
            Assert.That((bool)inView.Invoke(target, null), Is.False, "The target is off the look ray.");
            Assert.That(InteractionPhysicsQueries.Counters.ClassifiedHits, Is.EqualTo(foreignCount),
                "Without a recorded debug ray the miss needs no nearest non-target hit.");

            SetField(target, "drawDebugRay", true);
            InteractionPhysicsQueries.ResetCounters();
            Assert.That((bool)inView.Invoke(target, null), Is.False);
            Assert.That(InteractionPhysicsQueries.Counters.ClassifiedHits, Is.EqualTo(2L * foreignCount),
                "A recorded debug ray keeps the complete projection for its nearest end point.");
            Vector3 end = target.DebugRay.End;
            Assert.That(end.z, Is.EqualTo(Origin.z + 2f - 0.05f).Within(1e-3f),
                "The debug ray ends at the nearest foreign collider face.");
        }

        private sealed class IndexedProjectionLease
        {
            internal bool TwoDimensions;
            internal Ray Ray;
            internal QueryTriggerInteraction Triggers;
            internal InteractionPhysicsQueries.Hits3D Hits3D;
            internal InteractionPhysicsQueries.Hits2D Hits2D;
            internal int Frame;
            internal ulong Epoch;
            internal long Invalidations;
            internal long FrameAdvances;
            internal int Count => TwoDimensions ? Hits2D.Count : Hits3D.Count;
        }

        private static void ForceIndexedProjection(InteractiveObject target)
        {
            // WHY: These fixtures use a handful of raw hits and sometimes more target colliders than hits; force the
            // shared index, without the target-size hint, to test its exact parity.
            target.HitProjector.IndexedMinimumHits = 1;
            target.HitProjector.UseTargetSizeHint = false;
        }

        private IndexedProjectionLease WarmIndexedLease(bool twoDimensions, bool include3DTriggers, int expectedHits)
        {
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            InteractionPhysicsQueries.ResetCounters();
            Assert.That(_camera != null, Is.True);
            Assert.That(_camera.pixelWidth, Is.GreaterThan(0));
            Assert.That(_camera.pixelHeight, Is.GreaterThan(0));
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            IndexedProjectionLease lease = new IndexedProjectionLease
            {
                TwoDimensions = twoDimensions,
                Ray = ray,
                Triggers = include3DTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore
            };
            if (twoDimensions)
            {
                lease.Hits2D = InteractionPhysicsQueries.GetRayIntersection2D(_camera, ray, float.MaxValue, ~0,
                    InteractionPhysicsQueries.QueryMode.Shared);
                Assert.That(lease.Hits2D.IsComplete, Is.True);
            }
            else
            {
                lease.Hits3D = InteractionPhysicsQueries.Raycast3D(_camera, ray, float.MaxValue, ~0, lease.Triggers,
                    InteractionPhysicsQueries.QueryMode.Shared);
                Assert.That(lease.Hits3D.IsComplete, Is.True);
            }

            lease.Frame = Time.frameCount;
            lease.Epoch = InteractionPhysicsQueries.Epoch;
            lease.Invalidations = InteractionPhysicsQueries.Counters.Invalidations;
            lease.FrameAdvances = InteractionPhysicsQueries.Counters.FrameAdvances;
            Assert.That(lease.Count, Is.EqualTo(expectedHits).And.GreaterThan(0));
            AssertIndexedNativeControl(lease, expectedHits, true);
            AssertIndexedLeaseStable(lease);
            return lease;
        }

        private InteractionRayHit AssertIndexedParity(IndexedProjectionLease lease, InteractiveObject target,
            bool expectedSelection, out long inspectedWork)
        {
            bool obstacles = ReadIndexedField<bool>(target, "checkObstacles");
            int count = ProjectForTest(lease, target, keepNonTargetsOnMiss: true, out inspectedWork);
            InteractionRayHit[] projected = target.HitProjector.Hits;
            bool actual = InteractionQueryMath.TrySelectTarget(projected, count, obstacles, out InteractionRayHit actualHit);
            bool hasNearest = InteractionQueryMath.TryGetNearestHit(projected, count, out InteractionRayHit actualNearest);
            // WHY: Without the nearest-hit requirement a blocked or missed target keeps the indexed candidates
            // (targets plus the first blocker in front); their verdict and target hit must match too.
            int targetOnlyCount = ProjectForTest(lease, target, keepNonTargetsOnMiss: false, out _);
            bool targetOnly = InteractionQueryMath.TrySelectTarget(target.HitProjector.Hits, targetOnlyCount, obstacles,
                out InteractionRayHit targetOnlyHit);
            // WHY: Same raw lease + complete projection is the oracle; only the strategy switch differs.
            InteractionHitProjector projector = target.HitProjector;
            int strategy = projector.IndexedMinimumHits;
            try
            {
                projector.IndexedMinimumHits = int.MaxValue;
                int legacyCount = ProjectForTest(lease, target, keepNonTargetsOnMiss: true, out _);
                InteractionRayHit[] legacy = projector.Hits;
                bool expected = InteractionQueryMath.TrySelectTarget(legacy, legacyCount, obstacles,
                    out InteractionRayHit expectedHit);
                Assert.That(expected, Is.EqualTo(expectedSelection), "Native-positive/live legacy admission control.");
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(actualHit.Distance, Is.EqualTo(expectedHit.Distance));
                Assert.That(actualHit.Point, Is.EqualTo(expectedHit.Point));
                Assert.That(targetOnly, Is.EqualTo(expected), "Target-only candidates keep the clear-path verdict.");
                Assert.That(targetOnlyHit.Distance, Is.EqualTo(expectedHit.Distance));
                Assert.That(targetOnlyHit.Point, Is.EqualTo(expectedHit.Point));
                if (!actual)
                {
                    bool legacyNearest = InteractionQueryMath.TryGetNearestHit(legacy, legacyCount,
                        out InteractionRayHit expectedNearest);
                    Assert.That(hasNearest, Is.EqualTo(legacyNearest));
                    Assert.That(actualNearest.Distance, Is.EqualTo(expectedNearest.Distance));
                    Assert.That(actualNearest.Point, Is.EqualTo(expectedNearest.Point));
                    Assert.That(actualNearest.IsTarget, Is.EqualTo(expectedNearest.IsTarget));
                }
            }
            finally
            {
                projector.IndexedMinimumHits = strategy;
            }

            AssertIndexedLeaseStable(lease);
            return actualHit;
        }

        private static int ProjectForTest(IndexedProjectionLease lease, InteractiveObject target,
            bool keepNonTargetsOnMiss, out long inspectedWork)
        {
            bool obstacles = ReadIndexedField<bool>(target, "checkObstacles");
            long before = InteractionPhysicsQueries.Counters.ClassifiedHits;
            long passesBefore = InteractionPhysicsQueries.Counters.ClassificationPasses;
            InteractionTargetScope scope = target.TargetScope;
            int count = lease.TwoDimensions
                ? target.HitProjector.Project2D(lease.Hits2D, scope, false,
                    ReadIndexedField<bool>(target, "includeTriggerCollidersInMouseRaycast"), obstacles, keepNonTargetsOnMiss)
                : target.HitProjector.Project3D(lease.Hits3D, scope, false, obstacles, keepNonTargetsOnMiss);
            inspectedWork = InteractionPhysicsQueries.Counters.ClassifiedHits - before;
            Assert.That(InteractionPhysicsQueries.Counters.ClassificationPasses, Is.EqualTo(passesBefore + 1));
            return count;
        }

        private static T ReadIndexedField<T>(InteractiveObject target, string name)
        {
            return (T)typeof(InteractiveObject).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void AssertIndexedHover(IndexedProjectionLease lease, InteractiveObject target, bool expected)
        {
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.EqualTo(expected));
            AssertIndexedLeaseStable(lease);
        }

        private static void AssertIndexedLeaseStable(IndexedProjectionLease lease)
        {
            Assert.That(Time.frameCount, Is.EqualTo(lease.Frame));
            Assert.That(InteractionPhysicsQueries.Epoch, Is.EqualTo(lease.Epoch));
            Assert.That(lease.TwoDimensions ? lease.Hits2D.IsComplete : lease.Hits3D.IsComplete, Is.True);
            InteractionPhysicsQueries.Statistics counters = InteractionPhysicsQueries.Counters;
            Assert.That(counters.NativeCalls, Is.EqualTo(1));
            Assert.That(counters.FreshQueries + counters.CallbackBoundaries + counters.NativeCallsInsideCallbacks, Is.Zero);
            Assert.That(counters.Invalidations, Is.EqualTo(lease.Invalidations));
            Assert.That(counters.FrameAdvances, Is.EqualTo(lease.FrameAdvances));
        }

        private static void AssertIndexedNativeControl(IndexedProjectionLease lease, int expectedHits, bool compareRaw)
        {
            List<Component> nativeColliders = new List<Component>();
            List<InteractionRayHit> nativeHits = new List<InteractionRayHit>();
            if (lease.TwoDimensions)
            {
                RaycastHit2D[] array = new RaycastHit2D[32];
                int count = Physics2D.GetRayIntersectionNonAlloc(lease.Ray, array, float.MaxValue, ~0);
                RaycastHit2D[] all = Physics2D.GetRayIntersectionAll(lease.Ray, float.MaxValue, ~0);
                Assert.That(count, Is.LessThan(array.Length));
                Assert.That(all.Length, Is.EqualTo(count).And.EqualTo(expectedHits));
                for (int i = 0; i < count; i++)
                {
                    nativeColliders.Add(array[i].collider);
                    nativeHits.Add(new InteractionRayHit(array[i].distance, array[i].point, false, false));
                    Assert.That(System.Array.Exists(all, hit => hit.collider == array[i].collider &&
                        hit.distance == array[i].distance && hit.point == array[i].point), Is.True);
                }
            }
            else
            {
                RaycastHit[] array = new RaycastHit[32];
                int count = Physics.RaycastNonAlloc(lease.Ray, array, float.MaxValue, ~0, lease.Triggers);
                RaycastHit[] all = Physics.RaycastAll(lease.Ray, float.MaxValue, ~0, lease.Triggers);
                Assert.That(count, Is.LessThan(array.Length));
                Assert.That(all.Length, Is.EqualTo(count).And.EqualTo(expectedHits));
                for (int i = 0; i < count; i++)
                {
                    nativeColliders.Add(array[i].collider);
                    nativeHits.Add(new InteractionRayHit(array[i].distance, array[i].point, false, false));
                    Assert.That(System.Array.Exists(all, hit => hit.collider == array[i].collider &&
                        hit.distance == array[i].distance && hit.point == array[i].point), Is.True);
                }
            }

            if (!compareRaw)
            {
                return;
            }

            int liveRaw = 0;
            for (int i = 0; i < lease.Count; i++)
            {
                Component collider;
                float distance;
                Vector3 point;
                if (lease.TwoDimensions)
                {
                    RaycastHit2D hit = lease.Hits2D[i];
                    collider = hit.collider;
                    distance = hit.distance;
                    point = hit.point;
                }
                else
                {
                    RaycastHit hit = lease.Hits3D[i];
                    collider = hit.collider;
                    distance = hit.distance;
                    point = hit.point;
                }

                if (collider == null)
                {
                    continue;
                }

                liveRaw++;
                int nativeIndex = nativeColliders.IndexOf(collider);
                Assert.That(nativeIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(distance, Is.EqualTo(nativeHits[nativeIndex].Distance));
                Assert.That(point, Is.EqualTo(nativeHits[nativeIndex].Point));
            }

            Assert.That(liveRaw, Is.EqualTo(expectedHits));
        }

        private Component CreateIndexedCollider(bool twoDimensions, Vector3 position)
        {
            GameObject instance = CreateObject("IndexedNativeCollider");
            instance.transform.position = position;
            if (twoDimensions)
            {
                BoxCollider2D collider = instance.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one * 0.1f;
                return collider;
            }

            BoxCollider collider3D = instance.AddComponent<BoxCollider>();
            collider3D.size = Vector3.one * 0.1f;
            return collider3D;
        }

        private static Component IndexedPrimary(InteractiveObject target, bool twoDimensions)
        {
            return twoDimensions ? (Component)target.GetComponent<Collider2D>() : target.GetComponent<Collider>();
        }

        private static void SetIndexedEnabled(Component component, bool enabled)
        {
            if (component is Collider collider)
            {
                collider.enabled = enabled;
            }
            else
            {
                ((Collider2D)component).enabled = enabled;
            }
        }

        private static void SetIndexedTrigger(Component component, bool trigger)
        {
            if (component is Collider collider)
            {
                collider.isTrigger = trigger;
            }
            else
            {
                ((Collider2D)component).isTrigger = trigger;
            }
        }
    }
}
