using System;
using System.Reflection;
using UnityEngine;

namespace Neo.Condition
{
    /// <summary>
    ///     Comparison type for a condition.
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
    ///     What to compare against: a constant (number/text) or another object's field/property.
    /// </summary>
    public enum ThresholdSource
    {
        /// <summary>Compare to a fixed number or string (threshold).</summary>
        Constant,

        /// <summary>Compare to a field/property on another object.</summary>
        OtherObject
    }

    /// <summary>
    ///     Type of the value read from a component.
    /// </summary>
    public enum ValueType
    {
        Int,
        Float,
        Bool,
        String
    }

    /// <summary>
    ///     Data source for the condition: a component or the GameObject itself.
    /// </summary>
    public enum SourceMode
    {
        /// <summary>Read field/property from a component.</summary>
        Component,

        /// <summary>Read GameObject properties (activeSelf, tag, layer, etc.).</summary>
        GameObject
    }

    /// <summary>
    ///     Argument type for a single-parameter method call (int/float/string).
    /// </summary>
    public enum ArgumentKind
    {
        Int,
        Float,
        String
    }

    /// <summary>
    ///     One condition entry: GameObject → Component/GameObject → field/property, compare op and threshold.
    ///     At runtime reads the value via reflection (cached) and compares.
    /// </summary>
    [Serializable]
    public class ConditionEntry : IConditionEvaluator
    {
        [Tooltip("Data source: Component (component fields) or GameObject (object properties).")] [SerializeField]
        private SourceMode _sourceMode = SourceMode.Component;

        [Tooltip("Find target GameObject by name in scene (instead of direct reference).")] [SerializeField]
        private bool _useSceneSearch;

        [Tooltip("GameObject name to find in scene via GameObject.Find().")] [SerializeField]
        private string _searchObjectName = "";

        [Tooltip("Do not log Warning if object is not found. Useful for prefabs that will appear later (spawn).")]
        [SerializeField]
        private bool _waitForObject;

        [Tooltip(
            "Prefab for previewing components/properties in the Editor when the object is not in the scene. Not used at runtime.")]
        [SerializeField]
        private GameObject _prefabPreview;

        [Tooltip("GameObject on which to find the component. If empty — NeoCondition's object is used.")]
        [SerializeField]
        private GameObject _sourceObject;

        [Tooltip("Component index in list (filled by Editor).")] [SerializeField]
        private int _componentIndex;

        [Tooltip("Full component type name (for restore after reload).")] [SerializeField]
        private string _componentTypeName = "";

        [Tooltip("Field or property name to read value from.")] [SerializeField]
        private string _propertyName = "";

        [Tooltip("Value type.")] [SerializeField]
        private ValueType _valueType = ValueType.Int;

        [Tooltip("Comparison operator.")] [SerializeField]
        private CompareOp _compareOp = CompareOp.Equal;

        [Tooltip("Invert result (NOT).")] [SerializeField]
        private bool _invert;

        [Tooltip("Compare with constant (number/text) or with another object's variable.")] [SerializeField]
        private ThresholdSource _thresholdSource = ThresholdSource.Constant;

        // Threshold values by type (when _thresholdSource == Constant)
        [SerializeField] private int _thresholdInt;
        [SerializeField] private float _thresholdFloat;
        [SerializeField] private bool _thresholdBool = true;
        [SerializeField] private string _thresholdString = "";

        // Other object reference (when _thresholdSource == OtherObject)
        [SerializeField] private SourceMode _otherSourceMode = SourceMode.Component;
        [SerializeField] private bool _otherUseSceneSearch;
        [SerializeField] private string _otherSearchObjectName = "";
        [SerializeField] private bool _otherWaitForObject;
        [SerializeField] private GameObject _otherSourceObject;
        [SerializeField] private int _otherComponentIndex;
        [SerializeField] private string _otherComponentTypeName = "";
        [SerializeField] private string _otherPropertyName = "";

        [SerializeField] private bool _isMethodWithArgument;
        [SerializeField] private ArgumentKind _propertyArgumentKind = ArgumentKind.Int;
        [SerializeField] private int _propertyArgumentInt;
        [SerializeField] private float _propertyArgumentFloat;
        [SerializeField] private string _propertyArgumentString = "";

        [SerializeField] private bool _otherIsMethodWithArgument;
        [SerializeField] private ArgumentKind _otherPropertyArgumentKind = ArgumentKind.Int;
        [SerializeField] private int _otherPropertyArgumentInt;
        [SerializeField] private float _otherPropertyArgumentFloat;
        [SerializeField] private string _otherPropertyArgumentString = "";

        [NonSerialized] private ConditionValueSource _leftSource;
        [NonSerialized] private ConditionValueSource _rightSource;

        /// <summary>
        ///     Data source: Component or GameObject.
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
        ///     Find target GameObject by name in the scene.
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
        ///     Do not log a warning if the object is not found yet (wait for spawn).
        /// </summary>
        public bool WaitForObject
        {
            get => _waitForObject;
            set => _waitForObject = value;
        }

        /// <summary>
        ///     Prefab for component preview in the Editor (not used at runtime).
        /// </summary>
        public GameObject PrefabPreview
        {
            get => _prefabPreview;
            set => _prefabPreview = value;
        }

        /// <summary>
        ///     GameObject name to find in the scene.
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
        ///     GameObject that provides the value.
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
        ///     Object found by name search (runtime, read-only).
        /// </summary>
        public GameObject FoundByNameObject => _leftSource?.FoundByNameObject;

        /// <summary>
        ///     Component index on the object.
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
        ///     Full component type name.
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
        ///     Field or property name.
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
        ///     Value type.
        /// </summary>
        public ValueType CurrentValueType
        {
            get => _valueType;
            set => _valueType = value;
        }

        /// <summary>
        ///     Comparison operator.
        /// </summary>
        public CompareOp Compare
        {
            get => _compareOp;
            set => _compareOp = value;
        }

        /// <summary>
        ///     Invert the result.
        /// </summary>
        public bool Invert
        {
            get => _invert;
            set => _invert = value;
        }

        /// <summary>
        ///     Compare to a constant or to another object's variable.
        /// </summary>
        public ThresholdSource ThresholdSource
        {
            get => _thresholdSource;
            set
            {
                _thresholdSource = value;
                _rightSource?.InvalidateCacheFull();
            }
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

        internal bool IsMethodWithArgument => _isMethodWithArgument;
        internal ArgumentKind PropertyArgumentKind => _propertyArgumentKind;
        internal int PropertyArgumentInt => _propertyArgumentInt;
        internal float PropertyArgumentFloat => _propertyArgumentFloat;
        internal string PropertyArgumentString => _propertyArgumentString;
        internal SourceMode OtherSourceMode => _otherSourceMode;
        internal bool OtherUseSceneSearch => _otherUseSceneSearch;
        internal string OtherSearchObjectName => _otherSearchObjectName;
        internal bool OtherWaitForObject => _otherWaitForObject;
        internal GameObject OtherSourceObject => _otherSourceObject;
        internal int OtherComponentIndex => _otherComponentIndex;
        internal string OtherComponentTypeName => _otherComponentTypeName;
        internal string OtherPropertyName => _otherPropertyName;
        internal bool OtherIsMethodWithArgument => _otherIsMethodWithArgument;
        internal ArgumentKind OtherPropertyArgumentKind => _otherPropertyArgumentKind;
        internal int OtherPropertyArgumentInt => _otherPropertyArgumentInt;
        internal float OtherPropertyArgumentFloat => _otherPropertyArgumentFloat;
        internal string OtherPropertyArgumentString => _otherPropertyArgumentString;

        /// <summary>
        ///     Evaluates the condition. Returns true if it passes.
        /// </summary>
        /// <param name="fallbackObject">Owner GameObject (used when sourceObject is empty).</param>
        public bool Evaluate(GameObject fallbackObject)
        {
            bool result = EvaluateInternal(fallbackObject);
            return _invert ? !result : result;
        }

        private ConditionValueSource GetLeftSource()
        {
            return _leftSource ??= new ConditionValueSource(this, false);
        }

        private ConditionValueSource GetRightSource()
        {
            return _rightSource ??= new ConditionValueSource(this, true);
        }

        /// <summary>
        ///     Clears reflection cache and warning flags.
        /// </summary>
        public void InvalidateCache()
        {
            _leftSource?.InvalidateCache();
        }

        /// <summary>
        ///     Full cache reset, including name-search cache and the second-object cache.
        /// </summary>
        public void InvalidateCacheFull()
        {
            _leftSource?.InvalidateCacheFull();
            _rightSource?.InvalidateCacheFull();
        }

        /// <summary>
        ///     When mode is OtherObject and Other Source Object is unset, uses the left-hand (source) object.
        ///     Call from NeoCondition in Start so that with None in the Inspector comparison uses the same object.
        /// </summary>
        public void BindOtherToSourceIfNull(GameObject fallbackObject)
        {
            if (_thresholdSource != ThresholdSource.OtherObject)
            {
                return;
            }

            if (_otherUseSceneSearch && !string.IsNullOrEmpty(_otherSearchObjectName))
            {
                return;
            }

            if (_otherSourceObject != null)
            {
                return;
            }

            _otherSourceObject = GetLeftSource().GetResolvedTarget(fallbackObject);
            _rightSource?.InvalidateCacheFull();
        }

        private bool EvaluateInternal(GameObject fallbackObject)
        {
            ConditionValueSource left = GetLeftSource();
            if (!left.EnsureCache(fallbackObject))
            {
                return false;
            }

            object rawValue;
            try
            {
                rawValue = left.ReadValue();
            }
            catch (MissingReferenceException)
            {
                left.InvalidateCache();
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NeoCondition] Error reading value ({_propertyName}): {ex.Message}");
                left.InvalidateCache();
                return false;
            }

            if (rawValue == null)
            {
                return false;
            }

            object thresholdValue;
            if (_thresholdSource == ThresholdSource.OtherObject)
            {
                ConditionValueSource right = GetRightSource();
                if (!right.EnsureCache(fallbackObject))
                {
                    return false;
                }

                try
                {
                    thresholdValue = right.ReadValue();
                }
                catch (MissingReferenceException)
                {
                    right.InvalidateCache();
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[NeoCondition] Error reading second variable ({_otherPropertyName}): {ex.Message}");
                    right.InvalidateCache();
                    return false;
                }

                if (thresholdValue == null)
                {
                    return false;
                }
            }
            else
            {
                thresholdValue = _valueType switch
                {
                    ValueType.Int => _thresholdInt,
                    ValueType.Float => _thresholdFloat,
                    ValueType.Bool => _thresholdBool,
                    ValueType.String => _thresholdString,
                    _ => null
                };
            }

            try
            {
                if (_thresholdSource == ThresholdSource.OtherObject)
                {
                    return _valueType switch
                    {
                        ValueType.Int => CompareInt(Convert.ToInt32(rawValue), Convert.ToInt32(thresholdValue)),
                        ValueType.Float => CompareFloat(Convert.ToSingle(rawValue), Convert.ToSingle(thresholdValue)),
                        ValueType.Bool => CompareBool(Convert.ToBoolean(rawValue), Convert.ToBoolean(thresholdValue)),
                        ValueType.String => CompareString(rawValue.ToString(), thresholdValue.ToString()),
                        _ => false
                    };
                }

                return _valueType switch
                {
                    ValueType.Int => CompareInt(Convert.ToInt32(rawValue)),
                    ValueType.Float => CompareFloat(Convert.ToSingle(rawValue)),
                    ValueType.Bool => CompareBool(Convert.ToBoolean(rawValue)),
                    ValueType.String => CompareString(rawValue.ToString()),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NeoCondition] Comparison error: {ex.Message}");
                return false;
            }
        }

        internal static bool IsSupportedParameterType(Type type, ArgumentKind kind)
        {
            if (kind == ArgumentKind.Int)
            {
                return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte);
            }

            if (kind == ArgumentKind.Float)
            {
                return type == typeof(float) || type == typeof(double);
            }

            if (kind == ArgumentKind.String)
            {
                return type == typeof(string);
            }

            return false;
        }

        internal static bool IsSupportedReturnType(Type type)
        {
            return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                   type == typeof(bool) || type == typeof(string) ||
                   type == typeof(long) || type == typeof(short) || type == typeof(byte);
        }

        internal static MethodInfo FindMethodWithOneArgument(Type type, string methodName, ArgumentKind argumentKind,
            BindingFlags flags)
        {
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                if (!IsSupportedParameterType(parameters[0].ParameterType, argumentKind))
                {
                    continue;
                }

                if (!IsSupportedReturnType(method.ReturnType))
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        private bool CompareInt(int value)
        {
            return CompareInt(value, _thresholdInt);
        }

        private static bool ApplyCompareOp(int a, int b, CompareOp op)
        {
            switch (op)
            {
                case CompareOp.Equal: return a == b;
                case CompareOp.NotEqual: return a != b;
                case CompareOp.Greater: return a > b;
                case CompareOp.Less: return a < b;
                case CompareOp.GreaterOrEqual: return a >= b;
                case CompareOp.LessOrEqual: return a <= b;
                default: return false;
            }
        }

        private bool CompareInt(int a, int b)
        {
            return ApplyCompareOp(a, b, _compareOp);
        }

        private bool CompareFloat(float value)
        {
            return CompareFloat(value, _thresholdFloat);
        }

        private static bool ApplyCompareOp(float a, float b, CompareOp op)
        {
            switch (op)
            {
                case CompareOp.Equal: return Mathf.Approximately(a, b);
                case CompareOp.NotEqual: return !Mathf.Approximately(a, b);
                case CompareOp.Greater: return a > b;
                case CompareOp.Less: return a < b;
                case CompareOp.GreaterOrEqual: return a >= b;
                case CompareOp.LessOrEqual: return a <= b;
                default: return false;
            }
        }

        private bool CompareFloat(float a, float b)
        {
            return ApplyCompareOp(a, b, _compareOp);
        }

        private bool CompareBool(bool value)
        {
            return CompareBool(value, _thresholdBool);
        }

        private bool CompareBool(bool a, bool b)
        {
            switch (_compareOp)
            {
                case CompareOp.Equal: return a == b;
                case CompareOp.NotEqual: return a != b;
                default: return a == b;
            }
        }

        private bool CompareString(string value)
        {
            return CompareString(value, _thresholdString);
        }

        private bool CompareString(string a, string b)
        {
            switch (_compareOp)
            {
                case CompareOp.Equal:
                    return string.Equals(a, b, StringComparison.Ordinal);
                case CompareOp.NotEqual:
                    return !string.Equals(a, b, StringComparison.Ordinal);
                default:
                    return string.Equals(a, b, StringComparison.Ordinal);
            }
        }

        /// <summary>
        ///     String representation of the condition for debugging.
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
            string thresholdStr;
            if (_thresholdSource == ThresholdSource.OtherObject)
            {
                string otherSrc = _otherUseSceneSearch && !string.IsNullOrEmpty(_otherSearchObjectName)
                    ? $"Find(\"{_otherSearchObjectName}\")"
                    : _otherSourceObject != null
                        ? _otherSourceObject.name
                        : "self";
                string otherComp = string.IsNullOrEmpty(_otherComponentTypeName) ? "?" : _otherComponentTypeName;
                string otherProp = string.IsNullOrEmpty(_otherPropertyName) ? "?" : _otherPropertyName;
                thresholdStr = _otherSourceMode == SourceMode.GameObject
                    ? $"{otherSrc}.GO.{otherProp}"
                    : $"{otherSrc}.{otherComp}.{otherProp}";
            }
            else
            {
                thresholdStr = _valueType switch
                {
                    ValueType.Int => _thresholdInt.ToString(),
                    ValueType.Float => _thresholdFloat.ToString("F2"),
                    ValueType.Bool => _thresholdBool.ToString(),
                    ValueType.String => $"\"{_thresholdString}\"",
                    _ => "?"
                };
            }

            string inv = _invert ? " [NOT]" : "";

            if (_sourceMode == SourceMode.GameObject)
            {
                return $"{src}.GO.{prop} {op} {thresholdStr}{inv}";
            }

            string comp = string.IsNullOrEmpty(_componentTypeName) ? "?" : _componentTypeName;
            return $"{src}.{comp}.{prop} {op} {thresholdStr}{inv}";
        }
    }
}
