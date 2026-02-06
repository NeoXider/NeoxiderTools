using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Тип сравнения для условия.
    /// </summary>
    public enum CompareOp
    {
        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    ///     Тип значения, считанного из компонента.
    /// </summary>
    public enum ValueType
    {
        Int,
        Float,
        Bool,
        String
    }

    /// <summary>
    ///     Источник данных для условия: компонент или сам GameObject.
    /// </summary>
    public enum SourceMode
    {
        /// <summary>Читать поле/свойство из компонента.</summary>
        Component,

        /// <summary>Читать свойство самого GameObject (activeSelf, tag, layer и т.д.).</summary>
        GameObject
    }

    /// <summary>
    ///     Одна запись условия: ссылка на GameObject → Component/GameObject → поле/свойство, оператор сравнения и порог.
    ///     В рантайме через reflection (с кешированием) читает значение и сравнивает.
    /// </summary>
    [Serializable]
    public class ConditionEntry
    {
        [Tooltip("Источник данных: Component (поля компонента) или GameObject (свойства объекта).")] [SerializeField]
        private SourceMode _sourceMode = SourceMode.Component;

        [Tooltip("Искать целевой GameObject по имени в сцене (вместо прямой ссылки).")] [SerializeField]
        private bool _useSceneSearch;

        [Tooltip("Имя GameObject для поиска в сцене через GameObject.Find().")] [SerializeField]
        private string _searchObjectName = "";

        [Tooltip("Не выводить Warning если объект не найден. Полезно для префабов, которые появятся позже (спавн).")]
        [SerializeField]
        private bool _waitForObject;

        [Tooltip(
            "Префаб для предпросмотра компонентов/свойств в Editor, когда объект ещё не на сцене. Не используется в Runtime.")]
        [SerializeField]
        private GameObject _prefabPreview;

        [Tooltip("GameObject, на котором искать компонент. Если пусто — используется объект NeoCondition.")]
        [SerializeField]
        private GameObject _sourceObject;

        [Tooltip("Индекс компонента в списке (заполняется через Editor).")] [SerializeField]
        private int _componentIndex;

        [Tooltip("Полное имя типа компонента (для восстановления после перезагрузки).")] [SerializeField]
        private string _componentTypeName = "";

        [Tooltip("Имя поля или свойства для чтения значения.")] [SerializeField]
        private string _propertyName = "";

        [Tooltip("Определённый тип значения.")] [SerializeField]
        private ValueType _valueType = ValueType.Int;

        [Tooltip("Оператор сравнения.")] [SerializeField]
        private CompareOp _compareOp = CompareOp.Equal;

        [Tooltip("Инвертировать результат (NOT).")] [SerializeField]
        private bool _invert;

        // Threshold values by type
        [SerializeField] private int _thresholdInt;
        [SerializeField] private float _thresholdFloat;
        [SerializeField] private bool _thresholdBool = true;
        [SerializeField] private string _thresholdString = "";

        // Cached reflection data
        [NonSerialized] private Component _cachedComponent;
        [NonSerialized] private GameObject _cachedGameObject;
        [NonSerialized] private MemberInfo _cachedMember;
        [NonSerialized] private bool _cacheValid;

        // Кеш поиска по имени
        [NonSerialized] private GameObject _foundByNameObject;

        // Флаг: предупреждение уже показано (чтобы не спамить каждый кадр)
        [NonSerialized] private bool _hasLoggedDestroyedWarning;
        [NonSerialized] private bool _hasLoggedMissingMemberWarning;
        [NonSerialized] private bool _hasLoggedSearchNotFoundWarning;
        [NonSerialized] private bool _hasSearchedByName;

        /// <summary>
        ///     Источник данных: Component или GameObject.
        /// </summary>
        public SourceMode Source
        {
            get => _sourceMode;
            set
            {
                _sourceMode = value;
                InvalidateCache();
            }
        }

        /// <summary>
        ///     Искать целевой GameObject по имени в сцене.
        /// </summary>
        public bool UseSceneSearch
        {
            get => _useSceneSearch;
            set
            {
                _useSceneSearch = value;
                InvalidateCacheFull();
            }
        }

        /// <summary>
        ///     Не выводить Warning если объект не найден (ожидать появления).
        /// </summary>
        public bool WaitForObject
        {
            get => _waitForObject;
            set => _waitForObject = value;
        }

        /// <summary>
        ///     Префаб для предпросмотра компонентов в Editor (не используется в Runtime).
        /// </summary>
        public GameObject PrefabPreview
        {
            get => _prefabPreview;
            set => _prefabPreview = value;
        }

        /// <summary>
        ///     Имя GameObject для поиска в сцене.
        /// </summary>
        public string SearchObjectName
        {
            get => _searchObjectName;
            set
            {
                _searchObjectName = value;
                InvalidateCacheFull();
            }
        }

        /// <summary>
        ///     GameObject-источник значения.
        /// </summary>
        public GameObject SourceObject
        {
            get => _sourceObject;
            set
            {
                _sourceObject = value;
                InvalidateCache();
            }
        }

        /// <summary>
        ///     Найденный через поиск по имени объект (runtime, read-only).
        /// </summary>
        public GameObject FoundByNameObject => _foundByNameObject;

        /// <summary>
        ///     Индекс компонента на объекте.
        /// </summary>
        public int ComponentIndex
        {
            get => _componentIndex;
            set
            {
                _componentIndex = value;
                InvalidateCache();
            }
        }

        /// <summary>
        ///     Полное имя типа компонента.
        /// </summary>
        public string ComponentTypeName
        {
            get => _componentTypeName;
            set
            {
                _componentTypeName = value;
                InvalidateCache();
            }
        }

        /// <summary>
        ///     Имя поля/свойства.
        /// </summary>
        public string PropertyName
        {
            get => _propertyName;
            set
            {
                _propertyName = value;
                InvalidateCache();
            }
        }

        /// <summary>
        ///     Тип значения.
        /// </summary>
        public ValueType CurrentValueType
        {
            get => _valueType;
            set => _valueType = value;
        }

        /// <summary>
        ///     Оператор сравнения.
        /// </summary>
        public CompareOp Compare
        {
            get => _compareOp;
            set => _compareOp = value;
        }

        /// <summary>
        ///     Инвертировать результат.
        /// </summary>
        public bool Invert
        {
            get => _invert;
            set => _invert = value;
        }

        public int ThresholdInt
        {
            get => _thresholdInt;
            set => _thresholdInt = value;
        }

        public float ThresholdFloat
        {
            get => _thresholdFloat;
            set => _thresholdFloat = value;
        }

        public bool ThresholdBool
        {
            get => _thresholdBool;
            set => _thresholdBool = value;
        }

        public string ThresholdString
        {
            get => _thresholdString;
            set => _thresholdString = value;
        }

        /// <summary>
        ///     Сбросить кеш reflection и флаги предупреждений.
        ///     Кеш поиска по имени НЕ сбрасывается (объект мог остаться живым).
        ///     Для полного сброса используйте <see cref="InvalidateCacheFull" />.
        /// </summary>
        public void InvalidateCache()
        {
            _cacheValid = false;
            _cachedComponent = null;
            _cachedGameObject = null;
            _cachedMember = null;
            _hasLoggedDestroyedWarning = false;
            _hasLoggedMissingMemberWarning = false;
        }

        /// <summary>
        ///     Полный сброс кеша, включая кеш поиска по имени.
        /// </summary>
        public void InvalidateCacheFull()
        {
            InvalidateCache();
            _foundByNameObject = null;
            _hasSearchedByName = false;
            _hasLoggedSearchNotFoundWarning = false;
        }

        /// <summary>
        ///     Оценить условие. Возвращает true если условие выполнено.
        /// </summary>
        /// <param name="fallbackObject">GameObject-владелец (используется если sourceObject пуст).</param>
        public bool Evaluate(GameObject fallbackObject)
        {
            bool result = EvaluateInternal(fallbackObject);
            return _invert ? !result : result;
        }

        private bool EvaluateInternal(GameObject fallbackObject)
        {
            if (!EnsureCache(fallbackObject))
            {
                return false;
            }

            object rawValue;
            try
            {
                rawValue = ReadValue();
            }
            catch (MissingReferenceException)
            {
                // Объект или компонент был уничтожен между проверкой кеша и чтением
                LogDestroyedWarning("Объект/компонент уничтожен во время чтения значения");
                InvalidateCache();
                return false;
            }
            catch (Exception ex)
            {
                if (!_hasLoggedMissingMemberWarning)
                {
                    Debug.LogWarning($"[NeoCondition] Ошибка чтения значения ({_propertyName}): {ex.Message}");
                    _hasLoggedMissingMemberWarning = true;
                }

                InvalidateCache();
                return false;
            }

            if (rawValue == null)
            {
                return false;
            }

            try
            {
                switch (_valueType)
                {
                    case ValueType.Int:
                        return CompareInt(Convert.ToInt32(rawValue));
                    case ValueType.Float:
                        return CompareFloat(Convert.ToSingle(rawValue));
                    case ValueType.Bool:
                        return CompareBool(Convert.ToBoolean(rawValue));
                    case ValueType.String:
                        return CompareString(rawValue.ToString());
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                if (!_hasLoggedMissingMemberWarning)
                {
                    Debug.LogWarning(
                        $"[NeoCondition] Ошибка сравнения значения ({_propertyName}={rawValue}, тип={_valueType}): {ex.Message}");
                    _hasLoggedMissingMemberWarning = true;
                }

                return false;
            }
        }

        /// <summary>
        ///     Определяет целевой GameObject: через прямую ссылку, поиск по имени или fallback.
        /// </summary>
        private GameObject ResolveTargetObject(GameObject fallbackObject)
        {
            // Режим поиска по имени
            if (_useSceneSearch && !string.IsNullOrEmpty(_searchObjectName))
            {
                // Если уже нашли и объект жив — возвращаем кеш
                if (_foundByNameObject != null)
                {
                    return _foundByNameObject;
                }

                // Если объект был найден ранее, но уничтожен — ищем заново
                if (_hasSearchedByName && _foundByNameObject == null)
                {
                    // Объект уничтожен — сбросить флаг, искать снова
                    _hasSearchedByName = false;
                    _hasLoggedSearchNotFoundWarning = false;
                }

                // Поиск
                _foundByNameObject = GameObject.Find(_searchObjectName);
                _hasSearchedByName = true;

                if (_foundByNameObject == null)
                {
                    if (!_waitForObject && !_hasLoggedSearchNotFoundWarning)
                    {
                        Debug.LogWarning(
                            $"[NeoCondition] GameObject.Find(\"{_searchObjectName}\") — объект не найден в сцене.");
                        _hasLoggedSearchNotFoundWarning = true;
                    }

                    return null;
                }

                // Нашли — сброс предупреждения (объект мог появиться позже)
                _hasLoggedSearchNotFoundWarning = false;
                return _foundByNameObject;
            }

            // Прямая ссылка или fallback
            if (IsSourceObjectDestroyed())
            {
                return null; // warning будет в EnsureCache*
            }

            return _sourceObject != null ? _sourceObject : fallbackObject;
        }

        private bool EnsureCache(GameObject fallbackObject)
        {
            if (_sourceMode == SourceMode.GameObject)
            {
                return EnsureCacheGameObject(fallbackObject);
            }

            return EnsureCacheComponent(fallbackObject);
        }

        private bool EnsureCacheComponent(GameObject fallbackObject)
        {
            // Проверка: закешированный компонент мог быть уничтожен
            if (_cacheValid && _cachedMember != null)
            {
                if (_cachedComponent == null)
                {
                    // Компонент уничтожен — сбрасываем кеш
                    LogDestroyedWarning($"Компонент '{_componentTypeName}' был уничтожен");
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
            if (target == null)
            {
                if (!_useSceneSearch && IsSourceObjectDestroyed())
                {
                    LogDestroyedWarning(
                        $"Source GameObject был уничтожен (ожидался объект для компонента '{_componentTypeName}')");
                }

                return false;
            }

            if (string.IsNullOrEmpty(_componentTypeName) || string.IsNullOrEmpty(_propertyName))
            {
                return false;
            }

            // Find component by type name
            Component[] components;
            try
            {
                components = target.GetComponents<Component>();
            }
            catch (MissingReferenceException)
            {
                LogDestroyedWarning($"GameObject уничтожен при поиске компонента '{_componentTypeName}'");
                return false;
            }

            foreach (Component comp in components)
            {
                if (comp == null)
                {
                    continue;
                }

                if (comp.GetType().FullName == _componentTypeName ||
                    comp.GetType().Name == _componentTypeName)
                {
                    _cachedComponent = comp;
                    break;
                }
            }

            if (_cachedComponent == null)
            {
                if (!_hasLoggedMissingMemberWarning)
                {
                    Debug.LogWarning($"[NeoCondition] Компонент '{_componentTypeName}' не найден на '{target.name}'.");
                    _hasLoggedMissingMemberWarning = true;
                }

                return false;
            }

            // Find member (field or property)
            Type type = _cachedComponent.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            PropertyInfo prop = type.GetProperty(_propertyName, flags);
            if (prop != null && prop.CanRead)
            {
                _cachedMember = prop;
                _cacheValid = true;
                return true;
            }

            FieldInfo field = type.GetField(_propertyName, flags);
            if (field != null)
            {
                _cachedMember = field;
                _cacheValid = true;
                return true;
            }

            if (!_hasLoggedMissingMemberWarning)
            {
                Debug.LogWarning(
                    $"[NeoCondition] Свойство/поле '{_propertyName}' не найдено в '{_componentTypeName}' на '{target.name}'.");
                _hasLoggedMissingMemberWarning = true;
            }

            return false;
        }

        private bool EnsureCacheGameObject(GameObject fallbackObject)
        {
            // Проверка: закешированный GO мог быть уничтожен
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
            if (target == null)
            {
                if (!_useSceneSearch && IsSourceObjectDestroyed())
                {
                    LogDestroyedWarning("Source GameObject был уничтожен (режим GameObject)");
                }

                return false;
            }

            if (string.IsNullOrEmpty(_propertyName))
            {
                return false;
            }

            _cachedGameObject = target;

            // Find member on GameObject
            Type type = typeof(GameObject);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo prop = type.GetProperty(_propertyName, flags);
            if (prop != null && prop.CanRead)
            {
                _cachedMember = prop;
                _cacheValid = true;
                return true;
            }

            FieldInfo field = type.GetField(_propertyName, flags);
            if (field != null)
            {
                _cachedMember = field;
                _cacheValid = true;
                return true;
            }

            if (!_hasLoggedMissingMemberWarning)
            {
                Debug.LogWarning($"[NeoCondition] Свойство '{_propertyName}' не найдено на GameObject.");
                _hasLoggedMissingMemberWarning = true;
            }

            return false;
        }

        /// <summary>
        ///     Проверяет, был ли _sourceObject задан (не через fallback), но уничтожен.
        ///     Unity: уничтоженный объект == null, но ReferenceEquals(obj, null) == false.
        /// </summary>
        private bool IsSourceObjectDestroyed()
        {
            // Если _sourceObject никогда не задавался — это нормально (используется fallback на self)
            // Unity: если ссылка была задана, но объект уничтожен, то:
            //   _sourceObject == null (Unity override) → true
            //   ReferenceEquals(_sourceObject, null) → false (C# ссылка жива, но обёртка мертва)
            return !ReferenceEquals(_sourceObject, null) && _sourceObject == null;
        }

        private void LogDestroyedWarning(string message)
        {
            if (!_hasLoggedDestroyedWarning)
            {
                Debug.LogWarning($"[NeoCondition] {message}. Условие будет возвращать false.");
                _hasLoggedDestroyedWarning = true;
            }
        }

        private object ReadValue()
        {
            if (_sourceMode == SourceMode.GameObject)
            {
                // Повторная проверка: GO мог быть уничтожен после EnsureCache
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

            // Повторная проверка: компонент мог быть уничтожен после EnsureCache
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

            return null;
        }

        private bool CompareInt(int value)
        {
            switch (_compareOp)
            {
                case CompareOp.Equal: return value == _thresholdInt;
                case CompareOp.NotEqual: return value != _thresholdInt;
                case CompareOp.Greater: return value > _thresholdInt;
                case CompareOp.Less: return value < _thresholdInt;
                case CompareOp.GreaterOrEqual: return value >= _thresholdInt;
                case CompareOp.LessOrEqual: return value <= _thresholdInt;
                default: return false;
            }
        }

        private bool CompareFloat(float value)
        {
            switch (_compareOp)
            {
                case CompareOp.Equal: return Mathf.Approximately(value, _thresholdFloat);
                case CompareOp.NotEqual: return !Mathf.Approximately(value, _thresholdFloat);
                case CompareOp.Greater: return value > _thresholdFloat;
                case CompareOp.Less: return value < _thresholdFloat;
                case CompareOp.GreaterOrEqual: return value >= _thresholdFloat;
                case CompareOp.LessOrEqual: return value <= _thresholdFloat;
                default: return false;
            }
        }

        private bool CompareBool(bool value)
        {
            switch (_compareOp)
            {
                case CompareOp.Equal: return value == _thresholdBool;
                case CompareOp.NotEqual: return value != _thresholdBool;
                default: return value == _thresholdBool;
            }
        }

        private bool CompareString(string value)
        {
            switch (_compareOp)
            {
                case CompareOp.Equal:
                    return string.Equals(value, _thresholdString, StringComparison.Ordinal);
                case CompareOp.NotEqual:
                    return !string.Equals(value, _thresholdString, StringComparison.Ordinal);
                default:
                    return string.Equals(value, _thresholdString, StringComparison.Ordinal);
            }
        }

        /// <summary>
        ///     Строковое описание условия для отладки.
        /// </summary>
        public override string ToString()
        {
            string src;
            if (_useSceneSearch && !string.IsNullOrEmpty(_searchObjectName))
            {
                string wait = _waitForObject ? ", wait" : "";
                src = $"Find(\"{_searchObjectName}\"{wait})";
            }
            else
            {
                src = _sourceObject != null ? _sourceObject.name : "self";
            }

            string prop = string.IsNullOrEmpty(_propertyName) ? "?" : _propertyName;
            string op = _compareOp.ToString();
            string threshold = _valueType switch
            {
                ValueType.Int => _thresholdInt.ToString(),
                ValueType.Float => _thresholdFloat.ToString("F2"),
                ValueType.Bool => _thresholdBool.ToString(),
                ValueType.String => $"\"{_thresholdString}\"",
                _ => "?"
            };
            string inv = _invert ? " [NOT]" : "";

            if (_sourceMode == SourceMode.GameObject)
            {
                return $"{src}.GO.{prop} {op} {threshold}{inv}";
            }

            string comp = string.IsNullOrEmpty(_componentTypeName) ? "?" : _componentTypeName;
            return $"{src}.{comp}.{prop} {op} {threshold}{inv}";
        }
    }
}