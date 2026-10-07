using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Network;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     The Network docs once documented <c>StartHost()</c> / <c>NeoNetworkManager.Singleton</c>, which do not exist
    ///     (the real surface is <c>StartAsHost()</c> and friends). These tests keep the headline API names of the
    ///     pages honest: every listed member must exist on its type, and the known-stale spellings must not return.
    /// </summary>
    [TestFixture]
    public sealed class NetworkDocsConsistencyTests
    {
        private const string DocsRoot = "Assets/Neoxider/Docs/Network";

        private const BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                                | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        // "Type.Member" pairs the docs name as API. Members are looked up as methods, properties, fields or events.
        private static readonly string[] DocumentedMembers =
        {
            "Neo.Network.NeoNetworkManager.StartAsHost",
            "Neo.Network.NeoNetworkManager.StartAsClient",
            "Neo.Network.NeoNetworkManager.StartAsServer",
            "Neo.Network.NeoNetworkManager.StopNetwork",
            "Neo.Network.NeoNetworkManager.IsServer",
            "Neo.Network.NeoNetworkManager.IsClient",
            "Neo.Network.NeoNetworkManager.IsHost",
#if MIRROR
            "Neo.Network.NeoNetworkManager.ApplyPort",
            "Neo.Network.NeoNetworkManager.IsConnectionSpawned",
            "Neo.Network.NeoNetworkManager.IsLocalPlayerSpawned",
            "Neo.Network.NeoNetworkManager.HandshakeMode",
            "Neo.Network.NeoNetworkManager.ActivateSceneObjectsOnStart",
            "Neo.Network.NeoNetworkManager.OnServerClientConnectedEvent",
            "Neo.Network.NeoNetworkManager.OnServerClientReadyEvent",
            "Neo.Network.NeoNetworkManager.OnServerPlayerReadyEvent",
            "Neo.Network.NeoNetworkManager.OnServerClientDisconnectedEvent",
            "Neo.Network.NeoNetworkManager.ServerClientConnected",
            "Neo.Network.NeoNetworkManager.ServerClientReady",
            "Neo.Network.NeoNetworkManager.ServerPlayerReady",
            "Neo.Network.NeoNetworkManager.ServerClientDisconnected",
            "Neo.Network.NeoNetworkManager.LocalPlayerSpawned",
            "Neo.Network.NeoNetworkManager.ClientSessionStarted",
            "Neo.Network.NeoNetworkManager.ServerSessionStarted",
            "Neo.Network.NeoNetworkManager.UseScenePlayerTemplate",
            "Neo.Network.NeoNetworkManager.ScenePlayerTemplate",
            "Neo.Network.NeoNetworkManager.ScenePlayerTemplateSpawnId",
            "Neo.Network.NeoNetworkManager.DisableScenePlayerTemplate",
            "Neo.Network.NeoNetworkState.IsConnectionSpawned",
            "Neo.Network.NeoNetworkState.IsLocalHostConnection",
            "Neo.Network.NeoMirrorSceneReactivator.ActivateNetworkedSceneObjects",
            "Neo.Network.Realtime.NetClientHandlers.Add",
            "Neo.Network.Realtime.NetClientHandlers.RegisterNow",
            "Neo.Network.Realtime.NetClientHandlers.UnregisterAll",
            "Neo.Network.Realtime.NetClientHandlers.Tick",
            "Neo.Network.Realtime.NetServerHandlers.Add",
            "Neo.Network.Realtime.NetServerHandlers.RegisterNow",
            "Neo.Network.Realtime.NetReadyBroadcast.CanReceive",
            "Neo.Network.Realtime.NetReadyBroadcast.ToReadyClients",
            "Neo.Network.Realtime.NetReadyBroadcast.SendTo",
            "Neo.Network.Realtime.NetReadyBroadcast.CountReady",
            "Neo.Network.Realtime.NetFrameFraming.WriteHeader",
            "Neo.Network.Realtime.NetFrameFraming.PatchLength",
            "Neo.Network.Realtime.NetFrameFraming.ReadHeader",
            "Neo.Network.Realtime.NetFrameFraming.VerifyBodyLength",
            "Neo.Network.Realtime.NetFrameFraming.ReadCount",
            "Neo.Network.Realtime.NetFrameFraming.ReadCount32",
            "Neo.Network.Realtime.NetFrameFraming.NonNegative",
            "Neo.Network.Realtime.NetFrameFraming.ClampByte",
            "Neo.Network.Realtime.NetFrameSender.SendToReady",
            "Neo.Network.Realtime.NetFrameSender.SendTo",
            "Neo.Network.Realtime.NetFrameSender.ChunkBytes",
            "Neo.Network.Realtime.NetFrameReceiver.Subscribe",
            "Neo.Network.Realtime.NetFrameReceiver.GetAssembler",
            "Neo.Network.Realtime.NetFrameReceiver.Detach",
            "Neo.Network.NeoNetworkTelemetry.BuildSummary",
            "Neo.Network.NeoNetworkTelemetry.ToggleOverlay",
            "Neo.Network.NeoNetworkTelemetry.SetTimeline",
            "Neo.Network.NeoNetworkTelemetry.RttMs",
            "Neo.Network.NetTelemetry.RttMilliseconds",
#endif
            "Neo.Network.NeoNetworkBootstrap.StartNetwork",
            "Neo.Network.NeoNetworkBootstrap.StartAs",
            "Neo.Network.NeoNetworkBootstrap.StartAsHost",
            "Neo.Network.NeoNetworkBootstrap.StartAsClient",
            "Neo.Network.NeoNetworkBootstrap.StopNetwork",
            "Neo.Network.NeoNetworkBootstrap.BuildStatusLine",
            "Neo.Network.NeoNetworkBootstrap.StatusExtraProvider",
            "Neo.Network.NeoNetworkBootstrap.Resolve",
            "Neo.Network.NeoNetworkBootstrap.LocalPresentationSuppressed",
            "Neo.Network.NeoNetworkBootstrap.OwnsFrameRate",
            "Neo.Network.NeoNetworkBootstrap.OnNetworkStartedEvent",
            "Neo.Network.NeoNetworkBootstrap.IntentResolved",
            "Neo.Network.NeoStartupIntent.HasCustom",
            "Neo.Network.NeoStartupIntent.TryGetCustom",
            "Neo.Network.NeoStartupIntent.GetCustomInt",
            "Neo.Network.NeoStartupIntent.GetCustomFloat",
            "Neo.Network.NeoStartupIntent.WithMode",
            "Neo.Network.NeoStartupIntent.WithAddress",
            "Neo.Network.NeoStartupCommandLine.Parse",
            "Neo.Network.NeoStartupCommandLine.FromUrlQuery",
            "Neo.Network.NetTrafficMeter.Record",
            "Neo.Network.NetTrafficMeter.Tick",
            "Neo.Network.NetTrafficMeter.TryGetKind",
            "Neo.Network.NetTrafficMeter.GetTopKinds",
            "Neo.Network.Realtime.SnapshotTimeline.OnFrame",
            "Neo.Network.Realtime.SnapshotTimeline.Advance",
            "Neo.Network.Realtime.SnapshotTimeline.RenderTimeExact",
            "Neo.Network.Realtime.SnapshotTimeline.JitterSeconds",
            "Neo.Network.Realtime.SnapshotTimeline.ServerClockRate",
            "Neo.Network.Realtime.SnapshotTimelineSettings.ForSendRate",
            "Neo.Network.Realtime.LocalPredictionModel.Step",
            "Neo.Network.Realtime.LocalPredictionModel.Reconcile",
            "Neo.Network.Realtime.LocalPredictionModel.Relax",
            "Neo.Network.Realtime.LocalPredictionModel.RenderXAt",
            "Neo.Network.Realtime.LocalPredictionModel.Teleport",
            "Neo.Network.Realtime.LocalPredictionSettings.ForTickRate",
            "Neo.Network.Realtime.NetFragmentation.ChunkSizeForThreshold",
            "Neo.Network.Realtime.NetFragmentation.TrySlice",
            "Neo.Network.Realtime.NetFragmentAssembler.TryAdd",
            "Neo.Network.Realtime.NetQuantization.PackPosition",
            "Neo.Network.Realtime.NetQuantization.PackShort",
            "Neo.Network.Realtime.NetQuantization.PackHitPoints",
            "Neo.Network.Realtime.NetInterpolation.LerpAngleDegrees",
            "Neo.Network.Realtime.SnapshotBuffer`1.TrySample",
            "Neo.Network.Realtime.SnapshotBuffer`1.TryAdd",
            "Neo.Network.Realtime.SnapshotBuffer`1.OnEvicted",
            "Neo.Network.NetworkSingleton`1.I",
            "Neo.Network.NetworkSingleton`1.Instance",
            "Neo.Network.NetworkSingleton`1.HasInstance",
            "Neo.Network.NetworkSingleton`1.IsInitialized",
            "Neo.Network.NetworkSingleton`1.TryGetInstance",
            "Neo.Network.NetworkSingleton`1.ForgetFailedSearch",
            "Neo.Network.NetworkSingleton`1.DestroyInstance",
            "Neo.Network.NetworkSingleton`1.HasServerAuthority"
        };

        private static readonly string[] StaleSpellings =
        {
            "NeoNetworkManager.Singleton",
            "Singleton.StartHost()",
            "Singleton.StartClient()",
            "HasServerAuthority()",
            "NetworkSingleton<T>.Singleton"
        };

        private static Type FindType(string fullName)
        {
            Assembly assembly = typeof(NeoNetworkManager).Assembly;
            return assembly.GetType(fullName, false);
        }

        [TestCaseSource(nameof(DocumentedMembers))]
        public void DocumentedMember_Exists(string qualifiedMember)
        {
            int dot = qualifiedMember.LastIndexOf('.');
            string typeName = qualifiedMember.Substring(0, dot);
            string memberName = qualifiedMember.Substring(dot + 1);

            Type type = FindType(typeName);
            Assert.That(type, Is.Not.Null, $"documented type {typeName} was renamed or removed");

            MemberInfo[] members = type.GetMember(memberName, AllMembers);
            Assert.That(members, Is.Not.Empty,
                $"{qualifiedMember} is documented under Docs/Network but does not exist; fix the docs or the code");
        }

        [Test]
        public void NetworkDocs_DoNotUseStaleSpellings()
        {
            List<string> offenders = new List<string>();
            foreach (string file in Directory.GetFiles(DocsRoot, "*.md", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                for (int i = 0; i < StaleSpellings.Length; i++)
                {
                    if (text.Contains(StaleSpellings[i]))
                    {
                        offenders.Add($"{file.Replace('\\', '/')} contains '{StaleSpellings[i]}'");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "These spellings were documented before but never existed; use StartAsHost/StartAsClient/StartAsServer/StopNetwork and NetworkSingleton<T>.I.");
        }

        [Test]
        public void EveryNetworkPage_OpensWithWhatItIsAndHowToUse_OrContents()
        {
            string[] pages =
            {
                "NeoNetworkBootstrap.md", "NeoNetworkTelemetry.md", "NeoNetworkManager.md", "NetworkSingleton.md",
                "Realtime_IO_Guide.md", "Realtime/README.md", "Realtime/SnapshotInterpolation.md",
                "Realtime/LocalPredictionModel.md", "Realtime/NetQuantization.md", "Realtime/NetFraming.md",
                "Realtime/NetMessaging.md"
            };

            List<string> offenders = new List<string>();
            for (int i = 0; i < pages.Length; i++)
            {
                string text = File.ReadAllText(Path.Combine(DocsRoot, pages[i]));
                bool hasWhat = text.Contains("**What it is:**");
                bool hasHow = text.Contains("**How to use:**") || text.Contains("**Contents:**");
                if (!hasWhat || !hasHow)
                {
                    offenders.Add(pages[i]);
                }
            }

            Assert.That(offenders, Is.Empty, "DOCUMENTATION.md: every page opens with 'What it is' and 'How to use' (or 'Contents').");
        }
    }
}
