# HandView

**Purpose:** A helper component added to the **item prefab** (WorldDropPrefab). Defines local position, rotation, and scale offsets when the item is equipped in the hand via `InventoryHand`.

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
