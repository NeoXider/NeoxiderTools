using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    ///     Shared table board (e.g. five community cards in Hold'em or a discard pile in Durak).
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
            "Local override. If empty, Board falls back to the assigned DeckComponent or global animation config.")]
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

        private CancellationToken _ct;
        private CancellationTokenSource _layoutCancellation;
        private int _layoutRevision;
        private int _clearGeneration;
        private int _nextMembershipVersion;
        private readonly Dictionary<CardComponent, int> _membershipVersions = new();

        // WHY: every layout pass bumps _layoutRevision before refilling these, and a pass re-checks the revision
        // before each read, so an older animated pass never reads entries a newer pass overwrote.
        private readonly List<CardComponent> _layoutMembers = new();
        private readonly List<Vector3> _layoutPositions = new();
        private readonly List<Quaternion> _layoutRotations = new();
        private List<PendingPlacement> _sparePlacements;
        private List<CardComponent> _spareRemovals;

        /// <summary>
        ///     Cards on this board.
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        ///     Card count.
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Whether the board has no cards.
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Whether the board reached capacity.
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        ///     Maximum cards this board accepts before reporting full.
        /// </summary>
        public int MaxCards => _maxCards;

        /// <summary>
        ///     Face-up state applied to newly placed cards when overrideFaceUp is true.
        /// </summary>
        public bool FaceUp => _faceUp;

        /// <summary>
        ///     Slot transforms for slot layout mode.
        /// </summary>
        public Transform[] CardSlots => _cardSlots;

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();

            if (_autoGenerateSlots && (_cardSlots == null || _cardSlots.Length == 0))
            {
                GenerateSlots();
            }
        }

        private void OnDestroy()
        {
            InvalidateLayout();
        }

        /// <summary>
        ///     Places a card on the board.
        /// </summary>
        /// <param name="card">Card to place.</param>
        /// <param name="animate">Tween into slot/layout.</param>
        /// <param name="overrideFaceUp">If true, apply this component's FaceUp flag.</param>
        public async UniTask PlaceCardAsync(CardComponent card, bool animate = true, bool overrideFaceUp = true)
        {
            if (!CanPlaceCard(card))
            {
                return;
            }

            InvalidateLayout();
            _cards.Add(card);
            int membershipVersion = ++_nextMembershipVersion;
            _membershipVersions[card] = membershipVersion;

            // WHY: membership is committed before parenting so native hierarchy observers (OnTransformParentChanged,
            // OnTransformChildrenChanged) can clear, release or move this exact admission.
            card.transform.SetParent(transform, true);
            if (!IsCurrentMember(card, membershipVersion))
            {
                AbandonEscapedAdmission(card, membershipVersion);
                return;
            }

            if (overrideFaceUp)
            {
                card.IsFaceUp = _faceUp;
                if (!IsCurrentMember(card, membershipVersion))
                {
                    AbandonEscapedAdmission(card, membershipVersion);
                    return;
                }
            }

            await ArrangeCardsInternalAsync(animate);

            if (IsCurrentMember(card, membershipVersion))
            {
                OnCardPlaced?.Invoke(card);
                if (IsCurrentMember(card, membershipVersion) && IsFull)
                {
                    OnBoardFull?.Invoke();
                }
            }
            else
            {
                // WHY: an animated arrange spans frames; a card reparented or destroyed meanwhile without the
                // board API must not stay listed and count toward capacity.
                AbandonEscapedAdmission(card, membershipVersion);
            }
        }

        /// <summary>
        ///     Places without awaiting (no tween).
        /// </summary>
        /// <param name="card">Card.</param>
        public void PlaceCard(CardComponent card)
        {
            PlaceCardAsync(card, false).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Places several cards with one immediate (untweened) layout pass instead of one pass per card.
        /// </summary>
        /// <remarks>
        ///     Cards are admitted in list order until the board is full; null entries, duplicates and cards already on
        ///     the board are skipped. After the single layout pass <see cref="OnCardPlaced" /> is raised for every
        ///     admitted card that still belongs to the board, in admission order, followed by one
        ///     <see cref="OnBoardFull" /> when the board is full. A clear issued from a hierarchy or face callback stops
        ///     the batch without a layout pass; a clear from an <see cref="OnCardPlaced" /> listener stops the remaining
        ///     events, including <see cref="OnBoardFull" />. A card that leaves the board without the board API is
        ///     dropped as in <see cref="PlaceCardAsync" /> and frees its slot for the next listed card. Placing n cards
        ///     costs one layout pass (O(n) transform writes) instead of n passes (O(n^2)); a warm call does not
        ///     allocate.
        /// </remarks>
        /// <param name="cards">Cards to place.</param>
        /// <param name="overrideFaceUp">If true, apply <see cref="FaceUp" /> to each placed card.</param>
        /// <returns>Number of cards placed and published through <see cref="OnCardPlaced" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cards" /> is null.</exception>
        public int PlaceCardsImmediate(IReadOnlyList<CardComponent> cards, bool overrideFaceUp = true)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            List<PendingPlacement> pending = RentPlacements();
            try
            {
                int clearGeneration = _clearGeneration;
                bool attempted = false;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (clearGeneration != _clearGeneration || IsFull)
                    {
                        break;
                    }

                    CardComponent card = cards[i];
                    if (card == null || _cards.Contains(card))
                    {
                        continue;
                    }

                    attempted = true;
                    InvalidateLayout();
                    _cards.Add(card);
                    int membershipVersion = ++_nextMembershipVersion;
                    _membershipVersions[card] = membershipVersion;

                    // WHY: same admission order as PlaceCardAsync, so native hierarchy observers can release this
                    // exact admission during SetParent.
                    card.transform.SetParent(transform, true);
                    if (!IsCurrentMember(card, membershipVersion))
                    {
                        ReleaseEscapedAdmission(card, membershipVersion);
                        continue;
                    }

                    if (overrideFaceUp)
                    {
                        card.IsFaceUp = _faceUp;
                        if (!IsCurrentMember(card, membershipVersion))
                        {
                            ReleaseEscapedAdmission(card, membershipVersion);
                            continue;
                        }
                    }

                    pending.Add(new PendingPlacement(card, membershipVersion));
                }

                if (!attempted || clearGeneration != _clearGeneration)
                {
                    // WHY: a clear from a callback hands the board to its new owner; arranging here would cancel the
                    // owner's own layout.
                    return 0;
                }

                ArrangeCardsInternalAsync(false).SuppressCancellationThrow().Forget();

                int published = 0;
                for (int i = 0; i < pending.Count; i++)
                {
                    if (clearGeneration != _clearGeneration)
                    {
                        break;
                    }

                    PendingPlacement placement = pending[i];
                    if (IsCurrentMember(placement.Card, placement.Version))
                    {
                        published++;
                        OnCardPlaced?.Invoke(placement.Card);
                    }
                    else
                    {
                        AbandonEscapedAdmission(placement.Card, placement.Version);
                    }
                }

                // WHY: a placement listener that cleared (and maybe refilled) the board superseded this batch; the
                // refill publishes its own full event.
                if (published > 0 && clearGeneration == _clearGeneration && IsFull)
                {
                    OnBoardFull?.Invoke();
                }

                return published;
            }
            finally
            {
                ReturnPlacements(pending);
            }
        }

        /// <summary>
        ///     Returns whether the board can accept this card right now.
        /// </summary>
        /// <param name="card">Card to place.</param>
        public bool CanPlaceCard(CardComponent card)
        {
            return card != null && !IsFull && !_cards.Contains(card);
        }

        /// <summary>
        ///     Updates board capacity for custom layouts/games.
        /// </summary>
        /// <param name="maxCards">New maximum card count.</param>
        /// <param name="regenerateSlots">Regenerate auto slots immediately.</param>
        public void SetCapacity(int maxCards, bool regenerateSlots = true)
        {
            InvalidateLayout();
            _maxCards = Mathf.Max(0, maxCards);

            if (regenerateSlots && _autoGenerateSlots)
            {
                GenerateSlots();
            }

            ArrangeCardsInternalAsync(false).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Updates default face state for newly placed cards.
        /// </summary>
        /// <param name="faceUp">Face-up state.</param>
        /// <param name="applyToExisting">Apply to cards already on the board.</param>
        public void SetFaceUp(bool faceUp, bool applyToExisting = false)
        {
            _faceUp = faceUp;

            if (!applyToExisting)
            {
                return;
            }

            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    card.IsFaceUp = faceUp;
                }
            }
        }

        /// <summary>
        ///     Re-runs layout for every card on the board.
        /// </summary>
        [Button("Arrange Cards")]
        public void ArrangeCards()
        {
            ArrangeCardsInternalAsync(true).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Grows capacity once so every incoming card fits (restart helpers), regenerating auto slots one time
        ///     instead of once per card.
        /// </summary>
        /// <param name="incoming">Cards about to be placed.</param>
        private void ExpandCapacityFor(List<CardComponent> incoming)
        {
            if (!_autoExpandCapacity)
            {
                return;
            }

            int required = _cards.Count;
            for (int i = 0; i < incoming.Count; i++)
            {
                if (!_cards.Contains(incoming[i]))
                {
                    required++;
                }
            }

            if (required <= _maxCards)
            {
                return;
            }

            _maxCards = required;
            if (_autoGenerateSlots)
            {
                GenerateSlots();
            }
        }

        /// <summary>
        ///     Marks a card spawned during initial board setup.
        /// </summary>
        /// <param name="card">Card.</param>
        public void RegisterInitialCard(CardComponent card)
        {
            if (card == null)
            {
                return;
            }

            // WHY: the debug list outlives restarts; dropping destroyed entries keeps it (and this scan) bounded
            // instead of growing by a whole deck on every restart.
            bool registered = false;
            int write = 0;
            for (int read = 0; read < _initialSpawnedCards.Count; read++)
            {
                CardComponent entry = _initialSpawnedCards[read];
                if (entry == null)
                {
                    continue;
                }

                registered |= ReferenceEquals(entry, card);
                _initialSpawnedCards[write++] = entry;
            }

            _initialSpawnedCards.RemoveRange(write, _initialSpawnedCards.Count - write);
            if (!registered)
            {
                _initialSpawnedCards.Add(card);
            }
        }

        /// <summary>
        ///     Places multiple cards sequentially.
        /// </summary>
        /// <param name="cards">Cards.</param>
        /// <param name="animate">Tween each.</param>
        /// <param name="delayBetweenCards">Delay seconds between placements.</param>
        public async UniTask PlaceCardsAsync(IEnumerable<CardComponent> cards, bool animate = true,
            float delayBetweenCards = 0.1f)
        {
            int generation = _clearGeneration;
            List<CardComponent> snapshot = new(cards);
            foreach (CardComponent card in snapshot)
            {
                if (IsFull)
                {
                    break;
                }

                await PlaceCardAsync(card, animate);
                if (generation != _clearGeneration)
                {
                    return;
                }

                if (delayBetweenCards > 0)
                {
                    await UniTask.Delay((int)(delayBetweenCards * 1000), cancellationToken: _ct);
                    if (generation != _clearGeneration)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///     Removes a specific card.
        /// </summary>
        /// <param name="card">Card.</param>
        /// <returns>True if it was on the board.</returns>
        public bool RemoveCard(CardComponent card)
        {
            if (card == null)
            {
                return false;
            }

            bool removed = _cards.Remove(card);
            if (removed)
            {
                InvalidateLayout();
                _membershipVersions.Remove(card);
                card.CancelAnimation();
                card.DetachFromZone();
                ArrangeCardsInternalAsync(false).SuppressCancellationThrow().Forget();
            }

            return removed;
        }

        /// <summary>
        ///     Removes by index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Removed card, or null.</returns>
        public CardComponent RemoveAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return null;
            }

            CardComponent card = _cards[index];
            RemoveCard(card);
            return card;
        }

        /// <summary>
        ///     Removes several cards with one immediate layout pass for the remaining members instead of one pass per
        ///     card.
        /// </summary>
        /// <remarks>
        ///     Every listed card that is on the board is released exactly as <see cref="RemoveCard" /> releases it
        ///     (motion cancelled, detached by the zone rule, no event); null entries and non-members are skipped. The list
        ///     is copied first, so it may be <see cref="Cards" /> itself. A clear issued from a hierarchy callback stops
        ///     the batch and skips the final layout, because the board then belongs to the clearing caller. Removing n
        ///     cards costs one layout pass instead of n; a warm call does not allocate.
        /// </remarks>
        /// <param name="cards">Cards to remove.</param>
        /// <returns>Number of cards removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cards" /> is null.</exception>
        public int RemoveCards(IReadOnlyList<CardComponent> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            List<CardComponent> requested = RentRemovals();
            try
            {
                // WHY: hierarchy callbacks during detaching may edit the input (it can be Cards itself); a private
                // copy keeps the iteration stable. List.AddRange would copy through a temporary array.
                for (int i = 0; i < cards.Count; i++)
                {
                    requested.Add(cards[i]);
                }

                int clearGeneration = _clearGeneration;
                int removed = 0;
                for (int i = 0; i < requested.Count; i++)
                {
                    if (clearGeneration != _clearGeneration)
                    {
                        return removed;
                    }

                    CardComponent card = requested[i];
                    if (card == null || !_cards.Remove(card))
                    {
                        continue;
                    }

                    InvalidateLayout();
                    _membershipVersions.Remove(card);
                    card.CancelAnimation();
                    card.DetachFromZone();
                    removed++;
                }

                if (removed > 0 && clearGeneration == _clearGeneration)
                {
                    ArrangeCardsInternalAsync(false).SuppressCancellationThrow().Forget();
                }

                return removed;
            }
            finally
            {
                ReturnRemovals(requested);
            }
        }

        /// <summary>
        ///     Destroys every card on the board.
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
                    card.CancelAnimation();
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            OnBoardCleared?.Invoke();
        }

        /// <summary>
        ///     Detaches and returns cards without destroying them.
        /// </summary>
        /// <returns>Former board cards.</returns>
        public List<CardComponent> ClearAndReturn()
        {
            InvalidateLayout();
            _clearGeneration++;
            _membershipVersions.Clear();
            List<CardComponent> cards = new(_cards);

            foreach (CardComponent card in cards)
            {
                if (card != null)
                {
                    card.CancelAnimation();
                    card.DetachFromZone();
                }
            }

            _cards.Clear();
            OnBoardCleared?.Invoke();

            return cards;
        }

        /// <summary>
        ///     Flips every card instantly.
        /// </summary>
        [Button]
        public void FlipAll()
        {
            FlipAllAsync().SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Pulls cards from configured hands, other boards, and extra roots back onto this board.
        /// </summary>
        [Button]
        public void RestoreAllSourcesToBoard()
        {
            RestoreAllSourcesToBoardAsync().SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Async variant of <see cref="RestoreAllSourcesToBoard" />.
        /// </summary>
        /// <remarks>
        ///     Collected cards are placed immediately with one layout pass (see <see cref="PlaceCardsImmediate" />);
        ///     when Auto Expand Capacity is on, capacity grows once to fit them all, so <see cref="OnBoardFull" /> is
        ///     raised at most once. The returned task is already complete.
        /// </remarks>
        public UniTask RestoreAllSourcesToBoardAsync()
        {
            try
            {
                RestoreAllSources();
                return UniTask.CompletedTask;
            }
            catch (Exception exception)
            {
                return UniTask.FromException(exception);
            }
        }

        private void RestoreAllSources()
        {
            List<CardComponent> collected = new();
            HashSet<CardComponent> seen = new();

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

            ExpandCapacityFor(collected);
            PlaceCardsImmediate(collected);

            _lastCollectedCards.Clear();
            _lastCollectedCards.AddRange(collected);

            OnBoardCleared?.Invoke();
        }

        /// <summary>
        ///     Flips every card with tween spacing.
        /// </summary>
        /// <param name="delayBetweenCards">Seconds between flips.</param>
        public async UniTask FlipAllAsync(float delayBetweenCards = 0.1f)
        {
            int revision = _layoutRevision;
            CardComponent[] cards = _cards.ToArray();
            foreach (CardComponent card in cards)
            {
                if (revision != _layoutRevision || card == null || card.transform.parent != transform)
                {
                    return;
                }

                await card.FlipAsync();
                if (revision != _layoutRevision)
                {
                    return;
                }

                if (delayBetweenCards > 0)
                {
                    await UniTask.Delay((int)(delayBetweenCards * 1000), cancellationToken: _ct);
                }
            }
        }

        /// <summary>
        ///     Distinct non-joker ranks currently on the board.
        /// </summary>
        /// <remarks>Missing or destroyed entries are skipped.</remarks>
        /// <returns>Rank set.</returns>
        public HashSet<Rank> GetAllRanks()
        {
            HashSet<Rank> ranks = new();

            foreach (CardComponent card in _cards)
            {
                if (card != null && !card.Data.IsJoker)
                {
                    ranks.Add(card.Data.Rank);
                }
            }

            return ranks;
        }

        /// <summary>
        ///     Copies <see cref="CardData" /> for each board card.
        /// </summary>
        /// <remarks>Missing or destroyed entries are skipped.</remarks>
        /// <returns>Data list.</returns>
        public List<CardData> GetAllCardData()
        {
            List<CardData> data = new(_cards.Count);

            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    data.Add(card.Data);
                }
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
            InvalidateLayout();
            if (_cards.Count == 0)
            {
                // WHY: an emptied board must not keep the previous pass's released cards alive in the reused buffer.
                _layoutMembers.Clear();
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

            int revision = _layoutRevision;
            List<CardComponent> cards = _layoutMembers;
            cards.Clear();
            // WHY: List.AddRange copies through a temporary array in Unity's class library.
            for (int i = 0; i < _cards.Count; i++)
            {
                cards.Add(_cards[i]);
            }

            int count = cards.Count;
            bool slots = ShouldUseSlotPlacement();
            CardLayoutType layoutType = ResolveLayoutType();
            List<Vector3> positions = _layoutPositions;
            List<Quaternion> rotations = _layoutRotations;
            CardLayoutCalculator.CalculatePositions(layoutType, count, settings, positions);
            CardLayoutCalculator.CalculateRotations(layoutType, count, settings, rotations);
            // WHY: an immediate pass never awaits, so the layout revision alone retires it; only an animated pass
            // needs a linked source (and its registration on the destroy token) to cancel in-flight motion.
            using CancellationTokenSource layout = animate ? CancellationTokenSource.CreateLinkedTokenSource(_ct) : null;
            _layoutCancellation = layout;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (revision != _layoutRevision || (layout != null && layout.IsCancellationRequested))
                    {
                        return;
                    }

                    CardComponent card = cards[i];
                    if (card == null || card.transform.parent != transform)
                    {
                        continue;
                    }

                    int sibling = _stackZSorting == StackZSortingStrategy.TopCardFirst ? i : count - 1 - i;
                    card.transform.SetSiblingIndex(sibling);
                    if (revision != _layoutRevision || card == null || card.transform.parent != transform)
                    {
                        return;
                    }

                    Transform slot = slots ? GetSlot(i) : null;
                    if (slots && slot == null)
                    {
                        continue;
                    }

                    Vector3 worldPosition = slots ? slot.position : transform.TransformPoint(positions[i]);
                    if (animate)
                    {
                        await card.MoveToAsync(worldPosition, _placeDuration, layout.Token);
                    }
                    else
                    {
                        card.transform.position = worldPosition;
                    }

                    if (revision != _layoutRevision || card == null || card.transform.parent != transform)
                    {
                        return;
                    }

                    if (!slots)
                    {
                        card.transform.localRotation = rotations[i];
                    }

                    card.UpdateOriginalTransform();
                }
            }
            catch (OperationCanceledException) when (!_ct.IsCancellationRequested)
            {
                // WHY: membership/layout changed; the replacement pass owns the current cards.
            }
            finally
            {
                if (ReferenceEquals(_layoutCancellation, layout))
                {
                    _layoutCancellation = null;
                }
            }
        }

        private bool IsCurrentMember(CardComponent card, int version)
        {
            return card != null && card.transform.parent == transform &&
                   _membershipVersions.TryGetValue(card, out int current) && current == version;
        }

        /// <summary>
        ///     Drops an admission whose card left the board without the board API before its placement completed:
        ///     a hierarchy or face callback reparented it or destroyed it with DestroyImmediate, or other code
        ///     reparented or destroyed it while an animated arrange was still running.
        /// </summary>
        /// <remarks>
        ///     Releases through Clear, ClearAndReturn or RemoveCard already removed the version and are left alone,
        ///     as is a newer admission of the same card. The card keeps whatever parent and pose it was given.
        ///     A deferred Object.Destroy issued during an immediate placement lands after the placement completed;
        ///     that card stays a member until RemoveCard or Clear.
        /// </remarks>
        private void AbandonEscapedAdmission(CardComponent card, int version)
        {
            if (ReleaseEscapedAdmission(card, version))
            {
                // WHY: the escaped card leaves a gap, and this admission cancelled the previous layout pass;
                // re-arrange the remaining members so they do not stay wherever the cancelled motion left them.
                ArrangeCardsInternalAsync(false).SuppressCancellationThrow().Forget();
            }
        }

        /// <summary>
        ///     Removes the membership of an escaped admission without re-arranging (see
        ///     <see cref="AbandonEscapedAdmission" />).
        /// </summary>
        /// <returns>True when this exact admission was still recorded and has been removed.</returns>
        private bool ReleaseEscapedAdmission(CardComponent card, int version)
        {
            if (!_membershipVersions.TryGetValue(card, out int current) || current != version)
            {
                return false;
            }

            _membershipVersions.Remove(card);
            _cards.Remove(card);
            return true;
        }

        private List<PendingPlacement> RentPlacements()
        {
            // WHY: a hierarchy or event callback may start another batch on this board; it gets its own list.
            List<PendingPlacement> placements = _sparePlacements ?? new List<PendingPlacement>();
            _sparePlacements = null;
            return placements;
        }

        private void ReturnPlacements(List<PendingPlacement> placements)
        {
            placements.Clear();
            _sparePlacements = placements;
        }

        private List<CardComponent> RentRemovals()
        {
            // WHY: a hierarchy callback may start another batch removal on this board; it gets its own list.
            List<CardComponent> removals = _spareRemovals ?? new List<CardComponent>();
            _spareRemovals = null;
            return removals;
        }

        private void ReturnRemovals(List<CardComponent> removals)
        {
            // WHY: cleared so the spare list never keeps released cards alive.
            removals.Clear();
            _spareRemovals = removals;
        }

        private void InvalidateLayout()
        {
            _layoutRevision++;
            CancellationTokenSource previous = _layoutCancellation;
            _layoutCancellation = null;
            previous?.Cancel();
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
            // WHY: SetCapacity and the restore helper regenerate slots at runtime; old auto-generated
            // slot objects must be destroyed or they accumulate under the board.
            if (_cardSlots != null)
            {
                foreach (Transform slot in _cardSlots)
                {
                    if (slot != null && slot != transform && slot.parent == transform)
                    {
                        Destroy(slot.gameObject);
                    }
                }
            }

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

        private readonly struct PendingPlacement
        {
            public readonly CardComponent Card;
            public readonly int Version;

            public PendingPlacement(CardComponent card, int version)
            {
                Card = card;
                Version = version;
            }
        }
    }
}
