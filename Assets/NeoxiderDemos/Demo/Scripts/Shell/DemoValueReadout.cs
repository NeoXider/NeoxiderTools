using System.Globalization;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Formats a value into a label from a persistent UnityEvent (Slider.onValueChanged → <see cref="SetFloat" />,
    ///     Toggle.onValueChanged → <see cref="SetBool" />, TMP_Dropdown.onValueChanged → <see cref="SetInt" />), so
    ///     authored demo rows show their live value without controller code. Event-driven, no Update loop.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Demo Value Readout")]
    [DisallowMultipleComponent]
    public sealed class DemoValueReadout : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        [Tooltip("Numeric format passed to ToString (for example 0.## or 0%).")]
        [SerializeField] private string _format = "0.##";

        [SerializeField] private string _prefix = string.Empty;
        [SerializeField] private string _suffix = string.Empty;
        [SerializeField] private string _onText = "ON";
        [SerializeField] private string _offText = "OFF";

        public TMP_Text Target => _text;

        public string LastText { get; private set; }

        public void SetFloat(float value)
        {
            Write(value.ToString(_format, CultureInfo.InvariantCulture));
        }

        public void SetInt(int value)
        {
            Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetBool(bool value)
        {
            Write(value ? _onText : _offText);
        }

        public void SetText(string value)
        {
            Write(value ?? string.Empty);
        }

        /// <summary>Configures the readout. Used by the editor kit and by code-built frames.</summary>
        public void Configure(TMP_Text text, string format, string prefix = "", string suffix = "")
        {
            _text = text;
            _format = string.IsNullOrEmpty(format) ? "0.##" : format;
            _prefix = prefix ?? string.Empty;
            _suffix = suffix ?? string.Empty;
        }

        private void Write(string value)
        {
            LastText = _prefix + value + _suffix;
            if (_text != null)
            {
                _text.text = LastText;
            }
        }
    }
}
