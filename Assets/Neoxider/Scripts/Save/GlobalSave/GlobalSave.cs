using System;
using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Static holder of one project-wide <see cref="GlobalData" /> object, stored as JSON under the
    ///     <c>SavesData</c> key of the active <see cref="SaveProvider" />.
    /// </summary>
    public static class GlobalSave
    {
        private static GlobalData _data;

        private static readonly string saveData = "SavesData";

        // WHY: _data and IsReady surviving a play session (domain reload disabled) make the getter serve
        // the previous session's object instead of re-reading the save.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _data = null;
            IsReady = false;
        }

        /// <summary>
        ///     The global data object. Reading loads it lazily (a fresh object on first launch or corrupt JSON);
        ///     assigning replaces and stages it immediately. Assigning <c>null</c> resets it to a fresh object.
        /// </summary>
        public static GlobalData data
        {
            get
            {
                if (_data == null)
                {
                    LoadingData();
                }

                return _data;
            }
            set
            {
                _data = value ?? new GlobalData();
                SaveProgress();
            }
        }

        /// <summary>
        ///     Whether <see cref="data" /> was read from the provider at least once this session.
        /// </summary>
        public static bool IsReady { get; set; }

        /// <summary>
        ///     Reads <see cref="data" /> from the active provider, replacing the in-memory object.
        /// </summary>
        public static void LoadingData()
        {
            try
            {
                string jsonData = SaveProvider.GetString(saveData, string.Empty);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    _data = JsonUtility.FromJson<GlobalData>(jsonData);
                }

                // WHY: first launch (key absent) or corrupt JSON must still yield a usable object —
                // callers access GlobalSave.data.<field> directly and would NRE on null.
                _data ??= new GlobalData();
                IsReady = true;
            }
            catch (Exception e)
            {
                SaveProvider.LogError("Error loading data: " + e.Message);
                _data ??= new GlobalData();
            }
        }

        /// <summary>
        ///     Stages the in-memory <see cref="data" /> in the provider. Call <see cref="SaveProvider.Save" /> to flush
        ///     it to disk. Does nothing while the data was never loaded or assigned.
        /// </summary>
        public static void SaveProgress()
        {
            // WHY: JsonUtility.ToJson(null) is an empty string; writing it would erase the stored profile when a
            // quit/checkpoint handler saves before anything read GlobalSave.data.
            if (_data == null)
            {
                return;
            }

            try
            {
                string jsonData = JsonUtility.ToJson(_data);
                SaveProvider.SetString(saveData, jsonData);
            }
            catch (Exception e)
            {
                SaveProvider.LogError("Error saving progress: " + e.Message);
            }
        }
    }
}
