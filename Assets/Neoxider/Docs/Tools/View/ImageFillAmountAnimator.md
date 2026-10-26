# ImageFillAmountAnimator

**Purpose:** Animated 0..1 progress on an `Image` with optional inversion and optional unscaled-time
tween. Two ways to show the value: cut a Filled image (`FillAmount`), or stretch a 9-sliced bar so
its rounded caps survive (`SlicedWidth`).

## Setup

- Add the component via the Unity menu.
- Reference resolves automatically from the same GameObject in `Awake` when left empty.
- **FillAmount** (default): set the Image type to *Filled*.
- **SlicedWidth**: set the Image type to *Sliced*, put the fill inside its track and lay it out at
  **full** progress. The fill **must be horizontally stretch-anchored (`anchorMin.x < anchorMax.x`)**,
  with the track insets as offsets. Value 1 is that authored rect, value 0 collapses it onto its left
  anchor. Offsets are never modified. With point anchors (the default of *GameObject > UI > Image*, or
  any left-anchored fixed-size fill) the width cannot change: only value 0 does anything (it hides the
  image), and the component logs one warning per instance when `NeoDiagnostics` warnings are enabled.
- Until the first value arrives, `SlicedWidth` treats the authored rect as value 1, so the first
  `SetValue` animates from full instead of snapping empty first.

## Lifecycle

- Disabling the component (or its page) mid-tween stops the tween and applies the last requested value
  immediately, so a reopened page shows the current value instead of a half-way one. Destroying an enabled
  component does the same, because Unity runs `OnDisable` before `OnDestroy`; the destroy step itself only
  stops the tween.
- Switching `Mode` away from `SlicedWidth` stops the tween and restores the authored right anchor and the
  image's visibility.
- Destroying only the target `Image`, destroying an externally assigned target GameObject, or assigning
  another Image retires the captured animation on its next tween update. The animator stays enabled;
  unrelated animations continue. The retired animation neither completes nor writes to either target.
- A later `SetValue` / `SetValueImmediate` uses the currently assigned Image or resolves a fresh local
  Image when the old reference is missing. `SlicedWidth` captures the replacement's own authored anchors;
  switching modes restores those anchors rather than dimensions remembered from the previous Image.
- `SetValueImmediate` called by a UI dirty callback takes priority over the interrupted tween, including
  the image's visibility when the immediate value is zero.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_duration` | Duration. Non-positive applies the value immediately. |
| `_ease` | Ease. |
| `_image` | Image. Auto-resolves via `GetComponent` when empty. |
| `_invertValue` | Invert Value. |
| `_ignoreTimeScale` | Run tween on unscaled time (default false). |
| `_mode` | `FillAmount` or `SlicedWidth`. |
| `_minVisibleWidth` | SlicedWidth only: any non-zero value is drawn at least this wide so the two caps never overlap. Zero hides the image. |

## API

- `SetValue(float)` — animated; null-safe, clamps non-finite input to 0, applies immediately when inactive.
- `SetValueImmediate(float)` — syncs view without tween (respects inversion).
- `SetBool(bool)` / `SetBool01(float)` — bool mapping through `SetValue`.
- `Mode` — read/write the fill mode from code (leaving `SlicedWidth` restores the authored rect).
- `DisplayedValue` — the value currently on screen (after inversion; the untouched `SlicedWidth` rect is 1).

## Example: rounded progress bar

```csharp
// Track (Image, Sliced) > Fill (Image, Sliced, stretch anchors, 6-unit insets)
fillAnimator.Mode = ImageFillAmountAnimator.FillMode.SlicedWidth;
fillAnimator.SetValue(done / (float)target);
```

## See Also

- [Module Root](../README.md)
