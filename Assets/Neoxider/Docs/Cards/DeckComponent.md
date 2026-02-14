# DeckComponent

Компонент колоды карт для работы без кода.

---

## Описание

No-code обёртка над `DeckModel`. Позволяет настроить колоду через инспектор и использовать UnityEvent для реакции на события.
Поддерживает визуальную стопку, визуальное перемешивание и раздачу в руку из верхней карты стопки.

---

## Настройки в инспекторе

### Config

| Поле | Описание |
|------|----------|
| **Config** | Ссылка на DeckConfig |
| **Initialize On Start** | Автоинициализация при старте |
| **Shuffle On Start** | Перемешать при инициализации |

### Visual

| Поле | Описание |
|------|----------|
| **Spawn Point** | Точка появления карт |
| **Card Prefab** | Префаб CardComponent |
| **Visual Layout Type** | Общий тип layout (`CardLayoutType`) как в Hand/Board |
| **Visual Stack Board** | `BoardComponent` как визуальный контейнер стопки |
| **Spawn Visual On Initialize** | Автоспавн визуальной стопки при Initialize |
| **Stack Face Up** | Спавнить карты лицом вверх |
| **Stack Position Jitter** | Рандомный разброс позиции для живого вида |
| **Stack Rotation Jitter** | Рандомный разброс поворота |
| **Stack Step Y** | Шаг стопки по оси Y |
| **Stack Offset Position/Rotation** | Дополнительный общий offset для всей стопки |
| **Animation Config** | `CardAnimationConfig` для параметров shuffle/deal/stack |
| **Set Animation Config As Global** | Публикует конфиг Deck как глобальный fallback (`CardSettingsRuntime`) |

### Trump Display

| Поле | Описание |
|------|----------|
| **Show Trump Card** | Показывать козырную карту |
| **Trump Card Display** | CardComponent для отображения козыря |

---

## События (UnityEvent)

| Событие | Описание |
|---------|----------|
| `OnInitialized` | Колода инициализирована |
| `OnShuffled` | Колода перемешана |
| `OnDeckEmpty` | Колода опустела |
| `OnCardDrawn(CardComponent)` | Карта взята из колоды |
| `OnVisualStackChanged` | Визуальная стопка изменилась |
| `OnVisualStackBuilt` | Визуальная стопка построена |
| `OnShuffleVisualStarted(ShuffleVisualType)` | Старт визуального shuffle |
| `OnShuffleVisualCompleted` | Конец визуального shuffle |
| `OnCardDealt(CardComponent, HandComponent)` | Карта роздана в руку |

---

## Методы

### Initialize

```csharp
[Button]
public void Initialize();
```

Инициализирует колоду согласно конфигурации.

### Shuffle

```csharp
[Button]
public void Shuffle();
```

Перемешивает оставшиеся карты.

### DrawCard

```csharp
public CardComponent DrawCard(bool faceUp = true);
```

Берёт верхнюю карту из колоды.

### DrawCardAsync

```csharp
public UniTask<CardComponent> DrawCardAsync(
    Vector3 targetPosition, 
    bool faceUp = true, 
    float duration = 0.3f);
```

Берёт карту с анимацией перемещения.

### DrawCards

```csharp
public List<CardComponent> DrawCards(int count, bool faceUp = true);
```

Берёт несколько карт.

### ReturnCard

```csharp
public void ReturnCard(CardComponent card, bool toTop = false);
```

Возвращает карту в колоду.

### Reset

```csharp
[Button]
public void Reset();
```

Сбрасывает колоду в начальное состояние.

### BuildVisualStack / BuildVisualStackAsync

```csharp
[Button("Build Visual Stack")]
public void BuildVisualStack();
public UniTask BuildVisualStackAsync();
```

Строит визуальную стопку из текущей модели колоды на `VisualStackBoard`.
Для тестирования есть кнопка в инспекторе: `Build Visual Stack`.

### ShuffleVisual / ShuffleVisualAsync

```csharp
[Button("Shuffle Visual")]
public void ShuffleVisual(ShuffleVisualType type = ShuffleVisualType.Shake);
public UniTask ShuffleVisualAsync(ShuffleVisualType type, float? duration = null);
```

Перемешивает модель + синхронизирует визуальный порядок + запускает визуальный эффект (`Shake/Cut/Riffle`).
Для тестирования есть кнопка в инспекторе: `Shuffle Visual`.

### DealToHand / DealToHandAsync

```csharp
[Button("Deal To Hand")]
public void DealToHand(HandComponent hand, bool faceUp = true);
public UniTask<CardComponent> DealToHandAsync(HandComponent hand, bool faceUp, float? moveDuration = null);
```

Раздает верхнюю карту из визуальной стопки в руку с синхронизацией модели.
Для тестирования есть кнопка в инспекторе: `Deal To Hand`.

---

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Model` | `DeckModel` | Модель колоды |
| `RemainingCount` | `int` | Количество оставшихся карт |
| `IsEmpty` | `bool` | Пуста ли колода |
| `TrumpCard` | `CardData?` | Козырная карта |
| `TrumpSuit` | `Suit?` | Козырная масть |
| `SpawnPoint` | `Transform` | Точка спавна |
| `AnimationConfig` | `CardAnimationConfig` | Конфиг анимаций этой колоды |

---

## Пример использования

```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private DeckComponent _deck;
    [SerializeField] private HandComponent _playerHand;

    private async void Start()
    {
        _deck.Initialize();
        
        // Раздать 6 карт игроку
        for (int i = 0; i < 6; i++)
        {
            var card = _deck.DrawCard();
            await _playerHand.AddCardAsync(card);
        }
    }
}
```

### Пример: единый визуальный режим как у Hand/Board

```csharp
_deck.Initialize();
_deck.BuildVisualStack(); // через кнопку в инспекторе или код

// Те же режимы, что у руки и стола:
// Fan / Line / Stack / Grid / Slots / Scattered
// (Deck использует общий CardLayoutType)
await _deck.ShuffleVisualAsync(ShuffleVisualType.Shake);
await _deck.DealToHandAsync(_playerHand, faceUp: true);
```

---

## См. также

- [DeckConfig](./DeckConfig.md)
- [HandComponent](./HandComponent.md)
- [CardComponent](./CardComponent.md)

