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

        [Header("Settings")] [SerializeField] private float _duration = 0.5f;

        [SerializeField] private Ease _ease = Ease.OutQuad;

        [Tooltip("When enabled, inverts input value: value becomes (1 - value).")] [SerializeField]
        private bool _invertValue;

        [Tooltip("When enabled, the fill tween runs on unscaled time (e.g. progress UI over pause).")] [SerializeField]
        private bool _ignoreTimeScale;

        private Tween _anim;

        private void Awake()
        {
            _image ??= GetComponent<Image>();
        }

        private void OnValidate()
        {
            _image ??= GetComponent<Image>();
        }

        private void OnDisable()
        {
            KillTween();
        }

        private void OnDestroy()
        {
            KillTween();
        }

        /// <summary>Animates fillAmount to target value (0..1). Kills previous tween.</summary>
        public void SetValue(float value)
        {
            Image image = _image != null ? _image : GetComponent<Image>();
            _image = image;
            if (image == null)
            {
                KillTween();
                return;
            }

            float target = Sanitize(value);
            if (!isActiveAndEnabled)
            {
                SetValueImmediate(value);
                return;
            }

            if (_duration <= 0f)
            {
                SetValueImmediate(value);
                return;
            }

            KillTween();
            Image captured = image;
            _anim = DOTween.To(() => captured.fillAmount, x => captured.fillAmount = x, target, _duration)
                .SetEase(_ease)
                .SetRecyclable(false)
                .SetUpdate(_ignoreTimeScale);
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

        /// <summary>Applies fillAmount immediately (no tween), respecting inversion. Kills any running tween.</summary>
        public void SetValueImmediate(float value)
        {
            Image image = _image != null ? _image : GetComponent<Image>();
            _image = image;
            if (image == null)
            {
                KillTween();
                return;
            }

            KillTween();
            image.fillAmount = Sanitize(value);
        }

        private float Sanitize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 0f;
            }

            value = Mathf.Clamp01(value);
            if (_invertValue)
            {
                value = 1f - value;
            }

            return value;
        }

        private void KillTween()
        {
            if (_anim != null)
            {
                _anim.Kill();
                _anim = null;
            }
        }
    }
}
