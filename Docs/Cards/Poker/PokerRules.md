# PokerRules

**What it is:** static helpers (`Scripts/Cards/Poker/PokerRules.cs`, namespace `Neo.Cards.Poker`) for comparing poker hands, picking winners and split pots, Texas Hold'em evaluation and simple outs counting.

**How to use:** pass `CardData` collections (or already evaluated [PokerHandResult](./PokerHandResult.md) objects) and read the returned indices or results.

---

## Methods

| Method | Description |
|--------|-------------|
| `int CompareHands(PokerHandResult hand1, PokerHandResult hand2)` | Positive if the first wins, negative if the second, zero on a tie; `null` loses. |
| `int CompareHands(IEnumerable<CardData> cards1, IEnumerable<CardData> cards2)` | Evaluates both card sets first. |
| `List<int> GetWinners(IList<PokerHandResult> hands)` | Indices of the strongest hands (several on a split pot); `null` entries are skipped. |
| `List<int> GetWinners(IList<IEnumerable<CardData>> playerCards)` | Evaluates each player's cards; a `null` entry never wins. |
| `List<int> GetWinnersTexasHoldem(IEnumerable<CardData> communityCards, IList<IEnumerable<CardData>> playerHoleCards)` | Combines the board with each player's hole cards. |
| `PokerHandResult EvaluateTexasHoldem(IEnumerable<CardData> communityCards, IEnumerable<CardData> holeCards)` | Best hand of one player. |
| `bool IsWinner(int playerIndex, IList<PokerHandResult> hands)` | Whether the player wins or splits. |
| `PokerHandResult GetBestHand(IEnumerable<PokerHandResult> hands)` | Strongest result or `null`. |
| `int CountOuts(IEnumerable<CardData> currentHand, PokerCombination target, IEnumerable<CardData> remainingDeck)` | Number of single cards that bring the hand to at least `target`; uses the allocation-free strength path per candidate. |

```csharp
int outs = PokerRules.CountOuts(knownCards, PokerCombination.Flush, unseenCards);
float flushChance = outs / (float)unseenCards.Count;
```

## See also

- [Poker README](./README.md)
- [PokerHandEvaluator](./PokerHandEvaluator.md)
