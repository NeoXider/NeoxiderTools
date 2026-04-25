# QuestManager

**Purpose:** Main quest manager (Singleton). Stores the state of all accepted, failed, and completed quests. Automatically saves progress to `SaveProvider`. Handles quest acceptance logic (with condition checking) and tracks objective completion.

## Setup

1. Add `Add Component > Neoxider > Quest > QuestManager` to a global scene object.
2. Drag all available `QuestConfig`s into the `_knownQuests` array. This acts as the database the manager uses to look up quests by ID.
3. Ensure `_autoSave` and `_autoLoad` are checked.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_conditionContext` | The `GameObject` (usually the player) passed to `ConditionEntry` for evaluations. For example, to check the player's level before granting a quest. |
| `_knownQuests` | Database of all existing quests. |
| `_saveKey` | The key used in `SaveProvider` to save progress. |
| `_autoSave`, `_autoLoad` | Automatically save/load the quest list. |
| Events (`_onQuestAccepted`, etc.) | Unity Events you can bind UI to (e.g., to show a "Quest Updated" popup). |

## API and Usage

```csharp
// Accept a quest by ID (returns false if conditions aren't met or already accepted)
bool success = QuestManager.I.AcceptQuest("kill_boars");

// Complete the first objective (index 0) of a quest
QuestManager.I.CompleteObjective("kill_boars", 0);

// If the objective is KillCount: notify the manager an enemy 'boar' was killed.
// The manager will automatically find quests needing this mob and add +1.
QuestManager.I.NotifyKill("boar");

// Fail a quest
QuestManager.I.FailQuest("escort_npc");
```

## See Also
- [QuestConfig](QuestConfig.md)
- [QuestState](QuestState.md) - how progress is stored.
- [Module Root](../README.md)
