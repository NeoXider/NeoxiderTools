# Saving Quest Progress

**Purpose:** Describes what quest data to save and how to restore it on load. The Quest module does not ship a built-in save integration — you implement it in your own game. The serializable `QuestState` and the state list on `QuestManager` are the building blocks.

**How to use:** When saving — iterate `AllQuests`, serialize each `QuestState` (or a DTO); when loading — deserialize and pass the data back to `QuestManager` (add a `LoadStates` method in your fork if needed). Perform the load after `QuestManager` is present in the scene.

**Steps:**
1. On save: iterate `QuestManager.Instance.AllQuests`, serialize each `QuestState` (or a DTO: `questId`, `status`, `progress[]`, `completed[]`) into JSON or binary as part of the player save file.
2. On load: deserialize the data, create `QuestState` instances (or fill DTOs), and pass them to `QuestManager`. If the current build lacks a public state-restore method, add a `LoadStates(IEnumerable<QuestState>)` method in a fork or restore via reflection.
3. Perform the load after `QuestManager` is in the scene (e.g. in `Start` after save initialization).
4. When the save format changes, include a version field and implement migration logic.

---

## What to Save

For each quest:
- **QuestId** (`string`)
- **Status** (`int`: 0 = NotStarted, 1 = Active, 2 = Completed, 3 = Failed)
- **objectiveProgress** — `int[]` indexed by objective
- **objectiveCompleted** — `bool[]` indexed by objective

`QuestState` is marked `[Serializable]` with `[SerializeField]` fields, making it compatible with Unity serialization and custom solutions (JSON, etc.).

---

## Approaches

1. **Direct `List<QuestState>` serialization** — save `AllQuests` directly; on load, pass the list to `QuestManager` (requires a restore method in the manager).
2. **DTO** — save/load your own structures (`id`, `status`, `progress[]`, `completed[]`), map them to `QuestState` instances on load, and pass them to the manager.
3. **Completed-only** — save only the IDs of quests with `Completed` status; on load, create `QuestState` with `Completed` and empty progress. Active quests are not restored (or restored via a separate rule).

---

## Loading at Scene Start

1. Wait for save initialization and `QuestManager.Instance` to be available.
2. Load the saved quest data.
3. Restore the state list in `QuestManager` (via `LoadStates` or equivalent).
4. After that, `GetState`, events, etc. work as normal.

---

## See Also

If you use the Neoxider Save module (`SaveProvider`, `SaveField`) — align the format and load timing with its documentation. Overview: [Save/README](../Save/README.md).
