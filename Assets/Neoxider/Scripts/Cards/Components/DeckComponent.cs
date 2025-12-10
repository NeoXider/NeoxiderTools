using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo.Cards
{
    /// <summary>
    ///     Компонент колоды для работы без кода
    /// </summary>
    public class DeckComponent : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private DeckConfig _config;

        [SerializeField] private bool _initializeOnStart = true;
        [SerializeField] private bool _shuffleOnStart = true;

        [Header("Visual")] [SerializeField] private Transform _spawnPoint;

        [SerializeField] private CardComponent _cardPrefab;

        [Header("Trump Display")] [SerializeField]
        private bool _showTrumpCard = true;

        [SerializeField] private CardComponent _trumpCardDisplay;

        [Header("Events")] public UnityEvent OnInitialized;

        public UnityEvent OnShuffled;
        public UnityEvent OnDeckEmpty;
        public UnityEvent<CardComponent> OnCardDrawn;
        private readonly List<CardComponent> _activeCards = new();

        /// <summary>
        ///     Модель колоды
        /// </summary>
        public DeckModel Model { get; private set; }

        /// <summary>
        ///     Количество оставшихся карт
        /// </summary>
        public int RemainingCount => Model?.RemainingCount ?? 0;

        /// <summary>
        ///     Пуста ли колода
        /// </summary>
        public bool IsEmpty => Model?.IsEmpty ?? true;

        /// <summary>
        ///     Козырная карта
        /// </summary>
        public CardData? TrumpCard => Model?.PeekBottom();

        /// <summary>
        ///     Козырная масть
        /// </summary>
        public Suit? TrumpSuit => TrumpCard?.Suit;

        /// <summary>
        ///     Точка спавна карт
        /// </summary>
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        /// <summary>
        ///     Сбрасывает колоду
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
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
        ///     Инициализирует колоду
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
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
        }

        /// <summary>
        ///     Перемешивает колоду
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void Shuffle()
        {
            Model?.Shuffle();
            OnShuffled?.Invoke();
        }

        /// <summary>
        ///     Берёт карту из колоды
        /// </summary>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Компонент карты или null</returns>
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
        ///     Берёт карту из колоды с анимацией
        /// </summary>
        /// <param name="targetPosition">Целевая позиция</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <param name="duration">Длительность перемещения</param>
        /// <returns>Компонент карты</returns>
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
        ///     Берёт несколько карт
        /// </summary>
        /// <param name="count">Количество карт</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Список карт</returns>
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
        ///     Возвращает карту в колоду
        /// </summary>
        /// <param name="card">Карта для возврата</param>
        /// <param name="toTop">В начало колоды</param>
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
    }
}