using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Neo.Samples;
using Neo.Samples.Survivor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contracts of the Abilities group scene builders (AbilitiesNoCodeDemo, SurvivorDemo,
    ///     VampireSurvivorMCP): each builds a temporary scene twice with persistent wiring and zero churn on the
    ///     second pass, or fails naming the scene and the step when its hand-authored game is missing.
    /// </summary>
    public sealed class AbilitiesDemoBuildersTests
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
        public void AbilitiesNoCodeBuilder_AuthorsNoCodeWiringIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("AbilitiesNoCodeDemoBuilder.Build");
            OpenTempScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            AbilitiesDemoGuide guide = Single<AbilitiesDemoGuide>();
            Assert.That(guide.IsSceneBuilt, Is.True);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Hit Player 20").onClick), Is.EqualTo("AbilityNoCodeAction.Execute"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Cast Heal").onClick), Is.EqualTo("AbilityNoCodeAction.Execute"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Zap dummy").onClick), Is.EqualTo("AbilitiesDemoGuide.Zap"));
            Assert.That(DemoWiring.Describe(FindToggle().onValueChanged),
                Does.Contain("AbilityAutoCaster.set_enabled").And.Contain("AbilitiesDemoGuide.OnAutoZapChanged"));
            Assert.That(FindObject("How to play"), Is.Not.Null, "The play-mode smoke test checks this panel by name.");
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
        }

        [Test]
        public void AbilitiesTriggersBuilder_AuthorsTheLessonsIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("AbilitiesTriggersDemoBuilder.Build");
            OpenTempScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            AbilitiesTriggersDemoGuide guide = Single<AbilitiesTriggersDemoGuide>();
            Assert.That(guide.IsSceneBuilt, Is.True);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Strike golem (20)").onClick),
                Is.EqualTo("AbilitiesTriggersDemoGuide.Strike"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Empower").onClick), Is.EqualTo("AbilitiesTriggersDemoGuide.Empower"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Mark golem").onClick), Is.EqualTo("AbilitiesTriggersDemoGuide.Mark"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Venom dart").onClick),
                Is.EqualTo("AbilitiesTriggersDemoGuide.PoisonDart"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Place formation").onClick),
                Is.EqualTo("AbilitiesTriggersDemoGuide.Formation"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Golem smashes hero (70)").onClick),
                Is.EqualTo("AbilitiesTriggersDemoGuide.GolemSmash"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset demo").onClick),
                Is.EqualTo("AbilitiesTriggersDemoGuide.ResetDemo"));
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);
            Assert.That(FindObject("Shield Bubble").activeSelf, Is.False, "Indicators are saved hidden.");

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
        }

        [Test]
        public void SurvivorBuilder_AuthorsAPlayableSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("SurvivorDemoBuilder.Build");
            OpenTempScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            SurvivorGame game = Single<SurvivorGame>();
            Assert.That(game.IsSceneBuilt, Is.True);
            Assert.That(game.Hud.IsBuilt, Is.True);
            Assert.That(game.Hud.CardCount, Is.EqualTo(3));
            Assert.That(game.Hud.IsUpgradePickerOpen, Is.False, "Overlays are saved hidden.");
            Assert.That(game.Hud.IsGameOverShown, Is.False);
            for (int i = 1; i <= 3; i++)
            {
                Assert.That(DemoWiring.Describe(FindButton("Btn_Upgrade " + i).onClick), Is.EqualTo("SurvivorGame.PickUpgrade"));
            }

            Assert.That(DemoWiring.Describe(FindButton("Btn_Play again").onClick), Is.EqualTo("SurvivorGame.RestartRun"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Level up now").onClick), Is.EqualTo("SurvivorGame.GrantLevelUp"));
            GameObject templates = FindObject("Templates");
            Assert.That(templates, Is.Not.Null);
            Assert.That(templates.activeSelf, Is.False, "Pooled templates are saved inactive.");

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            AssertIdempotent(first, second);
        }

        [Test]
        public void VampireSurvivorBuilder_WithoutTheHandAuthoredGame_FailsNamingSceneAndStep()
        {
            DemoSceneBuilderInfo builder = Builder("VampireSurvivorDemoBuilder.Build");
            OpenTempScene();
            // WHY: the runner logs the builder's root cause (the innermost exception) before reporting the step.
            LogAssert.Expect(LogType.Exception, new Regex("no hand-authored RpgCharacter|find the hand-authored player"));

            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });

            Assert.That(result.Success, Is.False);
            string errors = string.Join("\n", result.Errors);
            Assert.That(errors, Does.Contain("VampireSurvivorMCP.unity").And.Contain("find the hand-authored player"));
        }

        private static void AssertIdempotent(DemoSceneResult first, DemoSceneResult second)
        {
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners), second.Summary());
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private void OpenTempScene()
        {
            string folderName = "__NeoAbilitiesBuilderTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/AbilitiesBuilderTest.unity";
            File.WriteAllText(path, EmptySceneYaml);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private GameObject FindObject(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == name)
                    {
                        return node.gameObject;
                    }
                }
            }

            return null;
        }

        private Button FindButton(string name)
        {
            GameObject host = FindObject(name);
            Button button = host != null ? host.GetComponent<Button>() : null;
            Assert.That(button, Is.Not.Null, "Missing button " + name);
            return button;
        }

        private Toggle FindToggle()
        {
            List<Toggle> toggles = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                toggles.AddRange(root.GetComponentsInChildren<Toggle>(true));
            }

            Assert.That(toggles.Count, Is.EqualTo(1), "The NoCode demo has exactly one toggle (Auto Zap).");
            return toggles[0];
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
