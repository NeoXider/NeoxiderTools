using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.UI
{
    /// <summary>
    ///     Guards the per-frame cost of UI Mesh Rig: geometry buffers are reused by every adapter's rebuild
    ///     path, shared presets are cached, and the reused output matches a fresh build exactly.
    /// </summary>
    public sealed class UIMeshRigAllocationTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void Build_WithMatchingReuse_OverwritesTheSameInstanceWithFreshResults()
        {
            List<UIMeshRigPointState> points = CreatePoints();
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(new Vector2(-2f, -1f), new Vector2(4f, 2f));
            Rect uv = new Rect(0.1f, 0.2f, 0.5f, 0.6f);
            UIMeshRigGeometry fresh = UIMeshRigGeometryBuilder.Build(4, 3, space, uv, true, points);
            UIMeshRigGeometry reuse = UIMeshRigGeometryBuilder.Build(
                4, 3, new UIMeshRigCoordinateSpace(Vector2.zero, Vector2.one), new Rect(0f, 0f, 1f, 1f), false,
                null);

            UIMeshRigGeometry result = UIMeshRigGeometryBuilder.Build(4, 3, space, uv, true, points, reuse);

            Assert.That(result, Is.SameAs(reuse));
            CollectionAssert.AreEqual(fresh.Vertices, result.Vertices);
            CollectionAssert.AreEqual(fresh.UV, result.UV);
            CollectionAssert.AreEqual(fresh.Indices, result.Indices);
        }

        [Test]
        public void Build_ReuseWithSameSizesButTransposedGrid_RewritesTheTopology()
        {
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(Vector2.zero, Vector2.one);
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            UIMeshRigGeometry wide = UIMeshRigGeometryBuilder.Build(4, 3, space, uv, false, null);
            UIMeshRigGeometry tall = UIMeshRigGeometryBuilder.Build(3, 4, space, uv, false, null);

            UIMeshRigGeometry result = UIMeshRigGeometryBuilder.Build(3, 4, space, uv, false, null, wide);

            Assert.That(result, Is.SameAs(wide), "4x3 and 3x4 grids have identical array sizes.");
            CollectionAssert.AreEqual(tall.Indices, result.Indices);
            CollectionAssert.AreEqual(tall.Vertices, result.Vertices);
        }

        [Test]
        public void Build_WithMismatchedReuse_ReturnsNewGeometryAndLeavesReuseUntouched()
        {
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(Vector2.zero, Vector2.one);
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            UIMeshRigGeometry small = UIMeshRigGeometryBuilder.Build(2, 2, space, uv, false, null);
            Vector3[] snapshot = (Vector3[])small.Vertices.Clone();

            UIMeshRigGeometry result = UIMeshRigGeometryBuilder.Build(
                6, 5, new UIMeshRigCoordinateSpace(Vector2.one, Vector2.one), uv, false, null, small);

            Assert.That(result, Is.Not.SameAs(small));
            Assert.That(result.Vertices, Has.Length.EqualTo(7 * 6));
            CollectionAssert.AreEqual(snapshot, small.Vertices);
        }

        [Test]
        public void Build_WithReuse_DoesNotAllocate()
        {
            List<UIMeshRigPointState> points = CreatePoints();
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(Vector2.zero, new Vector2(300f, 200f));
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            UIMeshRigGeometry geometry = UIMeshRigGeometryBuilder.Build(16, 20, space, uv, true, points);
            UIMeshRigGeometryBuilder.Build(16, 20, space, uv, true, points, geometry);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int index = 0; index < 50; index++)
            {
                UIMeshRigGeometryBuilder.Build(16, 20, space, uv, true, points, geometry);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        [Test]
        public void SharedPresets_AreCachedAndEvaluateLikeFreshlyCreatedOnes()
        {
            UIMeshRigMotionProfile shared = UIMeshRigMotionPresets.GetShared(UIMeshRigMotionPreset.Wave);
            UIMeshRigMotionProfile created = UIMeshRigMotionPresets.Create(UIMeshRigMotionPreset.Wave);
            AnimationCurve sharedFalloff = UIMeshRigFalloffPresets.GetShared(UIMeshRigFalloffPreset.Smooth);
            AnimationCurve createdFalloff = UIMeshRigFalloffPresets.Create(UIMeshRigFalloffPreset.Smooth);

            Assert.That(UIMeshRigMotionPresets.GetShared(UIMeshRigMotionPreset.Wave), Is.SameAs(shared));
            Assert.That(UIMeshRigFalloffPresets.GetShared(UIMeshRigFalloffPreset.Smooth), Is.SameAs(sharedFalloff));
            Assert.That(created, Is.Not.SameAs(shared), "Create must keep returning an editable copy.");
            for (int step = 0; step <= 10; step++)
            {
                float time = step * 0.17f;
                UIMeshRigProceduralPose a = UIMeshRigMotionEvaluator.Evaluate(shared, time, 1f, 0.2f,
                    new Vector2(0.3f, 0.6f), 5);
                UIMeshRigProceduralPose b = UIMeshRigMotionEvaluator.Evaluate(created, time, 1f, 0.2f,
                    new Vector2(0.3f, 0.6f), 5);
                Assert.That(a.Position, Is.EqualTo(b.Position));
                Assert.That(a.RotationDegrees, Is.EqualTo(b.RotationDegrees));
                Assert.That(a.Scale, Is.EqualTo(b.Scale));
                Assert.That(sharedFalloff.Evaluate(step * 0.1f), Is.EqualTo(createdFalloff.Evaluate(step * 0.1f)));
            }
        }

        [Test]
        public void ToolkitElement_RepaintPath_MatchesFreshBuildAndDoesNotAllocate()
        {
            Sprite sprite = CreateSprite();
            UIMeshRigElement element = new UIMeshRigElement
            {
                Sprite = sprite,
                Columns = 12,
                Rows = 10,
                LayoutPreset = UIMeshRigLayoutPreset.FlagCloth,
                MotionEnabled = true
            };
            Vector2 size = new Vector2(240f, 160f);

            UIMeshRigGeometry fresh = element.BuildGeometry(size, 0.4f);
            UIMeshRigGeometry reused = element.BuildGeometryReusingBuffers(size, 0.4f);
            CollectionAssert.AreEqual(fresh.Vertices, reused.Vertices);
            CollectionAssert.AreEqual(fresh.Indices, reused.Indices);
            Assert.That(element.BuildGeometryReusingBuffers(size, 0.5f), Is.SameAs(reused));

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int frame = 0; frame < 30; frame++)
            {
                element.BuildGeometryReusingBuffers(size, frame * 0.016f);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L),
                "A playing UI Toolkit rig must not rebuild presets, curves or arrays every repaint.");
        }

        [Test]
        public void ToolkitElement_RepaintsOnATimerOnlyWhileThePoseDependsOnTime()
        {
            Sprite sprite = CreateSprite();
            UIMeshRigElement element = new UIMeshRigElement
            {
                Sprite = sprite,
                LayoutPreset = UIMeshRigLayoutPreset.Character,
                MotionEnabled = true,
                MotionSpeed = 1f
            };
            Vector2 size = new Vector2(200f, 200f);
            Assert.That(element.NeedsTimedRepaint, Is.True);

            element.MotionSpeed = 0f;
            Assert.That(element.NeedsTimedRepaint, Is.False,
                "Speed 0 draws one fixed pose; a 16 ms repaint would redraw identical geometry.");
            Vector3[] still = (Vector3[])element.BuildGeometry(size, 0.1f).Vertices.Clone();
            CollectionAssert.AreEqual(still, element.BuildGeometry(size, 3.7f).Vertices,
                "The skipped repaints are safe only because a speed-0 pose does not change over time.");

            element.MotionSpeed = float.NaN;
            Assert.That(element.NeedsTimedRepaint, Is.False);
            element.MotionSpeed = -0.5f;
            Assert.That(element.NeedsTimedRepaint, Is.True, "A negative speed still animates backwards.");

            element.DeformationEnabled = false;
            Assert.That(element.NeedsTimedRepaint, Is.False);
            element.DeformationEnabled = true;
            element.Sprite = null;
            Assert.That(element.NeedsTimedRepaint, Is.False);
            element.Sprite = sprite;
            element.MotionEnabled = false;
            Assert.That(element.NeedsTimedRepaint, Is.False);
        }

        [Test]
        public void GraphicPopulateMesh_WarmRebuild_DoesNotAllocate()
        {
            GameObject canvasObject = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            GameObject rigObject = Track(new GameObject(
                "Allocation Rig",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic)));
            rigObject.transform.SetParent(canvasObject.transform, false);
            ((RectTransform)rigObject.transform).sizeDelta = new Vector2(200f, 120f);
            UIMeshRigGraphic rig = rigObject.GetComponent<UIMeshRigGraphic>();
            rig.SetSource(CreateSprite(), Color.white);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.Character, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            MethodInfo method = typeof(UIMeshRigGraphic).GetMethod("OnPopulateMesh",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);
            Assert.That(method, Is.Not.Null);
            Action<VertexHelper> populate =
                (Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>), rig, method);
            VertexHelper vertexHelper = new VertexHelper();
            try
            {
                populate(vertexHelper);
                points[0].SetProceduralPose(new Vector2(3f, 1f), 4f, Vector2.one);
                populate(vertexHelper);

                using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
                allocationCounter.Begin();
                for (int frame = 0; frame < 20; frame++)
                {
                    points[0].SetProceduralPose(new Vector2(frame, 1f), frame, Vector2.one);
                    populate(vertexHelper);
                }

                long allocationCalls = allocationCounter.End();

                Assert.That(allocationCalls, Is.EqualTo(0L));
                Assert.That(vertexHelper.currentVertCount, Is.EqualTo(rig.BuildGeometry().Vertices.Length));
            }
            finally
            {
                vertexHelper.Dispose();
            }
        }

        [Test]
        public void WorldRendererPoseRebuild_UpdatesVerticesKeepsTopologyAndDoesNotAllocate()
        {
            GameObject rigObject = Track(new GameObject(
                "World Allocation Rig",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(UIMeshRigWorldRenderer)));
            UIMeshRigWorldRenderer rig = rigObject.GetComponent<UIMeshRigWorldRenderer>();
            rig.SetSource(CreateSprite(), Color.white);
            rig.SetGridResolution(6, 5);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.SimpleBounce, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            MethodInfo method = typeof(UIMeshRigWorldRenderer).GetMethod("RebuildIfNeeded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Action rebuild = (Action)Delegate.CreateDelegate(typeof(Action), rig, method);
            Mesh mesh = rigObject.GetComponent<MeshFilter>().sharedMesh;
            rebuild();
            int[] restTriangles = mesh.triangles;

            points[0].SetProceduralPose(new Vector2(25f, -10f), 12f, new Vector2(1.2f, 0.9f));
            rebuild();

            CollectionAssert.AreEqual(rig.BuildGeometry().Vertices, mesh.vertices);
            CollectionAssert.AreEqual(restTriangles, mesh.triangles);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int frame = 0; frame < 20; frame++)
            {
                points[0].SetProceduralPose(new Vector2(frame, 0f), frame, Vector2.one);
                rebuild();
            }

            long allocationCalls = allocationCounter.End();
            Assert.That(allocationCalls, Is.EqualTo(0L));

            rig.SetGridResolution(3, 2);
            rebuild();
            Assert.That(mesh.vertexCount, Is.EqualTo(4 * 3));
            Assert.That(mesh.triangles, Has.Length.EqualTo(3 * 2 * 6));
        }

        [Test]
        public void WorldRendererPoseRebuild_KeepsTheUvsAndASpriteSwapOnTheSameGridReuploadsThem()
        {
            GameObject rigObject = Track(new GameObject(
                "World UV Rig",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(UIMeshRigWorldRenderer)));
            UIMeshRigWorldRenderer rig = rigObject.GetComponent<UIMeshRigWorldRenderer>();
            Sprite full = CreateSprite();
            rig.SetSource(full, Color.white);
            rig.SetGridResolution(4, 3);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.SimpleBounce, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            MethodInfo method = typeof(UIMeshRigWorldRenderer).GetMethod("RebuildIfNeeded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Action rebuild = (Action)Delegate.CreateDelegate(typeof(Action), rig, method);
            Mesh mesh = rigObject.GetComponent<MeshFilter>().sharedMesh;
            rebuild();

            for (int frame = 1; frame <= 3; frame++)
            {
                points[0].SetProceduralPose(new Vector2(6f * frame, 2f), 4f * frame, Vector2.one);
                rebuild();
            }

            CollectionAssert.AreEqual(rig.BuildGeometry().UV, mesh.uv, "Pose-only rebuilds keep the uploaded UVs.");
            CollectionAssert.AreEqual(rig.BuildGeometry().Vertices, mesh.vertices);

            Texture2D texture = new Texture2D(8, 4);
            _objects.Add(texture);
            Sprite half = Sprite.Create(texture, new Rect(4f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            _objects.Add(half);
            rig.SetSource(half, Color.white);
            rebuild();

            Vector2[] expected = rig.BuildGeometry().UV;
            Assert.That(expected[expected.Length - 1].x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(expected[0].x, Is.EqualTo(0.5f).Within(0.0001f),
                "The right half of the texture starts at u = 0.5.");
            CollectionAssert.AreEqual(expected, mesh.uv,
                "A sprite with another outer UV on the same grid must re-upload the UVs.");
        }

        [Test]
        public void SpriteRendererPoseRebuild_DoesNotAllocate()
        {
            GameObject rigObject = Track(new GameObject(
                "Sprite Allocation Rig",
                typeof(SpriteRenderer),
                typeof(UIMeshRigSpriteRenderer)));
            UIMeshRigSpriteRenderer rig = rigObject.GetComponent<UIMeshRigSpriteRenderer>();
            rig.SetSource(CreateSprite(), Color.white);
            rig.SetGridResolution(8, 6);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.Character, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            rig.Rebuild();
            points[0].SetProceduralPose(new Vector2(4f, 2f), 3f, Vector2.one);
            rig.Rebuild();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int frame = 0; frame < 20; frame++)
            {
                points[0].SetProceduralPose(new Vector2(frame, 1f), frame, Vector2.one);
                rig.Rebuild();
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L),
                "An animated sprite rig must rewrite its clone without managed allocations.");
        }

        [Test]
        public void OwnerResolution_FromADeepPoint_DoesNotAllocate()
        {
            GameObject rigObject = Track(new GameObject(
                "Owner Allocation Rig",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(UIMeshRigWorldRenderer)));
            Transform parent = rigObject.transform;
            for (int depth = 0; depth < 6; depth++)
            {
                GameObject level = new GameObject("Level " + depth);
                level.transform.SetParent(parent, false);
                parent = level.transform;
            }

            UIMeshRigWorldRenderer owner = rigObject.GetComponent<UIMeshRigWorldRenderer>();
            Assert.That(UIMeshRigOwnerResolver.Find(parent), Is.SameAs(owner));
            IUIMeshRigOwner found = null;

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int lookup = 0; lookup < 100; lookup++)
            {
                found = UIMeshRigOwnerResolver.Find(parent);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(found, Is.SameAs(owner));
            Assert.That(allocationCalls, Is.EqualTo(0L),
                "The nearest-owner walk must not allocate a component array per ancestor.");
        }

        private static List<UIMeshRigPointState> CreatePoints()
        {
            return new List<UIMeshRigPointState>
            {
                new UIMeshRigPointState(
                    true,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.1f, 0.1f),
                    new Vector2(0.45f, 0.4f),
                    1f,
                    AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                    new Vector2(0.3f, 0.2f),
                    15f,
                    new Vector2(1.1f, 0.9f)),
                new UIMeshRigPointState(
                    true,
                    new Vector2(0.2f, 0.8f),
                    Vector2.zero,
                    new Vector2(0.3f, 0.3f),
                    0.7f,
                    null,
                    new Vector2(-0.4f, 0.1f),
                    -8f,
                    Vector2.one)
            };
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(8, 4);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 4f), new Vector2(0.5f, 0.5f));
            _objects.Add(texture);
            _objects.Add(sprite);
            return sprite;
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }
    }
}
