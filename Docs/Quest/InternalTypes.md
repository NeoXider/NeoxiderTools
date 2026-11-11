# Quest - Internal Types

**What it is:** supporting public types of the Quest module (`Scripts/Quest/`) that have no page of their own.

**How to use:** reference them from code that reads quest state or configures the NoCode bridge; see the linked pages for behaviour.

---

| Type | Description |
|------|-------------|
| `QuestStatus` | Quest status enum: `NotStarted`, `Active`, `Completed`, `Failed`. Stored as an int in saves. |
| `QuestObjectiveType` | Objective type enum: `CustomCondition`, `KillCount`, `CollectCount`, `ReachPoint`, `Talk`. See [QuestObjectiveData](QuestObjectiveData.md). |
| `QuestNoCodeAction.ActionType` | Bridge action enum: `Accept`, `CompleteObjective`, `Fail`, `Restart`, `Reset`, `ResetAll`, `NotifyKill`, `NotifyCollect`, `NotifyReachPoint`, `NotifyTalk`, `EvaluateObjectiveConditions`. Values are serialized; new members are appended. See [Quest NoCode Action](QuestBridge.md). |
| `QuestFlowConfig.QuestChain` | One chain of a [QuestFlowConfig](QuestFlowConfig.md): `ChainId`, `DisplayName`, `StrictOrder`, `Quests`. |

## See Also
- [Quest NoCode Action](QuestBridge.md)
- [Quest](README.md)
