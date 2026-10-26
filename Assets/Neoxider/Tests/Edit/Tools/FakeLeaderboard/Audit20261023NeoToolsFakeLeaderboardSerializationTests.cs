using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests.Tools
{
    public sealed class Audit20261023NeoToolsFakeLeaderboardSerializationTests
    {
        private GameObject _root;
        private Leaderboard _board;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _root = new GameObject(nameof(Audit20261023NeoToolsFakeLeaderboardSerializationTests));
            _root.SetActive(false);
            _board = _root.AddComponent<Leaderboard>();
            _board.playerSaveKey = string.Empty;
            _board.generateLeaderboardOnAwake = false;
            _board.generateLeaderboardItemsOnValidate = false;
            _board.onAwakeSort = false;
            _board.player = new LeaderboardUser("Shared", 20);
            _board.users = new List<LeaderboardUser>();
            _board.sortUsers = null;
            _board.leaderboardItems = new List<LeaderboardItem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Random.state = _randomState;
        }

        [Test]
        public void WholeAggregateJsonRoundTrip_RebindsPlayerAndRebuildsDerivedSnapshot()
        {
            LeaderboardUser player = _board.player;
            LeaderboardUser bot = new LeaderboardUser("Shared", 100);
            string key = player.PersistentId;
            Audit20261023NeoToolsFakeLeaderboardAggregate original =
                new Audit20261023NeoToolsFakeLeaderboardAggregate
                {
                    player = player,
                    users = new List<LeaderboardUser> { bot, player },
                    sortUsers = new List<LeaderboardUser> { player }
                };

            Audit20261023NeoToolsFakeLeaderboardAggregate restored =
                JsonUtility.FromJson<Audit20261023NeoToolsFakeLeaderboardAggregate>(JsonUtility.ToJson(original));
            Assert.That(restored.player, Is.Not.SameAs(restored.users[1]), "The aggregate must exercise inline copies.");
            _board.player = restored.player;
            _board.users = restored.users;
            _board.sortUsers = restored.sortUsers;
            _board.Sort();
            _board.UpdatePlayerName("Renamed");
            _board.UpdatePlayerScore(30);

            Assert.That(_board.player.PersistentId, Is.EqualTo(key));
            Assert.That(_board.users, Has.Count.EqualTo(2));
            Assert.That(_board.users[1], Is.SameAs(_board.player));
            Assert.That(_board.users[0].name, Is.EqualTo("Shared"));
            Assert.That(_board.users[0].score, Is.EqualTo(100));
            Assert.That(_board.player.name, Is.EqualTo("Renamed"));
            Assert.That(_board.player.score, Is.EqualTo(30));
            Assert.That(_board.sortUsers, Has.Count.EqualTo(2));
            Assert.That(_board.sortUsers[_board.GetIdPlayer()], Is.SameAs(_board.player));
            Assert.That(_board.IsPlayerIdentityResolved, Is.True);
            Assert.That(_board.HasDuplicatePlayerKey, Is.False);
        }

        [Test]
        public void Sort_CanonicalReferenceWinsOverEarlierPersistentCopy()
        {
            LeaderboardUser copy = Copy(_board.player);
            copy.score = 100;
            _board.users.AddRange(new[] { copy, _board.player });
            LeaderboardItem first = CreateRow();
            LeaderboardItem second = CreateRow();
            _board.leaderboardItems.AddRange(new[] { first, second });

            _board.Sort();

            Assert.That(_board.users[0], Is.SameAs(copy));
            Assert.That(_board.users[0].score, Is.EqualTo(100));
            Assert.That(_board.users[1], Is.SameAs(_board.player));
            Assert.That(first.isPlayer, Is.False);
            Assert.That(second.isPlayer, Is.True);
            Assert.That(_board.GetIdPlayer(), Is.EqualTo(1));
            Assert.That(_board.HasDuplicatePlayerKey, Is.True);
        }

        [Test]
        public void DuplicateKeys_ExplicitRepairPreservesOtherRowsAndSurvivesAggregateReload()
        {
            string originalKey = _board.player.PersistentId;
            string legacyRuntimeId = _board.player.id;
            LeaderboardUser first = Copy(_board.player);
            LeaderboardUser second = Copy(_board.player);
            first.score = 100;
            second.score = 50;
            _board.users.AddRange(new[] { first, second });

            _board.Sort();

            Assert.That(_board.IsPlayerIdentityResolved, Is.False);
            Assert.That(_board.HasDuplicatePlayerKey, Is.True);
            Assert.That(_board.GetIdPlayer(), Is.EqualTo(-1));
            Assert.That(_board.users[0], Is.SameAs(first));
            Assert.That(_board.users[1], Is.SameAs(second));

            _board.UpdatePlayerScore(30);
            Assert.That(_board.users, Has.Count.EqualTo(3), "Unresolved rows must survive synchronization.");
            Assert.That(first.score, Is.EqualTo(100));
            Assert.That(second.score, Is.EqualTo(50));
            Assert.That(_board.sortUsers.FindAll(user => ReferenceEquals(user, _board.player)), Has.Count.EqualTo(1));

            LeaderboardUser previousCanonical = _board.player;
            Assert.That(_board.TryBindPlayerEntry(0), Is.True);

            Assert.That(_board.player.PersistentId, Is.Not.Empty.And.Not.EqualTo(originalKey));
            Assert.That(_board.player.id, Is.EqualTo(legacyRuntimeId));
            Assert.That(_board.player.score, Is.EqualTo(30));
            Assert.That(_board.users, Has.Count.EqualTo(3));
            Assert.That(_board.users[0], Is.SameAs(_board.player));
            Assert.That(_board.users[1], Is.SameAs(second));
            Assert.That(second.PersistentId, Is.EqualTo(originalKey));
            Assert.That(second.score, Is.EqualTo(50));
            Assert.That(_board.users[2], Is.SameAs(previousCanonical));
            Assert.That(previousCanonical.PersistentId, Is.EqualTo(originalKey));
            Assert.That(previousCanonical.score, Is.EqualTo(30));
            Assert.That(_board.sortUsers, Has.Count.EqualTo(3));
            Assert.That(_board.HasDuplicatePlayerKey, Is.False);

            Audit20261023NeoToolsFakeLeaderboardAggregate repaired =
                new Audit20261023NeoToolsFakeLeaderboardAggregate { player = _board.player, users = _board.users };
            Audit20261023NeoToolsFakeLeaderboardAggregate restored =
                JsonUtility.FromJson<Audit20261023NeoToolsFakeLeaderboardAggregate>(JsonUtility.ToJson(repaired));
            _board.player = restored.player;
            _board.users = restored.users;
            _board.Sort();
            Assert.That(_board.users, Has.Count.EqualTo(3));
            Assert.That(_board.users[0], Is.SameAs(_board.player));
            Assert.That(_board.HasDuplicatePlayerKey, Is.False);
        }

        [Test]
        public void Sort_KnownCanonicalAliasesRankOnceWithoutRemovingSourceSlots()
        {
            _board.users.AddRange(new[] { _board.player, _board.player });

            _board.Sort();

            Assert.That(_board.users, Has.Count.EqualTo(2));
            Assert.That(_board.sortUsers, Is.EqualTo(new[] { _board.player }));
            Assert.That(_board.GetIdPlayer(), Is.Zero);
            Assert.That(_board.HasDuplicatePlayerKey, Is.False);

            LeaderboardUser detachedAlias = Copy(_board.player);
            _board.users = new List<LeaderboardUser> { detachedAlias, detachedAlias };
            _board.Sort();
            Assert.That(_board.users, Has.Count.EqualTo(2));
            Assert.That(_board.users[0], Is.SameAs(_board.player));
            Assert.That(_board.users[1], Is.SameAs(_board.player));
            Assert.That(_board.sortUsers, Has.Count.EqualTo(1));
            Assert.That(_board.HasDuplicatePlayerKey, Is.False);
        }

        [Test]
        public void KeylessSameNameAndScore_DoNotInferIdentityAndCanBeBoundExplicitly()
        {
            const string json = "{\"name\":\"Shared\",\"score\":20,\"num\":0}";
            _board.player = JsonUtility.FromJson<LeaderboardUser>(json);
            LeaderboardUser independent = JsonUtility.FromJson<LeaderboardUser>(json);
            _board.users.Add(independent);

            _board.Sort();

            Assert.That(_board.player.PersistentId, Is.Null.Or.Empty);
            Assert.That(_board.GetIdPlayer(), Is.EqualTo(-1));
            Assert.That(_board.users[0], Is.SameAs(independent));
            Assert.That(_board.IsPlayerIdentityResolved, Is.False);

            Assert.That(_board.TryBindPlayerEntry(0), Is.True);
            Assert.That(_board.users, Has.Count.EqualTo(1));
            Assert.That(_board.users[0], Is.SameAs(_board.player));
            Assert.That(_board.player.PersistentId, Is.Not.Null.And.Not.Empty);
            Assert.That(_board.player.score, Is.EqualTo(20));
            Assert.That(_board.GetIdPlayer(), Is.Zero);
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void InvalidBinding_LeavesSourceIdentitySnapshotAndEventsUntouched(int index)
        {
            LeaderboardUser independent = new LeaderboardUser("Other", 100);
            _board.users.Add(independent);
            _board.Sort();
            List<LeaderboardUser> snapshot = _board.sortUsers;
            string key = _board.player.PersistentId;
            int events = 0;
            _board.OnSort = new UnityEvent();
            _board.OnSort.AddListener(() => events++);

            Assert.That(_board.TryBindPlayerEntry(index), Is.False);

            Assert.That(_board.users, Has.Count.EqualTo(1));
            Assert.That(_board.users[0], Is.SameAs(independent));
            Assert.That(_board.player.PersistentId, Is.EqualTo(key));
            Assert.That(_board.sortUsers, Is.SameAs(snapshot));
            Assert.That(events, Is.Zero);
        }

        [UnityTest]
        public IEnumerator InspectorResize_DoesNotRegenerateAmbiguousSourceRows()
        {
            LeaderboardUser first = Copy(_board.player);
            LeaderboardUser second = Copy(_board.player);
            _board.users.AddRange(new[] { first, second });
            _board.count = 2;
            int refreshes = 0;
            _board.OnSort = new UnityEvent();
            _board.OnSort.AddListener(() => refreshes++);
            using (SerializedObject serialized = new SerializedObject(_board))
            {
                serialized.FindProperty(nameof(Leaderboard.count)).intValue = 5;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            yield return null;

            Assert.That(refreshes, Is.GreaterThan(0), "Exercise real Inspector validation dispatch.");
            Assert.That(_board.users, Has.Count.EqualTo(2));
            Assert.That(_board.users[0].PersistentId, Is.EqualTo(first.PersistentId));
            Assert.That(_board.users[1].PersistentId, Is.EqualTo(second.PersistentId));
            Assert.That(_board.HasDuplicatePlayerKey, Is.True);
            Assert.That(_board.GetIdPlayer(), Is.EqualTo(-1));
        }

        [Test]
        public void LegacyRawJson_ReadsWritableIdTokensAndIgnoresDerivedSnapshot()
        {
            const string json = "{\"player\":{\"id\":\"player-key\",\"name\":\"Shared\",\"score\":20}," +
                                "\"users\":[{\"id\":\"bot-key\",\"name\":\"Shared\",\"score\":100}," +
                                "{\"id\":\"player-key\",\"name\":\"Old\",\"score\":999}]," +
                                "\"sortUsers\":[{\"id\":\"player-key\",\"score\":999}]}";

            Assert.That(LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser player,
                out List<LeaderboardUser> users), Is.True);
            _board.player = player;
            _board.users = users;
            _board.Sort();

            Assert.That(player.PersistentId, Is.EqualTo("player-key"));
            Assert.That(users[1], Is.SameAs(player));
            Assert.That(users[0].PersistentId, Is.EqualTo("bot-key"));
            Assert.That(users[0].score, Is.EqualTo(100));
            Assert.That(player.score, Is.EqualTo(20));
            Assert.That(player.name, Is.EqualTo("Shared"));
            Assert.That(_board.sortUsers, Has.Count.EqualTo(2));
            Assert.That(_board.GetIdPlayer(), Is.EqualTo(1));
        }

        [Test]
        public void LegacyRawJson_KeylessRowsStayIndependent()
        {
            const string json = "{\"player\":{\"name\":\"Shared\",\"score\":20}," +
                                "\"users\":[{\"name\":\"Shared\",\"score\":20}]}";

            Assert.That(LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser player,
                out List<LeaderboardUser> users), Is.True);

            Assert.That(player.PersistentId, Is.Null.Or.Empty);
            Assert.That(users, Has.Count.EqualTo(1));
            Assert.That(users[0], Is.Not.SameAs(player));
            _board.player = player;
            _board.users = users;
            _board.Sort();
            Assert.That(_board.GetIdPlayer(), Is.EqualTo(-1));
        }

        [TestCase("")]
        [TestCase("not-json")]
        [TestCase("{\"player\":{\"id\":\"p\"}}")]
        [TestCase("{\"player\":{\"id\":\"p\"},\"users\":[{\"id\":\"p\"},{\"id\":\"p\"}]}")]
        [TestCase("{\"player\":{\"id\":\"p\"},\"metadata\":{\"users\":[]}}")]
        [TestCase("{\"player\":{\"id\":\"p\",\"users\":[]}}")]
        [TestCase("{\"player\":{\"id\":\"p\",\"name\":\"\\\"users\\\":[]\"}}")]
        [TestCase("{\"player\":{},\"users\":null}")]
        [TestCase("{\"player\":{},\"users\":\"[]\"}")]
        [TestCase("{\"player\":{},\"users\":{}}")]
        [TestCase("{\"player\":{},\"users\":7}")]
        [TestCase("{\"player\":{},\"users\":true}")]
        [TestCase("{\"player\":{},\"Users\":[]}")]
        [TestCase("[{\"player\":{},\"users\":[]}]")]
        [TestCase("{\"player\":{},\"users\":[],\"users\":[]}")]
        [TestCase("{\"player\":{},\"users\":[}")]
        [TestCase("{\"player\":{},\"users\":[]}garbage")]
        [TestCase("{\"player\":{},\"users\":[],\"metadata\":{\"open\":[}")]
        [TestCase("{\"player\":{},\"users\":[],\"\\u0075sers\":[]}")]
        [TestCase("{\"player\":{},\"\\u0075sers\":[],\"users\":[]}")]
        [TestCase("{\"player\":{},\"\\u0075sers\":null}")]
        [TestCase("{\"player\":{},\"\\u0075sers\":\"[]\"}")]
        [TestCase("{\"player\":{},\"\\u0075sers\":{}}")]
        [TestCase("{\"player\":{},\"\\u0075sers\":7}")]
        [TestCase("{\"player\":{},\"\\u0075sers\":true}")]
        [TestCase("{\"player\":{},\"metadata\":{\"\\u0075sers\":[]}}")]
        [TestCase("{\"player\":{},\"\\u0055sers\":[]}")]
        [TestCase("{\"player\":{\"name\":\"\\u0075sers\"},\"usersX\":[]}")]
        [TestCase("{\"player\":{},\"users key\":[],\"namespace:users\":[]}")]
        [TestCase("{\"player\":{\"id\":\"p\"},\"users\":[{\"\\u0069d\":\"p\"},{\"id\":\"p\"}]}")]
        [TestCase("{\"player\":null,\"users\":[]}")]
        [TestCase("{\"users\":[]}")]
        [TestCase("{\"player\":{\"score\":2147483648},\"users\":[]}")]
        [TestCase("{\"player\":{\"score\":\"12\"},\"users\":[]}")]
        [TestCase("{\"player\":{},\"users\":[{\"id\":\"first\",\"score\":12},{\"id\":{}}]}")]
        [TestCase("{\"player\":{},\"_x0075_sers\":[]}")]
        [TestCase("{\"player\":{},\"\\\\u0075sers\":[]}")]
        [TestCase("{\"player\":{},\"\\u0075sers key\":[]}")]
        public void LegacyRawJson_RejectedAggregatePublishesNoPartialOutput(string json)
        {
            const string valid = "{\"player\":{\"id\":\"p\"},\"users\":[]}";
            Assert.That(LeaderboardLegacyJson.TryDeserialize(valid, out LeaderboardUser validPlayer,
                out List<LeaderboardUser> validUsers), Is.True, "Establish native empty-array support before rejection.");
            Assert.That(validPlayer.PersistentId, Is.EqualTo("p"));
            Assert.That(validUsers, Is.Not.Null.And.Empty);

            Assert.That(LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser player,
                out List<LeaderboardUser> users), Is.False);

            Assert.That(player, Is.Null);
            Assert.That(users, Is.Null);
        }

        [TestCase("{\"player\":{\"id\":\"p\"},\"users\":[]}", "p", null, 0, 0)]
        [TestCase("{\"users\":[],\"player\":{}}", null, null, 0, 0)]
        [TestCase("{\"player\":{\"name\":\"Player\",\"score\":12,\"num\":7},\"users\":[]}", null, "Player", 12, 7)]
        [TestCase(" \r\n{\"users\":[],\"player\":{\"id\":\"p\"}}\t", "p", null, 0, 0)]
        [TestCase("{\"metadata\":{\"users\":[{}]},\"player\":{\"id\":\"p\"},\"users\":[]}", "p", null, 0, 0)]
        [TestCase("{\"\\u0070layer\":{},\"\\u0075sers\":[]}", null, null, 0, 0)]
        [TestCase("{\"player\":{\"id\":null,\"name\":null},\"users\":[]}", null, null, 0, 0)]
        public void LegacyRawJson_ExplicitEmptyArrayPreservesOptionalPlayerData(string json, string id,
            string name, int score, int num)
        {
            Assert.That(LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser player,
                out List<LeaderboardUser> users), Is.True);

            Assert.That(users, Is.Not.Null.And.Empty);
            Assert.That(player, Is.Not.Null);
            if (id == null)
            {
                Assert.That(player.PersistentId, Is.Null.Or.Empty);
            }
            else
            {
                Assert.That(player.PersistentId, Is.EqualTo(id));
            }

            if (name == null)
            {
                Assert.That(player.name, Is.Null.Or.Empty);
            }
            else
            {
                Assert.That(player.name, Is.EqualTo(name));
            }
            Assert.That(player.score, Is.EqualTo(score));
            Assert.That(player.num, Is.EqualTo(num));
        }

        [Test]
        public void LegacyRawJson_EscapedRootArrayKeyPreservesUserPayload()
        {
            const string json = "{\"player\":{\"id\":\"p\"},\"\\u0075sers\":[" +
                                "{\"id\":\"bot\",\"name\":\"Other\",\"score\":12,\"num\":7}]}";

            Assert.That(LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser player,
                out List<LeaderboardUser> users), Is.True);

            Assert.That(player.PersistentId, Is.EqualTo("p"));
            Assert.That(users, Has.Count.EqualTo(1));
            Assert.That(users[0].PersistentId, Is.EqualTo("bot"));
            Assert.That(users[0].name, Is.EqualTo("Other"));
            Assert.That(users[0].score, Is.EqualTo(12));
            Assert.That(users[0].num, Is.EqualTo(7));
            Assert.That(users[0], Is.Not.SameAs(player));
        }

        [Test]
        public void LegacyRawJson_EscapedDtoNamesPreserveValuesAndCanonicalOwnership()
        {
            const string json = "{\"\\u0070layer\":{\"\\u0069d\":\"p\\u002dkey\",\"\\u006eame\":\"Player\\n\\u00c9\"," +
                                "\"\\u0073core\":-2147483648,\"\\u006eum\":7},\"\\u0075sers\":[" +
                                "{\"\\u0069d\":\"bot\\u002dkey\",\"\\u006eame\":\"Other \\\\u0075sers \\\"users\\\":[]\"," +
                                "\"\\u0073core\":2147483647,\"\\u006eum\":-3},null," +
                                "{\"id\":\"p-key\",\"name\":\"Stale\",\"score\":99,\"num\":99}]," +
                                "\"users key\":{\"id\":\"custom\"},\"namespace:users\":[{}]," +
                                "\"sortUsers\":[{\"id\":\"derived\"}],\"metadata\":{\"users\":[{}]}}";

            Assert.That(LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser player,
                out List<LeaderboardUser> users), Is.True);

            Assert.That(player.PersistentId, Is.EqualTo("p-key"));
            Assert.That(player.name, Is.EqualTo("Player\n\u00c9"));
            Assert.That(player.score, Is.EqualTo(int.MinValue));
            Assert.That(player.num, Is.EqualTo(7));
            Assert.That(users, Has.Count.EqualTo(3));
            Assert.That(users[0], Is.Not.SameAs(player));
            Assert.That(users[0].PersistentId, Is.EqualTo("bot-key"));
            Assert.That(users[0].name, Is.EqualTo("Other \\u0075sers \"users\":[]"));
            Assert.That(users[0].score, Is.EqualTo(int.MaxValue));
            Assert.That(users[0].num, Is.EqualTo(-3));
            Assert.That(users[1], Is.Null);
            Assert.That(users[2], Is.SameAs(player));
        }

        [Test]
        public void LegacyRawJson_ReaderMemberMappingReportsExactDecodedNames()
        {
            const string json = "{\"users\":[],\"\\u0075sers\":[],\"us\\u0065rs\":[]," +
                                "\"\\u0070layer\":{},\"\\u0069d\":\"p\",\"\\u006eame\":\"n\"," +
                                "\"\\u0073core\":20,\"\\u006eum\":7,\"users key\":[]," +
                                "\"namespace:users\":[],\"\\\\u0075sers\":[],\"Users\":[],\"_x0075_sers\":[]}";
            string[] expectedNames =
            {
                "users", "users", "users", "player", "id", "name", "score", "num",
                "users key", "namespace:users", "\\u0075sers", "Users", "_x0075_sers"
            };
            byte[] utf8 = Encoding.UTF8.GetBytes(json);
            using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(utf8,
                       XmlDictionaryReaderQuotas.Max))
            {
                Type readerType = reader.GetType();
                TestContext.WriteLine("JSON reader: " + readerType.FullName + "; assembly=" +
                                      readerType.Assembly.FullName + "; MVID=" + readerType.Module.ModuleVersionId);
                Assert.That(reader.MoveToContent(), Is.EqualTo(XmlNodeType.Element));
                Assert.That(reader.GetAttribute("type"), Is.EqualTo("object"));
                reader.ReadStartElement();
                for (int i = 0; i < expectedNames.Length; i++)
                {
                    Assert.That(reader.MoveToContent(), Is.EqualTo(XmlNodeType.Element), "Member " + i);
                    string localName = reader.LocalName;
                    string namespaceUri = reader.NamespaceURI;
                    string mappedName = reader.GetAttribute("item", string.Empty);
                    string tuple = "JSON member " + i + ": LocalName=[" + localName + "], NamespaceURI=[" +
                                   namespaceUri + "], item=[" + (mappedName ?? "<null>") + "]";
                    TestContext.WriteLine(tuple);
                    if (namespaceUri.Length == 0)
                    {
                        Assert.That(localName, Is.EqualTo(expectedNames[i]), tuple);
                        Assert.That(mappedName, Is.Null, tuple);
                    }
                    else
                    {
                        Assert.That(localName, Is.EqualTo("item"), tuple);
                        Assert.That(namespaceUri, Is.EqualTo("item"), tuple);
                        Assert.That(mappedName, Is.EqualTo(expectedNames[i]), tuple);
                    }

                    reader.Skip();
                }

                Assert.That(reader.MoveToContent(), Is.EqualTo(XmlNodeType.EndElement));
                reader.ReadEndElement();
                Assert.That(reader.MoveToContent(), Is.EqualTo(XmlNodeType.None));
            }
        }

        [Test]
        public void PrefabSaveReload_PreservesPersistentIdentityAndCanonicalPlayerValues()
        {
            _board.count = 2;
            LeaderboardUser bot = new LeaderboardUser("Shared", 100);
            string key = _board.player.PersistentId;
            _board.users.AddRange(new[] { bot, _board.player });
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Audit20261023NeoToolsFakeLeaderboard.prefab");
            GameObject contents = null;
            try
            {
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(_root, path);
                Assert.That(saved, Is.Not.Null);
                contents = PrefabUtility.LoadPrefabContents(path);
                Leaderboard restored = contents.GetComponent<Leaderboard>();
                restored.Sort();
                restored.UpdatePlayerName("Renamed");
                restored.UpdatePlayerScore(30);

                Assert.That(restored.player.PersistentId, Is.EqualTo(key));
                Assert.That(restored.users, Has.Count.EqualTo(2));
                Assert.That(restored.users[1], Is.SameAs(restored.player));
                Assert.That(restored.users[0].name, Is.EqualTo("Shared"));
                Assert.That(restored.users[0].score, Is.EqualTo(100));
                Assert.That(restored.player.name, Is.EqualTo("Renamed"));
                Assert.That(restored.player.score, Is.EqualTo(30));
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void AuthoredLegacyPrefab_RecoversOldYamlIdWithoutRewritingAsset()
        {
            string path = AssetDatabase.GUIDToAssetPath("433a7e0b84016c148888c749f1f360e1");
            Assert.That(path, Is.Not.Empty, "The packaged FakeLeaderboard prefab must be discoverable.");
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Leaderboard restored = contents.GetComponent<Leaderboard>();
                Assert.That(restored.player.PersistentId, Is.EqualTo("405a428d-b02a-4479-a1c1-eeeb5adf44e8"));
                int count = restored.users.Count;
                restored.Sort();

                Assert.That(restored.users, Has.Count.EqualTo(count));
                Assert.That(restored.users.FindAll(user => ReferenceEquals(user, restored.player)), Has.Count.EqualTo(1));
                Assert.That(restored.GetIdPlayer(), Is.GreaterThanOrEqualTo(0));
                Assert.That(restored.HasDuplicatePlayerKey, Is.False);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static LeaderboardUser Copy(LeaderboardUser user)
        {
            return JsonUtility.FromJson<LeaderboardUser>(JsonUtility.ToJson(user));
        }

        private LeaderboardItem CreateRow()
        {
            GameObject row = new GameObject("IdentityRow");
            row.transform.SetParent(_root.transform);
            return row.AddComponent<LeaderboardItem>();
        }
    }

    [Serializable]
    internal sealed class Audit20261023NeoToolsFakeLeaderboardAggregate
    {
        public LeaderboardUser player;
        public List<LeaderboardUser> users;
        public List<LeaderboardUser> sortUsers;
    }
}
