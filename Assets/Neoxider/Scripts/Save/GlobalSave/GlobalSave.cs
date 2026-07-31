using System;
using UnityEngine;

namespace Neo.Save
{
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
                _data = value;
                SaveProgress();
            }
        }

        public static bool IsReady { get; set; }

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

        public static void SaveProgress()
        {
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
