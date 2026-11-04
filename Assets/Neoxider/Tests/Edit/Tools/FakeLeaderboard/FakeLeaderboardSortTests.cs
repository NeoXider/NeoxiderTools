using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    public sealed class FakeLeaderboardSortTests
    {
        private GameObject _root;
        private Leaderboard _leaderboard;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(FakeLeaderboardSortTests));
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
        }

        [TestCase(SortOrder.Descending)]
        [TestCase(SortOrder.Ascending)]
        public void EqualScores_KeepSourceOrderAcrossManyRows(SortOrder order)
        {
            _leaderboard.sortOrder = order;
            List<LeaderboardUser> tied = new();
            for (int i = 0; i < 64; i++)
            {
                LeaderboardUser user = new("Bot" + i, 500);
                tied.Add(user);
                _leaderboard.users.Add(user);
            }

            LeaderboardUser best = new("Best", order == SortOrder.Descending ? 900 : 100);
            _leaderboard.users.Insert(10, best);
            _leaderboard.users.Add(_leaderboard.player);

            _leaderboard.Sort();

            Assert.That(_leaderboard.sortUsers[0], Is.SameAs(best));
            for (int i = 0; i < tied.Count; i++)
            {
                Assert.That(_leaderboard.sortUsers[i + 1], Is.SameAs(tied[i]), "ties must keep source order");
                Assert.That(tied[i].num, Is.EqualTo(i + 1));
            }

            Assert.That(_leaderboard.sortUsers[_leaderboard.sortUsers.Count - 1], Is.SameAs(_leaderboard.player),
                "the unscored player ranks after every scored row");
        }

        [Test]
        public void Sort_LeavesThePreviousSnapshotUnchanged()
        {
            LeaderboardUser bot = new("Bot", 10);
            _leaderboard.users.Add(bot);
            _leaderboard.Sort();
            List<LeaderboardUser> snapshot = _leaderboard.sortUsers;
            LeaderboardUser[] before = snapshot.ToArray();

            _leaderboard.UpdatePlayerScore(50);

            Assert.That(_leaderboard.sortUsers, Is.Not.SameAs(snapshot));
            Assert.That(snapshot, Is.EqualTo(before));
            Assert.That(_leaderboard.GetIdPlayer(), Is.Zero);
        }
    }
}
