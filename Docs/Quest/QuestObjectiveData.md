# QuestObjectiveData

**What it is:** a `[Serializable]` data class (`Scripts/Quest/QuestObjectiveData.cs`) describing one task inside a `QuestConfig`: its type, target id, required count, UI text and optional completion condition. It is not a MonoBehaviour.

**How to use:**
1. Add entries to **Objectives** on a [QuestConfig](QuestConfig.md); the list order defines the objective index (0, 1, ...).
2. Choose the **Type** and fill **Target Id** for target-driven types.
3. Report gameplay to `QuestManager` with the matching notification (table below) or complete the objective by index.

---

## Objective types

| Type | Progress | Credited by |
|------|----------|-------------|
| `CustomCondition` | binary | `CompleteObjective(quest, index)`, `QuestNoCodeAction(CompleteObjective)`, or `EvaluateObjectiveConditions()` when **Condition** is assigned. `Notify(CustomCondition, targetId)` can address it by a custom event id. |
| `KillCount` | counter | `NotifyKill(targetId[, amount])` until `RequiredCount`. |
| `CollectCount` | counter | `NotifyCollect(targetId[, amount])` until `RequiredCount`. |
| `ReachPoint` | binary | `NotifyReachPoint(targetId)`, for map triggers. |
| `Talk` | binary | `NotifyTalk(targetId)`, for NPC dialogues. |

Every type can also be credited by index with `CompleteObjective` / `AddObjectiveProgress`. Counter progress is clamped to `RequiredCount`; binary objectives store progress `1` when completed.

## Fields

| Field | Description |
|-------|-------------|
| `_type` | Objective type (table above). |
| `_targetId` | Target identifier matched ordinally by the notifications (e.g. `boar`). An empty id is never matched by a notification; such an objective is credited only by index (or by its **Condition**). |
| `_requiredCount` | Required amount for `KillCount` / `CollectCount` (values below 1 behave as 1). |
| `_displayText` | Custom UI text; when empty, UI can build text from Type/TargetId. |
| `_condition` | `ConditionEntry` evaluated by `EvaluateObjectiveConditions()` (only for `CustomCondition`). |

## Properties

| Property | Description |
|----------|-------------|
| `Type`, `TargetId`, `RequiredCount`, `DisplayText`, `Condition` | Read/write access to the fields above (for runtime-built configs). |
| `bool IsCounter` | True for `KillCount` / `CollectCount`. |
| `int TargetProgress` | Progress that completes the objective: `RequiredCount` (at least 1) for counters, otherwise 1. Useful for "3 / 5" UI. |

```csharp
QuestObjectiveData objective = config.Objectives[index];
int current = QuestManager.I.GetObjectiveProgress(config, index);
string label = objective.IsCounter ? $"{current} / {objective.TargetProgress}" : (current > 0 ? "Done" : "");
```

## See Also
- [QuestConfig](QuestConfig.md)
- [QuestManager](QuestManager.md)
