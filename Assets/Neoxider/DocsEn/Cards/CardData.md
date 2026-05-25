# CardData

`CardData` is a readonly value type that represents a single playing card. It stores suit, rank, joker flags, and comparison helpers for common card-game logic. File location is within the `Cards` module, namespace `Neo.Cards`.

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
var aceOfSpades = new CardData(Suit.Spades, Rank.Ace);
var redJoker = CardData.CreateJoker(isRed: true);
var blackJoker = CardData.CreateJoker(isRed: false);
var minion = CardData.CreateCustom("neutral_minion_01", "Neutral Minion", sortValue: 3, group: "Neutral");
```

Custom cards compare by `SortValue`, then by `CustomId`. `HasSameRank(...)` maps to equal `SortValue`, and `HasSameSuit(...)` maps to equal non-empty `Group`.

`customId` must be stable and non-empty because it is used for equality, hashing, deck validation, and sprite lookup.

## Comparison helpers

| API | Description |
|-----|-------------|
| `CompareTo(CardData other)` | Rank-based comparison useful for games like War/Drunkard. |
| `Beats(CardData other, Suit trump)` | Trump-aware comparison for games like Durak. |
| `CanCover(CardData other, Suit trump)` | Alias of `Beats(...)`. |
| `HasSameRank(CardData other)` | Checks matching ranks. |
| `HasSameSuit(CardData other)` | Checks matching suits. |

## Operators

`CardData` supports comparison and equality operators, so it can be used directly in gameplay rules and sorting.

## String helpers

- `ToString()` returns a compact card string.
- `ToLongEnglishString()` returns a human-readable English representation (e.g. `"Queen of Hearts"`).

## See also

- [README](./README.md)
- [Russian Cards docs](../../Docs/Cards/README.md)
- [Tools/Components](../Tools/Components/README.md)
