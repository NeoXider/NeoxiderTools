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
    ///     Компонент доски для общих карт (например, 5 карт на столе в Texas Hold'em или биты в "Дураке")
    /// </summary>
    public class BoardComponent : MonoBehaviour
    {
        [Header("Layout")] [SerializeField] private Transform[] _cardSlots;

        [SerializeField] private float _slotSpacing = 80f;
        [SerializeField] private bool _autoGenerateSlots = true;

        [Header("Settings")] [SerializeField] private int _maxCards = 5;

        [SerializeField] private bool _faceUp;

        [Tooltip("Допустимо ли автоматически увеличивать максимальное количество карт при возврате")] [SerializeField]
        private bool _autoExpandCapacity = true;

        [Header("Sources (for reset)")]
        [Tooltip("Руки, из которых нужно забирать карты при сбросе/рестарте")]
        [SerializeField]
        private List<HandComponent> _handSources = new();

        [Tooltip("Другие BoardComponent, которые нужно очищать в этот Board")] [SerializeField]
        private List<BoardComponent> _boardSources = new();

        [Tooltip("Дополнительные корневые объекты, из которых будут собраны CardComponent")] [SerializeField]
        private List<Transform> _extraRoots = new();

        [Header("Debug")] [Tooltip("Последний собранный список карт при RestoreAllSourcesToBoard")] [SerializeField]
        private List<CardComponent> _lastCollectedCards = new();

        [Tooltip("Карты, созданные при первоначальном спавне (Initial Board)")] [SerializeField]
        private List<CardComponent> _initialSpawnedCards = new();

        [Header("Animation")] [SerializeField] private float _placeDuration = 0.3f;

        public UnityEvent<CardComponent> OnCardPlaced;

        public UnityEvent OnBoardFull;
        public UnityEvent OnBoardCleared;

        [SerializeField] private List<CardComponent> _cards = new();

        /// <summary>
        ///     Карты на доске
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        ///     Количество карт на доске
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Пуста ли доска
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Заполнена ли доска
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        ///     Слоты для карт
        /// </summary>
        public Transform[] CardSlots => _cardSlots;

        private void Awake()
        {
            if (_autoGenerateSlots && (_cardSlots == null || _cardSlots.Length == 0))
            {
                GenerateSlots();
            }
        }

        /// <summary>
        ///     Размещает карту на доске
        /// </summary>
        /// <param name="card">Карта для размещения</param>
        /// <param name="animate">Анимировать</param>
        /// <param name="overrideFaceUp">Если true - применить настройку FaceUp из компонента, если false - оставить как есть</param>
        public async UniTask PlaceCardAsync(CardComponent card, bool animate = true, bool overrideFaceUp = true)
        {
            if (card == null || IsFull)
            {
                return;
            }

            int slotIndex = _cards.Count;
            Transform slot = GetSlot(slotIndex);

            card.transform.SetParent(transform, true);

            if (overrideFaceUp)
            {
                card.IsFaceUp = _faceUp;
            }

            _cards.Add(card);

            if (animate && slot != null)
            {
                await card.MoveToAsync(slot.position, _placeDuration);
            }
            else if (slot != null)
            {
                card.transform.position = slot.position;
            }

            OnCardPlaced?.Invoke(card);

            if (IsFull)
            {
                OnBoardFull?.Invoke();
            }
        }

        /// <summary>
        ///     Размещает карту синхронно
        /// </summary>
        /// <param name="card">Карта</param>
        public void PlaceCard(CardComponent card)
        {
            PlaceCardAsync(card, false).Forget();
        }

        /// <summary>
        ///     Принудительно размещает карту, расширяя вместимость при необходимости (используется для рестарта).
        /// </summary>
        /// <param name="card">Карта</param>
        /// <param name="animate">Анимировать</param>
        /// <param name="overrideFaceUp">Учитывать настройку FaceUp</param>
        private async UniTask ForcePlaceCardAsync(CardComponent card, bool animate = false, bool overrideFaceUp = true)
        {
            if (card == null)
            {
                return;
            }

            if (_autoExpandCapacity && IsFull)
            {
                _maxCards = Mathf.Max(_maxCards + 1, _cards.Count + 1);
                if (_autoGenerateSlots)
                {
                    GenerateSlots();
                }
            }

            await PlaceCardAsync(card, animate, overrideFaceUp);
        }

        /// <summary>
        ///     Регистрирует карту как созданную при первоначальном спавне (Initial Board).
        /// </summary>
        /// <param name="card">Карта</param>
        public void RegisterInitialCard(CardComponent card)
        {
            if (card == null)
            {
                return;
            }

            if (!_initialSpawnedCards.Contains(card))
            {
                _initialSpawnedCards.Add(card);
            }
        }

        /// <summary>
        ///     Размещает несколько карт
        /// </summary>
        /// <param name="cards">Карты для размещения</param>
        /// <param name="animate">Анимировать</param>
        /// <param name="delayBetweenCards">Задержка между картами</param>
        public async UniTask PlaceCardsAsync(IEnumerable<CardComponent> cards, bool animate = true,
            float delayBetweenCards = 0.1f)
        {
            foreach (CardComponent card in cards)
            {
                if (IsFull)
                {
                    break;
                }

                await PlaceCardAsync(card, animate);

                if (delayBetweenCards > 0)
                {
                    await UniTask.Delay((int)(delayBetweenCards * 1000));
                }
            }
        }

        /// <summary>
        ///     Удаляет карту с доски
        /// </summary>
        /// <param name="card">Карта для удаления</param>
        /// <returns>true если карта была удалена</returns>
        public bool RemoveCard(CardComponent card)
        {
            if (card == null)
            {
                return false;
            }

            bool removed = _cards.Remove(card);
            if (removed)
            {
                card.transform.SetParent(null, true);
            }

            return removed;
        }

        /// <summary>
        ///     Удаляет карту по индексу
        /// </summary>
        /// <param name="index">Индекс карты</param>
        /// <returns>Удалённая карта или null</returns>
        public CardComponent RemoveAt(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return null;
            }

            CardComponent card = _cards[index];
            _cards.RemoveAt(index);
            card.transform.SetParent(null, true);

            return card;
        }

        /// <summary>
        ///     Очищает доску
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void Clear()
        {
            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
            OnBoardCleared?.Invoke();
        }

        /// <summary>
        ///     Очищает доску и возвращает карты
        /// </summary>
        /// <returns>Список карт</returns>
        public List<CardComponent> ClearAndReturn()
        {
            List<CardComponent> cards = new(_cards);

            foreach (CardComponent card in cards)
            {
                if (card != null)
                {
                    card.transform.SetParent(null, true);
                }
            }

            _cards.Clear();
            OnBoardCleared?.Invoke();

            return cards;
        }

        /// <summary>
        ///     Переворачивает все карты
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void FlipAll()
        {
            FlipAllAsync().Forget();
        }

        /// <summary>
        ///     Возвращает все карты из указанных источников (рук, других досок и дополнительных корней) обратно на эту доску.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void RestoreAllSourcesToBoard()
        {
            RestoreAllSourcesToBoardAsync().Forget();
        }

        /// <summary>
        ///     Возвращает все карты из указанных источников (рук, других досок и дополнительных корней) обратно на эту доску.
        /// </summary>
        public async UniTask RestoreAllSourcesToBoardAsync()
        {
            List<CardComponent> collected = new();
            HashSet<CardComponent> seen = new();

            // Снимаем карты с рук
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

            // Снимаем карты с других досок
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

            // Собираем дополнительные корни
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

            // Возвращаем на эту доску
            foreach (CardComponent card in collected)
            {
                await ForcePlaceCardAsync(card);
            }

            _lastCollectedCards.Clear();
            _lastCollectedCards.AddRange(collected);

            OnBoardCleared?.Invoke();
        }

        /// <summary>
        ///     Переворачивает все карты с анимацией
        /// </summary>
        /// <param name="delayBetweenCards">Задержка между переворотами</param>
        public async UniTask FlipAllAsync(float delayBetweenCards = 0.1f)
        {
            foreach (CardComponent card in _cards)
            {
                await card.FlipAsync();

                if (delayBetweenCards > 0)
                {
                    await UniTask.Delay((int)(delayBetweenCards * 1000));
                }
            }
        }

        /// <summary>
        ///     Возвращает все ранги карт на доске
        /// </summary>
        /// <returns>Набор рангов</returns>
        public HashSet<Rank> GetAllRanks()
        {
            HashSet<Rank> ranks = new();

            foreach (CardComponent card in _cards)
            {
                if (!card.Data.IsJoker)
                {
                    ranks.Add(card.Data.Rank);
                }
            }

            return ranks;
        }

        /// <summary>
        ///     Возвращает все данные карт на доске
        /// </summary>
        /// <returns>Список данных карт</returns>
        public List<CardData> GetAllCardData()
        {
            List<CardData> data = new();

            foreach (CardComponent card in _cards)
            {
                data.Add(card.Data);
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

        private void GenerateSlots()
        {
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
    }
}