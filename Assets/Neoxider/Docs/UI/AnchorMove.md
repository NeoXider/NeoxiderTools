# AnchorMove

**What it is:** `Neo.UI.AnchorMove` (`Scripts/UI/AnchorMove.cs`) is a layout helper: whenever its values change
in the Inspector it collapses the `RectTransform` anchors to one normalized point and centres the element on it
(`anchoredPosition = 0`). At runtime it changes nothing on its own; code and UnityEvents re-anchor the element
through `Apply()`, `SetX`, `SetY` and `SetAnchor`.

**How to use:**
1. Add `Neoxider > UI > AnchorMove` to a UI element.
2. Drag `x` / `y` (0..1) in the Inspector; the element snaps to that point of its parent.
3. Remove the component when the layout is final, or keep it as a documented anchor setting.

---

## Fields

| Field | Description |
|-------|-------------|
| `x` | Horizontal anchor (0 = left edge, 1 = right edge of the parent). Default `0.5`. |
| `y` | Vertical anchor (0 = bottom edge, 1 = top edge of the parent). Default `0.5`. |

## API

| Member | Description |
|--------|-------------|
| `Apply()` | Writes the current `x` / `y` anchor and zeroes the anchored position. The Inspector calls it on every edit. |
| `SetX(float)` / `SetY(float)` | Set one anchor (clamped to 0..1) and apply it. One argument, so a Slider or other UnityEvent can drive it. |
| `SetAnchor(Vector2)` | Set both anchors (each clamped to 0..1) and apply them. |

Objects without a `RectTransform` are ignored.

## See Also

- [Module Root](../README.md)