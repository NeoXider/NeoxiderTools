# Poker

**What it is:** classic poker evaluation for standard 52-card hands — pure C# in `Scripts/Cards/Poker/` (namespace `Neo.Cards.Poker`), independent of scenes and views.

**Contents:**

| Page | Description |
|------|-------------|
| [PokerHandEvaluator](./PokerHandEvaluator.md) | Best five-card hand from 5–7 cards; allocation-free strength |
| [PokerHandResult](./PokerHandResult.md) | Combination, tie-break ranks, kickers and best hand |
| [PokerRules](./PokerRules.md) | Comparing hands, winners and split pots, Texas Hold'em, outs |

How to work with it: collect `CardData` from your hand/board components (or models) in a game controller, pass them to `PokerHandEvaluator` or `PokerRules`, and drive the scene from the result.

---

## Example

```csharp
IList<IEnumerable<CardData>> holes = new List<IEnumerable<CardData>> { playerHole, botHole };
List<int> winners = PokerRules.GetWinnersTexasHoldem(board.GetAllCardData(), holes);   // split pot = several indices
PokerHandResult mine = PokerRules.EvaluateTexasHoldem(board.GetAllCardData(), playerHole);
Debug.Log(mine);                                                                        // "Two Pair (K, 7) kickers: A"
```

## See also

- [Cards README](../README.md)
