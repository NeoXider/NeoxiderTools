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

        protected override void ApplyFloat(float value)
        {
            SetText st = _setText != null ? _setText : GetComponent<SetText>();
            if (st != null)
            {
                st.Set(value);
                return;
            }

            TMP_Text tmp = _fallbackText != null ? _fallbackText : GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
