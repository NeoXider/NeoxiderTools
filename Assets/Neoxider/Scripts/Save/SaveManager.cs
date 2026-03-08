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
    [CreateFromMenu("Neoxider/Save/SaveManager")]
    [AddComponentMenu("Neoxider/Save/" + nameof(SaveManager))]
    [NeoDoc("Save/README.md")]
    public class SaveManager : Singleton<SaveManager>
    {
        private const string DefaultJson = "{\"AllSavedComponents\":[]}";

        private const string saveDataKeyPrefix = "SaveData_";

        private static readonly Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> _saveableComponents
            = new();

        /// <summary>
        /// Gets whether the manager has completed its initial load pass.
        /// </summary>
        public static bool IsLoad { get; private set; }

        protected virtual void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            Save();
            Debug.Log("[SaveManager] Game Quit & Saved");
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

        /// <summary>
        /// Registers a saveable component and caches all fields marked with <see cref="SaveField"/>.
        /// </summary>
        /// <param name="monoObj">Component to register.</param>
        public static void Register(MonoBehaviour monoObj)
        {
            if (monoObj == null)
            {
                return;
            }

            CleanupDestroyedRegistrations();

            string key = SaveIdentityUtility.GetComponentKey(monoObj);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_saveableComponents.TryGetValue(key, out (MonoBehaviour instance, List<FieldInfo> fields) existingData))
            {
                if (existingData.instance == monoObj)
                {
                    return;
                }

                Debug.LogWarning($"[SaveManager] Duplicate save identity detected: {key}", monoObj);
                return;
            }

            List<FieldInfo> fieldsToSave = monoObj.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.IsDefined(typeof(SaveField), true))
                .ToList();

            if (fieldsToSave.Count > 0)
            {
                _saveableComponents[key] = (monoObj, fieldsToSave);
            }
        }

        /// <summary>
        /// Removes a component from the save registry.
        /// </summary>
        /// <param name="monoObj">Component to unregister.</param>
        public static void Unregister(MonoBehaviour monoObj)
        {
            if (monoObj == null)
            {
                return;
            }

            string key = SaveIdentityUtility.GetComponentKey(monoObj);
            if (!string.IsNullOrEmpty(key))
            {
                _saveableComponents.Remove(key);
            }
        }

        private static List<MonoBehaviour> RegisterAllSaveables()
        {
            CleanupDestroyedRegistrations();
            List<MonoBehaviour> newlyRegisteredComponents = new();
            MonoBehaviour[] allObjects =
                FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MonoBehaviour obj in allObjects)
            {
                if (obj is ISaveableComponent)
                {
                    string key = SaveIdentityUtility.GetComponentKey(obj);
                    if (!_saveableComponents.ContainsKey(key))
                    {
                        Register(obj);
                        newlyRegisteredComponents.Add(obj);
                    }
                }
            }

            Debug.Log($"[SaveManager] saveable count: {_saveableComponents.Count}");
            return newlyRegisteredComponents;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            List<MonoBehaviour> newlyRegistered = RegisterAllSaveables();
            Load(newlyRegistered); // только для новых объектов
            Debug.Log($"[SaveManager] Scene {scene.name} loaded. Re-registered & reloaded.");
        }

        #endregion

        #region Save/Load All

        /// <summary>
        /// Saves all currently registered components.
        /// </summary>
        public static void Save()
        {
            CleanupDestroyedRegistrations();
            SaveDataContainer container = new();

            foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> kvp in _saveableComponents)
            {
                string componentKey = kvp.Key;
                (MonoBehaviour instance, List<FieldInfo> fieldsToSave) = kvp.Value;
                if (instance == null)
                {
                    continue;
                }

                SavedComponent savedComponent = new() { ComponentKey = componentKey };

                foreach (FieldInfo field in fieldsToSave)
                {
                    SaveField saveAttr = field.GetCustomAttribute<SaveField>(true);
                    if (saveAttr != null && saveAttr.AutoSaveOnQuit)
                    {
                        object value = field.GetValue(instance);
                        SavedField savedField = new()
                        {
                            Key = saveAttr.Key,
                            TypeName = field.FieldType.AssemblyQualifiedName,
                            Value = ValueToString(value, field.FieldType)
                        };
                        savedComponent.Fields.Add(savedField);
                    }
                }

                if (savedComponent.Fields.Count > 0)
                {
                    container.AllSavedComponents.Add(savedComponent);
                }
            }

            string jsonData = JsonUtility.ToJson(container, true);
            SaveProvider.SetString($"{saveDataKeyPrefix}All", jsonData);
        }

        /// <summary>
        /// Loads data for the provided components, or for all registered components when no list is supplied.
        /// </summary>
        /// <param name="componentsToLoad">Optional subset of components to load.</param>
        public static void Load(List<MonoBehaviour> componentsToLoad = null)
        {
            CleanupDestroyedRegistrations();
            string jsonData = SaveProvider.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            if (string.IsNullOrEmpty(jsonData) || jsonData == DefaultJson)
            {
                return;
            }

            try
            {
                SaveDataContainer container = JsonUtility.FromJson<SaveDataContainer>(jsonData);
                if (container == null || container.AllSavedComponents == null)
                {
                    return;
                }

                Dictionary<string, SavedComponent> loadedDataMap =
                    container.AllSavedComponents.ToDictionary(c => c.ComponentKey);

                List<MonoBehaviour> targetComponents =
                    componentsToLoad ?? _saveableComponents.Values.Select(x => x.instance).ToList();

                foreach (MonoBehaviour monoObj in targetComponents)
                {
                    if (!monoObj)
                    {
                        continue;
                    }

                    string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
                    if (loadedDataMap.TryGetValue(componentKey, out SavedComponent savedComponent)
                        && _saveableComponents.TryGetValue(componentKey,
                            out (MonoBehaviour instance, List<FieldInfo> fields) registeredData))
                    {
                        MonoBehaviour instance = registeredData.instance;
                        List<FieldInfo> fields = registeredData.fields;

                        foreach (SavedField savedField in savedComponent.Fields)
                        {
                            FieldInfo field = fields.FirstOrDefault(f =>
                                f.GetCustomAttribute<SaveField>(true)?.Key == savedField.Key);
                            SaveField saveAttr = field?.GetCustomAttribute<SaveField>(true);

                            if (field != null && saveAttr != null && saveAttr.AutoLoadOnAwake)
                            {
                                Type fieldType = Type.GetType(savedField.TypeName);
                                if (fieldType != null && savedField.Value != null)
                                {
                                    try
                                    {
                                        object value = StringToValue(savedField.Value, fieldType);
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
            if (value == null)
            {
                return null;
            }

            Type type = declaredType ?? value.GetType();

            // enum
            if (type.IsEnum)
            {
                return value.ToString();
            }

            // примитивы и строка
            if (type.IsPrimitive)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (type == typeof(string))
            {
                return (string)value;
            }

            // массивы
            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                Type wrapperType = typeof(ArrayWrapper<>).MakeGenericType(elemType);
                object wrapper = Activator.CreateInstance(wrapperType);
                // wrapper.Items = (T[])value;
                wrapperType.GetField("Items").SetValue(wrapper, value);
                return JsonUtility.ToJson(wrapper);
            }

            // List<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elemType = type.GetGenericArguments()[0];
                Type wrapperType = typeof(ListWrapper<>).MakeGenericType(elemType);
                object wrapper = Activator.CreateInstance(wrapperType);

                // скопируем в wrapper.Items
                IEnumerable list = (IEnumerable)value;
                FieldInfo itemsField = wrapperType.GetField("Items");
                object targetList = itemsField.GetValue(wrapper); // это List<T>

                MethodInfo addMethod = targetList.GetType().GetMethod("Add");
                foreach (object it in list)
                {
                    addMethod.Invoke(targetList, new[] { it });
                }

                return JsonUtility.ToJson(wrapper);
            }

            // всё остальное — как объект/структура
            return JsonUtility.ToJson(value);
        }

        private static object StringToValue(string s, Type type)
        {
            if (s == null)
            {
                return null;
            }

            // enum
            if (type.IsEnum)
            {
                return Enum.Parse(type, s);
            }

            // примитивы и строка
            if (type.IsPrimitive)
            {
                return Convert.ChangeType(s, type, CultureInfo.InvariantCulture);
            }

            if (type == typeof(string))
            {
                return s;
            }

            // массивы
            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                Type wrapperType = typeof(ArrayWrapper<>).MakeGenericType(elemType);
                object wrapperObj = JsonUtility.FromJson(s, wrapperType);
                object items = wrapperType.GetField("Items").GetValue(wrapperObj);
                return items; // это T[]
            }

            // List<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elemType = type.GetGenericArguments()[0];
                Type wrapperType = typeof(ListWrapper<>).MakeGenericType(elemType);
                object wrapperObj = JsonUtility.FromJson(s, wrapperType);
                object items = wrapperType.GetField("Items").GetValue(wrapperObj); // List<T>
                return items;
            }

            // объекты/структуры
            return JsonUtility.FromJson(s, type);
        }

        #endregion

        #region Single Object Save/Load

        /// <summary>
        /// Saves a single component into the shared save container.
        /// </summary>
        /// <param name="monoObj">Component to save.</param>
        /// <param name="isSave">Reserved compatibility flag.</param>
        public static void Save(MonoBehaviour monoObj, bool isSave = false)
        {
            if (monoObj == null || !(monoObj is ISaveableComponent))
            {
                Debug.LogWarning("[SaveManager] Cannot save: null or not ISaveableComponent.");
                return;
            }

            Register(monoObj);

            string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
            if (!_saveableComponents.TryGetValue(componentKey,
                    out (MonoBehaviour instance, List<FieldInfo> fields) reg))
            {
                Debug.LogError($"[SaveManager] Could not save {componentKey}: not registered.");
                return;
            }

            (MonoBehaviour instance, List<FieldInfo> fieldsToSave) = reg;

            // читаем контейнер (валидный дефолт!)
            string currentJsonData = SaveProvider.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            SaveDataContainer container;
            try
            {
                container = JsonUtility.FromJson<SaveDataContainer>(currentJsonData) ?? new SaveDataContainer();
            }
            catch
            {
                container = new SaveDataContainer();
            }

            SavedComponent savedComponent =
                container.AllSavedComponents.FirstOrDefault(c => c.ComponentKey == componentKey);
            if (savedComponent == null)
            {
                savedComponent = new SavedComponent { ComponentKey = componentKey };
                container.AllSavedComponents.Add(savedComponent);
            }

            savedComponent.Fields.Clear();

            foreach (FieldInfo field in fieldsToSave)
            {
                SaveField saveAttr = field.GetCustomAttribute<SaveField>(true);
                if (saveAttr != null)
                {
                    object fieldValue = field.GetValue(instance);
                    SavedField savedField = new()
                    {
                        Key = saveAttr.Key,
                        TypeName = field.FieldType.AssemblyQualifiedName,
                        Value = ValueToString(fieldValue, field.FieldType)
                    };
                    savedComponent.Fields.Add(savedField);
                }
            }

            string newJsonData = JsonUtility.ToJson(container, true);
            SaveProvider.SetString($"{saveDataKeyPrefix}All", newJsonData);

            Debug.Log($"[SaveManager] Manually saved {componentKey}");
        }

        /// <summary>
        /// Loads data for a single registered component.
        /// </summary>
        /// <param name="monoObj">Component to load.</param>
        public static void Load(MonoBehaviour monoObj)
        {
            if (monoObj == null || !(monoObj is ISaveableComponent))
            {
                Debug.LogWarning("[SaveManager] Cannot load: null or not ISaveableComponent.");
                return;
            }

            Register(monoObj);

            string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
            if (!_saveableComponents.TryGetValue(componentKey,
                    out (MonoBehaviour instance, List<FieldInfo> fields) reg))
            {
                Debug.LogWarning($"[SaveManager] No registered fields for {componentKey}");
                return;
            }

            List<FieldInfo> fields = reg.fields;

            string jsonData = SaveProvider.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            if (string.IsNullOrEmpty(jsonData) || jsonData == DefaultJson)
            {
                return;
            }

            try
            {
                SaveDataContainer container = JsonUtility.FromJson<SaveDataContainer>(jsonData);
                SavedComponent savedComponent =
                    container.AllSavedComponents.FirstOrDefault(c => c.ComponentKey == componentKey);
                if (savedComponent != null)
                {
                    foreach (SavedField savedField in savedComponent.Fields)
                    {
                        FieldInfo field = fields.FirstOrDefault(f =>
                            f.GetCustomAttribute<SaveField>(true)?.Key == savedField.Key);
                        if (field == null)
                        {
                            continue;
                        }

                        Type fieldType = Type.GetType(savedField.TypeName);
                        if (fieldType != null && savedField.Value != null)
                        {
                            try
                            {
                                object value = StringToValue(savedField.Value, fieldType);
                                field.SetValue(monoObj, value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning(
                                    $"[SaveManager] Load field '{savedField.Key}' failed: {ex.Message}. Keep default.");
                            }
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

        private static void CleanupDestroyedRegistrations()
        {
            if (_saveableComponents.Count == 0)
            {
                return;
            }

            List<string> invalidKeys = _saveableComponents
                .Where(pair => pair.Value.instance == null)
                .Select(pair => pair.Key)
                .ToList();

            for (int i = 0; i < invalidKeys.Count; i++)
            {
                _saveableComponents.Remove(invalidKeys[i]);
            }
        }

        #endregion
    }
}