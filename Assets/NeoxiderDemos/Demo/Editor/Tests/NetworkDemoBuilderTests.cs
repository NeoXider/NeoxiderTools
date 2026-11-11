using System;
using System.Collections.Generic;
using System.IO;
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
    ///     End-to-end check of NetworkDemoBuilder on a copy of NetworkDemo.unity: the hand-authored NeoNetworkManager is
    ///     reused and configured for a self-contained scene, the relay and the shared boss are saved with non-zero
    ///     Mirror scene ids, buttons, channels and manager events carry their persistent listeners, and a second build
    ///     changes nothing. Network, RPG and Mirror types are reached by name, so this assembly needs no extra references.
    /// </summary>
    public sealed class NetworkDemoBuilderTests
    {
        private const string NetworkScene = "Scenes/Network/NetworkDemo.unity";
        private const long DiscoveryHandshake = 0x4E656F4E65744C;

        private static readonly string[] ChannelNames = { "Chat", "Ping", "Wave", "Hit boss", "Revive boss" };

        // WHY: NetworkActionScope values (AllClients 0, ServerOnly 1, OthersOnly 2) are a serialized contract.
        private static readonly int[] ChannelScopes = { 0, 0, 2, 1, 1 };

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
        public void NetworkBuilder_WiresSessionRelayAndSharedBoss()
        {
            if (Type.GetType("Mirror.NetworkIdentity, Mirror") == null)
            {
                Assert.Ignore("The network demo needs Mirror.");
            }

            DemoSceneBuilderInfo builder = Builder("NetworkDemoBuilder.Build");
            OpenSceneCopy(NetworkScene);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            Component controller = Single("NetworkDemoController");
            Assert.That(Property<bool>(controller, "IsSceneBuilt"), Is.True);
            Assert.That(Single<ModuleDemoSceneInfo>().HasAuthoredOverlay, Is.True);

            Component manager = Single("NeoNetworkManager");
            Assert.That(DemoSceneBuildContext.IsGenerated(manager.gameObject), Is.False,
                "The hand-authored 'Network Demo' NeoNetworkManager must be reused.");
            SerializedObject managerData = new(manager);
            Assert.That(managerData.FindProperty("autoCreatePlayer").boolValue, Is.False,
                "Without a player prefab Auto Create Player makes Mirror log an error on every Host.");
            Assert.That(managerData.FindProperty("dontDestroyOnLoad").boolValue, Is.False);
            Assert.That(managerData.FindProperty("transport").objectReferenceValue, Is.Not.Null);
            Assert.That(DemoWiring.Describe(EventProperty(manager, "OnServerStartedEvent")), Is.EqualTo("DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(EventProperty(manager, "OnServerClientConnectedEvent")),
                Is.EqualTo("NetworkDemoController.OnServerClientConnected"));

            Component relay = Single("NetworkActionRelay");
            SerializedObject relayData = new(relay);
            Assert.That(relayData.FindProperty("isNetworked").boolValue, Is.True);
            SerializedProperty channels = relayData.FindProperty("_channels");
            Assert.That(channels.arraySize, Is.EqualTo(ChannelNames.Length));
            for (int i = 0; i < ChannelNames.Length; i++)
            {
                SerializedProperty channel = channels.GetArrayElementAtIndex(i);
                Assert.That(channel.FindPropertyRelative("channelName").stringValue, Is.EqualTo(ChannelNames[i]));
                Assert.That(channel.FindPropertyRelative("scope").intValue, Is.EqualTo(ChannelScopes[i]), ChannelNames[i]);
            }

            Assert.That(DemoWiring.Describe(ChannelEvent(relay, 0, "onTriggeredString")),
                Is.EqualTo("NetworkDemoController.OnChatReceived"));
            Assert.That(DemoWiring.Describe(ChannelEvent(relay, 3, "onTriggeredFloat")),
                Is.EqualTo("RpgCharacterNetworkAdapter.NetDamage; NetworkDemoController.OnBossHitOnServer"),
                "Hits are applied by the adapter on the server only (ServerOnly channel).");
            Assert.That(DemoWiring.Describe(ChannelEvent(relay, 4, "onTriggered")),
                Is.EqualTo("RpgCharacter.Restore; NetworkDemoController.OnBossRevivedOnServer"));

            List<Component> identities = All("NetworkIdentity");
            Assert.That(identities.Count, Is.EqualTo(2), "Shared Relay and Shared Boss are the scene network objects.");
            foreach (Component identity in identities)
            {
                ulong sceneId = (ulong)identity.GetType().GetField("sceneId").GetValue(identity);
                Assert.That(sceneId, Is.Not.EqualTo(0UL), identity.name + " needs a saved Mirror scene id.");
            }

            SerializedObject bossData = new(Single("RpgCharacter"));
            Assert.That(bossData.FindProperty("isNetworked").boolValue, Is.True);
            Object template = bossData.FindProperty("_template").objectReferenceValue;
            Assert.That(template != null && EditorUtility.IsPersistent(template), Is.True,
                "The boss template must be a saved asset.");
            Single("RpgCharacterNetworkAdapter");

            Assert.That(new SerializedObject(Single("NetworkDiscovery")).FindProperty("secretHandshake").longValue,
                Is.EqualTo(DiscoveryHandshake), "Editor and build must share the LAN handshake.");
            Assert.That(new SerializedObject(Single("NeoNetworkDiscovery")).FindProperty("_autoDiscoverOnClient").boolValue,
                Is.False, "Opening the demo must not broadcast on the LAN.");
            Assert.That(new SerializedObject(Single("NeoNetworkTelemetry")).FindProperty("_showOverlay").boolValue,
                Is.False, "Demos never show IMGUI.");

            Assert.That(DemoWiring.Describe(FindButton("Btn_Host").onClick),
                Is.EqualTo("DemoLogFeed.Log; NeoNetworkManager.StartAsHost"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Join").onClick),
                Is.EqualTo("NetworkDemoController.JoinAddressFromField"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Stop").onClick),
                Is.EqualTo("DemoLogFeed.Log; NeoNetworkManager.StopNetwork"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Ping all").onClick),
                Is.EqualTo("DemoLogFeed.Log; NetworkActionRelay.TriggerByName"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Hit 10").onClick),
                Is.EqualTo("NetworkDemoController.RequestBossHit"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Quick Play").onClick),
                Is.EqualTo("DemoLogFeed.Log; NeoNetworkDiscovery.QuickPlay"));

            AssertIdempotent(builder, first);
            Assert.That(DemoWiring.Count(EventProperty(manager, "OnServerStartedEvent")), Is.EqualTo(1),
                "Listeners on the hand-authored manager must not pile up across rebuilds.");
        }

        private void AssertIdempotent(DemoSceneBuilderInfo builder, DemoSceneResult first)
        {
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        private static DemoSceneBuilderInfo Builder(string idSuffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(idSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, idSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private void OpenSceneCopy(string relativeScene)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
            {
                Assert.Ignore("Demo scene not found: " + source);
            }

            string folderName = "__NeoNetworkDemoTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + Path.GetFileName(relativeScene);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Scene copy did not open: " + path);
            SceneManager.SetActiveScene(_scene);
        }

        private static T Property<T>(object owner, string name)
        {
            PropertyInfo info = owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, owner.GetType().Name + "." + name);
            return (T)info.GetValue(owner);
        }

        private static UnityEventBase EventProperty(Component owner, string name)
        {
            return Property<UnityEventBase>(owner, name);
        }

        private static UnityEventBase ChannelEvent(Component relay, int index, string field)
        {
            MethodInfo getChannel = relay.GetType().GetMethod("GetChannel", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(getChannel, Is.Not.Null, "NetworkActionRelay.GetChannel");
            object channel = getChannel.Invoke(relay, new object[] { index });
            Assert.That(channel, Is.Not.Null, "Relay channel " + index);
            FieldInfo info = channel.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, "NetworkActionChannel." + field);
            return (UnityEventBase)info.GetValue(channel);
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

        private Component Single(string typeName)
        {
            List<Component> found = All(typeName);
            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }

        private List<Component> All(string typeName)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && component.GetType().Name == typeName)
                    {
                        found.Add(component);
                    }
                }
            }

            return found;
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
