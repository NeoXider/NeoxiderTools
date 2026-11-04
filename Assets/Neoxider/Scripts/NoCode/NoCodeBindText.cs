using Neo;
using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.NoCode
{
    /// <summary>
    ///     Reads a float from another component (field/property or supported reactive property)
    ///     and pushes it to <see cref="SetText"/> or <see cref="TMP_Text"/>.
    /// </summary>
    [NeoDoc("NoCode/README.md")]
    [CreateFromMenu("Neoxider/NoCode/NoCode Bind Text")]
    [AddComponentMenu("Neoxider/NoCode/" + nameof(NoCodeBindText))]
    public sealed class NoCodeBindText : NoCodeFloatBindingBehaviour
    {
        [Header("Display")]
        [Tooltip("If set, formatting matches SetText. Otherwise auto-find on this GameObject.")]
        [SerializeField]
        private SetText _setText;

        [Tooltip("Used when SetText is missing.")] [SerializeField]
        private TMP_Text _fallbackText;

        [Header("Time Display")] [Tooltip("If set, pushes float value (in seconds) to TimeToText.")] [SerializeField]
        private TimeToText _timeToText;

        // WHY: Cache resolved references to avoid GetComponent on every ApplyFloat.
        private SetText _resolvedSetText;
        private TimeToText _resolvedTimeToText;
        private TMP_Text _resolvedTmp;
        private bool _resolved;
        private bool _hasAppliedValue;
        private float _lastAppliedValue;
        private Object _lastAppliedTarget;

        protected override void OnValidate()
        {
            base.OnValidate();
            _resolved = false;
            _hasAppliedValue = false;
        }

        protected override void OnEnable()
        {
            ResolveReferences();
            // WHY: targets may have been changed externally while disabled - the first refresh always writes.
            _hasAppliedValue = false;
            base.OnEnable();
        }

        private void ResolveReferences()
        {
            _resolvedSetText = _setText != null ? _setText : GetComponent<SetText>();
            _resolvedTimeToText = _timeToText != null ? _timeToText : GetComponent<TimeToText>();
            _resolvedTmp = _fallbackText != null ? _fallbackText : GetComponent<TMP_Text>();
            _resolved = _resolvedSetText != null || _resolvedTimeToText != null || _resolvedTmp != null;
        }

        protected override void ApplyFloat(float value)
        {
            // WHY: Lazy re-resolve handles components added after OnEnable (edit-mode tests, runtime AddComponent).
            if (!_resolved || (_resolvedSetText == null && _resolvedTimeToText == null && _resolvedTmp == null))
            {
                ResolveReferences();
            }

            Object target = _resolvedSetText != null ? _resolvedSetText :
                _resolvedTimeToText != null ? (Object)_resolvedTimeToText : _resolvedTmp;
            // WHY: Poll mode refreshes every interval; re-sending an unchanged value restarted SetText's tween and
            // re-formatted the label each tick (closures + strings) for no visible change.
            if (_hasAppliedValue && target != null && target == _lastAppliedTarget && value.Equals(_lastAppliedValue))
            {
                return;
            }

            if (_resolvedSetText != null)
            {
                _resolvedSetText.Set(value);
            }
            else if (_resolvedTimeToText != null)
            {
                _resolvedTimeToText.Set(value);
            }
            else if (_resolvedTmp != null)
            {
                _resolvedTmp.text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            _hasAppliedValue = target != null;
            _lastAppliedValue = value;
            _lastAppliedTarget = target;
        }

        /// <summary>
        ///     Forgets the last written value so the next refresh writes the output again, even when the value is
        ///     unchanged (for example after another script overwrote the label).
        /// </summary>
        public void ForceRewrite()
        {
            _hasAppliedValue = false;
            RefreshBinding();
        }
    }
}
