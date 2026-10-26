# Poker Hand Evaluator

`PokerHandEvaluator.Evaluate(IEnumerable<CardData> cards)` returns the strongest five-card result. It evaluates all five-card subsets when more cards are supplied; five to seven standard cards is the usual poker input.

```csharp
PokerHandResult result = PokerHandEvaluator.Evaluate(communityCards.Concat(holeCards));
PokerCombination combination = result.Combination;
IReadOnlyList<CardData> winningCards = result.BestHand;
```

Jokers are ignored. At least five remaining cards are required. Custom cards and invalid suit/rank enum values throw `ArgumentException`; custom card groups and sort values have no classic-poker interpretation. Supply a legal standard deck without duplicate physical cards. Larger collections require combinatorial work; keep inputs bounded in gameplay code.

For Inspector-driven scenes, obtain `CardData` from hand/board components in your game controller and pass it to this pure C# evaluator. The evaluator does not require a particular view or scene component. `PokerRules` compares results, selects winners, and combines community/hole cards.

- [Poker README](./README.md)
