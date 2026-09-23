# ImageFillAmountAnimator

**Purpose:** Animated 0..1 progress on an `Image` with optional inversion and optional unscaled-time
tween. Two ways to show the value: cut a Filled image (`FillAmount`), or stretch a 9-sliced bar so
its rounded caps survive (`SlicedWidth`).

## Setup

- Add the component via the Unity menu.
- Reference resolves automatically from the same GameObject in `Awake` when left empty.
- **FillAmount** (default): set the Image type to *Filled*.
- **SlicedWidth**: set the Image type to *Sliced*, put the fill inside its track and lay it out at
  **full** progress (usually stretch-anchored with the track insets as offsets). Value 1 is that
  authored rect, value 0 collapses it onto its left anchor. Offsets are never modified.

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
- `Mode` — read/write the fill mode from code.
- `DisplayedValue` — the value currently on screen.

## Example: rounded progress bar

```csharp
// Track (Image, Sliced) > Fill (Image, Sliced, stretch anchors, 6-unit insets)
fillAnimator.Mode = ImageFillAmountAnimator.FillMode.SlicedWidth;
fillAnimator.SetValue(done / (float)target);
```

## See Also

- [Module Root](../README.md)
