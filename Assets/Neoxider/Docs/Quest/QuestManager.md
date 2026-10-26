# QuestManager

**Purpose:** Main quest manager (Singleton). Stores the state of all accepted, failed, and completed quests. Automatically saves progress to `SaveProvider`. Handles quest acceptance logic (with condition checking) and tracks objective completion.

## Setup

1. Add `Add Component > Neoxider > Quest > QuestManager` to a global scene object.
2. Drag available `QuestConfig`s into the `_knownQuests` array. Alternatively call `RegisterQuest(config)` from C#. Accepting by config also registers it. Registration after loading grows restored objective lists and repairs completed legacy binary progress to `1`, just as loading with an already known config does. Counter progress and quest status are preserved.
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

`CanAcceptQuest(config, out string reason)` checks the ID, existing state, and start conditions. Conditions can invoke user code: if a condition accepts this quest, the query returns false because a state now exists. Acceptance checks this again after conditions, so nested acceptance creates one state and one acceptance event sequence; the outer attempt returns false. `TryAcceptQuest(id, out string reason)` resolves a registered ID and uses the same admission path, evaluating start conditions once per attempt. Keep quest IDs unique and stable across saves.

`CompleteObjective` adds one for `KillCount`/`CollectCount`, completing at `RequiredCount`. For `CustomCondition`, `ReachPoint`, and `Talk`, it sets progress to `1` and completes the objective. Missing/inactive quests, invalid indices, and already completed objectives are ignored. `EvaluateObjectiveConditions()` explicitly evaluates assigned custom conditions; there is no automatic polling loop. Conditions use `ConditionContext`, falling back to the manager's GameObject.

## Runtime config ownership

`RegisterQuest(config)` keeps the first registered config for an ID. `bool UnregisterQuest(QuestConfig expected)` removes it only if `expected` is that exact instance; null, unknown, repeated or stale removal returns false. It preserves all states, progress and saved data, emits no events, and does not save or destroy the config.

Scene code that creates runtime configs should unregister each owned instance before destroying it. ID-based resolution and notifications need a registered config; registering a new config with the same ID later reconciles the retained state. A stale owner's cleanup cannot remove that replacement. See the [C# cleanup example](Code.md#runtime-config-lifetime). Inspector-authored assets normally stay in Known Quests for the manager's lifetime.

По-русски: перед уничтожением созданного в коде QuestConfig вызовите `UnregisterQuest` с этим экземпляром. Прогресс сохраняется; чужая регистрация с тем же ID не удаляется.

## Events and callbacks

Objective progress and its completion flag are updated before progress callbacks run. Repeated credit for an objective already completed is ignored. Quest completion is raised once for the current active state; callbacks that fail or replace that state prevent the old call from completing it. Notification loops (`NotifyKill`, `NotifyCollect`, `EvaluateObjectiveConditions`) operate on the states present at entry. A quest restarted or accepted by a callback receives the next notification.

Custom-condition getters and methods have the same protection: after each predicate returns, the manager verifies the original state is still current and active before awarding credit. Restarting from inside a predicate leaves the replacement at zero progress until the next explicit evaluation.

Callbacks run synchronously. Nested calls can interleave event delivery; read current state when refreshing UI. Subscribe to UnityEvents or C# events and remove your listeners when their owner is disabled/destroyed.

## Reset, restart, and persistence

`ResetQuest` removes a state; `ResetAllQuests` removes every state. `RestartQuest` removes the old state and then attempts acceptance with current start conditions. If acceptance fails, the removed progress is not restored. The manager does not enforce a flow config by itself; use [QuestFlowConfig](QuestFlowConfig.md) or the bridge's Flow Config.

`Save()` stores the state JSON through `SaveProvider`; `Load()` restores it without replaying gameplay events. Auto Save saves after accept/progress/fail/reset. See [Saving](Saving.md) for normalization rules and provider flush timing. Inspector action buttons are available in Play Mode.

## See Also

- [QuestConfig](QuestConfig.md)
- [QuestState](QuestState.md) - how progress is stored.
- [Module Root](../README.md)
