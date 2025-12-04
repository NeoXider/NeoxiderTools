# Пример: Игра «Пьяница»

Пошаговое руководство по созданию классической карточной игры «Пьяница» с использованием модуля Neo.Cards.

---

## Правила игры

1. Колода делится поровну между двумя игроками
2. Каждый ход игроки выкладывают верхнюю карту
3. У кого карта старше — забирает обе карты себе в низ колоды
4. При равенстве карт — «спор»: выкладывается ещё по одной карте рубашкой вверх, затем ещё по одной лицом — кто старше, забирает все
5. Побеждает тот, кто соберёт все карты

---

## Шаг 1: Подготовка сцены

### Иерархия объектов

```
DrunkardGame
├── Canvas
│   ├── PlayerDeck (DeckComponent)
│   ├── OpponentDeck (DeckComponent)
│   ├── PlayerCard (CardComponent)
│   ├── OpponentCard (CardComponent)
│   ├── PlayButton (Button)
│   ├── PlayerCountText (TMP_Text)
│   └── OpponentCountText (TMP_Text)
└── GameManager (DrunkardGame.cs)
```

### Настройка DeckConfig

1. Создайте `DeckConfig` через **Create → Neo → Cards → Deck Config**
2. Выберите тип колоды `Standard36` или `Standard52`
3. Назначьте спрайты карт и рубашки

---

## Шаг 2: Создание скрипта игры

```csharp
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame
{
    public class DrunkardGame : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private DeckConfig _deckConfig;
        [SerializeField] private CardComponent _cardPrefab;

        [Header("Player")]
        [SerializeField] private Transform _playerDeckPosition;
        [SerializeField] private Transform _playerCardPosition;
        [SerializeField] private TMP_Text _playerCountText;

        [Header("Opponent")]
        [SerializeField] private Transform _opponentDeckPosition;
        [SerializeField] private Transform _opponentCardPosition;
        [SerializeField] private TMP_Text _opponentCountText;

        [Header("UI")]
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private float _cardMoveDuration = 0.3f;
        [SerializeField] private float _roundDelay = 1f;

        private Queue<CardData> _playerCards = new();
        private Queue<CardData> _opponentCards = new();
        private CardComponent _playerCardView;
        private CardComponent _opponentCardView;
        private bool _isPlaying;

        private void Start()
        {
            _playButton.onClick.AddListener(PlayRound);
            InitializeGame();
        }

        /// <summary>
        /// Инициализирует игру: создаёт колоду и раздаёт карты
        /// </summary>
        public void InitializeGame()
        {
            _playerCards.Clear();
            _opponentCards.Clear();
            _resultText.text = "";

            // Создаём и перемешиваем колоду
            var deck = new DeckModel();
            deck.Initialize(_deckConfig.DeckType, shuffle: true);

            // Раздаём карты поровну
            bool toPlayer = true;
            while (!deck.IsEmpty)
            {
                CardData? card = deck.Draw();
                if (!card.HasValue) break;

                if (toPlayer)
                    _playerCards.Enqueue(card.Value);
                else
                    _opponentCards.Enqueue(card.Value);

                toPlayer = !toPlayer;
            }

            UpdateUI();
            _playButton.interactable = true;
        }

        /// <summary>
        /// Разыгрывает один раунд
        /// </summary>
        public async void PlayRound()
        {
            if (_isPlaying) return;
            if (_playerCards.Count == 0 || _opponentCards.Count == 0)
            {
                EndGame();
                return;
            }

            _isPlaying = true;
            _playButton.interactable = false;

            // Берём карты
            CardData playerCard = _playerCards.Dequeue();
            CardData opponentCard = _opponentCards.Dequeue();

            // Показываем карты
            await ShowCards(playerCard, opponentCard);

            // Определяем победителя раунда
            int comparison = playerCard.CompareTo(opponentCard);

            if (comparison > 0)
            {
                // Игрок выиграл
                _resultText.text = "Вы выиграли раунд!";
                _playerCards.Enqueue(playerCard);
                _playerCards.Enqueue(opponentCard);
            }
            else if (comparison < 0)
            {
                // Противник выиграл
                _resultText.text = "Противник выиграл раунд!";
                _opponentCards.Enqueue(opponentCard);
                _opponentCards.Enqueue(playerCard);
            }
            else
            {
                // Спор!
                _resultText.text = "Спор!";
                await HandleWar(playerCard, opponentCard);
            }

            await UniTask.Delay((int)(_roundDelay * 1000));

            // Убираем карты
            await HideCards();

            UpdateUI();
            CheckGameEnd();

            _isPlaying = false;
            _playButton.interactable = true;
        }

        /// <summary>
        /// Обрабатывает ситуацию «спора» при равных картах
        /// </summary>
        private async UniTask HandleWar(CardData card1, CardData card2)
        {
            var warPile = new List<CardData> { card1, card2 };

            while (true)
            {
                // Проверяем, достаточно ли карт для спора
                if (_playerCards.Count < 2 || _opponentCards.Count < 2)
                {
                    // Не хватает карт — делим пополам
                    foreach (var card in warPile)
                    {
                        if (warPile.IndexOf(card) % 2 == 0)
                            _playerCards.Enqueue(card);
                        else
                            _opponentCards.Enqueue(card);
                    }
                    return;
                }

                // Кладём по одной карте рубашкой вверх
                warPile.Add(_playerCards.Dequeue());
                warPile.Add(_opponentCards.Dequeue());

                // Открываем по одной карте
                CardData playerWarCard = _playerCards.Dequeue();
                CardData opponentWarCard = _opponentCards.Dequeue();
                warPile.Add(playerWarCard);
                warPile.Add(opponentWarCard);

                await ShowCards(playerWarCard, opponentWarCard);
                await UniTask.Delay(500);

                int comparison = playerWarCard.CompareTo(opponentWarCard);

                if (comparison > 0)
                {
                    _resultText.text = "Вы выиграли спор!";
                    foreach (var card in warPile)
                        _playerCards.Enqueue(card);
                    return;
                }
                else if (comparison < 0)
                {
                    _resultText.text = "Противник выиграл спор!";
                    foreach (var card in warPile)
                        _opponentCards.Enqueue(card);
                    return;
                }

                // Снова равенство — продолжаем спор
                _resultText.text = "Снова спор!";
                await UniTask.Delay(500);
            }
        }

        /// <summary>
        /// Показывает карты на столе
        /// </summary>
        private async UniTask ShowCards(CardData playerCard, CardData opponentCard)
        {
            // Создаём или переиспользуем карты
            if (_playerCardView == null)
            {
                _playerCardView = Instantiate(_cardPrefab, _playerDeckPosition.position, Quaternion.identity);
                _playerCardView.Config = _deckConfig;
            }

            if (_opponentCardView == null)
            {
                _opponentCardView = Instantiate(_cardPrefab, _opponentDeckPosition.position, Quaternion.identity);
                _opponentCardView.Config = _deckConfig;
            }

            // Устанавливаем данные
            _playerCardView.SetData(playerCard, faceUp: false);
            _opponentCardView.SetData(opponentCard, faceUp: false);

            _playerCardView.gameObject.SetActive(true);
            _opponentCardView.gameObject.SetActive(true);

            // Анимация: перемещаем на стол
            var movePlayer = _playerCardView.MoveToAsync(_playerCardPosition.position, _cardMoveDuration);
            var moveOpponent = _opponentCardView.MoveToAsync(_opponentCardPosition.position, _cardMoveDuration);
            await UniTask.WhenAll(movePlayer, moveOpponent);

            // Переворачиваем
            var flipPlayer = _playerCardView.FlipAsync();
            var flipOpponent = _opponentCardView.FlipAsync();
            await UniTask.WhenAll(flipPlayer, flipOpponent);
        }

        /// <summary>
        /// Скрывает карты со стола
        /// </summary>
        private async UniTask HideCards()
        {
            if (_playerCardView != null)
            {
                _playerCardView.gameObject.SetActive(false);
                _playerCardView.transform.position = _playerDeckPosition.position;
            }

            if (_opponentCardView != null)
            {
                _opponentCardView.gameObject.SetActive(false);
                _opponentCardView.transform.position = _opponentDeckPosition.position;
            }

            await UniTask.Yield();
        }

        /// <summary>
        /// Обновляет UI
        /// </summary>
        private void UpdateUI()
        {
            _playerCountText.text = $"Ваши карты: {_playerCards.Count}";
            _opponentCountText.text = $"Карты противника: {_opponentCards.Count}";
        }

        /// <summary>
        /// Проверяет окончание игры
        /// </summary>
        private void CheckGameEnd()
        {
            if (_playerCards.Count == 0 || _opponentCards.Count == 0)
            {
                EndGame();
            }
        }

        /// <summary>
        /// Завершает игру
        /// </summary>
        private void EndGame()
        {
            _playButton.interactable = false;

            if (_playerCards.Count > _opponentCards.Count)
            {
                _resultText.text = "🎉 Вы победили!";
            }
            else if (_opponentCards.Count > _playerCards.Count)
            {
                _resultText.text = "😢 Противник победил!";
            }
            else
            {
                _resultText.text = "🤝 Ничья!";
            }
        }

        /// <summary>
        /// Перезапускает игру
        /// </summary>
        public void RestartGame()
        {
            if (_playerCardView != null) Destroy(_playerCardView.gameObject);
            if (_opponentCardView != null) Destroy(_opponentCardView.gameObject);
            _playerCardView = null;
            _opponentCardView = null;

            InitializeGame();
        }
    }
}
```

---

## Шаг 3: Настройка в Unity

### 3.1 Создайте префаб карты

1. Создайте UI Image с компонентом `CardComponent`
2. Настройте размер карты (рекомендуется 100x140)
3. Добавьте компонент `CanvasGroup` для возможных эффектов
4. Сохраните как префаб

### 3.2 Настройте сцену

1. Создайте Canvas с режимом Screen Space - Overlay
2. Добавьте пустые объекты для позиций:
   - `PlayerDeckPosition` — позиция колоды игрока
   - `OpponentDeckPosition` — позиция колоды противника
   - `PlayerCardPosition` — куда выкладывается карта игрока
   - `OpponentCardPosition` — куда выкладывается карта противника
3. Добавьте UI элементы:
   - Кнопка «Играть»
   - Текст счётчика карт игрока
   - Текст счётчика карт противника
   - Текст результата

### 3.3 Назначьте ссылки

1. Создайте GameObject с компонентом `DrunkardGame`
2. Назначьте все сериализованные поля в инспекторе

---

## Шаг 4: Дополнительные улучшения

### Автоматическая игра

```csharp
[SerializeField] private bool _autoPlay;
[SerializeField] private float _autoPlayDelay = 0.5f;

private async void AutoPlayLoop()
{
    while (_autoPlay && _playerCards.Count > 0 && _opponentCards.Count > 0)
    {
        PlayRound();
        await UniTask.WaitUntil(() => !_isPlaying);
        await UniTask.Delay((int)(_autoPlayDelay * 1000));
    }
}
```

### Звуковые эффекты

```csharp
[SerializeField] private AudioSource _audioSource;
[SerializeField] private AudioClip _cardFlipSound;
[SerializeField] private AudioClip _winSound;
[SerializeField] private AudioClip _loseSound;

private void PlaySound(AudioClip clip)
{
    if (_audioSource != null && clip != null)
        _audioSource.PlayOneShot(clip);
}
```

### Сохранение статистики

```csharp
private int _wins;
private int _losses;

private void EndGame()
{
    if (_playerCards.Count > _opponentCards.Count)
    {
        _wins++;
        PlayerPrefs.SetInt("Drunkard_Wins", _wins);
    }
    else
    {
        _losses++;
        PlayerPrefs.SetInt("Drunkard_Losses", _losses);
    }
    PlayerPrefs.Save();
}
```

---

## Результат

После выполнения всех шагов у вас будет рабочая игра «Пьяница» с:

- ✅ Раздачей карт
- ✅ Анимацией выкладывания и переворота карт
- ✅ Логикой сравнения карт
- ✅ Обработкой «спора» при равных картах
- ✅ Подсчётом карт
- ✅ Определением победителя

---

## См. также

- [CardData](../CardData.md) — структура данных карты
- [DeckConfig](../DeckConfig.md) — конфигурация колоды
- [README](../README.md) — обзор модуля Cards


