using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Rpg
{
    internal static class RpgTimeUtility
    {
        internal static double GetCurrentUnixTimestamp() =>
            DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    /// <summary>
    /// Serializable RPG profile payload stored by the stats manager.
    /// </summary>
    [Serializable]
    public sealed class ActiveBuffEntry
    {
        [SerializeField] private string _buffId = string.Empty;
        [SerializeField] private double _expiresAtUtc;

        public string BuffId
        {
            get => _buffId;
            set => _buffId = value ?? string.Empty;
        }

        public double ExpiresAtUtc
        {
            get => _expiresAtUtc;
            set => _expiresAtUtc = value;
        }

        public double RemainingSeconds => Math.Max(0, _expiresAtUtc - RpgTimeUtility.GetCurrentUnixTimestamp());
    }

    /// <summary>
    /// Serializable active status effect entry.
    /// </summary>
    [Serializable]
    public sealed class ActiveStatusEntry
    {
        [SerializeField] private string _statusId = string.Empty;
        [SerializeField] private double _expiresAtUtc;
        [SerializeField] private int _stacks = 1;

        public string StatusId
        {
            get => _statusId;
            set => _statusId = value ?? string.Empty;
        }

        public double ExpiresAtUtc
        {
            get => _expiresAtUtc;
            set => _expiresAtUtc = value;
        }

        public int Stacks
        {
            get => _stacks;
            set => _stacks = Mathf.Max(1, value);
        }

        public double RemainingSeconds => Math.Max(0, _expiresAtUtc - RpgTimeUtility.GetCurrentUnixTimestamp());
    }

    /// <summary>
    /// Serializable RPG profile payload stored by the stats manager.
    /// </summary>
    [Serializable]
    public sealed class RpgProfileData
    {
        [SerializeField] private int _version = 1;
        [SerializeField] private float _currentHp = 100f;
        [SerializeField] private float _maxHp = 100f;
        [SerializeField] private int _level = 1;
        [SerializeField] private List<ActiveBuffEntry> _activeBuffs = new();
        [SerializeField] private List<ActiveStatusEntry> _activeStatusEffects = new();

        /// <summary>
        /// Gets or sets the serialized profile version.
        /// </summary>
        public int Version
        {
            get => _version;
            set => _version = value;
        }

        /// <summary>
        /// Gets or sets the current HP.
        /// </summary>
        public float CurrentHp
        {
            get => _currentHp;
            set => _currentHp = Mathf.Clamp(value, 0f, _maxHp);
        }

        /// <summary>
        /// Gets or sets the maximum HP.
        /// </summary>
        public float MaxHp
        {
            get => _maxHp;
            set => _maxHp = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Gets or sets the character level.
        /// </summary>
        public int Level
        {
            get => _level;
            set => _level = Mathf.Max(1, value);
        }

        /// <summary>
        /// Gets the list of active buff entries.
        /// </summary>
        public List<ActiveBuffEntry> ActiveBuffs => _activeBuffs;

        /// <summary>
        /// Gets the list of active status effect entries.
        /// </summary>
        public List<ActiveStatusEntry> ActiveStatusEffects => _activeStatusEffects;

        /// <summary>
        /// Cleans invalid values and removes expired entries.
        /// </summary>
        public void Sanitize()
        {
            _currentHp = Mathf.Clamp(_currentHp, 0f, _maxHp);
            _maxHp = Mathf.Max(1f, _maxHp);
            _level = Mathf.Max(1, _level);
            RemoveInvalidBuffEntries();
            RemoveInvalidStatusEntries();
        }

        private void RemoveInvalidBuffEntries()
        {
            if (_activeBuffs == null) return;
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(_activeBuffs[i].BuffId))
                    _activeBuffs.RemoveAt(i);
            }
        }

        private void RemoveInvalidStatusEntries()
        {
            if (_activeStatusEffects == null) return;
            for (int i = _activeStatusEffects.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(_activeStatusEffects[i].StatusId))
                    _activeStatusEffects.RemoveAt(i);
            }
        }

        /// <summary>
        /// Creates a deep copy of the profile.
        /// </summary>
        public RpgProfileData Clone()
        {
            var clone = new RpgProfileData
            {
                Version = _version,
                _currentHp = _currentHp,
                _maxHp = _maxHp,
                _level = _level
            };

            foreach (ActiveBuffEntry entry in _activeBuffs)
            {
                clone._activeBuffs.Add(new ActiveBuffEntry
                {
                    BuffId = entry.BuffId,
                    ExpiresAtUtc = entry.ExpiresAtUtc
                });
            }

            foreach (ActiveStatusEntry entry in _activeStatusEffects)
            {
                clone._activeStatusEffects.Add(new ActiveStatusEntry
                {
                    StatusId = entry.StatusId,
                    ExpiresAtUtc = entry.ExpiresAtUtc,
                    Stacks = entry.Stacks
                });
            }

            return clone;
        }

    }
}
