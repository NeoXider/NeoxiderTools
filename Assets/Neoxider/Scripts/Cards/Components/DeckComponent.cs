using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

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
        ///     Initializes the deck.
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
            BuildVisualStackAsync().Forget();
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
        ///     Synchronous inspector button wrapper for visual shuffle.
        /// </summary>
        [Button("Shuffle Visual")]
        public void ShuffleVisual(ShuffleVisualType type = ShuffleVisualType.Shake)
        {
            ShuffleVisualAsync(type).Forget();
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
                                  1000f));
                        break;
                    case ShuffleVisualType.Riffle:
                        await UniTask.Delay((int)((duration ??
                                                   (_animationConfig != null
                                                       ? _animationConfig.RiffleDuration
                                                       : 1.2f)) * 1000f));
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
            DealToHandAsync(hand, faceUp).Forget();
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

            await hand.AddCardAsync(card);
            OnCardDealt?.Invoke(card, hand);
            OnVisualStackChanged?.Invoke();
            return card;
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

            var cards = _visualStackBoard.Cards.ToList();
            var basePositions =
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
