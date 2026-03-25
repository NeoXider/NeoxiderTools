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
        public enum BoolToFillMapping
        {
            Direct,
            Inverted
        }

        [Header("References")] [SerializeField]
        private Image _image;

        [Header("Settings")]
        [SerializeField]
        private float _duration = 0.5f;

        [SerializeField] private Ease _ease = Ease.OutQuad;

        [Header("Bool input (optional)")]
        [Tooltip("How bool (or 0/1) input is mapped to fillAmount.")]
        [SerializeField]
        private BoolToFillMapping _boolMapping = BoolToFillMapping.Direct;

        private Tween _anim;

        private void OnValidate()
        {
            _image ??= GetComponent<Image>();
        }

        /// <summary>Animates fillAmount to target value (0..1). Kills previous tween.</summary>
        public void SetValue(float value)
        {
            _anim?.Kill();
            _anim = DOTween.To(() => _image.fillAmount, x => _image.fillAmount = x, value, _duration).SetEase(_ease);
        }

        /// <summary>Animates fillAmount to 1 (true) or 0 (false) using Bool Mapping.</summary>
        public void SetBool(bool value)
        {
            float target = MapBoolToFill(value);
            SetValue(target);
        }

        /// <summary>
        ///     Animates fillAmount using 0/1 input (e.g. from Animator float/int parameter) with optional inversion.
        /// </summary>
        public void SetBool01(float value01)
        {
            bool value = value01 >= 0.5f;
            SetBool(value);
        }

        private float MapBoolToFill(bool value)
        {
            bool mapped = _boolMapping == BoolToFillMapping.Inverted ? !value : value;
            return mapped ? 1f : 0f;
        }
    }
}
