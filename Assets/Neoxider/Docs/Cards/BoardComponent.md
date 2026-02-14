# BoardComponent

Компонент доски для общих карт.

---

## Описание

Управляет картами на игровом столе. Используется для:
- Общих карт в Texas Hold'em (флоп, тёрн, ривер)
- Бит в игре «Дурак»
- Любых зон с фиксированными слотами

---

## Настройки в инспекторе

### Layout

| Поле | Описание |
|------|----------|
| **Card Slots** | Массив Transform для позиций карт |
| **Slot Spacing** | Расстояние между слотами (при автогенерации) |
| **Auto Generate Slots** | Автоматически создать слоты |
| **Mode** | Режим стола: `Table` или `Beat` |
| **Layout Type** | Общий `CardLayoutType` (`Slots`, `Stack`, `Line`, `Fan`, `Grid`, `Scattered`) |
| **Layout Settings** | Параметры, используемые `CardLayoutCalculator` |
| **Stack Z Sorting** | Порядок слоев карт в стопке |
| **Animation Config** | Локальный `CardAnimationConfig` (optional override) |
| **Settings Source Deck** | Опциональный `DeckComponent`-источник для анимационного конфига |

### Settings

| Поле | Описание |
|------|----------|
| **Max Cards** | Максимум карт на доске |
| **Face Up** | Карты лицом вверх |

### Animation

| Поле | Описание |
|------|----------|
| **Place Duration** | Длительность анимации размещения |

---

## События (UnityEvent)

| Событие | Описание |
|---------|----------|
| `OnCardPlaced(CardComponent)` | Карта размещена |
| `OnBoardFull` | Доска заполнена |
| `OnBoardCleared` | Доска очищена |

---

## Методы

### PlaceCardAsync

```csharp
public UniTask PlaceCardAsync(CardComponent card, bool animate = true, bool overrideFaceUp = true);
```

Размещает карту в следующий свободный слот или перестраивает layout (в режимах без `Slots`).

### ArrangeCards

```csharp
[Button("Arrange Cards")]
public void ArrangeCards();
```

Перерасставляет все карты согласно текущему layout.

### PlaceCardsAsync

```csharp
public async UniTask PlaceCardsAsync(
    IEnumerable<CardComponent> cards, 
    bool animate = true, 
    float delayBetweenCards = 0.1f);
```

Размещает несколько карт с задержкой.

### RemoveCard

```csharp
public bool RemoveCard(CardComponent card);
public CardComponent RemoveAt(int index);
```

Удаляет карту с доски.

### Clear

```csharp
[Button]
public void Clear();
```

Очищает доску и уничтожает карты.

### ClearAndReturn

```csharp
public List<CardComponent> ClearAndReturn();
```

Очищает доску и возвращает карты (без уничтожения).

### FlipAll

```csharp
[Button]
public void FlipAll();

public async UniTask FlipAllAsync(float delayBetweenCards = 0.1f);
```

Переворачивает все карты.

### GetAllRanks

```csharp
public HashSet<Rank> GetAllRanks();
```

Возвращает все ранги карт на доске. Полезно для проверки подкидывания в «Дураке».

### GetAllCardData

```csharp
public List<CardData> GetAllCardData();
```

Возвращает данные всех карт.

---

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Cards` | `IReadOnlyList<CardComponent>` | Карты на доске |
| `Count` | `int` | Количество карт |
| `IsEmpty` | `bool` | Пуста ли доска |
| `IsFull` | `bool` | Заполнена ли доска |
| `CardSlots` | `Transform[]` | Слоты для карт |

---

## Пример: Texas Hold'em

```csharp
// Флоп — 3 карты
for (int i = 0; i < 3; i++)
{
    var card = deck.DrawCard(faceUp: true);
    await board.PlaceCardAsync(card);
}

// Тёрн — 1 карта
await board.PlaceCardAsync(deck.DrawCard(true));

// Ривер — 1 карта
await board.PlaceCardAsync(deck.DrawCard(true));

// Оценка рук
var communityCards = board.GetAllCardData();
var result = PokerRules.EvaluateTexasHoldem(communityCards, playerHoleCards);
```

## Пример: режим "Бита" (случайный разброс)

```csharp
// Board как "бита" с красивым хаотичным раскладом
// Mode = Beat
// Карты раскладываются через общий CardLayoutCalculator
foreach (var card in beatCards)
{
    await board.PlaceCardAsync(card, animate: true);
}
```

## Источник настроек (приоритет)

`BoardComponent` выбирает `CardAnimationConfig` в таком порядке:

1. Локальный `Animation Config` в самом Board
2. `Settings Source Deck.AnimationConfig`
3. Глобальный fallback: `CardSettingsRuntime.GlobalAnimationConfig`

---

## Пример: Дурак

```csharp
// Проверка: можно ли подкинуть карту
var ranksOnTable = board.GetAllRanks();
var validCards = playerHand.Model.GetCardsMatchingRanks(ranksOnTable);

if (validCards.Count > 0)
{
    // Можно подкинуть
}
```

---

## См. также

- [HandComponent](./HandComponent.md)
- [DeckComponent](./DeckComponent.md)
- [Poker](./Poker/README.md)

