# QuestManager

**What it is:** the quest runtime (`Singleton<QuestManager>`, `Scripts/Quest/QuestManager.cs`, namespace `Neo.Quest`). It stores the state of every accepted, completed and failed quest, accepts quests after checking start conditions, credits objectives (by index or by target id), raises UnityEvents and C# events, and persists states to `SaveProvider`.

**How to use:**
1. Add **Add Component > Neoxider > Quest > QuestManager** to a global scene object.
2. Drag the available `QuestConfig` assets into **Known Quests**, or call `RegisterQuest(config)` from C#. Accepting by config also registers it.
3. Assign **Condition Context** (usually the player) when quests have start conditions or `CustomCondition` objectives.
4. Keep **Auto Save** / **Auto Load** enabled for automatic persistence.
5. Accept quests with `AcceptQuest`, progress them with `NotifyKill` / `NotifyCollect` / `NotifyReachPoint` / `NotifyTalk` or `CompleteObjective`, and bind UI to the events. Without code use [QuestNoCodeAction](QuestBridge.md).

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Condition Context** (`_conditionContext`) | `GameObject` passed to `ConditionEntry.Evaluate` for start conditions and `CustomCondition` objectives. Falls back to the manager's GameObject. |
| **Known Quests** (`_knownQuests`) | Configs used for id -> config resolution (`AcceptQuest(string)`, notifications, loading). |
| **Save Key** (`_saveKey`) | `SaveProvider` key of the state JSON. Default `Settings_Quests`. |
| **Auto Save** (`_autoSave`) | Save after accept, progress, fail and reset. Also available as the `AutoSave` property. |
| **Auto Load** (`_autoLoad`) | Call `Load()` during singleton initialization. |
| **Editor Quest Id / Editor Objective Index** | Targets of the Play Mode Inspector buttons. |
| Events (`_onQuestAccepted`, ...) | UnityEvents for UI bindings, see [Events](#events). |

## Methods

### Acceptance and registration

| Method | Description |
|--------|-------------|
| `bool AcceptQuest(string questId)` / `AcceptQuest(QuestConfig)` | Accepts when the id is non-empty, no state exists and all start conditions pass. Accepting by config registers it. |
| `bool TryAcceptQuest(string questId, out string failReason)` / `TryAcceptQuest(QuestConfig, out string failReason)` | Same with a reason (`Quest not found.`, `Already accepted or completed.`, `Start conditions not met.`). Start conditions are evaluated once per attempt. |
| `bool CanAcceptQuest(QuestConfig, out string reason)` | Availability check without accepting. |
| `void RegisterQuest(QuestConfig)` / `bool UnregisterQuest(QuestConfig expected)` | Runtime config registration; see [Runtime config ownership](#runtime-config-ownership). |
| `QuestConfig GetConfig(string questId)` | Registered config or null. |

### Progress

| Method | Description |
|--------|-------------|
| `void CompleteObjective(string questId, int index)` / `CompleteObjective(QuestConfig, int index)` | Adds one to a counter (`KillCount`, `CollectCount`) or completes a binary objective (`CustomCondition`, `ReachPoint`, `Talk`). |
| `bool AddObjectiveProgress(QuestConfig, int index, int amount)` / `(string questId, ...)` | Adds `amount` to a counter, clamped to `RequiredCount`; binary objectives complete on any positive amount. Returns false for a nonpositive amount, inactive quest, invalid index or completed objective. |
| `void NotifyKill(string enemyId)` / `NotifyKill(string enemyId, int amount)` | Credits every active `KillCount` objective whose `TargetId` matches. |
| `void NotifyCollect(string itemId)` / `NotifyCollect(string itemId, int amount)` | Same for `CollectCount`. |
| `void NotifyReachPoint(string pointId)` | Completes active `ReachPoint` objectives with that `TargetId`. |
| `void NotifyTalk(string npcId)` | Completes active `Talk` objectives with that `TargetId`. |
| `int Notify(QuestObjectiveType type, string targetId, int amount = 1)` | General form of the notifications above; returns the number of credited objectives. |
| `void EvaluateObjectiveConditions()` | Evaluates assigned `CustomCondition` entries against Condition Context and completes the passing ones. There is no automatic polling loop. |
| `void FailQuest(string)` / `FailQuest(QuestConfig)` | Active -> Failed. |
| `bool ResetQuest(...)`, `bool RestartQuest(...)`, `void ResetAllQuests()` | Remove a state, remove and re-accept, or clear all states. |

`TargetId` matching is ordinal (case-sensitive). A null or empty target id credits nothing, so objectives without a `TargetId` (including default `CustomCondition` entries) are reached only by index or by their `ConditionEntry`. The single-argument `NotifyKill` / `NotifyCollect` / `NotifyReachPoint` / `NotifyTalk` overloads take one `string`, so they can be bound directly to a UnityEvent on the manager.

### Queries

| Member | Description |
|--------|-------------|
| `QuestState GetState(string)` / `GetState(QuestConfig)` | Current state or null. |
| `bool IsActive(...)`, `IsCompleted(...)`, `IsFailed(...)` | Status checks by id or config. |
| `int GetObjectiveProgress(QuestConfig, int index)` | Progress for display. |
| `IReadOnlyList<QuestState> AllQuests` | Every state (live list, do not cache across Load). |
| `int GetActiveQuests(List<QuestState> results)` | Clears and fills a caller-owned buffer without allocating. |
| `IReadOnlyList<QuestState> ActiveQuests` | Convenience copy; allocates a new list per access. |
| `IReadOnlyList<QuestConfig> KnownQuests` | Registered configs. |
| `void Save()` / `void Load()` | Manual persistence; see [Saving](Saving.md). |

## Events

| UnityEvent | C# event | When |
|------------|----------|------|
| `OnQuestAccepted (string questId)`, `OnAnyQuestAccepted ()` | `QuestAccepted (QuestConfig)` | After a state was created. |
| `OnObjectiveProgress (string, int index, int progress)` | `ObjectiveProgress (QuestConfig, int, int)` | After progress was committed. |
| `OnObjectiveCompleted (string, int index)` | `ObjectiveCompleted (QuestConfig, int)` | After an objective became completed (once). |
| `OnQuestCompleted (string)`, `OnAnyQuestCompleted ()` | `QuestCompleted (QuestConfig)` | After the last objective completed the current active state (once). |
| `OnQuestFailed (string)` | `QuestFailed (string questId)` | After `FailQuest` changed an active quest. |
| — | `QuestReset (string questId)` | After `ResetQuest`, each state cleared by `ResetAllQuests`, and the reset step of `RestartQuest`. |
| — | `QuestsLoaded ()` | After `Load()` published restored states. No gameplay events are replayed. |

`QuestFailed` and `QuestReset` pass the id because a failed or removed state may belong to an unregistered config.

## Examples

```csharp
using Neo.Quest;
using UnityEngine;

public sealed class EnemyQuestReporter : MonoBehaviour
{
    [SerializeField] private string _enemyId = "boar";

    public void OnKilled()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager != null)
        {
            manager.NotifyKill(_enemyId);
        }
    }
}
```

```csharp
// Accept, credit by target id, read progress.
QuestManager manager = QuestManager.I;
bool accepted = manager.AcceptQuest("kill_boars");
manager.NotifyKill("boar", 3);             // three kills at once, clamped to RequiredCount
manager.NotifyReachPoint("old_mill");      // ReachPoint objective with TargetId "old_mill"
int credited = manager.Notify(QuestObjectiveType.Talk, "elder");
manager.FailQuest("escort_npc");
```

## Behaviour details

`CanAcceptQuest(config, out string reason)` checks the id, existing state and start conditions. Conditions can invoke user code: if a condition accepts this quest, the query returns false because a state now exists. Acceptance checks this again after conditions, so nested acceptance creates one state and one acceptance event sequence; the outer attempt returns false. `TryAcceptQuest` resolves a registered id and uses the same admission path, evaluating start conditions once per attempt. Keep quest ids unique and stable across saves.

Objective progress and its completion flag are committed before progress callbacks run. Repeated credit for an objective already completed is ignored. Quest completion is raised once for the current active state; callbacks that fail or replace that state prevent the old call from completing it. Notification loops (`Notify*`, `EvaluateObjectiveConditions`) operate on the states present at entry; a quest restarted or accepted by a callback receives the next notification. Custom-condition predicates get the same protection: after each predicate returns, the manager verifies the original state is still current and active before awarding credit.

Callbacks run synchronously. Nested calls can interleave event delivery; read current state when refreshing UI. Remove your listeners when their owner is disabled or destroyed.

If an objective progress or objective-completed listener throws, the manager still completes the quest when that credit finished its last objective (raising the quest-completed events), then rethrows the listener's exception to the caller. A second exception from a quest-completed listener in the same call is logged through `NeoDiagnostics.LogException`. A saved quest is therefore never left Active with every objective completed. A throwing listener still stops the rest of a notification loop.

### Performance

`NotifyKill`, `NotifyCollect`, `Notify`, `EvaluateObjectiveConditions` (without condition entries) and `GetActiveQuests` allocate nothing when warm, including nested notifications from callbacks, so they are safe for high kill/pickup rates. With Auto Save enabled, all changes made by one top-level call and its callbacks (for example a completion handler that accepts the next chain quest) are written with a single `Save()` when the outermost operation finishes; the save also runs when a listener throws after the change was committed, and then contains the completed quest (see above).

## Runtime config ownership

`RegisterQuest(config)` keeps the first registered config for an id. Registration after loading grows restored objective lists and repairs completed legacy binary progress to `1`, just as loading with an already known config does; counter progress and status are preserved. `bool UnregisterQuest(QuestConfig expected)` removes the registration only if `expected` is that exact instance; null, unknown, repeated or stale removal returns false. It preserves all states, progress and saved data, emits no events, and does not save or destroy the config.

Scene code that creates runtime configs should unregister each owned instance before destroying it. Id-based resolution and notifications need a registered config; registering a new config with the same id later reconciles the retained state. A stale owner's cleanup cannot remove that replacement. See the [C# cleanup example](Code.md#runtime-config-lifetime). Inspector-authored assets normally stay in Known Quests for the manager's lifetime.

По-русски: перед уничтожением созданного в коде QuestConfig вызовите `UnregisterQuest` с этим экземпляром. Прогресс сохраняется; чужая регистрация с тем же ID не удаляется.

## Reset, restart, and persistence

`ResetQuest` removes a state; `ResetAllQuests` removes every state. `RestartQuest` removes the old state and then attempts acceptance with current start conditions. If acceptance fails, the removed progress is not restored. The manager does not enforce a flow config by itself; use [QuestFlowConfig](QuestFlowConfig.md) or the bridge's Flow Config.

`Save()` stores the state JSON through `SaveProvider`; `Load()` restores it without replaying gameplay events and raises `QuestsLoaded`. See [Saving](Saving.md) for normalization rules and provider flush timing. Inspector action buttons are available in Play Mode.

## See Also

- [QuestConfig](QuestConfig.md)
- [QuestObjectiveData](QuestObjectiveData.md)
- [QuestState](QuestState.md) - how progress is stored.
- [Quest NoCode Action](QuestBridge.md)
- [Module Root](README.md)
