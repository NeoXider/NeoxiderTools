using System;
using System.Collections.Generic;
using System.IO;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contracts of the CameraAspectRatio demo builder: an empty scene is authored with persistent wiring
    ///     (including the NoCode slider → CameraAspectRatioScaler.ReferenceOrthographicSize setter) and rebuilds without
    ///     changes, and the shipped scene keeps its hand-authored camera and background while the scaler gets a stored
    ///     9.6 reference instead of the crept camera size. The scaler is read through SerializedObject by type name so
    ///     this assembly needs no Neo.Tools.Components reference.
    /// </summary>
    public sealed class CameraAspectRatioBuilderTests
    {
        private const string ShippedScene = "Scenes/Tools/CameraAspectRatio.unity";
        private const string ScalerType = "CameraAspectRatioScaler";
        private const float Reference = 9.6f;

        // WHY: a saved scene asset can be opened additively next to the test runner's untitled scene.
        private const string EmptySceneYaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.2, g: 0.2, b: 0.2, a: 1}
  m_AmbientEquatorColor: {r: 0.1, g: 0.1, b: 0.1, a: 1}
  m_AmbientGroundColor: {r: 0.05, g: 0.05, b: 0.05, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.4, g: 0.4, b: 0.4, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots: []
";

        private string _folder;
        private Scene _scene;
        private Scene _previousActive;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (_previousActive.IsValid() && _previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActive);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
                _folder = null;
            }
        }

        [Test]
        public void Builder_AuthorsAWiredSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder();
            OpenEmptyScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            CameraAspectRatioDemoController controller = Single<CameraAspectRatioDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(controller.DeviceCount, Is.EqualTo(3));

            Assert.That(DemoWiring.Describe(FindButton("Btn_Fit Width").onClick),
                Is.EqualTo("CameraAspectRatioDemoController.SetScaleMode"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Landscape 16:9").onClick),
                Is.EqualTo("CameraAspectRatioDemoController.SetDesignPreset"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Ultrawide 21:9").onClick),
                Is.EqualTo("CameraAspectRatioDemoController.SetOutput"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset demo").onClick),
                Is.EqualTo("CameraAspectRatioDemoController.ResetDemo"));

            SerializedObject references = new(controller);
            Slider slider = (Slider)references.FindProperty("_referenceSlider").objectReferenceValue;
            Assert.That(slider, Is.Not.Null);
            Assert.That(DemoWiring.Describe(slider.onValueChanged),
                Is.EqualTo("DemoValueReadout.SetFloat; CameraAspectRatioScaler.set_ReferenceOrthographicSize; " +
                           "CameraAspectRatioDemoController.OnReferenceSizeChanged"));
            Assert.That(slider.value, Is.EqualTo(Reference).Within(0.001f));
            Assert.That(DemoWiring.Describe(Single<Toggle>().onValueChanged),
                Is.EqualTo("GameObject.SetActive; GameObject.SetActive; ModuleDemoSceneInfo.SetOverlayVisible"));

            AssertScalerAuthored(references);
            SpriteRenderer fill = (SpriteRenderer)references.FindProperty("_frameFill").objectReferenceValue;
            Assert.That(fill.size.x, Is.EqualTo(10.8f).Within(0.001f), "Design frame width = 2 × 9.6 × 1080 / 1920.");
            Assert.That(fill.size.y, Is.EqualTo(19.2f).Within(0.001f));
            GameObject preview = (GameObject)references.FindProperty("_previewRoot").objectReferenceValue;
            Assert.That(preview.activeSelf, Is.False, "The device preview is authored hidden.");
            SerializedProperty textures = references.FindProperty("_deviceTextures");
            for (int i = 0; i < textures.arraySize; i++)
            {
                Object texture = textures.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(texture, Is.Not.Null, "Device render texture " + i);
                Assert.That(EditorUtility.IsPersistent(texture), Is.True, "Device render textures are assets.");
            }

            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        [Test]
        public void Builder_KeepsTheShippedCameraAndBackgroundAndFixesTheReference()
        {
            DemoSceneBuilderInfo builder = Builder();
            OpenSceneCopy(ShippedScene);
            GameObject camera = Root("Main Camera");
            GameObject background = Root("9-Sliced");
            Assert.That(camera, Is.Not.Null, "The shipped scene has a hand-authored 'Main Camera'.");
            Assert.That(background, Is.Not.Null, "The shipped scene has the hand-authored '9-Sliced' background.");
            Component scaler = camera.GetComponent(ScalerType);
            Assert.That(scaler, Is.Not.Null);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Main Camera"), Is.SameAs(camera));
            Assert.That(Root("9-Sliced"), Is.SameAs(background));
            Assert.That(camera.GetComponent(ScalerType), Is.SameAs(scaler), "The authored scaler is reused.");

            CameraAspectRatioDemoController controller = Single<CameraAspectRatioDemoController>();
            SerializedObject references = new(controller);
            Assert.That(references.FindProperty("_camera").objectReferenceValue, Is.SameAs(camera.GetComponent<Camera>()));
            AssertScalerAuthored(references);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            AssertScalerAuthored(new SerializedObject(controller));
        }

        private static void AssertScalerAuthored(SerializedObject controller)
        {
            Object scalerObject = controller.FindProperty("_scaler").objectReferenceValue;
            Camera camera = (Camera)controller.FindProperty("_camera").objectReferenceValue;
            Assert.That(scalerObject, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            Assert.That(((Component)scalerObject).gameObject, Is.SameAs(camera.gameObject));

            SerializedObject scaler = new(scalerObject);
            Assert.That(scaler.FindProperty("_referenceSize").floatValue, Is.EqualTo(Reference).Within(0.001f),
                "The reference is stored on the scaler, not re-captured from a scaled camera.");
            Assert.That(scaler.FindProperty("scaleMultiplier").floatValue, Is.EqualTo(1f).Within(0.001f));
            Assert.That(scaler.FindProperty("targetResolution").vector2Value, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(scaler.FindProperty("scaleMode").intValue, Is.EqualTo(2), "FitBoth");
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(Reference).Within(0.001f));
        }

        private static DemoSceneBuilderInfo Builder()
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith("CameraAspectRatioBuilder.Build", StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, "CameraAspectRatioBuilder.Build must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private void OpenEmptyScene()
        {
            string path = NewFolder() + "/CameraAspectRatioTest.unity";
            File.WriteAllText(path, EmptySceneYaml);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            OpenAdditive(path);
        }

        private void OpenSceneCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Shipped scene missing: " + source);
            string path = NewFolder() + "/" + Path.GetFileName(relativeScenePath);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            OpenAdditive(path);
        }

        private string NewFolder()
        {
            string folderName = "__NeoCameraAspectTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            return _folder;
        }

        private void OpenAdditive(string path)
        {
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private GameObject Root(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private Button FindButton(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == name)
                    {
                        return button;
                    }
                }
            }

            Assert.Fail("Button '" + name + "' not found.");
            return null;
        }

        private T Single<T>() where T : Component
        {
            List<T> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }
    }
}
