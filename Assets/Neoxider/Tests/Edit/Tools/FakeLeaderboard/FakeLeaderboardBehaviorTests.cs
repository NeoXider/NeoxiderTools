using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    public sealed class FakeLeaderboardBehaviorTests
    {
        [Test]
        public void GenerateUserList_CreatesConfiguredCountAndIncludesPlayerExactlyOnce()
        {
            GameObject gameObject = new GameObject("FakeLeaderboardBehaviorTests");

            try
            {
                Leaderboard leaderboard = gameObject.AddComponent<Leaderboard>();
                leaderboard.count = 4;
                leaderboard.names = new[] { "Bot" };
                leaderboard.rangeScore = new Vector2Int(10, 11);
                leaderboard.player = new LeaderboardUser("Player", 25);

                leaderboard.GenerateUserList();

                Assert.That(leaderboard.users, Has.Count.EqualTo(4));
                Assert.That(leaderboard.users.FindAll(user => user.id == leaderboard.player.id), Has.Count.EqualTo(1));
                Assert.That(leaderboard.users.FindAll(user => user != leaderboard.player)
                    .TrueForAll(user => user.score == 10), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Sort_UsesConfiguredDirectionAndAlwaysPlacesZeroScoreLast()
        {
            GameObject gameObject = new GameObject("FakeLeaderboardSortBehaviorTests");

            try
            {
                Leaderboard leaderboard = gameObject.AddComponent<Leaderboard>();
                LeaderboardUser zero = new LeaderboardUser("No score", 0);
                LeaderboardUser low = new LeaderboardUser("Low", 10);
                LeaderboardUser high = new LeaderboardUser("High", 30);
                leaderboard.users = new List<LeaderboardUser> { low, zero, high };

                leaderboard.sortOrder = SortOrder.Descending;
                leaderboard.Sort();
                CollectionAssert.AreEqual(new[] { high, low, zero }, leaderboard.sortUsers);

                leaderboard.sortOrder = SortOrder.Ascending;
                leaderboard.Sort();
                CollectionAssert.AreEqual(new[] { low, high, zero }, leaderboard.sortUsers);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void UpdatePlayerScore_KeepsBestScoreUnlessOverrideIsRequested()
        {
            GameObject gameObject = new GameObject("FakeLeaderboardScoreBehaviorTests");

            try
            {
                Leaderboard leaderboard = gameObject.AddComponent<Leaderboard>();
                leaderboard.playerSaveKey = string.Empty;
                leaderboard.sortOrder = SortOrder.Descending;
                leaderboard.player = new LeaderboardUser("Player", 100);
                leaderboard.users = new List<LeaderboardUser> { leaderboard.player };

                leaderboard.UpdatePlayerScore(90);
                Assert.That(leaderboard.player.score, Is.EqualTo(100));

                leaderboard.UpdatePlayerScore(120);
                Assert.That(leaderboard.player.score, Is.EqualTo(120));
                Assert.That(leaderboard.sortUsers[0].score, Is.EqualTo(120));

                leaderboard.UpdatePlayerScore(5, true);
                Assert.That(leaderboard.player.score, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FormattingContracts_ReturnStableRawAndPaddedValues()
        {
            GameObject gameObject = new GameObject("FakeLeaderboardFormattingBehaviorTests");

            try
            {
                Leaderboard leaderboard = gameObject.AddComponent<Leaderboard>();
                leaderboard.count = 345;
                leaderboard.useZero = true;
                leaderboard.formatScore = false;
                leaderboard.useTimeFormat = false;

                Assert.That(leaderboard.FormatText(7), Is.EqualTo("007"));
                Assert.That(leaderboard.FormatScore(-42), Is.EqualTo("-42"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
