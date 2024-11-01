using UnityEngine;

namespace Neoxider
{
    namespace Level
    {
        public class Level : MonoBehaviour
        {

            [SerializeField] private int _level;
            public int level => _level;

            public int idComplexity;
            public int countLevels;

            private void Awake()
            {
                Load();
            }

            private void Load()
            {
                _level = PlayerPrefs.GetInt(idComplexity + nameof(_level), 0);
            }

            public void SetLevel()
            {

            }

            public void SaveLevel()
            {
                _level++;
                PlayerPrefs.SetInt(idComplexity + nameof(_level), _level);
            }

            public void OnValidate()
            {

            }
        }
    }
}