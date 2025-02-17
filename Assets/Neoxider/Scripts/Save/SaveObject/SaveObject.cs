using System;
using UnityEngine;

namespace Neo.Save
{
    public class SaveObject<T> : MonoBehaviour where T : class
    {
        [SerializeField]
        private bool _loadOnAwake=true;

        [SerializeField]
        protected T _data;
        private bool _isReady;

        [SerializeField]
        private string saveData = "SaveData";

        private void Awake()
        {
            if (_loadOnAwake)
                Load();
        }

        public T data
        {
            get
            {
                if (_data == null)
                {
                    Load();
                }
                return _data;
            }
            set
            {
                _data = value;
                Save();
            }
        }

        public bool IsReady
        {
            get => _isReady;
            set
            {
                _isReady = value;
            }
        }


        public void Load()
        {
            try
            {
                string jsonData = PlayerPrefs.GetString(saveData, string.Empty);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    _data = JsonUtility.FromJson<T>(jsonData);
                }
                IsReady = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading data: " + e.Message);
            }
        }

        public void Save()
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