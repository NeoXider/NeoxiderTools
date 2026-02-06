using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Компонент карты для работы без кода
    /// </summary>
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

        [Header("Animation")] [SerializeField] private float _flipDuration = 0.3f;

        [SerializeField] private float _moveDuration = 0.2f;
        [SerializeField] private Ease _flipEase = Ease.OutQuad;
        [SerializeField] private Ease _moveEase = Ease.OutQuad;

        [Header("Hover Effect")] [SerializeField]
        private bool _enableHoverEffect = true;

        [Tooltip("Дельта увеличения масштаба (0.1 = увеличение на 10%)")] [SerializeField]
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

        /// <summary>
        ///     Данные карты
        /// </summary>
        public CardData Data { get; private set; }

        /// <summary>
        ///     Показана ли карта лицом вверх
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
        ///     Интерактивна ли карта
        /// </summary>
        public bool IsInteractable
        {
            get => _isInteractable;
            set => _isInteractable = value;
        }

        /// <summary>
        ///     Конфигурация колоды
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
            _currentTargetScale = _originalScale;
            _originalPosition = transform.position;
            InitializeData();
            EnsureRaycastTarget();
        }

        private void Start()
        {
            UpdateVisual();
        }

        private void OnDestroy()
        {
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

            if (_config == null)
            {
                return;
            }

            Sprite sprite = _isFaceUp ? _config.GetSprite(Data) : _config.BackSprite;
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
                _hoverScaleTween = transform.DOScale(currentScale * (1f + _hoverScale), _hoverDuration);
            }

            if (hasMoveEffect)
            {
                _hoverMoveTween = transform.DOMove(_originalPosition + Vector3.up * _hoverYOffset, _hoverDuration);
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
        ///     Устанавливает данные карты
        /// </summary>
        /// <param name="data">Данные карты</param>
        /// <param name="faceUp">Показать лицом вверх</param>
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
        ///     Переворачивает карту
        /// </summary>
        [Button]
        public void Flip()
        {
            _isFaceUp = !_isFaceUp;
            UpdateVisual();
            OnFlip?.Invoke();
        }

        /// <summary>
        ///     Переворачивает карту с анимацией
        /// </summary>
        public async UniTask FlipAsync()
        {
            await FlipAsync(_flipDuration);
        }

        /// <summary>
        ///     Переворачивает карту с анимацией указанной длительности
        /// </summary>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask FlipAsync(float duration)
        {
            if (duration <= 0)
            {
                Flip();
                return;
            }

            _isAnimating = true;
            _currentTween?.Kill();

            if (_isHovered)
            {
                ResetHoverImmediate();
            }

            Vector3 scaleBeforeFlip = transform.localScale;
            float halfDuration = duration / 2f;

            TweenerCore<Vector3, Vector3, VectorOptions>
                tween1 = transform.DOScaleX(0, halfDuration).SetEase(_flipEase);
            await UniTask.WaitUntil(() => !tween1.IsActive());

            _isFaceUp = !_isFaceUp;
            UpdateVisual();

            TweenerCore<Vector3, Vector3, VectorOptions> tween2 = transform.DOScaleX(scaleBeforeFlip.x, halfDuration)
                .SetEase(_flipEase);
            await UniTask.WaitUntil(() => !tween2.IsActive());

            transform.localScale = scaleBeforeFlip;
            _currentTargetScale = scaleBeforeFlip;

            _isAnimating = false;
            OnFlip?.Invoke();
        }

        /// <summary>
        ///     Перемещает карту в позицию
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        public async UniTask MoveToAsync(Vector3 position)
        {
            await MoveToAsync(position, _moveDuration);
        }

        /// <summary>
        ///     Перемещает карту в позицию с указанной длительностью
        /// </summary>
        /// <param name="position">Целевая позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToAsync(Vector3 position, float duration)
        {
            if (duration <= 0)
            {
                transform.position = position;
                _originalPosition = position;
                OnMoveComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            _currentTween?.Kill();

            if (_isHovered)
            {
                ResetHoverImmediate();
            }

            _currentTween = transform.DOMove(position, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());

            _originalPosition = position;
            _isAnimating = false;
            OnMoveComplete?.Invoke();
        }

        /// <summary>
        ///     Перемещает карту в локальную позицию
        /// </summary>
        /// <param name="localPosition">Локальная позиция</param>
        /// <param name="duration">Длительность анимации</param>
        public async UniTask MoveToLocalAsync(Vector3 localPosition, float duration)
        {
            if (duration <= 0)
            {
                transform.localPosition = localPosition;
                _originalPosition = transform.position;
                OnMoveComplete?.Invoke();
                return;
            }

            _isAnimating = true;
            _currentTween?.Kill();

            if (_isHovered)
            {
                ResetHoverImmediate();
            }

            _currentTween = transform.DOLocalMove(localPosition, duration).SetEase(_moveEase);
            await UniTask.WaitUntil(() => !_currentTween.IsActive());

            _originalPosition = transform.position;
            _isAnimating = false;
            OnMoveComplete?.Invoke();
        }

        /// <summary>
        ///     Сбрасывает hover эффект с анимацией
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
                _hoverScaleTween = transform.DOScale(_currentTargetScale, _hoverDuration);
            }

            if (hasMoveEffect)
            {
                _hoverMoveTween = transform.DOMove(_originalPosition, _hoverDuration);
            }

            OnHoverExit?.Invoke();
        }

        /// <summary>
        ///     Мгновенно сбрасывает hover без анимации
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
        ///     Завершает текущую анимацию мгновенно
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
        ///     Обновляет оригинальную позицию (вызывается после завершения расстановки в HandComponent)
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
            if (_config == null)
            {
                return;
            }

            Sprite sprite = _isFaceUp ? _config.GetSprite(Data) : _config.BackSprite;

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