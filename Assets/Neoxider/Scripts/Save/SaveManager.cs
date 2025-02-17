using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Neo.Save
{
    [System.Serializable]
    public class FieldData
    {
        public string Key;
        public object Value;
    }

    public class SaveManager : MonoBehaviour
    {
        private static Dictionary<string, List<FieldInfo>> _saveableFields = new Dictionary<string, List<FieldInfo>>();
        private const string saveDataKeyPrefix = "SaveData_";

        private void Awake()
        {
            RegisterAllSaveables();
            Load();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public static void Register(object obj)
        {
            if (obj is MonoBehaviour monoObj)
            {
                string key = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
                List<FieldInfo> fieldsToSave = new List<FieldInfo>();

                FieldInfo[] fields = monoObj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    var saveAttr = field.GetCustomAttributes(typeof(SaveField), true) as SaveField[];
                    if (saveAttr.Length > 0)
                    {
                        fieldsToSave.Add(field);
                    }
                }

                if (fieldsToSave.Count > 0)
                {
                    _saveableFields[key] = fieldsToSave;
                }
            }
        }

        public static void Unregister(object obj)
        {
            if (obj is MonoBehaviour monoObj)
            {
                string key = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
                _saveableFields.Remove(key);
            }
        }

        private static void Save()
        {
            var saveData = new Dictionary<string, List<FieldData>>();
            foreach (var kvp in _saveableFields)
            {
                string key = kvp.Key;
                List<FieldInfo> fieldsToSave = kvp.Value;

                var fieldDatas = new List<FieldData>();
                var instance = GetInstanceByKey(key);
                if (instance != null)
                {
                    foreach (FieldInfo field in fieldsToSave)
                    {
                        var saveAttr = field.GetCustomAttributes(typeof(SaveField), true) as SaveField[];
                        if (saveAttr.Length > 0 && saveAttr[0].AutoSaveOnQuit)
                        {
                            fieldDatas.Add(new FieldData { Key = saveAttr[0].Key, Value = field.GetValue(instance) });
                        }
                    }
                }

                if (fieldDatas.Count > 0)
                {
                    saveData[key] = fieldDatas;
                }
            }

            string jsonData = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString($"{saveDataKeyPrefix}All", jsonData);
        }

        private static void Load()
        {
            try
            {
                string jsonData = PlayerPrefs.GetString($"{saveDataKeyPrefix}All", "{}");
                var loadedData = JsonUtility.FromJson<Dictionary<string, List<FieldData>>>(jsonData);

                foreach (var kvp in _saveableFields)
                {
                    string key = kvp.Key;
                    List<FieldInfo> fieldsToSave = kvp.Value;

                    if (loadedData.TryGetValue(key, out List<FieldData> data))
                    {
                        var instance = GetInstanceByKey(key);
                        if (instance != null)
                        {
                            foreach (FieldInfo field in fieldsToSave)
                            {
                                var saveAttr = field.GetCustomAttributes(typeof(SaveField), true) as SaveField[];
                                if (saveAttr.Length > 0 && saveAttr[0].AutoLoadOnAwake)
                                {
                                    FieldData fieldData = data.Find(fd => fd.Key == saveAttr[0].Key);
                                    if (fieldData != null)
                                    {
                                        field.SetValue(instance, fieldData.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading save data: " + e.Message);
            }
        }

        private static void RegisterAllSaveables()
        {
            var allObjects = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in allObjects)
            {
                if (obj is ISaveableComponent)
                {
                    Register(obj);
                }
            }
        }

        public static void SaveField(string key, string fieldName, object value)
        {
            if (_saveableFields.TryGetValue(key, out List<FieldInfo> fields))
            {
                foreach (var field in fields)
                {
                    var saveAttr = field.GetCustomAttributes(typeof(SaveField), true) as SaveField[];
                    if (saveAttr.Length > 0 && saveAttr[0].Key == fieldName)
                    {
                        var instance = GetInstanceByKey(key);
                        if (instance != null)
                        {
                            field.SetValue(instance, value);

                            string jsonData = JsonUtility.ToJson(new List<FieldData> { new FieldData { Key = fieldName, Value = value } });
                            PlayerPrefs.SetString($"{saveDataKeyPrefix}{key}_{fieldName}", jsonData);
                        }
                    }
                }
            }
        }

        public static void LoadField(string key, string fieldName)
        {
            var instance = GetInstanceByKey(key);
            if (instance != null && _saveableFields.TryGetValue(key, out List<FieldInfo> fields))
            {
                foreach (var field in fields)
                {
                    var saveAttr = field.GetCustomAttributes(typeof(SaveField), true) as SaveField[];
                    if (saveAttr.Length > 0 && saveAttr[0].Key == fieldName)
                    {
                        try
                        {
                            string jsonData = PlayerPrefs.GetString($"{saveDataKeyPrefix}{key}_{fieldName}", "{}");
                            var loadedData = JsonUtility.FromJson<List<FieldData>>(jsonData);

                            FieldData fieldData = loadedData.Find(fd => fd.Key == fieldName);
                            if (fieldData != null)
                            {
                                field.SetValue(instance, fieldData.Value);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Error loading field data: " + e.Message);
                        }
                    }
                }
            }
        }

        private static MonoBehaviour GetInstanceByKey(string key)
        {
            string[] parts = key.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out int instanceId))
            {
                var allObjects = FindObjectsOfType<MonoBehaviour>();
                foreach (var obj in allObjects)
                {
                    if (obj.GetInstanceID() == instanceId)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        public static MonoBehaviour FindObjectOfType(Predicate<MonoBehaviour> predicate)
        {
            var allObjects = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in allObjects)
            {
                if (predicate(obj))
                {
                    return obj;
                }
            }
            return null;
        }
    }
}