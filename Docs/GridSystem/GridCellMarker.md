# GridCellMarker

**What it is:** a scene-side `MonoBehaviour` that binds a GameObject/collider to a `FieldGenerator` cell. Useful for board views, click targets, drag/drop targets, and custom cell prefabs. Path: `Scripts/GridSystem/GridCellMarker.cs`, namespace `Neo.GridSystem`.

**How to use:**
1. Add `GridCellMarker` to your cell prefab (`Add Component → Neoxider/GridSystem/GridCellMarker`).
2. Either assign **Generator**/**Position** in the Inspector, or call `Bind(generator, position)` from your board-spawning code.
3. Read `Cell` to get the live `FieldCell` this marker points at.

---

## Fields

| Field | Description |
|-------|-------------|
| **Generator** | The `FieldGenerator` this marker belongs to. |
| **Position** | The `Vector3Int` cell coordinate inside the field. |

## API

| Member | Description |
|--------|-------------|
| `Cell` | The live `FieldCell` at `Generator`/`Position` (`null` if `Generator` is unassigned). |
| `Bind(FieldGenerator generator, Vector3Int position)` | Sets `Generator` and `Position` from code (e.g. when instantiating cell views). |

## See also

- [FieldGenerator](./FieldGenerator.md)
- [GridSystem README](./README.md)
