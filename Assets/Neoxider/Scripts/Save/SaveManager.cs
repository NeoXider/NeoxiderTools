using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Neo.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Save
{
    public class SaveManager : Singleton<SaveManager>
    {
        private const string DefaultJson = "{\"AllSavedComponents\":[]}";

        private const string saveDataKeyPrefix = "SaveData_";

        private static readonly Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> _saveableComponents
            = new();

        public static bool IsLoad { get; private set; }

        private void OnApplicationQuit()
        {
            Save();
            Debug.Log("[SaveManager] Game Quit & Saved");
        }

        protected virtual void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #region Singleton Pattern

        protected override bool DontDestroyOnLoadEnabled => true;

        protected override void Init()
        {
            base.Init();
            RegisterAllSaveables();
            Load(); // авто-загрузка
            Debug.Log("[SaveManager] Initialized and Loaded");
            IsLoad = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        #endregion

        #region Serializable DTO

        [Serializable]
        private class SavedField
        {
            public string Key;
            public string TypeName;
            public string Value; // строка с JSON / строкой / числом (как текст)
        }

        [Serializable]
        private class SavedComponent
        {
            public string ComponentKey;
            public List<SavedField> Fields = new();
        }

        [Serializable]
        private class SaveDataContainer
        {
            public List<SavedComponent> AllSavedComponents = new();
        }

        // Обёртки для массивов/списков (JsonUtility требует поле, а не корневой массив)
        [Serializable]
        private class ArrayWrapper<T>
        {
            public T[] Items;
        }

        [Serializable]
        private class ListWrapper<T>
        {
            public List<T> Items = new();
        }

        #endregion

        #region Registration

        public static void Register(MonoBehaviour monoObj)
        {
            if (monoObj == null) return;

            var key = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
            if (_saveableComponents.ContainsKey(key)) return;

            var fieldsToSave = monoObj.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.IsDefined(typeof(SaveField), true))
                .ToList();

            if (fieldsToSave.Count > 0)
                _saveableComponents[key] = (monoObj, fieldsToSave);
        }

        public static void Unregister(MonoBehaviour monoObj)
        {
            if (monoObj == null) return;
            var key = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
            _saveableComponents.Remove(key);
        }

        private static List<MonoBehaviour> RegisterAllSaveables()
        {
            var newlyRegisteredComponents = new List<MonoBehaviour>();
            var allObjects = FindObjectsOfType<MonoBehaviour>(true);
            foreach (var obj in allObjects)
                if (obj is ISaveableComponent)
                {
                    var key = $"{obj.GetType().Name}_{obj.GetInstanceID()}";
                    if (!_saveableComponents.ContainsKey(key))
                    {
                        Register(obj);
                        newlyRegisteredComponents.Add(obj);
                    }
                }

            Debug.Log($"[SaveManager] saveable count: {_saveableComponents.Count}");
            return newlyRegisteredComponents;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var newlyRegistered = RegisterAllSaveables();
            Load(newlyRegistered); // только для новых объектов
            Debug.Log($"[SaveManager] Scene {scene.name} loaded. Re-registered & reloaded.");
        }

        #endregion

        #region Save/Load All

        public static void Save()
        {
            var container = new SaveDataContainer();

            foreach (var kvp in _saveableComponents)
            {
                var componentKey = kvp.Key;
                var (instance, fieldsToSave) = kvp.Value;
                if (instance == null) continue;

                var savedComponent = new SavedComponent { ComponentKey = componentKey };

                foreach (var field in fieldsToSave)
                {
                    var saveAttr = field.GetCustomAttribute<SaveField>(true);
                    if (saveAttr != null && saveAttr.AutoSaveOnQuit)
                    {
                        var value = field.GetValue(instance);
                        var savedField = new SavedField
                        {
                            Key = saveAttr.Key,
                            TypeName = field.FieldType.AssemblyQualifiedName,
                            Value = ValueToString(value, field.FieldType)
                        };
                        savedComponent.Fields.Add(savedField);
                    }
                }

                if (savedComponent.Fields.Count > 0)
                    container.AllSavedComponents.Add(savedComponent);
            }

            var jsonData = JsonUtility.ToJson(container, true);
            PlayerPrefs.SetString($"{saveDataKeyPrefix}All", jsonData);
            PlayerPrefs.Save();
        }

        public static void Load(List<MonoBehaviour> componentsToLoad = null)
        {
            var jsonData = PlayerPrefs.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            if (string.IsNullOrEmpty(jsonData) || jsonData == DefaultJson) return;

            try
            {
                var container = JsonUtility.FromJson<SaveDataContainer>(jsonData);
                if (container == null || container.AllSavedComponents == null) return;

                var loadedDataMap = container.AllSavedComponents.ToDictionary(c => c.ComponentKey);

                var targetComponents = componentsToLoad ?? _saveableComponents.Values.Select(x => x.instance).ToList();

                foreach (var monoObj in targetComponents)
                {
                    if (!monoObj) continue;

                    var componentKey = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
                    if (loadedDataMap.TryGetValue(componentKey, out var savedComponent)
                        && _saveableComponents.TryGetValue(componentKey, out var registeredData))
                    {
                        var instance = registeredData.instance;
                        var fields = registeredData.fields;

                        foreach (var savedField in savedComponent.Fields)
                        {
                            var field = fields.FirstOrDefault(f =>
                                f.GetCustomAttribute<SaveField>(true)?.Key == savedField.Key);
                            var saveAttr = field?.GetCustomAttribute<SaveField>(true);

                            if (field != null && saveAttr != null && saveAttr.AutoLoadOnAwake)
                            {
                                var fieldType = Type.GetType(savedField.TypeName);
                                if (fieldType != null && savedField.Value != null)
                                    try
                                    {
                                        var value = StringToValue(savedField.Value, fieldType);
                                        field.SetValue(instance, value);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogWarning(
                                            $"[SaveManager] Failed to load field '{savedField.Key}' ({fieldType}): {ex.Message}. Keep default.");
                                        // оставляем текущее значение (дефолт сцены)
                                    }
                            }
                        }

                        (instance as ISaveableComponent)?.OnDataLoaded();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading save data: " + e.Message + "\nStackTrace: " + e.StackTrace);
            }
        }

        #endregion

        #region Helper (Value <-> string)

        private static string ValueToString(object value, Type declaredType)
        {
            if (value == null) return null;

            var type = declaredType ?? value.GetType();

            // enum
            if (type.IsEnum) return value.ToString();

            // примитивы и строка
            if (type.IsPrimitive) return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (type == typeof(string)) return (string)value;

            // массивы
            if (type.IsArray)
            {
                var elemType = type.GetElementType();
                var wrapperType = typeof(ArrayWrapper<>).MakeGenericType(elemType);
                var wrapper = Activator.CreateInstance(wrapperType);
                // wrapper.Items = (T[])value;
                wrapperType.GetField("Items").SetValue(wrapper, value);
                return JsonUtility.ToJson(wrapper);
            }

            // List<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elemType = type.GetGenericArguments()[0];
                var wrapperType = typeof(ListWrapper<>).MakeGenericType(elemType);
                var wrapper = Activator.CreateInstance(wrapperType);

                // скопируем в wrapper.Items
                var list = (IEnumerable)value;
                var itemsField = wrapperType.GetField("Items");
                var targetList = itemsField.GetValue(wrapper); // это List<T>

                var addMethod = targetList.GetType().GetMethod("Add");
                foreach (var it in list)
                    addMethod.Invoke(targetList, new[] { it });

                return JsonUtility.ToJson(wrapper);
            }

            // всё остальное — как объект/структура
            return JsonUtility.ToJson(value);
        }

        private static object StringToValue(string s, Type type)
        {
            if (s == null) return null;

            // enum
            if (type.IsEnum) return Enum.Parse(type, s);

            // примитивы и строка
            if (type.IsPrimitive) return Convert.ChangeType(s, type, CultureInfo.InvariantCulture);
            if (type == typeof(string)) return s;

            // массивы
            if (type.IsArray)
            {
                var elemType = type.GetElementType();
                var wrapperType = typeof(ArrayWrapper<>).MakeGenericType(elemType);
                var wrapperObj = JsonUtility.FromJson(s, wrapperType);
                var items = wrapperType.GetField("Items").GetValue(wrapperObj);
                return items; // это T[]
            }

            // List<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elemType = type.GetGenericArguments()[0];
                var wrapperType = typeof(ListWrapper<>).MakeGenericType(elemType);
                var wrapperObj = JsonUtility.FromJson(s, wrapperType);
                var items = wrapperType.GetField("Items").GetValue(wrapperObj); // List<T>
                return items;
            }

            // объекты/структуры
            return JsonUtility.FromJson(s, type);
        }

        #endregion

        #region Single Object Save/Load

        public static void Save(MonoBehaviour monoObj, bool isSave = false)
        {
            if (monoObj == null || !(monoObj is ISaveableComponent))
            {
                Debug.LogWarning("[SaveManager] Cannot save: null or not ISaveableComponent.");
                return;
            }

            Register(monoObj);

            var componentKey = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
            if (!_saveableComponents.TryGetValue(componentKey, out var reg))
            {
                Debug.LogError($"[SaveManager] Could not save {componentKey}: not registered.");
                return;
            }

            var (instance, fieldsToSave) = reg;

            // читаем контейнер (валидный дефолт!)
            var currentJsonData = PlayerPrefs.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            SaveDataContainer container;
            try
            {
                container = JsonUtility.FromJson<SaveDataContainer>(currentJsonData) ?? new SaveDataContainer();
            }
            catch
            {
                container = new SaveDataContainer();
            }

            var savedComponent = container.AllSavedComponents.FirstOrDefault(c => c.ComponentKey == componentKey);
            if (savedComponent == null)
            {
                savedComponent = new SavedComponent { ComponentKey = componentKey };
                container.AllSavedComponents.Add(savedComponent);
            }

            savedComponent.Fields.Clear();

            foreach (var field in fieldsToSave)
            {
                var saveAttr = field.GetCustomAttribute<SaveField>(true);
                if (saveAttr != null)
                {
                    var fieldValue = field.GetValue(instance);
                    var savedField = new SavedField
                    {
                        Key = saveAttr.Key,
                        TypeName = field.FieldType.AssemblyQualifiedName,
                        Value = ValueToString(fieldValue, field.FieldType)
                    };
                    savedComponent.Fields.Add(savedField);
                }
            }

            var newJsonData = JsonUtility.ToJson(container, true);
            PlayerPrefs.SetString($"{saveDataKeyPrefix}All", newJsonData);
            if (isSave) PlayerPrefs.Save();

            Debug.Log($"[SaveManager] Manually saved {componentKey}");
        }

        public static void Load(MonoBehaviour monoObj)
        {
            if (monoObj == null || !(monoObj is ISaveableComponent))
            {
                Debug.LogWarning("[SaveManager] Cannot load: null or not ISaveableComponent.");
                return;
            }

            Register(monoObj);

            var componentKey = $"{monoObj.GetType().Name}_{monoObj.GetInstanceID()}";
            if (!_saveableComponents.TryGetValue(componentKey, out var reg))
            {
                Debug.LogWarning($"[SaveManager] No registered fields for {componentKey}");
                return;
            }

            var fields = reg.fields;

            var jsonData = PlayerPrefs.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            if (string.IsNullOrEmpty(jsonData) || jsonData == DefaultJson) return;

            try
            {
                var container = JsonUtility.FromJson<SaveDataContainer>(jsonData);
                var savedComponent = container.AllSavedComponents.FirstOrDefault(c => c.ComponentKey == componentKey);
                if (savedComponent != null)
                {
                    foreach (var savedField in savedComponent.Fields)
                    {
                        var field = fields.FirstOrDefault(f =>
                            f.GetCustomAttribute<SaveField>(true)?.Key == savedField.Key);
                        if (field == null) continue;

                        var fieldType = Type.GetType(savedField.TypeName);
                        if (fieldType != null && savedField.Value != null)
                            try
                            {
                                var value = StringToValue(savedField.Value, fieldType);
                                field.SetValue(monoObj, value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning(
                                    $"[SaveManager] Load field '{savedField.Key}' failed: {ex.Message}. Keep default.");
                            }
                    }

                    (monoObj as ISaveableComponent)?.OnDataLoaded();
                    Debug.Log($"[SaveManager] Manually loaded {componentKey}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading save data for {componentKey}: " + e.Message);
            }
        }

        #endregion
    }
}