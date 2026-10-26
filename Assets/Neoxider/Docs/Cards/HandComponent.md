# HandComponent

**Purpose:** scene-facing card hand component. It owns `CardComponent` children, lays them out, animates add/remove operations, and mirrors card data into a backing `HandModel`.

**Use it when:** a Unity scene needs a visible player hand, bench, tray, or card row with Inspector-controlled layout and UnityEvents.

## Setup

1. Add `HandComponent` to the hand root object.
2. Configure **Layout Type** and spacing/arc/grid fields.
3. Set **Max Cards** for the visual component limit.
4. Add cards through `AddCardAsync(...)`, `AddCard(...)`, deck deal helpers, or your own scene code.
5. Subscribe UI to `OnCardCountChanged`, `OnCardAdded`, `OnCardRemoved`, `OnCardClicked`, or `OnHandChanged`.

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
| `MaxCards` | Read-only Inspector card limit (parity with `BoardComponent.MaxCards`). |
| `LayoutType` | Current layout type; assigning it rearranges cards. |
| `AddCardAsync(CardComponent card, bool animate = true)` | Adds a scene card and optionally animates it into the hand. |
| `TryAddCardAsync(CardComponent card, bool animate = true)` | Returns `UniTask<bool>`: true for accepted membership, false for null/full/duplicate rejection without mutation. |
| `TryAddCardAsync(CardComponent card, bool animate, CancellationToken cancellation)` | Cancels layout motion while preserving already accepted membership. |
| `RemoveCardAsync(CardComponent card, bool animate = true)` | Removes a scene card, detaches click listeners, and updates the model by index. |
| `RemoveAtAsync(int index, bool animate = true)` | Removes a card by visual/model index. |
| `DrawFirst()` / `DrawRandom()` | Removes and returns a card. |
| `SortByRankAsync(...)` / `SortBySuitAsync(...)` | Sorts both model and visual list, then rearranges. |
| `GetCardsThatCanBeat(CardData attackCard, Suit? trump)` | Finds Durak-compatible defense cards. |
| `Clear()` | Destroys all scene cards and clears the model. |

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

## See Also

- [Cards README](./README.md)
- [DeckComponent](./DeckComponent.md)
- [BoardComponent](./BoardComponent.md)
- [CardData](./CardData.md)
