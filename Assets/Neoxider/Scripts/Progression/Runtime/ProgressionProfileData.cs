using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Progression
{
    /// <summary>
    /// Serializable meta-progression payload stored by the progression manager.
    /// </summary>
    [Serializable]
    public sealed class ProgressionProfileData
    {
        [SerializeField] private int _version = 2;
        [SerializeField] private int _totalXp;
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _availablePerkPoints;
        [SerializeField] private List<string> _unlockedNodeIds = new();
        [SerializeField] private List<string> _purchasedPerkIds = new();

        /// <summary>
        /// Gets or sets the serialized profile version.
        /// </summary>
        public int Version
        {
            get => _version;
            set => _version = value;
        }

        /// <summary>
        /// Gets or sets the accumulated XP.
        /// </summary>
        public int TotalXp
        {
            get => _totalXp;
            set => _totalXp = Mathf.Max(0, value);
        }

        /// <summary>
        /// Gets or sets the resolved player level.
        /// </summary>
        public int CurrentLevel
        {
            get => _currentLevel;
            set => _currentLevel = Mathf.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the currently unspent perk points.
        /// </summary>
        public int AvailablePerkPoints
        {
            get => _availablePerkPoints;
            set => _availablePerkPoints = Mathf.Max(0, value);
        }

        /// <summary>
        /// Gets the unlocked node identifiers.
        /// </summary>
        public List<string> UnlockedNodeIds => _unlockedNodeIds;

        /// <summary>
        /// Gets the purchased perk identifiers.
        /// </summary>
        public List<string> PurchasedPerkIds => _purchasedPerkIds;

        /// <summary>
        /// Cleans invalid values and removes empty identifiers.
        /// </summary>
        public void Sanitize()
        {
            _totalXp = Mathf.Max(0, _totalXp);
            _currentLevel = Mathf.Max(1, _currentLevel);
            _availablePerkPoints = Mathf.Max(0, _availablePerkPoints);
            RemoveInvalidEntries(_unlockedNodeIds);
            RemoveInvalidEntries(_purchasedPerkIds);
        }

        /// <summary>
        /// Creates a deep copy of the profile.
        /// </summary>
        public ProgressionProfileData Clone()
        {
            return new ProgressionProfileData
            {
                Version = _version,
                TotalXp = _totalXp,
                CurrentLevel = _currentLevel,
                AvailablePerkPoints = _availablePerkPoints,
                _unlockedNodeIds = new List<string>(_unlockedNodeIds),
                _purchasedPerkIds = new List<string>(_purchasedPerkIds)
            };
        }

        private static void RemoveInvalidEntries(List<string> values)
        {
            if (values == null)
            {
                return;
            }

            for (int i = values.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                {
                    values.RemoveAt(i);
                }
            }
        }
    }
}
