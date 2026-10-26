# FieldSpawner

**Purpose:** instantiate cell views or gameplay objects at `FieldGenerator` cell centers. Use [FieldObjectSpawner](FieldObjectSpawner.md) when per-cell tracking and occupancy are required.

## Setup

1. Add `FieldSpawner` beside a generated `FieldGenerator`.
2. Assign `Prefabs` in the Inspector.
3. Connect `OnObjectSpawned(GameObject, FieldCell)` to view setup, or call the C# API from your scene controller.

## Runtime API

| Field | Description |
|-------|-------------|
| `Prefabs` | Available scene/prefab templates, selected by index. |
| `SpawnAt(position, prefabIndex)` | Instantiate under the spawner transform at the cell center; return the object or null on invalid input. |
| `SpawnOnAllWalkable(prefabIndex)` | Spawn on each enabled, walkable generated cell. Occupancy is ignored for this bulk operation. |
| `OnObjectSpawned` | Spawned object and target cell. |

```csharp
FieldSpawner spawner = GetComponent<FieldSpawner>();
GameObject view = spawner.SpawnAt(new Vector3Int(2, 1, 0), prefabIndex: 0);
```

`SpawnAt` validates cell existence, prefab index and a non-null template. Missing/destroyed templates return null without spawning or firing an event. The required generator is resolved lazily, so public calls do not depend on `Awake` having run.

Manual `SpawnAt` does not impose walkability, shape or occupancy rules; validate those rules in the calling gameplay layer. Bulk spawning enumerates the generated cells even when `Config.Size` contains a pending resize. Its Inspector button is available in Play Mode.

Instances are parented to this component's transform. The caller owns any earlier despawn or pooling policy; this component does not retain a spawn registry.

## See Also

- [Module Root](../README.md)
