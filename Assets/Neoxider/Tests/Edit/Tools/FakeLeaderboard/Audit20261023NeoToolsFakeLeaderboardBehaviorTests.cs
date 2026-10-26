using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests.Tools
{
    public sealed class Audit20261023NeoToolsFakeLeaderboardBehaviorTests
    {
        private GameObject _root;
        private Leaderboard _leaderboard;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _root = new GameObject(nameof(Audit20261023NeoToolsFakeLeaderboardBehaviorTests));
            _leaderboard = _root.AddComponent<Leaderboard>();
            _leaderboard.playerSaveKey = string.Empty;
            _leaderboard.player = new LeaderboardUser("Player");
            _leaderboard.users = new List<LeaderboardUser>();
            _leaderboard.sortUsers = null;
            _leaderboard.leaderboardItems = new List<LeaderboardItem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Random.state = _randomState;
        }

        [TestCase(SortOrder.Ascending, 12)]
        [TestCase(SortOrder.Ascending, -12)]
        [TestCase(SortOrder.Descending, -12)]
        public void UpdatePlayerScore_AcceptsFirstNonzeroResult(SortOrder order, int score)
        {
            _leaderboard.sortOrder = order;

            _leaderboard.UpdatePlayerScore(score);

            Assert.That(_leaderboard.player.score, Is.EqualTo(score));
            Assert.That(_leaderboard.sortUsers, Is.EqualTo(new[] { _leaderboard.player }));
            Assert.That(_leaderboard.GetIdPlayer(), Is.Zero);
        }

        [TestCase(SortOrder.Ascending, 12)]
        [TestCase(SortOrder.Descending, -12)]
        public void UpdatePlayerScore_ZeroRequiresExplicitOverride(SortOrder order, int score)
        {
            _leaderboard.sortOrder = order;
            _leaderboard.player.score = score;

            _leaderboard.UpdatePlayerScore(0);
            Assert.That(_leaderboard.player.score, Is.EqualTo(score));

            _leaderboard.UpdatePlayerScore(0, true);
            Assert.That(_leaderboard.player.score, Is.Zero);
            Assert.That(_leaderboard.sortUsers[0], Is.SameAs(_leaderboard.player));
        }

        [Test]
        public void UpdatePlayerScore_SameNamedBotIsNotOverwrittenOrSelectedAsPlayer()
        {
            LeaderboardUser bot = new LeaderboardUser("Player", 100);
            _leaderboard.users.Add(bot);

            _leaderboard.UpdatePlayerScore(25);

            Assert.That(bot.score, Is.EqualTo(100));
            Assert.That(_leaderboard.users, Has.Count.EqualTo(2));
            Assert.That(_leaderboard.sortUsers[_leaderboard.GetIdPlayer()], Is.SameAs(_leaderboard.player));
            Assert.That(_leaderboard.GetIdPlayer(), Is.EqualTo(1));
        }

        [Test]
        public void UpdatePlayerScore_DeserializedMissingIdsDoNotIdentifyAnotherUser()
        {
            LeaderboardUser bot = JsonUtility.FromJson<LeaderboardUser>("{\"name\":\"Player\",\"score\":100}");
            _leaderboard.player = JsonUtility.FromJson<LeaderboardUser>("{\"name\":\"Player\",\"score\":5}");
            _leaderboard.users.Add(bot);
            _leaderboard.users.Add(_leaderboard.player);

            _leaderboard.UpdatePlayerScore(25);

            Assert.That(bot.score, Is.EqualTo(100));
            Assert.That(_leaderboard.users, Has.Count.EqualTo(2));
            Assert.That(_leaderboard.users[1], Is.SameAs(_leaderboard.player));
            Assert.That(_leaderboard.GetIdPlayer(), Is.EqualTo(1));
        }

        [TestCase(SortOrder.Ascending)]
        [TestCase(SortOrder.Descending)]
        public void Sort_ExtremeResultsBeatZeroAndAllRanksExistWithoutViews(SortOrder order)
        {
            LeaderboardUser zero = new LeaderboardUser("Unscored");
            LeaderboardUser minimum = new LeaderboardUser("Minimum", int.MinValue);
            LeaderboardUser maximum = new LeaderboardUser("Maximum", int.MaxValue);
            _leaderboard.users.AddRange(new[] { zero, minimum, maximum, null });
            _leaderboard.sortOrder = order;

            _leaderboard.Sort();

            LeaderboardUser[] expected = order == SortOrder.Ascending
                ? new[] { minimum, maximum, zero }
                : new[] { maximum, minimum, zero };
            CollectionAssert.AreEqual(expected, _leaderboard.sortUsers);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(expected[i].num, Is.EqualTo(i));
            }
        }

        [Test]
        public void Sort_DedicatedPlayerRankAndHighlightDoNotDependOnVisibleRowCount()
        {
            LeaderboardUser bot = new LeaderboardUser("Player", 100);
            _leaderboard.player.score = 10;
            _leaderboard.users.AddRange(new[] { bot, _leaderboard.player });
            LeaderboardItem firstRow = CreateRow("First");
            _leaderboard.leaderboardItems.Add(firstRow);
            LeaderboardItem playerRow = CreateRow("DedicatedPlayer");
            _leaderboard.leaderboardItemPlayer = playerRow;
            playerRow.textId = CreateText("Rank");
            bool highlighted = true;
            firstRow.OnUser = new UnityEvent<bool>();
            firstRow.OnUser.AddListener(value => highlighted = value);

            _leaderboard.Sort();

            Assert.That(firstRow.user, Is.SameAs(bot));
            Assert.That(highlighted, Is.False);
            Assert.That(playerRow.user, Is.SameAs(_leaderboard.player));
            Assert.That(playerRow.isPlayer, Is.True);
            Assert.That(playerRow.textId.text, Is.EqualTo("2"));
        }

        [Test]
        public void Sort_ShrinkingAndGrowingListUpdatesRowVisibilityAndSkipsMissingRows()
        {
            LeaderboardItem first = CreateRow("First");
            LeaderboardItem second = CreateRow("Second");
            _leaderboard.leaderboardItems.AddRange(new[] { first, second, null });
            LeaderboardUser high = new LeaderboardUser("High", 100);
            LeaderboardUser low = new LeaderboardUser("Low", 10);
            _leaderboard.users.AddRange(new[] { high, low, _leaderboard.player });
            _leaderboard.Sort();
            Assert.That(second.user, Is.SameAs(low));

            _leaderboard.users.RemoveRange(1, 2);
            _leaderboard.Sort();
            Assert.That(first.gameObject.activeSelf, Is.True);
            Assert.That(second.gameObject.activeSelf, Is.False);

            _leaderboard.users.Add(_leaderboard.player);
            _leaderboard.Sort();
            Assert.That(second.gameObject.activeSelf, Is.True);
            Assert.That(second.user, Is.SameAs(_leaderboard.player));
        }

        [TestCase(0, false)]
        [TestCase(-4, false)]
        [TestCase(3, false)]
        [TestCase(3, true)]
        public void GenerateUserList_NormalizesCountAndSupportsEmptyNameSources(int count, bool nullNames)
        {
            _leaderboard.count = count;
            _leaderboard.names = nullNames ? null : Array.Empty<string>();
            _leaderboard.rangeScore = new Vector2Int(10, 11);

            _leaderboard.GenerateUserList();

            Assert.That(_leaderboard.count, Is.EqualTo(Math.Max(1, count)));
            Assert.That(_leaderboard.users, Has.Count.EqualTo(_leaderboard.count));
            Assert.That(_leaderboard.users.FindAll(user => ReferenceEquals(user, _leaderboard.player)),
                Has.Count.EqualTo(1));
            if (count > 1)
            {
                Assert.That(_leaderboard.users[0].name, Does.StartWith("nickname"));
                Assert.That(_leaderboard.users[0].score, Is.EqualTo(10));
            }
        }

        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void FormatScore_GroupingUsesSpacesAcrossCultures(string culture)
        {
            CultureInfo previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                _leaderboard.formatScore = true;

                Assert.That(_leaderboard.FormatScore(1234567), Is.EqualTo("1 234 567"));
                Assert.That(_leaderboard.FormatScore(int.MinValue), Is.EqualTo("-2 147 483 648"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Test]
        public void UpdatePlayerName_StagesNameWithoutRequiringAnotherScoreUpdate()
        {
            ISaveProvider original = SaveProvider.CurrentProvider;
            Audit20261023NeoToolsFakeLeaderboardSaveProvider provider =
                new Audit20261023NeoToolsFakeLeaderboardSaveProvider();
            try
            {
                SaveProvider.SetProvider(provider);
                _leaderboard.playerSaveKey = "Audit20261023NeoToolsFakeLeaderboard";
                _leaderboard.player.score = 42;

                _leaderboard.UpdatePlayerName("Renamed");

                Assert.That(provider.GetString(_leaderboard.playerSaveKey + "_Name"), Is.EqualTo("Renamed"));
                Assert.That(provider.GetInt(_leaderboard.playerSaveKey + "_Score"), Is.EqualTo(42));
                Assert.That(_leaderboard.sortUsers[0].name, Is.EqualTo("Renamed"));
            }
            finally
            {
                SaveProvider.SetProvider(original);
            }
        }

        [Test]
        public void ItemSet_ScoreOnlyViewStillBindsStateAndEmitsPlayerEvents()
        {
            LeaderboardItem row = CreateRow("ScoreOnly");
            row.textScore = CreateText("Score");
            row.OnUser = new UnityEvent<bool>();
            List<bool> highlights = new List<bool>();
            row.OnUser.AddListener(highlights.Add);
            _leaderboard.noScoreText = "No result";
            _leaderboard.formatScore = true;

            row.Set(_leaderboard.player, true, _leaderboard);
            Assert.That(row.textScore.text, Is.EqualTo("No result"));

            LeaderboardUser bot = new LeaderboardUser("Bot", 1234, 7);
            row.Set(bot, false, _leaderboard);
            Assert.That(row.user, Is.SameAs(bot));
            Assert.That(row.id, Is.EqualTo(7));
            Assert.That(row.textScore.text, Is.EqualTo("1 234"));
            CollectionAssert.AreEqual(new[] { true, false }, highlights);
        }

        private LeaderboardItem CreateRow(string name)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(_root.transform);
            return row.AddComponent<LeaderboardItem>();
        }

        private TMP_Text CreateText(string name)
        {
            GameObject label = new GameObject(name);
            label.transform.SetParent(_root.transform);
            return label.AddComponent<TextMeshPro>();
        }
    }

    internal sealed class Audit20261023NeoToolsFakeLeaderboardSaveProvider : ISaveProvider
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
        public event Action OnDataSaved;
        public event Action OnDataLoaded;
        public event Action<string> OnKeyChanged;

        public int GetInt(string key, int defaultValue = 0) =>
            _values.TryGetValue(key, out object value) && value is int result ? result : defaultValue;

        public float GetFloat(string key, float defaultValue = 0f) =>
            _values.TryGetValue(key, out object value) && value is float result ? result : defaultValue;

        public string GetString(string key, string defaultValue = "") =>
            _values.TryGetValue(key, out object value) && value is string result ? result : defaultValue;

        public bool GetBool(string key, bool defaultValue = false) =>
            _values.TryGetValue(key, out object value) && value is bool result ? result : defaultValue;

        public void SetInt(string key, int value) => Set(key, value);
        public void SetFloat(string key, float value) => Set(key, value);
        public void SetString(string key, string value) => Set(key, value);
        public void SetBool(string key, bool value) => Set(key, value);
        public bool HasKey(string key) => _values.ContainsKey(key);
        public void DeleteKey(string key) => _values.Remove(key);
        public void DeleteAll() => _values.Clear();
        public void Save() => OnDataSaved?.Invoke();
        public void Load() => OnDataLoaded?.Invoke();

        private void Set(string key, object value)
        {
            _values[key] = value;
            OnKeyChanged?.Invoke(key);
        }
    }
}
