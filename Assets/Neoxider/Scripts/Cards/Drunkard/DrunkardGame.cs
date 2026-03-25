using System.Collections.Generic;
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

        [Header("Game Rules")] [Tooltip("If true — player moves first; if false — opponent")] [SerializeField]
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

        private void Awake()
        {
            CacheHands();
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
                Debug.LogError("[DrunkardGame] DeckComponent is not assigned!");
                return;
            }

            if (_deckComponent.Config == null)
            {
                Debug.LogError("[DrunkardGame] DeckComponent has no DeckConfig assigned!");
            }

            if (_deckComponent.CardPrefab == null)
            {
                Debug.LogError("[DrunkardGame] DeckComponent has no CardPrefab assigned!");
            }

            if (_cardsParent == null)
            {
                Debug.LogWarning("[DrunkardGame] CardsParent is not set — cards parent to DrunkardGame.");
            }

            if (_playerDeckPosition == null)
            {
                Debug.LogWarning("[DrunkardGame] PlayerDeckPosition is not assigned.");
            }

            if (_playerCardPosition == null)
            {
                Debug.LogWarning("[DrunkardGame] PlayerCardPosition is not assigned.");
            }

            if (_opponentDeckPosition == null)
            {
                Debug.LogWarning("[DrunkardGame] OpponentDeckPosition is not assigned.");
            }

            if (_opponentCardPosition == null)
            {
                Debug.LogWarning("[DrunkardGame] OpponentCardPosition is not assigned.");
            }
        }

        /// <summary>
        ///     UI-friendly wrapper that starts <see cref="PlayRound" /> without awaiting.
        /// </summary>
        [Button]
        public void Play()
        {
            if (_debug)
            {
                Debug.Log("[DrunkardGame] Play() invoked.");
            }

            PlayRound().Forget();
        }

        /// <summary>
        ///     Plays one round (order depends on <c>_playerGoesFirst</c>).
        /// </summary>
        public async UniTask PlayRound()
        {
            if (IsPlaying)
            {
                return;
            }

            if (PlayerCardCount == 0 || OpponentCardCount == 0)
            {
                EndGame();
                return;
            }

            IsPlaying = true;

            if (!GameStarted)
            {
                GameStarted = true;
                _onGameStarted?.Invoke();
            }

            _onRoundStarted?.Invoke();

            CardData opponentCard = DrawOpponentCard();
            CardData playerCard = DrawPlayerCard();

            if (_playerGoesFirst)
            {
                await ShowPlayerCard(playerCard);
                await UniTask.Delay((int)(_turnDelay * 1000));
                await ShowOpponentCard(opponentCard);
            }
            else
            {
                await ShowOpponentCard(opponentCard);
                await UniTask.Delay((int)(_turnDelay * 1000));
                await ShowPlayerCard(playerCard);
            }

            int comparison = playerCard.CompareTo(opponentCard);

            if (comparison > 0)
            {
                await UniTask.Delay((int)(_cardReturnDelay * 1000));
                await MoveCardsToWinnerAsync(true, playerCard, opponentCard);
                _onPlayerWonRound?.Invoke();
            }
            else if (comparison < 0)
            {
                await UniTask.Delay((int)(_cardReturnDelay * 1000));
                await MoveCardsToWinnerAsync(false, playerCard, opponentCard);
                _onOpponentWonRound?.Invoke();
            }
            else
            {
                _onWarStarted?.Invoke();

                _warCards.Clear();
                if (_playerCardView != null)
                {
                    _warCards.Add(_playerCardView);
                }

                if (_opponentCardView != null)
                {
                    _warCards.Add(_opponentCardView);
                }

                await HandleWar(playerCard, opponentCard);
                _onWarEnded?.Invoke();
            }

            await UniTask.Delay((int)(_roundDelay * 1000));

            if (_warCards.Count == 0)
            {
                await HideCards();
            }
            else
            {
                await HideWarCards();
            }

            NotifyCardCountChanged();
            _onRoundEnded?.Invoke();

            if (!CheckGameEnd())
            {
                IsPlaying = false;
            }
        }

        /// <summary>
        ///     Resolves tied ranks by drawing extra face-down/up cards until someone wins.
        /// </summary>
        private async UniTask HandleWar(CardData card1, CardData card2)
        {
            List<CardData> warPile = new() { card1, card2 };

            while (true)
            {
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
                warPile.Add(DrawOpponentCard());

                CardData opponentWarCard = DrawOpponentCard();
                CardData playerWarCard = DrawPlayerCard();
                warPile.Add(playerWarCard);
                warPile.Add(opponentWarCard);

                if (_playerGoesFirst)
                {
                    await ShowAdditionalPlayerCard(playerWarCard);
                    await UniTask.Delay((int)(_turnDelay * 1000));
                    await ShowAdditionalOpponentCard(opponentWarCard);
                }
                else
                {
                    await ShowAdditionalOpponentCard(opponentWarCard);
                    await UniTask.Delay((int)(_turnDelay * 1000));
                    await ShowAdditionalPlayerCard(playerWarCard);
                }

                int comparison = playerWarCard.CompareTo(opponentWarCard);

                if (comparison > 0)
                {
                    await MoveAllWarCardsToWinnerAsync(true, warPile);
                    _onPlayerWonRound?.Invoke();
                    return;
                }

                if (comparison < 0)
                {
                    await MoveAllWarCardsToWinnerAsync(false, warPile);
                    _onOpponentWonRound?.Invoke();
                    return;
                }

                await UniTask.Delay((int)(_warContinueDelay * 1000));
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
                        Debug.Log($"[DrunkardGame] Drew player hand card: {data}");
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
                        Debug.Log($"[DrunkardGame] Drew opponent hand card: {data}");
                    }

                    return data;
                }
            }

            return _opponentCards.Dequeue();
        }

        /// <summary>
        ///     Awards every card in the war pile (and visuals) to the winner.
        /// </summary>
        private async UniTask MoveAllWarCardsToWinnerAsync(bool playerWins, List<CardData> warPile)
        {
            // Hidden war cards without matching CardComponent instances (war pile larger than _warCards).
            List<CardData> hiddenCards = null;
            if (UsePlayerHand || UseOpponentHand)
            {
                Dictionary<CardData, int> visualUsage = new();
                foreach (CardComponent card in _warCards)
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

            if (playerWins)
            {
                if (UsePlayerHand)
                {
                    foreach (CardComponent card in _warCards)
                    {
                        if (card != null)
                        {
                            await PlayerHand.AddCardAsync(card);
                            card.IsFaceUp = false;
                            await UniTask.Delay((int)(_cardReturnDelay * 1000));
                        }
                    }

                    if (hiddenCards is { Count: > 0 })
                    {
                        foreach (CardData data in hiddenCards)
                        {
                            CardComponent hiddenCardObj = SpawnCard(data, false);
                            if (hiddenCardObj == null)
                            {
                                continue;
                            }

                            await PlayerHand.AddCardAsync(hiddenCardObj, false);
                        }
                    }
                }
                else
                {
                    foreach (CardData card in warPile)
                    {
                        _playerCards.Enqueue(card);
                    }

                    foreach (CardComponent card in _warCards)
                    {
                        if (card != null)
                        {
                            Destroy(card.gameObject);
                        }
                    }
                }
            }
            else
            {
                if (UseOpponentHand)
                {
                    foreach (CardComponent card in _warCards)
                    {
                        if (card != null)
                        {
                            await OpponentHand.AddCardAsync(card);
                            card.IsFaceUp = false;
                            await UniTask.Delay((int)(_cardReturnDelay * 1000));
                        }
                    }

                    if (hiddenCards is { Count: > 0 })
                    {
                        foreach (CardData data in hiddenCards)
                        {
                            CardComponent hiddenCardObj = SpawnCard(data, false);
                            if (hiddenCardObj == null)
                            {
                                continue;
                            }

                            await OpponentHand.AddCardAsync(hiddenCardObj, false);
                        }
                    }
                }
                else
                {
                    foreach (CardData card in warPile)
                    {
                        _opponentCards.Enqueue(card);
                    }

                    foreach (CardComponent card in _warCards)
                    {
                        if (card != null)
                        {
                            Destroy(card.gameObject);
                        }
                    }
                }
            }

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
        private async UniTask MoveCardsToWinnerAsync(bool playerWins, CardData playerCard, CardData opponentCard)
        {
            await MoveCardsToWinnerAsync(playerWins);

            if (!UsePlayerHand && !UseOpponentHand)
            {
                if (playerWins)
                {
                    _playerCards.Enqueue(playerCard);
                    _playerCards.Enqueue(opponentCard);
                }
                else
                {
                    _opponentCards.Enqueue(opponentCard);
                    _opponentCards.Enqueue(playerCard);
                }
            }
        }

        /// <summary>
        ///     Returns visible trick cards to the winner's hands when hand mode is enabled.
        /// </summary>
        /// <param name="playerWins">True if the player won.</param>
        private async UniTask MoveCardsToWinnerAsync(bool playerWins)
        {
            if (playerWins)
            {
                if (UsePlayerHand)
                {
                    if (_playerCardView != null)
                    {
                        CardComponent card = _playerCardView;
                        _playerCardView = null;
                        await PlayerHand.AddCardAsync(card);
                        card.IsFaceUp = false;
                    }

                    await UniTask.Delay((int)(_cardReturnDelay * 1000));

                    if (_opponentCardView != null)
                    {
                        CardComponent card = _opponentCardView;
                        _opponentCardView = null;
                        await PlayerHand.AddCardAsync(card);
                        card.IsFaceUp = false;
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
                        await OpponentHand.AddCardAsync(card);
                        card.IsFaceUp = false;
                    }

                    await UniTask.Delay((int)(_cardReturnDelay * 1000));

                    if (_playerCardView != null)
                    {
                        CardComponent card = _playerCardView;
                        _playerCardView = null;
                        await OpponentHand.AddCardAsync(card);
                        card.IsFaceUp = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Spawns/flips an extra player card during war.
        /// </summary>
        private async UniTask ShowAdditionalPlayerCard(CardData playerCard)
        {
            CardComponent cardObj = SpawnCard(playerCard, false);
            if (cardObj == null)
            {
                return;
            }

            if (_playerDeckPosition != null)
            {
                cardObj.transform.position = _playerDeckPosition.position;
            }

            if (_playerCardPosition != null)
            {
                Vector3 offset = Vector3.right * (_warCards.Count / 2) * 30f;
                await cardObj.MoveToAsync(_playerCardPosition.position + offset, _cardMoveDuration);
            }

            await cardObj.FlipAsync();
            _warCards.Add(cardObj);
        }

        /// <summary>
        ///     Spawns/flips an extra opponent card during war.
        /// </summary>
        private async UniTask ShowAdditionalOpponentCard(CardData opponentCard)
        {
            CardComponent cardObj = SpawnCard(opponentCard, false);
            if (cardObj == null)
            {
                return;
            }

            if (_opponentDeckPosition != null)
            {
                cardObj.transform.position = _opponentDeckPosition.position;
            }

            if (_opponentCardPosition != null)
            {
                Vector3 offset = Vector3.right * (_warCards.Count / 2) * 30f;
                await cardObj.MoveToAsync(_opponentCardPosition.position + offset, _cardMoveDuration);
            }

            await cardObj.FlipAsync();
            _warCards.Add(cardObj);
        }

        /// <summary>
        ///     Animates the opponent's played card to the table.
        /// </summary>
        private async UniTask ShowOpponentCard(CardData opponentCard)
        {
            if (_debug)
            {
                Debug.Log($"[DrunkardGame] ShowOpponentCard: {opponentCard}");
            }

            if (_opponentCardView == null)
            {
                if (_deckComponent == null || _deckComponent.CardPrefab == null || _deckComponent.Config == null)
                {
                    Debug.LogError("[DrunkardGame] CardPrefab/DeckConfig are not set up!");
                    return;
                }

                _opponentCardView = Instantiate(_deckComponent.CardPrefab,
                    _cardsParent != null ? _cardsParent : transform);
                _opponentCardView.Config = _deckComponent.Config;
                if (_debug)
                {
                    Debug.Log(
                        $"[DrunkardGame] Spawned opponent card: {_opponentCardView.name} under {_cardsParent?.name}");
                }
            }

            _opponentCardView.SetData(opponentCard, false);
            _opponentCardView.gameObject.SetActive(true);

            if (_opponentDeckPosition != null)
            {
                _opponentCardView.transform.position = _opponentDeckPosition.position;
            }

            if (_opponentCardPosition != null)
            {
                await _opponentCardView.MoveToAsync(_opponentCardPosition.position, _cardMoveDuration);
            }

            await _opponentCardView.FlipAsync();
        }

        /// <summary>
        ///     Animates the player's played card to the table.
        /// </summary>
        private async UniTask ShowPlayerCard(CardData playerCard)
        {
            if (_playerCardView == null)
            {
                if (_deckComponent == null || _deckComponent.CardPrefab == null || _deckComponent.Config == null)
                {
                    Debug.LogError("[DrunkardGame] CardPrefab/DeckConfig are not set up!");
                    return;
                }

                _playerCardView = Instantiate(_deckComponent.CardPrefab,
                    _cardsParent != null ? _cardsParent : transform);
                _playerCardView.Config = _deckComponent.Config;
                if (_debug)
                {
                    Debug.Log($"[DrunkardGame] Spawned player card: {_playerCardView.name} under {_cardsParent?.name}");
                }
            }

            _playerCardView.SetData(playerCard, false);
            _playerCardView.gameObject.SetActive(true);

            if (_playerDeckPosition != null)
            {
                _playerCardView.transform.position = _playerDeckPosition.position;
            }

            if (_playerCardPosition != null)
            {
                await _playerCardView.MoveToAsync(_playerCardPosition.position, _cardMoveDuration);
            }

            await _playerCardView.FlipAsync();
        }

        /// <summary>
        ///     Destroys the active trick card views.
        /// </summary>
        private async UniTask HideCards()
        {
            if (_playerCardView != null)
            {
                Destroy(_playerCardView.gameObject);
                _playerCardView = null;
            }

            if (_opponentCardView != null)
            {
                Destroy(_opponentCardView.gameObject);
                _opponentCardView = null;
            }

            await UniTask.Yield();
        }

        /// <summary>
        ///     Destroys every card spawned during war resolution.
        /// </summary>
        private async UniTask HideWarCards()
        {
            foreach (CardComponent card in _warCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _warCards.Clear();
            _playerCardView = null;
            _opponentCardView = null;

            await UniTask.Yield();
        }

        /// <summary>
        ///     Fires card-count UnityEvents (and optional debug logs).
        /// </summary>
        private void NotifyCardCountChanged()
        {
            int playerCount = PlayerCardCount;
            int opponentCount = OpponentCardCount;

            if (_debug)
            {
                Debug.Log($"[DrunkardGame] NotifyCardCountChanged: Player={playerCount}, Opponent={opponentCount}");
            }

            _onPlayerCardCountChanged?.Invoke(playerCount);
            _onOpponentCardCountChanged?.Invoke(opponentCount);

            if (_debug)
            {
                Debug.Log(
                    $"[DrunkardGame] OnPlayerCardCountChanged listeners: {_onPlayerCardCountChanged?.GetPersistentEventCount() ?? 0}");
                Debug.Log(
                    $"[DrunkardGame] OnOpponentCardCountChanged listeners: {_onOpponentCardCountChanged?.GetPersistentEventCount() ?? 0}");
            }
        }

        /// <summary>
        ///     Ends the match if either side is out of cards.
        /// </summary>
        /// <returns>True if the match ended this check.</returns>
        private bool CheckGameEnd()
        {
            if (PlayerCardCount == 0)
            {
                _onOpponentWin?.Invoke();
                IsPlaying = false;
                return true;
            }

            if (OpponentCardCount == 0)
            {
                _onPlayerWin?.Invoke();
                IsPlaying = false;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Invokes win events when someone has zero cards between rounds.
        /// </summary>
        private void EndGame()
        {
            if (PlayerCardCount == 0)
            {
                _onOpponentWin?.Invoke();
            }
            else if (OpponentCardCount == 0)
            {
                _onPlayerWin?.Invoke();
            }

            IsPlaying = false;
        }

        /// <summary>
        ///     Rebuilds the deck and redeals (inspector button).
        /// </summary>
        [Button]
        public void RestartGame()
        {
            RestartGameAsync().Forget();
        }

        /// <summary>
        ///     Async implementation for <see cref="RestartGame" />.
        /// </summary>
        private async UniTask RestartGameAsync()
        {
            if (_deckComponent == null || _deckComponent.Config == null || _deckComponent.CardPrefab == null)
            {
                Debug.LogError("[DrunkardGame] DeckComponent is not ready (needs Config and CardPrefab).");
                return;
            }

            if (_playerCardView != null)
            {
                Destroy(_playerCardView.gameObject);
            }

            if (_opponentCardView != null)
            {
                Destroy(_opponentCardView.gameObject);
            }

            _playerCardView = null;
            _opponentCardView = null;

            _playerCards.Clear();
            _opponentCards.Clear();

            if (UsePlayerHand)
            {
                PlayerHand.Clear();
            }

            if (UseOpponentHand)
            {
                OpponentHand.Clear();
            }

            if (_initialBoard != null)
            {
                _initialBoard.Clear();
            }

            IsPlaying = false;
            GameStarted = false;
            _isInitialized = true;

            List<CardComponent> allCards = await BuildInitialCardsAsync();

            bool toPlayer = true;
            foreach (CardComponent cardObj in allCards)
            {
                if (toPlayer)
                {
                    if (UsePlayerHand)
                    {
                        if (_initialBoard != null)
                        {
                            _initialBoard.RemoveCard(cardObj);
                        }

                        PlayerHand.AddCard(cardObj);
                    }
                    else
                    {
                        _playerCards.Enqueue(cardObj.Data);
                        if (_initialBoard != null)
                        {
                            _initialBoard.RemoveCard(cardObj);
                        }

                        Destroy(cardObj.gameObject);
                    }
                }
                else
                {
                    if (UseOpponentHand)
                    {
                        if (_initialBoard != null)
                        {
                            _initialBoard.RemoveCard(cardObj);
                        }

                        OpponentHand.AddCard(cardObj);
                    }
                    else
                    {
                        _opponentCards.Enqueue(cardObj.Data);
                        if (_initialBoard != null)
                        {
                            _initialBoard.RemoveCard(cardObj);
                        }

                        Destroy(cardObj.gameObject);
                    }
                }

                toPlayer = !toPlayer;
            }

            int playerCount = UsePlayerHand ? PlayerHand.Count : _playerCards.Count;
            int opponentCount = UseOpponentHand ? OpponentHand.Count : _opponentCards.Count;

            if (_debug)
            {
                Debug.Log(
                    $"[DrunkardGame] Game restarted. Player cards: {playerCount}, opponent: {opponentCount}");
            }

            NotifyCardCountChanged();
            _onGameRestarted?.Invoke();
        }

        private async UniTask<List<CardComponent>> BuildInitialCardsAsync()
        {
            List<CardComponent> allCards = new();
            _deckComponent.Reset();
            _deckComponent.Initialize();

            while (!_deckComponent.IsEmpty)
            {
                CardComponent cardObj = _deckComponent.DrawCard(false);
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
                    _initialBoard.RegisterInitialCard(cardObj);
                }

                allCards.Add(cardObj);
            }

            if (_debug && _initialBoard != null)
            {
                Debug.Log($"[DrunkardGame] Cards spawned on board: {allCards.Count}");
            }

            return allCards;
        }

        private CardComponent SpawnCard(CardData data, bool faceUp)
        {
            if (_deckComponent == null || _deckComponent.CardPrefab == null || _deckComponent.Config == null)
            {
                Debug.LogError("[DrunkardGame] Cannot spawn card: prefab/config missing.");
                return null;
            }

            Transform parent = _cardsParent != null ? _cardsParent : transform;
            CardComponent cardObj = Instantiate(_deckComponent.CardPrefab, parent);
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
        ///     Cards remaining for the player (hand or queue).
        /// </summary>
        public int PlayerCardCount => UsePlayerHand ? PlayerHand.Count : _playerCards.Count;

        /// <summary>
        ///     Cards remaining for the opponent (hand or queue).
        /// </summary>
        public int OpponentCardCount => UseOpponentHand ? OpponentHand.Count : _opponentCards.Count;

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
