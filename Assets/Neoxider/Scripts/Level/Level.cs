using UnityEngine;

namespace Neoxider
{
    namespace Level
    {
        [System.Serializable]
        public class Map
        {
            [SerializeField] private int _level;
            public int level => _level;

            public int idMap;
            public int countLevels;

            public void Load()
            {
                _level = PlayerPrefs.GetInt(idMap + nameof(_level), 0);
            }

            public void SaveLevel()
            {
                _level = _level + 1;
                PlayerPrefs.SetInt(idMap + nameof(_level), _level);
            }
        }
    }
}