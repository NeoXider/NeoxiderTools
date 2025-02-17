using System;
using UnityEngine;

namespace Neo.Save
{
    public static class GlobalSave
    {
        private static GlobalData _data;
        private static bool _isReady;

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

        public static bool IsReady
        {
            get => _isReady;
            set
            {
                _isReady = value;
            }
        }

        private static string saveData = "SavesData";

        public static void LoadingData()
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(saveData, string.Empty);
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
                PlayerPrefs.SetString(saveData, jsonData);
            }
            catch (Exception e)
            {
                Debug.LogError("Error saving progress: " + e.Message);
            }
        }
    }
}