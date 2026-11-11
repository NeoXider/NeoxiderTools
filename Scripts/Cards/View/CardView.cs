using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        private CardTweenScope _animation;
        private Vector3? _flipScale;

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
            _animation?.Cancel();
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
            await FlipAsync(duration, default);
        }

        /// <summary>Flips with caller/destruction cancellation; replacement cancels the previous operation.</summary>
        public async UniTask FlipAsync(float duration, CancellationToken cancellation)
        {
            using CardTweenScope operation = BeginAnimation(cancellation);
            Vector3 scale = transform.localScale;
            _flipScale = scale;
            try
            {
                if (duration > 0f)
                {
                    _currentTween = transform.DOScaleX(0f, duration / 2f).SetEase(_flipEase).SetLink(gameObject);
                    await operation.WaitAsync(_currentTween);
                }

                operation.Token.ThrowIfCancellationRequested();
                Flip();
                if (duration > 0f)
                {
                    _currentTween = transform.DOScaleX(scale.x, duration / 2f).SetEase(_flipEase).SetLink(gameObject);
                    await operation.WaitAsync(_currentTween);
                }
            }
            finally
            {
                if (ReferenceEquals(_animation, operation) && this != null)
                {
                    transform.localScale = scale;
                    _flipScale = null;
                    FinishAnimation(operation);
                }
            }
        }

        /// <inheritdoc />
        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            await MoveToAsync(position, duration, default);
        }

        /// <summary>Moves with caller/destruction cancellation.</summary>
        public UniTask MoveToAsync(Vector3 position, float duration, CancellationToken cancellation)
        {
            return MoveAsync(position, duration, false, cancellation);
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
                .SetRecyclable(false)
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
                .SetRecyclable(false)
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
                    .SetRecyclable(false)
                    .SetTarget(transform)
                    .SetLink(gameObject);
            }

            return transform.DOMove(target, _hoverDuration).SetRecyclable(false).SetTarget(transform).SetLink(gameObject);
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
            await MoveToLocalAsync(localPosition, duration, default);
        }

        /// <summary>Moves in local space with caller/destruction cancellation.</summary>
        public UniTask MoveToLocalAsync(Vector3 localPosition, float duration, CancellationToken cancellation)
        {
            return MoveAsync(localPosition, duration, true, cancellation);
        }

        private async UniTask MoveAsync(Vector3 position, float duration, bool local, CancellationToken cancellation)
        {
            using CardTweenScope operation = BeginAnimation(cancellation);
            try
            {
                if (duration > 0f)
                {
                    _currentTween = (local ? transform.DOLocalMove(position, duration) : transform.DOMove(position, duration))
                        .SetEase(_moveEase).SetLink(gameObject);
                    await operation.WaitAsync(_currentTween);
                }
                else if (local)
                {
                    transform.localPosition = position;
                }
                else
                {
                    transform.position = position;
                }

                operation.Token.ThrowIfCancellationRequested();
                _originalPosition = transform.position;
            }
            finally
            {
                FinishAnimation(operation);
            }
        }

        /// <summary>
        ///     Rotates to the target world rotation.
        /// </summary>
        /// <param name="rotation">Target rotation.</param>
        /// <param name="duration">Duration.</param>
        public async UniTask RotateToAsync(Quaternion rotation, float duration = 0.2f)
        {
            await RotateToAsync(rotation, duration, default);
        }

        /// <summary>Rotates in world space with caller/destruction cancellation.</summary>
        public async UniTask RotateToAsync(Quaternion rotation, float duration, CancellationToken cancellation)
        {
            using CardTweenScope operation = BeginAnimation(cancellation);
            try
            {
                if (duration <= 0f)
                {
                    transform.rotation = rotation;
                    return;
                }

                _currentTween = transform.DORotateQuaternion(rotation, duration).SetEase(_moveEase).SetLink(gameObject);
                await operation.WaitAsync(_currentTween);
            }
            finally
            {
                FinishAnimation(operation);
            }
        }

        private CardTweenScope BeginAnimation(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            _ct.ThrowIfCancellationRequested();
            _animation?.Cancel();
            _currentTween?.Kill();
            if (_flipScale.HasValue)
            {
                transform.localScale = _flipScale.Value;
                _flipScale = null;
            }

            KillHoverTweens();
            _isHovering = false;
            _animation = new CardTweenScope(_ct, cancellation);
            return _animation;
        }

        private void FinishAnimation(CardTweenScope operation)
        {
            if (ReferenceEquals(_animation, operation))
            {
                _animation = null;
                _currentTween = null;
            }
        }

        private void UpdateVisual()
        {
            Sprite sprite = ResolveSprite();
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
