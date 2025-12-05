using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    /// Игра "Пьяница" (War Card Game).
    /// Классическая карточная игра где побеждает тот, кто соберёт все карты.
    /// </summary>
    public class DrunkardGame : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private DeckConfig _deckConfig;
        [SerializeField] private CardComponent _cardPrefab;
        [SerializeField] private bool _initializeOnStart = true;
        [SerializeField] private bool _debug = false;

        [Header("Positions")]
        [SerializeField] private Transform _cardsParent;
        [Tooltip("BoardComponent для начальной раздачи. Если указан - карты сначала спавнятся в Board, затем раздаются поровну.")]
        [SerializeField] private BoardComponent _initialBoard;
        [Tooltip("Позиция колоды игрока (откуда выезжает карта). Можно указать HandComponent.")]
        [SerializeField] private Transform _playerDeckPosition;
        [SerializeField] private Transform _playerCardPosition;
        [SerializeField] private Transform _opponentDeckPosition;
        [SerializeField] private Transform _opponentCardPosition;


        [Header("Timing")]
        [SerializeField] private float _cardMoveDuration = 0.3f;
        [SerializeField] private float _roundDelay = 1f;
        [Tooltip("Задержка между ходами игроков")]
        [SerializeField] private float _turnDelay = 0.3f;
        [SerializeField] private float _warContinueDelay = 0.5f;
        [SerializeField] private float _cardReturnDelay = 0.1f;
        
        [Header("Game Rules")]
        [Tooltip("Если true - игрок ходит первым, если false - соперник")]
        [SerializeField] private bool _playerGoesFirst = true;

        [Header("Events - Card Count")]
        [SerializeField] private UnityEvent<int> _onPlayerCardCountChanged;
        [SerializeField] private UnityEvent<int> _onOpponentCardCountChanged;

        [Header("Events - Game Flow")]
        [SerializeField] private UnityEvent _onGameStarted;
        [SerializeField] private UnityEvent _onGameRestarted;
        [SerializeField] private UnityEvent _onPlayerWin;
        [SerializeField] private UnityEvent _onOpponentWin;

        [Header("Events - Round")]
        [SerializeField] private UnityEvent _onRoundStarted;
        [SerializeField] private UnityEvent _onRoundEnded;
        [SerializeField] private UnityEvent _onPlayerWonRound;
        [SerializeField] private UnityEvent _onOpponentWonRound;
        [SerializeField] private UnityEvent _onWarStarted;
        [SerializeField] private UnityEvent _onWarEnded;

        private Queue<CardData> _playerCards = new();
        private Queue<CardData> _opponentCards = new();
        private CardComponent _playerCardView;
        private CardComponent _opponentCardView;
        private List<CardComponent> _warCards = new();
        private bool _isPlaying;
        private bool _gameStarted;
        private bool _isInitialized;
        
        private HandComponent _cachedPlayerHand;
        private HandComponent _cachedOpponentHand;
        private bool _handsCached;

        #region Properties

        /// <summary>
        /// Событие изменения количества карт игрока. Передаёт текущее количество карт.
        /// </summary>
        public UnityEvent<int> OnPlayerCardCountChanged => _onPlayerCardCountChanged;

        /// <summary>
        /// Событие изменения количества карт соперника. Передаёт текущее количество карт.
        /// </summary>
        public UnityEvent<int> OnOpponentCardCountChanged => _onOpponentCardCountChanged;

        /// <summary>
        /// Событие начала игры (первый раунд).
        /// </summary>
        public UnityEvent OnGameStarted => _onGameStarted;

        /// <summary>
        /// Событие перезапуска игры.
        /// </summary>
        public UnityEvent OnGameRestarted => _onGameRestarted;

        /// <summary>
        /// Событие победы игрока.
        /// </summary>
        public UnityEvent OnPlayerWin => _onPlayerWin;

        /// <summary>
        /// Событие победы соперника.
        /// </summary>
        public UnityEvent OnOpponentWin => _onOpponentWin;

        /// <summary>
        /// Событие начала раунда.
        /// </summary>
        public UnityEvent OnRoundStarted => _onRoundStarted;

        /// <summary>
        /// Событие окончания раунда.
        /// </summary>
        public UnityEvent OnRoundEnded => _onRoundEnded;

        /// <summary>
        /// Событие победы игрока в раунде.
        /// </summary>
        public UnityEvent OnPlayerWonRound => _onPlayerWonRound;

        /// <summary>
        /// Событие победы соперника в раунде.
        /// </summary>
        public UnityEvent OnOpponentWonRound => _onOpponentWonRound;

        /// <summary>
        /// Событие начала "войны" (равенство карт).
        /// </summary>
        public UnityEvent OnWarStarted => _onWarStarted;

        /// <summary>
        /// Событие окончания "войны".
        /// </summary>
        public UnityEvent OnWarEnded => _onWarEnded;

        /// <summary>
        /// Текущее количество карт у игрока.
        /// </summary>
        /// <summary>
        /// Текущее количество карт у игрока.
        /// </summary>
        public int PlayerCardCount => UsePlayerHand ? PlayerHand.Count : _playerCards.Count;

        /// <summary>
        /// Текущее количество карт у соперника.
        /// </summary>
        public int OpponentCardCount => UseOpponentHand ? OpponentHand.Count : _opponentCards.Count;

        /// <summary>
        /// Идёт ли сейчас раунд.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Началась ли игра (был хотя бы один раунд).
        /// </summary>
        public bool GameStarted => _gameStarted;

        /// <summary>
        /// HandComponent игрока (если указан в PlayerDeckPosition).
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
        /// HandComponent соперника (если указан в OpponentDeckPosition).
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
        /// Используется ли рука игрока.
        /// </summary>
        public bool UsePlayerHand => PlayerHand != null;

        /// <summary>
        /// Используется ли рука соперника.
        /// </summary>
        public bool UseOpponentHand => OpponentHand != null;

        #endregion

        private void Awake()
        {
            CacheHands();
        }

        private void CacheHands()
        {
            if (_handsCached) return;
            _handsCached = true;
            
            if (_playerDeckPosition != null)
                _cachedPlayerHand = _playerDeckPosition.GetComponent<HandComponent>();
            
            if (_opponentDeckPosition != null)
                _cachedOpponentHand = _opponentDeckPosition.GetComponent<HandComponent>();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            _handsCached = false;
        }

        private void Start()
        {
            ValidateSetup();
            
            if (_initializeOnStart)
            {
                RestartGame();
            }
        }

        private void ValidateSetup()
        {
            if (_deckConfig == null)
                Debug.LogError("[DrunkardGame] DeckConfig не назначен!");
            
            if (_cardPrefab == null)
                Debug.LogError("[DrunkardGame] CardPrefab не назначен!");
            
            if (_cardsParent == null)
                Debug.LogWarning("[DrunkardGame] CardsParent не назначен - карты будут создаваться на DrunkardGame");
            
            if (_playerDeckPosition == null)
                Debug.LogWarning("[DrunkardGame] PlayerDeckPosition не назначен");
            
            if (_playerCardPosition == null)
                Debug.LogWarning("[DrunkardGame] PlayerCardPosition не назначен");
            
            if (_opponentDeckPosition == null)
                Debug.LogWarning("[DrunkardGame] OpponentDeckPosition не назначен");
            
            if (_opponentCardPosition == null)
                Debug.LogWarning("[DrunkardGame] OpponentCardPosition не назначен");
        }

        /// <summary>
        /// Выполняет ход (для вызова из UI кнопки).
        /// Не-асинхронная обёртка над PlayRound().
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void Play()
        {
            if (_debug) Debug.Log("[DrunkardGame] Play() вызван");
            PlayRound().Forget();
        }

        /// <summary>
        /// Выполняет один раунд игры между игроком и противником.
        /// Сначала соперник показывает карту, затем игрок.
        /// </summary>
        public async UniTask PlayRound()
        {
            if (_isPlaying) return;
            if (PlayerCardCount == 0 || OpponentCardCount == 0)
            {
                EndGame();
                return;
            }

            _isPlaying = true;
            
            if (!_gameStarted)
            {
                _gameStarted = true;
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
                if (_playerCardView != null) _warCards.Add(_playerCardView);
                if (_opponentCardView != null) _warCards.Add(_opponentCardView);
                
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
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Обрабатывает ситуацию "войны" при равенстве карт.
        /// Все карты остаются на столе до определения победителя.
        /// </summary>
        private async UniTask HandleWar(CardData card1, CardData card2)
        {
            var warPile = new List<CardData> { card1, card2 };

            while (true)
            {
                if (PlayerCardCount < 2 || OpponentCardCount < 2)
                {
                    for (int i = 0; i < warPile.Count; i++)
                    {
                        if (i % 2 == 0)
                            _playerCards.Enqueue(warPile[i]);
                        else
                            _opponentCards.Enqueue(warPile[i]);
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
        /// Берёт карту у игрока (из руки или из очереди).
        /// </summary>
        private CardData DrawPlayerCard()
        {
            if (UsePlayerHand && PlayerHand.Count > 0)
            {
                var cardComponent = PlayerHand.DrawFirst();
                if (cardComponent != null)
                {
                    var data = cardComponent.Data;
                    Destroy(cardComponent.gameObject);
                    if (_debug) Debug.Log($"[DrunkardGame] Взята карта из руки игрока: {data}");
                    return data;
                }
            }
            
            return _playerCards.Dequeue();
        }

        /// <summary>
        /// Берёт карту у соперника (из руки или из очереди).
        /// </summary>
        private CardData DrawOpponentCard()
        {
            if (UseOpponentHand && OpponentHand.Count > 0)
            {
                var cardComponent = OpponentHand.DrawFirst();
                if (cardComponent != null)
                {
                    var data = cardComponent.Data;
                    Destroy(cardComponent.gameObject);
                    if (_debug) Debug.Log($"[DrunkardGame] Взята карта из руки соперника: {data}");
                    return data;
                }
            }
            
            return _opponentCards.Dequeue();
        }

        /// <summary>
        /// Перемещает все карты со стола (включая карты войны) победителю.
        /// </summary>
        private async UniTask MoveAllWarCardsToWinnerAsync(bool playerWins, List<CardData> warPile)
        {
            if (playerWins)
            {
                if (UsePlayerHand)
                {
                    foreach (var card in _warCards)
                    {
                        if (card != null)
                        {
                            await PlayerHand.AddCardAsync(card, animate: true);
                            card.IsFaceUp = false;
                            await UniTask.Delay((int)(_cardReturnDelay * 1000));
                        }
                    }
                }
                else
                {
                    foreach (var card in warPile)
                        _playerCards.Enqueue(card);
                    
                    foreach (var card in _warCards)
                    {
                        if (card != null) Destroy(card.gameObject);
                    }
                }
            }
            else
            {
                if (UseOpponentHand)
                {
                    foreach (var card in _warCards)
                    {
                        if (card != null)
                        {
                            await OpponentHand.AddCardAsync(card, animate: true);
                            card.IsFaceUp = false;
                            await UniTask.Delay((int)(_cardReturnDelay * 1000));
                        }
                    }
                }
                else
                {
                    foreach (var card in warPile)
                        _opponentCards.Enqueue(card);
                    
                    foreach (var card in _warCards)
                    {
                        if (card != null) Destroy(card.gameObject);
                    }
                }
            }
            
            _warCards.Clear();
            _playerCardView = null;
            _opponentCardView = null;
        }

        /// <summary>
        /// Перемещает карты со стола победителю.
        /// </summary>
        /// <param name="playerWins">true если выиграл игрок, false если соперник</param>
        /// <param name="playerCard">Данные карты игрока</param>
        /// <param name="opponentCard">Данные карты соперника</param>
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
        /// Перемещает карты со стола победителю.
        /// </summary>
        /// <param name="playerWins">true если выиграл игрок, false если соперник</param>
        private async UniTask MoveCardsToWinnerAsync(bool playerWins)
        {
            if (playerWins)
            {
                if (UsePlayerHand)
                {
                    if (_playerCardView != null)
                    {
                        var card = _playerCardView;
                        _playerCardView = null;
                        await PlayerHand.AddCardAsync(card, animate: true);
                        card.IsFaceUp = false;
                    }
                    
                    await UniTask.Delay((int)(_cardReturnDelay * 1000));
                    
                    if (_opponentCardView != null)
                    {
                        var card = _opponentCardView;
                        _opponentCardView = null;
                        await PlayerHand.AddCardAsync(card, animate: true);
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
                        var card = _opponentCardView;
                        _opponentCardView = null;
                        await OpponentHand.AddCardAsync(card, animate: true);
                        card.IsFaceUp = false;
                    }
                    
                    await UniTask.Delay((int)(_cardReturnDelay * 1000));
                    
                    if (_playerCardView != null)
                    {
                        var card = _playerCardView;
                        _playerCardView = null;
                        await OpponentHand.AddCardAsync(card, animate: true);
                        card.IsFaceUp = false;
                    }
                }
            }
        }

        /// <summary>
        /// Показывает дополнительную карту игрока (для войны).
        /// </summary>
        private async UniTask ShowAdditionalPlayerCard(CardData playerCard)
        {
            var cardObj = Instantiate(_cardPrefab, _cardsParent != null ? _cardsParent : transform);
            cardObj.Config = _deckConfig;
            cardObj.SetData(playerCard, faceUp: false);
            
            if (_playerDeckPosition != null)
                cardObj.transform.position = _playerDeckPosition.position;

            if (_playerCardPosition != null)
            {
                Vector3 offset = Vector3.right * (_warCards.Count / 2) * 30f;
                await cardObj.MoveToAsync(_playerCardPosition.position + offset, _cardMoveDuration);
            }
            
            await cardObj.FlipAsync();
            _warCards.Add(cardObj);
        }

        /// <summary>
        /// Показывает дополнительную карту соперника (для войны).
        /// </summary>
        private async UniTask ShowAdditionalOpponentCard(CardData opponentCard)
        {
            var cardObj = Instantiate(_cardPrefab, _cardsParent != null ? _cardsParent : transform);
            cardObj.Config = _deckConfig;
            cardObj.SetData(opponentCard, faceUp: false);
            
            if (_opponentDeckPosition != null)
                cardObj.transform.position = _opponentDeckPosition.position;

            if (_opponentCardPosition != null)
            {
                Vector3 offset = Vector3.right * (_warCards.Count / 2) * 30f;
                await cardObj.MoveToAsync(_opponentCardPosition.position + offset, _cardMoveDuration);
            }
            
            await cardObj.FlipAsync();
            _warCards.Add(cardObj);
        }

        /// <summary>
        /// Показывает карту соперника с анимацией.
        /// </summary>
        private async UniTask ShowOpponentCard(CardData opponentCard)
        {
            if (_debug) Debug.Log($"[DrunkardGame] ShowOpponentCard: {opponentCard}");
            
            if (_opponentCardView == null)
            {
                if (_cardPrefab == null)
                {
                    Debug.LogError("[DrunkardGame] CardPrefab не назначен!");
                    return;
                }
                _opponentCardView = Instantiate(_cardPrefab, _cardsParent != null ? _cardsParent : transform);
                _opponentCardView.Config = _deckConfig;
                if (_debug) Debug.Log($"[DrunkardGame] Создана карта соперника: {_opponentCardView.name} в {_cardsParent?.name}");
            }

            _opponentCardView.SetData(opponentCard, faceUp: false);
            _opponentCardView.gameObject.SetActive(true);
            
            if (_opponentDeckPosition != null)
                _opponentCardView.transform.position = _opponentDeckPosition.position;

            if (_opponentCardPosition != null)
                await _opponentCardView.MoveToAsync(_opponentCardPosition.position, _cardMoveDuration);
            
            await _opponentCardView.FlipAsync();
        }

        /// <summary>
        /// Показывает карту игрока с анимацией.
        /// </summary>
        private async UniTask ShowPlayerCard(CardData playerCard)
        {
            if (_playerCardView == null)
            {
                _playerCardView = Instantiate(_cardPrefab, _cardsParent != null ? _cardsParent : transform);
                _playerCardView.Config = _deckConfig;
                if (_debug) Debug.Log($"[DrunkardGame] Создана карта игрока: {_playerCardView.name} в {_cardsParent?.name}");
            }

            _playerCardView.SetData(playerCard, faceUp: false);
            _playerCardView.gameObject.SetActive(true);
            
            if (_playerDeckPosition != null)
                _playerCardView.transform.position = _playerDeckPosition.position;

            if (_playerCardPosition != null)
                await _playerCardView.MoveToAsync(_playerCardPosition.position, _cardMoveDuration);
            
            await _playerCardView.FlipAsync();
        }

        /// <summary>
        /// Скрывает текущие отображаемые карты.
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
        /// Скрывает все карты войны.
        /// </summary>
        private async UniTask HideWarCards()
        {
            foreach (var card in _warCards)
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
        /// Уведомляет об изменении количества карт через события.
        /// </summary>
        private void NotifyCardCountChanged()
        {
            int playerCount = PlayerCardCount;
            int opponentCount = OpponentCardCount;
            
            if (_debug) Debug.Log($"[DrunkardGame] NotifyCardCountChanged: Player={playerCount}, Opponent={opponentCount}");
            
            _onPlayerCardCountChanged?.Invoke(playerCount);
            _onOpponentCardCountChanged?.Invoke(opponentCount);
            
            if (_debug)
            {
                Debug.Log($"[DrunkardGame] OnPlayerCardCountChanged listeners: {_onPlayerCardCountChanged?.GetPersistentEventCount() ?? 0}");
                Debug.Log($"[DrunkardGame] OnOpponentCardCountChanged listeners: {_onOpponentCardCountChanged?.GetPersistentEventCount() ?? 0}");
            }
        }

        /// <summary>
        /// Проверяет, завершена ли игра.
        /// </summary>
        /// <returns>true если игра завершена</returns>
        private bool CheckGameEnd()
        {
            if (PlayerCardCount == 0)
            {
                _onOpponentWin?.Invoke();
                _isPlaying = false;
                return true;
            }

            if (OpponentCardCount == 0)
            {
                _onPlayerWin?.Invoke();
                _isPlaying = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Завершает игру и вызывает соответствующее событие.
        /// </summary>
        private void EndGame()
        {
            if (PlayerCardCount == 0)
                _onOpponentWin?.Invoke();
            else if (OpponentCardCount == 0)
                _onPlayerWin?.Invoke();

            _isPlaying = false;
        }

        /// <summary>
        /// Перезапускает игру: создаёт новую колоду и раздаёт карты.
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void RestartGame()
        {
            if (_deckConfig == null || _cardPrefab == null)
            {
                Debug.LogError("[DrunkardGame] DeckConfig или CardPrefab не назначены!");
                return;
            }

            if (_playerCardView != null) Destroy(_playerCardView.gameObject);
            if (_opponentCardView != null) Destroy(_opponentCardView.gameObject);
            _playerCardView = null;
            _opponentCardView = null;

            _playerCards.Clear();
            _opponentCards.Clear();
            
            if (UsePlayerHand)
                PlayerHand.Clear();
            
            if (UseOpponentHand)
                OpponentHand.Clear();
            
            if (_initialBoard != null)
                _initialBoard.Clear();
            
            _isPlaying = false;
            _gameStarted = false;
            _isInitialized = true;

            var deck = new DeckModel();
            deck.Initialize(_deckConfig.GameDeckType, shuffle: true);

            List<CardComponent> allCards = new List<CardComponent>();

            if (_initialBoard != null)
            {
                while (!deck.IsEmpty)
                {
                    CardData? card = deck.Draw();
                    if (!card.HasValue) break;

                    var cardObj = Instantiate(_cardPrefab, _cardsParent != null ? _cardsParent : transform);
                    cardObj.Config = _deckConfig;
                    cardObj.SetData(card.Value, faceUp: false);
                    allCards.Add(cardObj);
                }

                if (_debug) Debug.Log($"[DrunkardGame] Карты созданы в Board: {allCards.Count}");
            }
            else
            {
                while (!deck.IsEmpty)
                {
                    CardData? card = deck.Draw();
                    if (!card.HasValue) break;

                    var cardObj = Instantiate(_cardPrefab, _cardsParent != null ? _cardsParent : transform);
                    cardObj.Config = _deckConfig;
                    cardObj.SetData(card.Value, faceUp: false);
                    allCards.Add(cardObj);
                }
            }

            bool toPlayer = true;
            foreach (var cardObj in allCards)
            {
                if (toPlayer)
                {
                    if (UsePlayerHand)
                    {
                        if (_initialBoard != null)
                            _initialBoard.RemoveCard(cardObj);
                        PlayerHand.AddCard(cardObj);
                    }
                    else
                    {
                        _playerCards.Enqueue(cardObj.Data);
                        if (_initialBoard != null)
                            _initialBoard.RemoveCard(cardObj);
                        Destroy(cardObj.gameObject);
                    }
                }
                else
                {
                    if (UseOpponentHand)
                    {
                        if (_initialBoard != null)
                            _initialBoard.RemoveCard(cardObj);
                        OpponentHand.AddCard(cardObj);
                    }
                    else
                    {
                        _opponentCards.Enqueue(cardObj.Data);
                        if (_initialBoard != null)
                            _initialBoard.RemoveCard(cardObj);
                        Destroy(cardObj.gameObject);
                    }
                }

                toPlayer = !toPlayer;
            }

            int playerCount = UsePlayerHand ? PlayerHand.Count : _playerCards.Count;
            int opponentCount = UseOpponentHand ? OpponentHand.Count : _opponentCards.Count;
            
            if (_debug) Debug.Log($"[DrunkardGame] Игра перезапущена. Карт у игрока: {playerCount}, у соперника: {opponentCount}");
            
            NotifyCardCountChanged();
            _onGameRestarted?.Invoke();
        }
    }
}
