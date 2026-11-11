using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     CardGameBuilder against a temporary copy of the hand-authored CardGame scene: it adds the guide (info card,
    ///     live panel, persistent listeners) without dropping the game's own wiring, and a second pass changes nothing.
    /// </summary>
    public sealed class CardGameBuilderTests
    {
        private const string BuilderSuffix = "CardGameBuilder.Build";
        private const string GuideRootName = "[Demo] Guide";
        private const string LivePanelName = "LivePanel";

        private static readonly string[] LoggedGameEvents =
        {
            "OnGameRestarted", "OnGameStarted", "OnPlayerWonRound", "OnOpponentWonRound", "OnWarStarted", "OnWarEnded",
            "OnPlayerWin", "OnOpponentWin"
        };

        private string _folder;
        private Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (!string.IsNullOrEmpty(_folder) && AssetDatabase.IsValidFolder(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
            }

            _scene = default;
            _folder = null;
        }

        [Test]
        public void CardGameBuilder_DecoratesTheHandAuthoredGameIdempotently()
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(BuilderSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, "CardGameBuilder must be registered.");
            Assert.That(builder.Error, Is.Null);
            string source = builder.ResolveAssetPath();
            Assume.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Scene not found: " + source);
            Type gameType = FindType("Neo.Cards.DrunkardGame");
            Type selectorType = FindType("Neo.Tools.Selector");
            Assume.That(gameType != null && selectorType != null, Is.True, "Neo.Cards / Neo.Tools.View are not loaded.");
            OpenCopy(source);

            Component game = Single(gameType);
            Dictionary<string, List<string>> handAuthored = new();
            foreach (string eventName in LoggedGameEvents)
            {
                handAuthored[eventName] = HandAuthoredCalls(GameEvent(game, eventName));
            }

            Component tutorial = FindNamed(selectorType, "Tutorial");
            UnityEventBase finished = tutorial != null
                ? (UnityEventBase)selectorType.GetField("OnFinished").GetValue(tutorial)
                : null;
            List<string> handAuthoredFinished = finished != null ? HandAuthoredCalls(finished) : new List<string>();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());

            foreach (string eventName in LoggedGameEvents)
            {
                UnityEventBase gameEvent = GameEvent(game, eventName);
                Assert.That(HandAuthoredCalls(gameEvent), Is.EqualTo(handAuthored[eventName]),
                    eventName + ": hand-authored listeners must be kept.");
                Assert.That(DemoWiring.Describe(gameEvent), Does.Contain("DemoLogFeed.Log"), eventName);
            }

            Button restart = FindComponent<Button>(button => button.name == "Btn_Restart deal");
            Assert.That(restart, Is.Not.Null, "Missing Btn_Restart deal.");
            Assert.That(DemoWiring.Describe(restart.onClick), Is.EqualTo("DrunkardGame.RestartGame"));
            Toggle sameDeal = FindComponent<Toggle>(toggle =>
                DemoWiring.Describe(toggle.onValueChanged).Contains("DeckComponent.set_UseFixedShuffleSeed"));
            Assert.That(sameDeal, Is.Not.Null, "The seed toggle must drive DeckComponent.UseFixedShuffleSeed.");

            ModuleDemoSceneInfo info = FindComponent<ModuleDemoSceneInfo>(candidate => true);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Neo.Cards"));

            GameObject livePanel = FindLivePanel();
            Assert.That(livePanel, Is.Not.Null);
            if (finished != null)
            {
                Assert.That(HandAuthoredCalls(finished), Is.EqualTo(handAuthoredFinished));
                Assert.That(DemoWiring.Count(finished), Is.EqualTo(handAuthoredFinished.Count + 1),
                    "The tutorial end shows the live panel through one extra listener.");
                Assert.That(livePanel.activeSelf, Is.False, "The live panel waits for the dealer tutorial to end.");
            }

            Dictionary<string, int> counts = CountGameListeners(game);
            int finishedCount = DemoWiring.Count(finished);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(CountGameListeners(game), Is.EqualTo(counts), "Listeners on the game must not pile up.");
            Assert.That(DemoWiring.Count(finished), Is.EqualTo(finishedCount));
            Assert.That(DemoWiring.Count(restart.onClick), Is.EqualTo(1));
        }

        private static UnityEventBase GameEvent(Component game, string eventName)
        {
            PropertyInfo property = game.GetType().GetProperty(eventName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, "DrunkardGame." + eventName + " is missing.");
            return (UnityEventBase)property.GetValue(game);
        }

        private static List<string> HandAuthoredCalls(UnityEventBase unityEvent)
        {
            List<string> calls = new();
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                Object target = unityEvent.GetPersistentTarget(i);
                GameObject host = target is Component component ? component.gameObject : target as GameObject;
                if (target != null && !DemoSceneBuildContext.IsGenerated(host))
                {
                    calls.Add(target.GetType().Name + "." + unityEvent.GetPersistentMethodName(i));
                }
            }

            return calls;
        }

        private static Dictionary<string, int> CountGameListeners(Component game)
        {
            Dictionary<string, int> counts = new();
            foreach (string eventName in LoggedGameEvents)
            {
                counts[eventName] = DemoWiring.Count(GameEvent(game, eventName));
            }

            return counts;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private void OpenCopy(string source)
        {
            string folderName = "__NeoCardGameBuilderTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/CardGameCopy.unity";
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Scene copy did not open: " + path);
        }

        private Component Single(Type type)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren(type, true))
                {
                    if (!DemoSceneBuildContext.IsGenerated(component.gameObject))
                    {
                        found.Add(component);
                    }
                }
            }

            Assert.That(found.Count, Is.EqualTo(1), type.Name);
            return found[0];
        }

        private Component FindNamed(Type type, string objectName)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren(type, true))
                {
                    if (component.name == objectName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private T FindComponent<T>(Predicate<T> match) where T : Component
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (match(component))
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private GameObject FindLivePanel()
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == GuideRootName)
                {
                    Transform panel = root.transform.Find(LivePanelName);
                    return panel != null ? panel.gameObject : null;
                }
            }

            return null;
        }
    }
}
