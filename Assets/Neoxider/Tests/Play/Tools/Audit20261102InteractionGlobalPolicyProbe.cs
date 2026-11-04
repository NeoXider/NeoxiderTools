using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Same-frame global physics policy changes must not reuse a stale shared query: the cache key carries
    ///     every global flag the native query depends on (regression for filtered 2D and queriesHitTriggers).
    /// </summary>
    public sealed class Audit20261102InteractionGlobalPolicyProbe
    {
        private static readonly Vector3 Origin = new Vector3(9200f, 9200f, 0f);

        [UnityTest]
        public IEnumerator Filtered2D_GlobalTrueToFalse_SameFrameMatchesActualNative()
        {
            return CheckFilteredTransition(true, false);
        }

        [UnityTest]
        public IEnumerator Filtered2D_GlobalFalseToTrue_SameFrameMatchesActualNative()
        {
            return CheckFilteredTransition(false, true);
        }

        private static IEnumerator CheckFilteredTransition(bool firstFlag, bool secondFlag)
        {
            bool previousTriggers = Physics2D.queriesHitTriggers;
            bool previousStartInside = Physics2D.queriesStartInColliders;
            GameObject owned = new GameObject("OwnedSameEpochFilteredPolicyProbe");
            try
            {
                Collider2D trigger = Add2D(owned.transform, "trigger", 3f, true);
                Collider2D solid = Add2D(owned.transform, "solid", 5f, false);
                Physics2D.queriesStartInColliders = true;
                Physics2D.SyncTransforms();
                yield return null;

                ContactFilter2D filter = new ContactFilter2D
                {
                    useTriggers = true,
                    useLayerMask = true,
                    layerMask = ~0
                };
                Vector2 origin = new Vector2(Origin.x, Origin.y);
                // WHY: Non-empty native controls first, so an empty result can never pass as parity.
                Physics2D.queriesHitTriggers = true;
                List<RaycastHit2D> allowed = ReadNative(origin, filter, trigger, solid);
                Assert.That(allowed.Count, Is.EqualTo(2));
                filter.useTriggers = false;
                Assert.That(ReadNative(origin, filter, trigger, solid).Count, Is.EqualTo(1));
                filter.useTriggers = true;

                Physics2D.queriesHitTriggers = firstFlag;
                List<RaycastHit2D> expectedA = ReadNative(origin, filter, trigger, solid);
                // WHY: Buffers are released only before A; a release between A and B would hide a stale entry.
                InteractionPhysicsQueries.ReleaseBuffers();
                InteractionPhysicsQueries.ResetCounters();
                int frame = Time.frameCount;
                ulong beforeEpoch = InteractionPhysicsQueries.Epoch;
                InteractionPhysicsQueries.Hits2D a = InteractionPhysicsQueries.Raycast2D(null, origin,
                    Vector2.right, 10f, filter, InteractionPhysicsQueries.QueryMode.Shared);
                AssertMatches(a, expectedA);
                // WHY: Read A before the policy change, which may legitimately expire its epoch.
                int actualA = a.Count;
                InteractionPhysicsQueries.Statistics afterA = InteractionPhysicsQueries.Counters;
                ulong epochA = InteractionPhysicsQueries.Epoch;
                Assert.That(afterA.NativeCalls, Is.EqualTo(1));
                AssertSharedOnly(frame, afterA);

                Physics2D.queriesHitTriggers = secondFlag;
                List<RaycastHit2D> expectedB = ReadNative(origin, filter, trigger, solid);
                InteractionPhysicsQueries.Statistics beforeB = InteractionPhysicsQueries.Counters;
                InteractionPhysicsQueries.Hits2D b = InteractionPhysicsQueries.Raycast2D(null, origin,
                    Vector2.right, 10f, filter, InteractionPhysicsQueries.QueryMode.Shared);
                int actualB = b.Count;
                InteractionPhysicsQueries.Statistics afterB = InteractionPhysicsQueries.Counters;
                ulong epochB = InteractionPhysicsQueries.Epoch;
                TestContext.WriteLine("Filtered A->B receipt before parity assertion: frame=" + frame
                    + ", mode=Shared/requestFresh=false, expected=" + expectedA.Count + "->" + expectedB.Count
                    + ", actual=" + actualA + "->" + actualB + ", epochs=" + beforeEpoch + "/" + epochA + "/" + epochB
                    + ", afterA={" + Trace(afterA) + "}, beforeB={" + Trace(beforeB) + "}, afterB={" + Trace(afterB) + "}");
                AssertSharedOnly(frame, afterB);
                Assert.That(afterB.NativeCalls, Is.InRange(afterA.NativeCalls, afterA.NativeCalls + 1));
                // WHY: A global policy change may invalidate an internal epoch, so epochA == epochB is not demanded;
                // parity and the following unchanged-B reuse are the public contract.
                AssertMatches(b, expectedB);

                // WHY: No reset, invalidation, policy change or frame boundary between B and B again.
                InteractionPhysicsQueries.Hits2D repeatedB = InteractionPhysicsQueries.Raycast2D(null, origin,
                    Vector2.right, 10f, filter, InteractionPhysicsQueries.QueryMode.Shared);
                InteractionPhysicsQueries.Statistics afterRepeatedB = InteractionPhysicsQueries.Counters;
                ulong epochRepeatedB = InteractionPhysicsQueries.Epoch;
                TestContext.WriteLine("Filtered unchanged-B reuse receipt before assertions: epoch=" + epochRepeatedB
                    + ", counters={" + Trace(afterRepeatedB) + "}, previousB={" + Trace(afterB) + "}");
                Assert.That(repeatedB.IsValid, Is.True, "The current returned B view must be valid.");
                AssertMatches(repeatedB, expectedB);
                AssertSharedOnly(frame, afterRepeatedB);
                Assert.That(epochRepeatedB, Is.EqualTo(epochB), "Unchanged B->B has no policy/frame/callback boundary.");
                Assert.That(afterRepeatedB.FrameAdvances, Is.EqualTo(afterB.FrameAdvances));
                Assert.That(afterRepeatedB.Invalidations, Is.EqualTo(afterB.Invalidations));
                Assert.That(afterRepeatedB.NativeCalls, Is.EqualTo(afterB.NativeCalls),
                    "An unchanged Shared B context must not perform another native cast.");
                Assert.That(afterRepeatedB.CacheHits, Is.EqualTo(afterB.CacheHits + 1),
                    "Parity alone must not accept an always-fresh implementation.");
                TestContext.WriteLine("Filtered same-frame policy: global " + firstFlag + "->" + secondFlag
                    + ", filter.useTriggers=true, mode=Shared/requestFresh=false, native counts=" + expectedA.Count + "->" + expectedB.Count
                    + ", shared counts=" + actualA + "->" + actualB + "->" + repeatedB.Count
                    + ", epochs before/A/B/repeatedB=" + beforeEpoch + "/" + epochA + "/" + epochB + "/" + epochRepeatedB
                    + ", afterA={" + Trace(afterA) + "}, beforeB={" + Trace(beforeB) + "}, afterB={" + Trace(afterB)
                    + "}, afterRepeatedB={" + Trace(afterRepeatedB) + "}, frame=" + frame);
                Assert.That(Time.frameCount, Is.EqualTo(frame), "A and B must be in the same real frame.");
                Assert.That(Physics2D.queriesHitTriggers, Is.EqualTo(secondFlag));
                // WHY: Nothing assumes an explicit filter overrides the global flag on this Unity version. When the
                // native results differ this catches stale shared data; when they agree the check proves nothing.
                Assert.That(expectedB.Exists(hit => ReferenceEquals(hit.collider, solid)), Is.True);
            }
            finally
            {
                Physics2D.queriesHitTriggers = previousTriggers;
                Physics2D.queriesStartInColliders = previousStartInside;
                Object.DestroyImmediate(owned);
                Physics2D.SyncTransforms();
                InteractionPhysicsQueries.ReleaseBuffers();
                InteractionPhysicsQueries.ResetCounters();
            }
        }

        [UnityTest]
        public IEnumerator Explicit3DCollide_GlobalToggleRemainsNativeEquivalent()
        {
            bool previousTriggers = Physics.queriesHitTriggers;
            GameObject owned = new GameObject("OwnedExplicit3DPolicyControl");
            try
            {
                owned.transform.position = Origin + Vector3.forward * 4f;
                BoxCollider collider = owned.AddComponent<BoxCollider>();
                collider.size = Vector3.one * 0.25f;
                collider.isTrigger = true;
                Physics.SyncTransforms();
                yield return null;
                Ray ray = new Ray(Origin, Vector3.forward);
                Physics.queriesHitTriggers = false;
                RaycastHit[] expectedA = Physics.RaycastAll(ray, 10f, ~0, QueryTriggerInteraction.Collide);
                Assert.That(expectedA.Length, Is.EqualTo(1));
                Assert.That(expectedA[0].collider, Is.SameAs(collider));
                InteractionPhysicsQueries.ReleaseBuffers();
                InteractionPhysicsQueries.ResetCounters();
                int frame = Time.frameCount;
                ulong beforeEpoch = InteractionPhysicsQueries.Epoch;
                InteractionPhysicsQueries.Hits3D a = InteractionPhysicsQueries.Raycast3D(null, ray, 10f, ~0,
                    QueryTriggerInteraction.Collide, InteractionPhysicsQueries.QueryMode.Shared);
                Assert.That(a.IsComplete, Is.True);
                Assert.That(a.Count, Is.EqualTo(1));
                InteractionPhysicsQueries.Statistics afterA = InteractionPhysicsQueries.Counters;
                ulong epochA = InteractionPhysicsQueries.Epoch;
                Assert.That(afterA.NativeCalls, Is.EqualTo(1));
                AssertSharedOnly(frame, afterA);
                Physics.queriesHitTriggers = true;
                RaycastHit[] expectedB = Physics.RaycastAll(ray, 10f, ~0, QueryTriggerInteraction.Collide);
                InteractionPhysicsQueries.Hits3D b = InteractionPhysicsQueries.Raycast3D(null, ray, 10f, ~0,
                    QueryTriggerInteraction.Collide, InteractionPhysicsQueries.QueryMode.Shared);
                Assert.That(Time.frameCount, Is.EqualTo(frame));
                Assert.That(expectedB.Length, Is.EqualTo(1));
                Assert.That(b.Count, Is.EqualTo(expectedB.Length));
                Assert.That(b[0].collider, Is.SameAs(collider));
                InteractionPhysicsQueries.Statistics afterB = InteractionPhysicsQueries.Counters;
                ulong epochB = InteractionPhysicsQueries.Epoch;
                AssertSharedOnly(frame, afterB);
                Assert.That(afterB.NativeCalls, Is.InRange(afterA.NativeCalls, afterA.NativeCalls + 1));
                InteractionPhysicsQueries.Hits3D repeatedB = InteractionPhysicsQueries.Raycast3D(null, ray, 10f, ~0,
                    QueryTriggerInteraction.Collide, InteractionPhysicsQueries.QueryMode.Shared);
                InteractionPhysicsQueries.Statistics afterRepeatedB = InteractionPhysicsQueries.Counters;
                ulong epochRepeatedB = InteractionPhysicsQueries.Epoch;
                TestContext.WriteLine("3D unchanged-B receipt before assertions: frame=" + frame
                    + ", epochs before/A/B/repeatedB=" + beforeEpoch + "/" + epochA + "/" + epochB + "/" + epochRepeatedB
                    + ", A={" + Trace(afterA) + "}, B={" + Trace(afterB) + "}, repeatedB={" + Trace(afterRepeatedB) + "}");
                Assert.That(repeatedB.IsValid, Is.True);
                Assert.That(repeatedB.IsComplete, Is.True);
                Assert.That(repeatedB.Count, Is.EqualTo(1));
                Assert.That(repeatedB[0].collider, Is.SameAs(collider));
                AssertSharedOnly(frame, afterRepeatedB);
                Assert.That(epochRepeatedB, Is.EqualTo(epochB));
                Assert.That(afterRepeatedB.FrameAdvances, Is.EqualTo(afterB.FrameAdvances));
                Assert.That(afterRepeatedB.Invalidations, Is.EqualTo(afterB.Invalidations));
                Assert.That(afterRepeatedB.NativeCalls, Is.EqualTo(afterB.NativeCalls));
                Assert.That(afterRepeatedB.CacheHits, Is.EqualTo(afterB.CacheHits + 1));
                TestContext.WriteLine("3D explicit Collide: mode=Shared/requestFresh=false, frame=" + frame
                    + ", epochs before/A/B/repeatedB=" + beforeEpoch + "/" + epochA + "/" + epochB + "/" + InteractionPhysicsQueries.Epoch
                    + ", afterA={" + Trace(afterA) + "}, afterB={" + Trace(afterB) + "}, repeatedB={" + Trace(afterRepeatedB) + "}");
            }
            finally
            {
                Physics.queriesHitTriggers = previousTriggers;
                Object.DestroyImmediate(owned);
                Physics.SyncTransforms();
                InteractionPhysicsQueries.ReleaseBuffers();
                InteractionPhysicsQueries.ResetCounters();
            }
        }

        private static void AssertSharedOnly(int frame, InteractionPhysicsQueries.Statistics counters)
        {
            Assert.That(Time.frameCount, Is.EqualTo(frame), "No fixture yield/frame boundary is allowed across A/B/B.");
            Assert.That(counters.FreshQueries, Is.Zero, "All probe requests explicitly use Shared, not Fresh.");
            Assert.That(counters.CallbackBoundaries + counters.NativeCallsInsideCallbacks, Is.Zero);
        }

        private static string Trace(InteractionPhysicsQueries.Statistics counters)
        {
            return "native=" + counters.NativeCalls + ",cache=" + counters.CacheHits
                + ",fresh=" + counters.FreshQueries + ",invalidations=" + counters.Invalidations
                + ",frameAdvances=" + counters.FrameAdvances + ",callbacks=" + counters.CallbackBoundaries;
        }

        private static Collider2D Add2D(Transform parent, string name, float distance, bool trigger)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            child.transform.position = Origin + Vector3.right * distance;
            BoxCollider2D collider = child.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.25f;
            collider.isTrigger = trigger;
            return collider;
        }

        private static List<RaycastHit2D> ReadNative(Vector2 origin, ContactFilter2D filter,
            Collider2D trigger, Collider2D solid)
        {
            RaycastHit2D[] buffer = new RaycastHit2D[8];
            int count = Physics2D.Raycast(origin, Vector2.right, filter, buffer, 10f);
            List<RaycastHit2D> complete = new List<RaycastHit2D>(8);
            int listCount = Physics2D.Raycast(origin, Vector2.right, filter, complete, 10f);
            Assert.That(count, Is.LessThan(buffer.Length), "The native array reference must be complete.");
            Assert.That(listCount, Is.EqualTo(count));
            Assert.That(listCount, Is.GreaterThanOrEqualTo(1), "The solid is a mandatory positive control.");
            foreach (RaycastHit2D hit in complete)
            {
                Assert.That(ReferenceEquals(hit.collider, trigger) || ReferenceEquals(hit.collider, solid), Is.True);
                int index = Array.FindIndex(buffer, 0, count, item => ReferenceEquals(item.collider, hit.collider));
                Assert.That(index, Is.GreaterThanOrEqualTo(0));
                Assert.That(hit.distance, Is.EqualTo(buffer[index].distance).Within(1e-5f));
                Assert.That(Vector2.Distance(hit.point, buffer[index].point), Is.LessThanOrEqualTo(1e-5f));
            }
            return complete;
        }

        private static void AssertMatches(InteractionPhysicsQueries.Hits2D actual, List<RaycastHit2D> expected)
        {
            Assert.That(actual.IsComplete, Is.True);
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < actual.Count; i++)
            {
                RaycastHit2D hit = actual[i];
                int index = expected.FindIndex(item => ReferenceEquals(item.collider, hit.collider));
                Assert.That(index, Is.GreaterThanOrEqualTo(0));
                Assert.That(hit.distance, Is.EqualTo(expected[index].distance).Within(1e-5f));
                Assert.That(Vector2.Distance(hit.point, expected[index].point), Is.LessThanOrEqualTo(1e-5f));
            }
        }
    }
}
