using System;
using UnityEngine;

namespace Neo
{
    namespace Level
    {
        [Serializable]
        public class Map
        {
            public bool isInfinity = true;
            public int countLevels;
            public bool isLoopLevel;

            [Space] [SerializeField] private int _level;

            public int idMap;

            private string _saveKey;
            public int level => _level;

            public void Load(int i, string saveKey)
            {
                idMap = i;
                _saveKey = saveKey;

                _level = PlayerPrefs.GetInt(GetSaveKey() + nameof(_level), 0);
            }

            private string GetSaveKey()
            {
                return $"Map_{_saveKey}_{idMap}_";
            }

            public void SaveLevel()
            {
                PlayerPrefs.SetInt(GetSaveKey() + nameof(_level), ++_level);
            }

            public bool GetCopmplete()
            {
                if (!isInfinity)
                    return _level >= countLevels;
                return false;
            }
        }
    }
}