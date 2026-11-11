#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo.Audio;
using Neo.Core.Level;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Tests.Play
{
    // The release sample is optional. Reflection crosses that assembly boundary only; assertions
    // observe native assets, public APIs, UI callbacks and exact pool/config identities.
    public sealed class Audit20261023NeoRpgDemoSessionTests
    {
        private const string QuestSaveKey = "Audit20261023NeoRpgDemo.Quests";
        private readonly List<Object> _owned = new List<Object>();
        private readonly List<IDisposable> _contexts = new List<IDisposable>();
        private readonly List<Action> _beforeDestroy = new List<Action>();
        private ISaveProvider _previousProvider;
        private Random.State _randomState;
        private bool _snapshotted;
        private bool _audioAutoCreate;
        private bool _audioSettingsAutoCreate;
        private bool _poolAutoCreate;
        private bool _listenerPaused;
        private Camera _camera;
        private EventSystem _eventSystem;
        private bool _cameraOrthographic;
        private float _cameraSize;
        private Color _cameraBackground;
        private CameraClearFlags _cameraClearFlags;
        private Vector3 _cameraPosition;
        private Quaternion _cameraRotation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            RequireDemoType("Neo.Samples.NeoDemoShell");
            _previousProvider = SaveProvider.CurrentProvider;
            _randomState = Random.state;
            _audioAutoCreate = AM.CreateInstance;
            _audioSettingsAutoCreate = AMSettings.CreateInstance;
            _poolAutoCreate = PoolManager.CreateInstance;
            _listenerPaused = AudioListener.pause;
            _snapshotted = true;
            SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
            AM.CreateInstance = false;
            AMSettings.CreateInstance = false;
            PoolManager.CreateInstance = false;

            _camera = Camera.main;
            if (_camera == null)
            {
                GameObject cameraHost = Host("Borrowed camera");
                cameraHost.tag = "MainCamera";
                _camera = cameraHost.AddComponent<Camera>();
                cameraHost.SetActive(true);
            }

            _cameraOrthographic = _camera.orthographic;
            _cameraSize = _camera.orthographicSize;
            _cameraBackground = _camera.backgroundColor;
            _cameraClearFlags = _camera.clearFlags;
            _cameraPosition = _camera.transform.position;
            _cameraRotation = _camera.transform.rotation;

            _eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (_eventSystem == null)
            {
                GameObject eventHost = Host("Borrowed EventSystem");
                _eventSystem = eventHost.AddComponent<EventSystem>();
                eventHost.SetActive(true);
            }

            // Suppress the separate tutorial overlay so lifecycle tests count shell resources only.
            GameObject infoHost = Host("Info marker");
            Behaviour info = (Behaviour)infoHost.AddComponent(RequireDemoType("Neo.Samples.ModuleDemoSceneInfo"));
            info.enabled = false;
            infoHost.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (IDisposable context in _contexts) context.Dispose();
            _contexts.Clear();
            foreach (Action cleanup in _beforeDestroy) cleanup();
            _beforeDestroy.Clear();
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) Object.Destroy(_owned[i]);
            _owned.Clear();
            yield return null;
            yield return null;
            if (_camera != null)
            {
                _camera.orthographic = _cameraOrthographic;
                _camera.orthographicSize = _cameraSize;
                _camera.backgroundColor = _cameraBackground;
                _camera.clearFlags = _cameraClearFlags;
                _camera.transform.SetPositionAndRotation(_cameraPosition, _cameraRotation);
            }

            if (_snapshotted)
            {
                SaveProvider.SetProvider(_previousProvider);
                AM.CreateInstance = _audioAutoCreate;
                AMSettings.CreateInstance = _audioSettingsAutoCreate;
                PoolManager.CreateInstance = _poolAutoCreate;
                AudioListener.pause = _listenerPaused;
                Random.state = _randomState;
                _snapshotted = false;
            }
        }

        [UnityTest]
        public IEnumerator Shell_TwentyDisposals_ReturnNativeCountsAndSilenceCallbacks()
        {
            GameObject owner = Host("Shell owner");
            owner.SetActive(true);
            object warmup = BuildShell(owner.transform);
            ((IDisposable)warmup).Dispose();
            yield return null;
            yield return null;
            int spritesBefore = GradientCount<Sprite>();
            int texturesBefore = GradientCount<Texture2D>();
            Sprite sharedArt = SharedArt("RoundedRect");
            int calls = 0;

            for (int cycle = 0; cycle < 20; cycle++)
            {
                object context = BuildShell(owner.transform);
                Canvas canvas = (Canvas)Property(context, "Canvas");
                Image backdrop = canvas.transform.Find("Backdrop").GetComponent<Image>();
                Sprite gradient = backdrop.sprite;
                Texture2D texture = gradient.texture;
                Button[] buttons = (Button[])Call(context, "AddButtonRow",
                    (object)new (string, Action)[] { ("Probe", () => calls++) });
                Slider slider = (Slider)Call(context, "AddSlider", "Probe", 0f, 1f, 0f,
                    (Action<float>)(_ => calls++));
                Toggle toggle = (Toggle)Call(context, "AddToggle", "Probe", false,
                    (Action<bool>)(_ => calls++));
                Assert.That(canvas.transform.parent, Is.SameAs(owner.transform));
                Call(context, "SetVisible", false);
                buttons[0].onClick.Invoke();
                Assert.That(calls, Is.EqualTo(cycle * 3));
                Call(context, "SetVisible", true);
                buttons[0].onClick.Invoke();
                slider.onValueChanged.Invoke(0.5f);
                toggle.onValueChanged.Invoke(true);
                Assert.That(calls, Is.EqualTo((cycle + 1) * 3));

                ((IDisposable)context).Dispose();
                ((IDisposable)context).Dispose();
                Assert.That(Property(context, "IsDisposed"), Is.EqualTo(true));
                Assert.That(canvas.gameObject.activeSelf, Is.False);
                buttons[0].onClick.Invoke();
                slider.onValueChanged.Invoke(0.8f);
                toggle.onValueChanged.Invoke(false);
                Assert.That(calls, Is.EqualTo((cycle + 1) * 3), "Disposal must detach every row callback.");
                yield return null;
                yield return null;
                Assert.That(canvas == null && gradient == null && texture == null, Is.True);
                Assert.That(GradientCount<Sprite>(), Is.EqualTo(spritesBefore));
                Assert.That(GradientCount<Texture2D>(), Is.EqualTo(texturesBefore));
            }

            Assert.That(owner != null && _camera != null && _eventSystem != null && sharedArt != null, Is.True);
        }

        [UnityTest]
        public IEnumerator Shell_DisposingOneFrame_PreservesOtherFrameAndBorrowedArt()
        {
            GameObject owner = Host("Two shells");
            owner.SetActive(true);
            object first = BuildShell(null); // Existing two-argument API keeps its caller-owned root.
            object second = BuildShell(owner.transform);
            Canvas firstCanvas = (Canvas)Property(first, "Canvas");
            Canvas secondCanvas = (Canvas)Property(second, "Canvas");
            Assert.That(firstCanvas.transform.parent, Is.Null);
            Image firstBackdrop = firstCanvas.transform.Find("Backdrop").GetComponent<Image>();
            Sprite generated = firstBackdrop.sprite;
            Texture2D generatedTexture = generated.texture;
            Sprite borrowed = SharedArt("Disc");
            firstBackdrop.sprite = borrowed;
            Sprite secondGradient = secondCanvas.transform.Find("Backdrop").GetComponent<Image>().sprite;

            ((IDisposable)first).Dispose();
            yield return null;
            yield return null;

            Assert.That(generated == null && generatedTexture == null && firstCanvas == null, Is.True);
            Assert.That(borrowed != null && secondCanvas != null && secondGradient != null, Is.True);
            int calls = 0;
            Button[] buttons = (Button[])Call(second, "AddButtonRow",
                (object)new (string, Action)[] { ("Still live", () => calls++) });
            buttons[0].onClick.Invoke();
            Assert.That(calls, Is.EqualTo(1));
        }

        // WHY: NetworkDemo is saved with its UI; the controller only reads serialized scene references,
        // builds no UI at runtime and must stay quiet and exception-free in an unbuilt scene (no manager, relay or labels).
        [UnityTest]
        public IEnumerator NetworkController_ComponentRemovals_BuildNoUiAndTolerateMissingReferences()
        {
            GameObject owner = Host("Network controller owner");
            owner.SetActive(true);
            for (int cycle = 0; cycle < 20; cycle++)
            {
                Behaviour controller = (Behaviour)owner.AddComponent(RequireDemoType("Neo.Samples.NetworkDemoController"));
                yield return null;
                Call(controller, "OnPingReceived");
                Call(controller, "OnChatReceived", "hello");
                Call(controller, "RequestBossHit", 10f);
                Call(controller, "SendChatFromField");
                Assert.That(Property(controller, "PingsReceived"), Is.EqualTo(1));
                Assert.That(Property(controller, "LastChatMessage"), Is.EqualTo("hello"));
                Assert.That(Property(controller, "IsSceneBuilt"), Is.EqualTo(false));
                controller.enabled = false;
                controller.enabled = true;
                yield return null;

                Object.Destroy(controller);
                yield return null;
                Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty, "The authored demo builds no UI at runtime.");
                Assert.That(owner != null && _camera != null && _eventSystem != null, Is.True);
            }
        }

        // WHY: LevelFlowDemo is saved with its UI; the controller only drives the scene's
        // LevelComponent / LevelManager through serialized references and never builds UI or owns their assets.
        [UnityTest]
        public IEnumerator LevelController_ComponentRemoval_KeepsAuthoredLevelAndBuildsNoUi()
        {
            Component borrowedManager = FreshService(RequireType("Neo.Level.LevelManager"));
            borrowedManager.gameObject.SetActive(true);
            GameObject owner = Host("Level controller");
            LevelCurveDefinition curve = Own(ScriptableObject.CreateInstance<LevelCurveDefinition>());
            curve.SetLinear(100);
            LevelComponent consumer = owner.AddComponent<LevelComponent>();
            SerializedObject level = new SerializedObject(consumer);
            level.FindProperty("_levelCurve").objectReferenceValue = curve;
            level.FindProperty("_loadOnAwake").boolValue = false;
            level.FindProperty("_autoSave").boolValue = false;
            level.ApplyModifiedPropertiesWithoutUndo();
            Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.LevelDemoController"));
            SerializedObject references = new SerializedObject(controller);
            references.FindProperty("_level").objectReferenceValue = consumer;
            references.FindProperty("_levelManager").objectReferenceValue = borrowedManager;
            references.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(true);
            yield return null;

            Call(controller, "AddXp", 10);
            Assert.That(consumer.TotalXp, Is.EqualTo(10));
            Call(controller, "LevelUp");
            Assert.That(consumer.Level, Is.EqualTo(2), "Level up adds exactly the XP missing to the next level.");

            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(consumer != null && curve != null, Is.True, "Authored components and assets are not the controller's.");
            Assert.That(consumer.TotalXp, Is.EqualTo(100));
            Assert.That(borrowedManager != null && owner != null, Is.True);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty, "The authored demo builds no UI at runtime.");
        }

        [UnityTest]
        public IEnumerator LevelController_BorrowedManagerEvents_KeepExternalObserversAndState()
        {
            Component manager = FreshService(RequireType("Neo.Level.LevelManager"));
            FieldInfo levelEventField = manager.GetType().GetField("OnChangeLevel");
            FieldInfo maxEventField = manager.GetType().GetField("OnChangeMaxLevel");
            Assert.That(levelEventField, Is.Not.Null);
            Assert.That(maxEventField, Is.Not.Null);

            // WHY: each partially configured public event pair is exercised; the controller's listener targets must
            // work without UI references and must never replace the events or remove external observers.
            for (int scenario = 0; scenario < 2; scenario++)
            {
                UnityEvent<int> borrowedEvent = new UnityEvent<int>();
                UnityEvent<int> levelEvent = scenario == 0 ? borrowedEvent : null;
                UnityEvent<int> maxEvent = scenario == 1 ? borrowedEvent : null;
                int externalCalls = 0;
                int lastPayload = -1;
                borrowedEvent.AddListener(value =>
                {
                    externalCalls++;
                    lastPayload = value;
                });
                levelEventField.SetValue(manager, levelEvent);
                maxEventField.SetValue(manager, maxEvent);
                manager.gameObject.SetActive(true);
                object map = Property(manager, "Map");
                object currentLevel = Property(manager, "CurrentLevel");
                object maxLevel = Property(manager, "MaxLevel");
                GameObject owner = Host("Level borrowed events " + scenario);
                Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.LevelDemoController"));
                SerializedObject references = new SerializedObject(controller);
                references.FindProperty("_levelManager").objectReferenceValue = manager;
                references.ApplyModifiedPropertiesWithoutUndo();
                owner.SetActive(true);
                yield return null;

                Call(controller, "OnMapLevelChanged", 3);
                Call(controller, "OnMapProgressChanged", 2);
                Call(controller, "OnMapChanged", 0);
                Assert.That(levelEventField.GetValue(manager), Is.SameAs(levelEvent));
                Assert.That(maxEventField.GetValue(manager), Is.SameAs(maxEvent));
                borrowedEvent.Invoke(17 + scenario);
                Assert.That(externalCalls, Is.EqualTo(1));
                Assert.That(lastPayload, Is.EqualTo(17 + scenario));

                levelEventField.SetValue(manager, null);
                maxEventField.SetValue(manager, null);
                Object.Destroy(controller);
                yield return null;
                yield return null;

                Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
                Assert.That(manager != null && owner != null, Is.True);
                Assert.That(levelEventField.GetValue(manager), Is.Null);
                Assert.That(maxEventField.GetValue(manager), Is.Null);
                Assert.That(Property(manager, "Map"), Is.SameAs(map));
                Assert.That(Property(manager, "CurrentLevel"), Is.EqualTo(currentLevel));
                Assert.That(Property(manager, "MaxLevel"), Is.EqualTo(maxLevel));
                borrowedEvent.Invoke(29 + scenario);
                Assert.That(externalCalls, Is.EqualTo(2));
                Assert.That(lastPayload, Is.EqualTo(29 + scenario));
            }
        }

        [UnityTest]
        public IEnumerator QuestController_Destroy_PreservesReplacementAndSavedProgress()
        {
            Component manager = FreshQuestManager();
            Type configType = RequireType("Neo.Quest.QuestConfig");
            HashSet<ScriptableObject> before = new HashSet<ScriptableObject>(Resources.FindObjectsOfTypeAll<ScriptableObject>());
            GameObject owner = Host("Quest controller");
            Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.QuestDemoController"));
            owner.SetActive(true);
            yield return null;
            ScriptableObject[] generated = NewDefinitions(configType, before);
            Assert.That(generated, Has.Length.EqualTo(3));
            ScriptableObject wolf = generated.Single(config => (string)Property(config, "Id") == "demo_wolf_hunt");
            ScriptableObject herbs = generated.Single(config => (string)Property(config, "Id") == "demo_herbal_aid");
            ScriptableObject ruins = generated.Single(config => (string)Property(config, "Id") == "demo_scout_ruins");
            Assert.That(Call(manager, "AcceptQuest", wolf), Is.EqualTo(true));
            Call(manager, "CompleteObjective", wolf, 0);
            Assert.That(Call(manager, "AcceptQuest", herbs), Is.EqualTo(true));
            Call(manager, "RegisterQuest", ruins);
            Call(manager, "Save");
            string saved = SaveProvider.GetString(QuestSaveKey);
            Assert.That(saved, Is.Not.Empty);

            Assert.That(Call(manager, "UnregisterQuest", wolf), Is.EqualTo(true));
            ScriptableObject replacement = Own(ScriptableObject.CreateInstance(configType));
            SetString(replacement, "_id", "demo_wolf_hunt");
            Call(manager, "RegisterQuest", replacement);
            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(generated.All(config => config == null), Is.True);
            Assert.That(Call(manager, "GetConfig", "demo_wolf_hunt"), Is.SameAs(replacement));
            Assert.That(Call(manager, "GetConfig", "demo_herbal_aid"), Is.Null);
            Assert.That(Call(manager, "GetConfig", "demo_scout_ruins"), Is.Null);
            Assert.That(SaveProvider.GetString(QuestSaveKey), Is.EqualTo(saved));
            Call(manager, "Load");
            Assert.That(Call(manager, "GetObjectiveProgress", replacement, 0), Is.EqualTo(1));
            Assert.That(Call(manager, "GetState", "demo_herbal_aid"), Is.Not.Null);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
        }

        [UnityTest]
        public IEnumerator QuestView_Destroy_ReleasesOnlyGeneratedFallbackDefinitions()
        {
            Component manager = FreshQuestManager();
            Type configType = RequireType("Neo.Quest.QuestConfig");
            ScriptableObject borrowed = Own(ScriptableObject.CreateInstance(configType));
            SetString(borrowed, "_id", "audit_borrowed_quest");
            Call(manager, "RegisterQuest", borrowed);
            HashSet<ScriptableObject> before = new HashSet<ScriptableObject>(Resources.FindObjectsOfTypeAll<ScriptableObject>());
            GameObject owner = Host("Legacy quest view");
            Component view = owner.AddComponent(RequireDemoType("Neo.Demo.Quest.QuestDemoView"));
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_questManager").objectReferenceValue = manager;
            serialized.FindProperty("_loadQuestsFromResourcesIfEmpty").boolValue = false;
            serialized.FindProperty("_autoBindUiByName").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(true);
            yield return null;
            ScriptableObject[] generated = NewDefinitions(configType, before);
            Assert.That(generated, Has.Length.EqualTo(2));
            string[] ids = generated.Select(config => (string)Property(config, "Id")).ToArray();
            foreach (ScriptableObject config in generated) Call(manager, "RegisterQuest", config);

            Object.Destroy(view);
            yield return null;
            yield return null;

            Assert.That(generated.All(config => config == null), Is.True);
            foreach (string id in ids) Assert.That(Call(manager, "GetConfig", id), Is.Null);
            Assert.That(borrowed != null && manager != null && owner != null, Is.True);
            Assert.That(Call(manager, "GetConfig", "audit_borrowed_quest"), Is.SameAs(borrowed));
        }

        [UnityTest]
        public IEnumerator StateMachineController_DrivesAuthoredObjectsAndStopsItsMachineOnRemoval()
        {
            // WHY: StateMachineDemo.unity is saved with its UI; the controller builds nothing at
            // runtime (no UI, no StateData) and only drives the scene objects it references.
            Type dataType = RequireType("Neo.StateMachine.NoCode.StateMachineData");
            Type stateType = RequireType("Neo.StateMachine.NoCode.StateData");
            HashSet<ScriptableObject> before = new HashSet<ScriptableObject>(Resources.FindObjectsOfTypeAll<ScriptableObject>());
            GameObject owner = Host("State machine controller");
            SpriteRenderer agent = Own(new GameObject("Audit20261023NeoRpgDemo Agent")).AddComponent<SpriteRenderer>();
            SpriteRenderer prey = Own(new GameObject("Audit20261023NeoRpgDemo Prey")).AddComponent<SpriteRenderer>();
            Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.StateMachineDemoController"));
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("_agent").objectReferenceValue = agent.transform;
            serialized.FindProperty("_agentRenderer").objectReferenceValue = agent;
            serialized.FindProperty("_prey").objectReferenceValue = prey.transform;
            serialized.FindProperty("_preyRenderer").objectReferenceValue = prey;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(NewDefinitions(dataType, before).Concat(NewDefinitions(stateType, before)), Is.Empty);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(owner.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
            Assert.That(Property(controller, "AgentStateName"), Is.EqualTo("Idle"));
            Assert.That(prey.enabled, Is.False);
            Call(controller, "Provoke");
            Assert.That(Property(controller, "AgentStateName"), Is.EqualTo("Chase"));
            Assert.That(prey.enabled, Is.True);
            object machine = Property(controller, "AgentMachine");

            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(Property(machine, "CurrentState"), Is.Null);
            Assert.That(agent != null && prey != null && owner != null, Is.True);
        }

        private object BuildShell(Transform owner)
        {
            MethodInfo build = RequireDemoType("Neo.Samples.NeoDemoShell").GetMethod("Build",
                owner == null ? new[] { typeof(string), typeof(Color) } :
                    new[] { typeof(string), typeof(Color), typeof(Transform) });
            Assert.That(build, Is.Not.Null);
            object[] arguments = owner == null ? new object[] { "Lifecycle audit", Color.cyan } :
                new object[] { "Lifecycle audit", Color.cyan, owner };
            object context = build.Invoke(null, arguments);
            _contexts.Add((IDisposable)context);
            return context;
        }

        private Component FreshQuestManager()
        {
            Component manager = FreshService(RequireType("Neo.Quest.QuestManager"));
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("_autoLoad").boolValue = false;
            serialized.FindProperty("_autoSave").boolValue = false;
            serialized.FindProperty("_saveKey").stringValue = QuestSaveKey;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            manager.gameObject.SetActive(true);
            return manager;
        }

        private Component FreshService(Type type)
        {
            if (Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                Assert.Ignore("This regression requires an isolated " + type.FullName + ".");
            return Host(type.Name).AddComponent(type);
        }

        private GameObject Host(string name)
        {
            GameObject host = Own(new GameObject("Audit20261023NeoRpgDemo " + name));
            host.SetActive(false);
            return host;
        }

        private T Own<T>(T value) where T : Object
        {
            _owned.Add(value);
            return value;
        }

        private static int GradientCount<T>() where T : Object =>
            Resources.FindObjectsOfTypeAll<T>().Count(value => value.name == "NeoDemoShell Gradient");

        private static Sprite SharedArt(string name) =>
            (Sprite)RequireDemoType("Neo.Samples.Survivor.SurvivorArt").GetProperty(name).GetValue(null);

        private static ScriptableObject[] NewDefinitions(Type type, HashSet<ScriptableObject> before) =>
            Resources.FindObjectsOfTypeAll<ScriptableObject>()
                .Where(value => value.GetType() == type && !before.Contains(value)).ToArray();

        private static void SetString(Object target, string field, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static object Property(object target, string name)
        {
            Assert.That(target, Is.Not.Null, name);
            PropertyInfo property = target.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return property.GetValue(target);
        }

        private static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(candidate => candidate.Name == name && !candidate.ContainsGenericParameters &&
                    candidate.GetParameters().Length == arguments.Length &&
                    candidate.GetParameters().Select((parameter, index) =>
                        arguments[index] == null || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(matches => matches));
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(target, arguments);
        }

        private static Type RequireDemoType(string name)
        {
            Assembly demo = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Neo.Rpg.Demo");
            if (demo == null)
                Assert.Ignore("Import Demo Scenes, or use the developer Samples root, to compile Neo.Rpg.Demo.");
            Type type = demo.GetType(name);
            Assert.That(type, Is.Not.Null, name);
            return type;
        }

        private static Type RequireType(string name)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, "Required sample dependency: " + name);
            return type;
        }
    }
}
#endif
