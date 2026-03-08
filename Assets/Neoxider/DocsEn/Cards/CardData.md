# CardData

`CardData` is a readonly value type that represents a single playing card. It stores suit, rank, joker flags, and comparison helpers for common card-game logic. File location is within the `Cards` module, namespace `Neo.Cards`.

## Data it stores

| Property | Description |
|----------|-------------|
| `Suit` | Card suit. |
| `Rank` | Card rank. |
| `IsJoker` | Whether the card is a joker. |
| `IsRedJoker` | Whether the joker is red. |

## Creating cards

```csharp
var aceOfSpades = new CardData(Suit.Spades, Rank.Ace);
var redJoker = CardData.CreateJoker(isRed: true);
var blackJoker = CardData.CreateJoker(isRed: false);
```

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
- `ToRussianString()` returns a human-readable Russian representation.

## See also

- [README](./README.md)
- [Russian Cards docs](../../Docs/Cards/README.md)
- [Tools/Components](../Tools/Components/README.md)
