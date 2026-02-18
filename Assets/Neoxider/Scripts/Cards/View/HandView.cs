using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Визуальное представление руки игрока
    /// </summary>
    [NeoDoc("Cards/View/HandView.md")]
    public class HandView : MonoBehaviour, IHandView
    {
        [Header("Layout")] [SerializeField] private CardLayoutType _layoutType = CardLayoutType.Fan;

        [SerializeField] private float _spacing = 60f;
        [SerializeField] private float _arcAngle = 30f;
        [SerializeField] private float _arcRadius = 400f;

        [Header("Grid Settings")] [SerializeField]
        private int _gridColumns = 5;

        [SerializeField] private float _gridRowSpacing = 80f;

        [Header("Animation")] [SerializeField] private float _arrangeDuration = 0.3f;

        [SerializeField] private Ease _arrangeEase = Ease.OutQuad;

        private readonly List<ICardView> _cardViews = new();

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

        /// <summary>
        ///     Расстояние между картами
        /// </summary>
        public float Spacing
        {
            get => _spacing;
            set => _spacing = value;
        }

        /// <inheritdoc />
        public IReadOnlyList<ICardView> CardViews => _cardViews;

        /// <inheritdoc />
        public int Count => _cardViews.Count;

        /// <inheritdoc />
        public async UniTask AddCardAsync(ICardView cardView, bool animate = true)
        {
            if (cardView == null)
            {
                return;
            }

            _cardViews.Add(cardView);
            cardView.Transform.SetParent(transform, true);

            await ArrangeCardsAsync(animate);
        }

        /// <inheritdoc />
        public async UniTask RemoveCardAsync(ICardView cardView, bool animate = true)
        {
            if (cardView == null)
            {
                return;
            }

            _cardViews.Remove(cardView);
            await ArrangeCardsAsync(animate);
        }

        /// <inheritdoc />
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            if (_cardViews.Count == 0)
            {
                return;
            }

            List<Vector3> positions = CalculatePositions();
            List<Quaternion> rotations = CalculateRotations();

            List<UniTask> tasks = new();

            for (int i = 0; i < _cardViews.Count; i++)
            {
                ICardView cardView = _cardViews[i];
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
            return CardLayoutCalculator.CalculatePositions(_layoutType, _cardViews.Count, BuildLayoutSettings());
        }

        private List<Quaternion> CalculateRotations()
        {
            return CardLayoutCalculator.CalculateRotations(_layoutType, _cardViews.Count, BuildLayoutSettings());
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

        private async UniTask AnimateCard(ICardView cardView, Vector3 position, Quaternion rotation)
        {
            TweenerCore<Vector3, Vector3, VectorOptions> moveTween =
                cardView.Transform.DOLocalMove(position, _arrangeDuration).SetEase(_arrangeEase);
            TweenerCore<Quaternion, Quaternion, NoOptions> rotateTween = cardView.Transform
                .DOLocalRotateQuaternion(rotation, _arrangeDuration).SetEase(_arrangeEase);

            await UniTask.WaitUntil(() => !moveTween.IsActive() && !rotateTween.IsActive());
        }
    }
}