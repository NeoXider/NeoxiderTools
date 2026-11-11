using System;
using System.Collections.Generic;
using System.Globalization;
using Neo.Reactive;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.NoCode
{
    /// <summary>
    ///     Formats one or more numeric component bindings into a single text output.
    ///     Replaces domain-specific text wrappers such as HP/level/progression text components.
    /// </summary>
    [NeoDoc("NoCode/README.md")]
    [CreateFromMenu("Neoxider/NoCode/NoCode Formatted Text")]
    [AddComponentMenu("Neoxider/NoCode/" + nameof(NoCodeFormattedText))]
    public sealed class NoCodeFormattedText : MonoBehaviour
    {
        private const float DefaultPollIntervalSeconds = 0.16f;
        private const float MinPollIntervalSeconds = 0.016f;

        [Header("Sources")]
        [Tooltip("Numeric sources; element N is format argument {N}.")]
        [SerializeField]
        private ComponentFloatBinding[] _values = Array.Empty<ComponentFloatBinding>();

        [Tooltip(
            "Once: render on enable. Reactive: follow ReactiveProperty changes (ordinary fields fall back to polling). Poll: refresh every Poll Interval.")]
        [SerializeField]
        private NoCodeFloatUpdateMode _updateMode = NoCodeFloatUpdateMode.Reactive;

        [Tooltip("When Update Mode is Poll, refreshes in LateUpdate using Poll Interval.")] [SerializeField]
        private bool _pollInLateUpdate = true;

        [Tooltip(
            "Seconds between refreshes in Poll mode and Reactive fallback for ordinary fields. Default 0.16; minimum 0.016.")]
        [Min(MinPollIntervalSeconds)]
        [SerializeField]
        private float _pollIntervalSeconds = DefaultPollIntervalSeconds;

        [Header("Display")]
        [Tooltip("String.Format pattern. Example: '{0:0} / {1:0}' or 'Level {0:0} | XP {1:0}'.")]
        [SerializeField]
        private string _format = "{0}";

        [Tooltip("If set, formatting is pushed through SetText.")] [SerializeField]
        private SetText _setText;

        [Tooltip("Used when SetText is missing.")] [SerializeField]
        private TMP_Text _tmpText;

        [Tooltip("Optional legacy uGUI Text target.")] [SerializeField]
        private Text _uiText;

        [Header("Events")] [SerializeField] private UnityEvent<string> _onTextChanged = new();

        private readonly List<ReactivePropertyFloat> _subscribedFloats = new();
        private readonly List<ReactivePropertyInt> _subscribedInts = new();
        private readonly List<ReactivePropertyBool> _subscribedBools = new();
        private object[] _formatValues = Array.Empty<object>();
        private float[] _lastFloats = Array.Empty<float>();
        private object[] _rawValues = Array.Empty<object>();
        private bool[] _rawReadable = Array.Empty<bool>();
        private string _lastText;
        private bool _forceRender;
        private bool _resolved;
        private bool _reactiveSourcesSubscribed;
        private bool _useReactivePollFallback;
        private float _nextPollTime;
        private volatile bool _configurationDirty;

        /// <summary>Invoked with the new text whenever the rendered string changes.</summary>
        public UnityEvent<string> OnTextChanged => _onTextChanged;

        /// <summary>Last rendered text (null before the first render after enable).</summary>
        public string CurrentText => _lastText;

        /// <summary>Source bindings, one per format argument ({0}, {1}, ...). Binding setters apply on the next refresh.</summary>
        public IReadOnlyList<ComponentFloatBinding> Values => _values ?? Array.Empty<ComponentFloatBinding>();

        /// <summary>
        ///     <see cref="string.Format(IFormatProvider, string, object[])"/> pattern (invariant culture). Setting it
        ///     re-renders immediately when the component is active.
        /// </summary>
        public string Format
        {
            get => _format;
            set
            {
                if (string.Equals(_format, value, StringComparison.Ordinal))
                {
                    return;
                }

                _format = value;
                _forceRender = true;
                if (isActiveAndEnabled)
                {
                    RefreshFromSource();
                }
            }
        }

        /// <summary>How the text refreshes: once on enable, on reactive change (with poll fallback), or by polling.</summary>
        public NoCodeFloatUpdateMode UpdateMode
        {
            get => _updateMode;
            set
            {
                if (_updateMode == value)
                {
                    return;
                }

                _updateMode = value;
                _configurationDirty = true;
                if (isActiveAndEnabled)
                {
                    ReconcileDirtyConfiguration();
                }
            }
        }

        /// <summary>Seconds between refreshes in Poll mode and the Reactive fallback (clamped to at least 0.016).</summary>
        public float PollIntervalSeconds
        {
            get => Mathf.Max(MinPollIntervalSeconds, _pollIntervalSeconds);
            set => _pollIntervalSeconds = Mathf.Max(MinPollIntervalSeconds, value);
        }

        /// <summary>
        ///     Replaces the source bindings (copied) and optionally the format, then re-renders when active.
        ///     Typed C# path for the Inspector "Sources" list.
        /// </summary>
        /// <param name="values">New bindings; null clears the list.</param>
        /// <param name="format">New format, or null to keep the current one.</param>
        public void SetValues(IReadOnlyList<ComponentFloatBinding> values, string format = null)
        {
            UnsubscribeReactiveSources();
            int count = values?.Count ?? 0;
            ComponentFloatBinding[] copy = count == 0 ? Array.Empty<ComponentFloatBinding>() : new ComponentFloatBinding[count];
            for (int i = 0; i < count; i++)
            {
                copy[i] = values[i];
            }

            _values = copy;
            if (format != null)
            {
                _format = format;
            }

            InvalidateBindings();
            _forceRender = true;
            if (isActiveAndEnabled)
            {
                RefreshFromSource();
            }
        }

        private void OnValidate()
        {
            _pollIntervalSeconds = PollIntervalSeconds;
            _configurationDirty = true;
        }

        private void OnEnable()
        {
            _configurationDirty = false;
            InvalidateBindings();
            // WHY: targets may have been changed externally while disabled - force the next apply through.
            _lastText = null;
            _forceRender = false;
            _useReactivePollFallback = false;
            _nextPollTime = Time.unscaledTime + PollIntervalSeconds;
            ResolveReferences();
            // WHY: RefreshFromSource reads each source once and synchronises subscriptions from that read.
            RefreshFromSource();
        }

        private void OnDisable()
        {
            UnsubscribeReactiveSources();
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            ReconcileDirtyConfiguration();
            bool shouldPoll = _updateMode == NoCodeFloatUpdateMode.Poll ||
                              (_updateMode == NoCodeFloatUpdateMode.Reactive && _useReactivePollFallback);
            if (!shouldPoll || !_pollInLateUpdate)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextPollTime)
            {
                return;
            }

            RefreshFromSource();
            _nextPollTime = now + PollIntervalSeconds;
        }

        private void ReconcileDirtyConfiguration()
        {
            if (!_configurationDirty)
            {
                return;
            }

            _configurationDirty = false;
            InvalidateBindings();
            _forceRender = true;
            SubscribeReactiveSources();
        }

        private void ResolveReferences()
        {
            if (_setText == null)
            {
                _setText = GetComponent<SetText>();
            }

            if (_tmpText == null)
            {
                _tmpText = GetComponent<TMP_Text>();
            }

            if (_uiText == null)
            {
                _uiText = GetComponent<Text>();
            }

            _resolved = _setText != null || _tmpText != null || _uiText != null;
        }

        private void InvalidateBindings()
        {
            if (_values == null)
            {
                return;
            }

            for (int i = 0; i < _values.Length; i++)
            {
                _values[i]?.Invalidate();
            }
        }

        private void SubscribeReactiveSources()
        {
            SubscribeReactiveSources(null, null);
        }

        /// <summary>
        ///     Synchronises reactive subscriptions with the bindings. When <paramref name="rawValues"/> is supplied the
        ///     already read values are reused instead of reading every member again.
        /// </summary>
        private void SubscribeReactiveSources(object[] rawValues, bool[] readable)
        {
            if (!isActiveAndEnabled || _updateMode != NoCodeFloatUpdateMode.Reactive || _values == null)
            {
                UnsubscribeReactiveSources();
                return;
            }

            if (_reactiveSourcesSubscribed && ReactiveSourcesMatchBindings(rawValues, readable))
            {
                return;
            }

            UnsubscribeReactiveSources();
            _reactiveSourcesSubscribed = true;
            for (int i = 0; i < _values.Length; i++)
            {
                ComponentFloatBinding binding = _values[i];
                if (binding == null)
                {
                    continue;
                }

                if (TrySubscribeReactiveSource(binding, rawValues, readable, i))
                {
                    continue;
                }

                // WHY: ordinary and unresolved sources both need polling, including late spawns.
                _useReactivePollFallback = true;
            }
        }

        private bool TryGetReactiveSource(ComponentFloatBinding binding, object[] rawValues, bool[] readable,
            int index, out ReactivePropertyFloat reactiveFloat, out ReactivePropertyInt reactiveInt,
            out ReactivePropertyBool reactiveBool)
        {
            if (rawValues == null)
            {
                return binding.TryGetReactiveProperty(this, out reactiveFloat, out reactiveInt, out reactiveBool);
            }

            if (readable[index])
            {
                return ComponentFloatBinding.TryGetReactivePropertyFromValue(rawValues[index], out reactiveFloat,
                    out reactiveInt, out reactiveBool);
            }

            reactiveFloat = null;
            reactiveInt = null;
            reactiveBool = null;
            return false;
        }

        private bool ReactiveSourcesMatchBindings(object[] rawValues, bool[] readable)
        {
            int floatIndex = 0;
            int intIndex = 0;
            int boolIndex = 0;
            _useReactivePollFallback = false;
            for (int i = 0; i < _values.Length; i++)
            {
                ComponentFloatBinding binding = _values[i];
                if (binding == null)
                {
                    continue;
                }

                if (!TryGetReactiveSource(binding, rawValues, readable, i, out ReactivePropertyFloat reactiveFloat,
                        out ReactivePropertyInt reactiveInt, out ReactivePropertyBool reactiveBool))
                {
                    _useReactivePollFallback = true;
                    continue;
                }

                if (reactiveFloat != null)
                {
                    if (floatIndex >= _subscribedFloats.Count ||
                        !ReferenceEquals(_subscribedFloats[floatIndex++], reactiveFloat))
                    {
                        return false;
                    }
                }
                else if (reactiveInt != null)
                {
                    if (intIndex >= _subscribedInts.Count ||
                        !ReferenceEquals(_subscribedInts[intIndex++], reactiveInt))
                    {
                        return false;
                    }
                }
                else if (boolIndex >= _subscribedBools.Count ||
                         !ReferenceEquals(_subscribedBools[boolIndex++], reactiveBool))
                {
                    return false;
                }
            }

            return floatIndex == _subscribedFloats.Count && intIndex == _subscribedInts.Count &&
                   boolIndex == _subscribedBools.Count;
        }

        private bool TrySubscribeReactiveSource(ComponentFloatBinding binding, object[] rawValues, bool[] readable,
            int index)
        {
            if (!TryGetReactiveSource(binding, rawValues, readable, index, out ReactivePropertyFloat reactiveFloat,
                    out ReactivePropertyInt reactiveInt, out ReactivePropertyBool reactiveBool))
            {
                return false;
            }

            if (reactiveFloat != null)
            {
                reactiveFloat.AddListener(OnReactiveFloatChanged);
                _subscribedFloats.Add(reactiveFloat);
                return true;
            }

            if (reactiveInt != null)
            {
                reactiveInt.AddListener(OnReactiveIntChanged);
                _subscribedInts.Add(reactiveInt);
                return true;
            }

            reactiveBool.AddListener(OnReactiveBoolChanged);
            _subscribedBools.Add(reactiveBool);
            return true;
        }

        private void UnsubscribeReactiveSources()
        {
            for (int i = 0; i < _subscribedFloats.Count; i++)
            {
                _subscribedFloats[i]?.RemoveListener(OnReactiveFloatChanged);
            }

            for (int i = 0; i < _subscribedInts.Count; i++)
            {
                _subscribedInts[i]?.RemoveListener(OnReactiveIntChanged);
            }

            for (int i = 0; i < _subscribedBools.Count; i++)
            {
                _subscribedBools[i]?.RemoveListener(OnReactiveBoolChanged);
            }

            _subscribedFloats.Clear();
            _subscribedInts.Clear();
            _subscribedBools.Clear();
            _reactiveSourcesSubscribed = false;
            _useReactivePollFallback = false;
        }

        private void OnReactiveFloatChanged(float _)
        {
            if (isActiveAndEnabled)
            {
                RefreshFromSource();
            }
        }

        private void OnReactiveIntChanged(int _)
        {
            if (isActiveAndEnabled)
            {
                RefreshFromSource();
            }
        }

        private void OnReactiveBoolChanged(bool _)
        {
            if (isActiveAndEnabled)
            {
                RefreshFromSource();
            }
        }

        /// <summary>
        ///     Reads every source once, synchronises reactive subscriptions and re-renders when a value, the format
        ///     or the output target changed. <see cref="OnTextChanged"/> fires only when the text differs.
        /// </summary>
        public void RefreshFromSource()
        {
            ReconcileDirtyConfiguration();
            bool targetResolved = false;
            if (!_resolved || (_setText == null && _tmpText == null && _uiText == null))
            {
                ResolveReferences();
                targetResolved = _resolved;
            }

            int valueCount = _values?.Length ?? 0;
            if (_formatValues.Length != valueCount)
            {
                _formatValues = new object[valueCount];
                _lastFloats = new float[valueCount];
                _rawValues = new object[valueCount];
                _rawReadable = new bool[valueCount];
            }

            // WHY: one reflective read per source per refresh, shared by the subscription sync and formatting;
            // the former subscribe-then-read sequence read (and boxed) every plain member twice.
            for (int i = 0; i < valueCount; i++)
            {
                ComponentFloatBinding binding = _values[i];
                _rawReadable[i] = binding != null && binding.TryReadRaw(this, out _rawValues[i]);
            }

            SubscribeReactiveSources(_rawValues, _rawReadable);

            bool valuesChanged = false;
            for (int i = 0; i < valueCount; i++)
            {
                float value = _rawReadable[i] && ComponentFloatBinding.TryConvertToFloat(_rawValues[i], out float raw)
                    ? raw
                    : 0f;
                _rawValues[i] = null;
                // WHY: re-box only on change - poll mode reformats every interval and boxing each float
                // every tick is avoidable garbage.
                if (_formatValues[i] == null || !_lastFloats[i].Equals(value))
                {
                    _formatValues[i] = value;
                    _lastFloats[i] = value;
                    valuesChanged = true;
                }
            }

            // WHY: unchanged inputs, format and target produce the same string; skip string.Format garbage per poll.
            if (!valuesChanged && !targetResolved && !_forceRender && _lastText != null)
            {
                return;
            }

            string text;
            try
            {
                text = string.Format(CultureInfo.InvariantCulture, _format ?? string.Empty, _formatValues);
            }
            catch (FormatException)
            {
                text = _format ?? string.Empty;
            }

            // WHY: validation or a new target can require rendering without changing the notification baseline.
            bool textChanged = !string.Equals(text, _lastText, StringComparison.Ordinal);
            if (!textChanged && !targetResolved && !_forceRender)
            {
                return;
            }

            _lastText = text;
            _forceRender = false;
            ApplyText(text, textChanged);
        }

        private void ApplyText(string text, bool notify)
        {
            if (_setText != null)
            {
                _setText.Set(text);
            }
            else if (_tmpText != null)
            {
                _tmpText.text = text;
            }
            else if (_uiText != null)
            {
                _uiText.text = text;
            }

            if (notify)
            {
                _onTextChanged?.Invoke(text);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        ///     Edit Mode tests / editor utilities: apply binding when OnEnable did not run or after serialized rewiring.
        /// </summary>
        public void EditorInvokeRefreshFromSource()
        {
            RefreshFromSource();
        }
#endif
    }
}
