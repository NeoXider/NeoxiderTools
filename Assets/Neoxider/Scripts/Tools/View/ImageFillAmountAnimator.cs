using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Tools
{
    [NeoDoc("Tools/View/ImageFillAmountAnimator.md")]
    [CreateFromMenu("Neoxider/Tools/View/ImageFillAmountAnimator")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ImageFillAmountAnimator))]
    public class ImageFillAmountAnimator : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Image _image;

        [Header("Settings")]
        [SerializeField]
        private float _duration = 0.5f;

        [SerializeField] private Ease _ease = Ease.OutQuad;

        [Tooltip("When enabled, inverts input value: value becomes (1 - value).")]
        [SerializeField]
        private bool _invertValue;

        private Tween _anim;

        private void OnValidate()
        {
            _image ??= GetComponent<Image>();
        }

        /// <summary>Animates fillAmount to target value (0..1). Kills previous tween.</summary>
        public void SetValue(float value)
        {
            value = Mathf.Clamp01(value);
            if (_invertValue)
            {
                value = 1f - value;
            }

            _anim?.Kill();
            _anim = DOTween.To(() => _image.fillAmount, x => _image.fillAmount = x, value, _duration).SetEase(_ease);
        }

        /// <summary>Animates fillAmount to 1 (true) or 0 (false) using Bool Mapping.</summary>
        public void SetBool(bool value)
        {
            SetValue(value ? 1f : 0f);
        }

        /// <summary>
        ///     Animates fillAmount using 0/1 input (e.g. from Animator float/int parameter) with optional inversion.
        /// </summary>
        public void SetBool01(float value01)
        {
            bool value = value01 >= 0.5f;
            SetBool(value);
        }
    }
}
