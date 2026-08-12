using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.UI
{
    public sealed class UIMeshRigGeometryTests
    {
        [Test]
        public void Build_CreatesPredictableGridVerticesIndicesAndUv()
        {
            UIMeshRigGeometry geometry = UIMeshRigGeometryBuilder.Build(
                2,
                2,
                new UIMeshRigCoordinateSpace(new Vector2(-1f, -2f), new Vector2(4f, 6f)),
                new Rect(0.25f, 0.1f, 0.5f, 0.8f),
                false,
                null);

            Assert.That(geometry.Vertices, Has.Length.EqualTo(9));
            Assert.That(geometry.Indices, Has.Length.EqualTo(24));
            Assert.That(geometry.UV, Has.Length.EqualTo(9));
            Assert.That(geometry.Vertices[0], Is.EqualTo(new Vector3(-1f, -2f, 0f)));
            Assert.That(geometry.Vertices[4], Is.EqualTo(new Vector3(1f, 1f, 0f)));
            Assert.That(geometry.Vertices[8], Is.EqualTo(new Vector3(3f, 4f, 0f)));
            Assert.That(geometry.UV[0].x, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(geometry.UV[0].y, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(geometry.UV[8].x, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(geometry.UV[8].y, Is.EqualTo(0.9f).Within(0.0001f));
            CollectionAssert.AreEqual(new[] { 0, 3, 4, 0, 4, 1 }, FirstTrianglePair(geometry.Indices));
        }

        [Test]
        public void Influence_UsesEllipticalInnerAreaCurveAndHardOuterBoundary()
        {
            UIMeshRigPointState point = new UIMeshRigPointState(
                true,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.1f, 0.2f),
                new Vector2(0.3f, 0.4f),
                0.8f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                Vector2.zero,
                0f,
                Vector2.one);

            Assert.That(UIMeshRigGeometryBuilder.EvaluateInfluence(point, new Vector2(0.55f, 0.5f)),
                Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(UIMeshRigGeometryBuilder.EvaluateInfluence(point, new Vector2(0.7f, 0.5f)),
                Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(UIMeshRigGeometryBuilder.EvaluateInfluence(point, new Vector2(0.8f, 0.5f)),
                Is.Zero.Within(0.0001f));
            Assert.That(UIMeshRigGeometryBuilder.EvaluateInfluence(point, new Vector2(0.5f, 0.9f)),
                Is.Zero.Within(0.0001f));
        }

        [Test]
        public void Pose_ComposesTranslationRotationAndScaleWithoutRendererState()
        {
            UIMeshRigPointState point = new UIMeshRigPointState(
                true,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.one,
                1f,
                null,
                new Vector2(3f, 4f),
                90f,
                new Vector2(2f, 1f));
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(Vector2.zero, new Vector2(2f, 2f));

            Vector2 result = UIMeshRigGeometryBuilder.ApplyPose(point, new Vector2(2f, 1f), space);

            Assert.That(result.x, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(6f).Within(0.0001f));
        }

        [Test]
        public void ThreeAdapters_ProduceEquivalentGeometryForSamePointsAndMotionTime()
        {
            const int columns = 4;
            const int rows = 3;
            const float time = 0.37f;
            GameObject uguiObject = null;
            GameObject worldObject = null;
            Texture2D texture = null;
            Sprite sprite = null;

            try
            {
                texture = new Texture2D(8, 4);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 4f), new Vector2(0.5f, 0.5f));

                uguiObject = new GameObject(
                    "uGUI Rig",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(UIMeshRigGraphic));
                RectTransform uguiRect = (RectTransform)uguiObject.transform;
                uguiRect.sizeDelta = new Vector2(200f, 100f);
                UIMeshRigGraphic uguiRig = uguiObject.GetComponent<UIMeshRigGraphic>();
                uguiRig.SetSource(sprite, Color.white);
                uguiRig.SetGridResolution(columns, rows);
                uguiRig.SetPreserveAspect(false);
                UIMeshRigPoint[] uguiPoints = UIMeshRigLayoutBuilder.Apply(
                    uguiRig,
                    UIMeshRigLayoutPreset.Character,
                    true,
                    false);
                uguiRig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);

                worldObject = new GameObject(
                    "World Rig",
                    typeof(MeshFilter),
                    typeof(MeshRenderer),
                    typeof(UIMeshRigWorldRenderer));
                UIMeshRigWorldRenderer worldRig = worldObject.GetComponent<UIMeshRigWorldRenderer>();
                worldRig.SetSource(sprite, Color.white);
                worldRig.SetGridResolution(columns, rows);
                worldRig.SetPreserveAspect(false);
                worldRig.SetSize(new Vector2(2f, 1f));
                UIMeshRigPoint[] worldPoints = UIMeshRigLayoutBuilder.Apply(
                    worldRig,
                    UIMeshRigLayoutPreset.Character,
                    true,
                    false);
                worldRig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);

                ApplyPresetPose(uguiPoints, worldPoints, UIMeshRigLayoutPreset.Character, time);

                UIMeshRigElement toolkitElement = new UIMeshRigElement
                {
                    Sprite = sprite,
                    Columns = columns,
                    Rows = rows,
                    PreserveAspect = false,
                    DeformationEnabled = true,
                    LayoutPreset = UIMeshRigLayoutPreset.Character,
                    MotionEnabled = true,
                    MotionSpeed = 1f,
                    MotionPhase = 0f
                };

                UIMeshRigGeometry ugui = uguiRig.BuildGeometry();
                UIMeshRigGeometry toolkit = toolkitElement.BuildGeometry(new Vector2(200f, 100f), time);
                UIMeshRigGeometry world = worldRig.BuildGeometry();

                CollectionAssert.AreEqual(ugui.Indices, toolkit.Indices);
                CollectionAssert.AreEqual(ugui.Indices, world.Indices);
                Assert.That(toolkit.UV, Has.Length.EqualTo(ugui.UV.Length));
                Assert.That(world.UV, Has.Length.EqualTo(ugui.UV.Length));
                for (int index = 0; index < ugui.Vertices.Length; index++)
                {
                    Vector2 uguiNormalized = new Vector2(
                        (ugui.Vertices[index].x + 100f) / 200f,
                        (ugui.Vertices[index].y + 50f) / 100f);
                    Vector2 toolkitNormalized = new Vector2(
                        toolkit.Vertices[index].x / 200f,
                        1f - toolkit.Vertices[index].y / 100f);
                    Vector2 worldNormalized = new Vector2(
                        (world.Vertices[index].x + 1f) / 2f,
                        world.Vertices[index].y + 0.5f);

                    Assert.That(toolkitNormalized.x, Is.EqualTo(uguiNormalized.x).Within(0.0001f));
                    Assert.That(toolkitNormalized.y, Is.EqualTo(uguiNormalized.y).Within(0.0001f));
                    Assert.That(worldNormalized.x, Is.EqualTo(uguiNormalized.x).Within(0.0001f));
                    Assert.That(worldNormalized.y, Is.EqualTo(uguiNormalized.y).Within(0.0001f));
                    Assert.That(toolkit.UV[index].x, Is.EqualTo(ugui.UV[index].x).Within(0.0001f));
                    Assert.That(toolkit.UV[index].y, Is.EqualTo(ugui.UV[index].y).Within(0.0001f));
                    Assert.That(world.UV[index].x, Is.EqualTo(ugui.UV[index].x).Within(0.0001f));
                    Assert.That(world.UV[index].y, Is.EqualTo(ugui.UV[index].y).Within(0.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(uguiObject);
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static int[] FirstTrianglePair(int[] indices)
        {
            int[] result = new int[6];
            System.Array.Copy(indices, result, result.Length);
            return result;
        }

        private static void ApplyPresetPose(
            UIMeshRigPoint[] uguiPoints,
            UIMeshRigPoint[] worldPoints,
            UIMeshRigLayoutPreset preset,
            float time)
        {
            int pointCount = UIMeshRigLayoutPresets.GetPointCount(preset);
            Assert.That(uguiPoints, Has.Length.EqualTo(pointCount));
            Assert.That(worldPoints, Has.Length.EqualTo(pointCount));
            for (int index = 0; index < pointCount; index++)
            {
                UIMeshRigPointLayout layout = UIMeshRigLayoutPresets.GetPoint(preset, index);
                UIMeshRigMotionProfile profile = UIMeshRigMotionPresets.Create(layout.MotionPreset);
                UIMeshRigProceduralPose pose = UIMeshRigMotionEvaluator.Evaluate(
                    profile,
                    time,
                    1f,
                    layout.Phase,
                    layout.CenterNormalized,
                    layout.Seed);
                uguiPoints[index].SetProceduralPose(pose.Position, pose.RotationDegrees, pose.Scale);
                worldPoints[index].SetProceduralPose(pose.Position, pose.RotationDegrees, pose.Scale);
            }
        }
    }
}
