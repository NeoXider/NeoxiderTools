using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Cards
{
    /// <summary>
    ///     Hand component: layouts (fan, line, etc.), add/remove. Inspector setup; events via UnityEvent.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/HandComponent")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(HandComponent))]
    [NeoDoc("Cards/HandComponent.md")]
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

        [Header("Card Order")] [Tooltip("If true, new cards are added at bottom (sibling index 0)")] [SerializeField]
        private bool _addToBottom;

        [Header("Animation")] [SerializeField] private float _arrangeDuration = 0.3f;

        [SerializeField] private Ease _arrangeEase = Ease.OutQuad;

        [SerializeField] private UnityEvent<int> _onCardCountChanged = new();

        [SerializeField] private UnityEvent<CardComponent> _onCardAdded = new();
        [SerializeField] private UnityEvent<CardComponent> _onCardRemoved = new();
        [SerializeField] private UnityEvent<CardComponent> _onCardClicked = new();
        [SerializeField] private UnityEvent _onHandChanged = new();

        private readonly List<CardComponent> _cards = new();
        private readonly Dictionary<CardComponent, UnityAction> _cardClickHandlers = new();

        private CancellationToken _ct;
        private CancellationTokenSource _layoutCancellation;
        private int _layoutRevision;
        private int _nextMembershipVersion;
        private int _clearGeneration;
        private readonly Dictionary<CardComponent, int> _membershipVersions = new();

        /// <summary>
        ///     Invoked when the card count changes; carries the new count.
        /// </summary>
        public UnityEvent<int> OnCardCountChanged => _onCardCountChanged;

        /// <summary>
        ///     Invoked when a card is added.
        /// </summary>
        public UnityEvent<CardComponent> OnCardAdded => _onCardAdded;

        /// <summary>
        ///     Invoked when a card is removed.
        /// </summary>
        public UnityEvent<CardComponent> OnCardRemoved => _onCardRemoved;

        /// <summary>
        ///     Invoked when a card is clicked.
        /// </summary>
        public UnityEvent<CardComponent> OnCardClicked => _onCardClicked;

        /// <summary>
        ///     Invoked on any hand mutation.
        /// </summary>
        public UnityEvent OnHandChanged => _onHandChanged;

        /// <summary>
        ///     Backing hand model.
        /// </summary>
        public HandModel Model { get; private set; }

        /// <summary>
        ///     Cards currently parented to this hand.
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        ///     Card count.
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Whether the hand has no cards.
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Whether the hand reached <c>_maxCards</c>.
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        ///     Maximum visible cards this hand accepts before reporting full.
        /// </summary>
        public int MaxCards => _maxCards;

        /// <summary>
        ///     Active layout type.
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
        ///     Legacy layout enum for old scenes. Prefer <see cref="LayoutType" />.
        /// </summary>
        [Obsolete("Use LayoutType with CardLayoutType.")]
        public HandLayoutType LegacyLayoutType
        {
            get => (HandLayoutType)(int)_layoutType;
            set
            {
                _layoutType = (CardLayoutType)(int)value;
                ArrangeCardsAsync().SuppressCancellationThrow().Forget();
            }
        }

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();
            EnsureModelInitialized();
        }

        private void OnDestroy()
        {
            _clearGeneration++;
            InvalidateLayout();
            UnsubscribeAllCardClickHandlers();
        }

        private void EnsureModelInitialized()
        {
            if (Model == null)
            {
                Model = new HandModel();
            }

            // WHY: keep the data model's capacity in sync with the inspector limit so
            // Model.IsFull/CanAdd agree with the visual component.
            Model.Capacity = Mathf.Max(0, _maxCards);
        }

        /// <summary>
        ///     Adds a card and optionally animates layout.
        /// </summary>
        /// <param name="card">Card to add.</param>
        /// <param name="animate">Tween to slot.</param>
        public async UniTask AddCardAsync(CardComponent card, bool animate = true)
        {
            await TryAddCardAsync(card, animate);
        }

        /// <summary>
        ///     Attempts admission before animating. False leaves the hand and card unchanged.
        ///     True records admission even if a later caller removes the card during animation.
        /// </summary>
        public UniTask<bool> TryAddCardAsync(CardComponent card, bool animate = true)
        {
            return TryAddCardAsync(card, animate, default);
        }

        /// <summary>
        ///     Cancellable admission/layout. Cancellation stops motion; it does not undo accepted membership.
        /// </summary>
        public async UniTask<bool> TryAddCardAsync(CardComponent card, bool animate, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (card == null || IsFull || _cards.Contains(card))
            {
                return false;
            }

            EnsureModelInitialized();
            InvalidateLayout();
            int membershipVersion = ++_nextMembershipVersion;
            _membershipVersions[card] = membershipVersion;

            SubscribeCardClickHandler(card);
            Action onAccepted = () =>
            {
                // Reparenting can invoke native hierarchy observers synchronously.
                // They must see committed membership and no remaining deck cleanup ownership.
                card.ReleaseDeckCleanupOwnership();
                card.transform.SetParent(transform, true);
            };

            if (_addToBottom)
            {
                _cards.Insert(0, card);
                // WHY: model order must mirror the visual list, otherwise RemoveCardAsync
                // removes the wrong model entry by index.
                Model.Insert(0, card.Data, onAccepted);
            }
            else
            {
                _cards.Add(card);
                Model.Add(card.Data, onAccepted);
            }

            await ArrangeCardsAsync(animate, cancellation);

            if (card != null && card.transform.parent == transform &&
                _membershipVersions.TryGetValue(card, out int currentVersion) && currentVersion == membershipVersion)
            {
                _onCardAdded?.Invoke(card);
                cancellation.ThrowIfCancellationRequested();
                if (!_membershipVersions.TryGetValue(card, out currentVersion) || currentVersion != membershipVersion)
                {
                    return true;
                }

                _onCardCountChanged?.Invoke(_cards.Count);
                cancellation.ThrowIfCancellationRequested();
                if (_membershipVersions.TryGetValue(card, out currentVersion) && currentVersion == membershipVersion)
                {
                    _onHandChanged?.Invoke();
                }
            }

            return true;
        }

        /// <summary>
        ///     Adds a card without waiting (fires async with animate=false).
        /// </summary>
        /// <param name="card">Card.</param>
        public void AddCard(CardComponent card)
        {
            AddCardAsync(card, false).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Draws the first card (War-style).
        /// </summary>
        /// <returns>First card, or null.</returns>
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
        ///     Draws a random card.
        /// </summary>
        /// <returns>Random card, or null.</returns>
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
        ///     Removes a card from the hand.
        /// </summary>
        /// <param name="card">Card to remove.</param>
        /// <param name="animate">Re-layout with tween.</param>
        public async UniTask RemoveCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || !_cards.Contains(card))
            {
                return;
            }

            EnsureModelInitialized();
            int index = _cards.IndexOf(card);
            int clearGeneration = _clearGeneration;
            InvalidateLayout();
            _membershipVersions.Remove(card);
            card.CancelAnimation();

            UnsubscribeCardClickHandler(card);
            card.ResetHover();
            card.transform.SetParent(null, true);

            _cards.RemoveAt(index);
            Model.RemoveAt(index);

            await ArrangeCardsAsync(animate);

            if (clearGeneration != _clearGeneration || _cards.Contains(card))
            {
                return;
            }

            _onCardRemoved?.Invoke(card);
            if (clearGeneration != _clearGeneration)
            {
                return;
            }

            _onCardCountChanged?.Invoke(_cards.Count);
            if (clearGeneration == _clearGeneration)
            {
                _onHandChanged?.Invoke();
            }
        }

        /// <summary>
        ///     Removes without awaiting (animate=false).
        /// </summary>
        /// <param name="card">Card.</param>
        public void RemoveCard(CardComponent card)
        {
            RemoveCardAsync(card, false).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Removes the card at <paramref name="index" />.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="animate">Re-layout with tween.</param>
        /// <returns>Removed card, or null.</returns>
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
        ///     Sorts by rank (inspector button).
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        [Button]
        public void SortByRank(bool ascending = true)
        {
            SortByRankAsync(ascending).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Sorts by rank and re-layouts.
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
        ///     Sorts by suit (inspector button).
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        [Button]
        public void SortBySuit(bool ascending = true)
        {
            SortBySuitAsync(ascending).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Sorts by suit, then rank, with layout tween.
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
        ///     Cards that can beat <paramref name="attackCard" /> under Durak rules.
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>Matching components.</returns>
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
        ///     Destroys all cards and clears the model.
        /// </summary>
        [Button]
        public void Clear()
        {
            InvalidateLayout();
            _clearGeneration++;
            _membershipVersions.Clear();
            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    UnsubscribeCardClickHandler(card);
                    card.CancelAnimation();
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
        ///     Recomputes layout positions for every card.
        /// </summary>
        /// <param name="animate">Tween to new slots.</param>
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            await ArrangeCardsAsync(animate, default);
        }

        private async UniTask ArrangeCardsAsync(bool animate, CancellationToken cancellation)
        {
            InvalidateLayout();
            if (_cards.Count == 0)
            {
                return;
            }

            int revision = _layoutRevision;
            CardComponent[] cards = _cards.ToArray();
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

                    CardComponent card = cards[i];
                    if (card == null || card.transform.parent != transform)
                    {
                        continue;
                    }

                    card.transform.SetSiblingIndex(_addToBottom ? cards.Length - 1 - i : i);
                    if (revision != _layoutRevision || card == null || card.transform.parent != transform)
                    {
                        break;
                    }

                    if (animate)
                    {
                        tasks.Add(AnimateCard(card, positions[i], rotations[i], layout.Token));
                    }
                    else
                    {
                        card.transform.localPosition = positions[i];
                        card.transform.localRotation = rotations[i];
                        card.UpdateOriginalTransform();
                    }
                }

                if (tasks.Count > 0)
                {
                    await UniTask.WhenAll(tasks);
                }

                if (revision != _layoutRevision || layout.IsCancellationRequested)
                {
                    return;
                }

                foreach (CardComponent card in cards)
                {
                    if (card != null && card.transform.parent == transform)
                    {
                        card.UpdateOriginalTransform();
                    }
                }
            }
            catch (OperationCanceledException) when (!cancellation.IsCancellationRequested && !_ct.IsCancellationRequested)
            {
                // A newer layout owns the current members. Its cancellation is not an admission failure.
            }
            finally
            {
                if (ReferenceEquals(_layoutCancellation, layout))
                {
                    _layoutCancellation = null;
                }
            }
        }

        private void InvalidateLayout()
        {
            _layoutRevision++;
            CancellationTokenSource previous = _layoutCancellation;
            _layoutCancellation = null;
            previous?.Cancel();
        }

        private void HandleCardClick(CardComponent card)
        {
            _onCardClicked?.Invoke(card);
        }

        private void SubscribeCardClickHandler(CardComponent card)
        {
            if (card == null || card.OnClick == null)
            {
                return;
            }

            UnsubscribeCardClickHandler(card);
            UnityAction handler = () => HandleCardClick(card);
            _cardClickHandlers[card] = handler;
            card.OnClick.AddListener(handler);
        }

        private void UnsubscribeCardClickHandler(CardComponent card)
        {
            if (card == null)
            {
                return;
            }

            if (_cardClickHandlers.TryGetValue(card, out UnityAction handler))
            {
                card.OnClick?.RemoveListener(handler);
                _cardClickHandlers.Remove(card);
            }
        }

        private void UnsubscribeAllCardClickHandlers()
        {
            foreach (KeyValuePair<CardComponent, UnityAction> pair in _cardClickHandlers)
            {
                if (pair.Key != null)
                {
                    pair.Key.OnClick?.RemoveListener(pair.Value);
                }
            }

            _cardClickHandlers.Clear();
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
            ArrangeCardsAsync().SuppressCancellationThrow().Forget();
        }

        private async UniTask AnimateCard(CardComponent card, Vector3 position, Quaternion rotation,
            CancellationToken cancellation)
        {
            TweenerCore<Vector3, Vector3, VectorOptions> moveTween =
                card.transform.DOLocalMove(position, _arrangeDuration)
                    .SetEase(_arrangeEase)
                    .SetRecyclable(false)
                    .SetTarget(card.transform)
                    .SetLink(card.gameObject);
            TweenerCore<Quaternion, Quaternion, NoOptions> rotateTween =
                card.transform.DOLocalRotateQuaternion(rotation, _arrangeDuration)
                    .SetEase(_arrangeEase)
                    .SetRecyclable(false)
                    .SetTarget(card.transform)
                    .SetLink(card.gameObject);

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
