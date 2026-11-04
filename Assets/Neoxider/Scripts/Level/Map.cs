using System;
using Neo.Save;
using UnityEngine;

namespace Neo.Level
{
        /// <summary>
        ///     One level map of <see cref="LevelManager" />: level count, looping/infinite rules and the saved frontier
        ///     (number of completed levels) stored in <see cref="SaveProvider" />.
        /// </summary>
        [Serializable]
        public class Map
        {
            public bool isInfinity = true;
            public int countLevels;
            public bool isLoopLevel;

            [Space] [SerializeField] private int _level;

            public int idMap;

            private string _saveKey;

            /// <summary>Saved frontier: the number of completed levels, which is also the index of the next level.</summary>
            public int level => _level;

            /// <summary>Whether every level of a finite map is completed. Infinite maps are never complete.</summary>
            public bool IsComplete => !isInfinity && _level >= countLevels;

            /// <summary>Whether <see cref="Load" /> has bound the map to a save key, so a reset can reach storage.</summary>
            internal bool IsBound => _saveKey != null;

            /// <summary>Binds the map to its index and save key and reads the saved frontier.</summary>
            public void Load(int i, string saveKey)
            {
                idMap = i;
                // WHY: a null key still reads "Map__{id}__level", so it must count as bound for ResetProgress.
                _saveKey = saveKey ?? string.Empty;

                _level = Mathf.Max(0, SaveProvider.GetInt(GetSaveKey() + nameof(_level)));
            }

            private string GetSaveKey()
            {
                return $"Map_{_saveKey}_{idMap}_";
            }

            /// <summary>Advances the frontier by one completed level and stages it in <see cref="SaveProvider" />.</summary>
            public void SaveLevel()
            {
                if (_level == int.MaxValue)
                {
                    return;
                }

                int previousLevel = _level;
                _level++;
                try
                {
                    SaveProvider.SetInt(GetSaveKey() + nameof(_level), _level);
                }
                catch
                {
                    _level = previousLevel;
                    throw;
                }
            }

            /// <summary>
            ///     Clears the frontier back to level 0 and deletes its saved value from <see cref="SaveProvider" />
            ///     (staged like <see cref="SaveLevel" />). A map that was never bound by <see cref="Load" /> only resets
            ///     the in-memory frontier, because its save key is unknown; <see cref="LevelManager.TryResetProgress" />
            ///     binds it first.
            /// </summary>
            public void ResetProgress()
            {
                if (_saveKey != null)
                {
                    // WHY: delete first, so a throwing provider leaves memory and storage in agreement.
                    SaveProvider.DeleteKey(GetSaveKey() + nameof(_level));
                }

                _level = 0;
            }

            /// <summary>Misspelled legacy name of <see cref="IsComplete" />.</summary>
            [Obsolete("Use IsComplete.")]
            public bool GetCopmplete()
            {
                return IsComplete;
            }
        }
}
