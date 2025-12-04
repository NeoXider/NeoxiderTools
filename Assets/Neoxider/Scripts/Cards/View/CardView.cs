using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    /// Визуальное представление карты
    /// </summary>
    public class CardView : MonoBehaviour, ICardView, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual")]
        [SerializeField] private Image _cardImage;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Animation")]
        [SerializeField] private float _flipDuration = 0.3f;
        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private Ease _flipEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        [Header("Hover")]
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _hoverYOffset = 20f;

        private CardData _data;
        private bool _isFaceUp = true;
        private bool _isInteractable = true;
        private DeckConfig _config;
        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private Tween _currentTween;

        /// <inheritdoc />
        public CardData Data => _data;

        /// <inheritdoc />
        public bool IsFaceUp => _isFaceUp;

        /// <inheritdoc />
        public Transform Transform => transform;

        /// <inheritdoc />
        public event Action<ICardView> OnClicked;

        /// <inheritdoc />
        public event Action<ICardView> OnHovered;

        /// <inheritdoc />
        public event Action<ICardView> OnUnhovered;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        /// <summary>
        /// Инициализирует карту с конфигурацией
        /// </summary>
        /// <param name="config">Конфигурация колоды со спрайтами</param>
        public void Initialize(DeckConfig config)
        {
            _config = config;
        }

        /// <inheritdoc />
        public void SetData(CardData data, bool faceUp = true)
        {
            _data = data;
            _isFaceUp = faceUp;
            UpdateVisual();
        }

        /// <inheritdoc />
        public void Flip()
        {
            _isFaceUp = !_isFaceUp;
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

            var tween1 = transform.DOScaleX(0, halfDuration).SetEase(_flipEase);
            await UniTask.WaitUntil(() => !tween1.IsActive());

            _isFaceUp = !_isFaceUp;
            UpdateVisual();

            var tween2 = transform.DOScaleX(_originalScale.x, halfDuration).SetEase(_flipEase);
            await UniTask.WaitUntil(() => !tween2.IsActive());
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
            _currentTween = transform.DOMove(position, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());
            _originalPosition = position;
        }

        /// <summary>
        /// Перемещает карту в локальную позицию
        /// </summary>
        /// <param name="localPosition">Локальная позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToLocalAsync(Vector3 localPosition, float duration = 0.2f)
        {
            if (duration <= 0)
            {
                transform.localPosition = localPosition;
                return;
            }

            _currentTween?.Kill();
            _currentTween = transform.DOLocalMove(localPosition, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());
            _originalPosition = transform.position;
        }

        /// <summary>
        /// Поворачивает карту
        /// </summary>
        /// <param name="rotation">Целевой поворот</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask RotateToAsync(Quaternion rotation, float duration = 0.2f)
        {
            if (duration <= 0)
            {
                transform.rotation = rotation;
                return;
            }

            var rotateTween = transform.DORotateQuaternion(rotation, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !rotateTween.IsActive());
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
            if (!_isInteractable) return;

            OnHovered?.Invoke(this);

            _originalPosition = transform.position;
            transform.DOScale(_originalScale * _hoverScale, _hoverDuration);
            transform.DOMove(_originalPosition + Vector3.up * _hoverYOffset, _hoverDuration);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            OnUnhovered?.Invoke(this);

            transform.DOScale(_originalScale, _hoverDuration);
            transform.DOMove(_originalPosition, _hoverDuration);
        }

        private void UpdateVisual()
        {
            if (_config == null) return;

            Sprite sprite = _isFaceUp ? _config.GetSprite(_data) : _config.BackSprite;

            if (_cardImage != null)
            {
                _cardImage.sprite = sprite;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
        }
    }
}

