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
| `RemoveCardAsync(CardComponent card, bool animate = true)` | Removes a scene card, detaches click listeners, and updates the model by index. |
| `RemoveAtAsync(int index, bool animate = true)` | Removes a card by visual/model index. |
| `DrawFirst()` / `DrawRandom()` | Removes and returns a card. |
| `SortByRankAsync(...)` / `SortBySuitAsync(...)` | Sorts both model and visual list, then rearranges. |
| `GetCardsThatCanBeat(CardData attackCard, Suit? trump)` | Finds Durak-compatible defense cards. |
| `Clear()` | Destroys all scene cards and clears the model. |

## HandModel Capacity

`HandComponent` has its own Inspector **Max Cards** limit for visible `CardComponent` objects. For pure C# logic without scene cards, use `HandModel` directly:

```csharp
var hand = new HandModel { Capacity = 5 };

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

## See Also

- [Cards README](./README.md)
- [DeckComponent](./DeckComponent.md)
- [BoardComponent](./BoardComponent.md)
- [CardData](./CardData.md)
