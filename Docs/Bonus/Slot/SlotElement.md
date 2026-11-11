# SlotElement

**What it is:** one symbol cell on a slot reel (`MonoBehaviour`, `Scripts/Bonus/Slot/SlotElement.cs`). It shows a `SlotVisualData` (sprite on an `Image` or `SpriteRenderer`, optional description text), keeps its symbol `id`, plays an optional alpha crossfade and motion-stretch effect, and draws `[col,row] id` labels in the Scene view.

**How to use:**
1. Put at least `countSlotElement + 1` `SlotElement` objects under each [Row](Row.md); assign `image` or `spriteRenderer` (auto-filled from the same object in `OnValidate`).
2. Leave symbol assignment to the reel; call `SetVisuals(data)` / `SetVisuals(data, smooth)` yourself only for custom reels.
3. Tune **Crossfade Seconds** and **Motion Stretch Max Y** for the look; use the gizmo fields for Scene debugging.

---

`SetVisuals` skips redundant work on reel wraps (the description object is only toggled when its state changes).
Scene labels resolve their window row and column without sorting copies of the reel.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `gizmoAutoDetect` | Gizmo Auto Detect. |
| `gizmoColor` | Gizmo Color. |
| `gizmoEnabled` | Gizmo Enabled. |
| `gizmoFontSize` | Gizmo Font Size. |
| `gizmoIconSize` | Gizmo Icon Size. |
| `gizmoLabelOffset` | Gizmo Label Offset. |
| `gizmoManualCol` | Gizmo Manual Col. |
| `gizmoManualRow` | Gizmo Manual Row. |
| `gizmoOutline` | Gizmo Outline. |
| `gizmoOutlineColor` | Gizmo Outline Color. |
| `gizmoOutlineScreenPixels` | Label outline thickness in Scene view pixels. |
| `gizmoRowLabelMode` | Row index source: visible window (default) or full reel order. |
| `crossfadeSeconds` | Alpha crossfade length on a smooth symbol change (0 = instant). |
| `motionStretchMaxY` | Maximum Y stretch while the reel moves fast (1 = off). |
| `id` | Id. |
| `image` | Image. |
| `spriteRenderer` | Sprite Renderer. |
| `textDescription` | Text Description. |

## See Also

- [Module Root](../README.md)