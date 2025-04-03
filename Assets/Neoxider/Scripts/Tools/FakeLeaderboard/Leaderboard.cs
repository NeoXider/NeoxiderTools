using System;
using System.Collections.Generic;
using System.Linq;
using Neo;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class LeaderboardUser
{
    public string name;
    public int score;
    public string id;

    public LeaderboardUser(string name, int score = 0)
    {
        this.id = Guid.NewGuid().ToString();
        this.name = name;
        this.score = score;
    }
}

public class Leaderboard : Neo.Tools.Singleton<Leaderboard>
{
    public LeaderboardUser player = new LeaderboardUser("PlayerName");

    [Space]
    public Vector2Int rangeScore = new Vector2Int(1, 3495);
    public int count = 345;
    public bool useZero = true;
    public string[] names = { "nickname" };
    public string sep = "";
    public bool useNum = true;
    public bool formatNum = true;

    [Space]
    [Header("OtherSetting")]
    public bool onAwakeSort = true;

    [Space]
    public List<LeaderboardUser> users = new List<LeaderboardUser>();

    [Space]
    public List<LeaderboardUser> sortUsers;

    [Space]
    public LeaderboardItem prefab;
    public bool generateLeaderboardOnAwake = true;
    public bool generateLeaderboardItemsOnValidate;
    public Transform container;
    public List<LeaderboardItem> leaderboardItems = new List<LeaderboardItem>();

    [Space]
    public UnityEvent OnSort;

    private void Start()
    {
        if (generateLeaderboardOnAwake && prefab != null)
        {
            GenerateLeaderboardItems();
        }

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
        int idx = users.FindIndex(u => u.id == player.id);
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

        if(users.Count > count)
        {
            users = users.Take(count).ToList();
        }

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

        int countAdd = sortUsers.Count - leaderboardItems.Count;

        for (int i = 0; i < countAdd; i++)
        {
            var obj = GameObject.Instantiate(prefab, container);
            leaderboardItems.Add(obj);
        }
    }

    public void GenerateUserList()
    {
        users.Clear();

        for (int i = 0; i < count - 1; i++)
        {
            int score = rangeScore.RandomRange();
            string num = useNum ? (formatNum ? FormatText(i+1) : (i+1).ToString()) : "";
            users.Add(new LeaderboardUser(names.GetRandomElement() + sep + num, score));
        }

        if (!users.Exists(u => u.id == player.id))
        {
            users.Add(player);
        }
    }

    public void Sort()
    {
        sortUsers = users.OrderByDescending(x => x.score).ToList();

        SetItems();

        OnSort?.Invoke();
    }

    private void SetItems()
    {
        for (int i = 0; i < leaderboardItems.Count && i < sortUsers.Count; i++)
        {
            leaderboardItems[i].Set(sortUsers[i], i, sortUsers[i].id == player.id);
        }
    }

    public string FormatText(int num)
    {
        if (useZero)
        {
            int digitsCount = count.ToString().Length;
            return num.ToString("D" + digitsCount);
        }
        else
        {
            return num.ToString();
        }
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
