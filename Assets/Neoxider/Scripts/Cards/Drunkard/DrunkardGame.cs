using System.Collections.Generic;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    ///     War card game sample: higher rank wins the trick; ties trigger "war" rounds.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/DrunkardGame")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(DrunkardGame))]
    [NeoDoc("Cards/Examples/Drunkard.md")]
    public class DrunkardGame : MonoBehaviour
    {
        [Header("Config")] [Tooltip("Required deck and card prefab source.")] [SerializeField]
        private DeckComponent _deckComponent;

        [SerializeField] private bool _initializeOnStart = true;
        [SerializeField] private bool _debug;
        [SerializeField] private bool _debugWarnings;

        [Header("Positions")] [SerializeField] private Transform _cardsParent;

        [Tooltip(
            "Optional board for the initial deal: cards spawn on the board first, then split evenly between sides.")]
        [SerializeField]
        private BoardComponent _initialBoard;

        [Tooltip("Player deck position (where card moves from). Can reference HandComponent.")] [SerializeField]
        private Transform _playerDeckPosition;

        [SerializeField] private Transform _playerCardPosition;
        [SerializeField] private Transform _opponentDeckPosition;
        [SerializeField] private Transform _opponentCardPosition;


        [Header("Timing")] [SerializeField] private float _cardMoveDuration = 0.3f;

        [SerializeField] private float _roundDelay = 1f;

        [Tooltip("Delay between player turns")] [SerializeField]
        private float _turnDelay = 0.3f;

        [SerializeField] private float _warContinueDelay = 0.5f;
        [SerializeField] private float _cardReturnDelay = 0.1f;

        [Header("Game Rules")] [Tooltip("If true - player moves first; if false - opponent")] [SerializeField]
        private bool _playerGoesFirst = true;

        [Header("Events - Card Count")] [SerializeField]
        private UnityEvent<int> _onPlayerCardCountChanged;

        [SerializeField] private UnityEvent<int> _onOpponentCardCountChanged;

        [Header("Events - Game Flow")] [SerializeField]
        private UnityEvent _onGameStarted;

        [SerializeField] private UnityEvent _onGameRestarted;
        [SerializeField] private UnityEvent _onPlayerWin;
        [SerializeField] private UnityEvent _onOpponentWin;

        [Header("Events - Round")] [SerializeField]
        private UnityEvent _onRoundStarted;

        [SerializeField] private UnityEvent _onRoundEnded;
        [SerializeField] private UnityEvent _onPlayerWonRound;
        [SerializeField] private UnityEvent _onOpponentWonRound;
        [SerializeField] private UnityEvent _onWarStarted;
        [SerializeField] private UnityEvent _onWarEnded;
        private readonly Queue<CardData> _opponentCards = new();

        private readonly Queue<CardData> _playerCards = new();
        private readonly List<CardComponent> _warCards = new();
        private HandComponent _cachedOpponentHand;

        private HandComponent _cachedPlayerHand;
        private bool _handsCached;
        private bool _isInitialized;
        private CardComponent _opponentCardView;
        private CardComponent _playerCardView;
        private CancellationToken _ct;
        private CancellationTokenSource _gameCancellation;
        private int _gameGeneration;
        private bool _isRestarting;

        private readonly struct GameOperation
        {
            internal GameOperation(int generation, CancellationToken token)
            {
                Generation = generation;
                Token = token;
            }

            internal int Generation { get; }
            internal CancellationToken Token { get; }
        }

        private void Awake()
        {
            _ct = this.GetCancellationTokenOnDestroy();
            _gameCancellation = CancellationTokenSource.CreateLinkedTokenSource(_ct);
            CacheHands();
        }

        private void OnDestroy()
        {
            _gameGeneration++;
            _gameCancellation?.Cancel();
            _gameCancellation?.Dispose();
            _gameCancellation = null;
            CleanupRoundViews();
        }

        private GameOperation BeginNewGame()
        {
            _gameGeneration++;
            _gameCancellation?.Cancel();
            _gameCancellation?.Dispose();
            _gameCancellation = CancellationTokenSource.CreateLinkedTokenSource(_ct);
            return new GameOperation(_gameGeneration, _gameCancellation.Token);
        }

        private bool IsCurrent(GameOperation operation)
        {
            return this != null && operation.Generation == _gameGeneration && !operation.Token.IsCancellationRequested;
        }

        private void EnsureCurrent(GameOperation operation)
        {
            if (!IsCurrent(operation))
            {
                throw new OperationCanceledException(operation.Token);
            }
        }

        private void CleanupRoundViews()
        {
            HashSet<CardComponent> owned = new(_warCards);
            if (_playerCardView != null)
            {
                owned.Add(_playerCardView);
            }

            if (_opponentCardView != null)
            {
                owned.Add(_opponentCardView);
            }

            _warCards.Clear();
            _playerCardView = null;
            _opponentCardView = null;
            foreach (CardComponent card in owned)
            {
                if (card != null)
                {
                    card.CancelAnimation();
                    Destroy(card.gameObject);
                }
            }
        }

        private void ReleaseRoundView(CardComponent card)
        {
            _warCards.Remove(card);
            if (_playerCardView == card)
            {
                _playerCardView = null;
            }

            if (_opponentCardView == card)
            {
                _opponentCardView = null;
            }
        }

        private void Start()
        {
            ValidateSetup();

            if (_initializeOnStart)
            {
                RestartGame();
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            _handsCached = false;
        }

        private void CacheHands()
        {
            if (_handsCached)
            {
                return;
            }

            _handsCached = true;

            if (_playerDeckPosition != null)
            {
                _cachedPlayerHand = _playerDeckPosition.GetComponent<HandComponent>();
            }

            if (_opponentDeckPosition != null)
            {
                _cachedOpponentHand = _opponentDeckPosition.GetComponent<HandComponent>();
            }
        }

        private void ValidateSetup()
        {
            if (_deckComponent == null)
            {
                LogError("[DrunkardGame] DeckComponent is not assigned!");
                return;
            }

            if (_deckComponent.Config == null)
            {
                LogError("[DrunkardGame] DeckComponent has no DeckConfig assigned!");
            }

            if (_deckComponent.CardPrefab == null)
            {
                LogError("[DrunkardGame] DeckComponent has no CardPrefab assigned!");
            }

            if (!_debugWarnings)
            {
                return;
            }

            if (_cardsParent == null)
            {
                LogWarning("[DrunkardGame] CardsParent is not set - cards parent to DrunkardGame.");
            }

            if (_playerDeckPosition == null)
            {
                LogWarning("[DrunkardGame] PlayerDeckPosition is not assigned.");
            }

            if (_playerCardPosition == null)
            {
                LogWarning("[DrunkardGame] PlayerCardPosition is not assigned.");
            }

            if (_opponentDeckPosition == null)
            {
                LogWarning("[DrunkardGame] OpponentDeckPosition is not assigned.");
            }

            if (_opponentCardPosition == null)
            {
                LogWarning("[DrunkardGame] OpponentCardPosition is not assigned.");
            }
        }

        private void Log(string message)
        {
            NeoDiagnostics.Log(message, this, _debug);
        }

        private void LogWarning(string message)
        {
            NeoDiagnostics.LogWarning(message, this, _debugWarnings);
        }

        private void LogError(string message)
        {
            NeoDiagnostics.LogError(message, this);
        }

        /// <summary>
        ///     UI-friendly wrapper that starts <see cref="PlayRound" /> without awaiting.
        /// </summary>
        [Button]
        public void Play()
        {
            if (_debug)
            {
                Log("[DrunkardGame] Play() invoked.");
            }

            PlayRound().SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Plays one round (order depends on <c>_playerGoesFirst</c>).
        /// </summary>
        public async UniTask PlayRound()
        {
            if (IsPlaying || _isRestarting)
            {
                return;
            }

            GameOperation operation = new(_gameGeneration, _gameCancellation?.Token ?? _ct);
            if (PlayerCardCount == 0 || OpponentCardCount == 0)
            {
                EndGame(operation);
                return;
            }

            IsPlaying = true;
            try
            {
                EnsureCurrent(operation);
                if (!GameStarted)
                {
                    GameStarted = true;
                    _onGameStarted?.Invoke();
                    EnsureCurrent(operation);
                }

                _onRoundStarted?.Invoke();
                EnsureCurrent(operation);

                CardData opponentCard = DrawOpponentCard();
                EnsureCurrent(operation);
                CardData playerCard = DrawPlayerCard();
                EnsureCurrent(operation);

                if (_playerGoesFirst)
                {
                    await ShowPlayerCard(playerCard, operation);
                    EnsureCurrent(operation);
                    await UniTask.Delay((int)(_turnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);
                    await ShowOpponentCard(opponentCard, operation);
                }
                else
                {
                    await ShowOpponentCard(opponentCard, operation);
                    EnsureCurrent(operation);
                    await UniTask.Delay((int)(_turnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);
                    await ShowPlayerCard(playerCard, operation);
                }

                EnsureCurrent(operation);
                int comparison = playerCard.CompareTo(opponentCard);

                if (comparison != 0)
                {
                    await UniTask.Delay((int)(_cardReturnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);
                    await MoveCardsToWinnerAsync(comparison > 0, playerCard, opponentCard, operation);
                    EnsureCurrent(operation);
                    if (comparison > 0)
                    {
                        _onPlayerWonRound?.Invoke();
                    }
                    else
                    {
                        _onOpponentWonRound?.Invoke();
                    }
                }
                else
                {
                    _onWarStarted?.Invoke();
                    EnsureCurrent(operation);

                    _warCards.Clear();
                    if (_playerCardView != null)
                    {
                        _warCards.Add(_playerCardView);
                    }

                    if (_opponentCardView != null)
                    {
                        _warCards.Add(_opponentCardView);
                    }

                    await HandleWar(playerCard, opponentCard, operation);
                    EnsureCurrent(operation);
                    _onWarEnded?.Invoke();
                }

                EnsureCurrent(operation);
                await UniTask.Delay((int)(_roundDelay * 1000), cancellationToken: operation.Token);
                EnsureCurrent(operation);

                await HideCards(operation);
                EnsureCurrent(operation);
                NotifyCardCountChanged(operation);
                EnsureCurrent(operation);
                _onRoundEnded?.Invoke();
                EnsureCurrent(operation);
                CheckGameEnd(operation);
            }
            catch (OperationCanceledException) when (!IsCurrent(operation))
            {
                // Restart/destruction owns cleanup and the replacement game's flags.
            }
            finally
            {
                if (this != null && operation.Generation == _gameGeneration)
                {
                    IsPlaying = false;
                }
            }
        }

        /// <summary>
        ///     Resolves tied ranks by drawing extra face-down/up cards until someone wins.
        /// </summary>
        private async UniTask HandleWar(CardData card1, CardData card2, GameOperation operation)
        {
            List<CardData> warPile = new() { card1, card2 };

            while (true)
            {
                EnsureCurrent(operation);
                if (PlayerCardCount < 2 || OpponentCardCount < 2)
                {
                    for (int i = 0; i < warPile.Count; i++)
                    {
                        if (i % 2 == 0)
                        {
                            _playerCards.Enqueue(warPile[i]);
                        }
                        else
                        {
                            _opponentCards.Enqueue(warPile[i]);
                        }
                    }

                    return;
                }

                warPile.Add(DrawPlayerCard());
                EnsureCurrent(operation);
                warPile.Add(DrawOpponentCard());
                EnsureCurrent(operation);

                CardData opponentWarCard = DrawOpponentCard();
                EnsureCurrent(operation);
                CardData playerWarCard = DrawPlayerCard();
                EnsureCurrent(operation);
                warPile.Add(playerWarCard);
                warPile.Add(opponentWarCard);

                if (_playerGoesFirst)
                {
                    await ShowAdditionalPlayerCard(playerWarCard, operation);
                    EnsureCurrent(operation);
                    await UniTask.Delay((int)(_turnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);
                    await ShowAdditionalOpponentCard(opponentWarCard, operation);
                }
                else
                {
                    await ShowAdditionalOpponentCard(opponentWarCard, operation);
                    EnsureCurrent(operation);
                    await UniTask.Delay((int)(_turnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);
                    await ShowAdditionalPlayerCard(playerWarCard, operation);
                }

                EnsureCurrent(operation);
                int comparison = playerWarCard.CompareTo(opponentWarCard);

                if (comparison > 0)
                {
                    await MoveAllWarCardsToWinnerAsync(true, warPile, operation);
                    EnsureCurrent(operation);
                    _onPlayerWonRound?.Invoke();
                    return;
                }

                if (comparison < 0)
                {
                    await MoveAllWarCardsToWinnerAsync(false, warPile, operation);
                    EnsureCurrent(operation);
                    _onOpponentWonRound?.Invoke();
                    return;
                }

                await UniTask.Delay((int)(_warContinueDelay * 1000), cancellationToken: operation.Token);
            }
        }

        /// <summary>
        ///     Pops the next player card (hand mode or internal queue).
        /// </summary>
        private CardData DrawPlayerCard()
        {
            if (UsePlayerHand && PlayerHand.Count > 0)
            {
                CardComponent cardComponent = PlayerHand.DrawFirst();
                if (cardComponent != null)
                {
                    CardData data = cardComponent.Data;
                    Destroy(cardComponent.gameObject);
                    if (_debug)
                    {
                        Log($"[DrunkardGame] Drew player hand card: {data}");
                    }

                    return data;
                }
            }

            return _playerCards.Dequeue();
        }

        /// <summary>
        ///     Pops the next opponent card (hand mode or internal queue).
        /// </summary>
        private CardData DrawOpponentCard()
        {
            if (UseOpponentHand && OpponentHand.Count > 0)
            {
                CardComponent cardComponent = OpponentHand.DrawFirst();
                if (cardComponent != null)
                {
                    CardData data = cardComponent.Data;
                    Destroy(cardComponent.gameObject);
                    if (_debug)
                    {
                        Log($"[DrunkardGame] Drew opponent hand card: {data}");
                    }

                    return data;
                }
            }

            return _opponentCards.Dequeue();
        }

        /// <summary>
        ///     Awards every card in the war pile (and visuals) to the winner.
        /// </summary>
        private async UniTask MoveAllWarCardsToWinnerAsync(bool playerWins, List<CardData> warPile,
            GameOperation operation)
        {
            EnsureCurrent(operation);
            CardComponent[] visuals = _warCards.ToArray();
            // WHY: war pile can be larger than _warCards, so some entries have no matching CardComponent visual.
            List<CardData> hiddenCards = null;
            if (UsePlayerHand || UseOpponentHand)
            {
                Dictionary<CardData, int> visualUsage = new();
                foreach (CardComponent card in visuals)
                {
                    if (card == null)
                    {
                        continue;
                    }

                    if (!visualUsage.TryAdd(card.Data, 1))
                    {
                        visualUsage[card.Data]++;
                    }
                }

                foreach (CardData data in warPile)
                {
                    if (visualUsage.TryGetValue(data, out int remaining) && remaining > 0)
                    {
                        visualUsage[data] = remaining - 1;
                    }
                    else
                    {
                        hiddenCards ??= new List<CardData>();
                        hiddenCards.Add(data);
                    }
                }
            }

            HandComponent winnerHand = playerWins ? PlayerHand : OpponentHand;
            Queue<CardData> reserve = playerWins ? _playerCards : _opponentCards;
            if (winnerHand != null)
            {
                foreach (CardComponent card in visuals)
                {
                    EnsureCurrent(operation);
                    if (card != null)
                    {
                        ReleaseRoundView(card);
                        await AddToHandOrReserveAsync(winnerHand, reserve, card, operation);
                        EnsureCurrent(operation);
                        await UniTask.Delay((int)(_cardReturnDelay * 1000), cancellationToken: operation.Token);
                    }
                }

                EnsureCurrent(operation);
                if (hiddenCards is { Count: > 0 })
                {
                    foreach (CardData data in hiddenCards)
                    {
                        EnsureCurrent(operation);
                        CardComponent hiddenCardObj = SpawnCard(data, false, operation);
                        if (hiddenCardObj == null)
                        {
                            continue;
                        }

                        await AddToHandOrReserveAsync(winnerHand, reserve, hiddenCardObj, operation, false);
                        EnsureCurrent(operation);
                    }
                }
            }
            else
            {
                foreach (CardData data in warPile)
                {
                    reserve.Enqueue(data);
                }

                foreach (CardComponent card in visuals)
                {
                    ReleaseRoundView(card);
                    if (card != null)
                    {
                        Destroy(card.gameObject);
                    }
                }
            }

            EnsureCurrent(operation);
            _warCards.Clear();
            _playerCardView = null;
            _opponentCardView = null;
        }

        /// <summary>
        ///     Resolves a normal trick and enqueues captured data when not using hands.
        /// </summary>
        /// <param name="playerWins">True if the player won the trick.</param>
        /// <param name="playerCard">Player card data.</param>
        /// <param name="opponentCard">Opponent card data.</param>
        private async UniTask MoveCardsToWinnerAsync(bool playerWins, CardData playerCard, CardData opponentCard,
            GameOperation operation)
        {
            await MoveCardsToWinnerAsync(playerWins, operation);
            EnsureCurrent(operation);

            // WHY: hands are per-side, so only the winner's side decides how the trick is banked —
            // requiring both sides to be queue-driven would drop the cards in mixed hand/queue setups.
            if (playerWins)
            {
                if (!UsePlayerHand)
                {
                    _playerCards.Enqueue(playerCard);
                    _playerCards.Enqueue(opponentCard);
                }
            }
            else if (!UseOpponentHand)
            {
                _opponentCards.Enqueue(opponentCard);
                _opponentCards.Enqueue(playerCard);
            }
        }

        /// <summary>
        ///     Returns visible trick cards to the winner's hands when hand mode is enabled.
        /// </summary>
        /// <param name="playerWins">True if the player won.</param>
        private async UniTask MoveCardsToWinnerAsync(bool playerWins, GameOperation operation)
        {
            EnsureCurrent(operation);
            if (playerWins)
            {
                if (UsePlayerHand)
                {
                    if (_playerCardView != null)
                    {
                        CardComponent card = _playerCardView;
                        _playerCardView = null;
                        await AddToHandOrReserveAsync(PlayerHand, _playerCards, card, operation);
                        EnsureCurrent(operation);
                    }

                    await UniTask.Delay((int)(_cardReturnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);

                    if (_opponentCardView != null)
                    {
                        CardComponent card = _opponentCardView;
                        _opponentCardView = null;
                        await AddToHandOrReserveAsync(PlayerHand, _playerCards, card, operation);
                        EnsureCurrent(operation);
                    }
                }
            }
            else
            {
                if (UseOpponentHand)
                {
                    if (_opponentCardView != null)
                    {
                        CardComponent card = _opponentCardView;
                        _opponentCardView = null;
                        await AddToHandOrReserveAsync(OpponentHand, _opponentCards, card, operation);
                        EnsureCurrent(operation);
                    }

                    await UniTask.Delay((int)(_cardReturnDelay * 1000), cancellationToken: operation.Token);
                    EnsureCurrent(operation);

                    if (_playerCardView != null)
                    {
                        CardComponent card = _playerCardView;
                        _playerCardView = null;
                        await AddToHandOrReserveAsync(OpponentHand, _opponentCards, card, operation);
                        EnsureCurrent(operation);
                    }
                }
            }
        }

        /// <summary>
        ///     Keeps every banked card, using the data reserve when the scene hand cannot accept it.
        /// </summary>
        private async UniTask AddToHandOrReserveAsync(HandComponent hand, Queue<CardData> reserve,
            CardComponent card, GameOperation operation, bool animate = true)
        {
            EnsureCurrent(operation);
            if (card == null)
            {
                return;
            }

            if (hand == null || hand.IsFull)
            {
                reserve.Enqueue(card.Data);
                Destroy(card.gameObject);
                return;
            }

            bool accepted = await hand.TryAddCardAsync(card, animate, operation.Token);
            EnsureCurrent(operation);
            if (!accepted)
            {
                reserve.Enqueue(card.Data);
                Destroy(card.gameObject);
            }
            else if (card != null && card.transform.parent == hand.transform)
            {
                card.IsFaceUp = false;
            }
        }

        /// <summary>
        ///     Spawns/flips an extra player card during war.
        /// </summary>
        private async UniTask ShowAdditionalPlayerCard(CardData playerCard, GameOperation operation)
        {
            EnsureCurrent(operation);
            CardComponent cardObj = SpawnCard(playerCard, false, operation);
            if (cardObj == null)
            {
                return;
            }

            int warIndex = _warCards.Count / 2;
            _warCards.Add(cardObj);
            if (_playerDeckPosition != null)
            {
                cardObj.transform.position = _playerDeckPosition.position;
            }

            if (_playerCardPosition != null)
            {
                Vector3 offset = Vector3.right * warIndex * 30f;
                await cardObj.MoveToAsync(_playerCardPosition.position + offset, _cardMoveDuration, operation.Token);
                EnsureCurrent(operation);
            }

            await cardObj.FlipAsync(cardObj.FlipDuration, operation.Token);
            EnsureCurrent(operation);
        }

        /// <summary>
        ///     Spawns/flips an extra opponent card during war.
        /// </summary>
        private async UniTask ShowAdditionalOpponentCard(CardData opponentCard, GameOperation operation)
        {
            EnsureCurrent(operation);
            CardComponent cardObj = SpawnCard(opponentCard, false, operation);
            if (cardObj == null)
            {
                return;
            }

            int warIndex = _warCards.Count / 2;
            _warCards.Add(cardObj);
            if (_opponentDeckPosition != null)
            {
                cardObj.transform.position = _opponentDeckPosition.position;
            }

            if (_opponentCardPosition != null)
            {
                Vector3 offset = Vector3.right * warIndex * 30f;
                await cardObj.MoveToAsync(_opponentCardPosition.position + offset, _cardMoveDuration, operation.Token);
                EnsureCurrent(operation);
            }

            await cardObj.FlipAsync(cardObj.FlipDuration, operation.Token);
            EnsureCurrent(operation);
        }

        /// <summary>
        ///     Animates the opponent's played card to the table.
        /// </summary>
        private async UniTask ShowOpponentCard(CardData opponentCard, GameOperation operation)
        {
            EnsureCurrent(operation);
            if (_debug)
            {
                Log($"[DrunkardGame] ShowOpponentCard: {opponentCard}");
            }

            if (_opponentCardView == null)
            {
                if (_deckComponent == null || _deckComponent.CardPrefab == null || _deckComponent.Config == null)
                {
                    LogError("[DrunkardGame] CardPrefab/DeckConfig are not set up!");
                    return;
                }

                _opponentCardView = SpawnCard(opponentCard, false, operation);
                EnsureCurrent(operation);
                if (_opponentCardView == null)
                {
                    return;
                }
                if (_debug)
                {
                    Log(
                        $"[DrunkardGame] Spawned opponent card: {_opponentCardView.name} under {_cardsParent?.name}");
                }
            }

            CardComponent card = _opponentCardView;
            card.SetData(opponentCard, false);
            card.gameObject.SetActive(true);
            EnsureCurrent(operation);

            if (_opponentDeckPosition != null)
            {
                card.transform.position = _opponentDeckPosition.position;
            }

            if (_opponentCardPosition != null)
            {
                await card.MoveToAsync(_opponentCardPosition.position, _cardMoveDuration, operation.Token);
                EnsureCurrent(operation);
            }

            await card.FlipAsync(card.FlipDuration, operation.Token);
            EnsureCurrent(operation);
        }

        /// <summary>
        ///     Animates the player's played card to the table.
        /// </summary>
        private async UniTask ShowPlayerCard(CardData playerCard, GameOperation operation)
        {
            EnsureCurrent(operation);
            if (_playerCardView == null)
            {
                if (_deckComponent == null || _deckComponent.CardPrefab == null || _deckComponent.Config == null)
                {
                    LogError("[DrunkardGame] CardPrefab/DeckConfig are not set up!");
                    return;
                }

                _playerCardView = SpawnCard(playerCard, false, operation);
                EnsureCurrent(operation);
                if (_playerCardView == null)
                {
                    return;
                }
                if (_debug)
                {
                    Log($"[DrunkardGame] Spawned player card: {_playerCardView.name} under {_cardsParent?.name}");
                }
            }

            CardComponent card = _playerCardView;
            card.SetData(playerCard, false);
            card.gameObject.SetActive(true);
            EnsureCurrent(operation);

            if (_playerDeckPosition != null)
            {
                card.transform.position = _playerDeckPosition.position;
            }

            if (_playerCardPosition != null)
            {
                await card.MoveToAsync(_playerCardPosition.position, _cardMoveDuration, operation.Token);
                EnsureCurrent(operation);
            }

            await card.FlipAsync(card.FlipDuration, operation.Token);
            EnsureCurrent(operation);
        }

        /// <summary>
        ///     Destroys the active trick card views.
        /// </summary>
        private async UniTask HideCards(GameOperation operation)
        {
            EnsureCurrent(operation);
            CleanupRoundViews();
            await UniTask.Yield(operation.Token);
            EnsureCurrent(operation);
        }

        /// <summary>
        ///     Fires card-count UnityEvents (and optional debug logs).
        /// </summary>
        private void NotifyCardCountChanged(GameOperation operation)
        {
            EnsureCurrent(operation);
            int playerCount = PlayerCardCount;
            int opponentCount = OpponentCardCount;

            if (_debug)
            {
                Log($"[DrunkardGame] NotifyCardCountChanged: Player={playerCount}, Opponent={opponentCount}");
            }

            _onPlayerCardCountChanged?.Invoke(playerCount);
            EnsureCurrent(operation);
            _onOpponentCardCountChanged?.Invoke(opponentCount);
            EnsureCurrent(operation);

            if (_debug)
            {
                Log(
                    $"[DrunkardGame] OnPlayerCardCountChanged listeners: {_onPlayerCardCountChanged?.GetPersistentEventCount() ?? 0}");
                Log(
                    $"[DrunkardGame] OnOpponentCardCountChanged listeners: {_onOpponentCardCountChanged?.GetPersistentEventCount() ?? 0}");
            }
        }

        /// <summary>
        ///     Ends the match if either side is out of cards.
        /// </summary>
        /// <returns>True if the match ended this check.</returns>
        private bool CheckGameEnd(GameOperation operation)
        {
            EnsureCurrent(operation);
            if (PlayerCardCount == 0)
            {
                IsPlaying = false;
                _onOpponentWin?.Invoke();
                return true;
            }

            if (OpponentCardCount == 0)
            {
                IsPlaying = false;
                _onPlayerWin?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Invokes win events when someone has zero cards between rounds.
        /// </summary>
        private void EndGame(GameOperation operation)
        {
            EnsureCurrent(operation);
            IsPlaying = false;
            if (PlayerCardCount == 0)
            {
                _onOpponentWin?.Invoke();
            }
            else if (OpponentCardCount == 0)
            {
                _onPlayerWin?.Invoke();
            }
        }

        /// <summary>
        ///     Rebuilds the deck and redeals (inspector button).
        /// </summary>
        [Button]
        public void RestartGame()
        {
            RestartGameAsync().SuppressCancellationThrow().Forget();
        }

        /// <summary>
        ///     Async implementation for <see cref="RestartGame" />.
        /// </summary>
        private async UniTask RestartGameAsync()
        {
            if (_deckComponent == null || _deckComponent.Config == null || _deckComponent.CardPrefab == null)
            {
                LogError("[DrunkardGame] DeckComponent is not ready (needs Config and CardPrefab).");
                return;
            }

            GameOperation operation = BeginNewGame();
            _isRestarting = true;
            IsPlaying = false;
            GameStarted = false;
            _isInitialized = false;
            CleanupRoundViews();
            try
            {
                _playerCards.Clear();
                _opponentCards.Clear();
                if (UsePlayerHand)
                {
                    PlayerHand.Clear();
                    EnsureCurrent(operation);
                }

                if (UseOpponentHand)
                {
                    OpponentHand.Clear();
                    EnsureCurrent(operation);
                }

                if (_initialBoard != null)
                {
                    _initialBoard.Clear();
                    EnsureCurrent(operation);
                }

                List<CardComponent> allCards = await BuildInitialCardsAsync(operation);
                EnsureCurrent(operation);
                bool toPlayer = true;
                foreach (CardComponent cardObj in allCards)
                {
                    EnsureCurrent(operation);
                    if (_initialBoard != null)
                    {
                        _initialBoard.RemoveCard(cardObj);
                    }

                    EnsureCurrent(operation);
                    HandComponent hand = toPlayer ? PlayerHand : OpponentHand;
                    Queue<CardData> reserve = toPlayer ? _playerCards : _opponentCards;
                    await AddToHandOrReserveAsync(hand, reserve, cardObj, operation, false);
                    EnsureCurrent(operation);
                    toPlayer = !toPlayer;
                }

                _isInitialized = true;
                _isRestarting = false;
                int playerCount = PlayerCardCount;
                int opponentCount = OpponentCardCount;

                if (_debug)
                {
                    Log($"[DrunkardGame] Game restarted. Player cards: {playerCount}, opponent: {opponentCount}");
                }

                NotifyCardCountChanged(operation);
                EnsureCurrent(operation);
                _onGameRestarted?.Invoke();
            }
            catch (OperationCanceledException) when (!IsCurrent(operation))
            {
                // A later restart owns all shared queues, views and flags.
            }
            finally
            {
                if (this != null && operation.Generation == _gameGeneration)
                {
                    _isRestarting = false;
                }
            }
        }

        private async UniTask<List<CardComponent>> BuildInitialCardsAsync(GameOperation operation)
        {
            EnsureCurrent(operation);
            List<CardComponent> allCards = new();
            _deckComponent.Reset();
            EnsureCurrent(operation);
            _deckComponent.Initialize();
            EnsureCurrent(operation);

            while (!_deckComponent.IsEmpty)
            {
                EnsureCurrent(operation);
                CardComponent cardObj = _deckComponent.DrawCard(false);
                EnsureCurrent(operation);
                if (cardObj == null)
                {
                    break;
                }

                if (_cardsParent != null)
                {
                    cardObj.transform.SetParent(_cardsParent, true);
                }

                if (_initialBoard != null)
                {
                    await _initialBoard.PlaceCardAsync(cardObj, false, false);
                    EnsureCurrent(operation);
                    _initialBoard.RegisterInitialCard(cardObj);
                }

                allCards.Add(cardObj);
            }

            if (_debug && _initialBoard != null)
            {
                Log($"[DrunkardGame] Cards spawned on board: {allCards.Count}");
            }

            return allCards;
        }

        private CardComponent SpawnCard(CardData data, bool faceUp, GameOperation operation)
        {
            EnsureCurrent(operation);
            if (_deckComponent == null || _deckComponent.CardPrefab == null || _deckComponent.Config == null)
            {
                LogError("[DrunkardGame] Cannot spawn card: prefab/config missing.");
                return null;
            }

            Transform parent = _cardsParent != null ? _cardsParent : transform;
            CardComponent cardObj = Instantiate(_deckComponent.CardPrefab, parent);
            if (!IsCurrent(operation))
            {
                Destroy(cardObj.gameObject);
                return null;
            }

            cardObj.Config = _deckComponent.Config;
            cardObj.SetData(data, faceUp);
            return cardObj;
        }

        #region Properties

        /// <summary>
        ///     Player card count changed (carries new total).
        /// </summary>
        public UnityEvent<int> OnPlayerCardCountChanged => _onPlayerCardCountChanged;

        /// <summary>
        ///     Opponent card count changed (carries new total).
        /// </summary>
        public UnityEvent<int> OnOpponentCardCountChanged => _onOpponentCardCountChanged;

        /// <summary>
        ///     First round started.
        /// </summary>
        public UnityEvent OnGameStarted => _onGameStarted;

        /// <summary>
        ///     Match restarted via <see cref="RestartGame" />.
        /// </summary>
        public UnityEvent OnGameRestarted => _onGameRestarted;

        /// <summary>
        ///     Player collected the entire deck.
        /// </summary>
        public UnityEvent OnPlayerWin => _onPlayerWin;

        /// <summary>
        ///     Opponent collected the entire deck.
        /// </summary>
        public UnityEvent OnOpponentWin => _onOpponentWin;

        /// <summary>
        ///     Round begins.
        /// </summary>
        public UnityEvent OnRoundStarted => _onRoundStarted;

        /// <summary>
        ///     Round cleanup finished.
        /// </summary>
        public UnityEvent OnRoundEnded => _onRoundEnded;

        /// <summary>
        ///     Player won the trick (non-war).
        /// </summary>
        public UnityEvent OnPlayerWonRound => _onPlayerWonRound;

        /// <summary>
        ///     Opponent won the trick (non-war).
        /// </summary>
        public UnityEvent OnOpponentWonRound => _onOpponentWonRound;

        /// <summary>
        ///     War sequence started (tie).
        /// </summary>
        public UnityEvent OnWarStarted => _onWarStarted;

        /// <summary>
        ///     War sequence resolved.
        /// </summary>
        public UnityEvent OnWarEnded => _onWarEnded;

        /// <summary>
        ///     Cards remaining for the player, including the scene hand and queued reserve.
        /// </summary>
        public int PlayerCardCount => (UsePlayerHand ? PlayerHand.Count : 0) + _playerCards.Count;

        /// <summary>
        ///     Cards remaining for the opponent, including the scene hand and queued reserve.
        /// </summary>
        public int OpponentCardCount => (UseOpponentHand ? OpponentHand.Count : 0) + _opponentCards.Count;

        /// <summary>
        ///     True while a round is executing.
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        ///     True after the first round begins.
        /// </summary>
        public bool GameStarted { get; private set; }

        /// <summary>
        ///     Cached <see cref="HandComponent" /> on <c>PlayerDeckPosition</c>, if any.
        /// </summary>
        public HandComponent PlayerHand
        {
            get
            {
                CacheHands();
                return _cachedPlayerHand;
            }
        }

        /// <summary>
        ///     Cached <see cref="HandComponent" /> on <c>OpponentDeckPosition</c>, if any.
        /// </summary>
        public HandComponent OpponentHand
        {
            get
            {
                CacheHands();
                return _cachedOpponentHand;
            }
        }

        /// <summary>
        ///     True when a player <see cref="HandComponent" /> is assigned.
        /// </summary>
        public bool UsePlayerHand => PlayerHand != null;

        /// <summary>
        ///     True when an opponent <see cref="HandComponent" /> is assigned.
        /// </summary>
        public bool UseOpponentHand => OpponentHand != null;

        #endregion
    }
}
