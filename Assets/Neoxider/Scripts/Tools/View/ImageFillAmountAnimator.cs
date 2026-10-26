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
        private Image _boundImage;
        private float _current;
        private bool _hasCurrent;
        private bool _captured;
        private float _startAnchor;
        private float _fullAnchor;
        private float _target;
        private bool _hasTarget;
        private bool _pointAnchorsWarned;

        /// <summary>
        ///     Fill mode. Leaving <see cref="FillMode.SlicedWidth" /> stops the running tween and restores the
        ///     authored rect (right anchor and visibility), so the other mode starts from a full, visible image.
        /// </summary>
        public FillMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value)
                {
                    return;
                }

                KillTween();
                if (_mode == FillMode.SlicedWidth)
                {
                    RestoreAuthoredWidth();
                }

                _mode = value;
            }
        }

        /// <summary>
        ///     The value currently displayed (0..1, after inversion). In <see cref="FillMode.SlicedWidth" /> the
        ///     untouched authored rect counts as 1.
        /// </summary>
        public float DisplayedValue
        {
            get
            {
                if (_mode == FillMode.FillAmount)
                {
                    return _image != null ? _image.fillAmount : _current;
                }

                return _hasCurrent && ReferenceEquals(_boundImage, _image) ? _current : 1f;
            }
        }

        private void Awake()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }

        private void OnValidate()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }

        private void OnDisable()
        {
            Image image = _boundImage;
            bool wasAnimating = _anim != null && _anim.IsActive();
            KillTween();

            // WHY: a page closing mid-animation would otherwise reopen with a stale bar. The target is written
            // directly instead of Tween.Complete(), whose setter closure may still hold a destroyed Image.
            if (wasAnimating && _hasTarget && image != null && ReferenceEquals(_image, image))
            {
                ApplyValue(_target);
            }
        }

        private void OnDestroy()
        {
            KillTween();
        }

        /// <summary>Animates the fill to target value (0..1). Kills previous tween.</summary>
        public void SetValue(float value)
        {
            Image image = ResolveImage();
            if (image == null)
            {
                KillTween();
                return;
            }

            float target = Sanitize(value);
            _target = target;
            _hasTarget = true;
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
            if (image == null || !ReferenceEquals(_image, image))
            {
                return;
            }

            FillMode mode = _mode;
            if (mode != FillMode.FillAmount)
            {
                CaptureAnchors();
            }

            float from = mode == FillMode.FillAmount ? image.fillAmount : _current;
            Tween owned = null;
            owned = DOTween.To(() =>
                {
                    if (!CanApplyTween(image, owned))
                    {
                        return from;
                    }

                    return mode == FillMode.FillAmount ? image.fillAmount : from;
                }, x =>
                {
                    if (!CanApplyTween(image, owned))
                    {
                        return;
                    }

                    from = x;
                    if (mode == FillMode.FillAmount)
                    {
                        image.fillAmount = x;
                    }
                    else
                    {
                        ApplyWidth(image, x, owned);
                    }
                }, target, _duration)
                .SetEase(_ease)
                .SetRecyclable(false)
                .SetUpdate(_ignoreTimeScale);
            _anim = owned;
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
            Image image = ResolveImage();
            if (image == null)
            {
                KillTween();
                return;
            }

            KillTween();
            float sanitized = Sanitize(value);
            _target = sanitized;
            _hasTarget = true;
            ApplyValue(sanitized);
        }

        private void ApplyValue(float sanitized)
        {
            Image image = _image;
            if (image == null || !ReferenceEquals(_boundImage, image))
            {
                return;
            }

            if (_mode == FillMode.FillAmount)
            {
                image.fillAmount = sanitized;
                return;
            }

            CaptureAnchors();
            ApplyWidth(image, sanitized);
        }

        private Image ResolveImage()
        {
            Image image = _image != null ? _image : GetComponent<Image>();
            if (!ReferenceEquals(_boundImage, image))
            {
                KillTween();
                _boundImage = image;
                _captured = false;
                _hasCurrent = false;
                _hasTarget = false;
            }

            _image = image;
            return image;
        }

        private bool CanApplyTween(Image image, Tween owned)
        {
            if (this != null && isActiveAndEnabled && image != null &&
                ReferenceEquals(_image, image) && ReferenceEquals(_anim, owned))
            {
                return true;
            }

            // A retired callback may still run inside DOTween's current update; retire only its own handle.
            RetireTween(owned);
            return false;
        }

        /// <summary>
        ///     Remembers where the authored rect starts and ends. Value 1 is the rect exactly as it was
        ///     laid out, value 0 collapses it onto its left anchor; the offsets (insets inside a track) are
        ///     never touched, which is what lets the same fill sit inside tracks of any width.
        /// </summary>
        private void CaptureAnchors()
        {
            if (!_captured)
            {
                RectTransform rect = _image.rectTransform;
                _startAnchor = rect.anchorMin.x;
                _fullAnchor = rect.anchorMax.x;
                _captured = true;

                // WHY: the authored rect is value 1, so the first tween grows or shrinks from full instead of
                // snapping the bar empty, and DisplayedValue matches what is on screen.
                _current = 1f;
                _hasCurrent = true;
            }

            // WHY: the latch is set only when the warning is actually emitted, so turning NeoDiagnostics warnings
            // on later still reports the misconfiguration for this instance.
            if (Mathf.Approximately(_startAnchor, _fullAnchor) && !_pointAnchorsWarned &&
                NeoDiagnostics.RuntimeWarningsEnabled)
            {
                _pointAnchorsWarned = true;
                NeoDiagnostics.LogWarning(
                    $"[{nameof(ImageFillAmountAnimator)}] '{_image.gameObject.name}': SlicedWidth needs the fill " +
                    "horizontally stretch-anchored (anchorMin.x < anchorMax.x). With point anchors the width " +
                    "cannot change: only value 0 has an effect, and it hides the image.",
                    _image);
            }
        }

        private void RestoreAuthoredWidth()
        {
            Image image = _image;
            if (_captured && image != null && ReferenceEquals(_boundImage, image))
            {
                RectTransform rect = image.rectTransform;
                Vector2 max = rect.anchorMax;
                max.x = _fullAnchor;
                rect.anchorMax = max;
                if (image != null && ReferenceEquals(_image, image))
                {
                    image.enabled = true;
                }
            }

            _captured = false;
            _hasCurrent = false;
        }

        private void ApplyWidth(Image image, float value, Tween owned = null)
        {
            if (image == null || !ReferenceEquals(_image, image))
            {
                return;
            }

            _current = value;
            _hasCurrent = true;
            RectTransform rect = image.rectTransform;
            float anchor = Mathf.Lerp(_startAnchor, _fullAnchor, value);

            if (value > 0f && _minVisibleWidth > 0f && rect.parent is RectTransform parent)
            {
                // WHY: width = parentWidth * (anchorMax - anchorMin) + (offsetMax - offsetMin); solving for the
                // smallest anchorMax that still reaches the minimum keeps the caps apart.
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
            // A rect dirty callback can replace this tween with an immediate write on the same Image.
            if (image != null && ReferenceEquals(_image, image) &&
                (owned == null || ReferenceEquals(_anim, owned)))
            {
                image.enabled = value > 0f;
            }
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
            RetireTween(_anim);
        }

        private void RetireTween(Tween owned)
        {
            if (ReferenceEquals(_anim, owned))
            {
                _anim = null;
            }

            if (owned != null && owned.IsActive())
            {
                owned.Kill();
            }
        }
    }
}
