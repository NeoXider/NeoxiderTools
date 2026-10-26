# CardView

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Data` | Data. |
| `IsFaceUp` | Is Face Up. |
| `Transform` | Transform. |
| `_cardImage` | Card Image. |
| `_faceSpriteOverride` | Optional face sprite used without a DeckConfig. |
| `_backSpriteOverride` | Optional back sprite used without a DeckConfig. |
| `_flipDuration` | Flip Duration. |
| `_flipEase` | Flip Ease. |
| `_hoverDuration` | Hover Duration. |
| `_hoverScale` | Hover Scale. |
| `_moveDuration` | Move Duration. |
| `_moveEase` | Move Ease. |
| `_spriteRenderer` | Sprite Renderer. |

## Standalone use

Call `SetSpriteOverrides(faceSprite, backSprite)` when the card art comes from a custom TCG/deckbuilder data model instead of `DeckConfig`. `ClearSpriteOverrides()` returns lookup to the assigned `DeckConfig`.

Hover tweens are owned by the view: repeated hover/exit kills previous tweens, and destroying the GameObject clears active hover motion.

The existing `ICardView` API remains unchanged. Concrete `CardView` additionally accepts a `CancellationToken` as the last argument of `FlipAsync(float, CancellationToken)`, `MoveToAsync(Vector3, float, CancellationToken)`, `MoveToLocalAsync(Vector3, float, CancellationToken)` and `RotateToAsync(Quaternion, float, CancellationToken)`. Rotation retains its existing world-space behavior.

Move/flip/rotate operations own their exact awaited tween. A replacement operation or destruction cancels the previous one; caller cancellation kills that operation's tween and reports `OperationCanceledException`. An interrupted flip restores its full scale, and an old continuation cannot write a new operation's transform. HandView separately owns its layout tweens and cancels them on membership/order changes.

## See Also

- [Module Root](../README.md)
