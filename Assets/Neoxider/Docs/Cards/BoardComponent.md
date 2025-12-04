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
public async UniTask PlaceCardAsync(CardComponent card, bool animate = true);
```

Размещает карту в следующий свободный слот.

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

