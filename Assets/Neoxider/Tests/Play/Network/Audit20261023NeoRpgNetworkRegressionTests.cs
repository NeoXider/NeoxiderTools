using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Network;
using NUnit.Framework;
using UnityEngine;

#if MIRROR
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using Mirror.RemoteCalls;
using Neo.Core.Resources;
using Neo.Network;
using Neo.Rpg.Runtime;
using Neo.Save;
#endif

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoRpgNetworkLifecycleTests
    {
        [Test]
        public void InactiveCharacter_NetDamageAndHealWorkBeforeAwake()
        {
            GameObject actor = new("AuditRpgInactiveCharacter");
            actor.SetActive(false);
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            template.resources = new[]
            {
                new RpgResourceDefinition { startCurrent = 100f, startMax = 100f }
            };

            try
            {
                RpgCharacter character = actor.AddComponent<RpgCharacter>();
                RpgCharacterNetworkAdapter adapter = actor.AddComponent<RpgCharacterNetworkAdapter>();
                character.ApplyTemplate(template);
                int damageEvents = 0;
                int healEvents = 0;
                character.OnDamagedEvent.AddListener(_ => damageEvents++);
                character.OnHealedEvent.AddListener(_ => healEvents++);

                adapter.NetDamage(25f);
                Assert.That(character.HpValue, Is.EqualTo(75f));
                adapter.NetHeal(5f);

                Assert.That(character.HpValue, Is.EqualTo(80f));
                Assert.That(damageEvents, Is.EqualTo(1));
                Assert.That(healEvents, Is.EqualTo(1));
                Assert.That(actor.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(actor);
                Object.DestroyImmediate(template);
            }
        }
    }

#if MIRROR
    /// <summary>
    ///     Exercises the woven command and snapshot boundaries using the existing in-memory transport.
    ///     No private adapter fields or generated UserCode method names are accessed.
    /// </summary>
    public sealed class Audit20261023NeoRpgNetworkMirrorTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<ScriptableObject> _assets = new();
        private readonly List<string> _saveKeys = new();
        private GameObject _managerObject;
        private TestNetworkManager _manager;
        private RpgCharacterTemplate _template;
        private Transport _previousTransport;

        [SetUp]
        public void SetUp()
        {
            _previousTransport = Transport.active;
            _manager = NetworkTestHelper.CreateTestNetworkManager("AuditRpgNetworkManager", out _managerObject);
            _template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            _template.resources = new[]
            {
                new RpgResourceDefinition { startCurrent = 100f, startMax = 100f }
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_manager != null)
            {
                if (NetworkClient.active)
                {
                    _manager.StopClient();
                }

                if (NetworkServer.active)
                {
                    _manager.StopServer();
                }
            }

            foreach (GameObject ownedObject in _objects)
            {
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            _objects.Clear();
            foreach (ScriptableObject asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
            foreach (string key in _saveKeys)
            {
                SaveProvider.DeleteKey(key);
            }

            _saveKeys.Clear();
            Object.DestroyImmediate(_template);
            Object.DestroyImmediate(_managerObject);
            Transport.active = _previousTransport;
        }

        [TestCase(false, 100f)]
        [TestCase(true, 75f)]
        public void ClientStart_UsesCharacterToggleChangedAfterAwake(bool networked, float expectedHp)
        {
            _manager.StartServer();
            Assert.That(NetworkServer.active, Is.True);
            RpgCharacterNetworkAdapter source = CreateCharacter(true, out RpgCharacter sourceCharacter);
            source.NetDamage(25f);
            Assert.That(sourceCharacter.HpValue, Is.EqualTo(75f));
            byte[] payload = SerializeSnapshot(source, true);

            RpgCharacterNetworkAdapter target = CreateCharacter(!networked, out RpgCharacter targetCharacter);
            targetCharacter.isNetworked = networked;
            DeserializeSnapshot(target, payload, true);
            // A server-side replay does not run the client SyncVar hook: startup must apply the payload.
            Assert.That(targetCharacter.HpValue, Is.EqualTo(100f));

            target.OnStartClient();

            Assert.That(targetCharacter.HpValue, Is.EqualTo(expectedHp));
            Assert.That(target.isNetworked, Is.EqualTo(networked));
        }

        [TestCase(false, 100f)]
        [TestCase(true, 75f)]
        public void DeltaSnapshot_RespectsCharacterNetworkingOptOut(bool networked, float expectedHp)
        {
            _manager.StartServer();
            Assert.That(NetworkServer.active, Is.True);
            RpgCharacterNetworkAdapter source = CreateCharacter(true, out RpgCharacter sourceCharacter);
            source.NetDamage(25f);
            Assert.That(sourceCharacter.HpValue, Is.EqualTo(75f));
            byte[] payload = SerializeSnapshot(source, false);
            _manager.StopServer();
            _manager.StartClient();
            Assert.That(NeoNetworkState.IsClientOnly, Is.True);

            // This is a post-spawn delta contract. Complete the empty initial spawn batch
            // through Mirror's public transport boundary before expecting immediate hooks.
            Transport transport = _manager.transport;
            transport.OnClientConnected.Invoke();
            Assert.That(NetworkClient.isConnected, Is.True);
            Assert.That(NetworkClient.connection.isAuthenticated, Is.True);
            Assert.That(NetworkClient.spawned, Is.Empty);
            using (NetworkWriterPooled started = NetworkWriterPool.Get())
            using (NetworkWriterPooled finished = NetworkWriterPool.Get())
            using (NetworkWriterPooled batch = NetworkWriterPool.Get())
            {
                NetworkMessages.Pack(new ObjectSpawnStartedMessage(), started);
                NetworkMessages.Pack(new ObjectSpawnFinishedMessage(), finished);
                Batcher batcher = new(transport.GetMaxPacketSize(Channels.Reliable));
                double timestamp = NetworkTime.localTime;
                batcher.AddMessage(started.ToArraySegment(), timestamp);
                batcher.AddMessage(finished.ToArraySegment(), timestamp);
                Assert.That(batcher.GetBatch(batch), Is.True);
                transport.OnClientDataReceived.Invoke(batch.ToArraySegment(), Channels.Reliable);
            }

            Assert.That(NetworkClient.isConnected, Is.True);

            RpgCharacterNetworkAdapter target = CreateCharacter(true, out RpgCharacter targetCharacter);
            targetCharacter.isNetworked = networked;

            DeserializeSnapshot(target, payload, false);

            Assert.That(targetCharacter.HpValue, Is.EqualTo(expectedHp));
        }

        [Test]
        public void DisabledCharacter_RejectsCommandWithoutConsumingNextEnabledCommand()
        {
            _manager.StartServer();
            Assert.That(NetworkServer.active, Is.True);
            RpgCharacterNetworkAdapter adapter = CreateCharacter(false, out RpgCharacter character);
            character.AllowClientStateCommands = true;
            character.AuthorityMode = NetworkAuthorityMode.None;
            int damageEvents = 0;
            character.OnDamagedEvent.AddListener(_ => damageEvents++);
            NetworkConnectionToClient sender = new(88321);

            DispatchDamage(adapter, sender, 25f);
            Assert.That(character.HpValue, Is.EqualTo(100f));
            Assert.That(damageEvents, Is.Zero);

            character.isNetworked = true;
            DispatchDamage(adapter, sender, 25f);
            Assert.That(character.HpValue, Is.EqualTo(75f));
            Assert.That(damageEvents, Is.EqualTo(1));

            DispatchDamage(adapter, sender, 25f);
            Assert.That(character.HpValue, Is.EqualTo(75f), "The enabled command still uses the rate limiter.");
            Assert.That(damageEvents, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActiveServer_AutoLoadPublishesCompleteSnapshotWithoutReplayingCurrentGrant(bool rebuild)
        {
            _manager.StartServer();
            Assert.That(NetworkServer.active, Is.True);
            BuffDefinition buff = CreateCurrentGrantBuff();
            ConfigureSnapshotTemplate(buff);
            RpgCharacterProfileData saved = new() { Level = 3, Xp = 17f, UpgradePoints = 2 };
            saved.Resources.Add(new RpgResourceSaveEntry { Id = RpgResourceId.Hp, Max = 160f, Current = 130f });
            saved.Resources.Add(new RpgResourceSaveEntry { Id = "Mana", Max = 25f, Current = 7f });
            saved.Stats.Add(new RpgStatSaveEntry { Id = "S", Base = 42f });
            saved.Stats.Add(new RpgStatSaveEntry { Id = "Other", Base = 11f });
            saved.ActiveBuffs.Add(new ActiveBuffEntry
            {
                BuffId = buff.Id,
                ExpiresAtUtc = RpgTimeUtility.GetCurrentUnixTimestamp() + 3600d,
                Stacks = 1
            });
            string key = "Audit20261023NeoRpgNetwork_" + System.Guid.NewGuid().ToString("N");
            _saveKeys.Add(key);
            SaveProvider.SetString(key, new RpgCharacterProfileService().Serialize(saved));

            RpgCharacter sourceCharacter;
            RpgCharacterNetworkAdapter source;
            if (rebuild)
            {
                source = CreateCharacter(true, out sourceCharacter);
                sourceCharacter.Damage(0f); // Resolve the real adapter before clearing/rebuilding runtime state.
                SpawnCharacter(source, 88341);
            }
            else
            {
                GameObject actor = CreateObject("AuditRpgFirstInitialization");
                actor.SetActive(false);
                sourceCharacter = actor.AddComponent<RpgCharacter>();
                sourceCharacter.Template = _template;
                sourceCharacter.isNetworked = true;
                source = actor.AddComponent<RpgCharacterNetworkAdapter>();
            }

            SetAuthoringField(sourceCharacter, "_saveKey", key);
            SetAuthoringField(sourceCharacter, "_loadOnAwake", true);
            byte[] priorPayload = SerializeSnapshot(source, true);
            List<string> lifecycle = new();
            List<byte[]> payloadsDuringInitialization = new();
            sourceCharacter.RuntimeRebuilt += () =>
            {
                lifecycle.Add("rebuilt");
                payloadsDuringInitialization.Add(SerializeSnapshot(source, true));
            };
            sourceCharacter.OnProfileLoadedEvent.AddListener(() =>
            {
                lifecycle.Add("loaded");
                payloadsDuringInitialization.Add(SerializeSnapshot(source, true));
            });
            int sourceDeaths = 0;
            sourceCharacter.OnDeathEvent.AddListener(() => sourceDeaths++);

            if (rebuild)
            {
                sourceCharacter.RebuildRuntime();
            }
            else
            {
                // First initialization goes through the facade, including its post-Execute notification.
                // This branch checks completion/data but cannot isolate/count the core's final notification.
                source.NetDamage(0f);
            }

            Assert.That(lifecycle, Is.EqualTo(new[] { "rebuilt", "loaded" }));
            Assert.That(payloadsDuringInitialization.Count, Is.EqualTo(2));
            foreach (byte[] intermediatePayload in payloadsDuringInitialization)
            {
                Assert.That(intermediatePayload, Is.EqualTo(priorPayload),
                    "RuntimeRebuilt and auto-load callbacks must not expose a partially published snapshot.");
            }

            AssertRestoredSnapshot(sourceCharacter);
            Assert.That(sourceDeaths, Is.Zero);
            // Both branches observe the completed payload. Direct RebuildRuntime proves core-to-adapter
            // publication; first-init NetDamage also publishes from the wrapper after Execute.
            byte[] publishedPayload = SerializeSnapshot(source, true);
            Assert.That(publishedPayload, Is.Not.EqualTo(priorPayload));
            if (!rebuild)
            {
                source.gameObject.SetActive(true);
                SpawnCharacter(source, 88342);
                Assert.That(SerializeSnapshot(source, true), Is.EqualTo(publishedPayload));
            }

            RpgCharacterNetworkAdapter receiver = CreateCharacter(true, out RpgCharacter receiverCharacter);
            int receiverAppliedEvents = 0;
            int receiverDeaths = 0;
            receiverCharacter.OnBuffAppliedEvent.AddListener(_ => receiverAppliedEvents++);
            receiverCharacter.OnDeathEvent.AddListener(() => receiverDeaths++);
            for (int replay = 0; replay < 2; replay++)
            {
                DeserializeSnapshot(receiver, publishedPayload, true);
                // This public lifecycle invokes ApplyNetworkState on an unspawned, non-server receiver.
                receiver.OnStartClient();
                AssertRestoredSnapshot(receiverCharacter);
                Assert.That(receiverCharacter.CaptureProfile().ActiveBuffs.Count, Is.EqualTo(1));
                Assert.That(receiverCharacter.CaptureProfile().ActiveBuffs[0].BuffId, Is.EqualTo(buff.Id));
                Assert.That(receiverAppliedEvents, Is.Zero, "Snapshot restoration must not reapply/grant the buff.");
                Assert.That(receiverDeaths, Is.Zero);

                receiverCharacter.RefreshAllDerived(false);
                Assert.That(receiverCharacter.MaxHp, Is.EqualTo(150f),
                    "A later ordinary refresh uses BaseMax100 plus the restored buff50, not saved Max160 as base.");
                Assert.That(receiverCharacter.CurrentHp, Is.EqualTo(130f));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ApplyLevelGrowthCommand_UsesExistingTrustedStateOptInAndRateLimit(bool allowClientStateCommands)
        {
            _manager.StartServer();
            Assert.That(NetworkServer.active, Is.True);
            ConfigureGrowthTemplate(RpgLevelGrowthMode.AllStatsEveryLevel, false);
            RpgCharacterNetworkAdapter adapter = CreateCharacter(true, out RpgCharacter character);
            character.AllowClientStateCommands = allowClientStateCommands;
            character.AuthorityMode = NetworkAuthorityMode.None;
            character.SetLevel(3);
            Assert.That(character.GetStat("S"), Is.EqualTo(10f));
            int growthEvents = 0;
            character.OnStatChangedEvent.AddListener((string id, float value) => growthEvents++);
            NetworkConnectionToClient sender = new(88322);

            DispatchCommand(adapter, sender,
                new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyLevelGrowth));

            float expectedStat = allowClientStateCommands ? 14f : 10f;
            Assert.That(character.GetStat("S"), Is.EqualTo(expectedStat));
            Assert.That(growthEvents, Is.EqualTo(allowClientStateCommands ? 1 : 0));
            Assert.That(character.Level, Is.EqualTo(3));

            character.SetLevel(4); // Deferred growth preserves the last applied anchor.
            DispatchCommand(adapter, sender,
                new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.ApplyLevelGrowth));

            Assert.That(character.GetStat("S"), Is.EqualTo(expectedStat),
                "The same-frame repeat cannot bypass the existing command budget or trusted-state policy.");
            Assert.That(growthEvents, Is.EqualTo(allowClientStateCommands ? 1 : 0));
        }

        [TestCase(RpgLevelGrowthMode.None, true, 10f)]
        [TestCase(RpgLevelGrowthMode.ManualUpgradePoints, true, 10f)]
        [TestCase(RpgLevelGrowthMode.AllStatsEveryLevel, false, 10f)]
        [TestCase(RpgLevelGrowthMode.AllStatsEveryLevel, true, 14f)]
        [TestCase(RpgLevelGrowthMode.Hybrid, true, 14f)]
        public void SnapshotV2_PreservesDocumentedGrowthAnchorPolicy(
            RpgLevelGrowthMode mode, bool automatic, float expectedReceiverStat)
        {
            _manager.StartServer();
            Assert.That(NetworkServer.active, Is.True);
            ConfigureGrowthTemplate(mode, automatic);
            RpgCharacterNetworkAdapter source = CreateCharacter(true, out RpgCharacter sourceCharacter);
            sourceCharacter.SetLevel(3);
            sourceCharacter.ApplyLevelGrowth();
            Assert.That(sourceCharacter.GetStat("S"), Is.EqualTo(14f));
            byte[] payload = SerializeSnapshot(source, true);
            RpgCharacterNetworkAdapter receiver = CreateCharacter(true, out RpgCharacter receiverCharacter);

            DeserializeSnapshot(receiver, payload, true);
            receiver.OnStartClient();

            Assert.That(receiverCharacter.Level, Is.EqualTo(3));
            Assert.That(receiverCharacter.GetStat("S"), Is.EqualTo(expectedReceiverStat));
            Assert.That(receiverCharacter.CaptureProfile().Version, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClientProjectilePolicy_FallsBackToAdapterParentIdentity(bool nullSource)
        {
            _manager.StartClient();
            Assert.That(NeoNetworkState.IsClientOnly, Is.True);
            GameObject actor = CreateObject("AuditNetworkActor");
            actor.AddComponent<NetworkIdentity>();
            GameObject weapon = CreateObject("AuditNetworkWeapon");
            weapon.transform.SetParent(actor.transform);
            RpgAttackNetworkAdapter adapter = weapon.AddComponent<RpgAttackNetworkAdapter>();
            GameObject source = nullSource ? null : CreateObject("AuditLocalDamageSource");

            Assert.That(adapter.CanProcessBuiltInInput(source), Is.False);
            Assert.That(adapter.ShouldBlockProjectileSpawn(source), Is.True);
        }

        [Test]
        public void ClientProjectilePolicy_PreservesLocalObjectsWithoutIdentity()
        {
            _manager.StartClient();
            Assert.That(NeoNetworkState.IsClientOnly, Is.True);
            GameObject actor = CreateObject("AuditLocalActor");
            RpgAttackNetworkAdapter adapter = actor.AddComponent<RpgAttackNetworkAdapter>();

            Assert.That(adapter.CanProcessBuiltInInput(actor), Is.True);
            Assert.That(adapter.ShouldBlockProjectileSpawn(actor), Is.False);
        }

        private GameObject CreateObject(string name)
        {
            GameObject ownedObject = new(name);
            _objects.Add(ownedObject);
            return ownedObject;
        }

        private RpgCharacterNetworkAdapter CreateCharacter(bool networkedAtAwake, out RpgCharacter character)
        {
            GameObject actor = CreateObject("AuditRpgCharacter");
            actor.SetActive(false);
            NetworkIdentity identity = actor.AddComponent<NetworkIdentity>();
            character = actor.AddComponent<RpgCharacter>();
            character.Template = _template;
            character.isNetworked = networkedAtAwake;
            RpgCharacterNetworkAdapter adapter = actor.AddComponent<RpgCharacterNetworkAdapter>();
            // Mirror's one native Awake must see the complete, configured behaviour graph.
            actor.SetActive(true);
            Assert.That(adapter.netIdentity, Is.SameAs(identity));
            return adapter;
        }

        private static byte[] SerializeSnapshot(RpgCharacterNetworkAdapter adapter, bool initialState)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            adapter.OnSerialize(writer, initialState);
            return writer.ToArray();
        }

        private static void DeserializeSnapshot(RpgCharacterNetworkAdapter adapter, byte[] payload, bool initialState)
        {
            using NetworkReaderPooled reader = NetworkReaderPool.Get(payload);
            adapter.OnDeserialize(reader, initialState);
            Assert.That(reader.Remaining, Is.Zero, "The snapshot payload must be fully consumed.");
        }

        private static void DispatchDamage(RpgCharacterNetworkAdapter adapter, NetworkConnectionToClient sender,
            float amount)
        {
            DispatchCommand(adapter, sender,
                new RpgCharacterNetworkCommand(RpgCharacterNetworkCommandType.Damage, number: amount));
        }

        private static void DispatchCommand(RpgCharacterNetworkAdapter adapter, NetworkConnectionToClient sender,
            RpgCharacterNetworkCommand command)
        {
            // Mirror registers the Cecil full signature and omits the injected sender from the payload.
            const string commandSignature = "System.Void Neo.Rpg.Network.RpgCharacterNetworkAdapter::CmdExecute(" +
                                            "System.Int32,System.String,System.Single,System.Int32,System.Boolean," +
                                            "Mirror.NetworkConnectionToClient)";
            ushort commandHash = commandSignature.GetStableHashCode16();
            RemoteCallDelegate handler = RemoteProcedureCalls.GetDelegate(commandHash);
            Assert.That(handler, Is.Not.Null, "Mirror must weave and register the RPG command.");
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            // Use the registered int serializer selected by the same weaver as the receive delegate.
            writer.Write<int>((int)command.Type);
            writer.WriteString(command.Text);
            writer.WriteFloat(command.Number);
            writer.Write<int>(command.Integer);
            writer.WriteBool(command.Flag);
            using NetworkReaderPooled reader = NetworkReaderPool.Get(writer.ToArraySegment());
            handler(adapter, reader, sender);
            Assert.That(reader.Remaining, Is.Zero);
        }

        private void ConfigureSnapshotTemplate(BuffDefinition buff)
        {
            _template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgResourceId.Hp), startCurrent = 90f, startMax = 100f,
                    restoreToFull = false,
                    regen = new RpgRegenDefinition
                    {
                        enabled = true, mode = RpgRegenMode.PercentMaxPerSecond, value = 10f
                    }
                },
                new RpgResourceDefinition
                {
                    id = new RpgStatId("Mana"), startCurrent = 2f, startMax = 20f, restoreToFull = false
                }
            };
            _template.stats = new[]
            {
                new RpgStatDefinition { id = new RpgStatId("S"), baseValue = 10f },
                new RpgStatDefinition { id = new RpgStatId("Other"), baseValue = 3f }
            };
            _template.knownBuffs = new[] { buff };
        }

        private void ConfigureGrowthTemplate(RpgLevelGrowthMode mode, bool automatic)
        {
            RpgProgressionDefinition progression = ScriptableObject.CreateInstance<RpgProgressionDefinition>();
            _assets.Add(progression);
            progression.growthMode = mode;
            progression.autoApplyGrowthOnLevelUp = automatic;
            _template.progression = progression;
            _template.stats = new[]
            {
                new RpgStatDefinition
                {
                    id = new RpgStatId("S"), baseValue = 10f, affectedByLevel = true,
                    growth = new RpgStatGrowthRule { AddPerLevel = 2f }
                }
            };
        }

        private BuffDefinition CreateCurrentGrantBuff()
        {
            BuffDefinition buff = ScriptableObject.CreateInstance<BuffDefinition>();
            _assets.Add(buff);
            buff.name = "AuditRpgSnapshotGrant";
            BuffStatModifier max = new();
            SetAuthoringField(max, "_statType", BuffStatType.AddResourceMaxFlat);
            SetAuthoringField(max, "_targetId", new RpgStatId(RpgResourceId.Hp));
            SetAuthoringField(max, "_value", 50f);
            BuffStatModifier current = new();
            SetAuthoringField(current, "_statType", BuffStatType.AddResourceCurrentFlat);
            SetAuthoringField(current, "_targetId", new RpgStatId(RpgResourceId.Hp));
            SetAuthoringField(current, "_value", 40f);
            SetAuthoringField(buff, "_modifiers", new[] { max, current });
            return buff;
        }

        private static void SpawnCharacter(RpgCharacterNetworkAdapter adapter, uint assetId)
        {
            NetworkIdentity identity = adapter.GetComponent<NetworkIdentity>();
            Assert.That(adapter.netIdentity, Is.SameAs(identity));
            NetworkTestHelper.SetAssetId(identity, assetId);
            NetworkServer.Spawn(adapter.gameObject);
            Assert.That(adapter.isServer, Is.True);
            Assert.That(identity.netId, Is.Not.Zero);
        }

        private static void AssertRestoredSnapshot(RpgCharacter character)
        {
            Assert.That(character.CurrentHp, Is.EqualTo(130f));
            Assert.That(character.MaxHp, Is.EqualTo(160f));
            Assert.That(character.HpPercentValue, Is.EqualTo(0.8125f));
            Assert.That(character.Resources[RpgResourceId.Hp].ResolvedRegenPerSecond, Is.EqualTo(16f));
            Assert.That(character.Resources[RpgResourceId.Hp].BaseMax, Is.EqualTo(100f));
            Assert.That(character.GetResource("Mana"), Is.EqualTo(7f));
            Assert.That(character.GetResourceMax("Mana"), Is.EqualTo(25f));
            Assert.That(character.XpState.Value, Is.EqualTo(17f));
            Assert.That(character.Level, Is.EqualTo(3));
            Assert.That(character.UpgradePointsState.Value, Is.EqualTo(2));
            Assert.That(character.GetStat("S"), Is.EqualTo(42f));
            Assert.That(character.GetStat("Other"), Is.EqualTo(11f));
        }

        private static void SetAuthoringField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "The fixture configures an existing serialized authoring field.");
            field.SetValue(target, value);
        }
    }
#endif
}
