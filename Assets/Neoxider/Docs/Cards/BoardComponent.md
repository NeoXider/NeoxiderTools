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
| `CanPlaceCard(CardComponent card)` | Checks null/capacity before placement. |
| `SetCapacity(int maxCards, bool regenerateSlots = true)` | Changes capacity for custom boards/zones. |
| `SetFaceUp(bool faceUp, bool applyToExisting = false)` | Changes default face state and optionally updates current cards. |
| `PlaceCardAsync(...)` | Places a card with optional animation. |
| `Clear()` / `ClearAndReturn()` | Removes cards by destroying or detaching them. |

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `5` | 5. |
| `80f` | 80f. |
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
| `true` | True. |

## See Also

- [Module Root](../README.md)
