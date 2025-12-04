using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    /// Визуальное представление руки игрока
    /// </summary>
    public class HandView : MonoBehaviour, IHandView
    {
        [Header("Layout")]
        [SerializeField] private HandLayoutType _layoutType = HandLayoutType.Fan;
        [SerializeField] private float _spacing = 60f;
        [SerializeField] private float _arcAngle = 30f;
        [SerializeField] private float _arcRadius = 400f;

        [Header("Grid Settings")]
        [SerializeField] private int _gridColumns = 5;
        [SerializeField] private float _gridRowSpacing = 80f;

        [Header("Animation")]
        [SerializeField] private float _arrangeDuration = 0.3f;
        [SerializeField] private Ease _arrangeEase = Ease.OutQuad;

        private readonly List<ICardView> _cardViews = new();

        /// <inheritdoc />
        public IReadOnlyList<ICardView> CardViews => _cardViews;

        /// <inheritdoc />
        public int Count => _cardViews.Count;

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

        /// <summary>
        /// Расстояние между картами
        /// </summary>
        public float Spacing
        {
            get => _spacing;
            set => _spacing = value;
        }

        /// <inheritdoc />
        public async UniTask AddCardAsync(ICardView cardView, bool animate = true)
        {
            if (cardView == null) return;

            _cardViews.Add(cardView);
            cardView.Transform.SetParent(transform, true);

            await ArrangeCardsAsync(animate);
        }

        /// <inheritdoc />
        public async UniTask RemoveCardAsync(ICardView cardView, bool animate = true)
        {
            if (cardView == null) return;

            _cardViews.Remove(cardView);
            await ArrangeCardsAsync(animate);
        }

        /// <inheritdoc />
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            if (_cardViews.Count == 0) return;

            var positions = CalculatePositions();
            var rotations = CalculateRotations();

            var tasks = new List<UniTask>();

            for (int i = 0; i < _cardViews.Count; i++)
            {
                var cardView = _cardViews[i];
                cardView.Transform.SetSiblingIndex(i);

                if (animate)
                {
                    tasks.Add(AnimateCard(cardView, positions[i], rotations[i]));
                }
                else
                {
                    cardView.Transform.localPosition = positions[i];
                    cardView.Transform.localRotation = rotations[i];
                }
            }

            if (animate && tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            _cardViews.Clear();
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
            int count = _cardViews.Count;

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
            int count = _cardViews.Count;

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
            int count = _cardViews.Count;

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
            int count = _cardViews.Count;

            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(i * 2f, i * 2f, 0));
            }

            return positions;
        }

        private List<Vector3> CalculateGridPositions()
        {
            var positions = new List<Vector3>();
            int count = _cardViews.Count;

            int rows = Mathf.CeilToInt((float)count / _gridColumns);
            float totalWidth = (Mathf.Min(count, _gridColumns) - 1) * _spacing;
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
            for (int i = 0; i < _cardViews.Count; i++)
            {
                rotations.Add(Quaternion.identity);
            }
            return rotations;
        }

        private async UniTask AnimateCard(ICardView cardView, Vector3 position, Quaternion rotation)
        {
            var moveTween = cardView.Transform.DOLocalMove(position, _arrangeDuration).SetEase(_arrangeEase);
            var rotateTween = cardView.Transform.DOLocalRotateQuaternion(rotation, _arrangeDuration).SetEase(_arrangeEase);

            await UniTask.WaitUntil(() => !moveTween.IsActive() && !rotateTween.IsActive());
        }
    }
}

