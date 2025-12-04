# DeckConfig

ScriptableObject для конфигурации колоды карт со спрайтами.

---

## Создание

**ПКМ в Project → Create → Neo → Cards → Deck Config**

---

## Настройки в инспекторе

| Поле | Описание |
|------|----------|
| **Deck Type** | Тип колоды: 36, 52 или 54 карты |
| **Back Sprite** | Спрайт рубашки карты |
| **Hearts** | Спрайты червей (от младшей к старшей) |
| **Diamonds** | Спрайты бубен |
| **Clubs** | Спрайты треф |
| **Spades** | Спрайты пик |
| **Red Joker** | Спрайт красного джокера (для 54 карт) |
| **Black Joker** | Спрайт чёрного джокера |

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
List<CardData> allCards = config.GenerateDeck();
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
| `DeckType` | `DeckType` | Тип колоды |
| `BackSprite` | `Sprite` | Спрайт рубашки |
| `Hearts` | `IReadOnlyList<Sprite>` | Спрайты червей |
| `Diamonds` | `IReadOnlyList<Sprite>` | Спрайты бубен |
| `Clubs` | `IReadOnlyList<Sprite>` | Спрайты треф |
| `Spades` | `IReadOnlyList<Sprite>` | Спрайты пик |
| `RedJoker` | `Sprite` | Красный джокер |
| `BlackJoker` | `Sprite` | Чёрный джокер |

---

## См. также

- [DeckComponent](./DeckComponent.md)
- [CardData](./CardData.md)

