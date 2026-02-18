# Cards — модуль карточных игр

Полноценный модуль для работы с игральными картами в MVP архитектуре. Поддерживает игры: покер, дурак, пьяница и другие.

---

## Возможности

- **Гибкая архитектура MVP** — Model, View, Presenter
- **No-code компоненты** — настройка через инспектор и UnityEvent
- **Типы колод** — 36, 52, 54 карты
- **Сравнение карт** — по рангу, с учётом козыря
- **Покерные комбинации** — от пары до роял-флеша
- **Анимации** — переворот, перемещение через DOTween + UniTask
- **Раскладки руки** — веер, линия, стопка, сетка

---

## Быстрый старт

### 1. Создайте конфигурацию колоды

1. ПКМ в Project → **Create → Neoxider → Cards → Deck Config**
2. Выберите тип колоды (36/52/54)
3. Назначьте спрайты для каждой масти (от младшей к старшей)
4. Укажите спрайт рубашки

### 2. Настройте сцену

```
Hierarchy:
├── Deck (DeckComponent)
├── PlayerHand (HandComponent)
├── OpponentHand (HandComponent)
└── Board (BoardComponent)  // опционально
```

### 3. Базовый код

```csharp
// Инициализация колоды
deckComponent.Initialize();

// Раздача карт
for (int i = 0; i < 6; i++)
{
    CardComponent card = deckComponent.DrawCard();
    await playerHand.AddCardAsync(card);
}

// Проверка: можно ли побить карту
Suit? trump = deckComponent.TrumpSuit;
var validCards = playerHand.GetCardsThatCanBeat(attackCard.Data, trump);
```

---

## Структура модуля

| Папка | Описание |
|-------|----------|
| `Core/Enums` | Suit, Rank, DeckType |
| `Core/Data` | CardData — неизменяемая структура карты |
| `Model` | DeckModel, HandModel — логика без визуала |
| `View` | CardView, DeckView, HandView — визуализация |
| `Presenter` | Связь Model ↔ View |
| `Components` | No-code обёртки для инспектора |
| `Config` | DeckConfig, CardLayoutSettings, CardAnimationConfig |
| `Poker` | Покерные комбинации и правила |
| `Utils` | CardComparer — сортировка |

---

## Документация компонентов

- [CardData](./CardData.md) — структура данных карты
- [DeckConfig](./DeckConfig.md) — конфигурация колоды
- [CardComponent](./CardComponent.md) — компонент карты (no-code)
- [DeckComponent](./DeckComponent.md) — компонент колоды
- [HandComponent](./HandComponent.md) — компонент руки
- [BoardComponent](./BoardComponent.md) — компонент доски
- [Poker](./Poker/README.md) — покерные правила

---

## Готовые игры

| Игра | Компонент | Описание |
|------|-----------|----------|
| [Пьяница](./Examples/Drunkard.md) | `DrunkardGame` | Классическая карточная игра (War Card Game) |

### DrunkardGame — быстрый старт

```csharp
// Игра настраивается через инспектор без кода!
// Просто подключите UnityEvent к UI элементам:

// OnPlayerCardCountChanged (int) → TMP_Text.SetText
// OnOpponentCardCountChanged (int) → TMP_Text.SetText
// OnPlayerWin → WinPanel.SetActive(true)
// OnOpponentWin → LosePanel.SetActive(true)

// Для хода игрока — Button.OnClick → DrunkardGame.Play
// Для рестарта — Button.OnClick → DrunkardGame.RestartGame
```

---

## Сравнение карт

### По рангу (для игры «Пьяница»)

```csharp
if (card1 > card2)
    Debug.Log("Первая карта старше");

// Или через CompareTo
int result = card1.CompareTo(card2);
```

### С учётом козыря (для игры «Дурак»)

```csharp
Suit trump = Suit.Hearts;

// Проверка: бьёт ли карта другую
if (defendCard.Beats(attackCard, trump))
    Debug.Log("Карта побита");

// Или через CanCover (алиас)
if (defendCard.CanCover(attackCard, trump))
    Debug.Log("Можно покрыть");
```

### Логика сравнения с козырем

1. Козырь всегда бьёт не-козырь
2. Не-козырь не может побить козырь
3. При одинаковой масти — сравнение по рангу
4. Разные масти без козыря — не бьёт

---

## Типы раскладки (единые для Hand/Board/Deck)

```csharp
handComponent.LayoutType = CardLayoutType.Fan;       // Веер
handComponent.LayoutType = CardLayoutType.Line;      // Линия
handComponent.LayoutType = CardLayoutType.Stack;     // Стопка
handComponent.LayoutType = CardLayoutType.Grid;      // Сетка
boardComponent.LayoutType = CardLayoutType.Slots;    // Фиксированные слоты
boardComponent.LayoutType = CardLayoutType.Scattered;// Случайный разброс
```

---

## Покер

```csharp
using Neo.Cards.Poker;

// Оценка руки (5-7 карт)
var result = PokerHandEvaluator.Evaluate(cards);
Debug.Log(result.Combination); // Pair, Flush, FullHouse...

// Texas Hold'em
var winners = PokerRules.GetWinnersTexasHoldem(
    communityCards,  // 5 карт на столе
    playerHoleCards  // по 2 карты у каждого игрока
);
```

Подробнее: [Poker/README.md](./Poker/README.md)

---

## Зависимости

- **UniTask** — асинхронные операции
- **DOTween** — анимации

---

## См. также

- [Tools/Spawner](../Tools/Spawner/Spawner.md) — спавн объектов
- [Save](../Save/README.md) — сохранение состояния игры

