# PokerHandEvaluator

**What it is:** static evaluator (`Scripts/Cards/Poker/PokerHandEvaluator.cs`, namespace `Neo.Cards.Poker`) that finds the strongest five-card poker hand among standard cards.

**How to use:**
1. Call `PokerHandEvaluator.Evaluate(cards)` to get a full [PokerHandResult](./PokerHandResult.md).
2. In hot loops (AI, Monte-Carlo equity, outs) call `EvaluateStrength(cards)` and compare the returned integers.

---

## Methods

| Method | Description |
|--------|-------------|
| `PokerHandResult Evaluate(IEnumerable<CardData> cards)` | Best five-card result. Five to seven cards is the usual input; more cards are evaluated combinatorially. |
| `int EvaluateStrength(IReadOnlyList<CardData> cards)` | Allocation-free strength of the same best hand. Higher wins, equal values split — exactly the order of `PokerHandResult.CompareTo`. |
| `PokerCombination GetCombination(int strength)` | Decodes the combination from a strength value. |

```csharp
PokerHandResult result = PokerHandEvaluator.Evaluate(communityCards.Concat(holeCards));
PokerCombination combination = result.Combination;
IReadOnlyList<CardData> winningCards = result.BestHand;

int strength = PokerHandEvaluator.EvaluateStrength(sevenCards);   // no allocations
bool atLeastFlush = PokerHandEvaluator.GetCombination(strength) >= PokerCombination.Flush;
```

## Rules

- Jokers are ignored. At least five remaining cards are required, otherwise `ArgumentException` is thrown.
- Custom cards and invalid suit/rank enum values throw `ArgumentException`; custom groups and sort values have no classic-poker meaning. A null collection throws `ArgumentNullException`.
- The wheel (A-2-3-4-5) is a five-high straight; an ace-high straight flush is a royal flush.
- When several five-card subsets are equally strong, the first one in input order is reported as `BestHand`, whose cards are listed highest rank first (equal ranks keep input order).
- Supply a legal deck without duplicate physical cards for real poker; duplicates are evaluated but do not form special combinations.

## Performance

Every five-card subset is scored without allocations; only the winning subset is turned into a `PokerHandResult`. A seven-card `Evaluate` costs one small input copy plus the result object, and `EvaluateStrength` allocates nothing.

## See also

- [Poker README](./README.md)
- [PokerRules](./PokerRules.md)
