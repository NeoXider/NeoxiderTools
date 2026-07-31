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

        [Header("Sources")] [SerializeField]
        private ComponentFloatBinding[] _values = Array.Empty<ComponentFloatBinding>();

        [SerializeField] private NoCodeFloatUpdateMode _updateMode = NoCodeFloatUpdateMode.Reactive;

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
        private readonly List<ComponentFloatBinding> _pendingBindings = new();
        private object[] _formatValues = Array.Empty<object>();
        private float[] _lastFloats = Array.Empty<float>();
        private string _lastText;
        private bool _resolved;
        private bool _reactiveSourcesSubscribed;
        private bool _useReactivePollFallback;
        private float _nextPollTime;

        public UnityEvent<string> OnTextChanged => _onTextChanged;

        private float PollIntervalSeconds => Mathf.Max(MinPollIntervalSeconds, _pollIntervalSeconds);

        private void OnValidate()
        {
            InvalidateBindings();
            _pollIntervalSeconds = PollIntervalSeconds;
            _useReactivePollFallback = false;
            _lastText = null;
        }

        private void OnEnable()
        {
            // WHY: targets may have been changed externally while disabled - force the next apply through.
            _lastText = null;
            _useReactivePollFallback = false;
            _nextPollTime = Time.unscaledTime + PollIntervalSeconds;
            ResolveReferences();
            SubscribeReactiveSources();
            RefreshFromSource();
        }

        private void OnDisable()
        {
            UnsubscribeReactiveSources();
        }

        private void LateUpdate()
        {
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
            if (_updateMode != NoCodeFloatUpdateMode.Reactive || _values == null)
            {
                return;
            }

            if (_reactiveSourcesSubscribed)
            {
                RetryPendingReactiveSources();
                return;
            }

            _reactiveSourcesSubscribed = true;
            _pendingBindings.Clear();
            for (int i = 0; i < _values.Length; i++)
            {
                ComponentFloatBinding binding = _values[i];
                if (binding == null)
                {
                    continue;
                }

                if (TrySubscribeReactiveSource(binding))
                {
                    continue;
                }

                if (binding.TryReadFloat(this, out _))
                {
                    _useReactivePollFallback = true;
                    continue;
                }

                // WHY: source not resolvable yet (late spawn with Wait For Object / Find By Name).
                // Keep polling so the binding is retried until the object appears; otherwise the
                // text would freeze on the initial 0-formatted value forever.
                _pendingBindings.Add(binding);
                _useReactivePollFallback = true;
            }
        }

        private void RetryPendingReactiveSources()
        {
            for (int i = _pendingBindings.Count - 1; i >= 0; i--)
            {
                ComponentFloatBinding binding = _pendingBindings[i];
                if (binding == null || TrySubscribeReactiveSource(binding))
                {
                    _pendingBindings.RemoveAt(i);
                    continue;
                }

                if (binding.TryReadFloat(this, out _))
                {
                    // WHY: plain (non-reactive) member resolved; the active poll fallback covers it.
                    _pendingBindings.RemoveAt(i);
                }
            }
        }

        private bool TrySubscribeReactiveSource(ComponentFloatBinding binding)
        {
            if (!binding.TryGetReactiveProperty(this, out ReactivePropertyFloat reactiveFloat,
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
            _pendingBindings.Clear();
            _reactiveSourcesSubscribed = false;
        }

        private void OnReactiveFloatChanged(float _)
        {
            RefreshFromSource();
        }

        private void OnReactiveIntChanged(int _)
        {
            RefreshFromSource();
        }

        private void OnReactiveBoolChanged(bool _)
        {
            RefreshFromSource();
        }

        public void RefreshFromSource()
        {
            SubscribeReactiveSources();
            if (!_resolved)
            {
                ResolveReferences();
            }

            int valueCount = _values?.Length ?? 0;
            if (_formatValues.Length != valueCount)
            {
                _formatValues = new object[valueCount];
                _lastFloats = new float[valueCount];
            }

            for (int i = 0; i < valueCount; i++)
            {
                float value = _values[i] != null && _values[i].TryReadFloat(this, out float raw) ? raw : 0f;
                // WHY: re-box only on change - poll mode reformats every interval and boxing each float
                // every tick is avoidable garbage.
                if (_formatValues[i] == null || _lastFloats[i] != value)
                {
                    _formatValues[i] = value;
                    _lastFloats[i] = value;
                }
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

            // WHY: skip UI writes and the OnTextChanged event when the formatted result did not change (poll mode).
            if (string.Equals(text, _lastText, StringComparison.Ordinal))
            {
                return;
            }

            _lastText = text;
            ApplyText(text);
        }

        private void ApplyText(string text)
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

            _onTextChanged?.Invoke(text);
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
