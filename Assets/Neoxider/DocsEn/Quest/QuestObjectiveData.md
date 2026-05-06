# QuestObjectiveData

**Purpose:** A data class (`[Serializable]`) describing a single specific task within a quest (`QuestConfig`). It is not a MonoBehaviour.

## Description

Each objective has a `Type` that defines how it will be completed. Depending on the type, different approaches are used:

| Type | Description and Usage |
|------|-----------------------|
| `CustomCondition` | Completed via an external trigger (e.g., from the `QuestNoCodeAction` script or directly from code: `CompleteObjective(id, index)`). You can also define a `ConditionEntry` condition that the manager will evaluate. |
| `KillCount` | The player must kill an enemy with `TargetId`. When the enemy dies, `QuestManager.I.NotifyKill(targetId)` must be called. The manager automatically increments progress. Required amount: `RequiredCount`. |
| `CollectCount` | Similar to KillCount, but for collecting items `QuestManager.I.NotifyCollect(targetId)`. |
| `ReachPoint` | Used for map triggers (the player must reach a specific point). |
| `Talk` | Used for dialogues with NPCs (the player must talk to them). |

## Key Fields

| Field | Description |
|-------|-------------|
| `_type` | The objective type (from the list above). |
| `_targetId` | The target identifier (e.g., `boar`). |
| `_requiredCount` | The required amount (for `KillCount` and `CollectCount` types). |
| `_displayText` | Custom objective text that will be displayed in the UI. |
| `_condition` | A condition for automatic completion (only for `CustomCondition`). |

## See Also
- [QuestConfig](QuestConfig.md)
- [QuestManager](QuestManager.md)
