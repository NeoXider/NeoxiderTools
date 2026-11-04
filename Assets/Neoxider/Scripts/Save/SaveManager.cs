using System;
using System.Collections.Generic;
using System.Globalization;
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

        // WHY: [SaveField] metadata is per type; resolving it once avoids reflection and attribute allocations on
        // every registration, save and load of each saveable instance.
        private static readonly Dictionary<Type, TypeBindings> _bindingsByType = new();
        private static readonly Dictionary<Type, FieldInfo> _wrapperItemsFields = new();
        private static readonly List<string> _invalidKeysBuffer = new();

        [Header("Diagnostics")] [SerializeField]
        private bool _debugLog;

        private static bool DebugLogEnabled => HasInstance && I._debugLog;

        /// <summary>
        ///     Clears static caches across subsystem / domain reloads. Invoked from
        ///     <see cref="SaveManagerSubsystemRegistration"/> rather than via
        ///     <see cref="RuntimeInitializeOnLoadMethodAttribute"/> on this type —
        ///     Unity forbids runtime-init hooks on inheritance chains rooted in generics such as
        ///     <c>Neo.Tools.Singleton&lt;T&gt;</c>.
        /// </summary>
        public static void ClearSubsystemCaches()
        {
            _saveableComponents.Clear();
            _bindingsByType.Clear();
            _wrapperItemsFields.Clear();
            IsLoad = false;
        }

        /// <summary>
        ///     Gets whether the manager has completed its initial load pass.
        /// </summary>
        public static bool IsLoad { get; private set; }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            FlushNow("Game Quit");
        }

        /// <summary>
        ///     WHY: Android and iOS routinely kill a backgrounded process without ever calling
        ///     <c>OnApplicationQuit</c>, so pausing is the last guaranteed moment to persist the session.
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushNow("Application Paused");
            }
        }

        /// <summary>
        ///     Desktop and editor counterpart of <see cref="OnApplicationPause" />: losing focus is the
        ///     closest equivalent signal there.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // WHY: not deduplicated against a pause in the same frame; other components' focus and pause
                // handlers run between the two messages and may stage data that only the later flush persists.
                // An unchanged second flush costs no disk write with FileSaveProvider.
                FlushNow("Focus Lost");
            }
        }

        /// <summary>
        ///     Writes every registered saveable and flushes the provider to its backing store.
        ///     Safe to call at any time; call it manually before a risky operation.
        /// </summary>
        public void FlushNow()
        {
            FlushNow("Manual Flush");
        }

        private void FlushNow(string reason)
        {
            Save();
            // WHY: SetString/SetInt only stage values in the provider; file-backed providers
            // write to disk exclusively on an explicit flush.
            SaveProvider.Save();
            if (_debugLog)
            {
                SaveProvider.Log($"[SaveManager] {reason} & Saved", this);
            }
        }

        #region Singleton Pattern

        protected override bool DontDestroyOnLoadEnabled => true;

        protected override void Init()
        {
            base.Init();
            RegisterAllSaveables();
            Load();
            if (_debugLog)
            {
                SaveProvider.Log("[SaveManager] Initialized and Loaded", this);
            }

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
            public string Value; // WHY: encodes JSON / string / number as text
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

        // WHY: wrapper needed because JsonUtility requires a field, not a root array/list
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

        /// <summary>One [SaveField] of a component type with its attribute and serialized type name resolved once.</summary>
        private sealed class SaveFieldBinding
        {
            public readonly FieldInfo Field;
            public readonly SaveField Attribute;
            public readonly string TypeName;

            public SaveFieldBinding(FieldInfo field, SaveField attribute)
            {
                Field = field;
                Attribute = attribute;
                TypeName = field.FieldType.AssemblyQualifiedName;
            }
        }

        /// <summary>All [SaveField] bindings of one component type, most derived type first.</summary>
        private sealed class TypeBindings
        {
            public readonly SaveFieldBinding[] Bindings;

            // WHY: shared by every registration of the type; the registry only reads it.
            public readonly List<FieldInfo> Fields;

            public TypeBindings(List<SaveFieldBinding> bindings)
            {
                Bindings = bindings.ToArray();
                Fields = new List<FieldInfo>(Bindings.Length);
                for (int i = 0; i < Bindings.Length; i++)
                {
                    Fields.Add(Bindings[i].Field);
                }
            }
        }

        #endregion

        #region Registration

        /// <summary>
        ///     Registers a saveable component and caches all fields marked with <see cref="SaveField" />.
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

                SaveProvider.LogWarning($"[SaveManager] Duplicate save identity detected: {key}", monoObj);
                return;
            }

            TypeBindings bindings = GetBindings(monoObj.GetType());
            if (bindings.Fields.Count > 0)
            {
                _saveableComponents[key] = (monoObj, bindings.Fields);
            }
        }

        private static TypeBindings GetBindings(Type componentType)
        {
            if (_bindingsByType.TryGetValue(componentType, out TypeBindings cached))
            {
                return cached;
            }

            List<SaveFieldBinding> bindings = new();
            for (Type type = componentType; type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                FieldInfo[] declared = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                      BindingFlags.Instance | BindingFlags.DeclaredOnly);
                for (int i = 0; i < declared.Length; i++)
                {
                    SaveField attribute = declared[i].GetCustomAttribute<SaveField>(true);
                    if (attribute != null)
                    {
                        bindings.Add(new SaveFieldBinding(declared[i], attribute));
                    }
                }
            }

            TypeBindings result = new(bindings);
            _bindingsByType[componentType] = result;
            return result;
        }

        private static SaveFieldBinding FindBinding(SaveFieldBinding[] bindings, string key)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                if (string.Equals(bindings[i].Attribute.Key, key, StringComparison.Ordinal))
                {
                    return bindings[i];
                }
            }

            return null;
        }

        private static SavedField FindSavedField(SavedComponent component, string key)
        {
            List<SavedField> fields = component?.Fields;
            if (fields == null)
            {
                return null;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                SavedField field = fields[i];
                if (field != null && string.Equals(field.Key, key, StringComparison.Ordinal))
                {
                    return field;
                }
            }

            return null;
        }

        private static SavedComponent FindSavedComponent(SaveDataContainer container, string componentKey)
        {
            List<SavedComponent> components = container.AllSavedComponents;
            for (int i = 0; i < components.Count; i++)
            {
                SavedComponent component = components[i];
                if (component != null && string.Equals(component.ComponentKey, componentKey, StringComparison.Ordinal))
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        ///     Resolves the stored type of a field. The common case (type unchanged since the save) reuses the
        ///     field's own type instead of parsing the assembly-qualified name.
        /// </summary>
        private static Type ResolveSavedType(SavedField savedField, SaveFieldBinding binding)
        {
            if (string.IsNullOrEmpty(savedField.TypeName))
            {
                return null;
            }

            return string.Equals(savedField.TypeName, binding.TypeName, StringComparison.Ordinal)
                ? binding.Field.FieldType
                : Type.GetType(savedField.TypeName);
        }

        /// <summary>
        ///     Removes a component from the save registry.
        /// </summary>
        /// <param name="monoObj">Component to unregister.</param>
        public static void Unregister(MonoBehaviour monoObj)
        {
            if (monoObj == null)
            {
                return;
            }

            string key = SaveIdentityUtility.GetComponentKey(monoObj);
            if (!string.IsNullOrEmpty(key)
                && _saveableComponents.TryGetValue(key, out (MonoBehaviour instance, List<FieldInfo> fields) registered)
                && ReferenceEquals(registered.instance, monoObj))
            {
                _saveableComponents.Remove(key);
            }
        }

        private static List<MonoBehaviour> RegisterAllSaveables(Scene? sceneToLoad = null)
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
                    else if (sceneToLoad.HasValue && obj.gameObject.scene == sceneToLoad.Value)
                    {
                        // OnEnable can register scene objects before Unity raises sceneLoaded.
                        newlyRegisteredComponents.Add(obj);
                    }
                }
            }

            if (DebugLogEnabled)
            {
                SaveProvider.Log($"[SaveManager] saveable count: {_saveableComponents.Count}", I);
            }

            return newlyRegisteredComponents;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            List<MonoBehaviour> newlyRegistered = RegisterAllSaveables(scene);
            Load(newlyRegistered);
            if (_debugLog)
            {
                SaveProvider.Log($"[SaveManager] Scene {scene.name} loaded. Re-registered & reloaded.", this);
            }
        }

        #endregion

        #region Save/Load All

        /// <summary>
        ///     Saves all currently registered components.
        /// </summary>
        public static void Save()
        {
            CleanupDestroyedRegistrations();
            SaveDataContainer container = ReadAllSaveDataContainer();

            // WHY: keep the previously stored entries around — fields with AutoSaveOnQuit=false are
            // persisted only via Save(component); rebuilding without them would silently erase them.
            Dictionary<string, SavedComponent> existingByKey = new();
            foreach (SavedComponent component in container.AllSavedComponents)
            {
                if (component?.ComponentKey != null && _saveableComponents.ContainsKey(component.ComponentKey))
                {
                    existingByKey[component.ComponentKey] = component;
                }
            }

            container.AllSavedComponents.RemoveAll(component =>
                component == null
                || (component.ComponentKey != null && _saveableComponents.ContainsKey(component.ComponentKey)));

            foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> kvp in _saveableComponents)
            {
                string componentKey = kvp.Key;
                MonoBehaviour instance = kvp.Value.instance;
                if (instance == null)
                {
                    continue;
                }

                existingByKey.TryGetValue(componentKey, out SavedComponent existing);
                SavedComponent savedComponent = BuildAutoSaveComponent(componentKey, instance,
                    GetBindings(instance.GetType()).Bindings, existing);

                if (savedComponent.Fields.Count > 0)
                {
                    container.AllSavedComponents.Add(savedComponent);
                }
            }

            string jsonData = JsonUtility.ToJson(container, true);
            SaveProvider.SetString($"{saveDataKeyPrefix}All", jsonData);
        }

        /// <summary>
        ///     Builds the stored entry for one component: current values for auto-saved fields, and the
        ///     previously stored values for fields that opted out of auto-save (AutoSaveOnQuit=false).
        /// </summary>
        private static SavedComponent BuildAutoSaveComponent(string componentKey, MonoBehaviour instance,
            SaveFieldBinding[] bindings, SavedComponent existing)
        {
            SavedComponent savedComponent = new() { ComponentKey = componentKey };

            for (int i = 0; i < bindings.Length; i++)
            {
                SaveFieldBinding binding = bindings[i];
                if (binding.Attribute.AutoSaveOnQuit)
                {
                    savedComponent.Fields.Add(CreateSavedField(binding, instance));
                }
                else
                {
                    SavedField preserved = FindSavedField(existing, binding.Attribute.Key);
                    if (preserved != null)
                    {
                        savedComponent.Fields.Add(preserved);
                    }
                }
            }

            return savedComponent;
        }

        private static SavedField CreateSavedField(SaveFieldBinding binding, MonoBehaviour instance)
        {
            return new SavedField
            {
                Key = binding.Attribute.Key,
                TypeName = binding.TypeName,
                Value = ValueToString(binding.Field.GetValue(instance), binding.Field.FieldType)
            };
        }

        /// <summary>
        ///     Persists the auto-saved fields ([SaveField] AutoSaveOnQuit=true) of a single registered
        ///     component, keeping its manually saved fields intact. Called before a component leaves the
        ///     registry (e.g. <see cref="SaveableBehaviour.OnDisable" />) so session changes are not lost
        ///     when the object is inactive during the global quit save.
        /// </summary>
        /// <param name="monoObj">Registered component to flush.</param>
        public static void SaveAutoFields(MonoBehaviour monoObj)
        {
            if (monoObj == null)
            {
                return;
            }

            string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
            if (string.IsNullOrEmpty(componentKey)
                || !_saveableComponents.TryGetValue(componentKey,
                    out (MonoBehaviour instance, List<FieldInfo> fields) reg)
                || !ReferenceEquals(reg.instance, monoObj))
            {
                return;
            }

            SaveDataContainer container = ReadAllSaveDataContainer();
            SavedComponent existing = FindSavedComponent(container, componentKey);
            SavedComponent savedComponent = BuildAutoSaveComponent(componentKey, monoObj,
                GetBindings(monoObj.GetType()).Bindings, existing);

            container.AllSavedComponents.RemoveAll(c => c == null || c.ComponentKey == componentKey);
            if (savedComponent.Fields.Count > 0)
            {
                container.AllSavedComponents.Add(savedComponent);
            }

            SaveProvider.SetString($"{saveDataKeyPrefix}All", JsonUtility.ToJson(container, true));
        }

        private static SaveDataContainer ReadAllSaveDataContainer()
        {
            string currentJsonData = SaveProvider.GetString($"{saveDataKeyPrefix}All", DefaultJson);
            if (string.IsNullOrEmpty(currentJsonData))
            {
                return new SaveDataContainer();
            }

            try
            {
                SaveDataContainer container = JsonUtility.FromJson<SaveDataContainer>(currentJsonData);
                container ??= new SaveDataContainer();
                container.AllSavedComponents ??= new List<SavedComponent>();
                HashSet<string> componentKeys = new();
                for (int index = container.AllSavedComponents.Count - 1; index >= 0; index--)
                {
                    SavedComponent component = container.AllSavedComponents[index];
                    if (component == null || string.IsNullOrEmpty(component.ComponentKey)
                                          || !componentKeys.Add(component.ComponentKey))
                    {
                        container.AllSavedComponents.RemoveAt(index);
                        continue;
                    }

                    component.Fields ??= new List<SavedField>();
                    component.Fields.RemoveAll(field => field == null);
                }

                return container;
            }
            catch
            {
                return new SaveDataContainer();
            }
        }

        /// <summary>
        ///     Loads data for the provided components, or for all registered components when no list is supplied.
        /// </summary>
        /// <param name="componentsToLoad">Optional subset of components to load.</param>
        public static void Load(List<MonoBehaviour> componentsToLoad = null)
        {
            CleanupDestroyedRegistrations();
            try
            {
                SaveDataContainer container = ReadAllSaveDataContainer();

                // WHY: a hand-edited or legacy save file may contain duplicate or null ComponentKeys;
                // ToDictionary would throw and abort the whole load, so build the lookup tolerantly (last wins).
                Dictionary<string, SavedComponent> loadedDataMap = new();
                foreach (SavedComponent saved in container.AllSavedComponents)
                {
                    if (saved?.ComponentKey != null)
                    {
                        loadedDataMap[saved.ComponentKey] = saved;
                    }
                }

                List<MonoBehaviour> targetComponents = componentsToLoad;
                if (targetComponents == null)
                {
                    // WHY: OnDataLoaded callbacks may register or unregister components, so iterate a snapshot.
                    targetComponents = new List<MonoBehaviour>(_saveableComponents.Count);
                    foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> entry in
                             _saveableComponents)
                    {
                        targetComponents.Add(entry.Value.instance);
                    }
                }

                foreach (MonoBehaviour monoObj in targetComponents)
                {
                    if (!monoObj)
                    {
                        continue;
                    }

                    string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
                    if (loadedDataMap.TryGetValue(componentKey, out SavedComponent savedComponent)
                        && _saveableComponents.TryGetValue(componentKey,
                            out (MonoBehaviour instance, List<FieldInfo> fields) registeredData)
                        && ReferenceEquals(registeredData.instance, monoObj))
                    {
                        MonoBehaviour instance = registeredData.instance;
                        SaveFieldBinding[] bindings = GetBindings(instance.GetType()).Bindings;

                        foreach (SavedField savedField in savedComponent.Fields)
                        {
                            SaveFieldBinding binding = FindBinding(bindings, savedField.Key);
                            if (binding != null && binding.Attribute.AutoLoadOnAwake)
                            {
                                try
                                {
                                    Type fieldType = ResolveSavedType(savedField, binding);
                                    if (fieldType != null && savedField.Value != null)
                                    {
                                        object value = StringToValue(savedField.Value, fieldType);
                                        binding.Field.SetValue(instance, value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SaveProvider.LogWarning(
                                        $"[SaveManager] Failed to load field '{savedField.Key}': {ex.Message}. Keep default.");
                                }
                            }
                        }

                        NotifyDataLoaded(instance);
                    }
                }
            }
            catch (Exception e)
            {
                SaveProvider.LogError("Error loading save data: " + e.Message + "\nStackTrace: " + e.StackTrace);
            }
        }

        /// <summary>
        ///     Raises <see cref="ISaveableComponent.OnDataLoaded" />. A throwing callback is logged as an error and does
        ///     not stop the remaining components of the same load from being restored.
        /// </summary>
        private static void NotifyDataLoaded(MonoBehaviour instance)
        {
            if (!(instance is ISaveableComponent saveable))
            {
                return;
            }

            try
            {
                saveable.OnDataLoaded();
            }
            catch (Exception exception)
            {
                // WHY: the bulk load's outer handler logs only with save debug logging on, so one throwing callback
                // used to leave every later component unrestored without any visible error.
                NeoDiagnostics.LogException(exception, instance);
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

            if (type.IsEnum)
            {
                return value.ToString();
            }

            if (type.IsPrimitive)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (type == typeof(string))
            {
                return (string)value;
            }

            if (IsWrappedCollection(type))
            {
                // WHY: the wrapper only gives JsonUtility a root field; it serializes the stored collection itself,
                // so no copy of the list is needed.
                FieldInfo itemsField = GetWrapperItemsField(type);
                object wrapper = Activator.CreateInstance(itemsField.DeclaringType);
                itemsField.SetValue(wrapper, value);
                return JsonUtility.ToJson(wrapper);
            }

            return JsonUtility.ToJson(value);
        }

        private static bool IsWrappedCollection(Type type)
        {
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }

        /// <summary>
        ///     Resolves the <c>Items</c> field of the JSON wrapper for an array or <c>List&lt;T&gt;</c> type once per
        ///     type, instead of building the closed generic wrapper type on every save and load.
        /// </summary>
        private static FieldInfo GetWrapperItemsField(Type collectionType)
        {
            if (_wrapperItemsFields.TryGetValue(collectionType, out FieldInfo cached))
            {
                return cached;
            }

            Type wrapperType = collectionType.IsArray
                ? typeof(ArrayWrapper<>).MakeGenericType(collectionType.GetElementType())
                : typeof(ListWrapper<>).MakeGenericType(collectionType.GetGenericArguments()[0]);
            FieldInfo itemsField = wrapperType.GetField(nameof(ArrayWrapper<int>.Items));
            _wrapperItemsFields[collectionType] = itemsField;
            return itemsField;
        }

        private static object StringToValue(string s, Type type)
        {
            if (s == null)
            {
                return null;
            }

            if (type.IsEnum)
            {
                return Enum.Parse(type, s);
            }

            if (type.IsPrimitive)
            {
                return Convert.ChangeType(s, type, CultureInfo.InvariantCulture);
            }

            if (type == typeof(string))
            {
                return s;
            }

            if (IsWrappedCollection(type))
            {
                FieldInfo itemsField = GetWrapperItemsField(type);
                object wrapperObj = JsonUtility.FromJson(s, itemsField.DeclaringType);
                return itemsField.GetValue(wrapperObj); // WHY: runtime type is T[] or List<T>
            }

            return JsonUtility.FromJson(s, type);
        }

        #endregion

        #region Single Object Save/Load

        /// <summary>
        ///     Saves a single component into the shared save container.
        /// </summary>
        /// <param name="monoObj">Component to save.</param>
        /// <param name="isSave">Reserved compatibility flag.</param>
        public static void Save(MonoBehaviour monoObj, bool isSave = false)
        {
            if (monoObj == null || !(monoObj is ISaveableComponent))
            {
                SaveProvider.LogWarning("[SaveManager] Cannot save: null or not ISaveableComponent.");
                return;
            }

            Register(monoObj);

            string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
            if (!_saveableComponents.TryGetValue(componentKey,
                    out (MonoBehaviour instance, List<FieldInfo> fields) reg)
                || !ReferenceEquals(reg.instance, monoObj))
            {
                SaveProvider.LogError($"[SaveManager] Could not save {componentKey}: not registered.");
                return;
            }

            MonoBehaviour instance = reg.instance;
            SaveFieldBinding[] bindings = GetBindings(instance.GetType()).Bindings;

            SaveDataContainer container = ReadAllSaveDataContainer();

            SavedComponent savedComponent = FindSavedComponent(container, componentKey);
            if (savedComponent == null)
            {
                savedComponent = new SavedComponent { ComponentKey = componentKey };
                container.AllSavedComponents.Add(savedComponent);
            }

            savedComponent.Fields.Clear();

            for (int i = 0; i < bindings.Length; i++)
            {
                savedComponent.Fields.Add(CreateSavedField(bindings[i], instance));
            }

            string newJsonData = JsonUtility.ToJson(container, true);
            SaveProvider.SetString($"{saveDataKeyPrefix}All", newJsonData);

            if (DebugLogEnabled)
            {
                SaveProvider.Log($"[SaveManager] Manually saved {componentKey}", I);
            }
        }

        /// <summary>
        ///     Loads data for a single registered component.
        /// </summary>
        /// <param name="monoObj">Component to load.</param>
        public static void Load(MonoBehaviour monoObj)
        {
            if (monoObj == null || !(monoObj is ISaveableComponent))
            {
                SaveProvider.LogWarning("[SaveManager] Cannot load: null or not ISaveableComponent.");
                return;
            }

            Register(monoObj);

            string componentKey = SaveIdentityUtility.GetComponentKey(monoObj);
            if (!_saveableComponents.TryGetValue(componentKey,
                    out (MonoBehaviour instance, List<FieldInfo> fields) reg)
                || !ReferenceEquals(reg.instance, monoObj))
            {
                SaveProvider.LogWarning($"[SaveManager] No registered fields for {componentKey}");
                return;
            }

            SaveFieldBinding[] bindings = GetBindings(monoObj.GetType()).Bindings;

            try
            {
                SaveDataContainer container = ReadAllSaveDataContainer();
                SavedComponent savedComponent = FindSavedComponent(container, componentKey);
                if (savedComponent != null)
                {
                    foreach (SavedField savedField in savedComponent.Fields)
                    {
                        SaveFieldBinding binding = FindBinding(bindings, savedField.Key);
                        if (binding == null)
                        {
                            continue;
                        }

                        try
                        {
                            Type fieldType = ResolveSavedType(savedField, binding);
                            if (fieldType != null && savedField.Value != null)
                            {
                                object value = StringToValue(savedField.Value, fieldType);
                                binding.Field.SetValue(monoObj, value);
                            }
                        }
                        catch (Exception ex)
                        {
                            SaveProvider.LogWarning(
                                $"[SaveManager] Load field '{savedField.Key}' failed: {ex.Message}. Keep default.");
                        }
                    }

                    NotifyDataLoaded(monoObj);
                    if (DebugLogEnabled)
                    {
                        SaveProvider.Log($"[SaveManager] Manually loaded {componentKey}", I);
                    }
                }
            }
            catch (Exception e)
            {
                SaveProvider.LogError($"Error loading save data for {componentKey}: " + e.Message);
            }
        }

        private static void CleanupDestroyedRegistrations()
        {
            if (_saveableComponents.Count == 0)
            {
                return;
            }

            // WHY: runs on every Register (each SaveableBehaviour.OnEnable); scan without LINQ and allocate nothing
            // when no registration was destroyed.
            _invalidKeysBuffer.Clear();
            foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> pair in _saveableComponents)
            {
                if (pair.Value.instance == null)
                {
                    _invalidKeysBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < _invalidKeysBuffer.Count; i++)
            {
                _saveableComponents.Remove(_invalidKeysBuffer[i]);
            }

            _invalidKeysBuffer.Clear();
        }

        #endregion
    }
}
