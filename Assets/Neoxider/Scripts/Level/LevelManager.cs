using Neo.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Level
    {
        public class LevelManager : Singleton<LevelManager>
        {
            [SerializeField] private string _saveKey = "LevelManager";

            [SerializeField] private Transform _parentLevel;

            [SerializeField] private LevelButton[] _lvlBtns;

            [Space][SerializeField] private int _mapId;

            [SerializeField] private Map[] _maps = { new() };

            [SerializeField] private bool _onAwakeNextMap;
            [SerializeField] private bool _onAwakeNextLevel;

            [Space][SerializeField] private int _currentLevel;

            [Space] public UnityEvent<int> OnChangeLevel;

            public UnityEvent<int> OnChangeMap;
            public UnityEvent<int> OnLoadLevel;

            public int mapId => _mapId;
            public int currentLevel => _currentLevel;
            public Map map => _maps[_mapId];

            private void Start()
            {
                for (var i = 0; i < _maps.Length; i++) _maps[i].Load(i, _saveKey);

                for (var i = 0; i < _lvlBtns.Length; i++) _lvlBtns[i].SetLevelManager(this);

                if (_onAwakeNextMap) SetLastMap();
                if (_onAwakeNextLevel) SetLastLevel();

                OnLoadLevel?.Invoke(map.level);

                UpdateVisual();
            }

            private void OnValidate()
            {
                if (_parentLevel != null)
                {
                    var btns = new HashSet<LevelButton>();

                    foreach (var par in _parentLevel.GetComponentsInChildren<Transform>(true))
                        foreach (var child in par.GetComponentsInChildren<Transform>(true))
                            if (child.TryGetComponent(out LevelButton levelButton))
                                btns.Add(levelButton);

                    _lvlBtns = new LevelButton[btns.Count];
                    btns.CopyTo(_lvlBtns);
                }

                for (var i = 0; i < _maps.Length; i++) _maps[i].idMap = i;
            }

            public void SetLastMap()
            {
                var mapId = GetLastIdMap();
                if (mapId == -1) mapId = _maps.Length - 1;

                _mapId = mapId;
                SetMapId(mapId);
            }

            public int GetLastIdMap()
            {
                for (var i = 0; i < _maps.Length; i++)
                    if (!_maps[i].GetCopmplete())
                        return i;

                return -1;
            }

            public int GetLastLevelId()
            {
                return map.level;
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
                if (map.isLoopLevel && map.countLevels >= map.level)
                    NextLevel();
                else
                    SetLevel(map.level);
            }

            public void Restart()
            {
                SetLevel(_currentLevel);
            }

            public void SaveLevel()
            {
                if (map.level == _currentLevel)
                {
                    print("save level");
                    map.SaveLevel();

                    if (_onAwakeNextMap)
                        if (_mapId == GetLastIdMap() - 1)
                            SetLastMap();

                    UpdateVisual();
                }
            }

            public int MapId()
            {
                return map.idMap;
            }

            private void UpdateVisual()
            {
                var curLevel = map;

                foreach (var item in _lvlBtns) item.transform.gameObject.SetActive(false);

                for (var i = 0; i < _lvlBtns.Length && i < curLevel.countLevels; i++)
                {
                    _lvlBtns[i].transform.gameObject.SetActive(true);

                    var idVisual = i < curLevel.level ? 1 : i == curLevel.level ? 2 : 0;
                    _lvlBtns[i].SetVisual(idVisual, i);
                }
            }

            internal void SetLevel(int idLevel)
            {
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
                return (idLevel + count) % count;
            }
        }
    }
}