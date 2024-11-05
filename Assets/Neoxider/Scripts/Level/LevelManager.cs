using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Level
    {
        public class LevelManager : MonoBehaviour
        {
            [SerializeField] private Transform _parentLevel;
            [SerializeField] private LevelButton[] _lvlBtns;

            public int mapId;
            public Map[] maps;

            public int currentLevel;
            public bool isLoopLevel = false;

            [Space]
            public UnityEvent<int> OnChangeLevel;
            public UnityEvent<int> OnChangeMap;

            private void Start()
            {
                for (var i = 0; i < maps.Length; i++)
                {
                    maps[i].Load();
                }

                for (int i = 0; i < _lvlBtns.Length; i++)
                {
                    _lvlBtns[i].SetLevelManager(this);
                }

                UpdateVisual();
            }

            public void SetComplexity(int id)
            {
                mapId = id;
                OnChangeMap?.Invoke(currentLevel);
                UpdateVisual();
            }

            public void NextLevel()
            {
                SetLevel(currentLevel + 1);
            }

            public void Restart()
            {
                SetLevel(currentLevel);
            }

            public void SaveLevel()
            {
                if (CurrentMap().level == currentLevel)
                {
                    CurrentMap().SaveLevel();
                    UpdateVisual();
                }

            }

            public Map CurrentMap()
            {
                return maps[mapId];
            }

            private void UpdateVisual()
            {
                Map curLevel = CurrentMap();

                foreach (var item in _lvlBtns)
                {
                    item.transform.gameObject.SetActive(false);
                }

                for (int i = 0; i < _lvlBtns.Length && i < curLevel.countLevels; i++)
                {
                    _lvlBtns[i].transform.gameObject.SetActive(true);

                    int idVisual = i < curLevel.level ? 1 : (i == curLevel.level ? 2 : 0);
                    _lvlBtns[i].SetVisual(idVisual, i);
                }
            }

            internal void SetLevel(int idLevel)
            {
                Map curLvl = CurrentMap();
                currentLevel = isLoopLevel ? GetLoopLevel(idLevel, curLvl.countLevels) : Mathf.Min(idLevel, curLvl.countLevels - 1);
                OnChangeLevel?.Invoke(currentLevel);
            }

            private static int GetLoopLevel(int idLevel, int count)
            {
                return (idLevel + count) % count;
            }

            private void OnValidate()
            {
                HashSet<LevelButton> btns = new HashSet<LevelButton>();

                if (_parentLevel != null)
                {
                    foreach (var par in _parentLevel.GetComponentsInChildren<Transform>(true))
                    {
                        foreach (var child in par.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.TryGetComponent(out LevelButton levelButton))
                                btns.Add(levelButton);
                        }
                    }

                    _lvlBtns = new LevelButton[btns.Count];
                    btns.CopyTo(_lvlBtns);

                    for (int i = 0; i < maps.Length; i++)
                    {
                        maps[i].idMap = i;
                    }
                }
            }
        }
    }
}
