using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
using Neo.Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    public enum SortOrder
    {
        Descending,
        Ascending
    }

    [Serializable]
    public class LeaderboardUser : ISerializationCallbackReceiver
    {
        public int num;
        public string name;
        public int score;
        public readonly string id;

        [SerializeField] [FormerlySerializedAs("id")]
        private string _persistentId;

        /// <summary>Durable identity for authored/serialized users. May be empty in keyless legacy data.</summary>
        public string PersistentId => _persistentId;

        public LeaderboardUser(string name, int score = 0, int num = 0)
        {
            id = Guid.NewGuid().ToString();
            _persistentId = id;
            this.name = name;
            this.score = score;
            this.num = num;
        }

        internal LeaderboardUser(LeaderboardUser source)
        {
            id = source.id;
            _persistentId = source._persistentId;
            name = source.name;
            score = source.score;
            num = source.num;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            CopyLegacyRuntimeId();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            CopyLegacyRuntimeId();
        }

        private void CopyLegacyRuntimeId()
        {
            if (string.IsNullOrEmpty(_persistentId) && !string.IsNullOrEmpty(id))
            {
                _persistentId = id;
            }
        }

        internal void SetPersistentId(string persistentId)
        {
            _persistentId = persistentId;
        }

        internal void InitializePersistentId(bool renew = false)
        {
            if (renew || string.IsNullOrEmpty(_persistentId))
            {
                _persistentId = !renew && !string.IsNullOrEmpty(id) ? id : Guid.NewGuid().ToString();
            }
        }
    }

    [CreateFromMenu("Neoxider/Tools/FakeLeaderboard/Leaderboard")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(Leaderboard))]
    [NeoDoc("Tools/FakeLeaderboard/Leaderboard.md")]
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

        [Space] [Header("Sorting")] [Tooltip("Sort direction: descending (high to low) or ascending (low to high)")]
        public SortOrder sortOrder = SortOrder.Descending;

        [Space] [Header("Score Formatting")] [Tooltip("Whether to format score (add separators)")]
        public bool formatScore;

        [Space] [Header("Time Formatting")] [Tooltip("Use time formatting for score display")]
        public bool useTimeFormat;

        [Tooltip("Time format for score display")]
        public TimeFormat timeFormat = TimeFormat.Seconds;

        [Tooltip("Separator for time format")] public string timeSeparator = ":";

        [Space] [Header("Save Settings")] [Tooltip("Key for saving player data (change for different leaderboards)")]
        public string playerSaveKey = "LeaderboardPlayer";

        [Space] [Header("Player Score Display")] [Tooltip("Text shown when player has no score (default: --)")]
        public string noScoreText = "--";

        /// <summary>Whether the source list has the canonical reference or one unambiguous persistent match.</summary>
        public bool IsPlayerIdentityResolved => FindPlayerSourceIndex() >= 0;

        /// <summary>Whether distinct source users share the player's nonempty persistent key.</summary>
        public bool HasDuplicatePlayerKey
        {
            get
            {
                FindUniquePersistentPlayerIndex(out bool ambiguous);
                return ambiguous;
            }
        }

        private void Start()
        {
            player ??= new LeaderboardUser("PlayerName");
            LoadPlayerData();
            if (generateLeaderboardOnAwake && (users == null || users.Count == 0))
            {
                GenerateUserList();
            }
            else
            {
                SyncPlayer();
            }

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

            count = Mathf.Max(1, count);
            users ??= new List<LeaderboardUser>();
            player ??= new LeaderboardUser("PlayerName");

            // WHY: Loading ambiguous/keyless authored data must not regenerate or truncate its rows.
            if (users.Count > 0 && (!IsPlayerIdentityResolved || HasDuplicatePlayerKey))
            {
                Sort();
                return;
            }

            CanonicalizePlayer();

            if (users.Count > count)
            {
                users = users.Where(user => user != null && !ReferenceEquals(user, player)).Take(count - 1).ToList();
                SyncPlayer();
                Sort();
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

        private bool IsBetterScore(int newScore, int currentScore)
        {
            if (newScore == currentScore || newScore == 0)
            {
                return false;
            }

            if (currentScore == 0)
            {
                return true;
            }

            return sortOrder == SortOrder.Descending
                ? newScore > currentScore
                : newScore < currentScore;
        }

        /// <summary>
        ///     Updates the player score, optionally keeping only the best result.
        /// </summary>
        /// <param name="score">New player score.</param>
        /// <param name="overrideBestScore">
        ///     If false (default), a nonzero score updates an unscored player or improves the current result
        ///     per <see cref="sortOrder" />. Zero means no result.
        ///     If true, score always updates.
        /// </param>
        public void UpdatePlayerScore(int score, bool overrideBestScore = false)
        {
            player ??= new LeaderboardUser("PlayerName");
            if (!overrideBestScore && !IsBetterScore(score, player.score))
            {
                return;
            }

            player.score = score;
            SyncPlayer();
            SavePlayerData();
            Sort();
        }

        public void UpdatePlayerName(string newName)
        {
            player ??= new LeaderboardUser("PlayerName");
            player.name = newName;
            SyncPlayer();
            SavePlayerData();
            Sort();
        }

        private int FindPlayerSourceIndex()
        {
            if (player == null || users == null)
            {
                return -1;
            }

            int referenceIndex = users.FindIndex(user => ReferenceEquals(user, player));
            return referenceIndex >= 0 ? referenceIndex : FindUniquePersistentPlayerIndex(out bool ambiguous);
        }

        private int FindUniquePersistentPlayerIndex(out bool ambiguous)
        {
            ambiguous = false;
            if (player == null || users == null || string.IsNullOrEmpty(player.PersistentId))
            {
                return -1;
            }

            int matchIndex = -1;
            LeaderboardUser match = null;
            for (int i = 0; i < users.Count; i++)
            {
                LeaderboardUser candidate = users[i];
                if (candidate == null || candidate.PersistentId != player.PersistentId)
                {
                    continue;
                }

                if (match != null && !ReferenceEquals(match, candidate))
                {
                    ambiguous = true;
                    return -1;
                }

                if (matchIndex < 0)
                {
                    match = candidate;
                    matchIndex = i;
                }
            }

            return matchIndex;
        }

        private bool CanonicalizePlayer()
        {
            int index = FindPlayerSourceIndex();
            if (index < 0)
            {
                return false;
            }

            LeaderboardUser recognized = users[index];
            if (!ReferenceEquals(recognized, player))
            {
                for (int i = 0; i < users.Count; i++)
                {
                    if (ReferenceEquals(users[i], recognized))
                    {
                        users[i] = player;
                    }
                }
            }

            return true;
        }

        private void SyncPlayer()
        {
            users ??= new List<LeaderboardUser>();
            if (!CanonicalizePlayer())
            {
                users.Add(player);
            }
        }

        /// <summary>
        ///     Explicitly binds a non-null source slot to the canonical player, preserving every other slot
        ///     and the player's current name/score. If another slot already holds player, a new canonical
        ///     copy preserves that other row unchanged. Repairs a colliding or missing key, then sorts.
        ///     Invalid indices, missing source data or a null player return false without writes or events.
        /// </summary>
        public bool TryBindPlayerEntry(int sourceIndex)
        {
            if (player == null || users == null || sourceIndex < 0 || sourceIndex >= users.Count ||
                users[sourceIndex] == null)
            {
                return false;
            }

            LeaderboardUser boundPlayer = player;
            for (int i = 0; i < users.Count; i++)
            {
                if (i != sourceIndex && ReferenceEquals(users[i], player))
                {
                    // WHY: Do not change another row's key through an alias when intentionally repairing this slot.
                    boundPlayer = new LeaderboardUser(player);
                    break;
                }
            }

            bool renewKey = !ReferenceEquals(boundPlayer, player);
            for (int i = 0; i < users.Count; i++)
            {
                LeaderboardUser candidate = users[i];
                if (i != sourceIndex && candidate != null && !ReferenceEquals(candidate, boundPlayer) &&
                    !string.IsNullOrEmpty(boundPlayer.PersistentId) && candidate.PersistentId == boundPlayer.PersistentId)
                {
                    renewKey = true;
                    break;
                }
            }

            boundPlayer.InitializePersistentId(renewKey);
            player = boundPlayer;
            users[sourceIndex] = boundPlayer;
            Sort();
            return true;
        }

        private void GenerateLeaderboardItems()
        {
            leaderboardItems ??= new List<LeaderboardItem>();
            leaderboardItems.RemoveAll(item => item == null);

            if (container != null)
            {
                LeaderboardItem[] sceneItems = container.GetComponentsInChildren<LeaderboardItem>(true);
                foreach (LeaderboardItem item in sceneItems)
                {
                    if (item != leaderboardItemPlayer && !leaderboardItems.Contains(item))
                    {
                        // WHY: Only scene instances (not prefab assets)
                        // gameObject.scene.IsValid() is true for objects in a loaded scene
                        if (item.gameObject.scene.IsValid())
                        {
                            item.gameObject.SetActive(false);
                        }
                    }
                }
            }

            int countAdd = users.Count(user => user != null) - leaderboardItems.Count;

            for (int i = 0; i < countAdd; i++)
            {
                LeaderboardItem obj = Instantiate(prefab, container);
                obj.gameObject.SetActive(true);
                leaderboardItems.Add(obj);
            }

            Sort();
        }

        public void GenerateUserList()
        {
            count = Mathf.Max(1, count);
            users ??= new List<LeaderboardUser>();
            player ??= new LeaderboardUser("PlayerName");
            player.InitializePersistentId();
            users.Clear();

            for (int i = 0; i < count - 1; i++)
            {
                int score = rangeScore.RandomRange();
                string num = useNum ? formatNum ? FormatText(i + 1) : (i + 1).ToString() : "";
                string name = names != null && names.Length > 0 ? names.GetRandomElement() : "nickname";
                users.Add(new LeaderboardUser((name ?? "nickname") + sep + num, score));
            }

            users.Add(player);
        }

        public void Sort()
        {
            users ??= new List<LeaderboardUser>();
            CanonicalizePlayer();
            int playerSourceIndex = player != null ? users.FindIndex(user => ReferenceEquals(user, player)) : -1;
            // WHY: Repeated canonical references are known aliases. Preserve source slots but rank the user once.
            IEnumerable<LeaderboardUser> rankableUsers = users.Where((user, index) => user != null &&
                (!ReferenceEquals(user, player) || index == playerSourceIndex));
            if (sortOrder == SortOrder.Descending)
            {
                sortUsers = rankableUsers.OrderBy(x => x.score == 0)
                    .ThenByDescending(x => x.score).ToList();
            }
            else
            {
                sortUsers = rankableUsers.OrderBy(x => x.score == 0)
                    .ThenBy(x => x.score).ToList();
            }

            for (int i = 0; i < sortUsers.Count; i++)
            {
                sortUsers[i].num = i;
            }

            SetItems();

            OnSort?.Invoke();
        }

        private void SetItems()
        {
            leaderboardItems ??= new List<LeaderboardItem>();
            for (int i = 0; i < leaderboardItems.Count; i++)
            {
                LeaderboardItem item = leaderboardItems[i];
                if (item == null)
                {
                    continue;
                }

                bool hasUser = i < sortUsers.Count;
                item.gameObject.SetActive(hasUser);
                if (hasUser)
                {
                    item.Set(sortUsers[i], ReferenceEquals(sortUsers[i], player), this);
                }
            }

            if (leaderboardItemPlayer != null)
            {
                int playerId = GetIdPlayer();
                leaderboardItemPlayer.gameObject.SetActive(playerId >= 0);
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
            return sortUsers?.IndexOf(user) ?? -1;
        }

        public int GetIdPlayer()
        {
            return player != null ? sortUsers?.FindIndex(user => ReferenceEquals(user, player)) ?? -1 : -1;
        }

        private void SavePlayerData()
        {
            if (string.IsNullOrEmpty(playerSaveKey))
            {
                return;
            }

            SaveProvider.SetString($"{playerSaveKey}_Name", player.name);
            SaveProvider.SetInt($"{playerSaveKey}_Score", player.score);
        }

        private void LoadPlayerData()
        {
            if (string.IsNullOrEmpty(playerSaveKey))
            {
                return;
            }

            if (SaveProvider.HasKey($"{playerSaveKey}_Name"))
            {
                player.name = SaveProvider.GetString($"{playerSaveKey}_Name", "PlayerName");
            }

            if (SaveProvider.HasKey($"{playerSaveKey}_Score"))
            {
                player.score = SaveProvider.GetInt($"{playerSaveKey}_Score");
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
                return score.FormatWithSeparator(" ");
            }

            return score.ToString();
        }
    }
}
