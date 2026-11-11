# DeckComponent

**What it is:** scene deck adapter (`MonoBehaviour`, `Scripts/Cards/Components/DeckComponent.cs`, namespace `Neo.Cards`) with card spawning, an optional visual stack, deal events, a trump preview and a standalone `DeckModel` backing its draw pile.

**How to use:**
1. Add the component and assign **Config** and **Card Prefab** in the Inspector. The prefab needs `CardComponent` and either an Image or SpriteRenderer reference.
2. Enable **Initialize On Start**, or call `Initialize()` from code/a UnityEvent. **Shuffle On Start** controls initialization and reset shuffling; enable **Use Fixed Shuffle Seed** for a reproducible order.
3. Optionally assign **Visual Stack Board** and enable **Spawn Visual On Initialize**. Give the board enough capacity for the full deck.
4. Deal with `DealToHand(...)` from an Inspector event, or await `DealToHandAsync(...)` in C#.

---

## Initialize, Reset and seeding

`Initialize()` detaches its handlers from the previous model before replacing it. It loads `Config.GenerateDeck()`, including custom cards, and shows the trump display only when **Show Trump Card** is enabled and the new model has a bottom card. Reinitializing an empty custom deck hides the old preview. When **Spawn Visual On Initialize** is off, an existing visual stack built for the old model is cleared (its cards are destroyed and `OnVisualStackChanged` fires); when it is on, the stack is rebuilt.

`Reset()` cancels pending deals, clears the visual stack board, destroys deck-owned tracked objects, and restores that initialized composition, clearing discards. Cards already accepted by a hand have left deck cleanup ownership and survive. Reset does not rebuild the visual stack; call `BuildVisualStackAsync()` or `Initialize()` with **Spawn Visual On Initialize** to show it again. The visual stack board is owned by the deck: `BuildVisualStackAsync()`, `Reset()` and `Initialize()` clear every card on it, so do not use the same board as a gameplay zone.

The trump preview follows the bottom card at all times: `Shuffle()`, `ShuffleVisualAsync(...)`, `Reset()`, `ReturnCard(card, toTop: false)` and direct `Model` changes refresh it, and it hides when the deck empties.

```csharp
deck.UseFixedShuffleSeed = true;
deck.ShuffleSeed = 2026;
deck.Initialize();           // same order on every run
deck.Reset();                // same order again
```

Override `protected virtual System.Random CreateShuffleRandom()` in a subclass to share one game-wide random source. Logic-only code can construct `new DeckModel(seed)` directly.

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

| Member | Description |
|--------|-------------|
| `Model` | Backing `DeckModel` (replaced by every `Initialize()`). |
| `RemainingCount` / `IsEmpty` | Draw pile state. |
| `TrumpCard` / `TrumpSuit` | Bottom card of the deck and its suit. |
| `UseFixedShuffleSeed` / `ShuffleSeed` | Reproducible shuffling for the next `Initialize()`/`Reset()`. |
| `Initialize()` / `Reset()` / `Shuffle()` | Build, restore or shuffle the logical deck. |
| `DrawCard(bool faceUp)` / `DrawCards(int, bool)` / `DrawCardAsync(...)` | Spawn scene cards from the top of the deck. |
| `DealToHandAsync(HandComponent, bool, float?)` | Deal the top card into a hand; null when rejected or cancelled. |
| `ReturnCard(CardComponent, bool toTop)` | Return the card's data to the deck and destroy the scene card; a hand or board that holds the card is updated first. |
| `BuildVisualStackAsync()` / `ShuffleVisualAsync(ShuffleVisualType, float?)` | Visual stack construction and animated shuffle. |

An already full or null hand is rejected before drawing. The hand checks admission again after flight through `TryAddCardAsync`. If another call filled its last slot, the failed deal returns null, restores the exact drawn data to the original model's top, and returns its existing stack visual when using a visual stack; a newly spawned rejected visual is destroyed. Only accepted transfers publish `OnCardDealt`.

Reset/reinitialize invalidates the source generation before changing state. A pending old deal stops its owned tween and releases its detached visual instead of inserting data into the replacement deck. An accepted transfer is committed before hand layout completes; cancellation or a later layout change does not undo that admission. An explicit `moveDuration: 0f` skips the flight even when a deck animation config is assigned.

Cleanup ownership leaves the deck at successful hand-model admission, before the card is reparented and before public model/component add callbacks run. Native `OnTransformParentChanged` or destination `OnTransformChildrenChanged` observers therefore see committed hand membership and can reset/reinitialize the source without destroying the accepted object. Public model observers still see its final hand parent. An `OnCardDrawn` listener may also admit the card and reset/reinitialize the deck in that same listener. A deal whose source was replaced returns null and emits no old-generation `OnCardDealt`; it leaves that committed card alone instead of destroying it or rolling its data back. This applies to both regular and bottom-insert hands and needs no extra Inspector wiring.

Use `Model` directly for logic-only draws only when your code also owns visual synchronization. `DrawCard(...)` creates a new scene card; `ReturnCard(...)` restores its data, detaches it from its hand/board and destroys the scene object.

### UI card creation

When the prefab uses `RectTransform` and the spawn point belongs to a `Canvas`, new cards are created under that spawn point in local UI space. Both direct draws and visual stacks retain the prefab's authored scale and rotation, so the Canvas scale is never applied a second time to cards in a hand. The rest/hover baseline is captured there before the card is published. `DrawCard` can therefore return a UI child of the spawn point; callers can then transfer it to a hand or board through the usual APIs. A non-UI prefab, or a spawn point outside a Canvas, retains the unparented world-position/identity-rotation creation path.

### Visual stack

Visual-stack construction snapshots its model, board, generation and card data. Reset/reinitialize from a native hierarchy callback aborts that old build without publishing its completion events; cards the aborted build had already created but not yet placed are destroyed.

The stack is created and placed synchronously in one batch (`BoardComponent.PlaceCardsImmediate`), so the stack board runs a single layout pass for the whole deck instead of one per card, and the task returned by `BuildVisualStackAsync()` is already complete. The stack board's `OnCardPlaced` events arrive after the whole stack is on the board. `ShuffleVisualAsync(...)` re-places the reordered stack the same way.

The logical model is the source of truth. A deal from the stack always hands out the card data that left the model: if the model was reordered underneath the stack (`Shuffle()`, `ReturnCard(...)`, direct `Model` use), the top stack card is re-skinned with the drawn data instead of duplicating or losing a card. Prefer `ShuffleVisualAsync(...)` with a visual stack so the visible order also follows the model.

`ShuffleVisualAsync(...)` reorders the stack to the shuffled model and plays the chosen effect. Cards dealt while the shake runs are left alone (they are no longer shaken or snapped back). `Reset()`/`Initialize()` during the effect cancel it without raising `OnShuffleVisualCompleted`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_config` | `DeckConfig` that generates the deck and provides sprites. |
| `_initializeOnStart` | Call `Initialize()` in `Start`. |
| `_shuffleOnStart` | Shuffle on `Initialize()` and `Reset()`. |
| `_useFixedShuffleSeed` | Shuffle with `_shuffleSeed` so every Initialize/Reset gives the same order. |
| `_shuffleSeed` | Seed used when the fixed seed is enabled. |
| `_spawnPoint` | Where drawn cards appear (defaults to this transform). |
| `_cardPrefab` | `CardComponent` prefab for drawn and stack cards. |
| `_visualLayoutType` | Layout of the visual stack (default `Stack`). |
| `_visualStackBoard` | Board that shows the deck as a stack; owned by the deck. |
| `_spawnVisualOnInitialize` | Build the visual stack on `Initialize()`. |
| `_stackFaceUp` | Face state of stack cards. |
| `_stackPositionJitter` / `_stackRotationJitter` / `_stackStepY` | Stack look (deterministic per card index). |
| `_stackOffsetPosition` / `_stackOffsetRotation` | Extra local offset for every stack card. |
| `_animationConfig` | Optional `CardAnimationConfig` (deal/shuffle timings, stack look). |
| `_setAnimationConfigAsGlobal` | Publish `_animationConfig` as `CardSettingsRuntime.GlobalAnimationConfig` on `Start`. |
| `_showTrumpCard` / `_trumpCardDisplay` | Show the bottom card on a separate `CardComponent`. |

## Events

| Event | When |
|-------|------|
| `OnInitialized` | After `Initialize()` built the model. |
| `OnShuffled` | After the model was shuffled (`Shuffle()`, `ShuffleVisualAsync`). |
| `OnDeckEmpty` | The draw pile became empty. |
| `OnCardDrawn(CardComponent)` | `DrawCard`/`DrawCardAsync` spawned a card. |
| `OnCardDealt(CardComponent, HandComponent)` | A deal was accepted by the hand. |
| `OnVisualStackBuilt` | `BuildVisualStackAsync()` finished. |
| `OnVisualStackChanged` | The stack changed (build, deal, shuffle, reset/initialize clearing it). |
| `OnShuffleVisualStarted(ShuffleVisualType)` / `OnShuffleVisualCompleted` | Visual shuffle start/end (no completion when cancelled). |

## See Also

- [Cards README](./README.md)
- [HandComponent](./HandComponent.md)
- [BoardComponent](./BoardComponent.md)
- [DeckConfig](./DeckConfig.md)
