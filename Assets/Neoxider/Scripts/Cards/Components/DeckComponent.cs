using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Cards
{
    /// <summary>
    ///     Deck component: initialize, shuffle, draw cards. Inspector setup; events via UnityEvent.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/DeckComponent")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(DeckComponent))]
    [NeoDoc("Cards/DeckComponent.md")]
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

        [Tooltip("If enabled, deck config becomes global fallback for other Card components.")] [SerializeField]
        private bool _setAnimationConfigAsGlobal = true;

        [Header("Trump Display")] [SerializeField]
        private bool _showTrumpCard = true;

        [SerializeField] private CardComponent _trumpCardDisplay;

        /// <summary>
        ///     Invoked after the deck is initialized.
        /// </summary>
        public UnityEvent OnInitialized;

        /// <summary>
        ///     Invoked after the deck model is shuffled.
        /// </summary>
        public UnityEvent OnShuffled;

        /// <summary>
        ///     Invoked when the deck model has no cards left.
        /// </summary>
        public UnityEvent OnDeckEmpty;

        /// <summary>
        ///     Invoked when a card is drawn via DrawCard/DrawCardAsync.
        /// </summary>
        public UnityEvent<CardComponent> OnCardDrawn;

        /// <summary>
        ///     Invoked when the visual stack changes (add/remove/shuffle).
        /// </summary>
        public UnityEvent OnVisualStackChanged;

        /// <summary>
        ///     Invoked after the visual stack is built successfully.
        /// </summary>
        public UnityEvent OnVisualStackBuilt;

        /// <summary>
        ///     Invoked when visual shuffle starts.
        /// </summary>
        public UnityEvent<ShuffleVisualType> OnShuffleVisualStarted;

        /// <summary>
        ///     Invoked when visual shuffle completes.
        /// </summary>
        public UnityEvent OnShuffleVisualCompleted;

        /// <summary>
        ///     Invoked when a card is dealt to a hand.
        /// </summary>
        public UnityEvent<CardComponent, HandComponent> OnCardDealt;

        private readonly List<CardComponent> _activeCards = new();

        private CancellationToken _ct;
        private CancellationTokenSource _sourceCancellation;
        private int _sourceGeneration;

        /// <summary>
        ///     Deck model.
        /// </summary>
        public DeckModel Model { get; private set; }

        /// <summary>
        ///     Remaining card count.
        /// </summary>
        public int RemainingCount => Model?.RemainingCount ?? 0;

        /// <summary>
        ///     Whether the deck is empty.
        /// </summary>
        public bool IsEmpty => Model?.IsEmpty ?? true;

        /// <summary>
        ///     Trump card (bottom of deck in this setup).
        /// </summary>
        public CardData? TrumpCard => Model?.PeekBottom();

        /// <summary>
        ///     Trump suit.
        /// </summary>
        public Suit? TrumpSuit => TrumpCard?.Suit;

        /// <summary>
        ///     Spawn point for drawn cards.
        /// </summary>
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        /// <summary>
        ///     Deck configuration (sprites, deck type, etc.).
        /// </summary>
        public DeckConfig Config => _config;

        /// <summary>
        ///     Card prefab used when drawing.
        /// </summary>
        public CardComponent CardPrefab => _cardPrefab;

        /// <summary>
        ///     Animation config assigned to this deck.
        /// </summary>
        public CardAnimationConfig AnimationConfig => _animationConfig;

        /// <summary>
        ///     Resets the deck.
        /// </summary>
        [Button]
        public void Reset()
        {
            InvalidateDeals();
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

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();
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
            _sourceGeneration++;
            _sourceCancellation?.Cancel();
            _sourceCancellation?.Dispose();
            _sourceCancellation = null;
            if (Model != null)
            {
                Model.OnDeckEmpty -= HandleDeckEmpty;
            }
        }

        /// <summary>
        ///     Initializes the deck.
        /// </summary>
        [Button]
        public void Initialize()
        {
            if (_config == null)
            {
                return;
            }

            InvalidateDeals();
            if (Model != null)
            {
                Model.OnDeckEmpty -= HandleDeckEmpty;
            }

            Model = new DeckModel();
            Model.OnDeckEmpty += HandleDeckEmpty;
            Model.Initialize(_config.GenerateDeck(), _shuffleOnStart);

            UpdateTrumpDisplay();

            OnInitialized?.Invoke();

            if (_spawnVisualOnInitialize)
            {
                BuildVisualStackAsync().SuppressCancellationThrow().Forget();
            }
        }

        private void InvalidateDeals()
        {
            _sourceGeneration++;
            _sourceCancellation?.Cancel();
            _sourceCancellation?.Dispose();
            _sourceCancellation = CancellationTokenSource.CreateLinkedTokenSource(_ct);
        }

        private void UpdateTrumpDisplay()
        {
            if (_trumpCardDisplay == null)
            {
                return;
            }

            CardData? trump = TrumpCard;
            bool visible = _showTrumpCard && trump.HasValue;
            if (visible)
            {
                _trumpCardDisplay.SetData(trump.Value);
            }

            _trumpCardDisplay.gameObject.SetActive(visible);
        }

        /// <summary>
        ///     Shuffles the deck model.
        /// </summary>
        [Button]
        public void Shuffle()
        {
            Model?.Shuffle();
            OnShuffled?.Invoke();
        }

        /// <summary>
        ///     Synchronous inspector button wrapper for building the visual stack.
        /// </summary>
        [Button("Build Visual Stack")]
        public void BuildVisualStack()
        {
            BuildVisualStackAsync().SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Builds the visual card stack on the assigned BoardComponent.
        ///     Cards come from the current DeckModel order without reordering.
        /// </summary>
        public async UniTask BuildVisualStackAsync()
        {
            if (_visualStackBoard == null || _cardPrefab == null || Model == null)
            {
                return;
            }

            int generation = _sourceGeneration;
            DeckModel sourceModel = Model;
            BoardComponent sourceBoard = _visualStackBoard;
            sourceBoard.Clear();
            if (!IsVisualStackSourceCurrent(generation, sourceModel, sourceBoard)) return;
            int modelRevision = sourceModel.Revision;
            CardData[] sourceCards = new List<CardData>(sourceModel.Cards).ToArray();
            bool IsCurrent() => IsVisualStackSourceCurrent(generation, sourceModel, sourceBoard) &&
                                sourceModel.Revision == modelRevision;
            _activeCards.RemoveAll(card => card == null);

            foreach (CardData cardData in sourceCards)
            {
                CardComponent card = InstantiateCardAtSpawnPoint();
                if (!IsCurrent() || card == null)
                {
                    if (card != null) Destroy(card.gameObject);
                    return;
                }
                card.Config = _config;
                card.SetData(cardData, _stackFaceUp);
                if (!IsCurrent() || card == null)
                {
                    if (card != null) Destroy(card.gameObject);
                    return;
                }
                card.SetDeckCleanupOwner(this);
                _activeCards.Add(card);
                await sourceBoard.PlaceCardAsync(card, false, false);
                if (!IsCurrent()) return;
            }

            ApplyVisualLayout(sourceBoard.Cards);
            if (!IsCurrent()) return;
            OnVisualStackBuilt?.Invoke();
            if (!IsCurrent()) return;
            OnVisualStackChanged?.Invoke();
        }

        private bool IsVisualStackSourceCurrent(int generation, DeckModel sourceModel, BoardComponent sourceBoard)
        {
            return this != null && generation == _sourceGeneration && ReferenceEquals(Model, sourceModel) &&
                   sourceBoard != null && ReferenceEquals(_visualStackBoard, sourceBoard);
        }

        /// <summary>
        ///     Synchronous inspector button wrapper for visual shuffle.
        /// </summary>
        [Button("Shuffle Visual")]
        public void ShuffleVisual(ShuffleVisualType type = ShuffleVisualType.Shake)
        {
            ShuffleVisualAsync(type).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Shuffles the deck model and syncs the visual stack.
        ///     Optionally plays a visual shuffle effect.
        /// </summary>
        /// <param name="type">Visual shuffle effect type.</param>
        /// <param name="duration">Optional duration; if null, uses config defaults.</param>
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
                        await PlayShakeAnimation(duration ??
                                                 (_animationConfig != null ? _animationConfig.ShakeDuration : 1f));
                        break;
                    case ShuffleVisualType.Cut:
                        await UniTask.Delay(
                            (int)((duration ?? (_animationConfig != null ? _animationConfig.CutDuration : 0.8f)) *
                                  1000f), cancellationToken: _ct);
                        break;
                    case ShuffleVisualType.Riffle:
                        await UniTask.Delay((int)((duration ??
                                                   (_animationConfig != null
                                                       ? _animationConfig.RiffleDuration
                                                       : 1.2f)) * 1000f), cancellationToken: _ct);
                        break;
                }
            }

            OnShuffleVisualCompleted?.Invoke();
            OnVisualStackChanged?.Invoke();
        }

        /// <summary>
        ///     Synchronous inspector button wrapper for dealing the top card to a hand.
        /// </summary>
        [Button("Deal To Hand")]
        public void DealToHand(HandComponent hand, bool faceUp = true)
        {
            DealToHandAsync(hand, faceUp).SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Deals the top card from the visual stack (or directly from the model) into the hand.
        /// </summary>
        /// <param name="hand">Target hand.</param>
        /// <param name="faceUp">Show face up.</param>
        /// <param name="moveDuration">Optional fly duration.</param>
        /// <returns>Dealt card, or null if dealing failed.</returns>
        public async UniTask<CardComponent> DealToHandAsync(HandComponent hand, bool faceUp, float? moveDuration = null)
        {
            if (hand == null || hand.IsFull || Model == null || Model.IsEmpty)
            {
                return null;
            }

            DeckModel source = Model;
            int generation = _sourceGeneration;
            CancellationToken sourceToken = _sourceCancellation?.Token ?? _ct;
            BoardComponent stack = _visualStackBoard;
            bool fromStack = stack != null && stack.Count > 0;
            CardComponent card = null;
            CardData? drawnData = null;
            bool accepted = false;
            try
            {
                if (fromStack)
                {
                    card = stack.Cards[stack.Count - 1];
                    stack.RemoveCard(card);
                    drawnData = source.Draw();
                    if (card != null)
                    {
                        card.IsFaceUp = faceUp;
                    }
                }
                else
                {
                    card = DrawCard(faceUp);
                    if (card != null)
                    {
                        drawnData = card.Data;
                    }
                }

                if (card == null || generation != _sourceGeneration || !ReferenceEquals(Model, source))
                {
                    return null;
                }

                float duration = moveDuration ?? (_animationConfig != null ? _animationConfig.DealMoveDuration : 0f);
                if (duration > 0f)
                {
                    await card.MoveToAsync(hand.transform.position, duration, sourceToken);
                }

                sourceToken.ThrowIfCancellationRequested();
                if (hand == null || generation != _sourceGeneration || !ReferenceEquals(Model, source))
                {
                    return null;
                }

                // A draw observer may already have admitted this exact card to the requested hand.
                accepted = hand.Cards.Contains(card);
                if (!accepted)
                {
                    // Admission is synchronous before this task's first animation await.
                    UniTask<bool> admission = hand.TryAddCardAsync(card);
                    accepted = hand.Cards.Contains(card);
                    accepted = await admission;
                }

                if (!accepted)
                {
                    return null;
                }

                if (generation != _sourceGeneration || sourceToken.IsCancellationRequested)
                {
                    return null;
                }

                OnCardDealt?.Invoke(card, hand);
                if (generation == _sourceGeneration)
                {
                    OnVisualStackChanged?.Invoke();
                }

                return card;
            }
            catch (OperationCanceledException) when (sourceToken.IsCancellationRequested)
            {
                return null;
            }
            finally
            {
                // Hand admission releases cleanup ownership before any model/component observer can reset us.
                bool transferred = card != null && !card.IsDeckCleanupOwnedBy(this);
                if (!accepted && !transferred)
                {
                    bool currentSource = this != null && generation == _sourceGeneration && ReferenceEquals(Model, source);
                    if (currentSource && drawnData.HasValue)
                    {
                        source.ReturnToTop(drawnData.Value);
                    }

                    bool restoredVisual = false;
                    if (currentSource && generation == _sourceGeneration && fromStack && stack != null &&
                        ReferenceEquals(_visualStackBoard, stack) && stack.CanPlaceCard(card))
                    {
                        card.IsFaceUp = _stackFaceUp;
                        await stack.PlaceCardAsync(card, false, false);
                        ApplyVisualLayout(stack.Cards);
                        restoredVisual = true;
                    }

                    if (!restoredVisual && card != null)
                    {
                        _activeCards.Remove(card);
                        Destroy(card.gameObject);
                    }

                    if (currentSource && generation == _sourceGeneration)
                    {
                        UpdateTrumpDisplay();
                        OnVisualStackChanged?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        ///     Draws a card from the deck.
        /// </summary>
        /// <param name="faceUp">Show face up.</param>
        /// <returns>Card component, or null.</returns>
        public CardComponent DrawCard(bool faceUp = true)
        {
            if (Model == null || _cardPrefab == null)
            {
                return null;
            }

            int generation = _sourceGeneration;
            CardData? cardData = Model.Draw();
            if (!cardData.HasValue || generation != _sourceGeneration)
            {
                return null;
            }

            CardComponent card = InstantiateCardAtSpawnPoint();
            if (this == null || card == null || generation != _sourceGeneration)
            {
                if (card != null) Destroy(card.gameObject);
                return null;
            }

            card.Config = _config;
            card.SetData(cardData.Value, faceUp);
            if (this == null || card == null || generation != _sourceGeneration)
            {
                if (card != null) Destroy(card.gameObject);
                return null;
            }

            card.SetDeckCleanupOwner(this);
            _activeCards.Add(card);
            OnCardDrawn?.Invoke(card);

            return card;
        }

        private CardComponent InstantiateCardAtSpawnPoint()
        {
            Transform spawn = SpawnPoint;
            if (_cardPrefab.transform is RectTransform && spawn.GetComponentInParent<Canvas>(true) != null)
            {
                CardComponent card = Instantiate(_cardPrefab, spawn, false);
                if (card != null)
                {
                    card.transform.localPosition = Vector3.zero;
                    card.UpdateOriginalTransform();
                }
                return card;
            }

            return Instantiate(_cardPrefab, spawn.position, Quaternion.identity);
        }

        internal void ReleaseCardCleanup(CardComponent card)
        {
            _activeCards.Remove(card);
        }

        /// <summary>
        ///     Draws a card from the deck with move animation.
        /// </summary>
        /// <param name="targetPosition">Target position.</param>
        /// <param name="faceUp">Show face up.</param>
        /// <param name="duration">Move duration.</param>
        /// <returns>Card component.</returns>
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
        ///     Draws multiple cards.
        /// </summary>
        /// <param name="count">Number of cards.</param>
        /// <param name="faceUp">Show face up.</param>
        /// <returns>List of cards.</returns>
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
        ///     Returns a card to the deck.
        /// </summary>
        /// <param name="card">Card to return.</param>
        /// <param name="toTop">Place on top if true, bottom if false.</param>
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
            settings.PositionJitter =
                _animationConfig != null ? _animationConfig.StackPositionJitter : _stackPositionJitter;
            settings.RotationJitter =
                _animationConfig != null ? _animationConfig.StackRotationJitter : _stackRotationJitter;

            List<Vector3> positions = CardLayoutCalculator.CalculatePositions(_visualLayoutType, cards.Count, settings);
            List<Quaternion> rotations =
                CardLayoutCalculator.CalculateRotations(_visualLayoutType, cards.Count, settings);

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
            List<Vector3> basePositions =
                cards.Select(card => card != null ? card.transform.localPosition : Vector3.zero).ToList();

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

                await UniTask.Delay((int)(duration * 1000f / frames), cancellationToken: _ct);
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
