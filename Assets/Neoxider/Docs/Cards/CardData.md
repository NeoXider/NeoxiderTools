# CardData

**Что это:** неизменяемая `readonly struct` одной карты. Поддерживает классические карты (`Suit` + `Rank`), джокеров и custom-карты для TCG/декбилдеров/настольных игр.

Файл: `Assets/Neoxider/Scripts/Cards/Core/Data/CardData.cs`

## Поля

| Поле | Описание |
|------|----------|
| `Suit` | Масть классической карты. |
| `Rank` | Ранг классической карты. |
| `IsJoker`, `IsRedJoker` | Джокер и его цвет. |
| `IsCustom` | Карта создана через custom-id модель. |
| `CustomId` | Стабильный id для нестандартных игр. |
| `DisplayName` | Отображаемое имя custom-карты. |
| `SortValue` | Универсальное сравнимое значение: сила, стоимость, rarity order и т.д. |
| `Group` | Группа custom-карты: фракция, класс, цвет, suit-like ключ. |

## Создание

```csharp
var aceOfSpades = new CardData(Suit.Spades, Rank.Ace);

var redJoker = CardData.CreateJoker(isRed: true);

var fireball = CardData.CreateCustom(
    customId: "spell.fireball",
    displayName: "Fireball",
    sortValue: 4,
    group: "Mage");
```

`CreateCustom` требует непустой стабильный `customId`. Это основной путь для Hearthstone-like, ability cards, board-game cards и других нестандартных колод.

## Сравнение

`CompareTo`:

- для классических карт сравнивает `Rank`;
- джокер старше обычной карты;
- для custom-карт сначала сравнивает `SortValue`, затем `CustomId`.

`Beats(other, trump)`:

- для классических карт работает как Durak-style покрытие с optional trump;
- для custom-карт обе карты должны быть custom, `Group` должен совпадать или быть пустым, а `SortValue` должен быть выше;
- смешивание custom и classic возвращает `false`.

```csharp
bool canCover = defendCard.CanCover(attackCard, trump: Suit.Hearts);
bool stronger = customCard.Beats(otherCustomCard, trump: null);
```

## Использование во views

`CardData` передается в card views и board/hand/deck компоненты как универсальная модель:

```csharp
cardView.SetData(cardData, faceUp: true);
```

Для production card games держите правила игры отдельно от view: `CardData` описывает карту, а игровые сервисы решают, как она ходит, атакует, покупается или комбинируется.

## См. также

- [DeckComponent](./DeckComponent.md)
- [Cards README](./README.md)
