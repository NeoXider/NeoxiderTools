using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Hand view: lays out <see cref="ICardView" /> children.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/HandView")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(HandView))]
    [NeoDoc("Cards/View/HandView.md")]
    public class HandView : MonoBehaviour, IOrderedHandView
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

        private CancellationToken _ct;
        private CancellationTokenSource _layoutCancellation;
        private int _layoutRevision;

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();
        }

        private void OnDestroy()
        {
            InvalidateLayout();
        }

        /// <summary>
        ///     Layout algorithm.
        /// </summary>
        public CardLayoutType LayoutType
        {
            get => _layoutType;
            set
            {
                _layoutType = value;
                ArrangeCardsAsync().SuppressCancellationThrow().Forget();
            }
        }

        /// <summary>
        ///     Spacing between cards (line/grid/fan).
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
            if (cardView == null || _cardViews.Contains(cardView))
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

            if (!_cardViews.Remove(cardView))
            {
                return;
            }

            await ArrangeCardsAsync(animate);
        }

        /// <inheritdoc />
        public async UniTask SetOrderAsync(IReadOnlyList<ICardView> order, bool animate = true,
            CancellationToken cancellation = default)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            cancellation.ThrowIfCancellationRequested();
            List<ICardView> snapshot = new(order);
            if (snapshot.Count != _cardViews.Count)
            {
                throw new ArgumentException("Order must contain exactly the current hand members.", nameof(order));
            }

            HashSet<ICardView> remaining = new(_cardViews);
            foreach (ICardView card in snapshot)
            {
                if (card == null || !remaining.Remove(card))
                {
                    throw new ArgumentException("Order must be a permutation without duplicate members.", nameof(order));
                }
            }

            _cardViews.Clear();
            _cardViews.AddRange(snapshot);
            await ArrangeCardsAsync(animate, cancellation);
        }

        /// <inheritdoc />
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            await ArrangeCardsAsync(animate, default);
        }

        private async UniTask ArrangeCardsAsync(bool animate, CancellationToken cancellation)
        {
            InvalidateLayout();
            if (_cardViews.Count == 0)
            {
                return;
            }

            int revision = _layoutRevision;
            ICardView[] cards = _cardViews.ToArray();
            List<Vector3> positions = CalculatePositions();
            List<Quaternion> rotations = CalculateRotations();
            using CancellationTokenSource layout = CancellationTokenSource.CreateLinkedTokenSource(_ct, cancellation);
            _layoutCancellation = layout;
            try
            {
                List<UniTask> tasks = new(cards.Length);
                for (int i = 0; i < cards.Length; i++)
                {
                    if (revision != _layoutRevision || layout.IsCancellationRequested)
                    {
                        break;
                    }

                    ICardView cardView = cards[i];
                    Transform cardTransform = cardView?.Transform;
                    if (revision != _layoutRevision || cardTransform == null || cardTransform.parent != transform)
                    {
                        continue;
                    }

                    cardTransform.SetSiblingIndex(i);
                    if (revision != _layoutRevision || cardTransform == null || cardTransform.parent != transform)
                    {
                        break;
                    }

                    if (animate)
                    {
                        tasks.Add(AnimateCard(cardTransform, positions[i], rotations[i], layout.Token));
                    }
                    else
                    {
                        cardTransform.localPosition = positions[i];
                        cardTransform.localRotation = rotations[i];
                    }
                }

                if (tasks.Count > 0)
                {
                    await UniTask.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested && !_ct.IsCancellationRequested)
            {
                // A later membership/order change owns the replacement layout.
            }
            finally
            {
                if (ReferenceEquals(_layoutCancellation, layout))
                {
                    _layoutCancellation = null;
                }
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            InvalidateLayout();
            _cardViews.Clear();
        }

        private void InvalidateLayout()
        {
            _layoutRevision++;
            CancellationTokenSource previous = _layoutCancellation;
            _layoutCancellation = null;
            previous?.Cancel();
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

        private async UniTask AnimateCard(Transform cardTransform, Vector3 position, Quaternion rotation,
            CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            TweenerCore<Vector3, Vector3, VectorOptions> moveTween =
                cardTransform.DOLocalMove(position, _arrangeDuration)
                    .SetEase(_arrangeEase)
                    .SetRecyclable(false)
                    .SetTarget(cardTransform)
                    .SetLink(cardTransform.gameObject);
            TweenerCore<Quaternion, Quaternion, NoOptions> rotateTween = cardTransform
                .DOLocalRotateQuaternion(rotation, _arrangeDuration)
                .SetEase(_arrangeEase)
                .SetRecyclable(false)
                .SetTarget(cardTransform)
                .SetLink(cardTransform.gameObject);

            using CancellationTokenRegistration registration = cancellation.Register(() =>
            {
                moveTween.Kill();
                rotateTween.Kill();
            });
            try
            {
                await UniTask.WaitUntil(() => !moveTween.IsActive() && !rotateTween.IsActive(),
                    cancellationToken: cancellation);
            }
            finally
            {
                moveTween.Kill();
                rotateTween.Kill();
            }
        }
    }
}
