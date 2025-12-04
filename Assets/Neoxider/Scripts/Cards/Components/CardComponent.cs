using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    /// Компонент карты для работы без кода
    /// </summary>
    public class CardComponent : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Config")]
        [SerializeField] private DeckConfig _config;
        [SerializeField] private Suit _suit;
        [SerializeField] private Rank _rank;
        [SerializeField] private bool _isJoker;
        [SerializeField] private bool _isRedJoker;

        [Header("State")]
        [SerializeField] private bool _isFaceUp = true;
        [SerializeField] private bool _isInteractable = true;

        [Header("Visual")]
        [SerializeField] private Image _cardImage;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Animation")]
        [SerializeField] private float _flipDuration = 0.3f;
        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private Ease _flipEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        [Header("Hover Effect")]
        [SerializeField] private bool _enableHoverEffect = true;
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _hoverYOffset = 20f;
        [SerializeField] private float _hoverDuration = 0.15f;

        [Header("Events")]
        public UnityEvent OnClick;
        public UnityEvent OnFlip;
        public UnityEvent OnMoveComplete;
        public UnityEvent OnHoverEnter;
        public UnityEvent OnHoverExit;

        private CardData _data;
        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private Tween _currentTween;
        private bool _isHovered;

        /// <summary>
        /// Данные карты
        /// </summary>
        public CardData Data => _data;

        /// <summary>
        /// Показана ли карта лицом вверх
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
        /// Интерактивна ли карта
        /// </summary>
        public bool IsInteractable
        {
            get => _isInteractable;
            set => _isInteractable = value;
        }

        /// <summary>
        /// Конфигурация колоды
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

        private void Awake()
        {
            _originalScale = transform.localScale;
            InitializeData();
        }

        private void Start()
        {
            UpdateVisual();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                InitializeData();
                UpdateVisual();
            }
        }

        /// <summary>
        /// Устанавливает данные карты
        /// </summary>
        /// <param name="data">Данные карты</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        public void SetData(CardData data, bool faceUp = true)
        {
            _data = data;
            _suit = data.Suit;
            _rank = data.Rank;
            _isJoker = data.IsJoker;
            _isRedJoker = data.IsRedJoker;
            _isFaceUp = faceUp;
            UpdateVisual();
        }

        /// <summary>
        /// Переворачивает карту
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void Flip()
        {
            _isFaceUp = !_isFaceUp;
            UpdateVisual();
            OnFlip?.Invoke();
        }

        /// <summary>
        /// Переворачивает карту с анимацией
        /// </summary>
        public async UniTask FlipAsync()
        {
            await FlipAsync(_flipDuration);
        }

        /// <summary>
        /// Переворачивает карту с анимацией указанной длительности
        /// </summary>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask FlipAsync(float duration)
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

            OnFlip?.Invoke();
        }

        /// <summary>
        /// Перемещает карту в позицию
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        public async UniTask MoveToAsync(Vector3 position)
        {
            await MoveToAsync(position, _moveDuration);
        }

        /// <summary>
        /// Перемещает карту в позицию с указанной длительностью
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToAsync(Vector3 position, float duration)
        {
            if (duration <= 0)
            {
                transform.position = position;
                OnMoveComplete?.Invoke();
                return;
            }

            _currentTween?.Kill();
            _currentTween = transform.DOMove(position, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());
            _originalPosition = position;
            OnMoveComplete?.Invoke();
        }

        /// <summary>
        /// Перемещает карту в локальную позицию
        /// </summary>
        /// <param name="localPosition">Локальная позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToLocalAsync(Vector3 localPosition, float duration)
        {
            if (duration <= 0)
            {
                transform.localPosition = localPosition;
                OnMoveComplete?.Invoke();
                return;
            }

            _currentTween?.Kill();
            _currentTween = transform.DOLocalMove(localPosition, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());
            _originalPosition = transform.position;
            OnMoveComplete?.Invoke();
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
            if (!_isInteractable || !_enableHoverEffect) return;

            _isHovered = true;
            _originalPosition = transform.position;
            transform.DOScale(_originalScale * _hoverScale, _hoverDuration);
            transform.DOMove(_originalPosition + Vector3.up * _hoverYOffset, _hoverDuration);
            OnHoverEnter?.Invoke();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable || !_enableHoverEffect || !_isHovered) return;

            _isHovered = false;
            transform.DOScale(_originalScale, _hoverDuration);
            transform.DOMove(_originalPosition, _hoverDuration);
            OnHoverExit?.Invoke();
        }

        private void InitializeData()
        {
            if (_isJoker)
            {
                _data = CardData.CreateJoker(_isRedJoker);
            }
            else
            {
                _data = new CardData(_suit, _rank);
            }
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

