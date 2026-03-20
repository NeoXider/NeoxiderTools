using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Level
    {
        [CreateFromMenu("Neoxider/Level/LevelManager")]
        [AddComponentMenu("Neoxider/Level/" + nameof(LevelManager))]
        [NeoDoc("Level/LevelManager.md")]
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

            public int MaxLevel => TryGetCurrentMap(out Map map) ? map.level : 0;
            public int MapId => _mapId;
            public int CurrentLevel => _currentLevel;
            public Map Map => TryGetCurrentMap(out Map map) ? map : null;

            private void OnValidate()
            {
                if (_maps == null || _maps.Length == 0)
                {
                    _maps = new[] { new Map() };
                }

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

            protected override void Init()
            {
                base.Init();

                if (!HasMapsConfigured())
                {
                    Debug.LogWarning("[LevelManager] No maps configured.", this);
                    return;
                }

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

            [Button]
            public void SetLastMap()
            {
                if (!HasMapsConfigured())
                {
                    return;
                }

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
                if (!HasMapsConfigured())
                {
                    return -1;
                }

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
                return TryGetCurrentMap(out Map map) ? map.level : 0;
            }

            [Button]
            public void SetMapId(int id)
            {
                if (!TrySetMapId(id))
                {
                    return;
                }

                OnChangeMap?.Invoke(_mapId);
                OnChangeMaxLevel?.Invoke(MaxLevel);
                UpdateVisual();
            }

            [Button]
            public void NextLevel()
            {
                SetLevel(_currentLevel + 1);
            }

            [Button]
            public void SetLastLevel()
            {
                if (!TryGetCurrentMap(out Map map))
                {
                    return;
                }

                if (map.isLoopLevel && map.countLevels >= map.level)
                {
                    NextLevel();
                }
                else
                {
                    SetLevel(map.level);
                }
            }

            [Button]
            public void Restart()
            {
                SetLevel(_currentLevel);
            }

            [Button]
            public void SaveLevel()
            {
                if (!TryGetCurrentMap(out Map map))
                {
                    return;
                }

                if (map.level == _currentLevel)
                {
                    map.SaveLevel();
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
                if (!TryGetCurrentMap(out Map curLevel))
                {
                    return;
                }

                foreach (LevelButton item in _lvlBtns)
                {
                    if (item != null)
                    {
                        item.transform.gameObject.SetActive(false);
                    }
                }

                for (int i = 0; i < _lvlBtns.Length && i < curLevel.countLevels; i++)
                {
                    if (_lvlBtns[i] == null)
                    {
                        continue;
                    }

                    _lvlBtns[i].transform.gameObject.SetActive(true);

                    int idVisual = i < curLevel.level ? 1 : i == curLevel.level ? 2 : 0;
                    _lvlBtns[i].SetVisual(idVisual, i);
                }
            }

            [Button]
            internal void SetLevel(int idLevel)
            {
                if (!TryGetCurrentMap(out Map map))
                {
                    return;
                }

                _currentLevel = map.isLoopLevel
                    ? GetLoopLevel(idLevel, map.countLevels)
                    : Mathf.Min(idLevel,
                        map.isInfinity || map.countLevels == 0
                            ? map.level + 1
                            : map.countLevels - 1);

                OnChangeLevel?.Invoke(_currentLevel);
            }

            public static int GetLoopLevel(int idLevel, int count)
            {
                if (count <= 0)
                {
                    return 0;
                }

                return (idLevel + count) % count;
            }

            private bool HasMapsConfigured()
            {
                return _maps != null && _maps.Length > 0;
            }

            private bool TryGetCurrentMap(out Map map)
            {
                map = null;
                if (!HasMapsConfigured())
                {
                    return false;
                }

                if (_mapId < 0 || _mapId >= _maps.Length)
                {
                    return false;
                }

                map = _maps[_mapId];
                return map != null;
            }

            private bool TrySetMapId(int id)
            {
                if (!HasMapsConfigured())
                {
                    Debug.LogWarning("[LevelManager] Cannot set map. No maps configured.", this);
                    return false;
                }

                if (id < 0 || id >= _maps.Length)
                {
                    Debug.LogWarning($"[LevelManager] Map index {id} is out of range.", this);
                    return false;
                }

                _mapId = id;
                return true;
            }
        }
    }
}
