using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    ///     Компонент руки для работы без кода
    /// </summary>
    public class HandComponent : MonoBehaviour
    {
        [Header("Layout")] [SerializeField] private CardLayoutType _layoutType = CardLayoutType.Fan;

        [SerializeField] private float _spacing = 60f;
        [SerializeField] private float _arcAngle = 30f;
        [SerializeField] private float _arcRadius = 400f;

        [Header("Grid Settings")] [SerializeField]
        private int _gridColumns = 5;

        [SerializeField] private float _gridRowSpacing = 80f;

        [Header("Limits")] [SerializeField] private int _maxCards = 36;

        [Header("Card Order")]
        [Tooltip("Если true - новые карты добавляются под низ (sibling index 0)")]
        [SerializeField]
        private bool _addToBottom;

        [Header("Animation")] [SerializeField] private float _arrangeDuration = 0.3f;

        [SerializeField] private Ease _arrangeEase = Ease.OutQuad;

        [SerializeField] private UnityEvent<int> _onCardCountChanged;

        [SerializeField] private UnityEvent<CardComponent> _onCardAdded;
        [SerializeField] private UnityEvent<CardComponent> _onCardRemoved;
        [SerializeField] private UnityEvent<CardComponent> _onCardClicked;
        [SerializeField] private UnityEvent _onHandChanged;

        private readonly List<CardComponent> _cards = new();

        /// <summary>
        ///     Событие изменения количества карт в руке. Передаёт текущее количество.
        /// </summary>
        public UnityEvent<int> OnCardCountChanged => _onCardCountChanged;

        /// <summary>
        ///     Событие добавления карты.
        /// </summary>
        public UnityEvent<CardComponent> OnCardAdded => _onCardAdded;

        /// <summary>
        ///     Событие удаления карты.
        /// </summary>
        public UnityEvent<CardComponent> OnCardRemoved => _onCardRemoved;

        /// <summary>
        ///     Событие клика по карте.
        /// </summary>
        public UnityEvent<CardComponent> OnCardClicked => _onCardClicked;

        /// <summary>
        ///     Событие изменения руки.
        /// </summary>
        public UnityEvent OnHandChanged => _onHandChanged;

        /// <summary>
        ///     Модель руки
        /// </summary>
        public HandModel Model { get; private set; }

        /// <summary>
        ///     Карты в руке
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        ///     Количество карт
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Пуста ли рука
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Заполнена ли рука
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        ///     Тип раскладки
        /// </summary>
        public CardLayoutType LayoutType
        {
            get => _layoutType;
            set
            {
                _layoutType = value;
                ArrangeCardsAsync().Forget();
            }
        }

        private void Awake()
        {
            EnsureModelInitialized();
        }

        private void EnsureModelInitialized()
        {
            if (Model == null)
            {
                Model = new HandModel();
            }
        }

        /// <summary>
        ///     Добавляет карту в руку
        /// </summary>
        /// <param name="card">Карта для добавления</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask AddCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || IsFull)
            {
                return;
            }

            EnsureModelInitialized();

            Vector3 startPosition = card.transform.position;

            card.transform.SetParent(transform, true);
            card.OnClick.AddListener(() => HandleCardClick(card));

            if (_addToBottom)
            {
                _cards.Insert(0, card);
                Model.Add(card.Data);
            }
            else
            {
                _cards.Add(card);
                Model.Add(card.Data);
            }

            if (animate)
            {
                card.transform.position = startPosition;

                List<Vector3> positions = CalculatePositions();
                List<Quaternion> rotations = CalculateRotations();
                int targetIndex = _addToBottom ? 0 : _cards.Count - 1;

                for (int i = 0; i < _cards.Count; i++)
                {
                    _cards[i].transform.SetSiblingIndex(_addToBottom ? _cards.Count - 1 - i : i);
                }

                if (targetIndex < positions.Count && targetIndex < rotations.Count)
                {
                    await AnimateCard(card, positions[targetIndex], rotations[targetIndex]);
                }

                for (int i = 0; i < _cards.Count; i++)
                {
                    if (i != targetIndex && i < positions.Count && i < rotations.Count)
                    {
                        _cards[i].transform.localPosition = positions[i];
                        _cards[i].transform.localRotation = rotations[i];
                        _cards[i].UpdateOriginalTransform();
                    }
                }

                card.UpdateOriginalTransform();
            }
            else
            {
                await ArrangeCardsAsync(false);
            }

            _onCardAdded?.Invoke(card);
            _onCardCountChanged?.Invoke(_cards.Count);
            _onHandChanged?.Invoke();
        }

        /// <summary>
        ///     Добавляет карту синхронно
        /// </summary>
        /// <param name="card">Карта</param>
        public void AddCard(CardComponent card)
        {
            AddCardAsync(card, false).Forget();
        }

        /// <summary>
        ///     Берёт первую карту из руки (для игры "Пьяница").
        /// </summary>
        /// <returns>Первая карта или null если рука пуста</returns>
        public CardComponent DrawFirst()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            CardComponent card = _cards[0];
            RemoveCard(card);
            return card;
        }

        /// <summary>
        ///     Берёт случайную карту из руки.
        /// </summary>
        /// <returns>Случайная карта или null если рука пуста</returns>
        public CardComponent DrawRandom()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, _cards.Count);
            CardComponent card = _cards[index];
            RemoveCard(card);
            return card;
        }

        /// <summary>
        ///     Удаляет карту из руки
        /// </summary>
        /// <param name="card">Карта для удаления</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask RemoveCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || !_cards.Contains(card))
            {
                return;
            }

            EnsureModelInitialized();

            card.OnClick.RemoveAllListeners();
            card.ResetHover();
            card.transform.SetParent(null, true);

            _cards.Remove(card);
            Model.Remove(card.Data);

            await ArrangeCardsAsync(animate);

            _onCardRemoved?.Invoke(card);
            _onCardCountChanged?.Invoke(_cards.Count);
            _onHandChanged?.Invoke();
        }

        /// <summary>
        ///     Удаляет карту синхронно
        /// </summary>
        /// <param name="card">Карта</param>
        public void RemoveCard(CardComponent card)
        {
            RemoveCardAsync(card, false).Forget();
        }

        /// <summary>
        ///     Удаляет карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <param name="animate">Анимировать</param>
        /// <returns>Удалённая карта</returns>
        public async UniTask<CardComponent> RemoveAtAsync(int index, bool animate = true)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return null;
            }

            CardComponent card = _cards[index];
            await RemoveCardAsync(card, animate);
            return card;
        }

        /// <summary>
        ///     Сортирует карты по рангу
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
        [Button]
        public void SortByRank(bool ascending = true)
        {
            SortByRankAsync(ascending).Forget();
        }

        /// <summary>
        ///     Сортирует карты по рангу с анимацией
        /// </summary>
        public async UniTask SortByRankAsync(bool ascending = true, bool animate = true)
        {
            EnsureModelInitialized();
            Model.SortByRank(ascending);

            if (ascending)
            {
                _cards.Sort((a, b) => a.Data.Rank.CompareTo(b.Data.Rank));
            }
            else
            {
                _cards.Sort((a, b) => b.Data.Rank.CompareTo(a.Data.Rank));
            }

            await ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Сортирует карты по масти
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
        [Button]
        public void SortBySuit(bool ascending = true)
        {
            SortBySuitAsync(ascending).Forget();
        }

        /// <summary>
        ///     Сортирует карты по масти с анимацией
        /// </summary>
        public async UniTask SortBySuitAsync(bool ascending = true, bool animate = true)
        {
            EnsureModelInitialized();
            Model.SortBySuit(ascending);

            if (ascending)
            {
                _cards.Sort((a, b) =>
                {
                    int suitCompare = a.Data.Suit.CompareTo(b.Data.Suit);
                    return suitCompare != 0 ? suitCompare : a.Data.Rank.CompareTo(b.Data.Rank);
                });
            }
            else
            {
                _cards.Sort((a, b) =>
                {
                    int suitCompare = b.Data.Suit.CompareTo(a.Data.Suit);
                    return suitCompare != 0 ? suitCompare : b.Data.Rank.CompareTo(a.Data.Rank);
                });
            }

            await ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Находит карты, которыми можно побить указанную
        /// </summary>
        /// <param name="attackCard">Атакующая карта</param>
        /// <param name="trump">Козырная масть</param>
        /// <returns>Список карт</returns>
        public List<CardComponent> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            List<CardComponent> result = new();

            foreach (CardComponent card in _cards)
            {
                if (card.Data.CanCover(attackCard, trump))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        ///     Очищает руку
        /// </summary>
        [Button]
        public void Clear()
        {
            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    card.OnClick.RemoveAllListeners();
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();

            EnsureModelInitialized();
            Model.Clear();

            _onCardCountChanged?.Invoke(0);
            _onHandChanged?.Invoke();
        }

        /// <summary>
        ///     Расставляет карты
        /// </summary>
        /// <param name="animate">Анимировать</param>
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            List<Vector3> positions = CalculatePositions();
            List<Quaternion> rotations = CalculateRotations();

            List<UniTask> tasks = new();

            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].transform.SetSiblingIndex(i);

                if (animate)
                {
                    tasks.Add(AnimateCard(_cards[i], positions[i], rotations[i]));
                }
                else
                {
                    _cards[i].transform.localPosition = positions[i];
                    _cards[i].transform.localRotation = rotations[i];
                    _cards[i].UpdateOriginalTransform();
                }
            }

            if (animate && tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            foreach (CardComponent card in _cards)
            {
                card.UpdateOriginalTransform();
            }
        }

        private void HandleCardClick(CardComponent card)
        {
            _onCardClicked?.Invoke(card);
        }

        private List<Vector3> CalculatePositions()
        {
            return CardLayoutCalculator.CalculatePositions(_layoutType, _cards.Count, BuildLayoutSettings());
        }

        private List<Quaternion> CalculateRotations()
        {
            return CardLayoutCalculator.CalculateRotations(_layoutType, _cards.Count, BuildLayoutSettings());
        }

        private CardLayoutSettings BuildLayoutSettings()
        {
            CardLayoutSettings settings = CardLayoutSettings.Default;
            settings.Spacing = _spacing;
            settings.ArcAngle = _arcAngle;
            settings.ArcRadius = _arcRadius;
            settings.GridColumns = _gridColumns;
            settings.GridRowSpacing = _gridRowSpacing;
            settings.StackStep = 2f;
            return settings;
        }

        [Button("Arrange")]
        private void ArrangeByButton()
        {
            ArrangeCardsAsync().Forget();
        }

        /// <summary>
        ///     Устаревшее свойство для обратной совместимости со старыми сценами.
        ///     Используйте LayoutType (CardLayoutType).
        /// </summary>
        [System.Obsolete("Use LayoutType with CardLayoutType.")]
        public HandLayoutType LegacyLayoutType
        {
            get => (HandLayoutType)(int)_layoutType;
            set
            {
                _layoutType = (CardLayoutType)(int)value;
                ArrangeCardsAsync().Forget();
            }
        }

        private async UniTask AnimateCard(CardComponent card, Vector3 position, Quaternion rotation)
        {
            TweenerCore<Vector3, Vector3, VectorOptions> moveTween =
                card.transform.DOLocalMove(position, _arrangeDuration).SetEase(_arrangeEase);
            TweenerCore<Quaternion, Quaternion, NoOptions> rotateTween =
                card.transform.DOLocalRotateQuaternion(rotation, _arrangeDuration).SetEase(_arrangeEase);

            await UniTask.WaitUntil(() => !moveTween.IsActive() && !rotateTween.IsActive());
        }
    }
}