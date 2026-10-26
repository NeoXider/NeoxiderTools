# BoardComponent

`BoardComponent` is a reusable card zone for table cards, discard piles, market rows, lanes, and other board-style layouts. It stores placed `CardComponent` instances, applies layout, and can force newly placed cards face up or face down.

## Setup

- Add the component via the Unity menu.
- Set `Max Cards` for the zone size.
- Use slot transforms for fixed positions, or layout settings for dynamic arrangements.
- Call `SetCapacity(...)` and `SetFaceUp(...)` from custom game setup when the same prefab is reused for different zones.

## Main API

| API | Description |
|-----|-------------|
| `Cards` / `Count` | Current cards on the board. |
| `MaxCards` | Current board capacity. |
| `FaceUp` | Default face state applied on placement when enabled. |
| `CanPlaceCard(CardComponent card)` | Checks null, capacity, and whether this exact scene card is already present. |
| `SetCapacity(int maxCards, bool regenerateSlots = true)` | Changes capacity for custom boards/zones. |
| `SetFaceUp(bool faceUp, bool applyToExisting = false)` | Changes default face state and optionally updates current cards. |
| `PlaceCardAsync(...)` | Places a card with optional animation. |
| `Clear()` / `ClearAndReturn()` | Removes cards by destroying or detaching them. |

Repeated placement of the same `CardComponent` is ignored and does not raise `OnCardPlaced` again. Separate scene cards with equal `CardData` can occupy different slots. Remove a card from its previous zone before placement; parenting alone does not update that zone's list.

Every layout works from a fixed membership snapshot. Add/remove/clear, capacity changes and a newer arrange pass cancel obsolete motion before a detached card can be moved by its former board. Remaining members are arranged using their current order, including slot indices after removal. `OnCardPlaced`/`OnBoardFull` are published after arrangement only while that exact admission still belongs to the board. `ClearAndReturn()` cancels board-owned motion before handing objects back to the caller.

In Table/Slots mode, a serialized empty slot element remains optional. Placement still commits the member and parent and raises `OnCardPlaced`, but leaves its world position/rotation unchanged for both animated and immediate calls. Assigned slots retain their normal placement/animation. An empty slot is distinct from the existing board-root fallback used when the array is absent or an index is outside it; no asset rewrite is required.

For scene-independent logic, use `BoardModel(maxCards)` through `ICardContainer`. Its `CanAdd(...)` and `Add(...)` enforce the same finite limit; an overflowing add throws without changing state or emitting `OnChanged`. A zero-capacity model rejects all cards, and a negative constructor limit throws `ArgumentOutOfRangeException`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CardSlots` | Card Slots. |
| `Cards` | Cards. |
| `Count` | Count. |
| `IsEmpty` | Is Empty. |
| `IsFull` | Is Full. |
| `OnBoardCleared` | On Board Cleared. |
| `OnBoardFull` | On Board Full. |
| `OnCardPlaced` | On Card Placed. |
| `_animationConfig` | Animation Config. |
| `_boardSources` | Board Sources. |
| `_cardSlots` | Card Slots. |
| `_cards` | Cards. |
| `_extraRoots` | Extra Roots. |
| `_faceUp` | Face Up. |
| `_handSources` | Hand Sources. |
| `_initialSpawnedCards` | Initial Spawned Cards. |
| `_lastCollectedCards` | Last Collected Cards. |
| `_layoutSettings` | Layout Settings. |
| `_layoutType` | Layout Type. |
| `_mode` | Mode. |
| `_placeDuration` | Place Duration. |
| `_settingsSourceDeck` | Settings Source Deck. |
| `_stackZSorting` | Stack ZSorting. |

## See Also

- [Cards README](./README.md)
