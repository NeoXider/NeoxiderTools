using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Level
    {
        public class LevelManager : Singleton<LevelManager>
        {
            [SerializeField] private string _saveKey = "LevelManager";

            [GUIColor(0.7, 0.7, 1)] [Space] [SerializeField]
            private int _currentLevel;

            [SerializeField] private Map[] _maps = { new() };

            [Space] [SerializeField] private LevelButton[] _lvlBtns;

            [Space] [SerializeField] private int _mapId;
            [SerializeField] private bool _onAwakeNextLevel;

            [SerializeField] private bool _onAwakeNextMap;

            [SerializeField] private Transform _parentLevel;

            [Space] public UnityEvent<int> OnChangeLevel;

            public UnityEvent<int> OnChangeMap;
            [Space] public UnityEvent<int> OnChangeMaxLevel;

            public int MaxLevel => Map.level;
            public int MapId => _mapId;
            public int CurrentLevel => _currentLevel;
            public Map Map => _maps[_mapId];

            protected override void Init()
            {
                base.Init();

                for (int i = 0; i < _maps.Length; i++)
                {
                    _maps[i].Load(i, _saveKey);
                }

                for (int i = 0; i < _lvlBtns.Length; i++)
                {
                    _lvlBtns[i].SetLevelManager(this);
                }

                if (_onAwakeNextMap)
                {
                    SetLastMap();
                }

                if (_onAwakeNextLevel)
                {
                    SetLastLevel();
                }

                OnChangeMaxLevel?.Invoke(MaxLevel);
                UpdateVisual();
            }

            private void OnValidate()
            {
                if (_parentLevel != null)
                {
                    HashSet<LevelButton> btns = new();

                    foreach (Transform par in _parentLevel.GetComponentsInChildren<Transform>(true))
                    foreach (Transform child in par.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.TryGetComponent(out LevelButton levelButton))
                        {
                            btns.Add(levelButton);
                        }
                    }

                    _lvlBtns = new LevelButton[btns.Count];
                    btns.CopyTo(_lvlBtns);
                }

                for (int i = 0; i < _maps.Length; i++)
                {
                    _maps[i].idMap = i;
                }
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void SetLastMap()
            {
                int mapId = GetLastIdMap();
                if (mapId == -1)
                {
                    mapId = _maps.Length - 1;
                }

                _mapId = mapId;
                SetMapId(mapId);
            }

            public int GetLastIdMap()
            {
                for (int i = 0; i < _maps.Length; i++)
                {
                    if (!_maps[i].GetCopmplete())
                    {
                        return i;
                    }
                }

                return -1;
            }

            public int GetLastLevelId()
            {
                return Map.level;
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void SetMapId(int id)
            {
                _mapId = id;
                OnChangeMap?.Invoke(_currentLevel);
                UpdateVisual();
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void NextLevel()
            {
                SetLevel(_currentLevel + 1);
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void SetLastLevel()
            {
                if (Map.isLoopLevel && Map.countLevels >= Map.level)
                {
                    NextLevel();
                }
                else
                {
                    SetLevel(Map.level);
                }
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void Restart()
            {
                SetLevel(_currentLevel);
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            public void SaveLevel()
            {
                if (Map.level == _currentLevel)
                {
                    Map.SaveLevel();
                    OnChangeMaxLevel?.Invoke(MaxLevel);

                    if (_onAwakeNextMap)
                    {
                        if (_mapId == GetLastIdMap() - 1)
                        {
                            SetLastMap();
                        }
                    }

                    UpdateVisual();
                }
            }

            private void UpdateVisual()
            {
                Map curLevel = Map;

                foreach (LevelButton item in _lvlBtns)
                {
                    item.transform.gameObject.SetActive(false);
                }

                for (int i = 0; i < _lvlBtns.Length && i < curLevel.countLevels; i++)
                {
                    _lvlBtns[i].transform.gameObject.SetActive(true);

                    int idVisual = i < curLevel.level ? 1 : i == curLevel.level ? 2 : 0;
                    _lvlBtns[i].SetVisual(idVisual, i);
                }
            }

#if ODIN_INSPECTOR
            [Sirenix.OdinInspector.Button]
#else
            [Button]
#endif
            internal void SetLevel(int idLevel)
            {
                _currentLevel = Map.isLoopLevel
                    ? GetLoopLevel(idLevel, Map.countLevels)
                    : Mathf.Min(idLevel,
                        Map.isInfinity || Map.countLevels == 0
                            ? Map.level + 1
                            : Map.countLevels - 1);

                OnChangeLevel?.Invoke(_currentLevel);
            }

            public static int GetLoopLevel(int idLevel, int count)
            {
                return (idLevel + count) % count;
            }
        }
    }
}