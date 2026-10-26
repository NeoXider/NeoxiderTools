using System.Collections;
using System.Collections.Generic;
using Neo.Parallax;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoParallaxRegressionTests
    {
        // These Edit Mode contracts explicitly bind/release the camera. Native callbacks are
        // exercised separately by Audit20261023NeoParallaxNativeLifecycleTests below.
        private readonly List<Object> resources = new();
        private GameObject cameraObject;
        private GameObject layerObject;
        private Camera camera;
        private ParallaxLayer layer;
        private SpriteRenderer template;
        private Sprite sprite;
        private Random.State randomState;

        [SetUp]
        public void SetUp()
        {
            randomState = Random.state;
            cameraObject = new GameObject("AuditParallaxCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.aspect = 1f;

            layerObject = new GameObject("AuditParallaxLayer");
            layerObject.SetActive(false);
            template = layerObject.AddComponent<SpriteRenderer>();
            sprite = CreateSprite(8, 8);
            template.sprite = sprite;
            layer = layerObject.AddComponent<ParallaxLayer>();

            SerializedObject settings = new(layer);
            settings.FindProperty("targetCamera").objectReferenceValue = camera;
            settings.FindProperty("templateRenderer").objectReferenceValue = template;
            settings.FindProperty("useMainCameraFallback").boolValue = false;
            settings.FindProperty("randomiseOnInit").boolValue = false;
            settings.FindProperty("parallaxMultiplier").vector2Value = Vector2.zero;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (layer != null)
            {
                layer.SetTargetCamera(null);
            }

            if (layerObject != null)
            {
                Object.DestroyImmediate(layerObject);
            }

            if (cameraObject != null)
            {
                Object.DestroyImmediate(cameraObject);
            }

            for (int i = resources.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(resources[i]);
            }

            resources.Clear();
            Random.state = randomState;
        }

        [Test]
        public void RebuildAndReleaseCamera_PreserveAuthoredChildren()
        {
            GameObject decoration = new("AuthoredDecoration");
            decoration.transform.SetParent(layerObject.transform, false);
            decoration.transform.localPosition = new Vector3(2f, 3f, 4f);

            ActivateLayer();
            AssertGeneratedTilesReady(8);
            Assert.IsFalse(template.enabled);
            Assert.IsTrue(decoration != null, "Initialisation must retain authored children.");
            layer.SetTargetCamera(camera);
            AssertGeneratedTilesReady(8);
            Assert.IsTrue(decoration != null, "Rebinding must retain authored children.");

            layer.SetTargetCamera(null);
            Assert.IsTrue(decoration != null, "Cleanup must retain authored children.");
            Assert.AreEqual(layerObject.transform, decoration.transform.parent);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), decoration.transform.localPosition);
            Assert.IsTrue(decoration.activeSelf);
            Assert.AreEqual(0, GeneratedTiles().Count);
        }

        [TestCase(2f, 3f)]
        [TestCase(-2f, 3f)]
        [TestCase(2f, -3f)]
        public void RootTemplate_AppliesPositionAndScaleOnce(float scaleX, float scaleY)
        {
            layerObject.transform.position = new Vector3(12f, -3f, 0f);
            layerObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            Vector3 expectedSize = new(Mathf.Abs(scaleX), Mathf.Abs(scaleY), 0f);
            Vector3 expectedPosition = template.transform.position;

            ActivateLayer();
            AssertTilesMatchTemplate(expectedSize, expectedPosition);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChildTemplate_ScaleDeterminesTileSizeAndStride(bool nested)
        {
            Object.DestroyImmediate(template);
            Transform parent = layerObject.transform;
            if (nested)
            {
                GameObject group = new("AuthoredTemplateGroup");
                group.transform.SetParent(parent, false);
                group.transform.localScale = new Vector3(0.5f, 2f, 1f);
                parent = group.transform;
            }

            GameObject source = new("AuthoredTemplate");
            source.transform.SetParent(parent, false);
            source.transform.localPosition = new Vector3(0.5f, 0.25f, 0f);
            source.transform.localScale = new Vector3(2f, 3f, 1f);
            template = source.AddComponent<SpriteRenderer>();
            template.sprite = sprite;
            layerObject.transform.localScale = new Vector3(1.5f, 2f, 1f);
            Vector3 expectedSize = nested ? new Vector3(1.5f, 12f, 0f) : new Vector3(3f, 6f, 0f);
            Vector3 expectedPosition = template.transform.position;
            SerializedObject settings = new(layer);
            settings.FindProperty("templateRenderer").objectReferenceValue = template;
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();
            Assert.IsTrue(source != null, "A nested template and its ancestors must survive pool creation.");
            AssertTilesMatchTemplate(expectedSize, expectedPosition);
            layer.SetTargetCamera(null);
            Assert.AreEqual(expectedPosition, template.transform.position);
            Assert.IsTrue(template.enabled);
        }

        [Test]
        public void ClearingCamera_RestoresSourceAndRemovesPool()
        {
            ActivateLayer();
            Assert.GreaterOrEqual(GeneratedTiles().Count, 3);
            Assert.IsFalse(template.enabled);

            layer.SetTargetCamera(null);

            Assert.IsNull(layer.TargetCamera);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
        }

        [Test]
        public void RebindingAndReleasingCamera_RestoreInitiallyDisabledSource()
        {
            template.enabled = false;
            ActivateLayer();
            Assert.GreaterOrEqual(GeneratedTiles().Count, 3);
            layer.SetTargetCamera(camera);
            layer.SetTargetCamera(null);

            Assert.IsFalse(template.enabled);
            Assert.AreEqual(0, GeneratedTiles().Count);
        }

        [Test]
        public void ReplacingTemplate_RestoresPreviousRenderer()
        {
            ActivateLayer();
            AssertGeneratedTilesReady(8);
            GameObject replacement = new("ReplacementTemplate");
            replacement.transform.SetParent(layerObject.transform, false);
            SpriteRenderer replacementRenderer = replacement.AddComponent<SpriteRenderer>();
            replacementRenderer.sprite = sprite;
            SerializedObject settings = new(layer);
            settings.FindProperty("templateRenderer").objectReferenceValue = replacementRenderer;
            settings.ApplyModifiedPropertiesWithoutUndo();

            layer.SetTargetCamera(camera);

            Assert.IsTrue(template.enabled, "The previous source must be released when the binding changes.");
            Assert.IsFalse(replacementRenderer.enabled);
            AssertGeneratedTilesReady(8);
            layer.SetTargetCamera(null);
            Assert.IsTrue(template.enabled);
            Assert.IsTrue(replacementRenderer.enabled);
            Assert.AreEqual(0, GeneratedTiles().Count);
        }

        [Test]
        public void SetTargetCamera_WithEditorPreviewDisabled_LeavesSourceVisible()
        {
            SerializedObject settings = new(layer);
            settings.FindProperty("generateInEditor").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            Assert.AreSame(camera, layer.TargetCamera);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
        }

        [Test]
        public void NullSpriteVariants_DoNotCreateInvisiblePool_AndCanRecover()
        {
            template.sprite = null;
            SerializedObject settings = new(layer);
            settings.FindProperty("spriteVariants").arraySize = 2;
            settings.FindProperty("spriteVariants").GetArrayElementAtIndex(0).objectReferenceValue = null;
            settings.FindProperty("spriteVariants").GetArrayElementAtIndex(1).objectReferenceValue = null;
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
            template.sprite = sprite;
            layer.SetTargetCamera(camera);
            Assert.GreaterOrEqual(GeneratedTiles().Count, 3);
            Assert.IsFalse(template.enabled);
        }

        [TestCase(0f, 0f)]
        [TestCase(1f, -1f)]
        [TestCase(1f, -2f)]
        [TestCase(1f, float.NaN)]
        [TestCase(1f, float.PositiveInfinity)]
        public void InvalidTilingStride_DoesNotAllocatePool_AndCanRecover(float scaleX, float spacingX)
        {
            layerObject.transform.localScale = new Vector3(scaleX, 1f, 1f);
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = new Vector2(spacingX, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
            layerObject.transform.localScale = Vector3.one;
            settings.Update();
            settings.FindProperty("tileSpacing").vector2Value = Vector2.zero;
            settings.ApplyModifiedPropertiesWithoutUndo();
            layer.SetTargetCamera(camera);
            Assert.GreaterOrEqual(GeneratedTiles().Count, 3);
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void TileCounts_RejectUnrepresentableAxis_AndRecover(bool orthographic, bool horizontal)
        {
            camera.orthographic = orthographic;
            SetTiling(horizontal, !horizontal,
                horizontal ? new Vector2Int(int.MaxValue, 1) : new Vector2Int(1, int.MaxValue));

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);

            SetTiling(horizontal, !horizontal, Vector2Int.one);
            layer.SetTargetCamera(camera);

            Assert.AreEqual(orthographic || horizontal ? 8 : 6, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TileCounts_RejectUnrepresentableProduct_AndRecover(bool orthographic)
        {
            camera.orthographic = orthographic;
            SetTiling(true, true, new Vector2Int(25000, 25000));

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);

            SetTiling(true, true, Vector2Int.one);
            layer.SetTargetCamera(camera);

            Assert.AreEqual(orthographic ? 64 : 48, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);
        }

        [Test]
        public void TileCounts_RejectUnrepresentableViewport_AndRecover()
        {
            camera.orthographicSize = float.MaxValue;

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);

            camera.orthographicSize = 2f;
            layer.SetTargetCamera(camera);

            Assert.AreEqual(8, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);
        }

        [Test]
        public void TileCounts_RejectNonFiniteWorldPeriod_AndRecover()
        {
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = new Vector2(float.MaxValue, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);

            settings.Update();
            settings.FindProperty("tileSpacing").vector2Value = Vector2.zero;
            settings.ApplyModifiedPropertiesWithoutUndo();
            layer.SetTargetCamera(camera);

            Assert.AreEqual(8, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void TileCounts_DisabledAxesIgnoreExtremePadding(bool horizontal, bool vertical)
        {
            SetTiling(horizontal, vertical,
                new Vector2Int(horizontal ? 1 : int.MaxValue, vertical ? 1 : int.MaxValue));

            ActivateLayer();

            Assert.AreEqual(horizontal || vertical ? 8 : 1, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TileCounts_RejectionAfterLivePool_RestoresSourceState_AndRecovers(bool initiallyEnabled)
        {
            template.enabled = initiallyEnabled;
            ActivateLayer();
            Assert.AreEqual(8, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);

            SetTiling(true, false, new Vector2Int(int.MaxValue, 1));
            layer.SetTargetCamera(camera);

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.AreEqual(initiallyEnabled, template.enabled);

            SetTiling(true, false, Vector2Int.one);
            layer.SetTargetCamera(camera);

            Assert.AreEqual(8, GeneratedTiles().Count);
            Assert.IsFalse(template.enabled);
            layer.SetTargetCamera(null);
            Assert.AreEqual(initiallyEnabled, template.enabled);
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        public void LocalGeometry_RejectsUnrepresentablePitch_AndRecovers(bool initiallyEnabled, bool startWithLivePool)
        {
            template.enabled = initiallyEnabled;
            GameObject decoration = new("AuthoredLocalGeometryChild");
            decoration.transform.SetParent(layerObject.transform, false);
            decoration.transform.localPosition = new Vector3(2f, 3f, 4f);
            if (startWithLivePool)
            {
                ActivateLayer();
                AssertGeneratedTilesReady(8);
            }

            layerObject.transform.localScale = new Vector3(1f / 16f, 1f, 1f);
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = new Vector2(float.MaxValue / 8f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (startWithLivePool)
            {
                layer.SetTargetCamera(camera);
            }
            else
            {
                ActivateLayer();
            }

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.AreEqual(initiallyEnabled, template.enabled);
            Assert.IsTrue(decoration != null);
            Assert.AreSame(layerObject.transform, decoration.transform.parent);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), decoration.transform.localPosition);
            Assert.IsTrue(decoration.activeSelf);

            layerObject.transform.localScale = Vector3.one;
            settings.Update();
            settings.FindProperty("tileSpacing").vector2Value = Vector2.zero;
            settings.ApplyModifiedPropertiesWithoutUndo();
            layer.SetTargetCamera(camera);

            AssertGeneratedTilesReady(8);
            AssertTilesMatchTemplate(new Vector3(1f, 1f, 0f), Vector3.zero);
            Assert.IsFalse(template.enabled);
            layer.SetTargetCamera(null);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.AreEqual(initiallyEnabled, template.enabled);
            Assert.IsTrue(decoration != null);
            Assert.AreSame(layerObject.transform, decoration.transform.parent);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), decoration.transform.localPosition);
            Assert.IsTrue(decoration.activeSelf);
        }

        [Test]
        public void LocalGeometry_RejectsUnrepresentableCenteredEndpoints_AndRecovers()
        {
            layerObject.transform.localScale = new Vector3(1f / 8f, 1f, 1f);
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = new Vector2(float.MaxValue / 8f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);

            layerObject.transform.localScale = Vector3.one;
            settings.Update();
            settings.FindProperty("tileSpacing").vector2Value = Vector2.zero;
            settings.ApplyModifiedPropertiesWithoutUndo();
            layer.SetTargetCamera(camera);

            AssertGeneratedTilesReady(8);
            AssertTilesMatchTemplate(new Vector3(1f, 1f, 0f), Vector3.zero);
            Assert.IsFalse(template.enabled);
            layer.SetTargetCamera(null);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
        }

        [Test]
        public void LocalGeometry_RepresentableCenteredPositions_AvoidIntermediateOverflow()
        {
            layerObject.transform.localScale = new Vector3(1f / 4f, 1f, 1f);
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = new Vector2(float.MaxValue / 8f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            List<SpriteRenderer> tiles = AssertGeneratedTilesReady(5);
            tiles.Sort((SpriteRenderer left, SpriteRenderer right) =>
                left.transform.localPosition.x.CompareTo(right.transform.localPosition.x));
            float[] expectedLocalX =
            {
                -float.MaxValue, -float.MaxValue / 2f, 0f, float.MaxValue / 2f, float.MaxValue
            };
            float[] expectedWorldX =
            {
                -float.MaxValue / 4f, -float.MaxValue / 8f, 0f, float.MaxValue / 8f, float.MaxValue / 4f
            };
            for (int i = 0; i < tiles.Count; i++)
            {
                Assert.AreEqual(new Vector3(expectedLocalX[i], 0f, 0f), tiles[i].transform.localPosition);
                Assert.AreEqual(new Vector3(expectedWorldX[i], 0f, 0f), tiles[i].transform.position);
            }

            Assert.IsFalse(template.enabled);
            layer.SetTargetCamera(null);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void LocalGeometry_DisabledAxesIgnoreUnrepresentableUnusedPitch(bool horizontal, bool vertical)
        {
            SetTiling(horizontal, vertical, Vector2Int.one);
            layerObject.transform.localScale = horizontal
                ? new Vector3(1f, 1f / 16f, 1f)
                : new Vector3(1f / 16f, 1f, 1f);
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = horizontal
                ? new Vector2(0f, float.MaxValue / 8f)
                : new Vector2(float.MaxValue / 8f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            List<SpriteRenderer> tiles = AssertGeneratedTilesReady(horizontal || vertical ? 8 : 1);
            foreach (SpriteRenderer tile in tiles)
            {
                if (!horizontal)
                {
                    Assert.AreEqual(0f, tile.transform.localPosition.x);
                }

                if (!vertical)
                {
                    Assert.AreEqual(0f, tile.transform.localPosition.y);
                }
            }

            layer.SetTargetCamera(null);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
        }

        [Test]
        public void LocalGeometry_NegativeSpacing_PreservesOrdinaryOverlap()
        {
            SerializedObject settings = new(layer);
            settings.FindProperty("tileSpacing").vector2Value = new Vector2(-0.5f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();

            ActivateLayer();

            List<SpriteRenderer> tiles = AssertGeneratedTilesReady(12);
            tiles.Sort((SpriteRenderer left, SpriteRenderer right) =>
                left.transform.position.x.CompareTo(right.transform.position.x));
            Assert.AreEqual(-2.5f, tiles[0].transform.position.x);
            Assert.AreEqual(3f, tiles[tiles.Count - 1].transform.position.x);
            for (int i = 0; i < tiles.Count; i++)
            {
                Assert.That(tiles[i].bounds.size.x, Is.EqualTo(1f).Within(0.001f));
                Assert.AreEqual(0f, tiles[i].transform.position.y);
                if (i > 0)
                {
                    Assert.That(tiles[i].transform.position.x - tiles[i - 1].transform.position.x,
                        Is.EqualTo(0.5f).Within(0.001f));
                }
            }

            layer.SetTargetCamera(null);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.IsTrue(template.enabled);
        }

        private List<SpriteRenderer> AssertGeneratedTilesReady(int expectedCount)
        {
            List<SpriteRenderer> tiles = GeneratedTiles();
            Assert.AreEqual(expectedCount, tiles.Count);
            foreach (SpriteRenderer tile in tiles)
            {
                Assert.IsTrue(tile.enabled);
                Assert.IsTrue(tile.gameObject.activeInHierarchy);
                Assert.AreSame(sprite, tile.sprite);
                Vector3 localPosition = tile.transform.localPosition;
                Vector3 worldPosition = tile.transform.position;
                Assert.That(localPosition.x, Is.InRange(-float.MaxValue, float.MaxValue));
                Assert.That(localPosition.y, Is.InRange(-float.MaxValue, float.MaxValue));
                Assert.That(localPosition.z, Is.InRange(-float.MaxValue, float.MaxValue));
                Assert.That(worldPosition.x, Is.InRange(-float.MaxValue, float.MaxValue));
                Assert.That(worldPosition.y, Is.InRange(-float.MaxValue, float.MaxValue));
                Assert.That(worldPosition.z, Is.InRange(-float.MaxValue, float.MaxValue));
            }

            return tiles;
        }

        private void SetTiling(bool horizontal, bool vertical, Vector2Int padding)
        {
            SerializedObject settings = new(layer);
            settings.FindProperty("tileHorizontally").boolValue = horizontal;
            settings.FindProperty("tileVertically").boolValue = vertical;
            settings.FindProperty("paddingTiles").vector2IntValue = padding;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ConfigureTwoAxisPool()
        {
            SerializedObject settings = new(layer);
            settings.FindProperty("tileVertically").boolValue = true;
            settings.FindProperty("paddingTiles").vector2IntValue = new Vector2Int(2, 2);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void ActivateLayer()
        {
            layerObject.SetActive(true);
            layer.SetTargetCamera(camera);
        }

        private void AssertTilesMatchTemplate(Vector3 expectedSize, Vector3 expectedPosition)
        {
            List<SpriteRenderer> tiles = GeneratedTiles();
            tiles.Sort((SpriteRenderer left, SpriteRenderer right) =>
                left.transform.position.x.CompareTo(right.transform.position.x));
            Assert.GreaterOrEqual(tiles.Count, 3);
            bool containsAnchor = false;
            for (int i = 0; i < tiles.Count; i++)
            {
                SpriteRenderer tile = tiles[i];
                Assert.That(tile.bounds.size.x, Is.EqualTo(expectedSize.x).Within(0.001f));
                Assert.That(tile.bounds.size.y, Is.EqualTo(expectedSize.y).Within(0.001f));
                Assert.That(tile.transform.position.y, Is.EqualTo(expectedPosition.y).Within(0.001f));
                containsAnchor |= Mathf.Abs(tile.transform.position.x - expectedPosition.x) < 0.001f;
                if (i > 0)
                {
                    Assert.That(tile.transform.position.x - tiles[i - 1].transform.position.x,
                        Is.EqualTo(expectedSize.x).Within(0.001f));
                }
            }

            Assert.IsTrue(containsAnchor, "The template position must anchor one tile.");
        }

        private List<SpriteRenderer> GeneratedTiles()
        {
            List<SpriteRenderer> result = new();
            foreach (SpriteRenderer renderer in layerObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.hideFlags == HideFlags.HideAndDontSave)
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        private Sprite CreateSprite(int width, int height)
        {
            Texture2D texture = new(width, height);
            resources.Add(texture);
            Sprite created = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 8f);
            resources.Add(created);
            return created;
        }
    }

    [TestFixture]
    public class Audit20261023NeoParallaxNativeLifecycleTests
    {
        private readonly List<Object> resources = new();
        private GameObject cameraObject;
        private GameObject layerObject;
        private Camera camera;
        private ParallaxLayer layer;
        private SpriteRenderer template;
        private Sprite sprite;
        private Random.State randomState;
        private bool capturedRandomState;
        private bool ownsPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsFalse(Application.isPlaying, "This fixture requires its own EnterPlayMode transition from Edit Mode.");
            yield return new EnterPlayMode();
            ownsPlayMode = true;
            Assert.IsTrue(Application.isPlaying);
            randomState = Random.state;
            capturedRandomState = true;

            cameraObject = new GameObject("AuditParallaxNativeCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.aspect = 1f;

            layerObject = new GameObject("AuditParallaxNativeLayer");
            layerObject.SetActive(false);
            template = layerObject.AddComponent<SpriteRenderer>();
            sprite = CreateSprite();
            template.sprite = sprite;
            layer = layerObject.AddComponent<ParallaxLayer>();

            SerializedObject settings = new(layer);
            settings.FindProperty("targetCamera").objectReferenceValue = camera;
            settings.FindProperty("templateRenderer").objectReferenceValue = template;
            settings.FindProperty("useMainCameraFallback").boolValue = false;
            settings.FindProperty("randomiseOnInit").boolValue = false;
            settings.FindProperty("parallaxMultiplier").vector2Value = Vector2.zero;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(layerObject.activeInHierarchy);
            Assert.AreEqual(0, GeneratedTiles().Count);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            List<string> cleanupFaults = new();
            DestroyOwned(layerObject);
            DestroyOwned(cameraObject);
            if (ownsPlayMode && Application.isPlaying)
            {
                yield return WaitForNativeFrames("GameObject teardown", cleanupFaults);
            }

            for (int i = resources.Count - 1; i >= 0; i--)
            {
                DestroyOwned(resources[i]);
            }

            resources.Clear();
            if (ownsPlayMode && Application.isPlaying)
            {
                yield return WaitForNativeFrames("Sprite/texture teardown", cleanupFaults);
            }

            if (capturedRandomState)
            {
                Random.state = randomState;
                capturedRandomState = false;
            }

            if (ownsPlayMode && Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            ownsPlayMode = false;
            Assert.IsEmpty(cleanupFaults, string.Join("\n", cleanupFaults));
        }

        [UnityTest]
        public IEnumerator CameraTeleport_RecentresBothAxesWithoutChangingPoolSize()
        {
            ConfigureTwoAxisPool();
            layerObject.SetActive(true);
            yield return WaitForNativeFrames("Teleport pool settling");
            int initialCount = AssertActivePool(100).Count;
            camera.transform.position = new Vector3(1000f, -750f, 0f);

            yield return WaitForNativeFrames("Camera teleport observation");

            List<SpriteRenderer> tiles = AssertActivePool(initialCount);
            Assert.AreEqual(Vector3.zero, layerObject.transform.position);
            foreach (SpriteRenderer tile in tiles)
            {
                Assert.That(Mathf.Abs(tile.transform.position.x - camera.transform.position.x),
                    Is.LessThanOrEqualTo(5.001f), "Horizontal padding must stay centred around the camera.");
                Assert.That(Mathf.Abs(tile.transform.position.y - camera.transform.position.y),
                    Is.LessThanOrEqualTo(5.001f), "Vertical padding must stay centred around the camera.");
            }
        }

        [UnityTest]
        public IEnumerator StationaryCamera_DoesNotRerollPaddingTilesEveryFrame()
        {
            ConfigureTwoAxisPool();
            SerializedObject settings = new(layer);
            settings.FindProperty("randomiseOnRecycle").boolValue = true;
            settings.FindProperty("spriteVariants").arraySize = 1;
            settings.FindProperty("spriteVariants").GetArrayElementAtIndex(0).objectReferenceValue = CreateSprite();
            settings.ApplyModifiedPropertiesWithoutUndo();
            layerObject.SetActive(true);
            yield return WaitForNativeFrames("Stationary pool settling");
            List<SpriteRenderer> tiles = AssertActivePool(100, false);
            List<Sprite> previousSprites = new();
            foreach (SpriteRenderer tile in tiles)
            {
                previousSprites.Add(tile.sprite);
            }

            Random.State settledRandomState = Random.state;
            yield return WaitForNativeFrames("Stationary RNG/sprite observation");

            Assert.AreEqual(settledRandomState, Random.state,
                "Stationary padding must not consume random choices or flicker between sprite variants.");
            Assert.AreEqual(100, GeneratedTiles().Count);
            for (int i = 0; i < tiles.Count; i++)
            {
                Assert.AreSame(previousSprites[i], tiles[i].sprite);
            }
        }

        [UnityTest]
        public IEnumerator EnabledRootSource_DisableAndDestroy_RestoreStateAndRetireTiles()
        {
            yield return ObserveNativeLifecycle(true, 0);
        }

        [UnityTest]
        public IEnumerator DisabledRootSource_DisableAndDestroy_RestoreStateAndRetireTiles()
        {
            yield return ObserveNativeLifecycle(false, 0);
        }

        [UnityTest]
        public IEnumerator DirectChildSource_DisableAndDestroy_PreserveGeometryAndOwnership()
        {
            yield return ObserveNativeLifecycle(true, 1);
        }

        [UnityTest]
        public IEnumerator NestedChildSource_DisableAndDestroy_PreserveGeometryAndOwnership()
        {
            yield return ObserveNativeLifecycle(true, 2);
        }

        private IEnumerator ObserveNativeLifecycle(bool initiallyEnabled, int templateDepth)
        {
            Vector3 expectedSize = new(1f, 1f, 0f);
            int expectedCount = 8;
            if (templateDepth > 0)
            {
                Object.Destroy(template);
                yield return WaitForNativeFrames("Root template destruction");
                Assert.IsTrue(template == null);
                Transform parent = layerObject.transform;
                if (templateDepth == 2)
                {
                    GameObject group = new("AuditParallaxNativeTemplateGroup");
                    group.transform.SetParent(parent, false);
                    group.transform.localScale = new Vector3(0.5f, 2f, 1f);
                    parent = group.transform;
                }

                GameObject source = new("AuditParallaxNativeTemplate");
                source.transform.SetParent(parent, false);
                source.transform.localPosition = new Vector3(0.5f, 0.25f, 0f);
                source.transform.localScale = new Vector3(2f, 3f, 1f);
                template = source.AddComponent<SpriteRenderer>();
                template.sprite = sprite;
                layerObject.transform.localScale = new Vector3(1.5f, 2f, 1f);
                expectedSize = templateDepth == 2 ? new Vector3(1.5f, 12f, 0f) : new Vector3(3f, 6f, 0f);
                expectedCount = templateDepth == 2 ? 7 : 6;
                SerializedObject settings = new(layer);
                settings.FindProperty("templateRenderer").objectReferenceValue = template;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            template.enabled = initiallyEnabled;
            Vector3 expectedSourcePosition = template.transform.position;
            Vector3 expectedSourceScale = template.transform.localScale;
            Transform expectedSourceParent = template.transform.parent;
            GameObject decoration = new("AuditParallaxNativeDecoration");
            decoration.transform.SetParent(layerObject.transform, false);
            decoration.transform.localPosition = new Vector3(2f, 3f, 4f);

            layerObject.SetActive(true);
            Assert.IsTrue(layer.isActiveAndEnabled);
            Assert.AreSame(camera, layer.TargetCamera);
            yield return WaitForNativeFrames("Lifecycle pool settling");
            List<SpriteRenderer> retiredTiles = AssertActivePool(expectedCount);
            AssertGeometry(retiredTiles, expectedSize, expectedSourcePosition);
            Assert.IsFalse(template.enabled);

            layer.enabled = false;
            Assert.AreEqual(initiallyEnabled, template.enabled);
            foreach (SpriteRenderer tile in retiredTiles)
            {
                Assert.IsTrue(tile == null || !tile.gameObject.activeSelf,
                    "Retired tiles must stop rendering before deferred destruction.");
            }

            yield return WaitForNativeFrames("Disabled pool destruction");

            Assert.AreEqual(0, GeneratedTiles().Count);
            foreach (SpriteRenderer tile in retiredTiles)
            {
                Assert.IsTrue(tile == null);
            }

            Assert.AreEqual(expectedSourcePosition, template.transform.position);
            Assert.AreEqual(expectedSourceScale, template.transform.localScale);
            Assert.AreSame(expectedSourceParent, template.transform.parent);
            Assert.IsTrue(decoration != null);
            Assert.AreSame(layerObject.transform, decoration.transform.parent);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), decoration.transform.localPosition);

            layer.enabled = true;
            yield return WaitForNativeFrames("Re-enabled pool settling");
            List<SpriteRenderer> replacementTiles = AssertActivePool(expectedCount);
            AssertGeometry(replacementTiles, expectedSize, expectedSourcePosition);
            Assert.IsFalse(template.enabled);

            Object.Destroy(layer);
            yield return WaitForNativeFrames("Layer component destruction");

            Assert.IsTrue(layer == null);
            Assert.AreEqual(0, GeneratedTiles().Count);
            Assert.AreEqual(initiallyEnabled, template.enabled);
            Assert.AreEqual(expectedSourcePosition, template.transform.position);
            Assert.AreEqual(expectedSourceScale, template.transform.localScale);
            Assert.AreSame(expectedSourceParent, template.transform.parent);
            Assert.IsTrue(decoration != null);
            Assert.AreSame(layerObject.transform, decoration.transform.parent);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), decoration.transform.localPosition);
            Assert.IsTrue(decoration.activeSelf);
        }

        private static IEnumerator WaitForNativeFrames(string boundary, List<string> cleanupFaults = null)
        {
            int initialFrame = Time.frameCount;
            System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
            // Editor coroutine polls are not native frames. Two advances include a completed LateUpdate.
            while (Application.isPlaying && unchecked((uint)(Time.frameCount - initialFrame)) < 2u &&
                   elapsed.Elapsed.TotalSeconds < 10d)
            {
                yield return null;
            }

            if (!Application.isPlaying || unchecked((uint)(Time.frameCount - initialFrame)) < 2u)
            {
                string failure = $"{boundary}: expected two native frame advances while playing; " +
                                 $"frames {initialFrame} -> {Time.frameCount}, playing={Application.isPlaying}, " +
                                 $"elapsed={elapsed.Elapsed.TotalSeconds:F3}s.";
                if (cleanupFaults == null)
                {
                    Assert.Fail(failure);
                }
                else
                {
                    // Report after releasing owned resources and the acquired Play session.
                    cleanupFaults.Add(failure);
                }
            }
        }

        private List<SpriteRenderer> AssertActivePool(int expectedCount, bool expectTemplateSprite = true)
        {
            Assert.IsTrue(Application.isPlaying);
            Assert.IsTrue(layerObject.activeInHierarchy);
            Assert.IsTrue(layer.isActiveAndEnabled);
            Assert.AreSame(camera, layer.TargetCamera);
            List<SpriteRenderer> tiles = GeneratedTiles();
            Assert.AreEqual(expectedCount, tiles.Count);
            foreach (SpriteRenderer tile in tiles)
            {
                Assert.IsTrue(tile.enabled);
                Assert.IsTrue(tile.gameObject.activeInHierarchy);
                if (expectTemplateSprite)
                {
                    Assert.AreSame(sprite, tile.sprite);
                }
                else
                {
                    Assert.IsTrue(tile.sprite != null);
                }
            }

            return tiles;
        }

        private static void AssertGeometry(List<SpriteRenderer> tiles, Vector3 expectedSize, Vector3 expectedPosition)
        {
            tiles.Sort((SpriteRenderer left, SpriteRenderer right) =>
                left.transform.position.x.CompareTo(right.transform.position.x));
            bool containsAnchor = false;
            for (int i = 0; i < tiles.Count; i++)
            {
                Assert.That(tiles[i].bounds.size.x, Is.EqualTo(expectedSize.x).Within(0.001f));
                Assert.That(tiles[i].bounds.size.y, Is.EqualTo(expectedSize.y).Within(0.001f));
                Assert.That(tiles[i].transform.position.y, Is.EqualTo(expectedPosition.y).Within(0.001f));
                containsAnchor |= Mathf.Abs(tiles[i].transform.position.x - expectedPosition.x) < 0.001f;
                if (i > 0)
                {
                    Assert.That(tiles[i].transform.position.x - tiles[i - 1].transform.position.x,
                        Is.EqualTo(expectedSize.x).Within(0.001f));
                }
            }

            Assert.IsTrue(containsAnchor);
        }

        private void ConfigureTwoAxisPool()
        {
            SerializedObject settings = new(layer);
            settings.FindProperty("tileVertically").boolValue = true;
            settings.FindProperty("paddingTiles").vector2IntValue = new Vector2Int(2, 2);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private List<SpriteRenderer> GeneratedTiles()
        {
            List<SpriteRenderer> result = new();
            foreach (SpriteRenderer renderer in layerObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.hideFlags == HideFlags.HideAndDontSave)
                {
                    result.Add(renderer);
                }
            }

            return result;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new(8, 8);
            resources.Add(texture);
            Sprite created = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            resources.Add(created);
            return created;
        }

        private static void DestroyOwned(Object owned)
        {
            if (owned == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(owned);
            }
            else
            {
                Object.DestroyImmediate(owned);
            }
        }
    }
}
