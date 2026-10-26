# Quest module

The `Quest` module provides a compact quest runtime built around `QuestConfig`, `QuestManager`, runtime `QuestState`, and a UnityEvent-friendly no-code bridge component.

Demo in the release package: `Assets/Neoxider/Samples~/Demo/Scenes/Quests/QuestDemo.unity` — runtime-built UI via `NeoDemoShell`, controller `Demo/Scripts/Shell/QuestDemoController.cs`. During sample development the root is `Assets/Neoxider/Samples`; Package Manager imports samples under `Assets/Samples/NeoxiderTools/<version>/<sample name>/`.

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
| [C# guide](./Code.md) | Typed calls, progress notifications, and subscriptions |
| [Inspector scenarios](./Scenarios.md) | Scene setup and UnityEvent wiring |
| [Quest flow](./QuestFlowConfig.md) | Optional sequence and availability checks |
| [Saving](./Saving.md) | SaveProvider storage and objective list migration |

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

Each counter call adds one; other objective types store progress `1` when completed. `CustomCondition` entries are checked only when you call `EvaluateObjectiveConditions()`. `NextQuestIds` is metadata for your game/UI: the manager does not automatically accept or unlock those quests. `QuestFlowConfig` is an optional gate applied by your code or by `QuestNoCodeAction.FlowConfig`.

Use the Inspector action buttons in Play Mode. They can change saved progress when Auto Save is enabled.

## Key API

- `AcceptQuest(string questId)` and `AcceptQuest(QuestConfig quest)`
- `TryAcceptQuest(string questId, out string failReason)`
- `RegisterQuest(QuestConfig quest)` and `bool UnregisterQuest(QuestConfig expected)` — release only your exact runtime config before destroying it; progress is retained. See [runtime config lifetime](Code.md#runtime-config-lifetime).
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

Configs registered after loading reconcile saved objective lists and completed binary progress exactly as known-at-load configs do, without replaying gameplay events. Unregistering a config preserves saved states for later registration.

## See also

- [Condition](../Condition/README.md)
- [Save](../Save/README.md)
