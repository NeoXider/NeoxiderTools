using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    ///     Компонент колоды для работы без кода
    /// </summary>
    public class DeckComponent : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private DeckConfig _config;

        [SerializeField] private bool _initializeOnStart = true;
        [SerializeField] private bool _shuffleOnStart = true;

        [Header("Visual")] [SerializeField] private Transform _spawnPoint;

        [SerializeField] private CardComponent _cardPrefab;
        [SerializeField] private CardLayoutType _visualLayoutType = CardLayoutType.Stack;
        [SerializeField] private BoardComponent _visualStackBoard;
        [SerializeField] private bool _spawnVisualOnInitialize;
        [SerializeField] private bool _stackFaceUp;
        [SerializeField] private Vector3 _stackPositionJitter = new(0.02f, 0f, 0.02f);
        [SerializeField] private Vector3 _stackRotationJitter = new(0f, 0f, 3f);
        [SerializeField] private float _stackStepY = 0.01f;
        [SerializeField] private Vector3 _stackOffsetPosition;
        [SerializeField] private Vector3 _stackOffsetRotation;
        [SerializeField] private CardAnimationConfig _animationConfig;
        [Tooltip("Если включено, конфиг колоды становится глобальным fallback для остальных Card-компонентов.")]
        [SerializeField] private bool _setAnimationConfigAsGlobal = true;

        [Header("Trump Display")] [SerializeField]
        private bool _showTrumpCard = true;

        [SerializeField] private CardComponent _trumpCardDisplay;

        /// <summary>
        ///     Событие после инициализации колоды.
        /// </summary>
        public UnityEvent OnInitialized;

        /// <summary>
        ///     Событие после перемешивания модели колоды.
        /// </summary>
        public UnityEvent OnShuffled;

        /// <summary>
        ///     Событие, когда в модели больше не осталось карт.
        /// </summary>
        public UnityEvent OnDeckEmpty;

        /// <summary>
        ///     Событие взятия карты через DrawCard/DrawCardAsync.
        /// </summary>
        public UnityEvent<CardComponent> OnCardDrawn;

        /// <summary>
        ///     Событие изменения визуальной стопки (добавление, удаление, shuffle).
        /// </summary>
        public UnityEvent OnVisualStackChanged;

        /// <summary>
        ///     Событие успешной сборки визуальной стопки.
        /// </summary>
        public UnityEvent OnVisualStackBuilt;

        /// <summary>
        ///     Событие начала визуального перемешивания.
        /// </summary>
        public UnityEvent<ShuffleVisualType> OnShuffleVisualStarted;

        /// <summary>
        ///     Событие окончания визуального перемешивания.
        /// </summary>
        public UnityEvent OnShuffleVisualCompleted;

        /// <summary>
        ///     Событие раздачи карты в руку.
        /// </summary>
        public UnityEvent<CardComponent, HandComponent> OnCardDealt;
        private readonly List<CardComponent> _activeCards = new();

        /// <summary>
        ///     Модель колоды
        /// </summary>
        public DeckModel Model { get; private set; }

        /// <summary>
        ///     Количество оставшихся карт
        /// </summary>
        public int RemainingCount => Model?.RemainingCount ?? 0;

        /// <summary>
        ///     Пуста ли колода
        /// </summary>
        public bool IsEmpty => Model?.IsEmpty ?? true;

        /// <summary>
        ///     Козырная карта
        /// </summary>
        public CardData? TrumpCard => Model?.PeekBottom();

        /// <summary>
        ///     Козырная масть
        /// </summary>
        public Suit? TrumpSuit => TrumpCard?.Suit;

        /// <summary>
        ///     Точка спавна карт
        /// </summary>
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        /// <summary>
        ///     Конфигурация колоды (спрайты, тип колоды и пр.).
        /// </summary>
        public DeckConfig Config => _config;

        /// <summary>
        ///     Префаб карты, используемый колодой при DrawCard.
        /// </summary>
        public CardComponent CardPrefab => _cardPrefab;

        /// <summary>
        ///     Конфиг анимаций, назначенный на эту колоду.
        /// </summary>
        public CardAnimationConfig AnimationConfig => _animationConfig;

        /// <summary>
        ///     Сбрасывает колоду
        /// </summary>
        [Button]
        public void Reset()
        {
            foreach (CardComponent card in _activeCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _activeCards.Clear();

            Model?.Reset(_shuffleOnStart);
        }

        private void Start()
        {
            if (_setAnimationConfigAsGlobal && _animationConfig != null)
            {
                CardSettingsRuntime.GlobalAnimationConfig = _animationConfig;
            }

            if (_initializeOnStart)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (Model != null)
            {
                Model.OnDeckEmpty -= HandleDeckEmpty;
            }
        }

        /// <summary>
        ///     Инициализирует колоду
        /// </summary>
        [Button]
        public void Initialize()
        {
            Model = new DeckModel();
            Model.OnDeckEmpty += HandleDeckEmpty;
            Model.Initialize(_config.GameDeckType, _shuffleOnStart);

            if (_showTrumpCard && _trumpCardDisplay != null)
            {
                CardData? trump = TrumpCard;
                if (trump.HasValue)
                {
                    _trumpCardDisplay.SetData(trump.Value);
                    _trumpCardDisplay.gameObject.SetActive(true);
                }
            }

            OnInitialized?.Invoke();

            if (_spawnVisualOnInitialize)
            {
                BuildVisualStackAsync().Forget();
            }
        }

        /// <summary>
        ///     Перемешивает колоду
        /// </summary>
        [Button]
        public void Shuffle()
        {
            Model?.Shuffle();
            OnShuffled?.Invoke();
        }

        /// <summary>
        ///     Синхронная кнопка-обертка для сборки визуальной стопки.
        /// </summary>
        [Button("Build Visual Stack")]
        public void BuildVisualStack()
        {
            BuildVisualStackAsync().Forget();
        }

        /// <summary>
        ///     Строит визуальную стопку карт на указанном BoardComponent.
        ///     Карты берутся из текущей модели DeckModel без изменения порядка.
        /// </summary>
        public async UniTask BuildVisualStackAsync()
        {
            if (_visualStackBoard == null || _cardPrefab == null || Model == null)
            {
                return;
            }

            _visualStackBoard.Clear();
            _activeCards.RemoveAll(card => card == null);

            foreach (CardData cardData in Model.Cards)
            {
                CardComponent card = Instantiate(_cardPrefab, SpawnPoint.position, Quaternion.identity);
                card.Config = _config;
                card.SetData(cardData, _stackFaceUp);
                _activeCards.Add(card);
                await _visualStackBoard.PlaceCardAsync(card, false, false);
            }

            ApplyVisualLayout(_visualStackBoard.Cards);
            OnVisualStackBuilt?.Invoke();
            OnVisualStackChanged?.Invoke();
        }

        /// <summary>
        ///     Синхронная кнопка-обертка для визуального перемешивания.
        /// </summary>
        [Button("Shuffle Visual")]
        public void ShuffleVisual(ShuffleVisualType type = ShuffleVisualType.Shake)
        {
            ShuffleVisualAsync(type).Forget();
        }

        /// <summary>
        ///     Перемешивает модель колоды и синхронизирует визуальную стопку.
        ///     При необходимости воспроизводит визуальный эффект перемешивания.
        /// </summary>
        /// <param name="type">Тип визуального shuffle-эффекта.</param>
        /// <param name="duration">Опциональная длительность. Если null, берется из конфигурации.</param>
        public async UniTask ShuffleVisualAsync(ShuffleVisualType type, float? duration = null)
        {
            if (Model == null)
            {
                return;
            }

            OnShuffleVisualStarted?.Invoke(type);
            Model.Shuffle();
            OnShuffled?.Invoke();

            if (_visualStackBoard != null && _visualStackBoard.Cards.Count > 0)
            {
                List<CardComponent> all = _visualStackBoard.ClearAndReturn();
                List<CardComponent> reordered = ReorderByModel(all);
                foreach (CardComponent card in reordered)
                {
                    await _visualStackBoard.PlaceCardAsync(card, false, false);
                }

                ApplyVisualLayout(_visualStackBoard.Cards);

                switch (type)
                {
                    case ShuffleVisualType.Shake:
                        await PlayShakeAnimation(duration ?? (_animationConfig != null ? _animationConfig.ShakeDuration : 1f));
                        break;
                    case ShuffleVisualType.Cut:
                        await UniTask.Delay((int)((duration ?? (_animationConfig != null ? _animationConfig.CutDuration : 0.8f)) * 1000f));
                        break;
                    case ShuffleVisualType.Riffle:
                        await UniTask.Delay((int)((duration ?? (_animationConfig != null ? _animationConfig.RiffleDuration : 1.2f)) * 1000f));
                        break;
                }
            }

            OnShuffleVisualCompleted?.Invoke();
            OnVisualStackChanged?.Invoke();
        }

        /// <summary>
        ///     Синхронная кнопка-обертка для раздачи верхней карты в руку.
        /// </summary>
        [Button("Deal To Hand")]
        public void DealToHand(HandComponent hand, bool faceUp = true)
        {
            DealToHandAsync(hand, faceUp).Forget();
        }

        /// <summary>
        ///     Раздает верхнюю карту из визуальной стопки (или напрямую из модели) в указанную руку.
        /// </summary>
        /// <param name="hand">Целевая рука.</param>
        /// <param name="faceUp">Показать карту лицом вверх.</param>
        /// <param name="moveDuration">Опциональная длительность перелета карты.</param>
        /// <returns>Разданная карта или null, если раздача невозможна.</returns>
        public async UniTask<CardComponent> DealToHandAsync(HandComponent hand, bool faceUp, float? moveDuration = null)
        {
            if (hand == null)
            {
                return null;
            }

            CardComponent card = null;
            if (_visualStackBoard != null && _visualStackBoard.Cards.Count > 0)
            {
                card = _visualStackBoard.Cards[_visualStackBoard.Cards.Count - 1];
                _visualStackBoard.RemoveCard(card);
                if (Model != null && !Model.IsEmpty)
                {
                    Model.Draw();
                }
                card.IsFaceUp = faceUp;
            }
            else
            {
                card = DrawCard(faceUp);
            }

            if (card == null)
            {
                return null;
            }

            if (moveDuration.HasValue && moveDuration.Value > 0f)
            {
                await card.MoveToAsync(hand.transform.position, moveDuration.Value);
            }
            else if (_animationConfig != null && _animationConfig.DealMoveDuration > 0f)
            {
                await card.MoveToAsync(hand.transform.position, _animationConfig.DealMoveDuration);
            }

            await hand.AddCardAsync(card, true);
            OnCardDealt?.Invoke(card, hand);
            OnVisualStackChanged?.Invoke();
            return card;
        }

        /// <summary>
        ///     Берёт карту из колоды
        /// </summary>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Компонент карты или null</returns>
        public CardComponent DrawCard(bool faceUp = true)
        {
            if (Model == null || _cardPrefab == null)
            {
                return null;
            }

            CardData? cardData = Model.Draw();
            if (!cardData.HasValue)
            {
                return null;
            }

            CardComponent card = Instantiate(_cardPrefab, SpawnPoint.position, Quaternion.identity);
            card.Config = _config;
            card.SetData(cardData.Value, faceUp);

            _activeCards.Add(card);
            OnCardDrawn?.Invoke(card);

            return card;
        }

        /// <summary>
        ///     Берёт карту из колоды с анимацией
        /// </summary>
        /// <param name="targetPosition">Целевая позиция</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <param name="duration">Длительность перемещения</param>
        /// <returns>Компонент карты</returns>
        public async UniTask<CardComponent> DrawCardAsync(Vector3 targetPosition, bool faceUp = true,
            float duration = 0.3f)
        {
            CardComponent card = DrawCard(faceUp);
            if (card == null)
            {
                return null;
            }

            await card.MoveToAsync(targetPosition, duration);
            return card;
        }

        /// <summary>
        ///     Берёт несколько карт
        /// </summary>
        /// <param name="count">Количество карт</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Список карт</returns>
        public List<CardComponent> DrawCards(int count, bool faceUp = true)
        {
            List<CardComponent> cards = new();

            for (int i = 0; i < count; i++)
            {
                CardComponent card = DrawCard(faceUp);
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        /// <summary>
        ///     Возвращает карту в колоду
        /// </summary>
        /// <param name="card">Карта для возврата</param>
        /// <param name="toTop">В начало колоды</param>
        public void ReturnCard(CardComponent card, bool toTop = false)
        {
            if (card == null || Model == null)
            {
                return;
            }

            _activeCards.Remove(card);

            if (toTop)
            {
                Model.ReturnToTop(card.Data);
            }
            else
            {
                Model.ReturnToBottom(card.Data);
            }

            Destroy(card.gameObject);
        }

        private void HandleDeckEmpty()
        {
            if (_trumpCardDisplay != null)
            {
                _trumpCardDisplay.gameObject.SetActive(false);
            }

            OnDeckEmpty?.Invoke();
        }

        private List<CardComponent> ReorderByModel(List<CardComponent> cards)
        {
            if (Model == null || cards == null || cards.Count == 0)
            {
                return cards ?? new List<CardComponent>();
            }

            Dictionary<CardData, Queue<CardComponent>> map = new();
            foreach (CardComponent card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                if (!map.TryGetValue(card.Data, out Queue<CardComponent> queue))
                {
                    queue = new Queue<CardComponent>();
                    map[card.Data] = queue;
                }

                queue.Enqueue(card);
            }

            List<CardComponent> ordered = new(cards.Count);
            foreach (CardData data in Model.Cards)
            {
                if (map.TryGetValue(data, out Queue<CardComponent> queue) && queue.Count > 0)
                {
                    ordered.Add(queue.Dequeue());
                }
            }

            ordered.AddRange(cards.Where(card => card != null && !ordered.Contains(card)));
            return ordered;
        }

        private void ApplyVisualLayout(IReadOnlyList<CardComponent> cards)
        {
            if (_visualStackBoard == null || cards == null)
            {
                return;
            }

            CardLayoutSettings settings = CardLayoutSettings.Default;
            settings.StackStep = _animationConfig != null ? _animationConfig.StackStepY : _stackStepY;
            settings.PositionJitter = _animationConfig != null ? _animationConfig.StackPositionJitter : _stackPositionJitter;
            settings.RotationJitter = _animationConfig != null ? _animationConfig.StackRotationJitter : _stackRotationJitter;

            List<Vector3> positions = CardLayoutCalculator.CalculatePositions(_visualLayoutType, cards.Count, settings);
            List<Quaternion> rotations = CardLayoutCalculator.CalculateRotations(_visualLayoutType, cards.Count, settings);

            for (int i = 0; i < cards.Count; i++)
            {
                CardComponent card = cards[i];
                if (card == null)
                {
                    continue;
                }

                Vector3 localPosition = positions.Count > i ? positions[i] : Vector3.zero;
                Quaternion localRotation = rotations.Count > i ? rotations[i] : Quaternion.identity;

                localPosition += _stackOffsetPosition;
                localRotation = Quaternion.Euler(_stackOffsetRotation) * localRotation;

                card.transform.localPosition = localPosition;
                card.transform.localRotation = localRotation;
            }
        }

        private async UniTask PlayShakeAnimation(float duration)
        {
            if (_visualStackBoard == null || _visualStackBoard.Cards.Count == 0)
            {
                return;
            }

            int frames = _animationConfig != null ? _animationConfig.ShakeFrames : 20;
            float intensity = _animationConfig != null ? _animationConfig.ShakeIntensity : 0.12f;

            List<CardComponent> cards = _visualStackBoard.Cards.ToList();
            List<Vector3> basePositions = cards.Select(card => card != null ? card.transform.localPosition : Vector3.zero).ToList();

            for (int i = 0; i < frames; i++)
            {
                Vector3 offset = new(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity * 0.4f, intensity * 0.4f),
                    Random.Range(-intensity, intensity));

                for (int index = 0; index < cards.Count; index++)
                {
                    if (cards[index] != null)
                    {
                        cards[index].transform.localPosition = basePositions[index] + offset;
                    }
                }

                await UniTask.Delay((int)(duration * 1000f / frames));
            }

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    cards[i].transform.localPosition = basePositions[i];
                }
            }
        }
    }
}