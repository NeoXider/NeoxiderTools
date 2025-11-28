using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    public enum SortOrder
    {
        Descending,
        Ascending
    }

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

        [Space] [Header("Sorting")] 
        [Tooltip("Направление сортировки: по убыванию (больше к меньше) или по возрастанию (меньше к больше)")]
        public SortOrder sortOrder = SortOrder.Descending;

        [Space] [Header("Score Formatting")]
        [Tooltip("Форматировать ли счет (добавлять разделители)")]
        public bool formatScore = false;

        [Space] [Header("Time Formatting")]
        [Tooltip("Использовать ли форматирование времени для счета")]
        public bool useTimeFormat = false;

        [Tooltip("Формат времени для отображения счета")]
        public TimeFormat timeFormat = TimeFormat.Seconds;

        [Tooltip("Разделитель для формата времени")]
        public string timeSeparator = ":";

        [Space] [Header("Save Settings")]
        [Tooltip("Ключ для сохранения данных игрока (можно изменить для разных лидербордов)")]
        public string playerSaveKey = "LeaderboardPlayer";

        [Space] [Header("Player Score Display")]
        [Tooltip("Текст, отображаемый если у игрока нет счета (по умолчанию: --)")]
        public string noScoreText = "--";

        private void Start()
        {
            LoadPlayerData();

            if (generateLeaderboardOnAwake && prefab != null)
            {
                GenerateLeaderboardItems();
            }

            if (onAwakeSort)
            {
                Sort();
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (users.Count > count)
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

        public void UpdatePlayerScore(int score)
        {
            player.score = score;
            SyncPlayer();
            SavePlayerData();
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
            // Сначала ищем по id
            int idx = users.FindIndex(u => u.id == player.id);
            
            // Если не нашли по id, ищем по имени (на случай если id изменился после загрузки)
            if (idx < 0)
            {
                idx = users.FindIndex(u => u.name == player.name);
            }
            
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

        private void GenerateLeaderboardItems()
        {
            if (sortUsers == null || sortUsers.Count == 0)
            {
                Sort();
            }

            // Выключаем объекты на сцене, которые не находятся в списке
            if (container != null)
            {
                LeaderboardItem[] sceneItems = container.GetComponentsInChildren<LeaderboardItem>(true);
                foreach (LeaderboardItem item in sceneItems)
                {
                    if (!leaderboardItems.Contains(item))
                    {
                        // Проверяем, является ли объект объектом на сцене (не префабом)
                        // gameObject.scene.IsValid() возвращает true только для объектов на сцене
                        if (item.gameObject.scene.IsValid())
                        {
                            item.gameObject.SetActive(false);
                        }
                    }
                }
            }

            int countAdd = sortUsers.Count - leaderboardItems.Count;

            for (int i = 0; i < countAdd; i++)
            {
                LeaderboardItem obj = Instantiate(prefab, container);
                obj.gameObject.SetActive(true);
                leaderboardItems.Add(obj);
            }
        }

        public void GenerateUserList()
        {
            users.Clear();

            for (int i = 0; i < count - 1; i++)
            {
                int score = rangeScore.RandomRange();
                string num = useNum ? formatNum ? FormatText(i + 1) : (i + 1).ToString() : "";
                users.Add(new LeaderboardUser(names.GetRandomElement() + sep + num, score));
            }

            if (!users.Exists(u => u.id == player.id))
            {
                users.Add(player);
            }
        }

        public void Sort()
        {
            if (sortOrder == SortOrder.Descending)
            {
                // При сортировке по убыванию: сначала по счету, затем все с 0 счетом в конце
                sortUsers = users.OrderByDescending(x => x.score)
                    .ThenBy(x => x.score == 0 ? 1 : 0) // Все с 0 счетом в конце
                    .ToList();
            }
            else
            {
                sortUsers = users.OrderBy(x => x.score).ToList();
            }

            SetItems();

            OnSort?.Invoke();
        }

        private void SetItems()
        {
            for (int i = 0; i < leaderboardItems.Count && i < sortUsers.Count; i++)
            {
                sortUsers[i].num = i;
                // Проверяем по id или по имени (на случай если id не совпадает после загрузки)
                bool isPlayerItem = sortUsers[i].id == player.id || sortUsers[i].name == player.name;
                leaderboardItems[i].Set(sortUsers[i], isPlayerItem, this);
            }

            if (leaderboardItemPlayer != null)
            {
                int playerId = GetIdPlayer();
                if (playerId >= 0 && playerId < sortUsers.Count)
                {
                    leaderboardItemPlayer.Set(sortUsers[playerId], true, this);
                }
            }
        }

        public string FormatText(int num)
        {
            if (useZero)
            {
                int digitsCount = count.ToString().Length;
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

        private void SavePlayerData()
        {
            if (string.IsNullOrEmpty(playerSaveKey))
            {
                return;
            }

            PlayerPrefs.SetString($"{playerSaveKey}_Name", player.name);
            PlayerPrefs.SetInt($"{playerSaveKey}_Score", player.score);
            PlayerPrefs.Save();
        }

        private void LoadPlayerData()
        {
            if (string.IsNullOrEmpty(playerSaveKey))
            {
                return;
            }

            if (PlayerPrefs.HasKey($"{playerSaveKey}_Name"))
            {
                player.name = PlayerPrefs.GetString($"{playerSaveKey}_Name", "PlayerName");
            }

            if (PlayerPrefs.HasKey($"{playerSaveKey}_Score"))
            {
                player.score = PlayerPrefs.GetInt($"{playerSaveKey}_Score", 0);
            }
        }

        public string FormatScore(int score)
        {
            if (useTimeFormat)
            {
                return ((float)score).FormatTime(timeFormat, timeSeparator);
            }

            if (formatScore)
            {
                return score.ToString("N0").Replace(",", " ");
            }

            return score.ToString();
        }
    }
}