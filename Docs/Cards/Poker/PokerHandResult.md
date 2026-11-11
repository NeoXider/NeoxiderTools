# PokerHandResult

**What it is:** immutable result of a poker evaluation (`Scripts/Cards/Poker/PokerHandResult.cs`, namespace `Neo.Cards.Poker`), created by [PokerHandEvaluator](./PokerHandEvaluator.md).

**How to use:** read `Combination` for UI and payouts, compare two results with `CompareTo`/`IsStrongerThan`/`IsEqualTo`, or pass a list of results to `PokerRules.GetWinners`.

---

## Properties

| Property | Description |
|----------|-------------|
| `Combination` | `PokerCombination` from `HighCard` to `RoyalFlush`. |
| `CombinationRanks` | Ranks that form the combination, in tie-break order (e.g. full house: trips rank, pair rank; flush: all five ranks). |
| `Kickers` | Remaining tie-break ranks, highest first. |
| `BestHand` | The five cards of the best hand, highest rank first. |

## Methods

| Method | Description |
|--------|-------------|
| `int CompareTo(PokerHandResult other)` | Positive when this hand is stronger; compares combination, then combination ranks, then kickers. `null` is weaker than any hand. |
| `bool IsStrongerThan(PokerHandResult other)` | `CompareTo(other) > 0`. |
| `bool IsEqualTo(PokerHandResult other)` | Equal strength (split pot). |
| `string ToString()` | Short English description, e.g. `Pair (Q) kickers: A, 9, 4`. |

## See also

- [Poker README](./README.md)
- [PokerRules](./PokerRules.md)
