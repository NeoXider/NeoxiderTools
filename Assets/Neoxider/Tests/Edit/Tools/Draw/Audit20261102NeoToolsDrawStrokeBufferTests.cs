using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Stroke rebuild contracts of <see cref="Drawer" />: the non-allocating smoothing/limiting forms must match the
    ///     historical allocating algorithms exactly, a warm stroke must not allocate, oversized buffers are released,
    ///     and the live collider must follow the renderer without per-frame rebuilds.
    /// </summary>
    public sealed class Audit20261102NeoToolsDrawStrokeBufferTests
    {
        private const int RetainedBufferLimit = 16384;
        private const int OversizedCapacity = RetainedBufferLimit + 1024;
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void SmoothIntoBuffer_MatchesTheHistoricalChaikinOutputExactly(
            [Values(0, 1, 2, 3, 4)] int passes,
            [Values(0, 1, 2, 3, 4, 7)] int count,
            [Values(false, true)] bool flattenZ)
        {
            List<Vector3> points = CreatePolyline(count);
            float? fixedZ = flattenZ ? 3.5f : (float?)null;
            List<Vector3> reference = ReferenceSmooth(points, passes, fixedZ);
            List<Vector3> results = new() { Vector3.one * 99f };

            Drawer.Smooth(points, passes, fixedZ, results);

            CollectionAssert.AreEqual(reference, results);
            CollectionAssert.AreEqual(reference, Drawer.Smooth(points, passes, fixedZ));
            CollectionAssert.AreEqual(CreatePolyline(count), points, "The source polyline must stay untouched.");
        }

        [Test]
        public void SmoothIntoBuffer_SupportsInPlaceSmoothing()
        {
            List<Vector3> points = CreatePolyline(6);
            List<Vector3> reference = ReferenceSmooth(points, 3, 1f);

            Drawer.Smooth(points, 3, 1f, points);

            CollectionAssert.AreEqual(reference, points);
        }

        [Test]
        public void LimitPointsIntoBuffer_MatchesTheHistoricalSamplingInPlaceAndCopied(
            [Values(0, 1, 2, 3, 5, 9)] int max,
            [Values(0, 1, 2, 5, 9, 40)] int count)
        {
            List<Vector3> points = CreatePolyline(count);
            List<Vector3> reference = ReferenceLimit(points, max);
            List<Vector3> copied = new() { Vector3.one * 99f };
            List<Vector3> inPlace = new(points);

            Drawer.LimitPoints(points, max, copied);
            Drawer.LimitPoints(inPlace, max, inPlace);

            CollectionAssert.AreEqual(reference, copied);
            CollectionAssert.AreEqual(reference, inPlace);
            CollectionAssert.AreEqual(reference, Drawer.LimitPoints(points, max));
        }

        [Test]
        public void AppendPoint_WarmStrokeWithSmoothingLimitAndLiveCollider_DoesNotAllocate()
        {
            Drawer drawer = CreateDrawer();
            drawer.smoothing = 2;
            drawer.maxPoints = 64;
            drawer.addCollider = true;
            drawer.colliderAfterCreation = false;
            drawer.BeginLine(Vector3.zero);
            TestDelegate stroke = () =>
            {
                drawer.ResetPoints();
                AppendStroke(drawer, 200);
            };

            // WHY: the same delegate runs once before the measurement so the grow-only buffers and the first-call
            // costs of the freshly compiled lambda are not attributed to the warm stroke.
            stroke();

            // WHY: Unity's Boehm GC reports 0 from GC.GetAllocatedBytesForCurrentThread, so allocations are measured
            // through the GC.Alloc profiler recorder behind this constraint.
            Assert.That(stroke, Is.Not.AllocatingGCMemory(),
                "A warm stroke rebuild must reuse its smoothing and collider buffers.");
            Assert.That(drawer.CountPoints, Is.EqualTo(200));
            Assert.That(CurrentLine(drawer).positionCount, Is.EqualTo(64));
        }

        [Test]
        public void OversizedBuffers_AreReleasedWhenTheStrokeIsReset()
        {
            Drawer drawer = CreateDrawer();
            drawer.BeginLine(Vector3.zero);
            List<Vector3> processed = PrivateField<List<Vector3>>(drawer, "_processedPoints");
            List<Vector2> colliderPoints = PrivateField<List<Vector2>>(drawer, "_colliderPoints");
            processed.Capacity = OversizedCapacity;
            colliderPoints.Capacity = OversizedCapacity;
            drawer.rawPoints.Capacity = OversizedCapacity;
            SetPrivateField(drawer, "_cachedPositionsArray", new Vector3[OversizedCapacity]);
            SetPrivateField(drawer, "_rendererPositions", new Vector3[OversizedCapacity]);

            drawer.ResetPoints();

            Assert.That(processed.Capacity, Is.LessThanOrEqualTo(RetainedBufferLimit));
            Assert.That(colliderPoints.Capacity, Is.LessThanOrEqualTo(RetainedBufferLimit));
            Assert.That(drawer.rawPoints.Capacity, Is.LessThanOrEqualTo(RetainedBufferLimit));
            Assert.That(PrivateField<Vector3[]>(drawer, "_cachedPositionsArray"), Is.Null);
            Assert.That(PrivateField<Vector3[]>(drawer, "_rendererPositions"), Is.Null);

            drawer.AppendPoint(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            Assert.That(CurrentLine(drawer).positionCount, Is.EqualTo(2), "Released buffers must regrow on demand.");
        }

        [Test]
        public void UpdateColliderPoints_TrimsItsSharedBufferAfterAnOversizedCall()
        {
            GameObject owner = Track(new GameObject("Draw shared collider owner"));
            EdgeCollider2D collider = owner.AddComponent<EdgeCollider2D>();
            List<Vector3> points = new(OversizedCapacity);
            for (int i = 0; i < OversizedCapacity; i++)
            {
                points.Add(new Vector3(i * 0.01f, 0f, 0f));
            }

            Drawer.UpdateColliderPoints(collider, points);

            Assert.That(collider.pointCount, Is.EqualTo(OversizedCapacity));
            FieldInfo shared = typeof(Drawer).GetField("SharedColliderPoints",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(shared, Is.Not.Null);
            List<Vector2> buffer = (List<Vector2>)shared.GetValue(null);
            Assert.That(buffer.Capacity, Is.LessThanOrEqualTo(RetainedBufferLimit));
        }

        [Test]
        public void LiveCollider_TracksFollowedEndpointAndOwnerMovement()
        {
            Drawer drawer = CreateDrawer();
            drawer.addCollider = true;
            drawer.colliderAfterCreation = false;
            drawer.BeginLine(Vector3.zero);
            drawer.AppendPoint(Vector3.right);
            LineRenderer line = CurrentLine(drawer);
            EdgeCollider2D collider = line.GetComponent<EdgeCollider2D>();
            Assert.That(collider, Is.Not.Null);

            InvokeFollow(drawer, new Vector3(2f, 1f, 0f));

            AssertColliderMatchesRenderer(collider, line);
            Assert.That(Vector3.Distance(line.GetPosition(line.positionCount - 1), new Vector3(2f, 1f, 0f)),
                Is.LessThan(0.0001f));

            drawer.transform.position = new Vector3(5f, -3f, 0f);
            InvokeFollow(drawer, new Vector3(2f, 1f, 0f));

            AssertColliderMatchesRenderer(collider, line);
        }

        private static void AppendStroke(Drawer drawer, int count)
        {
            for (int i = 0; i < count; i++)
            {
                drawer.AppendPoint(new Vector3(i * 0.5f, Mathf.Sin(i * 0.3f), 0f));
            }
        }

        private static T PrivateField<T>(Drawer drawer, string name) where T : class
        {
            FieldInfo field = typeof(Drawer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(drawer);
        }

        private static void SetPrivateField(Drawer drawer, string name, object value)
        {
            FieldInfo field = typeof(Drawer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(drawer, value);
        }

        private static void InvokeFollow(Drawer drawer, Vector3 position)
        {
            MethodInfo follow = typeof(Drawer).GetMethod("Folow", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(follow, Is.Not.Null);
            follow.Invoke(drawer, new object[] { position });
        }

        private static void AssertColliderMatchesRenderer(EdgeCollider2D collider, LineRenderer line)
        {
            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.pointCount, Is.EqualTo(line.positionCount));
            Vector2[] points = collider.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 world = collider.transform.TransformPoint(points[i]);
                Assert.That(Vector2.Distance(world, line.GetPosition(i)), Is.LessThan(0.0001f), "Point " + i);
            }
        }

        private Drawer CreateDrawer()
        {
            GameObject cameraObject = Track(new GameObject("Draw buffer camera"));
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject owner = Track(new GameObject("Draw buffer owner"));
            owner.SetActive(false);
            Drawer drawer = owner.AddComponent<Drawer>();
            drawer.InputCamera = camera;
            drawer.isActive = false;
            drawer.minPointDistance = 0.1f;
            drawer.smoothing = 0;
            owner.SetActive(true);
            return drawer;
        }

        private static LineRenderer CurrentLine(Drawer drawer)
        {
            LineRenderer[] renderers = drawer.GetComponentsInChildren<LineRenderer>();
            return renderers[^1];
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static List<Vector3> CreatePolyline(int count)
        {
            List<Vector3> points = new(count);
            for (int i = 0; i < count; i++)
            {
                points.Add(new Vector3(i * 1.25f, (i % 3) - 1f, i * 0.5f - 2f));
            }

            return points;
        }

        /// <summary>The allocating Chaikin implementation shipped before the buffer rewrite.</summary>
        private static List<Vector3> ReferenceSmooth(List<Vector3> points, int passes, float? fixedZ)
        {
            Vector3[] input = points.ToArray();
            if (fixedZ.HasValue)
            {
                for (int i = 0; i < input.Length; i++)
                {
                    input[i].z = fixedZ.Value;
                }
            }

            if (passes <= 0 || points.Count < 3)
            {
                return new List<Vector3>(input);
            }

            for (int p = 0; p < passes; p++)
            {
                Vector3[] output = new Vector3[input.Length * 2];
                output[0] = input[0];
                int index = 1;
                for (int i = 0; i < input.Length - 1; i++)
                {
                    output[index++] = Vector3.Lerp(input[i], input[i + 1], 0.25f);
                    output[index++] = Vector3.Lerp(input[i], input[i + 1], 0.75f);
                }

                output[index] = input[^1];
                input = output;
            }

            return new List<Vector3>(input);
        }

        /// <summary>The allocating even-sampling implementation shipped before the buffer rewrite.</summary>
        private static List<Vector3> ReferenceLimit(List<Vector3> points, int max)
        {
            if (max <= 1 || points.Count <= max)
            {
                return new List<Vector3>(points);
            }

            if (max == 2)
            {
                return new List<Vector3> { points[0], points[^1] };
            }

            List<Vector3> result = new(max);
            float step = (points.Count - 1f) / (max - 1);
            for (int i = 0; i < max; i++)
            {
                result.Add(points[Mathf.RoundToInt(i * step)]);
            }

            return result;
        }
    }
}
