using System;
using System.Collections.Generic;
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

        private DeckConfig _config;
        private Tween _currentTween;
        private Tween _hoverMoveTween;
        private Tween _hoverScaleTween;
        private bool _isInteractable = true;
        private bool _isHovering;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private RectTransform _rect;

        private void Awake()
        {
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
            foreach (Tween t in _loopedTweens.Values)
            {
                t?.Kill();
            }

            _loopedTweens.Clear();
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
            if (_displayMode != CardDisplayMode.WithFlip)
            {
                return;
            }

            if (duration <= 0)
            {
                Flip();
                return;
            }

            _currentTween?.Kill();
            float halfDuration = duration / 2f;
            TweenerCore<Vector3, Vector3, VectorOptions>
                tween1 = transform.DOScaleX(0, halfDuration).SetEase(_flipEase);
            await UniTask.WaitUntil(() => !tween1.IsActive());
            IsFaceUp = !IsFaceUp;
            UpdateVisual();
            TweenerCore<Vector3, Vector3, VectorOptions> tween2 =
                transform.DOScaleX(_originalScale.x, halfDuration).SetEase(_flipEase);
            await UniTask.WaitUntil(() => !tween2.IsActive());
        }

        public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
        {
            if (duration <= 0)
            {
                transform.position = position;
                _originalPosition = position;
                return;
            }

            _currentTween?.Kill();
            _currentTween = transform.DOMove(position, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());
            _originalPosition = position;
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        public async UniTask PlayOneShotAsync(CardViewAnimationType type, float? duration = null,
            CancellationToken cancellation = default)
        {
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

            try
            {
                await UniTask.WaitUntil(() => !t.IsActive(), cancellationToken: cancellation);
            }
            catch (OperationCanceledException)
            {
                t.Kill();
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
