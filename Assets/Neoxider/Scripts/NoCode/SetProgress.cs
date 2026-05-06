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
        [Header("Targets")]
        [SerializeField] private Slider _slider;

        [SerializeField] private Image _image;

        [Header("Range")]
        [SerializeField] private float _minValue = 0f;

        [SerializeField] private float _maxValue = 1f;

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
