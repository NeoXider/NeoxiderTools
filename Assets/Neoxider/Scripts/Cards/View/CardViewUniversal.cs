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
        private bool _isInteractable = true;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
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
            _originalPosition = transform.position;
            transform.DOScale(_originalScale * (1f + _hoverScale), _hoverDuration);
            transform.DOMove(_originalPosition + Vector3.up * _hoverYOffset, _hoverDuration);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            OnUnhovered?.Invoke(this);
            transform.DOScale(_originalScale, _hoverDuration);
            transform.DOMove(_originalPosition, _hoverDuration);
        }

        public void Initialize(DeckConfig config)
        {
            _config = config;
        }

        private void UpdateVisual()
        {
            if (_config == null)
            {
                return;
            }

            Sprite sprite = IsFaceUp ? _config.GetSprite(Data) : _config.BackSprite;
            if (_cardImage != null)
            {
                _cardImage.sprite = sprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
    }
}
