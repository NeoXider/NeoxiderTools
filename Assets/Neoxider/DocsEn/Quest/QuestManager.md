# QuestManager

`QuestManager` is the central runtime component of the quest module. It stores `QuestState` entries, accepts quests, advances objectives, checks start conditions, and raises both UnityEvents and C# events. File: `Assets/Neoxider/Scripts/Quest/QuestManager.cs`, namespace: `Neo.Quest`.

## Typical setup

1. Add `QuestManager` to a scene object.
2. Fill `Known Quests` with every `QuestConfig` you plan to access by id.
3. Assign `Condition Context` when start conditions should evaluate against a player or other scene object.
4. Accept quests through code, UI events, or `QuestNoCodeAction`.
5. Advance progress with `CompleteObjective(...)`, `NotifyKill(...)`, and `NotifyCollect(...)`.

## Inspector fields

| Field | Purpose |
|------|---------|
| `Condition Context` | Scene object passed into start-condition evaluation. |
| `Known Quests` | Quest registry used for id-based lookups. |
| `Editor Quest Id` / `Editor Objective Index` | Debug helper fields used by editor buttons. |

## Main API

| API | Description |
|-----|-------------|
| `AcceptQuest(string questId)` / `AcceptQuest(QuestConfig quest)` | Accepts a quest if it exists and start conditions pass. |
| `TryAcceptQuest(string questId, out string failReason)` | Same as above, but returns a reason string on failure. |
| `CompleteObjective(string questId, int objectiveIndex)` | Advances a specific objective. |
| `NotifyKill(string enemyId)` | Increments matching `KillCount` objectives. |
| `NotifyCollect(string itemId)` | Increments matching `CollectCount` objectives. |
| `FailQuest(...)` | Marks a quest as failed. |
| `ResetQuest(...)` / `RestartQuest(...)` | Clears or restarts one quest. |
| `ResetAllQuests()` | Clears all runtime quest states. |
| `GetState(...)` | Returns the current `QuestState` for a quest. |
| `AllQuests`, `ActiveQuests` | Runtime state collections. |

## Events

UnityEvents:

- `OnQuestAccepted`
- `OnObjectiveProgress`
- `OnObjectiveCompleted`
- `OnQuestCompleted`
- `OnQuestFailed`
- `OnAnyQuestAccepted`
- `OnAnyQuestCompleted`

C# events:

- `QuestAccepted`
- `ObjectiveProgress`
- `QuestCompleted`

## Notes

- `QuestManager.Instance` can be `null` before initialization or after destruction.
- The module does not provide a full built-in save/restore pipeline for quest lists; project-level integration is still required.

## See also

- [README](./README.md)
- [Russian Quest docs](../../Docs/Quest/README.md)
- [Save](../Save/README.md)
