using System;
using Neo.Network;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Network;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if MIRROR
using Mirror;
using Mirror.Discovery;
#endif

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Network demo into NetworkDemo.unity. The hand-authored 'Network Demo' object keeps its
    ///     NeoNetworkManager + TelepathyTransport (configured for a self-contained scene: no player prefab, no
    ///     DontDestroyOnLoad) and its lifecycle and per-connection events are wired to the log feed. Generated:
    ///     '[Demo] Network Objects/Shared Relay' (NetworkIdentity + <see cref="NetworkActionRelay" /> with five channels
    ///     whose listeners are saved), '[Demo] Network Objects/Shared Boss' (RpgCharacter + NetworkIdentity +
    ///     <see cref="RpgCharacterNetworkAdapter" />, server-authoritative), '[Demo] Services/LAN Discovery' and
    ///     '[Demo] Services/Network Telemetry'. Left column: session (Host / Server / Stop call the manager directly,
    ///     Join is the C# StartAsClient(address) path) and LAN discovery. Right column: relay events (NoCode buttons,
    ///     a NoCode chat field and a C# Send button) and the replicated boss (hits travel through a ServerOnly channel).
    /// </summary>
    internal static class NetworkDemoBuilder
    {
        /// <summary>Template asset of the shared boss, relative to the Demo sample root.</summary>
        public const string BossTemplateAsset = "Data/Network/NetworkDemoBoss.asset";

        public const string NetworkObjectsRootKey = "Network Objects";
        public const string RelayObjectName = "Shared Relay";
        public const string BossObjectName = "Shared Boss";
        public const float BossHp = 200f;

        /// <summary>
        ///     LAN discovery handshake of the demo. Fixed so the editor and a build of this scene answer each other
        ///     (Mirror would otherwise roll a random value per scene save).
        /// </summary>
        public const long DiscoveryHandshake = 0x4E656F4E65744C;

        private const string Scene = "NetworkDemo.unity";

        private static readonly Color Accent = new(0.30f, 0.78f, 0.95f);

        private static readonly string[] ChannelNames =
        {
            NetworkDemoController.ChatChannel,
            NetworkDemoController.PingChannel,
            NetworkDemoController.WaveChannel,
            NetworkDemoController.HitBossChannel,
            NetworkDemoController.ReviveBossChannel
        };

        private static readonly NetworkActionScope[] ChannelScopes =
        {
            NetworkActionScope.AllClients,
            NetworkActionScope.AllClients,
            NetworkActionScope.OthersOnly,
            NetworkActionScope.ServerOnly,
            NetworkActionScope.ServerOnly
        };

        [DemoSceneBuilder("Scenes/Network/NetworkDemo.unity")]
        public static void Build(DemoSceneBuildContext ctx)
        {
            string step = "services";
            try
            {
                // WHY: the hand-authored 'Network Demo' (manager, transport, info) and 'Demo Controller' win.
                NeoNetworkManager manager = ctx.FindOrCreate<NeoNetworkManager>("Services", "Network Manager");
                ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
                NetworkDemoController controller = ctx.FindOrCreate<NetworkDemoController>("Services", "Demo Controller");

                step = "network manager";
                ConfigureManager(ctx, manager);

                step = "telemetry";
                NeoNetworkTelemetry telemetry = ctx.FindOrCreate<NeoNetworkTelemetry>("Services", "Network Telemetry");
                ConfigureTelemetry(ctx, telemetry);

#if MIRROR
                step = "LAN discovery";
                NeoNetworkDiscovery discovery = ctx.FindOrCreate<NeoNetworkDiscovery>("Services", "LAN Discovery");
                ConfigureDiscovery(ctx, discovery, manager);
#endif

                step = "boss template asset";
                RpgCharacterTemplate template =
                    DemoSceneAssets.EnsureAsset<RpgCharacterTemplate>(BossTemplateAsset, ConfigureBossTemplate);

                step = "shared relay";
                Transform objects = ctx.Root(NetworkObjectsRootKey).transform;
                NetworkActionRelay relay = SharedRelay(ctx, objects);

                step = "shared boss";
                RpgCharacter boss = SharedBoss(ctx, objects, template, out RpgCharacterNetworkAdapter adapter);

                step = "frame";
                // WHY: 1020 px keeps the centered card clear of the 400 px info card in the top-left corner.
                DemoFrame frame = DemoSceneKit.Frame(ctx,
                    new DemoFrameOptions("Neo.Network · Mirror", Accent) { ContentWidth = 1020f });
                frame.Content.Note("Offline the relay fires locally · Host here, Join from a build or a Multiplayer Play Mode clone");
                DemoPanel[] columns = frame.Content.Columns("Session | Shared objects", 2, 12f);

                step = "session rows";
                SessionRows session = BuildSessionRows(columns[0], frame, manager, controller);

#if MIRROR
                step = "LAN rows";
                LanRows lan = BuildLanRows(columns[0], frame, discovery, controller);
#endif

                step = "relay rows";
                RelayRows relayRows = BuildRelayRows(columns[1], frame, relay, controller);

                step = "boss rows";
                BossRows bossRows = BuildBossRows(columns[1], frame, relay, controller);

                step = "relay channel listeners";
                WireRelayChannels(ctx, relay, controller, boss, adapter);

                step = "network events";
                WireManagerEvents(ctx, manager, frame.Log, controller);
                DemoWiring.AddString(boss.OnDeathEvent, frame.Log.Log, "RpgCharacter.OnDeath · Shared Boss defeated");
                DemoWiring.AddString(boss.OnRevivedEvent, frame.Log.Log, "RpgCharacter.OnRevived · Shared Boss is back");
#if MIRROR
                WireDiscoveryEvents(discovery, frame.Log, controller);
#endif

                step = "info card";
                frame.InfoCard(info, InfoContent());

                step = "controller references";
                ctx.Set(controller, "_manager", manager);
                ctx.Set(controller, "_relay", relay);
                ctx.Set(controller, "_boss", boss);
                ctx.Set(controller, "_telemetry", telemetry);
                ctx.Set(controller, "_roleValue", session.Role);
                ctx.Set(controller, "_addressValue", session.Address);
                ctx.Set(controller, "_connectionsValue", session.Connections);
                ctx.Set(controller, "_pingValue", session.Ping);
                ctx.Set(controller, "_trafficValue", session.Traffic);
                ctx.Set(controller, "_addressField", session.AddressField);
                ctx.Set(controller, "_hostButton", session.Host);
                ctx.Set(controller, "_joinButton", session.Join);
                ctx.Set(controller, "_serverButton", session.Server);
                ctx.Set(controller, "_stopButton", session.Stop);
#if MIRROR
                ctx.Set(controller, "_discovery", discovery);
                ctx.Set(controller, "_lanValue", lan.State);
                ctx.Set(controller, "_lanButtons", lan.Buttons);
#else
                ctx.Set(controller, "_lanValue", null);
                ctx.Set(controller, "_lanButtons", Array.Empty<Button>());
#endif
                ctx.Set(controller, "_pingsValue", relayRows.Pings);
                ctx.Set(controller, "_lastMessageValue", relayRows.LastMessage);
                ctx.Set(controller, "_chatField", relayRows.ChatField);
                ctx.Set(controller, "_bossValue", bossRows.State);
                ctx.Set(controller, "_bossFill", bossRows.Fill);
                ctx.Set(controller, "_log", frame.Log);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    Scene + " build failed at step '" + step + "': " + exception.Message, exception);
            }
        }

        private static void ConfigureManager(DemoSceneBuildContext ctx, NeoNetworkManager manager)
        {
#if MIRROR
            if (manager.transport == null)
            {
                ctx.Set(manager, "transport", EnsureTransport(ctx, manager));
            }

            // WHY: the demo has no player prefab (peers are tracked per connection); with Auto Create Player on,
            // Mirror logs "The PlayerPrefab is empty" on every Host and Join.
            ctx.Set(manager, "autoCreatePlayer", false);
            // WHY: everything the demo uses lives in this scene. A DontDestroyOnLoad manager outlived the scene, and on
            // a reload Mirror destroyed the scene's own manager (the one the saved buttons call) as a duplicate.
            ctx.Set(manager, "dontDestroyOnLoad", false);
            // WHY: two instances on one machine must both keep ticking while the other window has focus.
            ctx.Set(manager, "runInBackground", true);
            ctx.Set(manager, "_activateSceneObjectsOnStart", true);
#else
            ctx.Warn("Mirror is not installed: the NeoNetworkManager stub is kept unconfigured and the LAN rows are skipped.");
#endif
            ctx.MarkModified(manager);
        }

#if MIRROR
        private static Transport EnsureTransport(DemoSceneBuildContext ctx, NeoNetworkManager manager)
        {
            Transport existing = manager.GetComponent<Transport>();
            if (existing != null)
            {
                return existing;
            }

            Type telepathy = Type.GetType("Mirror.TelepathyTransport, Mirror.Transports");
            if (telepathy == null)
            {
                throw new InvalidOperationException("'" + manager.name + "' has no Transport and Mirror.TelepathyTransport " +
                                                    "(assembly Mirror.Transports) was not found.");
            }

            return (Transport)ctx.Ensure(manager.gameObject, telepathy);
        }
#endif

        private static void ConfigureTelemetry(DemoSceneBuildContext ctx, NeoNetworkTelemetry telemetry)
        {
#if MIRROR
            ctx.Set(telemetry, "_captureTraffic", true);
            ctx.Set(telemetry, "_windowSeconds", 1f);
            // WHY: the overlay is IMGUI; the demo shows RttMs and the byte rates in its own uGUI rows instead.
            ctx.Set(telemetry, "_showOverlay", false);
#else
            ctx.MarkModified(telemetry);
#endif
        }

#if MIRROR
        private static void ConfigureDiscovery(DemoSceneBuildContext ctx, NeoNetworkDiscovery discovery,
            NeoNetworkManager manager)
        {
            NetworkDiscovery mirrorDiscovery = ctx.Ensure<NetworkDiscovery>(discovery.gameObject);
            ctx.Set(mirrorDiscovery, "transport", manager.transport);
            ctx.Set(mirrorDiscovery, "secretHandshake", DiscoveryHandshake);
            ctx.Set(mirrorDiscovery, "enableActiveDiscovery", true);
            ctx.Set(discovery, "_autoAdvertiseOnHost", true);
            // WHY: opening the demo must not broadcast on the LAN; searching starts from Find games or Quick Play.
            ctx.Set(discovery, "_autoDiscoverOnClient", false);
            ctx.Set(discovery, "_autoJoinFirstFound", true);
            ctx.Set(discovery, "_hostIfNoneFoundAfter", 3f);
        }
#endif

        private static void ConfigureBossTemplate(RpgCharacterTemplate template)
        {
            template.displayName = BossObjectName;
            template.description = "Network demo target: one HP pool replicated by RpgCharacterNetworkAdapter.";
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = BossHp,
                    startMax = BossHp,
                    restoreOnAwake = true,
                    restoreToFull = true,
                    // WHY: no regeneration; every regen tick on the server would publish a new snapshot to every peer.
                    regen = new RpgRegenDefinition { enabled = false }
                }
            };
            template.stats = Array.Empty<RpgStatDefinition>();
            template.progression = null;
        }

        private static NetworkActionRelay SharedRelay(DemoSceneBuildContext ctx, Transform parent)
        {
            GameObject host = ctx.Child(parent, RelayObjectName);
            EnsureSceneIdentity(ctx, host);
            NetworkActionRelay relay = ctx.Ensure<NetworkActionRelay>(host);
            ctx.Set(relay, "isNetworked", true);
            // WHY: a scene object has no owner; None lets every peer trigger it (the ServerOnly scopes still keep the
            // boss server-authoritative).
            ctx.Set(relay, "_authorityMode", (int)NetworkAuthorityMode.None);

            SerializedObject serialized = new(relay);
            SerializedProperty channels = serialized.FindProperty("_channels");
            if (channels == null || !channels.isArray)
            {
                throw new InvalidOperationException("NetworkActionRelay has no serialized '_channels' array.");
            }

            channels.arraySize = ChannelNames.Length;
            for (int i = 0; i < ChannelNames.Length; i++)
            {
                SerializedProperty channel = channels.GetArrayElementAtIndex(i);
                channel.FindPropertyRelative("channelName").stringValue = ChannelNames[i];
                channel.FindPropertyRelative("scope").intValue = (int)ChannelScopes[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            ctx.MarkModified(relay);
            return relay;
        }

        private static RpgCharacter SharedBoss(DemoSceneBuildContext ctx, Transform parent, RpgCharacterTemplate template,
            out RpgCharacterNetworkAdapter adapter)
        {
            GameObject host = ctx.Child(parent, BossObjectName);
            RpgCharacter boss = ctx.Ensure<RpgCharacter>(host);
            EnsureSceneIdentity(ctx, host);
            adapter = ctx.Ensure<RpgCharacterNetworkAdapter>(host);
            ctx.Set(boss, "_template", template);
            ctx.Set(boss, "_applyTemplateOnAwake", true);
            ctx.Set(boss, "isNetworked", true);
            // WHY: server-authoritative; clients reach the boss only through the ServerOnly relay channels.
            ctx.Set(boss, "_authorityMode", (int)NetworkAuthorityMode.ServerOnly);
            ctx.Set(boss, "_allowClientStateCommands", false);
            ctx.Set(boss, "_loadOnAwake", false);
            ctx.Set(boss, "_autoSave", false);
            return boss;
        }

        private static void EnsureSceneIdentity(DemoSceneBuildContext ctx, GameObject host)
        {
#if MIRROR
            NetworkIdentity identity = ctx.Ensure<NetworkIdentity>(host);
            if (identity.sceneId == 0)
            {
                // WHY: Mirror assigns scene ids from OnValidate in an interactive editor only; a scene object saved
                // with id 0 stops Play Mode ("needs to be opened and resaved"). The id is stable per object name.
                identity.sceneId = StableSceneId(host.name);
                ctx.MarkModified(identity);
            }
#endif
        }

        private static ulong StableSceneId(string objectName)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            string key = "Neo.Demo.NetworkDemo/" + objectName;
            uint hash = offset;
            for (int i = 0; i < key.Length; i++)
            {
                hash ^= key[i];
                hash *= prime;
            }

            return hash == 0 ? 1UL : hash;
        }

        private static SessionRows BuildSessionRows(DemoPanel column, DemoFrame frame, NeoNetworkManager manager,
            NetworkDemoController controller)
        {
            SessionRows rows = new();
            column.Section("Session · NeoNetworkManager");
            rows.Role = Fit(column.ValueLabel("Role", "Offline"));
            rows.Address = Fit(column.ValueLabel("Address", "localhost:7777"));
            rows.Connections = Fit(column.ValueLabel("Connections", "-"));
            rows.Ping = Fit(column.ValueLabel("Ping", "-"));
            rows.Traffic = Fit(column.ValueLabel("Traffic", "-"));

            rows.AddressField = column.InputField("Server address", "localhost", "IP or host name");
            rows.AddressField.characterLimit = 64;
            DemoWiring.AddDynamic(rows.AddressField.onEndEdit, controller.SetServerAddress);

            Button[] buttons = column.ButtonRow("Host", "Join", "Server", "Stop");
            rows.Host = buttons[0];
            rows.Join = buttons[1];
            rows.Server = buttons[2];
            rows.Stop = buttons[3];
            DemoWiring.AddString(rows.Host.onClick, frame.Log.Log, "NeoNetworkManager.StartAsHost() · NoCode");
            DemoWiring.AddVoid(rows.Host.onClick, manager.StartAsHost);
            DemoWiring.AddVoid(rows.Join.onClick, controller.JoinAddressFromField);
            DemoWiring.AddString(rows.Server.onClick, frame.Log.Log, "NeoNetworkManager.StartAsServer() · NoCode");
            DemoWiring.AddVoid(rows.Server.onClick, manager.StartAsServer);
            DemoWiring.AddString(rows.Stop.onClick, frame.Log.Log, "NeoNetworkManager.StopNetwork() · NoCode");
            DemoWiring.AddVoid(rows.Stop.onClick, manager.StopNetwork);
            column.Note("Host / Server / Stop: NoCode calls · Join: C# StartAsClient(address)");
            return rows;
        }

#if MIRROR
        private static LanRows BuildLanRows(DemoPanel column, DemoFrame frame, NeoNetworkDiscovery discovery,
            NetworkDemoController controller)
        {
            LanRows rows = new();
            column.Section("LAN · NeoNetworkDiscovery");
            rows.State = Fit(column.ValueLabel("LAN", "idle"));
            rows.Buttons = column.ButtonRow("Find games", "Join found", "Quick Play");
            DemoWiring.AddString(rows.Buttons[0].onClick, frame.Log.Log, "NeoNetworkDiscovery.StartDiscovery() · NoCode");
            DemoWiring.AddVoid(rows.Buttons[0].onClick, discovery.StartDiscovery);
            DemoWiring.AddVoid(rows.Buttons[1].onClick, controller.JoinFirstLanServer);
            DemoWiring.AddString(rows.Buttons[2].onClick, frame.Log.Log, "NeoNetworkDiscovery.QuickPlay() · NoCode");
            DemoWiring.AddVoid(rows.Buttons[2].onClick, discovery.QuickPlay);
            column.Note("Quick Play joins a LAN host, or hosts itself after 3 s");
            return rows;
        }
#endif

        private static RelayRows BuildRelayRows(DemoPanel column, DemoFrame frame, NetworkActionRelay relay,
            NetworkDemoController controller)
        {
            RelayRows rows = new();
            column.Section("Events · NetworkActionRelay");
            rows.Pings = Fit(column.ValueLabel("Pings heard here", "0"));
            rows.LastMessage = Fit(column.ValueLabel("Last chat message", "-"));

            Button[] buttons = column.ButtonRow("Ping all", "Wave to others");
            DemoWiring.AddString(buttons[0].onClick, frame.Log.Log,
                "NetworkActionRelay.TriggerByName(\"" + NetworkDemoController.PingChannel + "\") · NoCode");
            DemoWiring.AddString(buttons[0].onClick, relay.TriggerByName, NetworkDemoController.PingChannel);
            DemoWiring.AddString(buttons[1].onClick, frame.Log.Log,
                "NetworkActionRelay.TriggerByName(\"" + NetworkDemoController.WaveChannel + "\") · NoCode");
            DemoWiring.AddString(buttons[1].onClick, relay.TriggerByName, NetworkDemoController.WaveChannel);

            rows.ChatField = column.InputField("Chat", string.Empty, "Type, press Enter");
            rows.ChatField.characterLimit = 64;
            // WHY: NoCode path - Enter sends the field text through TriggerString (channel 0 is 'Chat').
            DemoWiring.AddDynamic(rows.ChatField.onSubmit, relay.TriggerString);
            DemoWiring.AddVoid(rows.ChatField.onSubmit, controller.ClearChatField);
            Button send = column.Button("Send · C# TriggerStringByName");
            DemoWiring.AddVoid(send.onClick, controller.SendChatFromField);
            return rows;
        }

        private static BossRows BuildBossRows(DemoPanel column, DemoFrame frame, NetworkActionRelay relay,
            NetworkDemoController controller)
        {
            BossRows rows = new();
            column.Section("State · RpgCharacterNetworkAdapter");
            rows.Fill = column.Bar("Shared Boss HP · replicated to every peer", DemoPalette.Danger, 1f);
            rows.State = Fit(column.ValueLabel("Shared Boss", "asleep · start a session"));

            Button[] buttons = column.ButtonRow("Hit 10", "Hit 25", "Revive");
            DemoWiring.AddFloat(buttons[0].onClick, controller.RequestBossHit, 10f);
            DemoWiring.AddFloat(buttons[1].onClick, controller.RequestBossHit, 25f);
            DemoWiring.AddString(buttons[2].onClick, frame.Log.Log,
                "NetworkActionRelay.TriggerByName(\"" + NetworkDemoController.ReviveBossChannel + "\") · NoCode");
            DemoWiring.AddString(buttons[2].onClick, relay.TriggerByName, NetworkDemoController.ReviveBossChannel);
            column.Note("Hits use a ServerOnly channel: only the server writes HP");
            return rows;
        }

        private static void WireRelayChannels(DemoSceneBuildContext ctx, NetworkActionRelay relay,
            NetworkDemoController controller, RpgCharacter boss, RpgCharacterNetworkAdapter adapter)
        {
            // WHY: ctx.Ensure cleared the relay's listeners in this pass, so each channel is wired from scratch.
            DemoWiring.AddDynamic(Channel(relay, 0).onTriggeredString, controller.OnChatReceived);
            DemoWiring.AddVoid(Channel(relay, 1).onTriggered, controller.OnPingReceived);
            DemoWiring.AddVoid(Channel(relay, 2).onTriggered, controller.OnWaveReceived);
            NetworkActionChannel hit = Channel(relay, 3);
            DemoWiring.AddDynamic(hit.onTriggeredFloat, adapter.NetDamage);
            DemoWiring.AddDynamic(hit.onTriggeredFloat, controller.OnBossHitOnServer);
            NetworkActionChannel revive = Channel(relay, 4);
            DemoWiring.AddVoid(revive.onTriggered, boss.Restore);
            DemoWiring.AddVoid(revive.onTriggered, controller.OnBossRevivedOnServer);
            ctx.MarkModified(relay);
        }

        private static NetworkActionChannel Channel(NetworkActionRelay relay, int index)
        {
            NetworkActionChannel channel = relay.GetChannel(index);
            if (channel == null || channel.channelName != ChannelNames[index])
            {
                throw new InvalidOperationException("Relay channel " + index + " should be '" + ChannelNames[index] +
                                                    "' but is '" + (channel == null ? "<missing>" : channel.channelName) +
                                                    "'.");
            }

            return channel;
        }

        private static void WireManagerEvents(DemoSceneBuildContext ctx, NeoNetworkManager manager, DemoLogFeed log,
            NetworkDemoController controller)
        {
            // WHY: the manager is hand-authored, so the context does not clear its events; this demo owns them.
            Rewire(manager.OnServerStartedEvent, log, "NeoNetworkManager.OnServerStarted");
            Rewire(manager.OnServerStoppedEvent, log, "NeoNetworkManager.OnServerStopped");
            Rewire(manager.OnClientConnectedEvent, log, "NeoNetworkManager.OnClientConnected");
            Rewire(manager.OnClientDisconnectedEvent, log, "NeoNetworkManager.OnClientDisconnected");
#if MIRROR
            DemoWiring.Clear(manager.OnServerClientConnectedEvent);
            DemoWiring.AddDynamic(manager.OnServerClientConnectedEvent, controller.OnServerClientConnected);
            DemoWiring.Clear(manager.OnServerClientDisconnectedEvent);
            DemoWiring.AddDynamic(manager.OnServerClientDisconnectedEvent, controller.OnServerClientDisconnected);
#endif
            ctx.MarkModified(manager);
        }

        private static void Rewire(UnityEngine.Events.UnityEvent unityEvent, DemoLogFeed log, string message)
        {
            DemoWiring.Clear(unityEvent);
            DemoWiring.AddString(unityEvent, log.Log, message);
        }

#if MIRROR
        private static void WireDiscoveryEvents(NeoNetworkDiscovery discovery, DemoLogFeed log,
            NetworkDemoController controller)
        {
            DemoWiring.AddVoid(discovery.OnDiscoveryStarted, controller.OnLanSearchStarted);
            DemoWiring.AddDynamic(discovery.OnServerFound, controller.OnLanServerFound);
            DemoWiring.AddDynamic(discovery.OnQuickPlayResolved, controller.OnQuickPlayResolved);
            DemoWiring.AddString(discovery.OnAdvertisingStarted, log.Log,
                "NeoNetworkDiscovery.OnAdvertisingStarted · this host is visible on the LAN");
        }
#endif

        private static TMP_Text Fit(TMP_Text label)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 24f;
            return label;
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Network · Mirror",
                Purpose = "NeoNetworkManager runs Host, Client or Server. NetworkActionRelay carries NoCode events to " +
                          "every peer, RpgCharacterNetworkAdapter replicates the Shared Boss and NeoNetworkDiscovery " +
                          "finds LAN games. Every button is a saved UnityEvent: select it to see the call.",
                RuntimeApi = "NeoNetworkManager.StartAsHost / StartAsClient(address) / StartAsServer / StopNetwork; " +
                             "NetworkActionRelay.TriggerByName / TriggerStringByName / TryGetChannelIndex + " +
                             "TriggerFloatAt; NeoNetworkDiscovery.StartDiscovery / QuickPlay / ConnectToFirstServer; " +
                             "RpgCharacterNetworkAdapter.NetDamage; NeoNetworkTelemetry.RttMs / BytesInPerSecond.",
                SceneWorkflow = "'Network Demo': NeoNetworkManager + TelepathyTransport, lifecycle events log to the " +
                                "feed. '[Demo] Network Objects/Shared Relay': five channels and their listeners. " +
                                "'[Demo] Network Objects/Shared Boss': RpgCharacter + NetworkIdentity + " +
                                "RpgCharacterNetworkAdapter. '[Demo] Services': LAN Discovery and Network Telemetry.",
                Verification = "Offline: Ping and Chat fire locally. Host: role 'Host' and the boss wakes up. A second " +
                               "instance joins (Join or Quick Play): Ping reaches both peers, Wave only the other one, " +
                               "Hit changes the boss HP on both.",
                Hints = new[]
                {
                    "Second instance: a build, a Multiplayer Play Mode or ParrelSync clone; then Join (localhost)",
                    "Relay channels are events (late joiners miss them); boss HP is replicated state",
                    "Hit and Revive use ServerOnly channels: clients never write HP"
                },
                PanelWidth = 400f
            };
        }

        private sealed class SessionRows
        {
            public TMP_Text Role;
            public TMP_Text Address;
            public TMP_Text Connections;
            public TMP_Text Ping;
            public TMP_Text Traffic;
            public TMP_InputField AddressField;
            public Button Host;
            public Button Join;
            public Button Server;
            public Button Stop;
        }

#if MIRROR
        private sealed class LanRows
        {
            public TMP_Text State;
            public Button[] Buttons;
        }
#endif

        private sealed class RelayRows
        {
            public TMP_Text Pings;
            public TMP_Text LastMessage;
            public TMP_InputField ChatField;
        }

        private sealed class BossRows
        {
            public Image Fill;
            public TMP_Text State;
        }
    }
}
