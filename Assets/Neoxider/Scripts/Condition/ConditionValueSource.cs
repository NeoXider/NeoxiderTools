using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Источник значения для условия: резолв GameObject (прямая ссылка / поиск по имени), компонент или GameObject,
    ///     чтение поля/свойства/метода через reflection с кешированием.
    /// </summary>
    internal sealed class ConditionValueSource
    {
        private readonly ConditionEntry _entry;
        private readonly bool _isOther;

        private Component _cachedComponent;
        private GameObject _cachedGameObject;
        private MemberInfo _cachedMember;
        private bool _cacheValid;
        private GameObject _foundByNameObject;
        private bool _hasLoggedDestroyedWarning;
        private bool _hasLoggedMissingMemberWarning;
        private bool _hasLoggedSearchNotFoundWarning;
        private bool _hasSearchedByName;

        public ConditionValueSource(ConditionEntry entry, bool isOther)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _isOther = isOther;
        }

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
                    return goProp.GetValue(_cachedGameObject);
                if (_cachedMember is FieldInfo goField)
                    return goField.GetValue(_cachedGameObject);
                return null;
            }

            if (_cachedComponent == null)
            {
                InvalidateCache();
                return null;
            }

            if (_cachedMember is PropertyInfo prop)
                return prop.GetValue(_cachedComponent);
            if (_cachedMember is FieldInfo field)
                return field.GetValue(_cachedComponent);
            if (_cachedMember is MethodInfo method)
            {
                object arg = GetArgumentValue();
                return method.Invoke(_cachedComponent, new[] { arg });
            }

            return null;
        }

        public void InvalidateCache()
        {
            _cacheValid = false;
            _cachedComponent = null;
            _cachedGameObject = null;
            _cachedMember = null;
            _hasLoggedDestroyedWarning = false;
            _hasLoggedMissingMemberWarning = false;
        }

        public void InvalidateCacheFull()
        {
            InvalidateCache();
            _foundByNameObject = null;
            _hasSearchedByName = false;
            _hasLoggedSearchNotFoundWarning = false;
        }

        internal GameObject FoundByNameObject => _foundByNameObject;

        /// <summary>
        ///     Возвращает целевой GameObject без построения кеша компонента (для BindOtherToSourceIfNull и т.п.).
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

            if (useSearch && !string.IsNullOrEmpty(searchName))
            {
                if (_foundByNameObject != null)
                    return _foundByNameObject;
                if (_hasSearchedByName && _foundByNameObject == null)
                {
                    _hasSearchedByName = false;
                    _hasLoggedSearchNotFoundWarning = false;
                }

                _foundByNameObject = GameObject.Find(searchName);
                _hasSearchedByName = true;
                if (_foundByNameObject == null)
                {
                    if (!wait && !_hasLoggedSearchNotFoundWarning)
                    {
                        Debug.LogWarning($"[NeoCondition] GameObject.Find(\"{searchName}\") — объект не найден в сцене.");
                        _hasLoggedSearchNotFoundWarning = true;
                    }
                    return null;
                }
                _hasLoggedSearchNotFoundWarning = false;
                return _foundByNameObject;
            }

            if (IsSourceObjectDestroyed(sourceObj))
                return null;
            return sourceObj != null ? sourceObj : fallbackObject;
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
                    LogDestroyedWarning($"Компонент '{typeName}' был уничтожен");
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

            GameObject target = ResolveTargetObject(fallbackObject);
            string componentTypeName = _isOther ? _entry.OtherComponentTypeName : _entry.ComponentTypeName;
            string propertyName = _isOther ? _entry.OtherPropertyName : _entry.PropertyName;
            bool useSearch = _isOther ? _entry.OtherUseSceneSearch : _entry.UseSceneSearch;

            if (target == null)
            {
                if (!useSearch && IsSourceObjectDestroyed(_isOther ? _entry.OtherSourceObject : _entry.SourceObject))
                    LogDestroyedWarning($"Source GameObject был уничтожен (ожидался объект для компонента '{componentTypeName}')");
                return false;
            }

            if (string.IsNullOrEmpty(componentTypeName) || string.IsNullOrEmpty(propertyName))
                return false;

            Component[] components;
            try
            {
                components = target.GetComponents<Component>();
            }
            catch (MissingReferenceException)
            {
                LogDestroyedWarning($"GameObject уничтожен при поиске компонента '{componentTypeName}'");
                return false;
            }

            foreach (Component comp in components)
            {
                if (comp == null) continue;
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
                    Debug.LogWarning($"[NeoCondition] Компонент '{componentTypeName}' не найден на '{target.name}'.");
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
                MethodInfo method = ConditionEntry.FindMethodWithOneArgument(type, propertyName, argKind, flags);
                if (method != null)
                {
                    _cachedMember = method;
                    _cacheValid = true;
                    return true;
                }
            }
            else
            {
                PropertyInfo prop = type.GetProperty(propertyName, flags);
                if (prop != null && prop.CanRead)
                {
                    _cachedMember = prop;
                    _cacheValid = true;
                    return true;
                }
                FieldInfo field = type.GetField(propertyName, flags);
                if (field != null)
                {
                    _cachedMember = field;
                    _cacheValid = true;
                    return true;
                }
            }

            if (!_hasLoggedMissingMemberWarning)
            {
                Debug.LogWarning($"[NeoCondition] Свойство/поле/метод '{propertyName}' не найдено в '{componentTypeName}' на '{target.name}'.");
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
                    LogDestroyedWarning("Целевой GameObject был уничтожен (режим GameObject)");
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

            GameObject target = ResolveTargetObject(fallbackObject);
            bool useSearch = _isOther ? _entry.OtherUseSceneSearch : _entry.UseSceneSearch;
            string propertyName = _isOther ? _entry.OtherPropertyName : _entry.PropertyName;

            if (target == null)
            {
                if (!useSearch && IsSourceObjectDestroyed(_isOther ? _entry.OtherSourceObject : _entry.SourceObject))
                    LogDestroyedWarning("Source GameObject был уничтожен (режим GameObject)");
                return false;
            }

            if (string.IsNullOrEmpty(propertyName))
                return false;

            _cachedGameObject = target;
            Type type = typeof(GameObject);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo prop = type.GetProperty(propertyName, flags);
            if (prop != null && prop.CanRead)
            {
                _cachedMember = prop;
                _cacheValid = true;
                return true;
            }
            FieldInfo field = type.GetField(propertyName, flags);
            if (field != null)
            {
                _cachedMember = field;
                _cacheValid = true;
                return true;
            }

            if (!_hasLoggedMissingMemberWarning)
            {
                Debug.LogWarning($"[NeoCondition] Свойство '{propertyName}' не найдено на GameObject.");
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
                Debug.LogWarning($"[NeoCondition] {message}. Условие будет возвращать false.");
                _hasLoggedDestroyedWarning = true;
            }
        }
    }
}
