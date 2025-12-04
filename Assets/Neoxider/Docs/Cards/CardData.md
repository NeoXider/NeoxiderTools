# CardData

Неизменяемая структура данных игральной карты.

---

## Описание

`CardData` — readonly struct, представляющий одну карту. Поддерживает сравнение, проверку на козырь и работу с джокерами.

---

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Suit` | `Suit` | Масть карты |
| `Rank` | `Rank` | Ранг (достоинство) карты |
| `IsJoker` | `bool` | Является ли картой джокером |
| `IsRedJoker` | `bool` | Красный джокер (true) или чёрный (false) |

---

## Создание

```csharp
// Обычная карта
var aceOfSpades = new CardData(Suit.Spades, Rank.Ace);

// Джокер
var redJoker = CardData.CreateJoker(isRed: true);
var blackJoker = CardData.CreateJoker(isRed: false);
```

---

## Методы сравнения

### CompareTo (для «Пьяницы»)

```csharp
int result = card1.CompareTo(card2);
// > 0 — card1 старше
// < 0 — card1 младше
// = 0 — равны
```

### Beats (для «Дурака»)

```csharp
bool beats = defendCard.Beats(attackCard, trump: Suit.Hearts);
```

**Логика:**
- Козырь бьёт не-козырь
- Не-козырь не бьёт козырь
- Одинаковая масть — сравнение по рангу
- Разные масти без козыря — false

### CanCover

Алиас для `Beats()`:

```csharp
bool canCover = card.CanCover(attackCard, trump);
```

### HasSameRank / HasSameSuit

```csharp
// Для подкидывания в «Дураке»
if (card.HasSameRank(tableCard))
    Debug.Log("Можно подкинуть");
```

---

## Операторы

```csharp
// Сравнение по рангу
if (card1 > card2) { }
if (card1 >= card2) { }
if (card1 < card2) { }
if (card1 <= card2) { }

// Равенство
if (card1 == card2) { }
if (card1 != card2) { }
```

---

## Строковое представление

```csharp
var card = new CardData(Suit.Hearts, Rank.Queen);

card.ToString();         // "Q♥"
card.ToRussianString();  // "Дама Червы"
```

---

## См. также

- [Suit / Rank](./Enums.md)
- [DeckModel](./DeckModel.md)

