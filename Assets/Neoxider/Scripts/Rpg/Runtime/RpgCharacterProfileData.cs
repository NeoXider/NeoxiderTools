using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg.Runtime
{
    /// <summary>
    ///     Universal save payload for <c>RpgCharacter</c>: any number of resources, stats,
    ///     and upgrades - not just the legacy HP / MaxHp / Level fields.
    ///     <para>Supersedes the old RPG profile payload.</para>
    /// </summary>
    [Serializable]
    public sealed class RpgCharacterProfileData
    {
        public const int CurrentVersion = 2;

        [SerializeField] private int _version = CurrentVersion;
        [SerializeField] private int _level = 1;
        [SerializeField] private float _xp;
        [SerializeField] private int _upgradePoints;
        [SerializeField] private bool _isDead;
        [SerializeField] private int _invulnerabilityLocks;

        [SerializeField] private List<RpgResourceSaveEntry> _resources = new();
        [SerializeField] private List<RpgStatSaveEntry> _stats = new();
        [SerializeField] private List<RpgUpgradeSaveEntry> _upgrades = new();
        [SerializeField] private List<ActiveBuffEntry> _activeBuffs = new();
        [SerializeField] private List<ActiveStatusEntry> _activeStatuses = new();

        public int Version { get => _version; set => _version = value; }
        public int Level { get => _level; set => _level = Mathf.Max(1, value); }
        public float Xp { get => _xp; set => _xp = Mathf.Max(0f, value); }
        public int UpgradePoints { get => _upgradePoints; set => _upgradePoints = Mathf.Max(0, value); }
        public bool IsDead { get => _isDead; set => _isDead = value; }
        public int InvulnerabilityLocks
        {
            get => _invulnerabilityLocks;
            set => _invulnerabilityLocks = Mathf.Max(0, value);
        }

        public List<RpgResourceSaveEntry> Resources => _resources;
        public List<RpgStatSaveEntry> Stats => _stats;
        public List<RpgUpgradeSaveEntry> Upgrades => _upgrades;
        public List<ActiveBuffEntry> ActiveBuffs => _activeBuffs;
        public List<ActiveStatusEntry> ActiveStatuses => _activeStatuses;

        public void Sanitize()
        {
            _version = CurrentVersion;
            _level = Mathf.Max(1, _level);
            _xp = Mathf.Max(0f, _xp);
            _upgradePoints = Mathf.Max(0, _upgradePoints);
            _invulnerabilityLocks = Mathf.Max(0, _invulnerabilityLocks);

            _resources ??= new List<RpgResourceSaveEntry>();
            _stats ??= new List<RpgStatSaveEntry>();
            _upgrades ??= new List<RpgUpgradeSaveEntry>();
            _activeBuffs ??= new List<ActiveBuffEntry>();
            _activeStatuses ??= new List<ActiveStatusEntry>();

            for (int i = _resources.Count - 1; i >= 0; i--)
            {
                if (_resources[i] == null || string.IsNullOrWhiteSpace(_resources[i].Id))
                {
                    _resources.RemoveAt(i);
                }
            }

            for (int i = _stats.Count - 1; i >= 0; i--)
            {
                if (_stats[i] == null || string.IsNullOrWhiteSpace(_stats[i].Id))
                {
                    _stats.RemoveAt(i);
                }
            }

            for (int i = _upgrades.Count - 1; i >= 0; i--)
            {
                if (_upgrades[i] == null || string.IsNullOrWhiteSpace(_upgrades[i].StatId))
                {
                    _upgrades.RemoveAt(i);
                }
            }

            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (_activeBuffs[i] == null || string.IsNullOrWhiteSpace(_activeBuffs[i].BuffId))
                {
                    _activeBuffs.RemoveAt(i);
                }
            }

            for (int i = _activeStatuses.Count - 1; i >= 0; i--)
            {
                if (_activeStatuses[i] == null || string.IsNullOrWhiteSpace(_activeStatuses[i].StatusId))
                {
                    _activeStatuses.RemoveAt(i);
                }
            }
        }

        public RpgCharacterProfileData Clone()
        {
            RpgCharacterProfileData copy = new()
            {
                _version = _version,
                _level = _level,
                _xp = _xp,
                _upgradePoints = _upgradePoints,
                _isDead = _isDead,
                _invulnerabilityLocks = _invulnerabilityLocks
            };
            for (int i = 0; i < (_resources?.Count ?? 0); i++)
            {
                RpgResourceSaveEntry e = _resources[i];
                if (e != null)
                {
                    copy._resources.Add(new RpgResourceSaveEntry { Id = e.Id, Current = e.Current, Max = e.Max });
                }
            }

            for (int i = 0; i < (_stats?.Count ?? 0); i++)
            {
                RpgStatSaveEntry e = _stats[i];
                if (e != null)
                {
                    copy._stats.Add(new RpgStatSaveEntry { Id = e.Id, Base = e.Base });
                }
            }

            for (int i = 0; i < (_upgrades?.Count ?? 0); i++)
            {
                RpgUpgradeSaveEntry e = _upgrades[i];
                if (e != null)
                {
                    copy._upgrades.Add(new RpgUpgradeSaveEntry { StatId = e.StatId, Count = e.Count });
                }
            }

            for (int i = 0; i < (_activeBuffs?.Count ?? 0); i++)
            {
                ActiveBuffEntry e = _activeBuffs[i];
                if (e != null)
                {
                    copy._activeBuffs.Add(new ActiveBuffEntry
                    {
                        BuffId = e.BuffId, ExpiresAtUtc = e.ExpiresAtUtc, Stacks = e.Stacks
                    });
                }
            }

            for (int i = 0; i < (_activeStatuses?.Count ?? 0); i++)
            {
                ActiveStatusEntry e = _activeStatuses[i];
                if (e != null)
                {
                    copy._activeStatuses.Add(new ActiveStatusEntry
                    {
                        StatusId = e.StatusId, ExpiresAtUtc = e.ExpiresAtUtc, Stacks = e.Stacks
                    });
                }
            }

            return copy;
        }
    }

    /// <summary>One v2 resource row: effective Max is authoritative during restoration only;
    /// BaseMax remains definition data for later derived refreshes. Duplicate IDs restore last-row-wins.</summary>
    [Serializable]
    public sealed class RpgResourceSaveEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private float _current;
        [SerializeField] private float _max;

        public string Id { get => _id; set => _id = value ?? string.Empty; }
        public float Current { get => _current; set => _current = value; }
        public float Max { get => _max; set => _max = value; }
    }

    /// <summary>One row in the stats section of the save payload.</summary>
    [Serializable]
    public sealed class RpgStatSaveEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private float _base;

        public string Id { get => _id; set => _id = value ?? string.Empty; }
        public float Base { get => _base; set => _base = value; }
    }

    /// <summary>Number of upgrade points invested in a given stat (Dark-Souls flow).</summary>
    [Serializable]
    public sealed class RpgUpgradeSaveEntry
    {
        [SerializeField] private string _statId;
        [SerializeField] private int _count;

        public string StatId { get => _statId; set => _statId = value ?? string.Empty; }
        public int Count { get => _count; set => _count = Mathf.Max(0, value); }
    }
}
