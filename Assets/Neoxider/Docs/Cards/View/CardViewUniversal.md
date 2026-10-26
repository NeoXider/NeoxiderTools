# CardViewUniversal

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Data` | Data. |
| `IsFaceUp` | Is Face Up. |
| `Mode` | Mode. |
| `Transform` | Transform. |
| `_animationConfig` | Animation Config. |
| `_cardImage` | Card Image. |
| `_displayMode` | Display Mode. |
| `_flipDuration` | Flip Duration. |
| `_flipEase` | Flip Ease. |
| `_hoverDuration` | Hover Duration. |
| `_hoverScale` | Hover Scale. |
| `_moveDuration` | Move Duration. |
| `_moveEase` | Move Ease. |
| `_spriteRenderer` | Sprite Renderer. |

Hover tweens are owned by the view: repeated hover/exit kills previous tweens, and `OnDestroy` clears hover, current, and looped animations.

## Cancellation and ownership

Concrete `FlipAsync(float duration, CancellationToken cancellation)` and `MoveToAsync(Vector3 position, float duration, CancellationToken cancellation)` add caller cancellation without expanding the required `ICardView` interface. A new move/flip cancels its predecessor, and destruction cancels the active operation. These methods report `OperationCanceledException`; a cancelled flip restores its full scale. Fixed face modes still prevent flips.

`PlayOneShotAsync(...)` tracks each returned template tween, links it to this view's destruction token and combines that token with the caller's token. It retains its existing quiet cancellation behavior. `OnDestroy` also releases outstanding one-shots. Template factories link their root tween/sequence to the target GameObject, including loops and UI highlight sequences; custom callers still own explicit stop/cancellation and the returned tween.

## See Also

- [Module Root](../README.md)
