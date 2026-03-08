# QuestState

`QuestState` is the runtime state object for one accepted quest. It stores quest id, status, objective progress, and completion flags. It is not a `MonoBehaviour`; instances are created and owned by `QuestManager`. File: `Assets/Neoxider/Scripts/Quest/QuestState.cs`, namespace: `Neo.Quest`.

## How to use it

1. Do not create `QuestState` manually.
2. Read it through `QuestManager.GetState(...)`.
3. Use it for UI, save serialization, and runtime status checks.
4. Change quest progress through `QuestManager`, not by mutating `QuestState` directly.

## Main members

| Member | Description |
|--------|-------------|
| `QuestId` | Quest identifier |
| `Status` | `NotStarted`, `Active`, `Completed`, or `Failed` |
| `ObjectiveCount` | Number of objectives in this quest |

## Main API

| API | Description |
|-----|-------------|
| `GetObjectiveProgress(int index)` | Returns numeric progress for one objective |
| `IsObjectiveCompleted(int index)` | Returns whether one objective is completed |

## Typical use in UI

- Show `Status` as active/completed/failed state.
- Use `GetObjectiveProgress(...)` for counters such as `2/3`.
- Use `IsObjectiveCompleted(...)` for binary goals.
- Read required counts from `QuestConfig.Objectives[index].RequiredCount`.

## Save note

`QuestState` is serializable, but the module still expects project-level logic to restore a saved list back into `QuestManager`.

## See also

- [README](./README.md)
- [QuestManager](./QuestManager.md)
- [QuestConfig](./QuestConfig.md)
