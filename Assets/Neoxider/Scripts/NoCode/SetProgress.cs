using Neo;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.NoCode
{
    /// <summary>
    ///     Maps a bound float through <see cref="Mathf.InverseLerp"/> into <see cref="Slider.normalizedValue"/> or
    ///     <see cref="Image.fillAmount"/> (Filling sprite mode).
    /// </summary>
    [NeoDoc("NoCode/README.md")]
    [CreateFromMenu("Neoxider/NoCode/SetProgress")]
    [AddComponentMenu("Neoxider/NoCode/" + nameof(SetProgress))]
    public sealed class SetProgress : NoCodeFloatBindingBehaviour
    {
        [Header("Targets")] [Tooltip("Optional Slider driven through normalizedValue.")] [SerializeField]
        private Slider _slider;

        [Tooltip("Optional Image driven through fillAmount (Image Type = Filled).")] [SerializeField]
        private Image _image;

        [Header("Range")] [Tooltip("Source value shown as an empty bar.")] [SerializeField]
        private float _minValue = 0f;

        [Tooltip("Source value shown as a full bar. Equal to Min disables the output.")] [SerializeField]
        private float _maxValue = 1f;

        /// <summary>Source value mapped to 0 (empty).</summary>
        public float MinValue
        {
            get => _minValue;
            set => _minValue = value;
        }

        /// <summary>Source value mapped to 1 (full), e.g. the current max HP.</summary>
        public float MaxValue
        {
            get => _maxValue;
            set => _maxValue = value;
        }

        /// <summary>
        ///     Sets both range ends (for example when max HP changes) and re-applies the bound value.
        /// </summary>
        public void SetRange(float minValue, float maxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
            if (isActiveAndEnabled)
            {
                RefreshBinding();
            }
        }

        protected override void ApplyFloat(float value)
        {
            if (Mathf.Approximately(_minValue, _maxValue))
            {
                return;
            }

            float t = Mathf.InverseLerp(_minValue, _maxValue, value);
            t = Mathf.Clamp01(t);
            if (_slider != null)
            {
                _slider.normalizedValue = t;
            }

            if (_image != null)
            {
                _image.fillAmount = t;
            }
        }
    }
}
