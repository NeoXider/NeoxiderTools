# DeckComponent

Компонент колоды карт для работы без кода.

---

## Описание

No-code обёртка над `DeckModel`. Позволяет настроить колоду через инспектор и использовать UnityEvent для реакции на события.

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
public async UniTask<CardComponent> DrawCardAsync(
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

---

## См. также

- [DeckConfig](./DeckConfig.md)
- [HandComponent](./HandComponent.md)
- [CardComponent](./CardComponent.md)

