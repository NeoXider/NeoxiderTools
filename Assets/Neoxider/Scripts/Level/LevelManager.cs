using System;
using Neo.Extensions;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Level
{
        /// <summary>
        ///     Scene singleton for map/level selection: owns a set of <see cref="Map" />s, the selected map and level, saved
        ///     progress per map and the level-button grid. Progress keys use <c>Map_{saveKey}_{mapIndex}_</c>.
        /// </summary>
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

            /// <summary>Raised with the selected zero-based level after <see cref="SetLevel" /> and the helpers that call it.</summary>
            [Space] public UnityEvent<int> OnChangeLevel;

            /// <summary>Raised with the new map index after a successful <see cref="SetMapId" />.</summary>
            public UnityEvent<int> OnChangeMap;
            /// <summary>Raised with the current map frontier on init, map change and after progress is saved.</summary>
            [Space] public UnityEvent<int> OnChangeMaxLevel;

            /// <summary>Frontier of the current map: completed level count and index of the next level to play.</summary>
            public int MaxLevel => TryGetCurrentMap(out Map map) ? map.level : 0;
            /// <summary>Index of the selected map.</summary>
            public int MapId => _mapId;
            /// <summary>Selected zero-based level.</summary>
            public int CurrentLevel => _currentLevel;
            /// <summary>Selected map, or null when none is configured.</summary>
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

            /// <summary>Selects the first incomplete map, or the last configured map when all are complete.</summary>
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

            /// <summary>Index of the first incomplete map, or -1 when every map is complete or none is configured.</summary>
            public int GetLastIdMap()
            {
                if (!HasMapsConfigured())
                {
                    return -1;
                }

                for (int i = 0; i < _maps.Length; i++)
                {
                    if (_maps[i] != null && !_maps[i].IsComplete)
                    {
                        return i;
                    }
                }

                return -1;
            }

            /// <summary>Same as <see cref="MaxLevel" />.</summary>
            public int GetLastLevelId()
            {
                return TryGetCurrentMap(out Map map) ? map.level : 0;
            }

            /// <summary>Selects a configured map by index; invalid indices are ignored with a warning.</summary>
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

            /// <summary>Selects the next level (wrapped on looping maps, clamped otherwise).</summary>
            [Button]
            public void NextLevel()
            {
                SetLevel(_currentLevel < int.MaxValue ? _currentLevel + 1 : int.MaxValue);
            }

            /// <summary>Selects the frontier level of the current map.</summary>
            [Button]
            public void SetLastLevel()
            {
                if (!TryGetCurrentMap(out Map map))
                {
                    return;
                }

                SetLevel(map.level);
            }

            /// <summary>Re-selects the current level and raises <see cref="OnChangeLevel" /> again.</summary>
            [Button]
            public void Restart()
            {
                SetLevel(_currentLevel);
            }

            /// <summary>
            ///     Marks the current level completed when it is the frontier: advances and stages the map progress, raises
            ///     <see cref="OnChangeMaxLevel" /> and refreshes buttons. Call it before <see cref="NextLevel" />.
            /// </summary>
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

                    if (_onAwakeNextMap && _mapId == completedMapId && map.IsComplete)
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

            /// <summary>
            ///     Clears the saved progress of one map: its frontier returns to level 0 and the save key
            ///     <c>Map_{saveKey}_{mapId}__level</c> is deleted. For the selected map it raises
            ///     <see cref="OnChangeMaxLevel" /> and refreshes the level buttons; the selected level is kept. Works before
            ///     the manager initialized too: a map that is not bound to its save key yet is bound first.
            /// </summary>
            /// <returns>False when <paramref name="mapId" /> is not a configured map.</returns>
            /// <remarks>
            ///     The key is deleted before the in-memory frontier changes, so when the save provider throws, the map,
            ///     its stored value and the events stay as they were and the exception reaches the caller.
            /// </remarks>
            public bool TryResetProgress(int mapId)
            {
                if (!HasMapsConfigured() || mapId < 0 || mapId >= _maps.Length || _maps[mapId] == null)
                {
                    return false;
                }

                ResetMapProgress(mapId);
                if (mapId == _mapId)
                {
                    NotifyFrontierChanged();
                }

                return true;
            }

            /// <summary>
            ///     UnityEvent-friendly form of <see cref="TryResetProgress" />. An invalid map index is ignored; the
            ///     warning goes through the <see cref="NeoDiagnostics" /> warning channel, which is off by default.
            /// </summary>
            [Button(PlayModeOnly = true)]
            public void ResetProgress(int mapId)
            {
                if (!TryResetProgress(mapId))
                {
                    NeoDiagnostics.LogWarning($"[LevelManager] Cannot reset map {mapId}: it is not configured.", this);
                }
            }

            /// <summary>Clears the saved progress of every configured map (see <see cref="TryResetProgress" />).</summary>
            /// <remarks>
            ///     Maps are reset in index order. When the save provider throws for one map, the maps before it stay
            ///     reset, that map and the ones after it keep their progress, the exception reaches the caller, and the
            ///     frontier event and button refresh still run when the selected map was among the reset ones.
            /// </remarks>
            [Button(PlayModeOnly = true)]
            public void ResetAllProgress()
            {
                if (!HasMapsConfigured())
                {
                    return;
                }

                bool selectedMapReset = false;
                try
                {
                    for (int i = 0; i < _maps.Length; i++)
                    {
                        if (_maps[i] == null)
                        {
                            continue;
                        }

                        ResetMapProgress(i);
                        if (i == _mapId)
                        {
                            selectedMapReset = true;
                        }
                    }
                }
                finally
                {
                    if (selectedMapReset)
                    {
                        NotifyFrontierChanged();
                    }
                }
            }

            private void ResetMapProgress(int mapId)
            {
                Map map = _maps[mapId];
                if (!map.IsBound)
                {
                    // WHY: before Init (inactive object, early caller) the map does not know its save key, so the reset
                    // would only clear memory and the next Init would read the old frontier back.
                    map.Load(mapId, _saveKey);
                }

                map.ResetProgress();
            }

            private void NotifyFrontierChanged()
            {
                OnChangeMaxLevel?.Invoke(MaxLevel);
                UpdateVisual();
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

            /// <summary>Wraps a level index into <c>[0, count)</c>.</summary>
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
