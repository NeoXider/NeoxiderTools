using System;
using UnityEngine;

namespace Neo.Save
{
    public static class GlobalSave
    {
        private static GlobalData _data;

        private static readonly string saveData = "SavesData";

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

                IsReady = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading data: " + e.Message);
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
                Debug.LogError("Error saving progress: " + e.Message);
            }
        }
    }
}