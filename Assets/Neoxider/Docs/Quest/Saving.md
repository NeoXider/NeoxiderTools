# Saving Quest Progress

**Purpose:** Describes how quest progress is persisted. `QuestManager` ships with built-in persistence: quest states are serialized to JSON and stored through `SaveProvider` under the `Save Key` field (default `Settings_Quests`).

**How to use:** Keep `Auto Save` and `Auto Load` enabled on `QuestManager` — states are saved on accept/complete/fail/reset and restored during initialization. Call `Save()` / `Load()` for manual control (e.g. custom save points).

**Steps:**
1. Set `Save Key` on `QuestManager` (or keep the default `Settings_Quests`).
2. Leave `Auto Save` / `Auto Load` enabled for automatic persistence, or call `QuestManager.I.Save()` and `QuestManager.I.Load()` yourself.
3. If you need a custom format (e.g. cloud saves), iterate `QuestManager.Instance.AllQuests` and serialize each `QuestState` (or a DTO: `questId`, `status`, `progress[]`, `completed[]`) into your own save file.
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

1. **Built-in (default)** — `QuestManager` serializes the state list to JSON and stores it via `SaveProvider.SetString(saveKey, json)`; `Load()` restores it. No extra code needed.
2. **DTO** — save/load your own structures (`id`, `status`, `progress[]`, `completed[]`) when you need a custom format; map them from `AllQuests` on save.
3. **Completed-only** — save only the IDs of quests with `Completed` status; on load, re-accept and complete them via the public API. Active quests are not restored (or restored via a separate rule).

---

## Loading at Scene Start

1. With `Auto Load` enabled, `QuestManager` calls `Load()` during its singleton initialization — nothing else is required.
2. For manual flows, call `QuestManager.I.Load()` after your save system is ready.
3. After that, `GetState`, events, etc. work as normal.

---

## See Also

If you use the Neoxider Save module (`SaveProvider`, `SaveField`) — align the format and load timing with its documentation. Overview: [Save/README](../Save/README.md).
