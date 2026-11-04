# HandView

**What it is:** `MonoBehaviour` placed on an item's World Drop Prefab that defines its local position, rotation and scale while equipped by `InventoryHand`. `Scripts/Tools/Inventory/Runtime/HandView.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `HandView` to the item prefab (root or child).
2. Tune **Position Offset**, **Rotation Offset** and **Scale In Hand** against the hand anchor.
3. `InventoryHand` reads it when it spawns the visual; no code is required.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Position Offset** | Item position offset relative to the hand anchor (local space). |
| **Rotation Offset** | Rotation offset in degrees (Euler) relative to the hand. |
| **Scale In Hand** | Base scale of the item in hand (1 = unchanged). The hand-wide scale from `InventoryHand` is applied on top. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `Vector3 PositionOffset { get; }` | Returns the position offset. |
| `Vector3 RotationOffset { get; }` | Returns the rotation offset. |
| `float ScaleInHand { get; }` | Returns the base scale (min 0.01). |

## Examples

### No-Code Example (Inspector)
Open your sword prefab. Add `HandView`. Adjust `Position Offset` and `Rotation Offset` until the sword sits properly in the character's hand. When `InventoryHand` equips this item, it will automatically use your settings.

## See Also
- [InventoryHand](InventoryHand.md)
- ← [Tools/Inventory](README.md)
