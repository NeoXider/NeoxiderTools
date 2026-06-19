# HandComponent

**Что это:** компонент руки: набор карт, раскладка (Fan, Line, Stack, Grid и др.), анимации. Обёртка над HandModel. Файл: `Scripts/Cards/Components/HandComponent.cs`.

**Как использовать:** добавить на объект руки, задать Layout Type и параметры раскладки; добавлять/удалять карты через API компонента; привязать HandView для визуала. См. секции ниже.

`HandComponent` управляет сценовыми `CardComponent` и ограничивает их через инспекторное поле **Max Cards**. Если нужна чистая C# модель без визуала, используйте `HandModel`: у него есть `Capacity` (0 = без лимита), `RemainingCapacity`, `IsFull`, `TryAdd(...)` и `AddRangeUntilFull(...)` для CCG hand limit, лавок, draft tray и market row.

---

## Настройки в инспекторе

### Layout

| Поле | Описание |
|------|----------|
| **Layout Type** | Общий `CardLayoutType`: Fan, Line, Stack, Grid, Slots, Scattered |
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
| `IsFull` | `bool` | Заполнена ли рука по инспекторному `Max Cards` компонента |
| `LayoutType` | `CardLayoutType` | Тип раскладки |
| `LegacyLayoutType` | `HandLayoutType` | Устаревшее свойство для совместимости со старыми сценами |

### Runtime-модель HandModel

```csharp
var hand = new HandModel { Capacity = 5 };

if (!hand.TryAdd(cardData))
{
    ConvertOverflowToDust(cardData);
}

int added = hand.AddRangeUntilFull(drawnCards);
int overflow = drawnCards.Count - added;
```

- `Capacity = 0` сохраняет старое unlimited-поведение.
- `TryAdd(...)` возвращает `false`, если рука заполнена.
- `Add(...)` и `AddRange(...)` остаются строгими и бросают исключение при переполнении finite hand.
- `RemainingCapacity` удобно показывать в UI или использовать при массовой выдаче наград.

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

### Slots (Слоты)

Режим с фиксированными позициями (обычно используется на `BoardComponent`).

### Scattered (Разброс)

Случайное распределение карт в заданном радиусе.

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

