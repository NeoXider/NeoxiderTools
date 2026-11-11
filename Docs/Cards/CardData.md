# CardData

**What it is:** readonly value type (`struct`, `Scripts/Cards/Core/Data/CardData.cs`, namespace `Neo.Cards`) for one card: a standard suit/rank card, a joker, or a custom card with id, display name, sort value and group. It carries the comparison helpers used by models, comparers and rules.

**How to use:**
1. Create standard cards with `new CardData(suit, rank)`, jokers with `CardData.CreateJoker(isRed)`, and any other card with `CardData.CreateCustom(id, displayName, sortValue, group)`.
2. Store them in `DeckModel`/`HandModel` or assign them to views with `SetData(...)`.
3. Compare with `CompareTo`, `Beats`, `HasSameRank`/`HasSameSuit`, or sort with `CardComparer`.

---

## Data it stores

| Property | Description |
|----------|-------------|
| `Suit` | Card suit. |
| `Rank` | Card rank. |
| `IsJoker` | Whether the card is a joker. |
| `IsRedJoker` | Whether the joker is red. |
| `IsCustom` | Whether the card uses custom id/group/value data instead of built-in suit/rank data. |
| `CustomId` | Stable custom identifier for TCG/board-game cards. |
| `DisplayName` | Optional display name for custom cards. |
| `SortValue` | Generic comparable value for custom card rules. |
| `Group` | Optional custom grouping key such as faction, class, color, or lane. |

## Creating cards

```csharp
CardData aceOfSpades = new CardData(Suit.Spades, Rank.Ace);
CardData redJoker = CardData.CreateJoker(isRed: true);
CardData blackJoker = CardData.CreateJoker(isRed: false);
CardData minion = CardData.CreateCustom("neutral_minion_01", "Neutral Minion", sortValue: 3, group: "Neutral");
```

Standard cards get `SortValue = (int)Rank` and `Group` = the English suit name (`"Hearts"`, ...); jokers get `SortValue = int.MaxValue` and `Group = "Joker"`. Creating standard cards allocates nothing, so decks, AI search and poker outs can build cards in bulk.

Custom cards compare by `SortValue`, then by `CustomId`. `HasSameRank(...)` maps to equal `SortValue`, and `HasSameSuit(...)` maps to equal non-empty `Group`.

`customId` must be stable and non-empty because it is used for equality, hashing, deck validation, and sprite lookup.

## Comparison helpers

| API | Description |
|-----|-------------|
| `CompareTo(CardData other)` | Rank-based comparison (jokers above every rank; custom cards by `SortValue`, then `CustomId`). Useful for War/Drunkard. |
| `Beats(CardData other, Suit? trump)` | Trump-aware comparison for games like Durak; custom cards beat lower `SortValue` in the same (or empty) group. |
| `CanCover(CardData other, Suit? trump)` | Alias of `Beats(...)`. |
| `HasSameRank(CardData other)` | Checks matching ranks (custom: equal `SortValue`). |
| `HasSameSuit(CardData other)` | Checks matching suits (custom: equal non-empty `Group`). |

Equality: standard cards by suit and rank, jokers by color, custom cards by `CustomId` only. Two equal values are interchangeable copies; scene components distinguish physical copies by object identity.

## Operators

`CardData` supports comparison and equality operators, so it can be used directly in gameplay rules and sorting.

## String helpers

- `ToString()` returns a compact card string.
- `ToLongEnglishString()` returns a human-readable English representation (e.g. `"Queen of Hearts"`).

## See also

- [Cards README](./README.md)
- [CardComparer and sorting](./README.md#deterministic-shuffling-and-sorting)
- [DeckConfig](./DeckConfig.md)
