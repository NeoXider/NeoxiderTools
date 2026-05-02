using System;
using System.Reflection;
using Neo.Condition;
using Neo.Reactive;
using UnityEngine;

namespace Neo.NoCode
{
    /// <summary>
    ///     Resolves a float (or <see cref="ReactivePropertyFloat"/>) from a component field/property with
    ///     <see cref="ReflectionCache"/>. Refreshes member resolution when invalidated; no per-frame reflection.
    /// </summary>
    [Serializable]
    public sealed class ComponentFloatBinding
    {
        [Tooltip("When set, use GameObject.Find (same as NeoCondition «Find By Name») instead of Source Root when resolving the source object.")]
        [SerializeField]
        private bool _useSceneSearch;

        [Tooltip("Name passed to GameObject.Find when Use Scene Search is on.")]
        [SerializeField]
        private string _searchObjectName = "";

        [Tooltip("If true, do not log a warning when the object is not found (e.g. prefab or late spawn).")]
        [SerializeField]
        private bool _waitForObject;

        [Tooltip(
            "Seconds between GameObject.Find attempts while the object is still missing (Find By Name only). 0 = retry every check (no throttle).")]
        [SerializeField]
        private float _findRetryIntervalSeconds = BindingSourceGameObjectResolver.DefaultFindRetryIntervalSeconds;

        [Tooltip(
            "Prefab for previewing components in the Editor when the object is not in the scene. Not used at runtime.")]
        [SerializeField]
        private GameObject _prefabPreview;

        [SerializeField] private GameObject _sourceRoot;
        [SerializeField] private string _componentTypeName = "";
        [SerializeField] private string _memberName = "";

        [NonSerialized] private Component _cachedComponent;
        [NonSerialized] private MemberInfo _cachedMember;
        [NonSerialized] private bool _cacheValid;
        [NonSerialized] private bool _hasLoggedMissing;
        [NonSerialized] private BindingSourceGameObjectResolver.ResolveCache _sourceResolveCache;

        public GameObject SourceRoot
        {
            get => _sourceRoot;
            set
            {
                if (_sourceRoot != value)
                {
                    _sourceRoot = value;
                    Invalidate();
                }
            }
        }

        public string ComponentTypeName
        {
            get => _componentTypeName;
            set
            {
                if (_componentTypeName != value)
                {
                    _componentTypeName = value;
                    Invalidate();
                }
            }
        }

        public string MemberName
        {
            get => _memberName;
            set
            {
                if (_memberName != value)
                {
                    _memberName = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        ///     Editor-only prefab for component pickers when <see cref="_useSceneSearch"/> finds no instance (same idea as
        ///     <see cref="Neo.Condition.ConditionEntry.PrefabPreview"/>). Not used at runtime.
        /// </summary>
        public GameObject PrefabPreview
        {
            get => _prefabPreview;
            set
            {
                if (_prefabPreview != value)
                {
                    _prefabPreview = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        ///     Seconds between <see cref="GameObject.Find"/> retries when using Find By Name and the object is still missing.
        ///     0 = retry on every <see cref="EnsureCache"/>. See <see cref="BindingSourceGameObjectResolver"/>.
        /// </summary>
        public float FindRetryIntervalSeconds
        {
            get => _findRetryIntervalSeconds;
            set
            {
                float v = Mathf.Max(0f, value);
                if (!Mathf.Approximately(_findRetryIntervalSeconds, v))
                {
                    _findRetryIntervalSeconds = v;
                    Invalidate();
                }
            }
        }

        public void Invalidate()
        {
            _cacheValid = false;
            _cachedComponent = null;
            _cachedMember = null;
            _hasLoggedMissing = false;
            BindingSourceGameObjectResolver.InvalidateFindCache(ref _sourceResolveCache);
        }

        /// <summary>
        ///     Resolves the source <see cref="GameObject"/> using scene search and/or <see cref="_sourceRoot"/> /
        ///     <paramref name="host"/>.
        /// </summary>
        public bool EnsureCache(MonoBehaviour host, out string error)
        {
            error = null;
            if (host == null)
            {
                error = "Host is null.";
                return false;
            }

            if (_cacheValid && _cachedMember != null)
            {
                if (_cachedComponent == null)
                {
                    Invalidate();
                }
                else
                {
                    return true;
                }
            }

            _cacheValid = false;
            _cachedComponent = null;
            _cachedMember = null;

            GameObject root = BindingSourceGameObjectResolver.Resolve(_useSceneSearch, _searchObjectName,
                _waitForObject, _sourceRoot, host.gameObject, ref _sourceResolveCache, "[Neo.NoCode]",
                _findRetryIntervalSeconds);
            if (root == null)
            {
                error = _useSceneSearch && !string.IsNullOrEmpty(_searchObjectName)
                    ? $"GameObject.Find(\"{_searchObjectName}\") returned no object."
                    : "Source GameObject is missing.";
                return false;
            }

            if (string.IsNullOrEmpty(_componentTypeName) || string.IsNullOrEmpty(_memberName))
            {
                error = "Component type name and member name are required.";
                return false;
            }

            Component[] components;
            try
            {
                components = root.GetComponents<Component>();
            }
            catch (MissingReferenceException)
            {
                error = "Source GameObject was destroyed.";
                return false;
            }

            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                Type ct = comp.GetType();
                if (ct.FullName == _componentTypeName || ct.Name == _componentTypeName)
                {
                    _cachedComponent = comp;
                    break;
                }
            }

            if (_cachedComponent == null)
            {
                if (!_hasLoggedMissing)
                {
                    Debug.LogWarning($"[Neo.NoCode] Component '{_componentTypeName}' not found on '{root.name}'.");
                    _hasLoggedMissing = true;
                }

                error = $"Component '{_componentTypeName}' not found.";
                return false;
            }

            Type type = _cachedComponent.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo prop = ReflectionCache.GetProperty(type, _memberName, flags);
            if (prop != null && prop.CanRead)
            {
                _cachedMember = prop;
                _cacheValid = true;
                return true;
            }

            FieldInfo field = ReflectionCache.GetField(type, _memberName, flags);
            if (field != null)
            {
                _cachedMember = field;
                _cacheValid = true;
                return true;
            }

            if (!_hasLoggedMissing)
            {
                Debug.LogWarning(
                    $"[Neo.NoCode] Property/field '{_memberName}' not found on '{_componentTypeName}' on '{root.name}'.");
                _hasLoggedMissing = true;
            }

            error = $"Member '{_memberName}' not found.";
            return false;
        }

        public bool TryReadRaw(MonoBehaviour host, out object raw)
        {
            raw = null;
            if (!EnsureCache(host, out _))
            {
                return false;
            }

            try
            {
                if (_cachedMember is PropertyInfo pi)
                {
                    raw = pi.GetValue(_cachedComponent);
                }
                else if (_cachedMember is FieldInfo fi)
                {
                    raw = fi.GetValue(_cachedComponent);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Neo.NoCode] Failed to read member: {ex.Message}");
                Invalidate();
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Reads a numeric float; unwraps <see cref="ReactivePropertyFloat.CurrentValue"/>.
        /// </summary>
        public bool TryReadFloat(MonoBehaviour host, out float value)
        {
            value = 0f;
            if (!TryReadRaw(host, out object raw))
            {
                return false;
            }

            switch (raw)
            {
                case null:
                    return false;
                case ReactivePropertyFloat rpf:
                    value = rpf.CurrentValue;
                    return true;
                case float f:
                    value = f;
                    return true;
                case int i:
                    value = i;
                    return true;
                case double d:
                    value = (float)d;
                    return true;
                default:
                    try
                    {
                        value = Convert.ToSingle(raw);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }
        }

        /// <summary>
        ///     Returns reactive instance when the member holds <see cref="ReactivePropertyFloat"/> (reference type).
        /// </summary>
        public bool TryGetReactivePropertyFloat(MonoBehaviour host, out ReactivePropertyFloat reactive)
        {
            reactive = null;
            if (!TryReadRaw(host, out object raw))
            {
                return false;
            }

            reactive = raw as ReactivePropertyFloat;
            return reactive != null;
        }
    }
}
