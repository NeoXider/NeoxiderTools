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
    ///     Explicit UI Mesh Rig benchmarks (audit 2026-11-02, M1): one animated rebuild per adapter at several grid
    ///     sizes and point counts, and a clean-frame control (the adapter's LateUpdate with nothing changed). Each
    ///     NEO_PERF line carries the grid's vertex and index counts, so runs at different sizes can be compared.
    /// </summary>
    public sealed class UIMeshRigPerformanceTests
    {
        private const int OperationsPerSample = 10;
        private const int Samples = 60;
        private static readonly Vector2 ToolkitSurface = new Vector2(240f, 240f);

        private readonly List<Object> _objects = new List<Object>();
        private readonly List<Action> _cleanups = new List<Action>();

        [OneTimeSetUp]
        public void EmitCalibration()
        {
            NeoPerf.EmitCalibration("ui.meshrig", Samples);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _cleanups.Count; index++)
            {
                _cleanups[index]();
            }

            _cleanups.Clear();
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
        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        public void Perf_AnimatedRebuild(
            [Values("ugui", "world", "sprite", "toolkit")] string adapter,
            [Values(2, 10, 40)] int grid,
            [Values(UIMeshRigLayoutPreset.SimpleBounce, UIMeshRigLayoutPreset.Character)]
            UIMeshRigLayoutPreset layout)
        {
            RigBench bench = CreateBench(adapter, grid, layout);
            int frame = 0;
            Action operation = () =>
            {
                frame++;
                bench.Animate(frame);
            };

            NeoPerf.MeasureSynchronous(
                    "ui.meshrig.rebuild.animated." + adapter + ".grid" + grid + "." + layout,
                    grid,
                    OperationsPerSample,
                    NeoPerf.DefaultWarmupSamples,
                    Samples,
                    operation)
                .Add("adapter", adapter)
                .Add("points", (long)bench.PointCount)
                .Add("vertices", (long)((grid + 1) * (grid + 1)))
                .Add("indices", (long)(grid * grid * 6))
                .Emit();

            Assert.That(bench.CountVertices(), Is.EqualTo((grid + 1) * (grid + 1)),
                "The measured rebuild must produce the full grid.");
        }

        [Test]
        [Category(NeoPerf.Category)]
        [Explicit(NeoPerf.ExplicitReason)]
        public void Perf_CleanFrame([Values("ugui", "world", "sprite")] string adapter, [Values(10, 40)] int grid)
        {
            RigBench bench = CreateBench(adapter, grid, UIMeshRigLayoutPreset.Character);
            bench.Animate(1);
            bench.Frame();

            NeoPerf.MeasureSynchronous(
                    "ui.meshrig.frame.clean." + adapter + ".grid" + grid,
                    grid,
                    OperationsPerSample,
                    NeoPerf.DefaultWarmupSamples,
                    Samples,
                    bench.Frame)
                .Add("adapter", adapter)
                .Add("points", (long)bench.PointCount)
                .Add("vertices", (long)((grid + 1) * (grid + 1)))
                .Emit();

            Assert.That(bench.CountVertices(), Is.EqualTo((grid + 1) * (grid + 1)));
        }

        private RigBench CreateBench(string adapter, int grid, UIMeshRigLayoutPreset layout)
        {
            Sprite sprite = CreateSprite();
            switch (adapter)
            {
                case "ugui":
                    return CreateGraphicBench(sprite, grid, layout);
                case "world":
                    return CreateWorldBench(sprite, grid, layout);
                case "sprite":
                    return CreateSpriteBench(sprite, grid, layout);
                default:
                    return CreateToolkitBench(sprite, grid, layout);
            }
        }

        private RigBench CreateGraphicBench(Sprite sprite, int grid, UIMeshRigLayoutPreset layout)
        {
            GameObject canvasObject = Track(new GameObject("Bench canvas", typeof(RectTransform), typeof(Canvas)));
            GameObject rigObject = Track(new GameObject(
                "Bench uGUI rig",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic)));
            rigObject.transform.SetParent(canvasObject.transform, false);
            ((RectTransform)rigObject.transform).sizeDelta = new Vector2(240f, 240f);
            UIMeshRigGraphic rig = rigObject.GetComponent<UIMeshRigGraphic>();
            rig.SetSource(sprite, Color.white);
            rig.SetGridResolution(grid, grid);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, layout, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            Action<VertexHelper> populate = (Action<VertexHelper>)Delegate.CreateDelegate(
                typeof(Action<VertexHelper>),
                rig,
                GetPrivateMethod(typeof(UIMeshRigGraphic), "OnPopulateMesh", typeof(VertexHelper)));
            VertexHelper vertexHelper = new VertexHelper();
            _cleanups.Add(vertexHelper.Dispose);
            return new RigBench(
                points,
                () => populate(vertexHelper),
                CreatePrivateAction(rig, typeof(UIMeshRigGraphic), "LateUpdate"),
                () => vertexHelper.currentVertCount);
        }

        private RigBench CreateWorldBench(Sprite sprite, int grid, UIMeshRigLayoutPreset layout)
        {
            GameObject rigObject = Track(new GameObject(
                "Bench world rig",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(UIMeshRigWorldRenderer)));
            UIMeshRigWorldRenderer rig = rigObject.GetComponent<UIMeshRigWorldRenderer>();
            rig.SetSource(sprite, Color.white);
            rig.SetGridResolution(grid, grid);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, layout, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            MeshFilter filter = rigObject.GetComponent<MeshFilter>();
            return new RigBench(
                points,
                CreatePrivateAction(rig, typeof(UIMeshRigWorldRenderer), "RebuildIfNeeded"),
                CreatePrivateAction(rig, typeof(UIMeshRigWorldRenderer), "LateUpdate"),
                () => filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0);
        }

        private RigBench CreateSpriteBench(Sprite sprite, int grid, UIMeshRigLayoutPreset layout)
        {
            GameObject rigObject = Track(new GameObject(
                "Bench sprite rig",
                typeof(SpriteRenderer),
                typeof(UIMeshRigSpriteRenderer)));
            UIMeshRigSpriteRenderer rig = rigObject.GetComponent<UIMeshRigSpriteRenderer>();
            rig.SetSource(sprite, Color.white);
            rig.SetGridResolution(grid, grid);
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, layout, true, false);
            rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            // WHY: RebuildIfNeeded, not Rebuild(): the public Rebuild forces a dirty flag, while the measured
            // animated path is dirtied by the procedural pose exactly like a playing motion.
            return new RigBench(
                points,
                CreatePrivateAction(rig, typeof(UIMeshRigSpriteRenderer), "RebuildIfNeeded"),
                CreatePrivateAction(rig, typeof(UIMeshRigSpriteRenderer), "LateUpdate"),
                () => rig.DeformedSprite != null ? rig.DeformedSprite.vertices.Length : 0);
        }

        private static RigBench CreateToolkitBench(Sprite sprite, int grid, UIMeshRigLayoutPreset layout)
        {
            UIMeshRigElement element = new UIMeshRigElement
            {
                Sprite = sprite,
                Columns = grid,
                Rows = grid,
                LayoutPreset = layout,
                MotionEnabled = true,
                MotionSpeed = 1f
            };
            UIMeshRigGeometry last = null;
            return new RigBench(
                UIMeshRigLayoutPresets.GetPointCount(layout),
                frame => last = element.BuildGeometryReusingBuffers(ToolkitSurface, frame * 0.016f),
                () => last != null ? last.Vertices.Length : 0);
        }

        private static MethodInfo GetPrivateMethod(Type type, string name, params Type[] parameters)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null,
                parameters, null);
            Assert.That(method, Is.Not.Null, type.Name + "." + name);
            return method;
        }

        private static Action CreatePrivateAction(object target, Type type, string name)
        {
            return (Action)Delegate.CreateDelegate(typeof(Action), target, GetPrivateMethod(type, name));
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(64, 64);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f));
            _objects.Add(texture);
            _objects.Add(sprite);
            return sprite;
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }

        /// <summary>One rig under test: how to animate one frame, run a clean frame and read the output size.</summary>
        private sealed class RigBench
        {
            private readonly UIMeshRigPoint[] _points;
            private readonly Action _rebuild;
            private readonly Action<int> _animate;
            private readonly Func<int> _countVertices;

            public RigBench(UIMeshRigPoint[] points, Action rebuild, Action frame, Func<int> countVertices)
            {
                _points = points;
                _rebuild = rebuild;
                Frame = frame;
                _countVertices = countVertices;
                PointCount = points.Length;
            }

            public RigBench(int pointCount, Action<int> animate, Func<int> countVertices)
            {
                _points = Array.Empty<UIMeshRigPoint>();
                _animate = animate;
                _countVertices = countVertices;
                Frame = () => { };
                PointCount = pointCount;
            }

            public int PointCount { get; }

            /// <summary>The adapter's own per-frame entry point (LateUpdate), measured with nothing changed.</summary>
            public Action Frame { get; }

            public void Animate(int frame)
            {
                if (_animate != null)
                {
                    _animate(frame);
                    return;
                }

                // WHY: a procedural pose is what UIMeshRigPointMotion writes every frame; it dirties the owner
                // like a playing motion, without a player loop in Edit Mode.
                _points[0].SetProceduralPose(new Vector2(frame % 7, 1f), frame % 11, Vector2.one);
                _rebuild();
            }

            public int CountVertices()
            {
                return _countVertices();
            }
        }
    }
}
