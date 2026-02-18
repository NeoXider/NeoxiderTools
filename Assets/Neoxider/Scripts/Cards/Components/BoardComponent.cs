using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    ///     Компонент доски для общих карт (например, 5 карт на столе в Texas Hold'em или биты в "Дураке")
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/BoardComponent")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(BoardComponent))]
    [NeoDoc("Cards/BoardComponent.md")]
    public class BoardComponent : MonoBehaviour
    {
        [Header("Layout")] [SerializeField] private Transform[] _cardSlots;

        [SerializeField] private float _slotSpacing = 80f;
        [SerializeField] private bool _autoGenerateSlots = true;
        [SerializeField] private BoardMode _mode = BoardMode.Table;
        [SerializeField] private CardLayoutType _layoutType = CardLayoutType.Slots;
        [SerializeField] private CardLayoutSettings _layoutSettings;
        [SerializeField] private StackZSortingStrategy _stackZSorting = StackZSortingStrategy.TopCardFirst;

        [Tooltip(
            "Локальный override. Если не задан, Board попробует взять конфиг из Deck-источника или глобального fallback.")]
        [SerializeField]
        private CardAnimationConfig _animationConfig;

        [Tooltip("Optional Deck source for settings. If local config is empty, Board will use config from here.")]
        [SerializeField]
        private DeckComponent _settingsSourceDeck;

        [Header("Settings")] [SerializeField] private int _maxCards = 5;

        [SerializeField] private bool _faceUp;

        [Tooltip("Whether to automatically increase max card count when returning")] [SerializeField]
        private bool _autoExpandCapacity = true;

        [Header("Sources (for reset)")] [Tooltip("Hands from which to take cards on reset/restart")] [SerializeField]
        private List<HandComponent> _handSources = new();

        [Tooltip("Other BoardComponents to clear into this Board")] [SerializeField]
        private List<BoardComponent> _boardSources = new();

        [Tooltip("Additional root objects from which CardComponents will be collected")] [SerializeField]
        private List<Transform> _extraRoots = new();

        [Header("Debug")] [Tooltip("Last collected list of cards from RestoreAllSourcesToBoard")] [SerializeField]
        private List<CardComponent> _lastCollectedCards = new();

        [Tooltip("Cards created at initial spawn (Initial Board)")] [SerializeField]
        private List<CardComponent> _initialSpawnedCards = new();

        [Header("Animation")] [SerializeField] private float _placeDuration = 0.3f;

        public UnityEvent<CardComponent> OnCardPlaced;

        public UnityEvent OnBoardFull;
        public UnityEvent OnBoardCleared;

        [SerializeField] private List<CardComponent> _cards = new();

        /// <summary>
        ///     Карты на доске
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        ///     Количество карт на доске
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Пуста ли доска
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Заполнена ли доска
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        ///     Слоты для карт
        /// </summary>
        public Transform[] CardSlots => _cardSlots;

        private void Awake()
        {
            if (_autoGenerateSlots && (_cardSlots == null || _cardSlots.Length == 0))
            {
                GenerateSlots();
            }
        }

        /// <summary>
        ///     Размещает карту на доске
        /// </summary>
        /// <param name="card">Карта для размещения</param>
        /// <param name="animate">Анимировать</param>
        /// <param name="overrideFaceUp">Если true - применить настройку FaceUp из компонента, если false - оставить как есть</param>
        public async UniTask PlaceCardAsync(CardComponent card, bool animate = true, bool overrideFaceUp = true)
        {
            if (card == null || IsFull)
            {
                return;
            }

            int slotIndex = _cards.Count;
            Transform slot = GetSlot(slotIndex);

            card.transform.SetParent(transform, true);

            if (overrideFaceUp)
            {
                card.IsFaceUp = _faceUp;
            }

            _cards.Add(card);
            if (ShouldUseSlotPlacement())
            {
                if (animate && slot != null)
                {
                    await card.MoveToAsync(slot.position, _placeDuration);
                }
                else if (slot != null)
                {
                    card.transform.position = slot.position;
                }
            }
            else
            {
                await ArrangeCardsInternalAsync(animate);
            }

            OnCardPlaced?.Invoke(card);

            if (IsFull)
            {
                OnBoardFull?.Invoke();
            }
        }

        /// <summary>
        ///     Размещает карту синхронно
        /// </summary>
        /// <param name="card">Карта</param>
        public void PlaceCard(CardComponent card)
        {
            PlaceCardAsync(card, false).Forget();
        }

        /// <summary>
        ///     Перерасставляет карты на столе согласно текущему режиму и layout-настройкам.
        /// </summary>
        [Button("Arrange Cards")]
        public void ArrangeCards()
        {
            ArrangeCardsInternalAsync(true).Forget();
        }

        /// <summary>
        ///     Принудительно размещает карту, расширяя вместимость при необходимости (используется для рестарта).
        /// </summary>
        /// <param name="card">Карта</param>
        /// <param name="animate">Анимировать</param>
        /// <param name="overrideFaceUp">Учитывать настройку FaceUp</param>
        private async UniTask ForcePlaceCardAsync(CardComponent card, bool animate = false, bool overrideFaceUp = true)
        {
            if (card == null)
            {
                return;
            }

            if (_autoExpandCapacity && IsFull)
            {
                _maxCards = Mathf.Max(_maxCards + 1, _cards.Count + 1);
                if (_autoGenerateSlots)
                {
                    GenerateSlots();
                }
            }

            await PlaceCardAsync(card, animate, overrideFaceUp);
        }

        /// <summary>
        ///     Регистрирует карту как созданную при первоначальном спавне (Initial Board).
        /// </summary>
        /// <param name="card">Карта</param>
        public void RegisterInitialCard(CardComponent card)
        {
            if (card == null)
            {
                return;
            }

            if (!_initialSpawnedCards.Contains(card))
            {
                _initialSpawnedCards.Add(card);
            }
        }

        /// <summary>
        ///     Размещает несколько карт
        /// </summary>
        /// <param name="cards">Карты для размещения</param>
        /// <param name="animate">Анимировать</param>
        /// <param name="delayBetweenCards">Задержка между картами</param>
        public async UniTask PlaceCardsAsync(IEnumerable<CardComponent> cards, bool animate = true,
            float delayBetweenCards = 0.1f)
        {
            foreach (CardComponent card in cards)
            {
                if (IsFull)
                {
                    break;
                }

                await PlaceCardAsync(card, animate);

                if (delayBetweenCards > 0)
                {
                    await UniTask.Delay((int)(delayBetweenCards * 1000));
                }
            }
        }

        /// <summary>
        ///     Удаляет карту с доски
        /// </summary>
        /// <param name="card">Карта для удаления</param>
        /// <returns>true если карта была удалена</returns>
        public bool RemoveCard(CardComponent card)
        {
            if (card == null)
            {
                return false;
            }

            bool removed = _cards.Remove(card);
            if (removed)
            {
                card.transform.SetParent(null, true);
                if (!ShouldUseSlotPlacement())
                {
                    ArrangeCardsInternalAsync(false).Forget();
                }
            }

            return removed;
        }

        /// <summary>
        ///     Удаляет карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <returns>Удалённая карта или null</returns>
        public CardComponent RemoveAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return null;
            }

            CardComponent card = _cards[index];
            _cards.RemoveAt(index);
            card.transform.SetParent(null, true);
            if (!ShouldUseSlotPlacement())
            {
                ArrangeCardsInternalAsync(false).Forget();
            }

            return card;
        }

        /// <summary>
        ///     Очищает доску
        /// </summary>
        [Button]
        public void Clear()
        {
            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            OnBoardCleared?.Invoke();
        }

        /// <summary>
        ///     Очищает доску и возвращает карты
        /// </summary>
        /// <returns>Список карт</returns>
        public List<CardComponent> ClearAndReturn()
        {
            List<CardComponent> cards = new(_cards);

            foreach (CardComponent card in cards)
            {
                if (card != null)
                {
                    card.transform.SetParent(null, true);
                }
            }

            _cards.Clear();
            OnBoardCleared?.Invoke();

            return cards;
        }

        /// <summary>
        ///     Переворачивает все карты
        /// </summary>
        [Button]
        public void FlipAll()
        {
            FlipAllAsync().Forget();
        }

        /// <summary>
        ///     Возвращает все карты из указанных источников (рук, других досок и дополнительных корней) обратно на эту доску.
        /// </summary>
        [Button]
        public void RestoreAllSourcesToBoard()
        {
            RestoreAllSourcesToBoardAsync().Forget();
        }

        /// <summary>
        ///     Возвращает все карты из указанных источников (рук, других досок и дополнительных корней) обратно на эту доску.
        /// </summary>
        public async UniTask RestoreAllSourcesToBoardAsync()
        {
            List<CardComponent> collected = new();
            HashSet<CardComponent> seen = new();

            // Снимаем карты с рук
            foreach (HandComponent hand in _handSources)
            {
                if (hand == null)
                {
                    continue;
                }

                while (hand.Count > 0)
                {
                    CardComponent card = hand.DrawFirst();
                    if (card != null && seen.Add(card))
                    {
                        collected.Add(card);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Снимаем карты с других досок
            foreach (BoardComponent board in _boardSources)
            {
                if (board == null || board == this)
                {
                    continue;
                }

                List<CardComponent> returned = board.ClearAndReturn();
                foreach (CardComponent card in returned)
                {
                    if (card != null && seen.Add(card))
                    {
                        collected.Add(card);
                    }
                }
            }

            // Собираем дополнительные корни
            foreach (Transform root in _extraRoots)
            {
                if (root == null)
                {
                    continue;
                }

                CardComponent[] cards = root.GetComponentsInChildren<CardComponent>(true);
                foreach (CardComponent card in cards)
                {
                    if (card == null || card.transform == transform)
                    {
                        continue;
                    }

                    if (seen.Add(card))
                    {
                        collected.Add(card);
                    }
                }
            }

            // Возвращаем на эту доску
            foreach (CardComponent card in collected)
            {
                await ForcePlaceCardAsync(card);
            }

            _lastCollectedCards.Clear();
            _lastCollectedCards.AddRange(collected);

            OnBoardCleared?.Invoke();
        }

        /// <summary>
        ///     Переворачивает все карты с анимацией
        /// </summary>
        /// <param name="delayBetweenCards">Задержка между переворотами</param>
        public async UniTask FlipAllAsync(float delayBetweenCards = 0.1f)
        {
            foreach (CardComponent card in _cards)
            {
                await card.FlipAsync();

                if (delayBetweenCards > 0)
                {
                    await UniTask.Delay((int)(delayBetweenCards * 1000));
                }
            }
        }

        /// <summary>
        ///     Возвращает все ранги карт на доске
        /// </summary>
        /// <returns>Набор рангов</returns>
        public HashSet<Rank> GetAllRanks()
        {
            HashSet<Rank> ranks = new();

            foreach (CardComponent card in _cards)
            {
                if (!card.Data.IsJoker)
                {
                    ranks.Add(card.Data.Rank);
                }
            }

            return ranks;
        }

        /// <summary>
        ///     Возвращает все данные карт на доске
        /// </summary>
        /// <returns>Список данных карт</returns>
        public List<CardData> GetAllCardData()
        {
            List<CardData> data = new();

            foreach (CardComponent card in _cards)
            {
                data.Add(card.Data);
            }

            return data;
        }

        private Transform GetSlot(int index)
        {
            if (_cardSlots == null || _cardSlots.Length == 0)
            {
                return transform;
            }

            if (index < 0 || index >= _cardSlots.Length)
            {
                return transform;
            }

            return _cardSlots[index];
        }

        private bool ShouldUseSlotPlacement()
        {
            return _mode == BoardMode.Table && _layoutType == CardLayoutType.Slots;
        }

        private CardLayoutType ResolveLayoutType()
        {
            if (_mode == BoardMode.Beat)
            {
                return CardLayoutType.Scattered;
            }

            return _layoutType;
        }

        private async UniTask ArrangeCardsInternalAsync(bool animate)
        {
            if (_cards.Count == 0 || ShouldUseSlotPlacement())
            {
                return;
            }

            CardLayoutSettings settings = _layoutSettings;
            if (settings.Spacing == 0f && settings.ArcRadius == 0f && settings.GridColumns == 0)
            {
                settings = CardLayoutSettings.Default;
                settings.Spacing = _slotSpacing;
            }

            CardAnimationConfig resolvedAnimationConfig = ResolveAnimationConfig();
            if (resolvedAnimationConfig != null)
            {
                settings.StackStep = resolvedAnimationConfig.StackStepY;
                settings.PositionJitter = resolvedAnimationConfig.StackPositionJitter;
                settings.RotationJitter = resolvedAnimationConfig.StackRotationJitter;
            }

            CardLayoutType layoutType = ResolveLayoutType();
            List<Vector3> positions = CardLayoutCalculator.CalculatePositions(layoutType, _cards.Count, settings);
            List<Quaternion> rotations = CardLayoutCalculator.CalculateRotations(layoutType, _cards.Count, settings);

            for (int i = 0; i < _cards.Count; i++)
            {
                CardComponent card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                int sibling = _stackZSorting == StackZSortingStrategy.TopCardFirst ? i : _cards.Count - 1 - i;
                card.transform.SetSiblingIndex(sibling);
                Vector3 worldPosition = transform.TransformPoint(positions[i]);

                if (animate)
                {
                    await card.MoveToAsync(worldPosition, _placeDuration);
                }
                else
                {
                    card.transform.position = worldPosition;
                }

                card.transform.localRotation = rotations[i];
            }
        }

        private CardAnimationConfig ResolveAnimationConfig()
        {
            if (_animationConfig != null)
            {
                return _animationConfig;
            }

            if (_settingsSourceDeck != null && _settingsSourceDeck.AnimationConfig != null)
            {
                return _settingsSourceDeck.AnimationConfig;
            }

            return CardSettingsRuntime.GlobalAnimationConfig;
        }

        private void GenerateSlots()
        {
            _cardSlots = new Transform[_maxCards];

            float totalWidth = (_maxCards - 1) * _slotSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < _maxCards; i++)
            {
                GameObject slot = new($"Slot_{i}");
                slot.transform.SetParent(transform);
                slot.transform.localPosition = new Vector3(startX + i * _slotSpacing, 0, 0);
                slot.transform.localRotation = Quaternion.identity;
                _cardSlots[i] = slot.transform;
            }
        }
    }
}