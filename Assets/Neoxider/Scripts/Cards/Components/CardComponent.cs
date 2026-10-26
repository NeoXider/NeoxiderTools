using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Card MonoBehaviour: visuals, tweens, clicks. Inspector setup; events via UnityEvent.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/CardComponent")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(CardComponent))]
    [NeoDoc("Cards/CardComponent.md")]
    public class CardComponent : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Config")] [SerializeField] private DeckConfig _config;

        [SerializeField] private Suit _suit;
        [SerializeField] private Rank _rank;
        [SerializeField] private bool _isJoker;
        [SerializeField] private bool _isRedJoker;

        [Header("State")] [SerializeField] private bool _isFaceUp = true;

        [SerializeField] private bool _isInteractable = true;

        [Header("Visual")] [SerializeField] private Image _cardImage;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _faceSpriteOverride;
        [SerializeField] private Sprite _backSpriteOverride;

        [Header("Animation")] [SerializeField] private float _flipDuration = 0.3f;

        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private Ease _flipEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        [Header("Hover Effect")] [SerializeField]
        private bool _enableHoverEffect = true;

        [Tooltip("Scale delta on hover (0.1 = 10% scale increase)")] [SerializeField]
        private float _hoverScale = 0.1f;

        [SerializeField] private float _hoverYOffset = 20f;
        [SerializeField] private float _hoverDuration = 0.15f;

        public UnityEvent OnClick;

        public UnityEvent OnFlip;
        public UnityEvent OnMoveComplete;
        public UnityEvent OnHoverEnter;
        public UnityEvent OnHoverExit;
        private Vector3 _currentTargetScale;
        private Tween _currentTween;

        private Tween _hoverMoveTween;
        private Tween _hoverScaleTween;
        private bool _isAnimating;
        private bool _isHovered;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private CancellationToken _ct;
        private CardTweenScope _animation;
        private Vector3? _flipScale;
        private DeckComponent _deckCleanupOwner;

        internal bool IsDeckCleanupOwnedBy(DeckComponent deck)
        {
            return ReferenceEquals(_deckCleanupOwner, deck);
        }

        internal void SetDeckCleanupOwner(DeckComponent deck)
        {
            ReleaseDeckCleanupOwnership();
            _deckCleanupOwner = deck;
        }

        internal void ReleaseDeckCleanupOwnership()
        {
            DeckComponent previous = _deckCleanupOwner;
            _deckCleanupOwner = null;
            if (previous != null)
            {
                previous.ReleaseCardCleanup(this);
            }
        }

        /// <summary>
        ///     Current card data.
        /// </summary>
        public CardData Data { get; private set; }

        /// <summary>
        ///     Face-up flag (drives sprite selection).
        /// </summary>
        public bool IsFaceUp
        {
            get => _isFaceUp;
            set
            {
                if (_isFaceUp != value)
                {
                    _isFaceUp = value;
                    UpdateVisual();
                }
            }
        }

        /// <summary>
        ///     Whether hover/click responses are enabled.
        /// </summary>
        public bool IsInteractable
        {
            get => _isInteractable;
            set => _isInteractable = value;
        }

        /// <summary>
        ///     Deck config for sprites.
        /// </summary>
        public DeckConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                UpdateVisual();
            }
        }

        /// <summary>Configured duration used by the parameterless flip operation.</summary>
        public float FlipDuration => _flipDuration;

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();
            _originalScale = transform.localScale;
            _currentTargetScale = _originalScale;
            _originalPosition = transform.position;
            // WHY: keep the card artwork at its native aspect in adaptive UI — without this the Image
            // stretches to whatever (possibly non-card) shape the responsive RectTransform ends up with.
            if (_cardImage != null)
            {
                _cardImage.preserveAspect = true;
            }

            InitializeData();
            EnsureRaycastTarget();
        }

        private void Start()
        {
            UpdateVisual();
        }

        private void OnDestroy()
        {
            _animation?.Cancel();
            _currentTween?.Kill();
            _hoverScaleTween?.Kill();
            _hoverMoveTween?.Kill();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            InitializeData();

            Sprite sprite = ResolveSprite();
            if (sprite == null)
            {
                return;
            }

            if (_cardImage != null && _cardImage.sprite != sprite)
            {
                _cardImage.sprite = sprite;
            }

            if (_spriteRenderer != null && _spriteRenderer.sprite != sprite)
            {
                _spriteRenderer.sprite = sprite;
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (_isInteractable)
            {
                OnClick?.Invoke();
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable || !_enableHoverEffect)
            {
                return;
            }

            bool hasScaleEffect = !Mathf.Approximately(_hoverScale, 0f);
            bool hasMoveEffect = !Mathf.Approximately(_hoverYOffset, 0f);

            if (!hasScaleEffect && !hasMoveEffect)
            {
                return;
            }

            if (_isAnimating)
            {
                CompleteCurrentAnimation();
            }

            if (_isHovered)
            {
                ResetHoverImmediate();
            }

            _isHovered = true;
            _originalPosition = transform.position;

            _hoverScaleTween?.Kill();
            _hoverMoveTween?.Kill();

            if (hasScaleEffect)
            {
                Vector3 currentScale = transform.localScale;
                _hoverScaleTween = transform.DOScale(currentScale * (1f + _hoverScale), _hoverDuration)
                    .SetRecyclable(false).SetLink(gameObject);
            }

            if (hasMoveEffect)
            {
                _hoverMoveTween = transform.DOMove(_originalPosition + Vector3.up * _hoverYOffset, _hoverDuration)
                    .SetRecyclable(false).SetLink(gameObject);
            }

            OnHoverEnter?.Invoke();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable || !_enableHoverEffect)
            {
                return;
            }

            ResetHover();
        }

        private void EnsureRaycastTarget()
        {
            if (_cardImage != null)
            {
                _cardImage.raycastTarget = true;
            }
        }

        /// <summary>
        ///     Assigns data and refreshes sprites.
        /// </summary>
        /// <param name="data">Card data.</param>
        /// <param name="faceUp">Face up.</param>
        public void SetData(CardData data, bool faceUp = true)
        {
            Data = data;
            _suit = data.Suit;
            _rank = data.Rank;
            _isJoker = data.IsJoker;
            _isRedJoker = data.IsRedJoker;
            _isFaceUp = faceUp;
            UpdateVisual();
        }

        /// <summary>
        ///     Overrides face/back sprites for standalone custom-card views.
        /// </summary>
        /// <param name="faceSprite">Sprite used while face up.</param>
        /// <param name="backSprite">Optional sprite used while face down.</param>
        /// <param name="refresh">Refresh visual immediately.</param>
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
        /// <param name="refresh">Refresh visual immediately.</param>
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
        ///     Toggles face instantly.
        /// </summary>
        [Button]
        public void Flip()
        {
            _isFaceUp = !_isFaceUp;
            UpdateVisual();
            OnFlip?.Invoke();
        }

        /// <summary>
        ///     Flips using <c>_flipDuration</c>.
        /// </summary>
        public async UniTask FlipAsync()
        {
            await FlipAsync(_flipDuration);
        }

        /// <summary>
        ///     Flips with explicit duration.
        /// </summary>
        /// <param name="duration">Seconds.</param>
        public async UniTask FlipAsync(float duration)
        {
            await FlipAsync(duration, default);
        }

        /// <summary>Flips with caller/destruction cancellation and exact tween ownership.</summary>
        public async UniTask FlipAsync(float duration, CancellationToken cancellation)
        {
            using CardTweenScope operation = BeginAnimation(cancellation);
            Vector3 scaleBeforeFlip = transform.localScale;
            _flipScale = scaleBeforeFlip;
            try
            {
                if (duration > 0f)
                {
                    _currentTween = transform.DOScaleX(0f, duration / 2f).SetEase(_flipEase).SetLink(gameObject);
                    await operation.WaitAsync(_currentTween);
                }

                operation.Token.ThrowIfCancellationRequested();
                _isFaceUp = !_isFaceUp;
                UpdateVisual();

                if (duration > 0f)
                {
                    _currentTween = transform.DOScaleX(scaleBeforeFlip.x, duration / 2f)
                        .SetEase(_flipEase).SetLink(gameObject);
                    await operation.WaitAsync(_currentTween);
                }
            }
            finally
            {
                if (ReferenceEquals(_animation, operation) && this != null)
                {
                    transform.localScale = scaleBeforeFlip;
                    _currentTargetScale = scaleBeforeFlip;
                    _flipScale = null;
                    FinishAnimation(operation);
                }
            }

            operation.Token.ThrowIfCancellationRequested();
            OnFlip?.Invoke();
        }

        /// <summary>
        ///     Moves in world space using default duration.
        /// </summary>
        /// <param name="position">World position.</param>
        public async UniTask MoveToAsync(Vector3 position)
        {
            await MoveToAsync(position, _moveDuration);
        }

        /// <summary>
        ///     DOMove with custom duration.
        /// </summary>
        /// <param name="position">World position.</param>
        /// <param name="duration">Seconds.</param>
        public async UniTask MoveToAsync(Vector3 position, float duration)
        {
            await MoveToAsync(position, duration, default);
        }

        /// <summary>Moves in world space; cancellation kills only this operation's tween.</summary>
        public UniTask MoveToAsync(Vector3 position, float duration, CancellationToken cancellation)
        {
            return MoveAsync(position, duration, false, cancellation);
        }

        /// <summary>
        ///     DOLocalMove helper.
        /// </summary>
        /// <param name="localPosition">Local position.</param>
        /// <param name="duration">Seconds.</param>
        public async UniTask MoveToLocalAsync(Vector3 localPosition, float duration)
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

            operation.Token.ThrowIfCancellationRequested();
            OnMoveComplete?.Invoke();
        }

        private CardTweenScope BeginAnimation(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            _ct.ThrowIfCancellationRequested();
            CancelAnimation();

            _isAnimating = true;
            _animation = new CardTweenScope(_ct, cancellation);
            return _animation;
        }

        internal void CancelAnimation()
        {
            _animation?.Cancel();
            _animation = null;
            _currentTween?.Kill();
            _currentTween = null;
            _isAnimating = false;
            if (_isHovered && this != null)
            {
                ResetHoverImmediate();
                _isHovered = false;
            }
            else
            {
                _hoverMoveTween?.Kill();
                _hoverScaleTween?.Kill();
            }

            if (_flipScale.HasValue && this != null)
            {
                transform.localScale = _flipScale.Value;
                _flipScale = null;
            }
        }

        private void FinishAnimation(CardTweenScope operation)
        {
            if (ReferenceEquals(_animation, operation))
            {
                _animation = null;
                _currentTween = null;
                _isAnimating = false;
            }
        }

        /// <summary>
        ///     Animates hover state back to rest.
        /// </summary>
        public void ResetHover()
        {
            if (!_isHovered)
            {
                return;
            }

            _isHovered = false;

            bool hasScaleEffect = !Mathf.Approximately(_hoverScale, 0f);
            bool hasMoveEffect = !Mathf.Approximately(_hoverYOffset, 0f);

            _hoverScaleTween?.Kill();
            _hoverMoveTween?.Kill();

            if (hasScaleEffect)
            {
                _hoverScaleTween = transform.DOScale(_currentTargetScale, _hoverDuration)
                    .SetRecyclable(false).SetLink(gameObject);
            }

            if (hasMoveEffect)
            {
                _hoverMoveTween = transform.DOMove(_originalPosition, _hoverDuration)
                    .SetRecyclable(false).SetLink(gameObject);
            }

            OnHoverExit?.Invoke();
        }

        /// <summary>
        ///     Instantly clears hover offsets.
        /// </summary>
        private void ResetHoverImmediate()
        {
            _hoverScaleTween?.Kill();
            _hoverMoveTween?.Kill();

            bool hasScaleEffect = !Mathf.Approximately(_hoverScale, 0f);
            bool hasMoveEffect = !Mathf.Approximately(_hoverYOffset, 0f);

            if (hasScaleEffect)
            {
                transform.localScale = _currentTargetScale;
            }

            if (hasMoveEffect)
            {
                transform.position = _originalPosition;
            }
        }

        /// <summary>
        ///     Completes the active move/flip tween immediately.
        /// </summary>
        private void CompleteCurrentAnimation()
        {
            if (!_isAnimating)
            {
                return;
            }

            _currentTween?.Complete();
            _isAnimating = false;
        }

        /// <summary>
        ///     Refreshes baseline transform after <see cref="HandComponent" /> finishes layout.
        /// </summary>
        public void UpdateOriginalTransform()
        {
            _originalPosition = transform.position;
            _currentTargetScale = transform.localScale;
        }

        private void InitializeData()
        {
            if (_isJoker)
            {
                Data = CardData.CreateJoker(_isRedJoker);
            }
            else
            {
                Data = new CardData(_suit, _rank);
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
            if (_isFaceUp)
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
    }
}
