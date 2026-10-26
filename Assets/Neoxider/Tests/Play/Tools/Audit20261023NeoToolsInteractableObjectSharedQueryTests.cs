using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public class Audit20261023NeoToolsInteractableObjectSharedQueryTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<GameObject> _previousMainCameras = new();
        private static readonly Vector3 Origin = new(2800f, 2800f, 0f);
        private Camera _camera;
        private RenderTexture _texture;
        private bool _globalTriggers2D;
        private bool _startInside2D;
        private bool _globalTriggers3D;

        [SetUp]
        public void SetUp()
        {
            _globalTriggers2D = Physics2D.queriesHitTriggers;
            _startInside2D = Physics2D.queriesStartInColliders;
            _globalTriggers3D = Physics.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
            Physics2D.queriesStartInColliders = true;
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.CompareTag("MainCamera"))
                {
                    _previousMainCameras.Add(camera.gameObject);
                    camera.gameObject.tag = "Untagged";
                }
            }

            GameObject cameraObject = CreateObject("SharedQueryCamera", Origin);
            cameraObject.tag = "MainCamera";
            _camera = cameraObject.AddComponent<Camera>();
            _texture = new RenderTexture(256, 256, 0);
            _camera.targetTexture = _texture;
            cameraObject.AddComponent<InteractionRayProvider>().Mode = InteractionRayProvider.RayMode.ScreenCenter;
            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
        }

        [TearDown]
        public void TearDown()
        {
            InteractionPhysicsQueries.Invalidate();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            if (_texture != null)
            {
                _texture.Release();
                Object.DestroyImmediate(_texture);
            }

            foreach (GameObject cameraObject in _previousMainCameras)
            {
                if (cameraObject != null)
                {
                    cameraObject.tag = "MainCamera";
                }
            }

            _previousMainCameras.Clear();
            Physics2D.queriesHitTriggers = _globalTriggers2D;
            Physics2D.queriesStartInColliders = _startInside2D;
            Physics.queriesHitTriggers = _globalTriggers3D;
            InteractionPhysicsQueries.Invalidate();
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void AutomaticCameraPass_OneRawQueryForAllConsumers(int targetCount)
        {
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            List<InteractiveObject> targets = new();
            for (int i = 0; i < targetCount; i++)
            {
                targets.Add(CreateTarget(selected));
            }

            Physics.SyncTransforms();
            foreach (InteractiveObject target in targets)
            {
                target.SendMessage("Update");
            }

            // Start a stable warmed epoch. Target-specific range LOS is tested separately below.
            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            foreach (InteractiveObject target in targets)
            {
                target.SendMessage("Update");
                Assert.That(target.IsHovered, Is.True);
            }

            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.CameraNativeCalls, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.CacheHits, Is.EqualTo(targetCount - 1));
        }

        [Test]
        public void AutomaticUpdate_PreservesDistinctRangeLosWithoutRepeatingCameraQueries()
        {
            List<InteractiveObject> targets = new();
            for (int i = -1; i <= 1; i++)
            {
                Collider selected = CreateCollider3D(Origin + new Vector3(i * 2f, 0f, 5f));
                InteractiveObject target = CreateTarget(selected);
                target.DistanceCheckPoint = _camera.transform;
                target.InteractionDistance = 10f;
                targets.Add(target);
            }

            Physics.SyncTransforms();
            foreach (InteractiveObject target in targets)
            {
                target.SendMessage("Update");
            }

            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            foreach (InteractiveObject target in targets)
            {
                target.SendMessage("Update");
            }

            Assert.That(InteractionPhysicsQueries.Counters.CameraNativeCalls, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.RangeNativeCalls, Is.GreaterThanOrEqualTo(3));
            Assert.That(targets[0].IsHovered, Is.False);
            Assert.That(targets[1].IsHovered, Is.True);
            Assert.That(targets[2].IsHovered, Is.False);
        }

        [Test]
        public void TargetLocalObstaclePolicies_ShareRawHitsAndKeepDifferentAnswers()
        {
            CreateCollider3D(Origin + Vector3.forward * 2f);
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            InteractiveObject blocked = CreateTarget(selected);
            InteractiveObject throughWall = CreateTarget(selected);
            SetField(throughWall, "checkObstacles", false);
            Physics.SyncTransforms();
            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();

            blocked.SendMessage("UpdateMouseTargetRaycast");
            throughWall.SendMessage("UpdateMouseTargetRaycast");

            Assert.That(blocked.IsHovered, Is.False);
            Assert.That(throughWall.IsHovered, Is.True);
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(1));
        }

        [Test]
        public void ActualMasksAndSubEpsilonGeometry_AreDistinctQueries()
        {
            Collider first = CreateCollider3D(Origin + Vector3.forward * 3f);
            Collider second = CreateCollider3D(Origin + Vector3.forward * 5f);
            second.gameObject.layer = 1;
            Physics.SyncTransforms();
            Ray ray = new(Origin, Vector3.forward);
            InteractionPhysicsQueries.Hits3D firstLayer = Shared3D(ray, 1);
            InteractionPhysicsQueries.Hits3D secondLayer = Shared3D(ray, 2);

            Assert.That(firstLayer.Count, Is.EqualTo(1));
            Assert.That(firstLayer[0].collider, Is.SameAs(first));
            Assert.That(secondLayer.Count, Is.EqualTo(1));
            Assert.That(secondLayer[0].collider, Is.SameAs(second));
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(2));

            Ray nearZero = new(new Vector3(0f, 0f, -5000f), Vector3.forward);
            Shared3D(nearZero, 1);
            Ray slightlyShifted = new(new Vector3(0.000001f, 0f, -5000f), Vector3.forward);
            Shared3D(slightlyShifted, 1);
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(4),
                "The key must compare exact float components rather than approximate Vector3 equality.");
        }

        [Test]
        public void DifferentCamerasAndViewport_KeepSeparateContexts()
        {
            CreateCollider3D(Origin + Vector3.forward * 5f);
            Physics.SyncTransforms();
            Ray ray = new(Origin, Vector3.forward);
            Shared3D(ray);
            Camera second = CreateObject("SecondSharedCamera", Origin).AddComponent<Camera>();
            InteractionPhysicsQueries.Raycast3D(second, ray, 20f, ~0, QueryTriggerInteraction.Collide,
                InteractionPhysicsQueries.QueryMode.Shared);
            _camera.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            Shared3D(ray);

            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(3));
        }

        [Test]
        public void DistinctCenterAndPointerGeometry_UseTwoQueriesForRepeatedConsumers()
        {
            CreateCollider3D(Origin + Vector3.forward * 5f);
            Physics.SyncTransforms();
            Ray center = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Ray pointer = _camera.ViewportPointToRay(new Vector3(0.9f, 0.5f, 0f));
            for (int i = 0; i < 100; i++)
            {
                Assert.That(Shared3D(center).Count, Is.EqualTo(1));
                Assert.That(Shared3D(pointer).Count, Is.Zero);
            }

            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(2));
        }

        [Test]
        public void Global2DTriggers_AndExplicitContactFilter_KeepTheirOwnSemantics()
        {
            Collider2D trigger = CreateCollider2D(Origin + Vector3.right * 3f);
            trigger.isTrigger = true;
            List<Collider2D> colliders = new() { trigger };
            Physics2D.SyncTransforms();
            Vector2 origin = new(Origin.x, Origin.y);
            Physics2D.queriesHitTriggers = false;
            InteractionPhysicsQueries.Hits2D excluded = InteractionPhysicsQueries.Raycast2D(_camera, origin,
                Vector2.right, 20f, ~0, InteractionPhysicsQueries.QueryMode.Shared);
            Assert.That(excluded.Count, Is.Zero);
            ContactFilter2D filter = new() { useTriggers = true, useLayerMask = true, layerMask = ~0 };
            // Establish the exact native contract under global=false before inferring a filter override.
            List<RaycastHit2D> reference = ReadDirectFilteredHits(colliders, filter, null);
            AssertPlanarParity(reference, origin, Vector2.right, 20f, true, filter);
            Physics2D.queriesHitTriggers = true;
            InteractionPhysicsQueries.Hits2D included = InteractionPhysicsQueries.Raycast2D(_camera, origin,
                Vector2.right, 20f, ~0, InteractionPhysicsQueries.QueryMode.Shared);

            Assert.That(included.Count, Is.EqualTo(1));
            Assert.That(included[0].collider, Is.SameAs(trigger));
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts2D, Is.EqualTo(2));
            Assert.That(InteractionPhysicsQueries.Counters.FilteredRaycasts2D, Is.EqualTo(1));

            foreach (bool globalTriggers in new[] { false, true })
            {
                Physics2D.queriesHitTriggers = globalTriggers;
                foreach (bool filterTriggers in new[] { false, true })
                {
                    filter.useTriggers = filterTriggers;
                    // Keep hard trigger-positive/negative controls; the disputed combination uses native parity.
                    int? expected = !filterTriggers ? 0 : globalTriggers ? 1 : (int?)null;
                    AssertExplicitFilterColdAndWarm(colliders, filter, expected);
                }
            }

            trigger.isTrigger = false;
            Physics2D.SyncTransforms();
            Physics2D.queriesHitTriggers = false;
            filter.useTriggers = false;
            AssertExplicitFilterColdAndWarm(colliders, filter, 1); // Real solid hit with both trigger flags off.
            filter.useTriggers = true;
            AssertExplicitFilterColdAndWarm(colliders, filter, 1);
            filter.layerMask = 0;
            AssertExplicitFilterColdAndWarm(colliders, filter, 0);
            filter.useLayerMask = false;
            AssertExplicitFilterColdAndWarm(colliders, filter, 1);
        }

        [Test]
        public void Explicit3DTriggerPolicies_AreIndependentOfTheGlobalFlag()
        {
            Collider trigger = CreateCollider3D(Origin + Vector3.forward * 3f);
            trigger.isTrigger = true;
            Physics.SyncTransforms();
            Physics.queriesHitTriggers = false;
            Ray ray = new(Origin, Vector3.forward);
            InteractionPhysicsQueries.Hits3D included = Shared3D(ray);
            InteractionPhysicsQueries.Hits3D excluded = InteractionPhysicsQueries.Raycast3D(_camera, ray,
                20f, ~0, QueryTriggerInteraction.Ignore, InteractionPhysicsQueries.QueryMode.Shared);
            Physics.queriesHitTriggers = true;
            InteractionPhysicsQueries.Hits3D includedAgain = Shared3D(ray);

            Assert.That(included.Count, Is.EqualTo(1));
            Assert.That(excluded.Count, Is.Zero);
            Assert.That(includedAgain[0].collider, Is.SameAs(trigger));
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(2));
        }

        [Test]
        public void Global2DStartInsidePolicy_IsPartOfTheQueryContext()
        {
            Collider2D collider = CreateCollider2D(Origin);
            Physics2D.SyncTransforms();
            Vector2 origin = new(Origin.x, Origin.y);
            Physics2D.queriesStartInColliders = false;
            InteractionPhysicsQueries.Hits2D excluded = InteractionPhysicsQueries.Raycast2D(_camera,
                origin, Vector2.right, 20f, ~0, InteractionPhysicsQueries.QueryMode.Shared);
            Physics2D.queriesStartInColliders = true;
            InteractionPhysicsQueries.Hits2D included = InteractionPhysicsQueries.Raycast2D(_camera,
                origin, Vector2.right, 20f, ~0, InteractionPhysicsQueries.QueryMode.Shared);

            Assert.That(excluded.Count, Is.Zero);
            Assert.That(included.Count, Is.EqualTo(1));
            Assert.That(included[0].collider, Is.SameAs(collider));
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts2D, Is.EqualTo(2));
        }

        [Test]
        public void DisabledRenderingCamera_PreservesDirectRaysAndCachedTargetGeometry()
        {
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            InteractiveObject target = CreateTarget(selected);
            InteractionRayProvider provider = _camera.GetComponent<InteractionRayProvider>();
            Physics.SyncTransforms();
            Assert.That(provider.TryGetHoverRay(out Ray enabledHover), Is.True);
            Assert.That(provider.TryGetClickRay(out Ray enabledClick), Is.True);
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.True);

            _camera.enabled = false;
            InteractionRayProvider.InvalidateQueries();
            Assert.That(provider.TryGetHoverRay(out Ray disabledHover), Is.True);
            Assert.That(provider.TryGetClickRay(out Ray disabledClick), Is.True);
            Assert.That(disabledHover.origin, Is.EqualTo(enabledHover.origin));
            Assert.That(disabledHover.direction, Is.EqualTo(enabledHover.direction));
            Assert.That(disabledClick.origin, Is.EqualTo(enabledClick.origin));
            Assert.That(disabledClick.direction, Is.EqualTo(enabledClick.direction));
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.True, "Cached geometry does not require enabled rendering.");

            _camera.pixelRect = new Rect(0f, 0f, 0f, 0f);
            Assert.That(provider.TryGetHoverRay(out _), Is.False);
            Assert.That(provider.TryGetClickRay(out _), Is.False);
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.False);

            Object.DestroyImmediate(_camera.gameObject);
            Assert.That(InteractionRayProvider.FindOnMainCamera(), Is.Null);
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.False);
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(false, 3)]
        [TestCase(false, 4)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        [TestCase(true, 3)]
        [TestCase(true, 4)]
        public void PlanarOriginalArguments_MatchDirectNativeColdAndWarm(bool explicitFilter, int directionCase)
        {
            Vector2 origin = new(Origin.x, Origin.y);
            Vector2 direction;
            float distance = 20f;
            switch (directionCase)
            {
                case 0: direction = Vector2.right; break;
                case 1: direction = new Vector2(7f, 0f); break;
                case 2: direction = new Vector2(1e-6f, 0f); break;
                case 3: direction = Vector2.zero; break;
                default:
                    // The LOS caller projects a normalized 3D displacement without normalizing XY again.
                    GameObject checkpoint = CreateObject("LargeZCheckpoint", Origin);
                    GameObject endpoint = CreateObject("LargeZEndpoint", Origin + new Vector3(0.1f, 0f, 100000f));
                    Vector3 displacement = endpoint.transform.position - checkpoint.transform.position;
                    Vector3 normalized = displacement.normalized;
                    direction = new Vector2(normalized.x, normalized.y);
                    distance = InteractionQueryMath.GetObstacleCheckDistance(displacement.magnitude);
                    break;
            }

            for (int i = 0; i < 17; i++)
            {
                Vector3 position = directionCase == 3 ? Origin : Origin + Vector3.right * (1f + i * 0.3f);
                CreateCollider2D(position);
            }

            Physics2D.SyncTransforms();
            ContactFilter2D filter = new() { useLayerMask = true, layerMask = ~0, useTriggers = true };
            RaycastHit2D[] referenceBuffer = new RaycastHit2D[32];
            // Original native overloads, with enough capacity to make their result demonstrably complete.
            int referenceCount = explicitFilter
                ? Physics2D.Raycast(origin, direction, filter, referenceBuffer, distance)
                : Physics2D.RaycastNonAlloc(origin, direction, referenceBuffer, distance, ~0);
            Assert.That(referenceCount, Is.LessThan(referenceBuffer.Length));
            List<RaycastHit2D> reference = new();
            for (int i = 0; i < referenceCount; i++)
            {
                reference.Add(referenceBuffer[i]);
            }

            if (directionCase <= 1)
            {
                Assert.That(reference.Count, Is.EqualTo(17), "Unit/non-unit positive controls must see real colliders.");
            }

            InteractionPhysicsQueries.ReleaseBuffers();
            InteractionPhysicsQueries.ResetCounters();
            AssertPlanarParity(reference, origin, direction, distance, explicitFilter, filter);
            long coldCalls = reference.Count >= 16 ? 2L : 1L;
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls, Is.EqualTo(coldCalls));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.EqualTo(coldCalls - 1));
            AssertPlanarParity(reference, origin, direction, distance, explicitFilter, filter);
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls, Is.EqualTo(coldCalls));
            Assert.That(InteractionPhysicsQueries.Counters.CacheHits, Is.EqualTo(1));

            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            AssertPlanarParity(reference, origin, direction, distance, explicitFilter, filter);
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.Zero);
        }

        [Test]
        public void RangeLargeZSmallProjection_MatchesOriginalFilteredNativeColdAndWarm()
        {
            GameObject checkpoint = CreateObject("RangeLargeZCheckpoint", Origin - Vector3.forward * 100000f);
            Collider2D selected = CreateCollider2D(Origin + Vector3.right * 0.1f);
            BoxCollider2D nearWall = (BoxCollider2D)CreateCollider2D(Origin + Vector3.right * 0.025f);
            nearWall.size = Vector2.one * 0.005f;
            for (int i = 0; i < 17; i++)
            {
                CreateCollider2D(Origin + Vector3.right * (1f + i * 0.3f));
            }

            InteractiveObject target = CreateTarget(selected);
            target.DistanceCheckPoint = checkpoint.transform;
            target.InteractionDistance = 200000f;
            Physics2D.SyncTransforms();
            Vector3 displacement = selected.bounds.center - checkpoint.transform.position;
            Vector3 normalized = displacement.normalized;
            Vector2 direction = new(normalized.x, normalized.y);
            Assert.That(Mathf.Abs(displacement.z), Is.GreaterThan(99999f));
            Assert.That(direction.x, Is.GreaterThan(0f).And.LessThan(1e-5f));
            Vector2 origin = new(checkpoint.transform.position.x, checkpoint.transform.position.y);
            float distance = InteractionQueryMath.GetObstacleCheckDistance(displacement.magnitude);
            ContactFilter2D filter = new() { useLayerMask = true, layerMask = ~0, useTriggers = false };
            RaycastHit2D[] reference = new RaycastHit2D[64];
            int count = Physics2D.Raycast(origin, direction, filter, reference, distance);
            Assert.That(count, Is.LessThan(reference.Length));
            InteractionRayHit[] candidates = new InteractionRayHit[count];
            for (int i = 0; i < count; i++)
            {
                bool isTarget = reference[i].collider == selected;
                candidates[i] = new InteractionRayHit(reference[i].distance, reference[i].point, isTarget, !isTarget);
            }

            bool expected = !InteractionQueryMath.TryGetNearestHit(candidates, count, out InteractionRayHit nearest) ||
                            nearest.IsTarget;
            InteractionPhysicsQueries.ReleaseBuffers();
            InteractionPhysicsQueries.ResetCounters();
            Assert.That(target.IsInInteractionRange, Is.EqualTo(expected));
            Assert.That(InteractionPhysicsQueries.Counters.FilteredRaycasts2D, Is.EqualTo(count >= 16 ? 2 : 1));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.EqualTo(count >= 16 ? 1 : 0));
            InteractionPhysicsQueries.ResetCounters();
            Assert.That(target.IsInInteractionRange, Is.EqualTo(expected));
            Assert.That(InteractionPhysicsQueries.Counters.FilteredRaycasts2D, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.Zero);
        }

        [Test]
        public void PlanarOriginalDirection_IsPartOfTheExactKey()
        {
            CreateCollider2D(Origin + Vector3.right * 3f);
            Physics2D.SyncTransforms();
            Vector2 origin = new(Origin.x, Origin.y);
            InteractionPhysicsQueries.Raycast2D(_camera, origin, Vector2.right, 20f, ~0,
                InteractionPhysicsQueries.QueryMode.Shared);
            InteractionPhysicsQueries.Raycast2D(_camera, origin, new Vector2(7f, 0f), 20f, ~0,
                InteractionPhysicsQueries.QueryMode.Shared);
            InteractionPhysicsQueries.Raycast2D(_camera, origin, new Vector2(1e-6f, 0f), 20f, ~0,
                InteractionPhysicsQueries.QueryMode.Shared);
            InteractionPhysicsQueries.Raycast2D(_camera, origin, Vector2.zero, 20f, ~0,
                InteractionPhysicsQueries.QueryMode.Shared);
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts2D, Is.EqualTo(4));
        }

        private void AssertPlanarParity(List<RaycastHit2D> reference, Vector2 origin, Vector2 direction,
            float distance, bool explicitFilter, ContactFilter2D filter)
        {
            InteractionPhysicsQueries.Hits2D actual = explicitFilter
                ? InteractionPhysicsQueries.Raycast2D(_camera, origin, direction, distance, filter,
                    InteractionPhysicsQueries.QueryMode.Shared)
                : InteractionPhysicsQueries.Raycast2D(_camera, origin, direction, distance, ~0,
                    InteractionPhysicsQueries.QueryMode.Shared);
            Assert.That(actual.IsComplete, Is.True);
            Assert.That(actual.Count, Is.EqualTo(reference.Count));
            Dictionary<Collider2D, RaycastHit2D> expectedByCollider = new();
            foreach (RaycastHit2D hit in reference)
            {
                expectedByCollider.Add(hit.collider, hit);
            }

            for (int i = 0; i < actual.Count; i++)
            {
                RaycastHit2D hit = actual[i];
                Assert.That(expectedByCollider.TryGetValue(hit.collider, out RaycastHit2D expected), Is.True);
                Assert.That(hit.distance, Is.EqualTo(expected.distance).Within(1e-5f));
                Assert.That(Vector2.Distance(hit.point, expected.point), Is.LessThanOrEqualTo(1e-5f));
                expectedByCollider.Remove(hit.collider);
            }

            Assert.That(expectedByCollider, Is.Empty);
        }

        [TestCase(17, 0)]
        [TestCase(64, 0)]
        [TestCase(1000, 0)]
        [TestCase(17, 1)]
        [TestCase(64, 1)]
        [TestCase(1000, 1)]
        [TestCase(17, 2)]
        [TestCase(64, 2)]
        [TestCase(1000, 2)]
        [TestCase(17, 3)]
        [TestCase(64, 3)]
        [TestCase(1000, 3)]
        public void DenseQuery_ReturnsEveryColliderAndReusesCapacity(int hitCount, int queryKind)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Vector3 position = Origin + (queryKind < 2 ? Vector3.forward : Vector3.right) * (1f + i * 0.3f);
                if (queryKind == 0)
                {
                    CreateCollider3D(position);
                }
                else
                {
                    CreateCollider2D(position);
                }
            }

            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.ReleaseBuffers();
            InteractionPhysicsQueries.ResetCounters();
            AssertDenseResult(hitCount, queryKind);
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls, Is.EqualTo(2));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls,
                Is.EqualTo(1L + InteractionPhysicsQueries.Counters.OverflowFallbacks));

            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            AssertDenseResult(hitCount, queryKind);
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.Zero);
        }

        [Test]
        public void DenseExplicit2DFilter_RetainsTriggerPolicyDuringFullFallback()
        {
            List<Collider2D> colliders = new();
            for (int i = 0; i < 17; i++)
            {
                Collider2D collider = CreateCollider2D(Origin + Vector3.right * (1f + i * 0.3f));
                collider.isTrigger = true;
                colliders.Add(collider);
            }

            Physics2D.SyncTransforms();
            ContactFilter2D filter = new() { useLayerMask = true, layerMask = ~0, useTriggers = true };
            foreach (bool globalTriggers in new[] { false, true })
            {
                Physics2D.queriesHitTriggers = globalTriggers;
                filter.useTriggers = true;
                AssertExplicitFilterColdAndWarm(colliders, filter, globalTriggers ? 17 : (int?)null);
                filter.useTriggers = false;
                AssertExplicitFilterColdAndWarm(colliders, filter, 0);
            }

            foreach (Collider2D collider in colliders)
            {
                collider.isTrigger = false;
            }

            Physics2D.SyncTransforms();
            Physics2D.queriesHitTriggers = false;
            AssertExplicitFilterColdAndWarm(colliders, filter, 17); // Full fallback also works without any trigger allowance.
            filter.layerMask = 0;
            AssertExplicitFilterColdAndWarm(colliders, filter, 0);
            filter.useLayerMask = false;
            AssertExplicitFilterColdAndWarm(colliders, filter, 17);
        }

        private List<RaycastHit2D> ReadDirectFilteredHits(List<Collider2D> colliders, ContactFilter2D filter,
            int? expectedCount)
        {
            Vector2 origin = new(Origin.x, Origin.y);
            RaycastHit2D[] buffer = new RaycastHit2D[colliders.Count + 1];
            int arrayCount = Physics2D.Raycast(origin, Vector2.right, filter, buffer, 20f);
            List<RaycastHit2D> complete = new();
            int listCount = Physics2D.Raycast(origin, Vector2.right, filter, complete, 20f);
            string context = $"Direct 2D filter: globalTriggers={Physics2D.queriesHitTriggers}, " +
                $"startInside={Physics2D.queriesStartInColliders}, filterTriggers={filter.useTriggers}, " +
                $"useLayerMask={filter.useLayerMask}, mask={filter.layerMask.value}, " +
                $"array={arrayCount}/{buffer.Length}, list={listCount}.";
            TestContext.WriteLine(context);
            Assert.That(arrayCount, Is.LessThan(buffer.Length), "The direct reference must be complete. " + context);
            Assert.That(listCount, Is.EqualTo(arrayCount), context);
            Assert.That(complete.Count, Is.EqualTo(listCount), context);
            if (expectedCount.HasValue)
            {
                Assert.That(arrayCount, Is.EqualTo(expectedCount.Value), context);
            }

            Dictionary<Collider2D, RaycastHit2D> expected = new();
            for (int i = 0; i < arrayCount; i++)
            {
                RaycastHit2D hit = buffer[i];
                Assert.That(colliders.Contains(hit.collider), Is.True, "Only owned ray targets may appear. " + context);
                expected.Add(hit.collider, hit);
            }

            foreach (RaycastHit2D hit in complete)
            {
                Assert.That(expected.TryGetValue(hit.collider, out RaycastHit2D arrayHit), Is.True, context);
                Assert.That(hit.distance, Is.EqualTo(arrayHit.distance).Within(1e-5f), context);
                Assert.That(Vector2.Distance(hit.point, arrayHit.point), Is.LessThanOrEqualTo(1e-5f), context);
                expected.Remove(hit.collider);
            }

            Assert.That(expected, Is.Empty, context);
            return complete;
        }

        private void AssertExplicitFilterColdAndWarm(List<Collider2D> colliders, ContactFilter2D filter,
            int? expectedCount)
        {
            bool globalTriggers = Physics2D.queriesHitTriggers;
            List<RaycastHit2D> reference = ReadDirectFilteredHits(colliders, filter, expectedCount);
            Vector2 origin = new(Origin.x, Origin.y);
            InteractionPhysicsQueries.ReleaseBuffers();
            InteractionPhysicsQueries.ResetCounters();
            AssertPlanarParity(reference, origin, Vector2.right, 20f, true, filter);
            Assert.That(InteractionPhysicsQueries.Counters.FilteredRaycasts2D, Is.EqualTo(reference.Count >= 16 ? 2 : 1));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.EqualTo(reference.Count >= 16 ? 1 : 0));

            InteractionPhysicsQueries.ResetCounters();
            AssertPlanarParity(reference, origin, Vector2.right, 20f, true, filter);
            Assert.That(InteractionPhysicsQueries.Counters.NativeCalls, Is.Zero, "An unchanged complete context is cached.");
            Assert.That(InteractionPhysicsQueries.Counters.CacheHits, Is.EqualTo(1));

            InteractionPhysicsQueries.Invalidate();
            InteractionPhysicsQueries.ResetCounters();
            AssertPlanarParity(reference, origin, Vector2.right, 20f, true, filter);
            Assert.That(InteractionPhysicsQueries.Counters.FilteredRaycasts2D, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.OverflowFallbacks, Is.Zero);
            Assert.That(Physics2D.queriesHitTriggers, Is.EqualTo(globalTriggers), "The backend must not change global policy.");
        }

        [TestCase(false, 17)]
        [TestCase(false, 64)]
        [TestCase(false, 1000)]
        [TestCase(true, 17)]
        [TestCase(true, 64)]
        [TestCase(true, 1000)]
        public void DenseTargetAndBlockerSelection_MatchesCompleteUncachedPhysics(bool twoDimensions, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = Origin + Vector3.forward * (1f + i * 0.3f);
                if (twoDimensions)
                {
                    CreateCollider2D(position).isTrigger = true;
                }
                else
                {
                    CreateCollider3D(position).isTrigger = true;
                }
            }

            Component selected = twoDimensions
                ? CreateCollider2D(Origin + Vector3.forward * (count * 0.3f + 3f))
                : CreateCollider3D(Origin + Vector3.forward * (count * 0.3f + 3f));
            Component wall = twoDimensions
                ? CreateCollider2D(Origin + Vector3.forward * 0.5f)
                : CreateCollider3D(Origin + Vector3.forward * 0.5f);
            InteractiveObject target = CreateTarget(selected);
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.False);
            Assert.That(target.IsHovered, Is.EqualTo(UncachedAcceptsTarget(selected, twoDimensions)));

            if (wall is Collider wall3D)
            {
                wall3D.enabled = false;
            }
            else if (wall is Collider2D wall2D)
            {
                wall2D.enabled = false;
            }

            Physics.SyncTransforms();
            Physics2D.SyncTransforms();
            InteractionPhysicsQueries.Invalidate();
            target.SendMessage("UpdateMouseTargetRaycast");
            Assert.That(target.IsHovered, Is.True, "Foreign triggers must not hide the target beyond the old capacity.");
            Assert.That(target.IsHovered, Is.EqualTo(UncachedAcceptsTarget(selected, twoDimensions)));
        }

        [Test]
        public void PublicRangeGetter_IsFreshAfterAnUnannouncedBlockerChange()
        {
            Collider wall = CreateCollider3D(Origin + Vector3.forward * 2f);
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            InteractiveObject target = CreateTarget(selected);
            target.DistanceCheckPoint = _camera.transform;
            target.InteractionDistance = 10f;
            Physics.SyncTransforms();
            Assert.That(target.IsInInteractionRange, Is.False);

            wall.enabled = false;
            Physics.SyncTransforms();

            Assert.That(target.IsInInteractionRange, Is.True);
            Assert.That(InteractionPhysicsQueries.Counters.FreshQueries, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ExplicitInvalidation_ExpiresBorrowedHitsAndSeesNewGeometry()
        {
            Collider collider = CreateCollider3D(Origin + Vector3.forward * 5f);
            Physics.SyncTransforms();
            Ray ray = new(Origin, Vector3.forward);
            InteractionPhysicsQueries.Hits3D borrowed = Shared3D(ray);
            Assert.That(borrowed.Count, Is.EqualTo(1));
            collider.enabled = false;
            Physics.SyncTransforms();

            InteractionRayProvider.InvalidateQueries();
            InteractionPhysicsQueries.Hits3D next = Shared3D(ray);

            Assert.That(borrowed.IsValid, Is.False);
            Assert.That(borrowed.IsComplete, Is.False);
            Assert.That(next.IsComplete, Is.True);
            Assert.That(next.Count, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => { _ = borrowed[0]; });
        }

        [Test]
        public void CallbackMutationAndException_EndTheEpochBeforeAndAfterDispatch()
        {
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            InteractiveObject target = CreateTarget(selected);
            Physics.SyncTransforms();
            Ray ray = new(Origin, Vector3.forward);
            InteractionPhysicsQueries.Hits3D before = Shared3D(ray);
            int insideCount = -1;
            target.onHoverEnter = new UnityEvent();
            target.onHoverEnter.AddListener(() =>
            {
                CreateCollider3D(Origin + Vector3.forward * 2f);
                Physics.SyncTransforms();
                insideCount = Shared3D(ray).Count;
                throw new InvalidOperationException("Expected observer failure");
            });

            Assert.Throws<InvalidOperationException>(() =>
                target.OnPointerEnter(new PointerEventData(EventSystem.current)));
            InteractionPhysicsQueries.Hits3D after = Shared3D(ray);

            Assert.That(before.IsValid, Is.False);
            Assert.That(insideCount, Is.EqualTo(2));
            Assert.That(after.Count, Is.EqualTo(2));
            Assert.That(InteractionPhysicsQueries.Counters.NativeCallsInsideCallbacks, Is.EqualTo(1));
        }

        [Test]
        public void HoverCallbackAddingAWall_InvalidatesTheCachedClickHit()
        {
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            InteractiveObject target = CreateTarget(selected);
            target.onHoverEnter = new UnityEvent();
            target.onClick = new UnityEvent();
            int clicks = 0;
            target.onClick.AddListener(() => clicks++);
            target.onHoverEnter.AddListener(() =>
            {
                CreateCollider3D(Origin + Vector3.forward * 2f);
                Physics.SyncTransforms();
            });
            Physics.SyncTransforms();
            target.SendMessage("UpdateMouseTargetRaycast");

            target.OnPointerClick(new PointerEventData(EventSystem.current));

            Assert.That(clicks, Is.Zero, "The pre-callback hit cannot authorize a click through the new wall.");
        }

        [Test]
        public void UniqueLosDirections_RemainDifferentQueries()
        {
            Collider selected = CreateCollider3D(Origin + Vector3.forward * 5f);
            CreateCollider3D(Origin + Vector3.forward * 2f);
            Physics.SyncTransforms();
            InteractionPhysicsQueries.Hits3D blocked = Shared3D(new Ray(Origin, Vector3.forward));
            InteractionPhysicsQueries.Hits3D clear = Shared3D(new Ray(Origin + Vector3.right * 2f,
                selected.bounds.center - (Origin + Vector3.right * 2f)));

            Assert.That(blocked.Count, Is.EqualTo(2));
            Assert.That(clear.Count, Is.EqualTo(1));
            Assert.That(clear[0].collider, Is.SameAs(selected));
            Assert.That(InteractionPhysicsQueries.Counters.Raycasts3D, Is.EqualTo(2));
        }

        private void AssertDenseResult(int expected, int kind)
        {
            Ray ray = new(Origin, Vector3.forward);
            if (kind == 0)
            {
                InteractionPhysicsQueries.Hits3D hits = Shared3D(ray, ~0, 1000f);
                HashSet<Collider> colliders = new();
                Assert.That(hits.IsComplete, Is.True);
                for (int i = 0; i < hits.Count; i++)
                {
                    colliders.Add(hits[i].collider);
                }

                Assert.That(hits.Count, Is.EqualTo(expected));
                Assert.That(colliders.Count, Is.EqualTo(expected));
                return;
            }

            InteractionPhysicsQueries.Hits2D hits2D;
            Vector2 origin = new(Origin.x, Origin.y);
            if (kind == 1)
            {
                hits2D = InteractionPhysicsQueries.GetRayIntersection2D(_camera, ray, 1000f, ~0,
                    InteractionPhysicsQueries.QueryMode.Shared);
            }
            else if (kind == 2)
            {
                hits2D = InteractionPhysicsQueries.Raycast2D(_camera, origin, Vector2.right, 1000f, ~0,
                    InteractionPhysicsQueries.QueryMode.Shared);
            }
            else
            {
                ContactFilter2D filter = new() { useLayerMask = true, layerMask = ~0, useTriggers = true };
                hits2D = InteractionPhysicsQueries.Raycast2D(_camera, origin, Vector2.right, 1000f, filter,
                    InteractionPhysicsQueries.QueryMode.Shared);
            }

            HashSet<Collider2D> colliders2D = new();
            Assert.That(hits2D.IsComplete, Is.True);
            for (int i = 0; i < hits2D.Count; i++)
            {
                colliders2D.Add(hits2D[i].collider);
            }

            Assert.That(hits2D.Count, Is.EqualTo(expected));
            Assert.That(colliders2D.Count, Is.EqualTo(expected));
        }

        private InteractionPhysicsQueries.Hits3D Shared3D(Ray ray, int mask = ~0, float distance = 20f)
        {
            return InteractionPhysicsQueries.Raycast3D(_camera, ray, distance, mask,
                QueryTriggerInteraction.Collide, InteractionPhysicsQueries.QueryMode.Shared);
        }

        private bool UncachedAcceptsTarget(Component selected, bool twoDimensions)
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            List<InteractionRayHit> complete = new();
            if (twoDimensions)
            {
                RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, float.MaxValue, ~0);
                foreach (RaycastHit2D hit in hits)
                {
                    bool isTarget = hit.collider == selected;
                    complete.Add(new InteractionRayHit(hit.distance, hit.point, isTarget,
                        !isTarget && !hit.collider.isTrigger));
                }
            }
            else
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, float.MaxValue, ~0, QueryTriggerInteraction.Collide);
                foreach (RaycastHit hit in hits)
                {
                    bool isTarget = hit.collider == selected;
                    complete.Add(new InteractionRayHit(hit.distance, hit.point, isTarget,
                        !isTarget && !hit.collider.isTrigger));
                }
            }

            return InteractionQueryMath.TrySelectTarget(complete.ToArray(), complete.Count, true, out _);
        }

        private InteractiveObject CreateTarget(Component collider)
        {
            GameObject instance = CreateObject("SharedConsumer", collider.transform.position);
            instance.SetActive(false);
            InteractiveObject target = instance.AddComponent<InteractiveObject>();
            JsonUtility.FromJsonOverwrite("{\"_autoCheckEventSystem\":false,\"_autoCreateEventSystemIfMissing\":false}", target);
            SetField(target, collider is Collider ? "targetCollider3D" : "targetCollider2D", collider);
            target.UseScreenCenterRay = true;
            target.UseKeyboardInteraction = false;
            target.DistanceCheckPoint = null;
            target.onHoverEnter = null;
            target.onHoverExit = null;
            target.onHoverChanged = null;
            target.onEnterRange = null;
            target.onExitRange = null;
            target.onInteractDown = null;
            target.onInteractUp = null;
            target.onClick = null;
            target.onDoubleClick = null;
            target.onRightClick = null;
            target.onMiddleClick = null;
            instance.SetActive(true);
            target.DistanceCheckPoint = null;
            target.InvalidateCachedColliders();
            return target;
        }

        private static void SetField(InteractiveObject target, string name, object value)
        {
            typeof(InteractiveObject).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private Collider CreateCollider3D(Vector3 position)
        {
            BoxCollider collider = CreateObject("SharedCollider3D", position).AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.1f;
            return collider;
        }

        private Collider2D CreateCollider2D(Vector3 position)
        {
            BoxCollider2D collider = CreateObject("SharedCollider2D", position).AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.1f;
            return collider;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject instance = new(name);
            instance.transform.position = position;
            _objects.Add(instance);
            return instance;
        }
    }
}
