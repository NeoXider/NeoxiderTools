using System;
using System.Collections.Generic;
using System.IO;
using Neo.Samples;
using Neo.Settings;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>Edit-mode contracts of the demo scene authoring kit (runs only while the Demo sample is visible).</summary>
    public sealed class DemoSceneKitTests
    {
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
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightingDataAsset: {fileID: 0}
  m_LightingSettings: {fileID: 0}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    accuratePlacement: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots: []
";

        private readonly List<Object> _owned = new();
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
            foreach (Object owned in _owned)
            {
                if (owned != null)
                {
                    Object.DestroyImmediate(owned);
                }
            }

            _owned.Clear();
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
        public void LogFeed_ShowsNewestFirstBoundedAndCollapsesRepeats()
        {
            GameObject host = Own(new GameObject("Feed", typeof(RectTransform)));
            TextMeshProUGUI text = host.AddComponent<TextMeshProUGUI>();
            DemoLogFeed feed = host.AddComponent<DemoLogFeed>();
            SerializedObject serialized = new(feed);
            serialized.FindProperty("_maxLines").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            feed.SetTarget(text);

            feed.Log("a");
            feed.Log("b");
            feed.Log("b");
            feed.Log("c");
            feed.Log("d");
            feed.Log(string.Empty);

            Assert.That(feed.Count, Is.EqualTo(3));
            Assert.That(feed.GetLine(0), Is.EqualTo("d"));
            Assert.That(feed.GetLine(1), Is.EqualTo("c"));
            Assert.That(feed.GetLine(2), Is.EqualTo("b"));
            Assert.That(feed.GetRepeatCount(2), Is.EqualTo(2));
            Assert.That(text.text, Does.Contain("×2"));

            feed.Clear();
            Assert.That(feed.Count, Is.Zero);
            Assert.That(text.text, Does.Contain("ready"));
        }

        [Test]
        public void ValueReadout_FormatsEventValues()
        {
            GameObject host = Own(new GameObject("Readout", typeof(RectTransform)));
            TextMeshProUGUI text = host.AddComponent<TextMeshProUGUI>();
            DemoValueReadout readout = host.AddComponent<DemoValueReadout>();
            readout.Configure(text, "0.0", "x", "!");

            readout.SetFloat(2.25f);
            Assert.That(text.text, Is.EqualTo("x2.3!").Or.EqualTo("x2.2!"));
            readout.SetBool(true);
            Assert.That(text.text, Is.EqualTo("xON!"));
            readout.SetInt(7);
            Assert.That(readout.LastText, Is.EqualTo("x7!"));
        }

        [Test]
        public void InfoCard_AppliesTextsAndCollapsesAuthoredParts()
        {
            GameObject host = Own(new GameObject("Info"));
            ModuleDemoSceneInfo info = host.AddComponent<ModuleDemoSceneInfo>();
            GameObject card = Own(new GameObject("Card", typeof(RectTransform)));
            TMP_Text title = NewText(card.transform, "Title");
            TMP_Text description = NewText(card.transform, "Description");
            TMP_Text hints = NewText(card.transform, "Hints");
            TMP_Text collapse = NewText(card.transform, "Collapse");
            GameObject body = new("Body", typeof(RectTransform));
            body.transform.SetParent(card.transform, false);

            info.AssignOverlay(card, title, description, hints, collapse, body);
            info.Configure("Module X", "Purpose", "Api", "Workflow", "Verify");
            info.SetHints("press A");

            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(title.text, Is.EqualTo("Module X"));
            Assert.That(description.text, Is.EqualTo("Purpose"));
            Assert.That(hints.text, Does.Contain("Runtime API").And.Contain("press A"));
            info.ToggleCollapsed();
            Assert.That(body.activeSelf, Is.False);
            Assert.That(collapse.text, Is.EqualTo("+"));
            info.ToggleCollapsed();
            Assert.That(body.activeSelf, Is.True);
            Assert.That(collapse.text, Is.EqualTo("-"));
        }

        [Test]
        public void SampleRoot_ResolvesTheCompilingDemoSample()
        {
            Assert.That(DemoSampleRoots.TryResolveDemoRoot(out string root, out string error), Is.True, error);
            Assert.That(AssetDatabase.IsValidFolder(root + "/Scenes"), Is.True, root);
        }

        [Test]
        public void Wiring_RejectsLambdasAndDeduplicatesIdenticalCalls()
        {
            GameObject host = Own(new GameObject("Wiring", typeof(RectTransform)));
            Button button = host.AddComponent<Button>();
            DemoLogFeed feed = host.AddComponent<DemoLogFeed>();

            Assert.Throws<ArgumentException>(() => DemoWiring.AddVoid(button.onClick, () => { }));
            DemoWiring.AddString(button.onClick, feed.Log, "one");
            DemoWiring.AddString(button.onClick, feed.Log, "one");
            Assert.That(DemoWiring.Count(button.onClick), Is.EqualTo(1));
            DemoWiring.AddString(button.onClick, feed.Log, "two");
            Assert.That(DemoWiring.Count(button.onClick), Is.EqualTo(2));
            DemoWiring.Clear(button.onClick);
            Assert.That(DemoWiring.Count(button.onClick), Is.Zero);
        }

        [Test]
        public void Frame_RebuildReusesObjectsPrunesStaleRowsAndKeepsHandAuthoredObjects()
        {
            OpenTempScene();
            GameObject handAuthored = new("Hand Authored");
            DemoLogFeed handFeed = handAuthored.AddComponent<DemoLogFeed>();

            DemoSceneBuildContext first = Build(ctx => BuildSample(ctx, true));
            Assert.That(first.Created, Is.GreaterThan(10));
            Button save = FindButton("Btn_Save");
            Assert.That(DemoWiring.Count(save.onClick), Is.EqualTo(1));
            Assert.That(FindButton("Btn_Extra"), Is.Not.Null);

            DemoSceneBuildContext second = Build(ctx => BuildSample(ctx, true));
            Assert.That(second.Created, Is.Zero, "A rebuild with the same builder must reuse every object.");
            Assert.That(second.Removed, Is.Zero);
            Assert.That(FindButton("Btn_Save"), Is.SameAs(save));
            Assert.That(DemoWiring.Count(save.onClick), Is.EqualTo(1), "Listeners must not pile up across rebuilds.");

            DemoSceneBuildContext third = Build(ctx => BuildSample(ctx, false));
            Assert.That(third.Removed, Is.GreaterThan(0));
            Assert.That(FindButton("Btn_Extra"), Is.Null, "Rows the builder no longer makes are pruned.");
            Assert.That(handAuthored != null && handFeed != null, Is.True, "Hand-authored objects are never touched.");
            Assert.That(third.Warnings, Is.Empty, string.Join("\n", third.Warnings));
        }

        [Test]
        public void WorldLabel_RebuildWithAnotherPositionMovesTheSavedLabel()
        {
            OpenTempScene();
            string path = _scene.path;
            Build(ctx => ProbeLabel(ctx, new Vector3(1f, 2f, -0.5f)));
            Assert.That(EditorSceneManager.SaveScene(_scene), Is.True);
            ReopenTempScene(path);

            Build(ctx => ProbeLabel(ctx, new Vector3(-3f, -4f, -0.25f)));
            Assert.That(EditorSceneManager.SaveScene(_scene), Is.True);
            ReopenTempScene(path);

            RectTransform label = null;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (RectTransform node in root.GetComponentsInChildren<RectTransform>(true))
                {
                    label = node.name == "Probe Label" ? node : label;
                }
            }

            Assert.That(label, Is.Not.Null);
            Assert.That(label.localPosition, Is.EqualTo(new Vector3(-3f, -4f, -0.25f)), "the saved label moved");
            Assert.That(label.anchoredPosition, Is.EqualTo(new Vector2(-3f, -4f)));
            StringAssert.Contains("m_AnchoredPosition: {x: -3, y: -4}", File.ReadAllText(path));
        }

        [Test]
        public void SettingsBuilder_AuthorsAWiredSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith("SettingsDemoBuilder.Build", StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, "SettingsDemoBuilder must be registered.");
            Assert.That(builder.Error, Is.Null);
            OpenTempScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            SettingsDemoController controller = Single<SettingsDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(controller.Settings, Is.Not.Null);

            SerializedObject serialized = new(controller);
            Slider slider = (Slider)serialized.FindProperty("_sensitivitySlider").objectReferenceValue;
            Assert.That(slider, Is.Not.Null);
            Assert.That(DemoWiring.Describe(slider.onValueChanged),
                Does.Contain("GameSettingsComponent.SetMouseSensitivityForMenu").And.Contain("SettingsDemoController.LogSensitivity"));
            Button thirty = FindButton("Btn_30 fps");
            Assert.That(DemoWiring.Describe(thirty.onClick), Is.EqualTo("SettingsDemoController.SetFramerateCap"));
            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Neo.Settings"));
            int sliderListeners = DemoWiring.Count(slider.onValueChanged);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(DemoWiring.Count(slider.onValueChanged), Is.EqualTo(sliderListeners));
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        private static void BuildSample(DemoSceneBuildContext ctx, bool withExtraRow)
        {
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Info");
            DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Kit test", DemoPalette.Cyan));
            frame.Content.Section("Rows");
            frame.Content.ValueLabel("Value");
            frame.Content.Toggle("Flag", true);
            frame.Content.Slider("Amount", 0f, 1f, 0.5f);
            Button[] buttons = frame.Content.ButtonRow("Save", "Load");
            DemoWiring.AddString(buttons[0].onClick, frame.Log.Log, "saved");
            if (withExtraRow)
            {
                frame.Content.Button("Extra");
            }

            frame.InfoCard(info);
        }

        private DemoSceneBuildContext Build(Action<DemoSceneBuildContext> build)
        {
            DemoSceneBuildContext context = new(_scene);
            context.Begin();
            build(context);
            context.Finish();
            return context;
        }

        private static void ProbeLabel(DemoSceneBuildContext ctx, Vector3 position)
        {
            DemoSceneKit.WorldLabel(ctx, ctx.Root("World").transform, "Probe Label", "probe", position, 3f, Color.white,
                new Vector2(4f, 1f));
        }

        private void ReopenTempScene(string path)
        {
            EditorSceneManager.CloseScene(_scene, true);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not reopen: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private void OpenTempScene()
        {
            string folderName = "__NeoDemoKitTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/KitTest.unity";
            File.WriteAllText(path, EmptySceneYaml);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
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

        private static TMP_Text NewText(Transform parent, string name)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private T Own<T>(T value) where T : Object
        {
            _owned.Add(value);
            return value;
        }
    }
}
