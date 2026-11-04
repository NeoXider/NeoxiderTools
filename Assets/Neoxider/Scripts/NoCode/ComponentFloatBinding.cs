using System;
using System.Reflection;
using Neo.Condition;
using Neo.Reactive;
using UnityEngine;

namespace Neo.NoCode
{
    /// <summary>
    ///     Resolves a float (or supported reactive property) from a component field/property with
    ///     <see cref="ReflectionCache"/>. Caches member resolution; reads the member value on each refresh.
    /// </summary>
    [Serializable]
    public sealed class ComponentFloatBinding
    {
        [Tooltip(
            "When set, use GameObject.Find (same as NeoCondition \"Find By Name\") instead of Source Root when resolving the source object.")]
        [SerializeField]
        private bool _useSceneSearch;

        [Tooltip("Name passed to GameObject.Find when Use Scene Search is on.")] [SerializeField]
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

        [Tooltip("GameObject that holds the source component. Empty = the binding component's own GameObject.")]
        [SerializeField] private GameObject _sourceRoot;
        [Tooltip("Full or short type name of the source component.")]
        [SerializeField] private string _componentTypeName = "";
        [Tooltip("Zero-based occurrence among components matching Component Type Name. Missing or negative occurrences fail to resolve.")]
        [SerializeField] private int _componentOccurrence;
        [Tooltip("Readable field or non-indexed property: a number or ReactivePropertyFloat/Int/Bool.")]
        [SerializeField] private string _memberName = "";

        [NonSerialized] private Component _cachedComponent;
        [NonSerialized] private MonoBehaviour _cachedHost;
        [NonSerialized] private MemberInfo _cachedMember;
        [NonSerialized] private bool _cacheValid;
        [NonSerialized] private bool _hasLoggedMissing;
        [NonSerialized] private BindingSourceGameObjectResolver.ResolveCache _sourceResolveCache;
        [NonSerialized] private int _invalidationVersion;

        /// <summary>
        ///     When true, the source is resolved with <see cref="GameObject.Find(string)"/> using
        ///     <see cref="SearchObjectName"/> instead of <see cref="SourceRoot"/>.
        /// </summary>
        public bool UseSceneSearch
        {
            get => _useSceneSearch;
            set
            {
                if (_useSceneSearch != value)
                {
                    _useSceneSearch = value;
                    Invalidate();
                }
            }
        }

        /// <summary>GameObject name used when <see cref="UseSceneSearch"/> is on.</summary>
        public string SearchObjectName
        {
            get => _searchObjectName;
            set
            {
                if (_searchObjectName != value)
                {
                    _searchObjectName = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Suppresses the one-time warning while the named object has not spawned yet.</summary>
        public bool WaitForObject
        {
            get => _waitForObject;
            set => _waitForObject = value;
        }

        /// <summary>GameObject that holds the source component; null uses the host's GameObject.</summary>
        public GameObject SourceRoot
        {
            get => _sourceRoot;
            set
            {
                if (!ReferenceEquals(_sourceRoot, value))
                {
                    _sourceRoot = value;
                    Invalidate();
                }
            }
        }

        /// <summary>Full or short type name of the source component.</summary>
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

        /// <summary>Readable field or non-indexed property to read (number or supported reactive property).</summary>
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
        ///     Zero-based occurrence among matching component types in GetComponents order. Invalid values fail resolution.
        /// </summary>
        public int ComponentOccurrence
        {
            get => _componentOccurrence;
            set
            {
                if (_componentOccurrence != value)
                {
                    _componentOccurrence = value;
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

        /// <summary>
        ///     Increments on every <see cref="Invalidate"/> (setters call it too), so owners can detect a rewire that
        ///     happened inside their own output callback.
        /// </summary>
        internal int InvalidationVersion => _invalidationVersion;

        /// <summary>Drops the resolved component/member and the Find cache; the next read resolves again.</summary>
        public void Invalidate()
        {
            unchecked
            {
                _invalidationVersion++;
            }

            _cacheValid = false;
            _cachedComponent = null;
            _cachedHost = null;
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
            return EnsureCache(host, true, out error);
        }

        private bool EnsureCache(MonoBehaviour host, bool describeError, out string error)
        {
            error = null;
            if (host == null)
            {
                error = "Host is null.";
                return false;
            }

            if (_componentOccurrence < 0)
            {
                error = "Component occurrence must be non-negative.";
                return false;
            }

            if (_cacheValid && _cachedMember != null)
            {
                if (_cachedComponent == null || _cachedHost != host)
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
                // WHY: unresolved bindings are retried on every poll; only build the message when a caller wants it.
                if (describeError)
                {
                    error = _useSceneSearch && !string.IsNullOrEmpty(_searchObjectName)
                        ? $"GameObject.Find(\"{_searchObjectName}\") returned no object."
                        : "Source GameObject is missing.";
                }

                return false;
            }

            if (string.IsNullOrEmpty(_componentTypeName) || string.IsNullOrEmpty(_memberName))
            {
                error = "Component type name and member name are required.";
                return false;
            }

            _cachedComponent =
                BindingSourceGameObjectResolver.FindComponentOccurrence(root, _componentTypeName, _componentOccurrence);
            if (_cachedComponent == null)
            {
                if (!_hasLoggedMissing)
                {
                    NeoDiagnostics.LogWarning(
                        $"[Neo.NoCode] Component '{_componentTypeName}' occurrence {_componentOccurrence} not found on '{root.name}'.");
                    _hasLoggedMissing = true;
                }

                if (describeError)
                {
                    error = $"Component '{_componentTypeName}' occurrence {_componentOccurrence} not found.";
                }

                return false;
            }

            Type type = _cachedComponent.GetType();
            if (TryResolveSupportedSourceMember(type, _memberName, out MemberInfo member))
            {
                _cachedMember = member;
                _cachedHost = host;
                _cacheValid = true;
                return true;
            }

            if (!_hasLoggedMissing)
            {
                NeoDiagnostics.LogWarning(
                    $"[Neo.NoCode] Property/field '{_memberName}' not found on '{_componentTypeName}' on '{root.name}'.");
                _hasLoggedMissing = true;
            }

            if (describeError)
            {
                error = $"Member '{_memberName}' not found.";
            }

            return false;
        }

        /// <summary>
        ///     Defines the NoCode binding contract: source members are readable fields or non-indexed readable properties.
        ///     Methods are intentionally excluded so Inspector wiring remains a data-binding layer, not hidden behavior.
        /// </summary>
        public static bool IsSupportedSourceMember(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo:
                    return true;
                case PropertyInfo property:
                    return property.CanRead && property.GetIndexParameters().Length == 0;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Resolves <paramref name="memberName"/> on <paramref name="componentType"/> to a supported source member
        ///     (see <see cref="IsSupportedSourceMember"/>): a readable non-indexed property first, then a field.
        /// </summary>
        /// <returns>False for methods, write-only properties, indexers (including overloaded ones) and unknown names.</returns>
        public static bool TryResolveSupportedSourceMember(Type componentType, string memberName, out MemberInfo member)
        {
            member = null;
            if (componentType == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            // WHY: overloaded indexers share one property name and are not valid data sources; the exception-free
            // lookup caches that ambiguity so unresolved bindings can retry cheaply.
            ReflectionCache.TryGetProperty(componentType, memberName, flags, out PropertyInfo prop);
            if (IsSupportedSourceMember(prop))
            {
                member = prop;
                return true;
            }

            FieldInfo field = ReflectionCache.GetField(componentType, memberName, flags);
            if (IsSupportedSourceMember(field))
            {
                member = field;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Resolves the binding (when needed) and reads the member value. Value-type members are boxed; reactive
        ///     properties are returned as their instance.
        /// </summary>
        /// <returns>False when the source cannot be resolved or the getter throws.</returns>
        public bool TryReadRaw(MonoBehaviour host, out object raw)
        {
            raw = null;
            if (!EnsureCache(host, false, out _))
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
                NeoDiagnostics.LogWarning($"[Neo.NoCode] Failed to read member: {ex.Message}");
                Invalidate();
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Reads a numeric float; unwraps supported reactive property values.
        /// </summary>
        public bool TryReadFloat(MonoBehaviour host, out float value)
        {
            value = 0f;
            return TryReadRaw(host, out object raw) && TryConvertToFloat(raw, out value);
        }

        /// <summary>
        ///     Converts a value read by <see cref="TryReadRaw"/> to float: numbers, and the current value of
        ///     <see cref="ReactivePropertyFloat"/> / <see cref="ReactivePropertyInt"/> / <see cref="ReactivePropertyBool"/>
        ///     (bool maps to 1/0).
        /// </summary>
        public static bool TryConvertToFloat(object raw, out float value)
        {
            value = 0f;
            switch (raw)
            {
                case null:
                    return false;
                case ReactivePropertyFloat rpf:
                    value = rpf.CurrentValue;
                    return true;
                case ReactivePropertyInt rpi:
                    value = rpi.CurrentValue;
                    return true;
                case ReactivePropertyBool rpb:
                    value = rpb.CurrentValue ? 1f : 0f;
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
        ///     Reads the member and returns its reactive instance when it holds a supported reactive property.
        ///     Exactly one out value is non-null on success.
        /// </summary>
        public bool TryGetReactiveProperty(MonoBehaviour host, out ReactivePropertyFloat reactiveFloat,
            out ReactivePropertyInt reactiveInt, out ReactivePropertyBool reactiveBool)
        {
            reactiveFloat = null;
            reactiveInt = null;
            reactiveBool = null;
            return TryReadRaw(host, out object raw) &&
                   TryGetReactivePropertyFromValue(raw, out reactiveFloat, out reactiveInt, out reactiveBool);
        }

        /// <summary>
        ///     Classifies a value read by <see cref="TryReadRaw"/>; exactly one out value is non-null on success.
        /// </summary>
        public static bool TryGetReactivePropertyFromValue(object raw, out ReactivePropertyFloat reactiveFloat,
            out ReactivePropertyInt reactiveInt, out ReactivePropertyBool reactiveBool)
        {
            reactiveFloat = null;
            reactiveInt = null;
            reactiveBool = null;
            switch (raw)
            {
                case ReactivePropertyFloat rpf:
                    reactiveFloat = rpf;
                    return true;
                case ReactivePropertyInt rpi:
                    reactiveInt = rpi;
                    return true;
                case ReactivePropertyBool rpb:
                    reactiveBool = rpb;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Returns reactive instance when the member holds <see cref="ReactivePropertyFloat"/> (reference type).
        /// </summary>
        public bool TryGetReactivePropertyFloat(MonoBehaviour host, out ReactivePropertyFloat reactive)
        {
            bool result = TryGetReactiveProperty(host, out reactive, out _, out _);
            return result && reactive != null;
        }

        /// <summary>
        ///     Returns reactive instance when the member holds <see cref="ReactivePropertyInt" />.
        /// </summary>
        public bool TryGetReactivePropertyInt(MonoBehaviour host, out ReactivePropertyInt reactive)
        {
            bool result = TryGetReactiveProperty(host, out _, out reactive, out _);
            return result && reactive != null;
        }

        /// <summary>
        ///     Returns reactive instance when the member holds <see cref="ReactivePropertyBool" />.
        /// </summary>
        public bool TryGetReactivePropertyBool(MonoBehaviour host, out ReactivePropertyBool reactive)
        {
            bool result = TryGetReactiveProperty(host, out _, out _, out reactive);
            return result && reactive != null;
        }
    }
}
