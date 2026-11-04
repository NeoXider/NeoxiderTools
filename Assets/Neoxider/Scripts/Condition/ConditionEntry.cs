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
    ///     One condition entry: GameObject -> Component/GameObject -> field/property, compare op and threshold.
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
            "Seconds between GameObject.Find attempts while the object is still missing. Only when Find By Name is used. 0 = retry on every check (no throttle).")]
        [SerializeField]
        private float _findRetryIntervalSeconds = BindingSourceGameObjectResolver.DefaultFindRetryIntervalSeconds;

        [Tooltip(
            "Prefab for previewing components/properties in the Editor when the object is not in the scene. Not used at runtime.")]
        [SerializeField]
        private GameObject _prefabPreview;

        [Tooltip("GameObject on which to find the component. If empty - NeoCondition's object is used.")]
        [SerializeField]
        private GameObject _sourceObject;

        [Tooltip("Component index in list (filled by Editor).")] [SerializeField]
        private int _componentIndex;

        [Tooltip("Zero-based occurrence among matching component types. Negative or missing occurrences are unavailable.")]
        [SerializeField]
        private int _componentOccurrence;

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

        // WHY: threshold values by type, used only when _thresholdSource == Constant
        [SerializeField] private int _thresholdInt;
        [SerializeField] private float _thresholdFloat;
        [SerializeField] private bool _thresholdBool = true;
        [SerializeField] private string _thresholdString = "";

        // WHY: other-object reference, used only when _thresholdSource == OtherObject
        [SerializeField] private SourceMode _otherSourceMode = SourceMode.Component;
        [SerializeField] private bool _otherUseSceneSearch;
        [SerializeField] private string _otherSearchObjectName = "";
        [SerializeField] private bool _otherWaitForObject;

        [Tooltip(
            "Other-object branch: seconds between Find retries when Other Find By Name finds nothing. 0 = every check.")]
        [SerializeField]
        private float _otherFindRetryIntervalSeconds = BindingSourceGameObjectResolver.DefaultFindRetryIntervalSeconds;

        [SerializeField] private GameObject _otherSourceObject;
        [SerializeField] private int _otherComponentIndex;
        [Tooltip("Other source: zero-based occurrence among matching component types. Negative or missing occurrences are unavailable.")]
        [SerializeField]
        private int _otherComponentOccurrence;
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
        ///     Seconds between <see cref="GameObject.Find"/> retries while the target is still missing (Find By Name only).
        ///     0 = retry on every evaluation (no throttle).
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
                    InvalidateCacheFull();
                }
            }
        }

        /// <summary>
        ///     Same as <see cref="FindRetryIntervalSeconds"/> for the &quot;Other object&quot; threshold branch when it uses Find By Name.
        /// </summary>
        public float OtherFindRetryIntervalSeconds
        {
            get => _otherFindRetryIntervalSeconds;
            set
            {
                float v = Mathf.Max(0f, value);
                if (!Mathf.Approximately(_otherFindRetryIntervalSeconds, v))
                {
                    _otherFindRetryIntervalSeconds = v;
                    InvalidateCacheFull();
                }
            }
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
        ///     Legacy Inspector component-list index. Runtime selection uses <see cref="ComponentOccurrence"/>.
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
        ///     Zero-based occurrence among components matching <see cref="ComponentTypeName"/> in GetComponents order.
        ///     Defaults to zero; negative or missing occurrences are unavailable. Independent of <see cref="ComponentIndex"/>.
        /// </summary>
        public int ComponentOccurrence
        {
            get => _componentOccurrence;
            set
            {
                _componentOccurrence = value;
                InvalidateCache();
            }
        }

        /// <summary>
        ///     Zero-based occurrence of the matching component on the Other Object source. Defaults to zero;
        ///     negative or missing occurrences are unavailable. Does not reinterpret the legacy Other Component Index.
        /// </summary>
        public int OtherComponentOccurrence
        {
            get => _otherComponentOccurrence;
            set
            {
                _otherComponentOccurrence = value;
                _rightSource?.InvalidateCache();
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

        /// <summary>Constant threshold for <see cref="ValueType.Int"/> comparisons.</summary>
        public int ThresholdInt
        {
            get => _thresholdInt;
            set => _thresholdInt = value;
        }

        /// <summary>Constant threshold for <see cref="ValueType.Float"/> comparisons (Equal uses Mathf.Approximately).</summary>
        public float ThresholdFloat
        {
            get => _thresholdFloat;
            set => _thresholdFloat = value;
        }

        /// <summary>Constant threshold for <see cref="ValueType.Bool"/> comparisons (only Equal / NotEqual).</summary>
        public bool ThresholdBool
        {
            get => _thresholdBool;
            set => _thresholdBool = value;
        }

        /// <summary>Constant threshold for <see cref="ValueType.String"/> comparisons (ordinal, Equal / NotEqual).</summary>
        public string ThresholdString
        {
            get => _thresholdString;
            set => _thresholdString = value;
        }

        /// <summary>
        ///     When true, <see cref="PropertyName"/> names a single-argument method that is invoked with the
        ///     argument of <see cref="PropertyArgumentKind"/>.
        /// </summary>
        public bool IsMethodWithArgument
        {
            get => _isMethodWithArgument;
            set
            {
                _isMethodWithArgument = value;
                InvalidateCache();
            }
        }

        /// <summary>Argument kind used to pick the method overload (int / float / string).</summary>
        public ArgumentKind PropertyArgumentKind
        {
            get => _propertyArgumentKind;
            set
            {
                _propertyArgumentKind = value;
                InvalidateCache();
            }
        }

        /// <summary>Integer method argument; read on every evaluation.</summary>
        public int PropertyArgumentInt
        {
            get => _propertyArgumentInt;
            set => _propertyArgumentInt = value;
        }

        /// <summary>Float method argument; read on every evaluation.</summary>
        public float PropertyArgumentFloat
        {
            get => _propertyArgumentFloat;
            set => _propertyArgumentFloat = value;
        }

        /// <summary>String method argument; read on every evaluation.</summary>
        public string PropertyArgumentString
        {
            get => _propertyArgumentString;
            set => _propertyArgumentString = value;
        }

        /// <summary>Other-object branch: read from a component or from the GameObject itself.</summary>
        public SourceMode OtherSourceMode
        {
            get => _otherSourceMode;
            set
            {
                _otherSourceMode = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: find the source by name with <see cref="GameObject.Find(string)"/>.</summary>
        public bool OtherUseSceneSearch
        {
            get => _otherUseSceneSearch;
            set
            {
                _otherUseSceneSearch = value;
                _rightSource?.InvalidateCacheFull();
            }
        }

        /// <summary>Other-object branch: GameObject name for <see cref="OtherUseSceneSearch"/>.</summary>
        public string OtherSearchObjectName
        {
            get => _otherSearchObjectName;
            set
            {
                _otherSearchObjectName = value;
                _rightSource?.InvalidateCacheFull();
            }
        }

        /// <summary>Other-object branch: suppress the warning while the named object is still missing.</summary>
        public bool OtherWaitForObject
        {
            get => _otherWaitForObject;
            set => _otherWaitForObject = value;
        }

        /// <summary>
        ///     Other-object branch: explicit source GameObject. When unset, NeoCondition binds it to the left-hand
        ///     source in Start (<see cref="BindOtherToSourceIfNull"/>); direct callers fall back to the context.
        /// </summary>
        public GameObject OtherSourceObject
        {
            get => _otherSourceObject;
            set
            {
                _otherSourceObject = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: legacy Inspector component-list index. Runtime uses <see cref="OtherComponentOccurrence"/>.</summary>
        public int OtherComponentIndex
        {
            get => _otherComponentIndex;
            set
            {
                _otherComponentIndex = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: full or short component type name.</summary>
        public string OtherComponentTypeName
        {
            get => _otherComponentTypeName;
            set
            {
                _otherComponentTypeName = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: field, property or single-argument method name.</summary>
        public string OtherPropertyName
        {
            get => _otherPropertyName;
            set
            {
                _otherPropertyName = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: <see cref="OtherPropertyName"/> names a single-argument method.</summary>
        public bool OtherIsMethodWithArgument
        {
            get => _otherIsMethodWithArgument;
            set
            {
                _otherIsMethodWithArgument = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: argument kind used to pick the method overload.</summary>
        public ArgumentKind OtherPropertyArgumentKind
        {
            get => _otherPropertyArgumentKind;
            set
            {
                _otherPropertyArgumentKind = value;
                _rightSource?.InvalidateCache();
            }
        }

        /// <summary>Other-object branch: integer method argument; read on every evaluation.</summary>
        public int OtherPropertyArgumentInt
        {
            get => _otherPropertyArgumentInt;
            set => _otherPropertyArgumentInt = value;
        }

        /// <summary>Other-object branch: float method argument; read on every evaluation.</summary>
        public float OtherPropertyArgumentFloat
        {
            get => _otherPropertyArgumentFloat;
            set => _otherPropertyArgumentFloat = value;
        }

        /// <summary>Other-object branch: string method argument; read on every evaluation.</summary>
        public string OtherPropertyArgumentString
        {
            get => _otherPropertyArgumentString;
            set => _otherPropertyArgumentString = value;
        }

        /// <summary>
        ///     Evaluates the condition. Returns true if it passes.
        /// </summary>
        /// <param name="fallbackObject">Owner GameObject (used when sourceObject is empty).</param>
        public bool Evaluate(GameObject fallbackObject)
        {
            bool result = TryCompare(fallbackObject, out bool comparison) && comparison;
            return _invert ? !result : result;
        }

        /// <summary>
        ///     Evaluates the condition and reports whether a comparison actually took place.
        ///     Unlike <see cref="Evaluate"/>, a missing source, missing member, null value, read exception or
        ///     conversion failure is not folded into <c>false</c> (and therefore not flipped to <c>true</c> by
        ///     <see cref="Invert"/>): the method returns false and <paramref name="result"/> is false.
        /// </summary>
        /// <param name="fallbackObject">Owner GameObject (used when the source object is empty).</param>
        /// <param name="result">Comparison result with <see cref="Invert"/> applied; false when unavailable.</param>
        /// <returns>True when the values were read and compared.</returns>
        public bool TryEvaluate(GameObject fallbackObject, out bool result)
        {
            if (!TryCompare(fallbackObject, out bool comparison))
            {
                result = false;
                return false;
            }

            result = _invert ? !comparison : comparison;
            return true;
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

            // WHY: only an unassigned field binds; an Editor placeholder for an unassigned field counts as unassigned,
            // while a live or destroyed explicit reference keeps the user's choice.
            if (_otherSourceObject != null || BindingSourceGameObjectResolver.IsDestroyedReference(_otherSourceObject))
            {
                return;
            }

            _otherSourceObject = GetLeftSource().GetResolvedTarget(fallbackObject);
            _rightSource?.InvalidateCacheFull();
        }

        /// <summary>
        ///     Reads both sides and compares them. Returns false when any side is unavailable or the comparison throws;
        ///     <paramref name="comparison"/> is the raw result before <see cref="Invert"/>.
        /// </summary>
        private bool TryCompare(GameObject fallbackObject, out bool comparison)
        {
            comparison = false;
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
                NeoDiagnostics.LogWarning($"[NeoCondition] Error reading value ({_propertyName}): {ex.Message}");
                left.InvalidateCache();
                return false;
            }

            if (rawValue == null)
            {
                return false;
            }

            // WHY: constant thresholds are compared straight from the typed fields; boxing them into an object on
            // every evaluation was pure garbage for the common Constant case.
            object thresholdValue = null;
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
                    NeoDiagnostics.LogWarning(
                        $"[NeoCondition] Error reading second variable ({_otherPropertyName}): {ex.Message}");
                    right.InvalidateCache();
                    return false;
                }

                if (thresholdValue == null)
                {
                    return false;
                }
            }

            try
            {
                if (_thresholdSource == ThresholdSource.OtherObject)
                {
                    switch (_valueType)
                    {
                        case ValueType.Int:
                            comparison = CompareInt(Convert.ToInt32(rawValue), Convert.ToInt32(thresholdValue));
                            return true;
                        case ValueType.Float:
                            comparison = CompareFloat(Convert.ToSingle(rawValue), Convert.ToSingle(thresholdValue));
                            return true;
                        case ValueType.Bool:
                            comparison = CompareBool(Convert.ToBoolean(rawValue), Convert.ToBoolean(thresholdValue));
                            return true;
                        case ValueType.String:
                            comparison = CompareString(rawValue.ToString(), thresholdValue.ToString());
                            return true;
                        default:
                            return false;
                    }
                }

                switch (_valueType)
                {
                    case ValueType.Int:
                        comparison = CompareInt(Convert.ToInt32(rawValue));
                        return true;
                    case ValueType.Float:
                        comparison = CompareFloat(Convert.ToSingle(rawValue));
                        return true;
                    case ValueType.Bool:
                        comparison = CompareBool(Convert.ToBoolean(rawValue));
                        return true;
                    case ValueType.String:
                        comparison = CompareString(rawValue.ToString());
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                NeoDiagnostics.LogWarning($"[NeoCondition] Comparison error: {ex.Message}");
                comparison = false;
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
                if (method.Name != methodName || method.ContainsGenericParameters)
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
