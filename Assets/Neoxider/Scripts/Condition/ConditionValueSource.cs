using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Value source for a condition: resolve GameObject (direct reference / name search), component or GameObject,
    ///     read field/property/method via reflection with caching.
    /// </summary>
    internal sealed class ConditionValueSource
    {
        private readonly ConditionEntry _entry;
        private readonly bool _isOther;

        private Component _cachedComponent;
        private GameObject _cachedGameObject;
        private MemberInfo _cachedMember;
        private object[] _cachedArgs;
        private bool _cacheValid;
        private bool _hasLoggedDestroyedWarning;
        private bool _hasLoggedMissingMemberWarning;
        private BindingSourceGameObjectResolver.ResolveCache _resolveCache;

        public ConditionValueSource(ConditionEntry entry, bool isOther)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _isOther = isOther;
        }

        internal GameObject FoundByNameObject => _resolveCache.FoundByNameObject;

        public bool EnsureCache(GameObject fallbackObject)
        {
            SourceMode mode = _isOther ? _entry.OtherSourceMode : _entry.Source;
            return mode == SourceMode.GameObject
                ? EnsureCacheGameObject(fallbackObject)
                : EnsureCacheComponent(fallbackObject);
        }

        public object ReadValue()
        {
            SourceMode mode = _isOther ? _entry.OtherSourceMode : _entry.Source;
            if (mode == SourceMode.GameObject)
            {
                if (_cachedGameObject == null)
                {
                    InvalidateCache();
                    return null;
                }

                if (_cachedMember is PropertyInfo goProp)
                {
                    return goProp.GetValue(_cachedGameObject);
                }

                if (_cachedMember is FieldInfo goField)
                {
                    return goField.GetValue(_cachedGameObject);
                }

                return null;
            }

            if (_cachedComponent == null)
            {
                InvalidateCache();
                return null;
            }

            if (_cachedMember is PropertyInfo prop)
            {
                return prop.GetValue(_cachedComponent);
            }

            if (_cachedMember is FieldInfo field)
            {
                return field.GetValue(_cachedComponent);
            }

            if (_cachedMember is MethodInfo method)
            {
                return method.Invoke(_cachedComponent, _cachedArgs);
            }

            return null;
        }

        public void InvalidateCache()
        {
            _cacheValid = false;
            _cachedComponent = null;
            _cachedGameObject = null;
            _cachedMember = null;
            _cachedArgs = null;
            _hasLoggedDestroyedWarning = false;
            _hasLoggedMissingMemberWarning = false;
        }

        public void InvalidateCacheFull()
        {
            InvalidateCache();
            BindingSourceGameObjectResolver.InvalidateFindCache(ref _resolveCache);
        }

        /// <summary>
        ///     Returns the target GameObject without building the component cache (for BindOtherToSourceIfNull, etc.).
        /// </summary>
        public GameObject GetResolvedTarget(GameObject fallbackObject)
        {
            return ResolveTargetObject(fallbackObject);
        }

        private GameObject ResolveTargetObject(GameObject fallbackObject)
        {
            bool useSearch = _isOther ? _entry.OtherUseSceneSearch : _entry.UseSceneSearch;
            string searchName = _isOther ? _entry.OtherSearchObjectName : _entry.SearchObjectName;
            bool wait = _isOther ? _entry.OtherWaitForObject : _entry.WaitForObject;
            GameObject sourceObj = _isOther ? _entry.OtherSourceObject : _entry.SourceObject;

            return BindingSourceGameObjectResolver.Resolve(useSearch, searchName, wait, sourceObj, fallbackObject,
                ref _resolveCache, "[NeoCondition]",
                _isOther ? _entry.OtherFindRetryIntervalSeconds : _entry.FindRetryIntervalSeconds);
        }

        private static bool IsSourceObjectDestroyed(GameObject obj)
        {
            return obj != null && !ReferenceEquals(obj, null) && obj == null;
        }

        private bool EnsureCacheComponent(GameObject fallbackObject)
        {
            if (_cacheValid && _cachedMember != null)
            {
                if (_cachedComponent == null)
                {
                    string typeName = _isOther ? _entry.OtherComponentTypeName : _entry.ComponentTypeName;
                    LogDestroyedWarning($"Component '{typeName}' was destroyed");
                    InvalidateCache();
                }
                else
                {
                    return true;
                }
            }

            _cacheValid = false;
            _cachedComponent = null;
            _cachedGameObject = null;
            _cachedMember = null;
            _cachedArgs = null;

            GameObject target = ResolveTargetObject(fallbackObject);
            string componentTypeName = _isOther ? _entry.OtherComponentTypeName : _entry.ComponentTypeName;
            string propertyName = _isOther ? _entry.OtherPropertyName : _entry.PropertyName;
            bool useSearch = _isOther ? _entry.OtherUseSceneSearch : _entry.UseSceneSearch;

            if (target == null)
            {
                if (!useSearch && IsSourceObjectDestroyed(_isOther ? _entry.OtherSourceObject : _entry.SourceObject))
                {
                    LogDestroyedWarning(
                        $"Source GameObject was destroyed (expected object for component '{componentTypeName}')");
                }

                return false;
            }

            if (string.IsNullOrEmpty(componentTypeName) || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            Component[] components;
            try
            {
                components = target.GetComponents<Component>();
            }
            catch (MissingReferenceException)
            {
                LogDestroyedWarning($"GameObject was destroyed while resolving component '{componentTypeName}'");
                return false;
            }

            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                if (comp.GetType().FullName == componentTypeName || comp.GetType().Name == componentTypeName)
                {
                    _cachedComponent = comp;
                    break;
                }
            }

            if (_cachedComponent == null)
            {
                if (!_hasLoggedMissingMemberWarning)
                {
                    Debug.LogWarning($"[NeoCondition] Component '{componentTypeName}' not found on '{target.name}'.");
                    _hasLoggedMissingMemberWarning = true;
                }

                return false;
            }

            Type type = _cachedComponent.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            bool isMethod = _isOther ? _entry.OtherIsMethodWithArgument : _entry.IsMethodWithArgument;
            ArgumentKind argKind = _isOther ? _entry.OtherPropertyArgumentKind : _entry.PropertyArgumentKind;

            if (isMethod)
            {
                MethodInfo method = ReflectionCache.GetMethod(type, propertyName, argKind, flags);
                if (method != null)
                {
                    _cachedMember = method;
                    _cachedArgs = new[] { GetArgumentValue() };
                    _cacheValid = true;
                    return true;
                }
            }
            else
            {
                PropertyInfo prop = ReflectionCache.GetProperty(type, propertyName, flags);
                if (prop != null && prop.CanRead)
                {
                    _cachedMember = prop;
                    _cacheValid = true;
                    return true;
                }

                FieldInfo field = ReflectionCache.GetField(type, propertyName, flags);
                if (field != null)
                {
                    _cachedMember = field;
                    _cacheValid = true;
                    return true;
                }
            }

            if (!_hasLoggedMissingMemberWarning)
            {
                Debug.LogWarning(
                    $"[NeoCondition] Property/field/method '{propertyName}' not found on '{componentTypeName}' on '{target.name}'.");
                _hasLoggedMissingMemberWarning = true;
            }

            return false;
        }

        private bool EnsureCacheGameObject(GameObject fallbackObject)
        {
            if (_cacheValid && _cachedMember != null)
            {
                if (_cachedGameObject == null)
                {
                    LogDestroyedWarning("Target GameObject was destroyed (GameObject mode)");
                    InvalidateCache();
                }
                else
                {
                    return true;
                }
            }

            _cacheValid = false;
            _cachedComponent = null;
            _cachedGameObject = null;
            _cachedMember = null;
            _cachedArgs = null;

            GameObject target = ResolveTargetObject(fallbackObject);
            bool useSearch = _isOther ? _entry.OtherUseSceneSearch : _entry.UseSceneSearch;
            string propertyName = _isOther ? _entry.OtherPropertyName : _entry.PropertyName;

            if (target == null)
            {
                if (!useSearch && IsSourceObjectDestroyed(_isOther ? _entry.OtherSourceObject : _entry.SourceObject))
                {
                    LogDestroyedWarning("Source GameObject was destroyed (GameObject mode)");
                }

                return false;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            _cachedGameObject = target;
            Type type = typeof(GameObject);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo prop = ReflectionCache.GetProperty(type, propertyName, flags);
            if (prop != null && prop.CanRead)
            {
                _cachedMember = prop;
                _cacheValid = true;
                return true;
            }

            FieldInfo field = ReflectionCache.GetField(type, propertyName, flags);
            if (field != null)
            {
                _cachedMember = field;
                _cacheValid = true;
                return true;
            }

            if (!_hasLoggedMissingMemberWarning)
            {
                Debug.LogWarning($"[NeoCondition] Property '{propertyName}' not found on GameObject.");
                _hasLoggedMissingMemberWarning = true;
            }

            return false;
        }

        private object GetArgumentValue()
        {
            ArgumentKind kind = _isOther ? _entry.OtherPropertyArgumentKind : _entry.PropertyArgumentKind;
            return kind switch
            {
                ArgumentKind.Int => _isOther ? _entry.OtherPropertyArgumentInt : _entry.PropertyArgumentInt,
                ArgumentKind.Float => _isOther ? _entry.OtherPropertyArgumentFloat : _entry.PropertyArgumentFloat,
                ArgumentKind.String => _isOther ? _entry.OtherPropertyArgumentString : _entry.PropertyArgumentString,
                _ => _isOther ? _entry.OtherPropertyArgumentInt : _entry.PropertyArgumentInt
            };
        }

        private void LogDestroyedWarning(string message)
        {
            if (!_hasLoggedDestroyedWarning)
            {
                Debug.LogWarning($"[NeoCondition] {message}. Condition will evaluate to false.");
                _hasLoggedDestroyedWarning = true;
            }
        }
    }
}
