using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [Serializable]
    public class LeaderboardUser
    {
        public int num;
        public string name;
        public int score;
        public readonly string id;

        public LeaderboardUser(string name, int score = 0, int num = 0)
        {
            id = Guid.NewGuid().ToString();
            this.name = name;
            this.score = score;
            this.num = num;
        }
    }

    public class Leaderboard : Singleton<Leaderboard>
    {
        public Transform container;
        public int count = 345;
        public bool formatNum = true;
        public bool generateLeaderboardItemsOnValidate;
        public bool generateLeaderboardOnAwake = true;
        public LeaderboardItem leaderboardItemPlayer;
        public List<LeaderboardItem> leaderboardItems = new();
        public string[] names = { "nickname" };

        [Space] [Header("OtherSetting")] public bool onAwakeSort = true;

        [Space] public UnityEvent OnSort;
        public LeaderboardUser player = new("PlayerName");

        [Space] public LeaderboardItem prefab;

        [Space] public Vector2Int rangeScore = new(1, 3495);
        public string sep = "";

        [Space] public List<LeaderboardUser> sortUsers;
        public bool useNum = true;

        [Space] public List<LeaderboardUser> users = new();
        public bool useZero = true;

        private void Start()
        {
            if (generateLeaderboardOnAwake && prefab != null) GenerateLeaderboardItems();

            if (onAwakeSort)
                Sort();
        }

        public void UpdatePlayerScore(int score)
        {
            player.score = score;
            SyncPlayer();
            Sort();
        }

        public void UpdatePlayerName(string newName)
        {
            player.name = newName;
            SyncPlayer();
            Sort();
        }

        private void SyncPlayer()
        {
            var idx = users.FindIndex(u => u.id == player.id);
            if (idx >= 0)
            {
                users[idx].score = player.score;
                users[idx].name = player.name;
            }
            else
            {
                users.Add(player);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            if (users.Count > count) users = users.Take(count).ToList();

            if (users.Count < count)
            {
                GenerateUserList();
                Sort();
            }

            if (generateLeaderboardItemsOnValidate && prefab != null)
            {
                GenerateLeaderboardItems();
                Sort();
            }
        }

        private void GenerateLeaderboardItems()
        {
            if (sortUsers == null || sortUsers.Count == 0)
                Sort();

            var countAdd = sortUsers.Count - leaderboardItems.Count;

            for (var i = 0; i < countAdd; i++)
            {
                var obj = Instantiate(prefab, container);
                leaderboardItems.Add(obj);
            }
        }

        public void GenerateUserList()
        {
            users.Clear();

            for (var i = 0; i < count - 1; i++)
            {
                var score = rangeScore.RandomRange();
                var num = useNum ? formatNum ? FormatText(i + 1) : (i + 1).ToString() : "";
                users.Add(new LeaderboardUser(names.GetRandomElement() + sep + num, score));
            }

            if (!users.Exists(u => u.id == player.id)) users.Add(player);
        }

        public void Sort()
        {
            sortUsers = users.OrderByDescending(x => x.score).ToList();

            SetItems();

            OnSort?.Invoke();
        }

        private void SetItems()
        {
            for (var i = 0; i < leaderboardItems.Count && i < sortUsers.Count; i++)
            {
                sortUsers[i].num = i;
                leaderboardItems[i].Set(sortUsers[i], sortUsers[i].id == player.id);
            }

            if (leaderboardItemPlayer != null) leaderboardItemPlayer.Set(sortUsers[GetIdPlayer()], true);
        }

        public string FormatText(int num)
        {
            if (useZero)
            {
                var digitsCount = count.ToString().Length;
                return num.ToString("D" + digitsCount);
            }

            return num.ToString();
        }

        public int GetId(LeaderboardUser user)
        {
            return sortUsers.IndexOf(user);
        }

        public int GetIdPlayer()
        {
            return sortUsers.FindIndex(x => x.id == player.id);
        }
    }
}