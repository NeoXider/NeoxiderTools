using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Default card view implementation.
    /// </summary>
    [NeoDoc("Cards/View/CardView.md")]
    public class CardView : MonoBehaviour, ICardView, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual")] [SerializeField] private Image _cardImage;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _faceSpriteOverride;
        [SerializeField] private Sprite _backSpriteOverride;

        [Header("Animation")] [SerializeField] private float _flipDuration = 0.3f;

        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private Ease _flipEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        [Header("Hover")] [Tooltip("Scale delta on hover (0.1 = 10% scale increase)")] [SerializeField]
        private float _hoverScale = 0.1f;

        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _hoverYOffset = 20f;
        private DeckConfig _config;
        private Tween _currentTween;
        private Tween _hoverMoveTween;
        private Tween _hoverScaleTween;

        private bool _isInteractable = true;
        private bool _isHovering;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private RectTransform _rect;
        private CancellationToken _ct;

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();
            // WHY: RectTransform => the card lives in a Canvas; hover must animate anchoredPosition,
            // not world position, or a scaled/camera canvas warps its size and place.
            _rect = transform as RectTransform;
            _originalScale = transform.localScale;
            // WHY: keep the card artwork at its native aspect in adaptive UI (no stretch distortion).
            if (_cardImage != null)
            {
                _cardImage.preserveAspect = true;
            }
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
            KillHoverTweens();
        }

        /// <inheritdoc />
        public CardData Data { get; private set; }

        /// <inheritdoc />
        public bool IsFaceUp { get; private set; } = true;

        /// <inheritdoc />
        public Transform Transform => transform;

        /// <inheritdoc />
        public event Action<ICardView> OnClicked;

        /// <inheritdoc />
        public event Action<ICardView> OnHovered;

        /// <inheritdoc />
        public event Action<ICardView> OnUnhovered;

        /// <inheritdoc />
        public void SetData(CardData data, bool faceUp = true)
        {
            Data = data;
            IsFaceUp = faceUp;
            UpdateVisual();
        }

        /// <inheritdoc />
        public void Flip()
        {
            IsFaceUp = !IsFaceUp;
            UpdateVisual();
        }

        /// <inheritdoc />
        public async UniTask FlipAsync(float duration = 0.3f)
        {
            if (duration <= 0)
            {
                Flip();
                return;
            }

            _currentTween?.Kill();

            float halfDuration = duration / 2f;

            TweenerCore<Vector3, Vector3, VectorOptions>
                tween1 = transform.DOScaleX(0, halfDuration).SetEase(_flipEase).SetLink(gameObject);
            _currentTween = tween1;
            await UniTask.WaitUntil(() => !tween1.IsActive(), cancellationToken: _ct);

            IsFaceUp = !IsFaceUp;
            UpdateVisual();

            TweenerCore<Vector3, Vector3, VectorOptions> tween2 =
                transform.DOScaleX(_originalScale.x, halfDuration).SetEase(_flipEase).SetLink(gameObject);
            _currentTween = tween2;
            await UniTask.WaitUntil(() => !tween2.IsActive(), cancellationToken: _ct);
        }

        /// <inheritdoc />
        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            if (duration <= 0)
            {
                transform.position = position;
                return;
            }

            _currentTween?.Kill();
            _currentTween = transform.DOMove(position, duration).SetEase(_moveEase).SetLink(gameObject);
            await UniTask.WaitUntil(() => !_currentTween.IsActive(), cancellationToken: _ct);
            _originalPosition = position;
        }

        /// <inheritdoc />
        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (_isInteractable)
            {
                OnClicked?.Invoke(this);
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            OnHovered?.Invoke(this);

            // WHY: capture the RESTING scale/position at hover start (not the stale Awake scale), so a
            // card the game resized/moved after Awake returns to its real size on hover exit.
            if (!_isHovering)
            {
                _originalScale = transform.localScale;
                _originalPosition = _rect != null ? _rect.anchoredPosition3D : transform.position;
            }

            _isHovering = true;
            KillHoverTweens();
            _hoverScaleTween = transform.DOScale(_originalScale * (1f + _hoverScale), _hoverDuration)
                .SetTarget(transform)
                .SetLink(gameObject);
            _hoverMoveTween = HoverMoveTween(_originalPosition + Vector3.up * _hoverYOffset);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            OnUnhovered?.Invoke(this);

            _isHovering = false;
            KillHoverTweens();
            _hoverScaleTween = transform.DOScale(_originalScale, _hoverDuration)
                .SetTarget(transform)
                .SetLink(gameObject);
            _hoverMoveTween = HoverMoveTween(_originalPosition);
        }

        // WHY: UI cards must move by anchoredPosition; only SpriteRenderer cards use world DOMove.
        private Tween HoverMoveTween(Vector3 target)
        {
            if (_rect != null)
            {
                return DOTween.To(() => _rect.anchoredPosition3D, v => _rect.anchoredPosition3D = v,
                        target, _hoverDuration)
                    .SetTarget(transform)
                    .SetLink(gameObject);
            }

            return transform.DOMove(target, _hoverDuration).SetTarget(transform).SetLink(gameObject);
        }

        /// <summary>
        ///     Initializes sprites from deck config.
        /// </summary>
        /// <param name="config">Deck config.</param>
        public void Initialize(DeckConfig config)
        {
            _config = config;
            UpdateVisual();
        }

        /// <summary>
        ///     Overrides face/back sprites so the view can be used without a DeckConfig.
        /// </summary>
        public void SetSpriteOverrides(Sprite faceSprite, Sprite backSprite = null, bool refresh = true)
        {
            _faceSpriteOverride = faceSprite;
            _backSpriteOverride = backSprite;

            if (refresh)
            {
                UpdateVisual();
            }
        }

        /// <summary>
        ///     Clears standalone sprite overrides and returns to DeckConfig sprites.
        /// </summary>
        public void ClearSpriteOverrides(bool refresh = true)
        {
            _faceSpriteOverride = null;
            _backSpriteOverride = null;

            if (refresh)
            {
                UpdateVisual();
            }
        }

        /// <summary>
        ///     Moves to a local position with tween.
        /// </summary>
        /// <param name="localPosition">Local position.</param>
        /// <param name="duration">Duration.</param>
        public async UniTask MoveToLocalAsync(Vector3 localPosition, float duration = 0.2f)
        {
            if (duration <= 0)
            {
                transform.localPosition = localPosition;
                return;
            }

            _currentTween?.Kill();
            _currentTween = transform.DOLocalMove(localPosition, duration).SetEase(_moveEase).SetLink(gameObject);
            await UniTask.WaitUntil(() => !_currentTween.IsActive(), cancellationToken: _ct);
            _originalPosition = transform.position;
        }

        /// <summary>
        ///     Rotates to the target local rotation.
        /// </summary>
        /// <param name="rotation">Target rotation.</param>
        /// <param name="duration">Duration.</param>
        public async UniTask RotateToAsync(Quaternion rotation, float duration = 0.2f)
        {
            if (duration <= 0)
            {
                transform.rotation = rotation;
                return;
            }

            TweenerCore<Quaternion, Quaternion, NoOptions> rotateTween =
                transform.DORotateQuaternion(rotation, duration).SetEase(_moveEase).SetLink(gameObject);
            _currentTween = rotateTween;
            await UniTask.WaitUntil(() => !rotateTween.IsActive(), cancellationToken: _ct);
        }

        private void UpdateVisual()
        {
            Sprite sprite = ResolveSprite();
            if (sprite == null)
            {
                return;
            }

            if (_cardImage != null)
            {
                _cardImage.sprite = sprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }

        private Sprite ResolveSprite()
        {
            if (IsFaceUp)
            {
                if (_faceSpriteOverride != null)
                {
                    return _faceSpriteOverride;
                }

                return _config != null ? _config.GetSprite(Data) : null;
            }

            if (_backSpriteOverride != null)
            {
                return _backSpriteOverride;
            }

            return _config != null ? _config.BackSprite : null;
        }

        private void KillHoverTweens()
        {
            _hoverScaleTween?.Kill();
            _hoverMoveTween?.Kill();
            _hoverScaleTween = null;
            _hoverMoveTween = null;
        }
    }
}
