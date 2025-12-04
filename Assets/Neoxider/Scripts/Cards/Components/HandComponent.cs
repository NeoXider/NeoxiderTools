using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    /// Компонент руки для работы без кода
    /// </summary>
    public class HandComponent : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private HandLayoutType _layoutType = HandLayoutType.Fan;
        [SerializeField] private float _spacing = 60f;
        [SerializeField] private float _arcAngle = 30f;
        [SerializeField] private float _arcRadius = 400f;

        [Header("Grid Settings")]
        [SerializeField] private int _gridColumns = 5;
        [SerializeField] private float _gridRowSpacing = 80f;

        [Header("Limits")]
        [SerializeField] private int _maxCards = 36;

        [Header("Animation")]
        [SerializeField] private float _arrangeDuration = 0.3f;
        [SerializeField] private Ease _arrangeEase = Ease.OutQuad;

        [Header("Events")]
        public UnityEvent<CardComponent> OnCardAdded;
        public UnityEvent<CardComponent> OnCardRemoved;
        public UnityEvent<CardComponent> OnCardClicked;
        public UnityEvent OnHandChanged;

        private readonly List<CardComponent> _cards = new();
        private HandModel _model;

        /// <summary>
        /// Модель руки
        /// </summary>
        public HandModel Model => _model;

        /// <summary>
        /// Карты в руке
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        /// Количество карт
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        /// Пуста ли рука
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        /// Заполнена ли рука
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        /// Тип раскладки
        /// </summary>
        public HandLayoutType LayoutType
        {
            get => _layoutType;
            set
            {
                _layoutType = value;
                ArrangeCardsAsync(true).Forget();
            }
        }

        private void Awake()
        {
            _model = new HandModel();
        }

        /// <summary>
        /// Добавляет карту в руку
        /// </summary>
        /// <param name="card">Карта для добавления</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask AddCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || IsFull) return;

            card.transform.SetParent(transform, true);
            card.OnClick.AddListener(() => HandleCardClick(card));

            _cards.Add(card);
            _model.Add(card.Data);

            await ArrangeCardsAsync(animate);

            OnCardAdded?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Добавляет карту синхронно
        /// </summary>
        /// <param name="card">Карта</param>
        public void AddCard(CardComponent card)
        {
            AddCardAsync(card, false).Forget();
        }

        /// <summary>
        /// Удаляет карту из руки
        /// </summary>
        /// <param name="card">Карта для удаления</param>
        /// <param name="animate">Анимировать</param>
        public async UniTask RemoveCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || !_cards.Contains(card)) return;

            card.OnClick.RemoveAllListeners();
            card.transform.SetParent(null, true);

            _cards.Remove(card);
            _model.Remove(card.Data);

            await ArrangeCardsAsync(animate);

            OnCardRemoved?.Invoke(card);
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Удаляет карту синхронно
        /// </summary>
        /// <param name="card">Карта</param>
        public void RemoveCard(CardComponent card)
        {
            RemoveCardAsync(card, false).Forget();
        }

        /// <summary>
        /// Удаляет карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <param name="animate">Анимировать</param>
        /// <returns>Удалённая карта</returns>
        public async UniTask<CardComponent> RemoveAtAsync(int index, bool animate = true)
        {
            if (index < 0 || index >= _cards.Count) return null;

            CardComponent card = _cards[index];
            await RemoveCardAsync(card, animate);
            return card;
        }

        /// <summary>
        /// Сортирует карты по рангу
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void SortByRank(bool ascending = true)
        {
            SortByRankAsync(ascending, true).Forget();
        }

        /// <summary>
        /// Сортирует карты по рангу с анимацией
        /// </summary>
        public async UniTask SortByRankAsync(bool ascending = true, bool animate = true)
        {
            _model.SortByRank(ascending);

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
        /// Сортирует карты по масти
        /// </summary>
        /// <param name="ascending">По возрастанию</param>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void SortBySuit(bool ascending = true)
        {
            SortBySuitAsync(ascending, true).Forget();
        }

        /// <summary>
        /// Сортирует карты по масти с анимацией
        /// </summary>
        public async UniTask SortBySuitAsync(bool ascending = true, bool animate = true)
        {
            _model.SortBySuit(ascending);

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
        /// Находит карты, которыми можно побить указанную
        /// </summary>
        /// <param name="attackCard">Атакующая карта</param>
        /// <param name="trump">Козырная масть</param>
        /// <returns>Список карт</returns>
        public List<CardComponent> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            var result = new List<CardComponent>();

            foreach (var card in _cards)
            {
                if (card.Data.CanCover(attackCard, trump))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        /// Очищает руку
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void Clear()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    card.OnClick.RemoveAllListeners();
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            _model.Clear();
            OnHandChanged?.Invoke();
        }

        /// <summary>
        /// Расставляет карты
        /// </summary>
        /// <param name="animate">Анимировать</param>
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            if (_cards.Count == 0) return;

            var positions = CalculatePositions();
            var rotations = CalculateRotations();

            var tasks = new List<UniTask>();

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
                }
            }

            if (animate && tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        private void HandleCardClick(CardComponent card)
        {
            OnCardClicked?.Invoke(card);
        }

        private List<Vector3> CalculatePositions()
        {
            return _layoutType switch
            {
                HandLayoutType.Fan => CalculateFanPositions(),
                HandLayoutType.Line => CalculateLinePositions(),
                HandLayoutType.Stack => CalculateStackPositions(),
                HandLayoutType.Grid => CalculateGridPositions(),
                _ => CalculateLinePositions()
            };
        }

        private List<Quaternion> CalculateRotations()
        {
            return _layoutType switch
            {
                HandLayoutType.Fan => CalculateFanRotations(),
                _ => CalculateNoRotations()
            };
        }

        private List<Vector3> CalculateFanPositions()
        {
            var positions = new List<Vector3>();
            int count = _cards.Count;
            if (count == 0) return positions;

            float totalAngle = Mathf.Min(_arcAngle * (count - 1), 60f);
            float startAngle = totalAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle - (totalAngle / Mathf.Max(1, count - 1)) * i;
                if (count == 1) angle = 0;

                float radians = angle * Mathf.Deg2Rad;
                float x = Mathf.Sin(radians) * _arcRadius;
                float y = Mathf.Cos(radians) * _arcRadius - _arcRadius;

                positions.Add(new Vector3(x, y, 0));
            }

            return positions;
        }

        private List<Quaternion> CalculateFanRotations()
        {
            var rotations = new List<Quaternion>();
            int count = _cards.Count;
            if (count == 0) return rotations;

            float totalAngle = Mathf.Min(_arcAngle * (count - 1), 60f);
            float startAngle = totalAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle - (totalAngle / Mathf.Max(1, count - 1)) * i;
                if (count == 1) angle = 0;

                rotations.Add(Quaternion.Euler(0, 0, angle));
            }

            return rotations;
        }

        private List<Vector3> CalculateLinePositions()
        {
            var positions = new List<Vector3>();
            int count = _cards.Count;

            float totalWidth = (count - 1) * _spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(startX + i * _spacing, 0, 0));
            }

            return positions;
        }

        private List<Vector3> CalculateStackPositions()
        {
            var positions = new List<Vector3>();
            int count = _cards.Count;

            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(i * 2f, i * 2f, 0));
            }

            return positions;
        }

        private List<Vector3> CalculateGridPositions()
        {
            var positions = new List<Vector3>();
            int count = _cards.Count;

            int rows = Mathf.CeilToInt((float)count / _gridColumns);
            float totalHeight = (rows - 1) * _gridRowSpacing;

            for (int i = 0; i < count; i++)
            {
                int row = i / _gridColumns;
                int col = i % _gridColumns;

                int itemsInRow = Mathf.Min(_gridColumns, count - row * _gridColumns);
                float rowWidth = (itemsInRow - 1) * _spacing;

                float x = -rowWidth / 2f + col * _spacing;
                float y = totalHeight / 2f - row * _gridRowSpacing;

                positions.Add(new Vector3(x, y, 0));
            }

            return positions;
        }

        private List<Quaternion> CalculateNoRotations()
        {
            var rotations = new List<Quaternion>();
            for (int i = 0; i < _cards.Count; i++)
            {
                rotations.Add(Quaternion.identity);
            }
            return rotations;
        }

        private async UniTask AnimateCard(CardComponent card, Vector3 position, Quaternion rotation)
        {
            var moveTween = card.transform.DOLocalMove(position, _arrangeDuration).SetEase(_arrangeEase);
            var rotateTween = card.transform.DOLocalRotateQuaternion(rotation, _arrangeDuration).SetEase(_arrangeEase);

            await UniTask.WaitUntil(() => !moveTween.IsActive() && !rotateTween.IsActive());
        }
    }
}

