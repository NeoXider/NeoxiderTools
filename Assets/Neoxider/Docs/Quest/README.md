# Quest module

The `Quest` module provides a compact quest runtime built around `QuestConfig`, `QuestManager`, runtime `QuestState`, and a UnityEvent-friendly no-code bridge component.

Demo: `Samples/Demo/Scenes/Quests/QuestDemo.unity` — runtime-built UI via `NeoDemoShell`, controller `Samples/Demo/Scripts/Shell/QuestDemoController.cs`.

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

`QuestManager` persists quest states through `SaveProvider` under `Save Key` (default `Settings_Quests`). `Auto Save` saves on accept/complete/fail/reset, `Auto Load` restores on initialization; `Save()` / `Load()` are public for manual control. See [Saving](./Saving.md).

## See also

- [Condition](../Condition/README.md)
- [Save](../Save/README.md)
