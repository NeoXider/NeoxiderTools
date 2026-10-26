# FieldObjectSpawner

**Purpose:** spawn objects at cell centers, retain per-cell metadata and maintain occupancy for blocking objects.

## Setup

1. Add `FieldObjectSpawner` beside a generated `FieldGenerator`.
2. Assign `Prefabs` in the Inspector.
3. Call `SpawnAt` from a scene controller, and connect spawn/remove/occupied/freed UnityEvents to views as needed.

## Runtime API

| Field | Description |
|-------|-------------|
| `Prefabs` | Available templates, selected by index. |
| `SpawnAt(position, prefabIndex, occupiesSpace, layer)` | Return `SpawnedObjectInfo`, or null on an invalid cell/index/template. `layer` is reserved for compatibility. |
| `GetObjectsInCell(position)`, `GetAllObjects()` | Return copies of the tracked metadata lists. |
| `IsCellOccupied(position)` | Read the field occupancy flag or tracked blocking objects. |
| `RemoveObject(instance)` | Remove a tracked instance and schedule its destruction; free its original cell after the last tracked blocker only if that cell still belongs to the current board. Call in Play Mode. |
| `SpawnedObjectInfo.Cell / GameObject / OccupiesSpace` | Binding and blocking metadata. |
| `OnObjectSpawned`, `OnObjectRemoved` | Object metadata notifications. |
| `OnCellOccupied`, `OnCellFreed` | Blocking occupancy transition notifications. |

```csharp
FieldObjectSpawner spawner = GetComponent<FieldObjectSpawner>();
SpawnedObjectInfo info = spawner.SpawnAt(Vector3Int.zero, occupiesSpace: true);
if (info != null)
{
    // Retain info.GameObject to remove this particular instance later.
}
```

Missing/destroyed templates return null without registering metadata, occupying a cell or firing a spawn event. Generator lookup is lazy for public spawn/query calls, including calls before `Awake`.

Multiple objects may share a cell. `occupiesSpace: false` objects do not block it. Manual spawning checks cell existence and template validity; the caller decides whether the shape, walkability or existing occupancy permits a placement.

Registration is visible inside `OnObjectSpawned` before that call publishes occupancy. A listener may synchronously spawn another blocker on the same initially free cell: the first valid blocking call to finish its spawn callback sets the current cell occupied. Other registrations still inside their callbacks do not prevent that publication. With both bindings still current, the pair raises one occupied field-state notification and one `OnCellOccupied`; returning from the outer call does not duplicate them. That occupied event can therefore arrive while the outer spawn callback is still running. Either removal order keeps the cell occupied until its last blocker is removed, then publishes one free transition. Decorative objects may remain tracked after that transition.

If that outer callback removes the nested blocker after its occupied notification, the outer registration keeps the cell occupied. Returning from the outer call still emits no duplicate occupied state/notification. Removing the remaining outer object then frees the cell once.

For compatibility, a cell already occupied when `SpawnAt` begins still receives the legacy occupied notification from its first blocking registration. Eligibility for this notification is captured before that registration and requires no prior registered blocker; it is not inferred from another blocker's removal during a callback. Ordinary sequential spawning continues to allow multiple objects in the same occupied cell. This preserves the existing manual spawn policy and does not introduce ownership of an externally written occupancy flag.

## Regeneration and callbacks

Regeneration replaces cell instances. Surviving objects keep their original bindings and remain in `GetAllObjects()` until explicitly removed; `GetObjectsInCell(position)` queries only the current board's cell. The spawner does not automatically destroy or rebind those objects. Removing an old object still unregisters and destroys that instance and raises `OnObjectRemoved`, but cannot free a replacement cell or emit `OnCellFreed` for it.

The ordinary event order remains `OnObjectSpawned` → field `OnCellStateChanged` → `OnCellOccupied`, and `OnObjectRemoved` → field `OnCellStateChanged` → `OnCellFreed` for the relevant first/last blocker. Registration precedes `OnObjectSpawned`; unregistering and scheduling destruction precede `OnObjectRemoved`. Occupancy writes recheck cell identity and registry state after object callbacks. If a field-state callback regenerates the board, the subsequent stale spawner occupancy event is suppressed as well. A spawn listener may remove or detach the instance before `SpawnAt` returns; the returned metadata still identifies that created instance.

Use `SpawnedObjectInfo` as inspection data while registered; do not reassign its public `Cell`, `GameObject` or `OccupiesSpace` fields. Listener exceptions propagate; arbitrary callback mutation is not rolled back.

Use this spawner as the occupancy owner for its cells. External destruction/despawning and independent writers of the occupancy flag still need application-level coordination with the registry. Remove objects through `RemoveObject` in Play Mode; pooling/unregister leases remain a separate design proposal.

При регенерации старые объекты сохраняют исходные привязки. Их удаление не освобождает клетки нового поля; автоматического переноса или удаления объектов нет. Вложенный спавн двух блокирующих объектов в свободную клетку вызывает одно уведомление о занятости. Удаление вложенного объекта после этого уведомления до возврата внешнего вызова не повторяет его. Клетка освобождается после удаления последнего блокирующего объекта.

## See Also

- [Module Root](README.md)
