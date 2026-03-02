# Cards — модуль карточных игр

**Что это:** модуль для игральных карт: MVP (Model, View, Presenter), компоненты колоды/руки/доски, покер, дурак, пьяница. Скрипты в `Scripts/Cards/`.

**Навигация:** [← К Docs](../README.md) · оглавление — раздел «Документация компонентов» ниже

---

## Возможности

- **Гибкая архитектура MVP** — Model, View, Presenter
- **Компоненты** — настройка в инспекторе и UnityEvent
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
| `View` | CardView, CardViewUniversal, DeckView, HandView, CardViewAnimationTemplates — визуализация и анимации |
| `Presenter` | Связь Model ↔ View |
| `Components` | No-code обёртки для инспектора |
| `Config` | DeckConfig, CardLayoutSettings, CardAnimationConfig |
| `Poker` | Покерные комбинации и правила |
| `Utils` | CardComparer, CardLayoutCalculator — сортировка и раскладки |

---

## Что переиспользовать в разных карточных играх

В любых карточных играх (классика, CCG, deckbuilder) удобно переиспользовать:

- **CardViewAnimationTemplates** — готовые анимации (Bounce, Pulse, Shake, Highlight, FlyIn, Idle); вызывать из любой вью по [CardViewUniversal](View/CardViewUniversal.md#переиспользование-шаблонов).
- **CardLayoutCalculator** и **CardLayoutSettings** — расчёт позиций и поворотов для Fan, Line, Grid, Stack и др.
- **HandView / IHandView** — контейнер карт с раскладкой; для нескольких зон — несколько HandView.
- **MoveToAsync / FlipAsync** — перемещение и переворот, не привязаны к типу данных.

Подробнее: [Interfaces](Interfaces.md) и [CustomCardViewGuide](View/CustomCardViewGuide.md) (игры с собственной моделью карты).

---

## Документация компонентов

- [CardData](./CardData.md) — структура данных карты
- [DeckConfig](./DeckConfig.md) — конфигурация колоды
- [CardComponent](./CardComponent.md) — компонент карты
- [DeckComponent](./DeckComponent.md) — компонент колоды
- [HandComponent](./HandComponent.md) — компонент руки
- [BoardComponent](./BoardComponent.md) — компонент доски
- **Интерфейсы и переиспользование:** [Interfaces](./Interfaces.md) — ICardView, ICardDisplayMode, ICardViewAnimations; что переиспользовать в разных карточных играх
- **View (MVP):** [CardView](./View/CardView.md), [CardViewUniversal](./View/CardViewUniversal.md), [DeckView](./View/DeckView.md), [HandView](./View/HandView.md)
- [Custom Card View Guide](./View/CustomCardViewGuide.md) — пошаговая своя реализация карты
- [Poker](./Poker/README.md) — покерные правила

---

## Готовые игры

| Игра | Компонент | Описание |
|------|-----------|----------|
| [Пьяница](./Examples/Drunkard.md) | `DrunkardGame` | Классическая карточная игра (War Card Game) |

### DrunkardGame — быстрый старт

```csharp
// Игра настраивается через инспектор и UnityEvent.
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

