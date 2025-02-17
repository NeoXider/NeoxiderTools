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

    public LeaderboardUser(string name, int score = 0)
    {
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

    [Space]
    [Header("OtherSetting")]
    public bool onAwakeSort = true;

    [Space]
    public List<LeaderboardUser> users = new List<LeaderboardUser>();

    [Space]
    public List<LeaderboardUser> sortUsers;

    [Space]
    public LeaderboardItem prefab;
    public bool generateLeaderboardItems;
    public Transform container;
    public List<LeaderboardItem> leaderboardItems = new List<LeaderboardItem>();

    [Space]
    public UnityEvent OnSort;

    private void Start()
    {
        if (onAwakeSort)
            Sort();
    }

    public void UpdatePlayer(int score)
    {
        player.score = score;
        Sort();
    }

    public void UpdatePlayerName(string newName)
    {
        player.name = newName;
        Sort();
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        if (users.Count < count)
        {
            GenerateUserList();
            Sort();
        }

        if (generateLeaderboardItems && prefab != null)
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

        for (int i = 0; i < count; i++)
        {
            int score = rangeScore.RandomRange();
            users.Add(new LeaderboardUser("PlayerName" + sep + FormatText(i), score));
        }

        if (!users.Contains(player))
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
            leaderboardItems[i].Set(sortUsers[i], i, sortUsers[i] == player);
        }
    }

    public string FormatText(int score)
    {
        if (useZero)
        {
            int digitsCount = count.ToString().Length;
            return score.ToString("D" + digitsCount);
        }
        else
        {
            return score.ToString();
        }
    }

    public int GetId(LeaderboardUser user)
    {
        return sortUsers.IndexOf(user);
    }

    public int GetIdPlayer()
    {
        return sortUsers.FindIndex(x => x == player);
    }
}
