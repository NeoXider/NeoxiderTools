using System;
using UnityEngine;

namespace Neo.Core.Level
{
    /// <summary>
    ///     Serializable data for level/XP persistence (SaveKey, LoadOnAwake, etc. on component).
    /// </summary>
    [Serializable]
    public sealed class LevelProfileData
    {
        [SerializeField] private int _version = 1;
        [SerializeField] private int _totalXp;
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private int _maxLevel; // 0 = no cap

        public int Version
        {
            get => _version;
            set => _version = value;
        }

        public int TotalXp
        {
            get => _totalXp;
            set => _totalXp = value < 0 ? 0 : value;
        }

        public int CurrentLevel
        {
            get => _currentLevel;
            set => _currentLevel = value < 1 ? 1 : value;
        }

        public int MaxLevel
        {
            get => _maxLevel;
            set => _maxLevel = value < 0 ? 0 : value;
        }

        public void Sanitize()
        {
            _totalXp = _totalXp < 0 ? 0 : _totalXp;
            _currentLevel = _currentLevel < 1 ? 1 : _currentLevel;
            _maxLevel = _maxLevel < 0 ? 0 : _maxLevel;
            if (_maxLevel > 0 && _currentLevel > _maxLevel)
            {
                _currentLevel = _maxLevel;
            }
        }

        public LevelProfileData Clone()
        {
            return new LevelProfileData
            {
                Version = _version,
                TotalXp = _totalXp,
                CurrentLevel = _currentLevel,
                MaxLevel = _maxLevel
            };
        }
    }
}
