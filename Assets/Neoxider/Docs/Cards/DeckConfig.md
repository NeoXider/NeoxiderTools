# DeckConfig

ScriptableObject для конфигурации колоды карт со спрайтами.

---

## Создание

**ПКМ в Project → Create → Neoxider → Cards → Deck Config**

---

## Настройки в инспекторе

| Поле | Описание |
|------|----------|
| **Deck Type** | Тип колоды для спрайтов: сколько карт загружено (36, 52 или 54) |
| **Game Deck Type** | Тип колоды для игры: сколько карт использовать (по умолчанию 54) |
| **Back Sprite** | Спрайт рубашки карты |
| **Hearts** | Спрайты червей (от младшей к старшей) |
| **Diamonds** | Спрайты бубен |
| **Clubs** | Спрайты треф |
| **Spades** | Спрайты пик |
| **Red Joker** | Спрайт красного джокера (для 54 карт) |
| **Black Joker** | Спрайт чёрного джокера |

---

## DeckType vs GameDeckType

| Параметр | Описание | Пример |
|----------|----------|--------|
| **DeckType** | Сколько спрайтов загружено | Загружены все 52 карты |
| **GameDeckType** | Сколько карт использовать в игре | Играем только с 36 картами |

### Примеры использования

```
DeckType = Standard52 (все спрайты от 2 до A)
GameDeckType = Standard36 → В игре будут только карты от 6 до A
GameDeckType = Standard52 → В игре будут все карты от 2 до A
GameDeckType = Standard54 → В игре будут все карты + 2 джокера
```

### Ограничения

- `GameDeckType` не может требовать карты, которых нет в `DeckType`
- Например: если `DeckType = Standard36`, то `GameDeckType` не может быть `Standard52` (нет спрайтов для карт 2-5)

---

## Кастомный редактор

Редактор DeckConfig включает:

- **Превью спрайтов** — сетка с миниатюрами по мастям
- **Валидация** — зелёная/красная индикация количества спрайтов
- **Кнопка Validate** — проверка всей конфигурации

---

## Порядок спрайтов

### Для колоды 52 карты

```
Hearts[0]  = 2♥
Hearts[1]  = 3♥
...
Hearts[12] = A♥
```

### Для колоды 36 карт

```
Hearts[0] = 6♥
Hearts[1] = 7♥
...
Hearts[8] = A♥
```

---

## Методы

### GetSprite

```csharp
Sprite sprite = config.GetSprite(cardData);
```

### GenerateDeck

```csharp
// Генерирует колоду на основе GameDeckType
List<CardData> gameCards = config.GenerateDeck();

// Генерирует колоду указанного типа
List<CardData> cards36 = config.GenerateDeck(DeckType.Standard36);
List<CardData> cards52 = config.GenerateDeck(DeckType.Standard52);
```

### Validate

```csharp
if (config.Validate(out List<string> errors))
{
    Debug.Log("Конфигурация валидна");
}
else
{
    foreach (var error in errors)
        Debug.LogError(error);
}
```

---

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `DeckType` | `DeckType` | Тип колоды для спрайтов |
| `GameDeckType` | `DeckType` | Тип колоды для игры |
| `BackSprite` | `Sprite` | Спрайт рубашки |
| `Hearts` | `IReadOnlyList<Sprite>` | Спрайты червей |
| `Diamonds` | `IReadOnlyList<Sprite>` | Спрайты бубен |
| `Clubs` | `IReadOnlyList<Sprite>` | Спрайты треф |
| `Spades` | `IReadOnlyList<Sprite>` | Спрайты пик |
| `RedJoker` | `Sprite` | Красный джокер |
| `BlackJoker` | `Sprite` | Чёрный джокер |

---

## Пример: Универсальная конфигурация

Создайте один DeckConfig с полным набором спрайтов (52 или 54), и используйте его для разных игр:

```
DeckConfig "UniversalDeck"
├── DeckType = Standard52 (или Standard54)
├── GameDeckType = Standard54 (по умолчанию)
└── Все спрайты загружены
```

Затем в конкретной игре:
- **Пьяница** → `GameDeckType = Standard36`
- **Покер** → `GameDeckType = Standard52`
- **С джокерами** → `GameDeckType = Standard54`

---

## См. также

- [DeckComponent](./DeckComponent.md)
- [CardData](./CardData.md)
- [Пьяница](./Examples/Drunkard.md)
