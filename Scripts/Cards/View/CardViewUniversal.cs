using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Universal card view: <see cref="ICardView" />, <see cref="ICardDisplayMode" />,
    ///     <see cref="ICardViewAnimations" />. Display modes (flip / always up / always down) and built-in tweens via
    ///     <see cref="CardViewAnimationTemplates" />.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/CardViewUniversal")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(CardViewUniversal))]
    [NeoDoc("Cards/View/CardViewUniversal.md")]
    public class CardViewUniversal : MonoBehaviour, ICardView, ICardDisplayMode, ICardViewAnimations,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual")] [SerializeField] private Image _cardImage;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _faceSpriteOverride;
        [SerializeField] private Sprite _backSpriteOverride;

        [Header("Display")] [SerializeField] private CardDisplayMode _displayMode = CardDisplayMode.WithFlip;

        [Header("Animation")] [SerializeField] private float _flipDuration = 0.3f;

        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private Ease _flipEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;
        [SerializeField] private CardAnimationConfig _animationConfig;

        [Header("Hover")] [SerializeField] private float _hoverScale = 0.1f;

        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _hoverYOffset = 20f;
        private readonly Dictionary<CardViewAnimationType, Tween> _loopedTweens = new();
        private readonly HashSet<Tween> _oneShotTweens = new();

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
            foreach (Tween t in _loopedTweens.Values)
            {
                t?.Kill();
            }

            _loopedTweens.Clear();
            foreach (Tween tween in _oneShotTweens)
            {
                tween.Kill();
            }

            _oneShotTweens.Clear();
        }

        public CardDisplayMode Mode => _displayMode;

        public CardData Data { get; private set; }
        public bool IsFaceUp { get; private set; } = true;
        public Transform Transform => transform;

        public event Action<ICardView> OnClicked;
        public event Action<ICardView> OnHovered;
        public event Action<ICardView> OnUnhovered;

        public void SetData(CardData data, bool faceUp = true)
        {
            Data = data;
            if (_displayMode == CardDisplayMode.AlwaysFaceUp)
            {
                IsFaceUp = true;
            }
            else if (_displayMode == CardDisplayMode.AlwaysFaceDown)
            {
                IsFaceUp = false;
            }
            else
            {
                IsFaceUp = faceUp;
            }

            UpdateVisual();
        }

        public void Flip()
        {
            if (_displayMode != CardDisplayMode.WithFlip)
            {
                return;
            }

            IsFaceUp = !IsFaceUp;
            UpdateVisual();
        }

        public async UniTask FlipAsync(float duration = 0.3f)
        {
            await FlipAsync(duration, default);
        }

        /// <summary>Flips with caller/destruction cancellation; fixed display modes remain unchanged.</summary>
        public async UniTask FlipAsync(float duration, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (_displayMode != CardDisplayMode.WithFlip)
            {
                return;
            }

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

        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            await MoveToAsync(position, duration, default);
        }

        /// <summary>Moves with caller/destruction cancellation and exact tween ownership.</summary>
        public async UniTask MoveToAsync(Vector3 position, float duration, CancellationToken cancellation)
        {
            using CardTweenScope operation = BeginAnimation(cancellation);
            try
            {
                if (duration > 0f)
                {
                    _currentTween = transform.DOMove(position, duration).SetEase(_moveEase).SetLink(gameObject);
                    await operation.WaitAsync(_currentTween);
                }
                else
                {
                    transform.position = position;
                }

                operation.Token.ThrowIfCancellationRequested();
                _originalPosition = position;
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

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        public async UniTask PlayOneShotAsync(CardViewAnimationType type, float? duration = null,
            CancellationToken cancellation = default)
        {
            if (cancellation.IsCancellationRequested || _ct.IsCancellationRequested)
            {
                return;
            }

            Tween t = type switch
            {
                CardViewAnimationType.Bounce => CardViewAnimationTemplates.Bounce(transform, duration ?? 0.25f, 0.15f,
                    _animationConfig),
                CardViewAnimationType.Pulse => CardViewAnimationTemplates.Pulse(transform, duration ?? 0.4f, 0.08f,
                    _animationConfig),
                CardViewAnimationType.Shake => CardViewAnimationTemplates.Shake(transform, duration ?? 0.3f, 8f,
                    _animationConfig),
                CardViewAnimationType.Highlight => CardViewAnimationTemplates.Highlight(transform, duration ?? 0.2f,
                    _animationConfig),
                _ => CardViewAnimationTemplates.PlayOneShot(transform, type, duration)
            };
            if (t == null)
            {
                return;
            }

            _oneShotTweens.Add(t);
            using CardTweenScope operation = new(_ct, cancellation);
            try
            {
                await operation.WaitAsync(t);
            }
            catch (OperationCanceledException)
            {
                t.Kill();
            }
            finally
            {
                _oneShotTweens.Remove(t);
            }
        }

        public void PlayLooped(CardViewAnimationType type, float? duration = null)
        {
            StopLooped(type);
            Tween t = CardViewAnimationTemplates.PlayLooped(transform, type, duration, _animationConfig);
            if (t != null)
            {
                _loopedTweens[type] = t;
            }
        }

        public void StopLooped(CardViewAnimationType type)
        {
            if (_loopedTweens.TryGetValue(type, out Tween t))
            {
                t?.Kill();
                _loopedTweens.Remove(type);
            }
        }

        public void StopAllLooped()
        {
            foreach (Tween t in _loopedTweens.Values)
            {
                t?.Kill();
            }

            _loopedTweens.Clear();
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
