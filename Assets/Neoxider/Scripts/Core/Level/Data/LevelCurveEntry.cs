using System;
using UnityEngine;

namespace Neo.Core.Level
{
    /// <summary>
    ///     Serializable level entry for Custom curve (Level, RequiredXp). Optional reward ids can be added later.
    /// </summary>
    [Serializable]
    public sealed class LevelCurveEntry : ILevelCurveEntry
    {
        [SerializeField] [Min(1)] private int _level = 1;
        [SerializeField] [Min(0)] private int _requiredXp;

        public int Level => _level;
        public int RequiredXp => _requiredXp;

        public LevelCurveEntry()
        {
        }

        public LevelCurveEntry(int level, int requiredXp)
        {
            _level = level < 1 ? 1 : level;
            _requiredXp = requiredXp < 0 ? 0 : requiredXp;
        }
    }
}
