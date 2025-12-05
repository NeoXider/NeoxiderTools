using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Cards
{
    /// <summary>
    /// Компонент колоды для работы без кода
    /// </summary>
    public class DeckComponent : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private DeckConfig _config;
        [SerializeField] private bool _initializeOnStart = true;
        [SerializeField] private bool _shuffleOnStart = true;

        [Header("Visual")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private CardComponent _cardPrefab;

        [Header("Trump Display")]
        [SerializeField] private bool _showTrumpCard = true;
        [SerializeField] private CardComponent _trumpCardDisplay;

        [Header("Events")]
        public UnityEvent OnInitialized;
        public UnityEvent OnShuffled;
        public UnityEvent OnDeckEmpty;
        public UnityEvent<CardComponent> OnCardDrawn;

        private DeckModel _model;
        private readonly List<CardComponent> _activeCards = new();

        /// <summary>
        /// Модель колоды
        /// </summary>
        public DeckModel Model => _model;

        /// <summary>
        /// Количество оставшихся карт
        /// </summary>
        public int RemainingCount => _model?.RemainingCount ?? 0;

        /// <summary>
        /// Пуста ли колода
        /// </summary>
        public bool IsEmpty => _model?.IsEmpty ?? true;

        /// <summary>
        /// Козырная карта
        /// </summary>
        public CardData? TrumpCard => _model?.PeekBottom();

        /// <summary>
        /// Козырная масть
        /// </summary>
        public Suit? TrumpSuit => TrumpCard?.Suit;

        /// <summary>
        /// Точка спавна карт
        /// </summary>
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        private void Start()
        {
            if (_initializeOnStart)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Инициализирует колоду
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void Initialize()
        {
            _model = new DeckModel();
            _model.OnDeckEmpty += HandleDeckEmpty;
            _model.Initialize(_config.GameDeckType, _shuffleOnStart);

            if (_showTrumpCard && _trumpCardDisplay != null)
            {
                CardData? trump = TrumpCard;
                if (trump.HasValue)
                {
                    _trumpCardDisplay.SetData(trump.Value, true);
                    _trumpCardDisplay.gameObject.SetActive(true);
                }
            }

            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Перемешивает колоду
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void Shuffle()
        {
            _model?.Shuffle();
            OnShuffled?.Invoke();
        }

        /// <summary>
        /// Берёт карту из колоды
        /// </summary>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Компонент карты или null</returns>
        public CardComponent DrawCard(bool faceUp = true)
        {
            if (_model == null || _cardPrefab == null) return null;

            CardData? cardData = _model.Draw();
            if (!cardData.HasValue) return null;

            CardComponent card = Instantiate(_cardPrefab, SpawnPoint.position, Quaternion.identity);
            card.Config = _config;
            card.SetData(cardData.Value, faceUp);

            _activeCards.Add(card);
            OnCardDrawn?.Invoke(card);

            return card;
        }

        /// <summary>
        /// Берёт карту из колоды с анимацией
        /// </summary>
        /// <param name="targetPosition">Целевая позиция</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <param name="duration">Длительность перемещения</param>
        /// <returns>Компонент карты</returns>
        public async UniTask<CardComponent> DrawCardAsync(Vector3 targetPosition, bool faceUp = true, float duration = 0.3f)
        {
            CardComponent card = DrawCard(faceUp);
            if (card == null) return null;

            await card.MoveToAsync(targetPosition, duration);
            return card;
        }

        /// <summary>
        /// Берёт несколько карт
        /// </summary>
        /// <param name="count">Количество карт</param>
        /// <param name="faceUp">Показать лицом вверх</param>
        /// <returns>Список карт</returns>
        public List<CardComponent> DrawCards(int count, bool faceUp = true)
        {
            var cards = new List<CardComponent>();

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
        /// Возвращает карту в колоду
        /// </summary>
        /// <param name="card">Карта для возврата</param>
        /// <param name="toTop">В начало колоды</param>
        public void ReturnCard(CardComponent card, bool toTop = false)
        {
            if (card == null || _model == null) return;

            _activeCards.Remove(card);

            if (toTop)
            {
                _model.ReturnToTop(card.Data);
            }
            else
            {
                _model.ReturnToBottom(card.Data);
            }

            Destroy(card.gameObject);
        }

        /// <summary>
        /// Сбрасывает колоду
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void Reset()
        {
            foreach (var card in _activeCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _activeCards.Clear();

            _model?.Reset(_shuffleOnStart);
        }

        private void HandleDeckEmpty()
        {
            if (_trumpCardDisplay != null)
            {
                _trumpCardDisplay.gameObject.SetActive(false);
            }

            OnDeckEmpty?.Invoke();
        }

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.OnDeckEmpty -= HandleDeckEmpty;
            }
        }
    }
}

