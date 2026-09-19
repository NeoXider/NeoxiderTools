# ImageFillAmountAnimator

**Purpose:** Animated `Image.fillAmount` progress (0..1) with optional inversion and optional unscaled-time tween.

## Setup

- Add the component via the Unity menu.
- Reference resolves automatically from the same GameObject in `Awake` when left empty.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_duration` | Duration. Non-positive applies the value immediately. |
| `_ease` | Ease. |
| `_image` | Image. Auto-resolves via `GetComponent` when empty. |
| `_invertValue` | Invert Value. |
| `_ignoreTimeScale` | Run tween on unscaled time (default false). |

## API

- `SetValue(float)` — animated; null-safe, clamps non-finite input to 0, applies immediately when inactive.
- `SetValueImmediate(float)` — syncs view without tween (respects inversion).
- `SetBool(bool)` / `SetBool01(float)` — bool mapping through `SetValue`.

## See Also

- [Module Root](../README.md)