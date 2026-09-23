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
        /// <summary>How a 0..1 value is shown on the image.</summary>
        public enum FillMode
        {
            /// <summary>Drives <see cref="Image.fillAmount" />. The image must be of type Filled.</summary>
            FillAmount,

            /// <summary>
            ///     Moves the image's right anchor between its authored start and end, so a 9-sliced
            ///     bar is stretched rather than cut and keeps its rounded caps at every value.
            /// </summary>
            SlicedWidth
        }

        [Header("References")] [SerializeField]
        private Image _image;

        [Header("Settings")] [SerializeField] private float _duration = 0.5f;

        [SerializeField] private Ease _ease = Ease.OutQuad;

        [Tooltip("When enabled, inverts input value: value becomes (1 - value).")] [SerializeField]
        private bool _invertValue;

        [Tooltip("When enabled, the fill tween runs on unscaled time (e.g. progress UI over pause).")] [SerializeField]
        private bool _ignoreTimeScale;

        [Tooltip("FillAmount cuts a Filled image. SlicedWidth stretches the image's own rect from its left edge, " +
                 "so a 9-sliced bar keeps its rounded end caps at every value.")]
        [SerializeField]
        private FillMode _mode = FillMode.FillAmount;

        [Tooltip("SlicedWidth only. Any non-zero value is shown at least this wide (canvas units), so the two " +
                 "end caps never overlap into a smudge. Zero itself hides the image.")]
        [Min(0f)]
        [SerializeField]
        private float _minVisibleWidth;

        private Tween _anim;
        private float _current;
        private bool _hasCurrent;
        private bool _captured;
        private float _startAnchor;
        private float _fullAnchor;

        public FillMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        /// <summary>The value currently displayed (0..1, after inversion).</summary>
        public float DisplayedValue => _mode == FillMode.FillAmount && _image != null ? _image.fillAmount : _current;

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

        /// <summary>Animates the fill to target value (0..1). Kills previous tween.</summary>
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
            if (_mode == FillMode.FillAmount)
            {
                _anim = DOTween.To(() => captured.fillAmount, x => captured.fillAmount = x, target, _duration)
                    .SetEase(_ease)
                    .SetRecyclable(false)
                    .SetUpdate(_ignoreTimeScale);
                return;
            }

            CaptureAnchors();
            float from = _hasCurrent ? _current : 0f;
            _anim = DOTween.To(() => from, x =>
                {
                    from = x;
                    ApplyWidth(x);
                }, target, _duration)
                .SetEase(_ease)
                .SetRecyclable(false)
                .SetUpdate(_ignoreTimeScale);
        }

        /// <summary>Animates the fill to 1 (true) or 0 (false) using Bool Mapping.</summary>
        public void SetBool(bool value)
        {
            SetValue(value ? 1f : 0f);
        }

        /// <summary>
        ///     Animates the fill using 0/1 input (e.g. from Animator float/int parameter) with optional inversion.
        /// </summary>
        public void SetBool01(float value01)
        {
            bool value = value01 >= 0.5f;
            SetBool(value);
        }

        /// <summary>Applies the fill immediately (no tween), respecting inversion. Kills any running tween.</summary>
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
            float sanitized = Sanitize(value);
            if (_mode == FillMode.FillAmount)
            {
                image.fillAmount = sanitized;
                return;
            }

            CaptureAnchors();
            ApplyWidth(sanitized);
        }

        /// <summary>
        ///     Remembers where the authored rect starts and ends. Value 1 is the rect exactly as it was
        ///     laid out, value 0 collapses it onto its left anchor; the offsets (insets inside a track) are
        ///     never touched, which is what lets the same fill sit inside tracks of any width.
        /// </summary>
        private void CaptureAnchors()
        {
            if (_captured)
            {
                return;
            }

            RectTransform rect = _image.rectTransform;
            _startAnchor = rect.anchorMin.x;
            _fullAnchor = rect.anchorMax.x;
            _captured = true;
        }

        private void ApplyWidth(float value)
        {
            _current = value;
            _hasCurrent = true;
            if (_image == null)
            {
                return;
            }

            RectTransform rect = _image.rectTransform;
            float anchor = Mathf.Lerp(_startAnchor, _fullAnchor, value);

            if (value > 0f && _minVisibleWidth > 0f && rect.parent is RectTransform parent)
            {
                // width = parentWidth * (anchorMax - anchorMin) + (offsetMax - offsetMin); solve for the
                // smallest anchorMax that still reaches the minimum.
                float parentWidth = parent.rect.width;
                if (parentWidth > 0f)
                {
                    float offsets = rect.offsetMax.x - rect.offsetMin.x;
                    float minAnchor = _startAnchor + (_minVisibleWidth - offsets) / parentWidth;
                    anchor = Mathf.Clamp(Mathf.Max(anchor, minAnchor), _startAnchor, Mathf.Max(_fullAnchor, _startAnchor));
                }
            }

            Vector2 max = rect.anchorMax;
            max.x = anchor;
            rect.anchorMax = max;
            _image.enabled = value > 0f;
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
