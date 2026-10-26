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

## Loading and config changes

`Load()` does not replay acceptance, progress, or completion events. Refresh UI from `AllQuests`/`GetState` after loading.

- Empty quest IDs and null states are skipped. For duplicate IDs, the first saved state wins.
- Missing progress/completion lists are recreated, and both lists grow to the largest saved/config count without discarding entries.
- States for unregistered configs are retained. Call `RegisterQuest(config)` after loading a runtime config to grow its saved lists for newly added objectives.
- Both known-at-load and later-registered configs use the same reconciliation: completed binary objectives (`CustomCondition`, `ReachPoint`, `Talk`) from older saves recover progress `1` from their completion flag. Counter progress, completion flags and quest status remain unchanged; reconciliation emits no gameplay events and does not write the save.
- Normalization finishes before replacing the live registry. A parse/normalization exception leaves the previous registry in place and reports through `NeoDiagnostics`.

Objective identity is positional. Append objectives to preserve existing indices; changes to order or meaning need an application migration. `NextQuestIds` is not automatically processed during load or completion.

`UnregisterQuest(expected)` removes only the exact current config registration. It retains the state and stored JSON, including active, completed and failed quests. Use it before disposing an owned runtime config; later registration of the same ID can resume that progress. `ResetQuest` deliberately removes progress and serves a different purpose. No save-format or key migration is required for these registration APIs.

`QuestManager.Save()` calls `SaveProvider.SetString`. Disk flush policy belongs to the selected provider; call `SaveProvider.Save()` at an application save checkpoint when an immediate flush is required. Quest Auto Save does not imply an immediate provider flush on every objective.

---

## See Also

If you use the Neoxider Save module (`SaveProvider`, `SaveField`) — align the format and load timing with its documentation. Overview: [Save/README](../Save/README.md).
