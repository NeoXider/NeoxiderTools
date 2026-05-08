using Neo;
using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.NoCode
{
    /// <summary>
    ///     Reads a float from another component (field/property or <see cref="Neo.Reactive.ReactivePropertyFloat"/>)
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

        [Tooltip("Used when SetText is missing.")]
        [SerializeField]
        private TMP_Text _fallbackText;

        [Header("Time Display")]
        [Tooltip("If set, pushes float value (in seconds) to TimeToText.")]
        [SerializeField]
        private TimeToText _timeToText;

        // Cached resolved references (avoid GetComponent on every ApplyFloat)
        private SetText _resolvedSetText;
        private TimeToText _resolvedTimeToText;
        private TMP_Text _resolvedTmp;
        private bool _resolved;

        protected override void OnEnable()
        {
            ResolveReferences();
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
            // Lazy re-resolve: handles components added after OnEnable (edit-mode tests, runtime AddComponent)
            if (!_resolved) ResolveReferences();

            if (_resolvedSetText != null)
            {
                _resolvedSetText.Set(value);
                return;
            }

            if (_resolvedTimeToText != null)
            {
                _resolvedTimeToText.Set(value);
                return;
            }

            if (_resolvedTmp != null)
            {
                _resolvedTmp.text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
