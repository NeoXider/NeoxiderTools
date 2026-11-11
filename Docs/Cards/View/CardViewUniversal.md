# CardViewUniversal

**What it is:** universal card view (`MonoBehaviour`, `Scripts/Cards/View/CardViewUniversal.cs`) implementing `ICardView`, `ICardDisplayMode` and `ICardViewAnimations`: display modes (flip / always face up / always face down), move/flip tweens, hover, and the built-in animation presets of `CardViewAnimationTemplates`.

**How to use:**
1. Add the component (menu `Neoxider/Cards/CardViewUniversal`) to a card prefab with an `Image` or `SpriteRenderer` and assign it.
2. Call `Initialize(deckConfig)` or `SetSpriteOverrides(face, back)` so it can resolve sprites.
3. Wrap it in a `CardPresenter` and add it to a `HandPresenter`/`HandView`, or drive it directly (`SetData`, `FlipAsync`, `MoveToAsync`, `PlayOneShotAsync`, `PlayLooped`).

---

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

## Reusing animation templates

`CardViewAnimationTemplates` is a static class of DOTween presets that any view can call for its own `Transform`: `Bounce`, `Pulse`, `PulseLooped`, `Shake`, `Highlight`, `HighlightGraphic`, `FlyIn`, `Idle`, plus the dispatchers `PlayOneShot(target, type, duration)` and `PlayLooped(target, type, cycleDuration, config)`. Each factory returns the tween linked to the target GameObject; the caller owns stopping it.

```csharp
Tween bounce = CardViewAnimationTemplates.Bounce(transform, 0.25f, 0.15f, animationConfig);
Tween idle = CardViewAnimationTemplates.Idle(transform);   // looped; kill it when the card leaves the hand
```

## See Also

- [Module Root](../README.md)
- [Custom card view guide](./CustomCardViewGuide.md)
