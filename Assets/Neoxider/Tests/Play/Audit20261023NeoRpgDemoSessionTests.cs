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
using TMPro;
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

        [UnityTest]
        public IEnumerator NetworkController_TwentyComponentRemovals_ReuseOnEnableAndReleaseShell()
        {
            GameObject owner = Host("Network controller owner");
            owner.SetActive(true);
            for (int cycle = 0; cycle < 20; cycle++)
            {
                Behaviour controller = (Behaviour)owner.AddComponent(RequireDemoType("Neo.Samples.NetworkDemoController"));
                yield return null;
                Canvas canvas = owner.GetComponentInChildren<Canvas>();
                Assert.That(canvas, Is.Not.Null);
                Sprite gradient = canvas.transform.Find("Backdrop").GetComponent<Image>().sprite;
                Texture2D texture = gradient.texture;
                controller.enabled = false;
                Assert.That(canvas.gameObject.activeSelf, Is.False);
                controller.enabled = true;
                Assert.That(owner.GetComponentsInChildren<Canvas>(true), Has.Length.EqualTo(1));
                Assert.That(owner.GetComponentInChildren<Canvas>(), Is.SameAs(canvas));
                Assert.That(canvas.transform.Find("Backdrop").GetComponent<Image>().sprite, Is.SameAs(gradient));

                Object.Destroy(controller);
                yield return null;
                yield return null;
                Assert.That(canvas == null && gradient == null && texture == null, Is.True);
                Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
                Assert.That(owner != null && _camera != null && _eventSystem != null, Is.True);
            }

            owner.AddComponent(RequireDemoType("Neo.Samples.NetworkDemoController"));
            yield return null;
            Canvas finalCanvas = owner.GetComponentInChildren<Canvas>();
            Sprite finalGradient = finalCanvas.transform.Find("Backdrop").GetComponent<Image>().sprite;
            Texture2D finalTexture = finalGradient.texture;
            Object.Destroy(owner);
            yield return null;
            yield return null;
            Assert.That(finalCanvas == null && finalGradient == null && finalTexture == null, Is.True,
                "Destroying the owner must also release its native resources during hierarchy teardown.");
            Assert.That(_camera != null && _eventSystem != null, Is.True);
        }

        [UnityTest]
        public IEnumerator AudioController_DestroyDuringCrossfade_ReleasesSixClipsAndKeepsBorrowedTrack()
        {
            if (AMSettings.HasInstance) Assert.Ignore("This audio regression requires an isolated settings service.");
            AM manager = (AM)FreshService(typeof(AM));
            manager.gameObject.SetActive(true);
            AudioListener.pause = false;
            AudioClip borrowed = Own(AudioClip.Create("Audit borrowed music", 441000, 1, 44100, false));
            HashSet<AudioClip> before = new HashSet<AudioClip>(Resources.FindObjectsOfTypeAll<AudioClip>());
            GameObject owner = Host("Audio controller");
            Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.AudioDemoController"));
            owner.SetActive(true);
            yield return null;

            AudioClip[] generated = Resources.FindObjectsOfTypeAll<AudioClip>()
                .Where(clip => !before.Contains(clip) && clip.name.StartsWith("demo_", StringComparison.Ordinal)).ToArray();
            Assert.That(generated, Has.Length.EqualTo(6));
            FindButton(owner, "Beep").onClick.Invoke();
            Assert.That(manager.GetComponentsInChildren<AudioSource>(true).Any(source =>
                source.clip != null && source.clip.name == "demo_beep"), Is.True,
                "The demo must use an independently releasable 2D voice.");
            owner.GetComponentInChildren<Toggle>().isOn = true;
            Assert.That(manager.Music.isPlaying, Is.True, "This acceptance requires enabled Unity audio.");
            AudioClip outgoingClip = manager.Music.clip;
            manager.PlayMusicByClip(borrowed, MusicTransition.Fade(10f));
            AudioSource outgoing = manager.GetComponentsInChildren<AudioSource>(true)
                .FirstOrDefault(source => source != manager.Music && source.clip == outgoingClip);
            Assert.That(outgoing, Is.Not.Null, "Arrange a real outgoing demo pad, not only the main music source.");

            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(generated.All(clip => clip == null), Is.True);
            Assert.That(manager != null && borrowed != null, Is.True);
            Assert.That(manager.Music.clip, Is.SameAs(borrowed));
            Assert.That(manager.Music.isPlaying, Is.True, "Removing the demo must not globally stop borrowed music.");
            Assert.That(outgoing == null || outgoing.clip == null, Is.True);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
        }

        [UnityTest]
        public IEnumerator LevelController_ComponentRemoval_ReleasesCurveAndOwnConsumers()
        {
            Component borrowedManager = FreshService(RequireType("Neo.Level.LevelManager"));
            borrowedManager.gameObject.SetActive(true);
            GameObject owner = Host("Level controller");
            Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.LevelDemoController"));
            owner.SetActive(true);
            yield return null;
            LevelComponent consumer = owner.GetComponentInChildren<LevelComponent>();
            Assert.That(consumer, Is.Not.Null);
            LevelCurveDefinition curve = consumer.LevelCurveDefinition;
            Assert.That(curve, Is.Not.Null);
            FindButton(owner, "+10 XP").onClick.Invoke();
            Assert.That(consumer.TotalXp, Is.EqualTo(10));

            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(consumer == null && curve == null, Is.True);
            Assert.That(borrowedManager != null && owner != null, Is.True);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
        }

        [UnityTest]
        public IEnumerator LevelController_PartialBorrowedEvents_PreservesObserversAndReleasesResources()
        {
            Component manager = FreshService(RequireType("Neo.Level.LevelManager"));
            FieldInfo levelEventField = manager.GetType().GetField("OnChangeLevel");
            FieldInfo maxEventField = manager.GetType().GetField("OnChangeMaxLevel");
            Assert.That(levelEventField, Is.Not.Null);
            Assert.That(maxEventField, Is.Not.Null);

            // Exercise each partially configured public event pair, then clear its surviving field
            // before removal. The demo must not create/replace events or remove external observers.
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
                GameObject owner = Host("Level partial events " + scenario);
                Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.LevelDemoController"));
                owner.SetActive(true);
                yield return null;

                Assert.That(levelEventField.GetValue(manager), Is.SameAs(levelEvent));
                Assert.That(maxEventField.GetValue(manager), Is.SameAs(maxEvent));
                LevelComponent consumer = owner.GetComponentInChildren<LevelComponent>();
                Assert.That(consumer, Is.Not.Null);
                LevelCurveDefinition curve = consumer.LevelCurveDefinition;
                Assert.That(curve, Is.Not.Null);
                Canvas canvas = owner.GetComponentInChildren<Canvas>();
                Assert.That(canvas, Is.Not.Null);
                Sprite gradient = canvas.transform.Find("Backdrop").GetComponent<Image>().sprite;
                Texture2D texture = gradient.texture;
                FindButton(owner, "+10 XP").onClick.Invoke();
                Assert.That(consumer.TotalXp, Is.EqualTo(10));
                borrowedEvent.Invoke(17 + scenario);
                Assert.That(externalCalls, Is.EqualTo(1));
                Assert.That(lastPayload, Is.EqualTo(17 + scenario));
                string signalName = scenario == 0 ? "OnChangeLevel" : "OnChangeMaxLevel";
                string expectedLog = $"event {signalName} → {17 + scenario}";
                Assert.That(owner.GetComponentsInChildren<TMP_Text>()
                    .Any(label => label.text.Contains(expectedLog)), Is.True,
                    "A present borrowed event must still reach the demo through its public UI log.");

                levelEventField.SetValue(manager, null);
                maxEventField.SetValue(manager, null);
                Object.Destroy(controller);
                yield return null;
                yield return null;

                Assert.That(consumer == null && curve == null, Is.True);
                Assert.That(canvas == null && gradient == null && texture == null, Is.True);
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
        public IEnumerator StateMachineController_ComponentRemoval_StopsConsumerAndReleasesDefinitions()
        {
            Type dataType = RequireType("Neo.StateMachine.NoCode.StateMachineData");
            Type stateType = RequireType("Neo.StateMachine.NoCode.StateData");
            HashSet<ScriptableObject> before = new HashSet<ScriptableObject>(Resources.FindObjectsOfTypeAll<ScriptableObject>());
            GameObject owner = Host("State machine controller");
            Component controller = owner.AddComponent(RequireDemoType("Neo.Samples.StateMachineDemoController"));
            owner.SetActive(true);
            yield return null;
            yield return null;
            ScriptableObject[] definitions = NewDefinitions(dataType, before).Concat(NewDefinitions(stateType, before)).ToArray();
            Assert.That(definitions, Has.Length.EqualTo(3));
            Component consumer = owner.GetComponentInChildren(RequireType("Neo.StateMachine.StateMachineBehaviourBase"));
            object machine = Property(consumer, "StateMachine");
            Call(consumer, "ChangeState", "Alert");
            Assert.That(Property(Property(machine, "CurrentState"), "StateName"), Is.EqualTo("Alert"));
            Sprite borrowed = SharedArt("Disc");

            Object.Destroy(controller);
            yield return null;
            yield return null;

            Assert.That(Property(machine, "CurrentState"), Is.Null);
            Assert.That(consumer == null && definitions.All(definition => definition == null), Is.True);
            Assert.That(owner.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
            Assert.That(owner.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(borrowed != null && owner != null, Is.True);
        }

        [UnityTest]
        public IEnumerator SurvivorRemoval_RetiresExactSources_PreservesSameNamePoolAndLateReturn()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Assert.Ignore("The current Survivor gameplay sample requires legacy input support.");
            yield break;
#else
            Type hubType = RequireType("Neo.Abilities.AbilitySystemBehaviour");
            Type gameType = RequireDemoType("Neo.Samples.Survivor.SurvivorGame");
            if (Object.FindObjectsByType(hubType, FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0 ||
                Object.FindObjectsByType(gameType, FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                Assert.Ignore("This integration requires an isolated Survivor game and Abilities hub.");
            PoolManager manager = (PoolManager)FreshService(typeof(PoolManager));
            manager.gameObject.SetActive(true);
            ScriptableObject config = Own(ScriptableObject.CreateInstance(RequireDemoType("Neo.Samples.Survivor.SurvivorConfig")));
            ScriptableObject playerTemplate = Own(ScriptableObject.CreateInstance(RequireType("Neo.Abilities.UnitTemplate")));
            config.GetType().GetField("PlayerTemplate").SetValue(config, playerTemplate);
            GameObject owner = Host("Survivor");
            Component game = owner.AddComponent(gameType);
            SerializedObject serialized = new SerializedObject(game);
            serialized.FindProperty("_config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(true);
            yield return null;
            GameObject source = owner.transform.Find("Templates/XpOrb").gameObject;
            GameObject otherSource = Host("External source");
            otherSource.name = source.name;
            _beforeDestroy.Add(() => SpawnUtility.RetirePool(otherSource));
            GameObject lateRental = Own(PoolManager.Get(source, Vector3.zero, Quaternion.identity));
            GameObject parked = Own(PoolManager.Get(source, Vector3.zero, Quaternion.identity));
            GameObject external = Own(PoolManager.Get(otherSource, Vector3.zero, Quaternion.identity));
            Assert.That(lateRental != null && parked != null && external != null, Is.True);
            PoolManager.Release(parked);
            PoolManager.Release(external);
            Transform sharedStorage = manager.transform.Find(source.name + "_Pool");
            Assert.That(parked.transform.parent, Is.SameAs(sharedStorage));
            Assert.That(external.transform.parent, Is.SameAs(sharedStorage));

            Object.Destroy(game);
            yield return null;
            yield return null;

            Assert.That(parked == null && lateRental != null, Is.True);
            Assert.That(external != null && sharedStorage != null && manager != null, Is.True);
            Assert.That(PoolManager.Get(source, Vector3.zero, Quaternion.identity), Is.Null,
                "A removed demo's exact source must not recreate a registration.");
            Assert.That(SpawnUtility.RetirePool(source), Is.False, "The demo must already have removed its registration.");
            GameObject externalAgain = PoolManager.Get(otherSource, Vector3.zero, Quaternion.identity);
            Assert.That(externalAgain, Is.SameAs(external));
            PoolManager.Release(externalAgain);
            PoolManager.Release(lateRental);
            yield return null;
            yield return null;
            Assert.That(lateRental == null && sharedStorage != null && external != null, Is.True);
#endif
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

        private static Button FindButton(GameObject owner, string label)
        {
            Button button = owner.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(candidate => candidate.GetComponentInChildren<TMP_Text>().text == label);
            Assert.That(button, Is.Not.Null, label);
            return button;
        }

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
