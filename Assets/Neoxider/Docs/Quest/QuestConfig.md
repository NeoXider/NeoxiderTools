# QuestConfig

**Purpose:** Configuration for a single quest. Created as a `ScriptableObject`. It stores UI text (title, description), a list of objectives, and the conditions under which the quest can be accepted.

## Setup

1. Create a quest via the menu `Right Click > Create > Neoxider > Quest > Quest Config`.
2. Set the `_id` (a unique identifier, e.g., `kill_boars`).
3. Fill in `_title` and `_description`.
4. Configure the `_objectives` list (see [QuestObjectiveData](QuestObjectiveData.md)).
5. If needed, specify `_startConditions` (e.g., "Player must be level 5").

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_id` | Unique quest ID. Used in code for accepting/saving. |
| `_title`, `_description` | Name and detailed description for the UI. |
| `_icon` | (Optional) Quest sprite. |
| `_objectives` | A list of tasks that must be completed to finish the quest. |
| `_startConditions` | A list of conditions (NeoCondition) checked before giving the quest. |
| `_nextQuestIds` | Optional next-quest metadata for UI/game code; no automatic acceptance or unlock behavior. |

Use unique, non-empty IDs and define at least one objective. Keep objective indices stable in released content; appending objectives is supported during load, while reordering requires your own save migration. Start conditions are evaluated against `QuestManager.ConditionContext` (or the manager's GameObject if unassigned); avoid scene object references in reusable quest assets.

## See Also

- [QuestFlowConfig](QuestFlowConfig.md) - For linking quests into chains.
- [QuestManager](QuestManager.md) - Main manager.
- [Module Root](../README.md)
