# Quest module

The `Quest` module provides a compact quest runtime built around `QuestConfig`, `QuestManager`, runtime `QuestState`, and a UnityEvent-friendly no-code bridge component.

## Main pieces

- `QuestConfig` stores quest id, title, description, objectives, start conditions, and next quest ids.
- `QuestManager` accepts quests, tracks runtime state, completes objectives, and raises UnityEvents or C# events.
- `QuestState` stores per-quest runtime progress and status.
- `QuestNoCodeAction` lets you trigger quest actions from Inspector events (`Accept`, `CompleteObjective`, `Fail`, `Restart`, `Reset`, `ResetAll`).
- `QuestContext` marks the object used as condition evaluation context for quest start conditions.

## Entry pages

| Page | Description |
|------|-------------|
| [QuestConfig](./QuestConfig.md) | Quest asset structure, objectives, and start conditions |
| [QuestManager](./QuestManager.md) | Scene runtime manager, events, and main API |
| [QuestState](./QuestState.md) | Runtime quest state, progress, and UI-facing data |
| [Quest NoCode Action](./QuestBridge.md) | UnityEvent bridge for inspector-driven quest actions |
| [Russian Quest docs](../../Docs/Quest/README.md) | Full Russian module documentation |

## Objective types

- `CustomCondition`
- `KillCount`
- `CollectCount`
- `ReachPoint`
- `Talk`

## Typical flow

1. Create one or more `QuestConfig` assets.
2. Add `QuestManager` to the scene and register configs in `Known Quests`.
3. Assign `Condition Context` if start conditions should be evaluated against a player or world object.
4. Accept quests via `AcceptQuest(...)`, `QuestNoCodeAction(Accept)`, or UI events.
5. Progress objectives through `CompleteObjective(...)`, `NotifyKill(...)`, `NotifyCollect(...)`, or `QuestNoCodeAction(CompleteObjective)`.

## Key API

- `AcceptQuest(string questId)` and `AcceptQuest(QuestConfig quest)`
- `TryAcceptQuest(string questId, out string failReason)`
- `CompleteObjective(string questId, int objectiveIndex)`
- `NotifyKill(string enemyId)`
- `NotifyCollect(string itemId)`
- `GetState(string questId)` and `GetState(QuestConfig quest)`
- `FailQuest(...)`
- `AllQuests`, `ActiveQuests`

## Events

`QuestManager` exposes both Inspector-facing `UnityEvent` callbacks and C# events:

- `OnQuestAccepted`
- `OnObjectiveProgress`
- `OnObjectiveCompleted`
- `OnQuestCompleted`
- `OnQuestFailed`
- `OnAnyQuestAccepted`
- `OnAnyQuestCompleted`

## Save note

The current module does not ship with a built-in save/load restoration API for quest states. `QuestState` is serializable, but restoring a saved list still requires project-level integration.

## See also

- Russian docs: [`../../Docs/Quest/README.md`](../../Docs/Quest/README.md)
- [Condition](../Condition/README.md)
- [Save](../Save/README.md)
