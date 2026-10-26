using System;
using Neo.Extensions;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Level
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

            [Space] [SerializeField] private LevelButton[] _lvlBtns = Array.Empty<LevelButton>();

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
                    _lvlBtns = _parentLevel.GetComponentsInChildren<LevelButton>(true);
                }

                for (int i = 0; i < _maps.Length; i++)
                {
                    if (_maps[i] != null)
                    {
                        _maps[i].idMap = i;
                    }
                }
            }

            protected override void Init()
            {
                base.Init();

                if (!HasMapsConfigured())
                {
                    NeoDiagnostics.LogWarning("[LevelManager] No maps configured.", this);
                    return;
                }

                for (int i = 0; i < _maps.Length; i++)
                {
                    _maps[i]?.Load(i, _saveKey);
                }

                _lvlBtns ??= Array.Empty<LevelButton>();
                for (int i = 0; i < _lvlBtns.Length; i++)
                {
                    if (_lvlBtns[i] != null)
                    {
                        _lvlBtns[i].SetLevelManager(this);
                    }
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
                    for (int i = _maps.Length - 1; i >= 0; i--)
                    {
                        if (_maps[i] != null)
                        {
                            mapId = i;
                            break;
                        }
                    }
                }

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
                    if (_maps[i] != null && !_maps[i].GetCopmplete())
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
                SetLevel(_currentLevel < int.MaxValue ? _currentLevel + 1 : int.MaxValue);
            }

            [Button]
            public void SetLastLevel()
            {
                if (!TryGetCurrentMap(out Map map))
                {
                    return;
                }

                SetLevel(map.level);
            }

            [Button]
            public void Restart()
            {
                SetLevel(_currentLevel);
            }

            [Button]
            public void SaveLevel()
            {
                if (!TryGetCurrentMap(out Map map) || (map.isLoopLevel && map.countLevels <= 0))
                {
                    return;
                }

                int frontierLevel = map.isLoopLevel ? GetLoopLevel(map.level, map.countLevels) : map.level;
                if (frontierLevel == _currentLevel)
                {
                    int completedMapId = _mapId;
                    map.SaveLevel();
                    OnChangeMaxLevel?.Invoke(MaxLevel);

                    if (_onAwakeNextMap && _mapId == completedMapId && map.GetCopmplete())
                    {
                        int nextMapId = GetLastIdMap();
                        if (nextMapId > completedMapId)
                        {
                            SetMapId(nextMapId);
                        }
                    }

                    UpdateVisual();
                }
            }

            private void UpdateVisual()
            {
                if (!TryGetCurrentMap(out Map curLevel) || _lvlBtns == null)
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

                    int idVisual = i < curLevel.level ? 2 : i == curLevel.level ? 1 : 0;
                    _lvlBtns[i].SetVisual(idVisual, i);
                }
            }

            /// <summary>Selects a zero-based level, wrapping looping maps and clamping other maps to valid bounds.</summary>
            [Button]
            public void SetLevel(int idLevel)
            {
                if (!TryGetCurrentMap(out Map map))
                {
                    return;
                }

                int maxLevel = map.isInfinity || map.countLevels == 0
                    ? (map.level < int.MaxValue ? map.level + 1 : int.MaxValue)
                    : (map.countLevels > 0 ? map.countLevels - 1 : 0);
                _currentLevel = map.isLoopLevel
                    ? GetLoopLevel(idLevel, map.countLevels)
                    : Mathf.Clamp(idLevel, 0, maxLevel);

                OnChangeLevel?.Invoke(_currentLevel);
            }

            public static int GetLoopLevel(int idLevel, int count)
            {
                return idLevel.Wrap(0, count);
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
                    NeoDiagnostics.LogWarning("[LevelManager] Cannot set map. No maps configured.", this);
                    return false;
                }

                if (id < 0 || id >= _maps.Length)
                {
                    NeoDiagnostics.LogWarning($"[LevelManager] Map index {id} is out of range.", this);
                    return false;
                }

                if (_maps[id] == null)
                {
                    NeoDiagnostics.LogWarning($"[LevelManager] Map at index {id} is not configured.", this);
                    return false;
                }

                _mapId = id;
                return true;
            }
        }
}
