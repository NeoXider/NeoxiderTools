using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace Neo.Tests.UI
{
    /// <summary>
    ///     The SpriteRenderer adapter has one rule that outranks every feature: the imported Sprite asset is
    ///     shared project state and must never change. These cases pin that, plus the fact that the adapter
    ///     really deforms (a rig that quietly renders the rest pose would look "supported" and be useless).
    /// </summary>
    public sealed class UIMeshRigSpriteRendererTests
    {
        private GameObject _root;
        private UIMeshRigSpriteRenderer _rig;
        private Texture2D _texture;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(64, 32);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 64f, 32f), new Vector2(0.5f, 0.5f), 100f);
            _root = new GameObject("Sprite Rig", typeof(SpriteRenderer), typeof(UIMeshRigSpriteRenderer));
            _rig = _root.GetComponent<UIMeshRigSpriteRenderer>();
            _rig.SetSource(_sprite, Color.white);
            _rig.SetGridResolution(6, 6);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Rebuild_DrawsAClone_AndLeavesTheSourceSpriteUntouched()
        {
            int sourceVertexCount = _sprite.GetVertexCount();

            _rig.Rebuild();

            SpriteRenderer renderer = _root.GetComponent<SpriteRenderer>();
            Assert.That(_rig.DeformedSprite, Is.Not.Null, "The adapter must build a runtime clone.");
            Assert.That(renderer.sprite, Is.SameAs(_rig.DeformedSprite));
            Assert.That(renderer.sprite, Is.Not.SameAs(_sprite),
                "Rendering the source asset would mean deforming shared project state.");
            Assert.That(_rig.DeformedSprite.GetVertexCount(), Is.EqualTo(7 * 7));
            Assert.That(_sprite.GetVertexCount(), Is.EqualTo(sourceVertexCount),
                "The imported Sprite asset must keep its import-time geometry.");
        }

        [Test]
        public void DisablingTheComponent_RestoresTheSourceSpriteOnTheRenderer()
        {
            _rig.Rebuild();
            SpriteRenderer renderer = _root.GetComponent<SpriteRenderer>();
            Assert.That(renderer.sprite, Is.Not.SameAs(_sprite));

            _rig.enabled = false;

            Assert.That(renderer.sprite, Is.SameAs(_sprite),
                "A disabled rig must hand the renderer its original sprite back, not a dangling clone.");
        }

        [Test]
        public void PosedPoint_MovesCloneVerticesWhileTheRestPoseStaysFlat()
        {
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(
                _rig, UIMeshRigLayoutPreset.SimpleBounce, true, false);
            _rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
            UIMeshRigGeometry rest = _rig.BuildGeometry();

            points[0].SetProceduralPose(new Vector2(20f, 10f), 0f, Vector2.one);
            UIMeshRigGeometry posed = _rig.BuildGeometry();

            float maxDelta = 0f;
            for (int index = 0; index < rest.Vertices.Length; index++)
            {
                maxDelta = Mathf.Max(maxDelta, (rest.Vertices[index] - posed.Vertices[index]).magnitude);
            }

            Assert.That(maxDelta, Is.GreaterThan(0.05f), "Posing a point must actually warp the mesh.");

            _rig.Rebuild();
            Vector3[] written = _rig.DeformedSprite.GetVertexAttribute<Vector3>(VertexAttribute.Position).ToArray();
            Assert.That(written, Has.Length.EqualTo(posed.Vertices.Length));
        }

        [Test]
        public void CloneBounds_CarryDeformHeadroom_SoAWarpedSpriteIsNotCulledEarly()
        {
            _rig.Rebuild();

            Bounds cloneBounds = _rig.DeformedSprite.bounds;
            Assert.That(cloneBounds.extents.x, Is.GreaterThan(_sprite.bounds.extents.x),
                "Sprite bounds never grow with written geometry, so the clone has to carry the margin.");
            Assert.That(_rig.NativeSize.x, Is.EqualTo(0.64f).Within(0.0001f));
            Assert.That(_rig.NativeSize.y, Is.EqualTo(0.32f).Within(0.0001f));
        }

        [Test]
        public void AdapterGeometry_MatchesTheWorldRendererForTheSameRigAndSize()
        {
            GameObject worldObject = new GameObject(
                "World Rig", typeof(MeshFilter), typeof(MeshRenderer), typeof(UIMeshRigWorldRenderer));
            try
            {
                UIMeshRigWorldRenderer worldRig = worldObject.GetComponent<UIMeshRigWorldRenderer>();
                worldRig.SetSource(_sprite, Color.white);
                worldRig.SetGridResolution(6, 6);
                worldRig.SetPreserveAspect(false);
                worldRig.SetSize(_rig.NativeSize);

                UIMeshRigGeometry sprite = _rig.BuildGeometry();
                UIMeshRigGeometry world = worldRig.BuildGeometry();

                CollectionAssert.AreEqual(world.Indices, sprite.Indices);
                Assert.That(sprite.Vertices, Has.Length.EqualTo(world.Vertices.Length));
                for (int index = 0; index < world.Vertices.Length; index++)
                {
                    Assert.That(sprite.Vertices[index].x, Is.EqualTo(world.Vertices[index].x).Within(0.0001f));
                    Assert.That(sprite.Vertices[index].y, Is.EqualTo(world.Vertices[index].y).Within(0.0001f));
                    Assert.That(sprite.UV[index].x, Is.EqualTo(world.UV[index].x).Within(0.0001f));
                    Assert.That(sprite.UV[index].y, Is.EqualTo(world.UV[index].y).Within(0.0001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
            }
        }

        [Test]
        public void PointOwnerResolution_FindsTheSpriteAdapterWithoutAGraphicOrWorldRenderer()
        {
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(
                _rig, UIMeshRigLayoutPreset.Character, true, false);

            Assert.That(points, Has.Length.EqualTo(4));
            Assert.That(_rig.Points.Count, Is.EqualTo(4),
                "A new adapter must be discovered by the shared owner resolver, not by a hard-coded type list.");
            Assert.That(UIMeshRigOwnerResolver.Find(points[0].transform), Is.SameAs(_rig));
        }
    }
}
