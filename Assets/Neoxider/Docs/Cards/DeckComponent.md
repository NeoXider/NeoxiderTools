# DeckComponent

**Purpose:** scene deck adapter with card spawning, optional visual stacks, deal events, and a standalone `DeckModel` backing its draw pile.

## Setup

1. Add the component and assign **Config** and **Card Prefab** in the Inspector. The prefab needs `CardComponent` and either an Image or SpriteRenderer reference.
2. Enable **Initialize On Start**, or call `Initialize()` from code/a UnityEvent. **Shuffle On Start** controls initialization and reset shuffling.
3. Optionally assign **Visual Stack Board** and enable **Spawn Visual On Initialize**. Give the board enough capacity for the full deck.
4. Deal with `DealToHand(...)` from an Inspector event, or await `DealToHandAsync(...)` in C#.

`Initialize()` detaches `OnDeckEmpty` from the previous model before replacing it. It loads `Config.GenerateDeck()`, including custom cards, and shows the trump display only when **Show Trump Card** is enabled and the new model has a bottom card. Reinitializing an empty custom deck hides the old preview.

`Reset()` cancels pending deals, destroys deck-owned tracked objects, and restores that initialized composition, clearing discards. Cards already accepted by a hand have left deck cleanup ownership. Reset does not automatically rebuild a visual stack or reconcile external hand/board lists. Coordinate a whole-table restart through the owners of those zones.

## Runtime API

```csharp
if (!hand.IsFull)
{
    CardComponent dealt = await deck.DealToHandAsync(hand, faceUp: true);
    if (dealt != null)
    {
        // The card was added; OnCardDealt also identifies the destination hand.
    }
}
```

An already full or null hand is rejected before drawing. The hand checks admission again after flight through `TryAddCardAsync`. If another call filled its last slot, the failed deal returns null, restores the exact drawn data to the original model's top, and returns its existing stack visual when using a visual stack; a newly spawned rejected visual is destroyed. Only accepted transfers publish `OnCardDealt`.

Reset/reinitialize invalidates the source generation before changing state. A pending old deal stops its owned tween and releases its detached visual instead of inserting data into the replacement deck. An accepted transfer is committed before hand layout completes; cancellation or a later layout change does not undo that admission. An explicit `moveDuration: 0f` skips the flight even when a deck animation config is assigned.

Cleanup ownership leaves the deck at successful hand-model admission, before the card is reparented and before public model/component add callbacks run. Native `OnTransformParentChanged` or destination `OnTransformChildrenChanged` observers therefore see committed hand membership and can reset/reinitialize the source without destroying the accepted object. Public model observers still see its final hand parent. An `OnCardDrawn` listener may also admit the card and reset/reinitialize the deck in that same listener. A deal whose source was replaced returns null and emits no old-generation `OnCardDealt`; it leaves that committed card alone instead of destroying it or rolling its data back. This applies to both regular and bottom-insert hands and needs no extra Inspector wiring.

Use `Model` directly for logic-only draws only when your code also owns visual synchronization. `DrawCard(...)` creates a new scene card; `ReturnCard(...)` restores its data and destroys that scene object, so remove it from its hand/board first.

### UI card creation

When the prefab uses `RectTransform` and the spawn point belongs to a `Canvas`, new cards are created
under that spawn point in local UI space. Both direct draws and visual stacks retain the prefab's
authored scale and rotation. The rest/hover baseline is captured there before the card is published.
`DrawCard` can therefore return a UI child of the spawn point; callers can then transfer it to a hand
or board through the usual APIs. A non-UI prefab, or a spawn point outside a Canvas, retains the
unparented world-position/identity-rotation creation path.

Режим UI определяется `RectTransform` префаба и Canvas у точки появления. Карта сразу создаётся в
локальном пространстве UI, поэтому масштаб Canvas не превращается в обратное увеличение карты в
руке. Тот же путь применяется к визуальной стопке. Обычные мировые карты сохраняют прежний способ
создания; публичные настройки руки, стола и префаба остаются действующими.

Visual-stack construction snapshots its model, board, generation and card data. Reset/reinitialize
from a native hierarchy callback aborts that old build without publishing its completion events.

With a visual stack, use `DealToHandAsync(...)` and `ShuffleVisualAsync(...)` consistently. `DrawCard(...)` and `Shuffle()` change the logical deck without removing or reordering existing stack views.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `AnimationConfig` | Animation Config. |
| `CardPrefab` | Card Prefab. |
| `Config` | Config. |
| `IsEmpty` | Is Empty. |
| `Model` | Model. |
| `OnCardDrawn` | On Card Drawn. |
| `OnDeckEmpty` | On Deck Empty. |
| `OnInitialized` | On Initialized. |
| `OnShuffleVisualCompleted` | On Shuffle Visual Completed. |
| `OnShuffleVisualStarted` | On Shuffle Visual Started. |
| `OnShuffled` | On Shuffled. |
| `OnVisualStackBuilt` | On Visual Stack Built. |
| `OnVisualStackChanged` | On Visual Stack Changed. |
| `RemainingCount` | Remaining Count. |
| `SpawnPoint` | Spawn Point. |
| `_animationConfig` | Animation Config. |
| `_cardPrefab` | Card Prefab. |
| `_config` | Config. |
| `_spawnPoint` | Spawn Point. |
| `_spawnVisualOnInitialize` | Spawn Visual On Initialize. |
| `_stackFaceUp` | Stack Face Up. |
| `_stackOffsetPosition` | Stack Offset Position. |
| `_stackOffsetRotation` | Stack Offset Rotation. |
| `_stackPositionJitter` | Stack Position Jitter. |
| `_stackRotationJitter` | Stack Rotation Jitter. |
| `_stackStepY` | Stack Step Y. |
| `_trumpCardDisplay` | Trump Card Display. |
| `_visualLayoutType` | Visual Layout Type. |
| `_visualStackBoard` | Visual Stack Board. |

## See Also

- [Cards README](./README.md)
- [HandComponent](./HandComponent.md)
