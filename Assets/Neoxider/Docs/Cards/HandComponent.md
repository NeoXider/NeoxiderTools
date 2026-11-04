# HandComponent

**What it is:** scene-facing card hand (`MonoBehaviour`, `Scripts/Cards/Components/HandComponent.cs`, namespace `Neo.Cards`). It owns `CardComponent` children, lays them out, animates add/remove operations, and mirrors card data into a backing `HandModel`. Use it for a visible player hand, bench, tray, or card row with Inspector-controlled layout and UnityEvents.

**How to use:**
1. Add `HandComponent` to the hand root object.
2. Configure **Layout Type** and spacing/arc/grid fields.
3. Set **Max Cards** for the visual component limit.
4. Add cards through `AddCardAsync(...)`, `AddCard(...)`, deck deal helpers, or your own scene code.
5. Subscribe UI to `OnCardCountChanged`, `OnCardAdded`, `OnCardRemoved`, `OnCardClicked`, or `OnHandChanged`; wire the **Sort By Rank/Suit** buttons or call `SortAsync(...)` from C#.

---

## Inspector Fields

| Field | Description |
|-------|-------------|
| **Layout Type** | `CardLayoutType`: Fan, Line, Stack, Grid, Slots, Scattered. |
| **Spacing** | Distance between cards. |
| **Arc Angle** | Fan angle. |
| **Arc Radius** | Fan radius. |
| **Grid Columns** | Column count for Grid layout. |
| **Grid Row Spacing** | Distance between Grid rows. |
| **Max Cards** | Maximum visible `CardComponent` count for this scene component. |
| **Add To Bottom** | Inserts new cards at sibling index 0; useful for War/Drunkard-style piles. |
| **Arrange Duration** | Layout animation duration. |
| **Arrange Ease** | DOTween easing for layout moves. |

## Runtime API

| Member | Description |
|--------|-------------|
| `Model` | Backing `HandModel` data model. |
| `Cards` | Read-only list of scene `CardComponent` objects. |
| `Count` / `IsEmpty` / `IsFull` | Scene component state; `IsFull` uses **Max Cards**. |
| `Contains(CardComponent card)` | Whether this exact scene card is a member. |
| `MaxCards` | Read-only Inspector card limit (parity with `BoardComponent.MaxCards`). |
| `LayoutType` | Current layout type; assigning it rearranges cards. |
| `AddCardAsync(CardComponent card, bool animate = true)` | Adds a scene card and optionally animates it into the hand. |
| `TryAddCardAsync(CardComponent card, bool animate = true)` | Returns `UniTask<bool>`: true for accepted membership, false for null/full/duplicate rejection without mutation. |
| `TryAddCardAsync(CardComponent card, bool animate, CancellationToken cancellation)` | Cancels layout motion while preserving already accepted membership. |
| `RemoveCardAsync(CardComponent card, bool animate = true)` | Removes a scene card, detaches click listeners, and updates the model by index. The card keeps its world pose: a world card goes to the scene root, a UI card stays under the hand's root Canvas so it keeps rendering while it moves on. |
| `RemoveAtAsync(int index, bool animate = true)` | Removes a card by visual/model index. |
| `DrawFirst()` / `DrawRandom()` | Removes and returns a card (`DrawRandom` uses `UnityEngine.Random`). |
| `DrawRandom(System.Random random)` | Same, with a caller-provided (seedable) random source. |
| `SortByRankAsync(...)` / `SortBySuitAsync(...)` | Stable sort of model and visual list, then rearranges. |
| `SortAsync(IComparer<CardData> comparer, bool animate = true)` / `SortAsync(Comparison<CardData>, bool)` | Stable sort by any rule (for example `CardComparer.ByValueDescending` for custom cards). |
| `GetCardsThatCanBeat(CardData attackCard, Suit? trump)` | Finds Durak-compatible defense cards. |
| `Clear()` | Destroys all scene cards and clears the model. |

Sorting is stable: cards with equal keys keep their current order, and the visual list and `Model` receive the identical permutation, so `RemoveAtAsync(index)` keeps removing the matching model entry after any sort. The view list is sorted before the model, so `Model.OnHandChanged` listeners already see the final order.

```csharp
await hand.SortAsync(CardComparer.ByValueDescending, animate: true);   // custom cards by SortValue
await hand.SortAsync((a, b) => string.CompareOrdinal(a.Group, b.Group)); // any comparison
CardComponent picked = hand.DrawRandom(new System.Random(seed));       // reproducible pick
```

Adding the same `CardComponent` twice is ignored without firing another add/count/change event. Distinct scene cards with equal `CardData` remain independent copies. Transfer a card by removing it from its old hand/board before adding it to the new owner; changing its parent alone does not update the old list. Use the component methods to keep `Cards`, `Model`, and click subscriptions synchronized.

```csharp
bool accepted = await hand.TryAddCardAsync(card, animate: true);
if (!accepted)
{
    // The caller still owns this card and chooses its next destination.
}
```

Admission commits before animation. A true result records that admission even if another caller subsequently removes the card during layout. Add/remove/clear and new layouts cancel obsolete hand-owned tweens. Layout uses a membership snapshot and cannot finish by moving a card already transferred elsewhere. `OnCardAdded` is emitted after layout only while that same admission still belongs to this hand; clear/remove suppress stale add notifications. Existing `AddCardAsync` remains a wrapper for callers that do not need the result.

For a deck-created card, successful model admission releases the deck's cleanup ownership, then reparents the card, before public `Model.OnCardAdded` or component add callbacks. Native hierarchy listeners see committed component/model membership; public model listeners also see the final parent. Resetting/reinitializing that deck from a parent/children change callback, an admission callback, or immediately after admission in `OnCardDrawn` cannot destroy the accepted hand member. Normal and bottom insertion use the same order. A rejected admission performs no handoff or reparenting. Pure `HandModel` use and its public event order remain unchanged.

## HandModel Capacity

`HandComponent` has its own Inspector **Max Cards** limit for visible `CardComponent` objects. For pure C# logic without scene cards, use `HandModel` directly:

```csharp
HandModel hand = new HandModel { Capacity = 5 };

if (!hand.TryAdd(cardData))
{
    ConvertOverflowToDust(cardData);
}

int added = hand.AddRangeUntilFull(drawnCards);
```

- `Capacity = 0` keeps the legacy unlimited hand behavior.
- `TryAdd(...)` returns `false` when a finite hand is full.
- `Add(...)` and `AddRange(...)` remain strict and throw on finite-hand overflow.
- `RemainingCapacity` and `IsFull` are intended for UI badges, reward overflow conversion, and bulk draw/recruit flows.
- `AddRange(...)` snapshots input before adding, including `hand.Cards`; capacity rejection leaves the hand unchanged. `AddRangeUntilFull(...)` streams an independent input and stops before advancing beyond remaining capacity.
- `Sort(IComparer<CardData>)` / `Sort(Comparison<CardData>)` sort the model stably and raise `OnHandChanged` once.
- Queries (`Contains`, `ContainsRank`, `ContainsSuit`, `IndexOf`, `GetLowestCard`, `GetHighestCard`) allocate nothing; `GetLowestCard`/`GetHighestCard` return the first of equal candidates and null for joker-only hands.

## Layout cost

Every add or remove re-arranges the whole hand. An immediate arrange (`animate: false`, or `ArrangeCardsAsync(false)`) reuses the hand's member and pose buffers and creates no cancellation source, so a warm call does not allocate; an animated arrange still creates its tweens and one linked cancellation source. Adding n cards one by one still costs O(n²) transform writes in total, so keep hands to playable sizes and use a `BoardComponent` batch (`PlaceCardsImmediate`) for whole-deck piles.

## See Also

- [Cards README](./README.md)
- [DeckComponent](./DeckComponent.md)
- [BoardComponent](./BoardComponent.md)
- [CardData](./CardData.md)
