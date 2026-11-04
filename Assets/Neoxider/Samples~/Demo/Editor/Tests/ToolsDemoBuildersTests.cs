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
    ///     Edit-mode contracts of the Tools demo builders (ChanceSystemExample, Dialogue): an empty scene is authored
    ///     with persistent wiring and rebuilds without changes, and the shipped pre-kit scene migrates while its
    ///     hand-authored module objects and data survive. Module types are checked by name so this assembly needs
    ///     no extra module references.
    /// </summary>
    public sealed class ToolsDemoBuildersTests
    {
        private const string ChanceScene = "Scenes/Tools/ChanceSystemExample.unity";
        private const string DialogueScene = "Scenes/Tools/Dialogue.unity";

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
        public void ChanceBuilder_AuthorsAWiredSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("ChanceSystemExampleBuilder.Build");
            OpenEmptyScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            ChanceDemoController controller = Single<ChanceDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(DemoWiring.Describe(FindButton("Btn_Roll").onClick),
                Is.EqualTo("ChanceSystemBehaviour.GenerateVoid; Counter.Add"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Sample 1,000").onClick),
                Is.EqualTo("ChanceDemoController.RollInCode"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Draw 100,000").onClick),
                Is.EqualTo("ChanceDemoController.DrawTickets"));

            SerializedObject chance = new(SingleByTypeName("ChanceSystemBehaviour"));
            Assert.That(chance.FindProperty("manager.entries").arraySize, Is.EqualTo(4));
            Assert.That(chance.FindProperty("eventsByIndex").arraySize, Is.EqualTo(4));
            // WHY: four highlight resets, the Index SetText and ChanceDemoController.RecordRoll.
            Assert.That(CallCount(chance, "OnIdGenerated"), Is.EqualTo(6));
            Assert.That(new SerializedObject(controller).FindProperty("_exactOdds.entries").arraySize, Is.EqualTo(3));
            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Neo.Tools.Random"));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            chance.Update();
            Assert.That(CallCount(chance, "OnIdGenerated"), Is.EqualTo(6), "Hand-authored listeners must not pile up.");
        }

        [Test]
        public void ChanceBuilder_MigratesTheShippedSceneAndKeepsTheTable()
        {
            DemoSceneBuilderInfo builder = Builder("ChanceSystemExampleBuilder.Build");
            OpenSceneCopy(ChanceScene);
            GameObject chanceObject = Root("Chance");
            Assert.That(chanceObject, Is.Not.Null, "The shipped scene has a hand-authored 'Chance' object.");
            Component chanceComponent = chanceObject.GetComponent("ChanceSystemBehaviour");
            float firstWeight = new SerializedObject(chanceComponent)
                .FindProperty("manager.entries.Array.data[0].weight").floatValue;

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Canvas"), Is.Null, "The pre-kit layout is retired.");
            Assert.That(Root("UnityLifecycleEvents"), Is.Null);
            Assert.That(Root("Chance"), Is.SameAs(chanceObject), "The hand-authored module object survives.");
            SerializedObject chance = new(chanceComponent);
            Assert.That(chance.FindProperty("manager.entries.Array.data[0].weight").floatValue, Is.EqualTo(firstWeight));
            Assert.That(chance.FindProperty("manager.entries.Array.data[3].label").stringValue, Is.EqualTo("Legendary"));
            AssertNoMissingTargets(chance);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
        }

        [Test]
        public void DialogueBuilder_AuthorsAWiredSceneIdempotently()
        {
            DemoSceneBuilderInfo builder = Builder("DialogueBuilder.Build");
            OpenEmptyScene();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            DialogueDemoController controller = Single<DialogueDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True);
            Assert.That(DemoWiring.Describe(FindButton("Dialogue Box").onClick), Is.EqualTo("DialogueController.SkipOrNext"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Skip or next").onClick),
                Is.EqualTo("DialogueController.SkipOrNext"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Hit!").onClick), Is.EqualTo("DialogueLinePlayer.RequestContext"));

            SerializedObject dialogue = new(SingleByTypeName("DialogueController"));
            Assert.That(dialogue.FindProperty("dialogues").arraySize, Is.EqualTo(1));
            Object ui = dialogue.FindProperty("_dialogueUI").objectReferenceValue;
            Assert.That(ui, Is.Not.Null);
            Assert.That(((Component)ui).gameObject.name, Is.EqualTo("Dialogue Box"));
            Assert.That(CallCount(dialogue, "OnCharacterChange"), Is.EqualTo(1));
            SerializedObject barks = new(SingleByTypeName("DialogueLinePlayer"));
            Assert.That(barks.FindProperty("lines").arraySize, Is.EqualTo(10));
            Assert.That(CallCount(barks, "OnLineStarted"), Is.EqualTo(1));
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            dialogue.Update();
            Assert.That(CallCount(dialogue, "OnCharacterChange"), Is.EqualTo(1));
        }

        [Test]
        public void DialogueBuilder_MigratesTheShippedSceneAndKeepsTheConversation()
        {
            DemoSceneBuilderInfo builder = Builder("DialogueBuilder.Build");
            OpenSceneCopy(DialogueScene);
            GameObject dialogueObject = Root("DialogueComponent");
            Assert.That(dialogueObject, Is.Not.Null, "The shipped scene has a hand-authored 'DialogueComponent'.");
            Component dialogueComponent = dialogueObject.GetComponent("DialogueController");
            SerializedObject before = new(dialogueComponent);
            int monologues = before.FindProperty("dialogues.Array.data[0].monologues").arraySize;
            int audioCalls = before.FindProperty("dialogues.Array.data[0].OnChangeDialog.m_PersistentCalls.m_Calls").arraySize;

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Assert.That(Root("Canvas"), Is.Null, "The pre-kit DialogueUI canvas is retired.");
            Assert.That(Root("DialogueComponent"), Is.SameAs(dialogueObject));
            SerializedObject dialogue = new(dialogueComponent);
            Assert.That(dialogue.FindProperty("dialogues.Array.data[0].monologues").arraySize, Is.EqualTo(monologues));
            Assert.That(dialogue.FindProperty("dialogues.Array.data[0].OnChangeDialog.m_PersistentCalls.m_Calls").arraySize,
                Is.EqualTo(audioCalls), "The author's own listener (Audio Source) is kept.");
            Object ui = dialogue.FindProperty("_dialogueUI").objectReferenceValue;
            Assert.That(ui, Is.Not.Null);
            Assert.That(DemoSceneBuildContext.IsGenerated(((Component)ui).gameObject), Is.True);
            AssertNoMissingTargets(dialogue);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static int CallCount(SerializedObject owner, string eventPath)
        {
            SerializedProperty calls = owner.FindProperty(eventPath + ".m_PersistentCalls.m_Calls");
            Assert.That(calls, Is.Not.Null, eventPath);
            return calls.arraySize;
        }

        private static void AssertNoMissingTargets(SerializedObject owner)
        {
            SerializedProperty iterator = owner.GetIterator();
            bool enter = true;
            List<string> missing = new();
            while (iterator.Next(enter))
            {
                enter = iterator.propertyType == SerializedPropertyType.Generic;
                if (iterator.name != "m_Target" || iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceValue == null)
                {
                    missing.Add(iterator.propertyPath);
                }
            }

            Assert.That(missing, Is.Empty, "Persistent listeners without a target: " + string.Join(", ", missing));
        }

        private void OpenEmptyScene()
        {
            string path = NewFolder() + "/ToolsDemoTest.unity";
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
            string folderName = "__NeoToolsDemoTests_" + Guid.NewGuid().ToString("N");
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

        private Component SingleByTypeName(string typeName)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour != null && behaviour.GetType().Name == typeName)
                    {
                        found.Add(behaviour);
                    }
                }
            }

            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }
    }
}
