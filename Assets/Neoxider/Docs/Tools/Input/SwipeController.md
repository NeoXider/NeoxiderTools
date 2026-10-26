# SwipeController

Detect cardinal swipe gestures from touch or the left mouse button. The component uses namespace `Neo.Tools`, assembly `Neo.Tools.Input`.

## Setup

1. Add `SwipeController` from the Neoxider Tools component menu.
2. Set the minimum X/Y distances in screen pixels. Movement must strictly exceed the corresponding threshold.
3. Enable `detectSwipeOnlyAfterRelease` for one deliberate decision at release, or leave it disabled to detect movement segments while dragging.
4. Wire `OnSwipe` in the Inspector or subscribe from C#.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `minDistanceForSwipeX`, `minDistanceForSwipeY` | Per-axis pixel thresholds. Only an axis that meets its own threshold can supply the direction. |
| `detectSwipeOnlyAfterRelease` | Defers detection until the active contact ends. |
| `ignoreLastSwipe` | Allows repeated notifications in the same direction during one drag. Otherwise a direction change or a new gesture is required. |
| `OnSwipe` | UnityEvent carrying `SwipeData`: start/end screen positions and direction. |

If both axes qualify, the greater absolute displacement wins; ties choose the horizontal direction. Each qualifying movement segment advances the stored origin, including a segment whose repeated direction was suppressed.

## C# API

```csharp
swipeController.OnSwipe.AddListener(HandleSwipe);

// HandleSwipe takes a SwipeData value; remove the listener when its owner is disabled.
if (SwipeController.GetSwipeDirection(out SwipeDirection direction))
{
    MoveSelection(direction);
}
```

`SwipeData.StartPosition` and `EndPosition` follow the actual movement order. `GetSwipeDirection` consumes the last pending direction once; multiple swipes before polling collapse to the latest direction. Prefer `OnSwipe` when every gesture matters. `Instance` is the most recently awakened controller, not an auto-created singleton; use one controller for static polling. `ISwipeSubscriber` is only a contract: implement `SubscribeToSwipe(SwipeData)` and register it with `OnSwipe.AddListener` yourself.

## Input and lifecycle

Legacy input uses `Input.GetTouch` without allocating an `Input.touches` array. The first begun contact owns the gesture until Ended/Canceled; other finger IDs cannot replace its origin or release it. Mouse dragging requires an observed button-down. Cancellation, component disable, application pause and focus loss clear active gestures and pending polling state.

Mouse and touch share `InputBackendCompat` selection with keyboard readers: Both prefers legacy, New-only skips it, and a rejected legacy read is remembered until reset. With legacy input disabled, mouse reads use `MouseInputCompat` and touch reads use the optional Input System's `Touchscreen.primaryTouch`. This is a single-contact recognizer, not a multi-touch gesture system. If the Input System package/device is absent, no gesture is produced. Input System primary-touch lifetime can span additional fingers; verify that policy on the target device when multi-touch is relevant.

## See Also

- [Module Root](README.md)
