# HandComponent

Компонент руки игрока для работы без кода.

---

## Описание

No-code обёртка над `HandModel`. Управляет набором карт с автоматической раскладкой и анимациями.

---

## Настройки в инспекторе

### Layout

| Поле | Описание |
|------|----------|
| **Layout Type** | Тип раскладки: Fan, Line, Stack, Grid |
| **Spacing** | Расстояние между картами |
| **Arc Angle** | Угол дуги (для Fan) |
| **Arc Radius** | Радиус дуги (для Fan) |

### Grid Settings

| Поле | Описание |
|------|----------|
| **Grid Columns** | Количество колонок |
| **Grid Row Spacing** | Расстояние между рядами |

### Limits

| Поле | Описание |
|------|----------|
| **Max Cards** | Максимум карт в руке |

### Card Order

| Поле | Описание |
|------|----------|
| **Add To Bottom** | Если true - новые карты добавляются под низ (sibling index 0). Для игры "Пьяница" - включить. |

### Animation

| Поле | Описание |
|------|----------|
| **Arrange Duration** | Длительность анимации расстановки |
| **Arrange Ease** | Тип easing |

---

## События (UnityEvent)

| Событие | Параметр | Описание |
|---------|----------|----------|
| `OnCardCountChanged` | `int` | Количество карт изменилось |
| `OnCardAdded` | `CardComponent` | Карта добавлена |
| `OnCardRemoved` | `CardComponent` | Карта удалена |
| `OnCardClicked` | `CardComponent` | Клик по карте |
| `OnHandChanged` | — | Рука изменилась |

### Пример подключения в инспекторе

```
OnCardCountChanged (int):
  → CardCountText.SetText (Dynamic int)
```

---

## Методы

### AddCardAsync

```csharp
public async UniTask AddCardAsync(CardComponent card, bool animate = true);
```

Добавляет карту в руку.

### RemoveCardAsync

```csharp
public async UniTask RemoveCardAsync(CardComponent card, bool animate = true);
```

Удаляет карту из руки.

### DrawFirst / DrawRandom

```csharp
public CardComponent DrawFirst();   // Берёт первую карту
public CardComponent DrawRandom();  // Берёт случайную карту
```

Берёт карту из руки и удаляет её. Возвращает `null` если рука пуста.

### SortByRank / SortBySuit

```csharp
[Button]
public void SortByRank(bool ascending = true);

[Button]
public void SortBySuit(bool ascending = true);
```

Сортирует карты с анимацией.

### GetCardsThatCanBeat

```csharp
public List<CardComponent> GetCardsThatCanBeat(CardData attackCard, Suit? trump);
```

Находит карты, которыми можно побить атакующую.

### Clear

```csharp
[Button]
public void Clear();
```

Очищает руку и уничтожает карты.

---

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Model` | `HandModel` | Модель руки |
| `Cards` | `IReadOnlyList<CardComponent>` | Карты в руке |
| `Count` | `int` | Количество карт |
| `IsEmpty` | `bool` | Пуста ли рука |
| `IsFull` | `bool` | Заполнена ли рука |
| `LayoutType` | `HandLayoutType` | Тип раскладки |

---

## Типы раскладки

### Fan (Веер)

Карты расположены дугой, как в руке игрока.

### Line (Линия)

Карты в ряд с перекрытием.

### Stack (Стопка)

Карты друг на друге со смещением.

### Grid (Сетка)

Карты в несколько рядов.

---

## Пример использования

```csharp
// Игрок выбирает карту для атаки
handComponent.OnCardClicked.AddListener(card =>
{
    if (isMyTurn)
    {
        PlayCard(card);
    }
});

// Найти карты для защиты
var validDefense = handComponent.GetCardsThatCanBeat(attackCard.Data, trumpSuit);
foreach (var card in validDefense)
{
    card.SetHighlighted(true);
}
```

---

## См. также

- [DeckComponent](./DeckComponent.md)
- [BoardComponent](./BoardComponent.md)
- [CardData](./CardData.md)

