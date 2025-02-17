using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Level
    {
        public class LevelManager : MonoBehaviour
        {
            [SerializeField]
            private string _saveKey = "LevelManager";

            [SerializeField]
            private Transform _parentLevel;

            [SerializeField]
            private LevelButton[] _lvlBtns;

            [Space]
            [SerializeField]
            private int _mapId;

            [SerializeField]
            private Map[] _maps = { new() };

            [SerializeField]
            private bool _autoNextMap = false;

            [Space]
            [SerializeField]
            public int _currentLevel;

            [SerializeField]
            private bool _isLoopLevel = false;

            [Space]
            public UnityEvent<int> OnChangeLevel;
            public UnityEvent<int> OnChangeMap;

            public int mapId => _mapId;
            public int currentLevel => _currentLevel;

            private void Start()
            {
                for (var i = 0; i < _maps.Length; i++)
                {
                    _maps[i].Load(i, _saveKey);
                }

                for (int i = 0; i < _lvlBtns.Length; i++)
                {
                    _lvlBtns[i].SetLevelManager(this);
                }

                if (_autoNextMap)
                {
                    SetLastMap();
                }

                UpdateVisual();
            }

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
                    if (!_maps[i].GetCoplete())
                    {
                        return i;
                    }
                }

                return -1;
            }

            public int GetLastLevelId()
            {
                return CurrentMap().level;
            }

            public void SetMapId(int id)
            {
                _mapId = id;
                OnChangeMap?.Invoke(_currentLevel);
                UpdateVisual();
            }

            public void NextLevel()
            {
                SetLevel(_currentLevel + 1);
            }

            public void SetLastLevel()
            {
                SetLevel(CurrentMap().level);
            }

            public void Restart()
            {
                SetLevel(_currentLevel);
            }

            public void SaveLevel()
            {
                if (CurrentMap().level == _currentLevel)
                {
                    print("save level");
                    CurrentMap().SaveLevel();

                    if (_autoNextMap)
                    {
                        if (_mapId == GetLastIdMap() - 1)
                            SetLastMap();
                    }

                    UpdateVisual();
                }
            }

            public Map CurrentMap()
            {
                return _maps[_mapId];
            }

            public int CurrentMapId()
            {
                return CurrentMap().idMap;
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
                _currentLevel = _isLoopLevel ? GetLoopLevel(idLevel, curLvl.countLevels) : Mathf.Min(idLevel, curLvl.countLevels - 1);
                OnChangeLevel?.Invoke(_currentLevel);
            }

            private static int GetLoopLevel(int idLevel, int count)
            {
                return (idLevel + count) % count;
            }

            private void OnValidate()
            {
                if (_parentLevel != null)
                {
                    HashSet<LevelButton> btns = new HashSet<LevelButton>();

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
                }

                for (int i = 0; i < _maps.Length; i++)
                {
                    _maps[i].idMap = i;
                }
            }
        }
    }
}
