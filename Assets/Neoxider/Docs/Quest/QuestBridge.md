# Quest NoCode Action

**What it is:** `QuestNoCodeAction` (MonoBehaviour, `Scripts/Quest/Bridge/QuestNoCodeAction.cs`) is the UnityEvent bridge for quest actions without custom code. Each action calls one typed `QuestManager` method on the singleton, so everything it does is also available from C#.

**How to use:**
1. Add **Add Component > Neoxider > Quest > Quest NoCode Action** to a scene object (a button, trigger, enemy prefab, pickup).
2. Choose the **Action Type**.
3. Assign **Quest** (and **Objective Index**) for quest actions, or **Target Id** for notification actions.
4. Call `Execute()` from a button, trigger, condition or any other UnityEvent source.
5. Wire **On Success / On Failed / On Result Message** to UI or logs when feedback is needed.

---

## Supported actions

| Action | QuestManager call | Needs |
|--------|-------------------|-------|
| `Accept` | `AcceptQuest(Quest)` (after the optional Flow Config gate) | Quest |
| `CompleteObjective` | `AddObjectiveProgress(Quest, Objective Index, Amount)`: counters add **Amount**, binary objectives complete | Quest, Objective Index |
| `Fail` | `FailQuest(Quest)` | Quest |
| `Restart` | `RestartQuest(Quest)` | Quest |
| `Reset` | `ResetQuest(Quest)` | Quest |
| `ResetAll` | `ResetAllQuests()` | — |
| `NotifyKill` | `Notify(KillCount, Target Id, Amount)` | Target Id |
| `NotifyCollect` | `Notify(CollectCount, Target Id, Amount)` | Target Id |
| `NotifyReachPoint` | `Notify(ReachPoint, Target Id)` | Target Id |
| `NotifyTalk` | `Notify(Talk, Target Id)` | Target Id |
| `EvaluateObjectiveConditions` | `EvaluateObjectiveConditions()` | — |

New action values are appended to the enum, so existing scenes keep their serialized selection.

## Fields

| Field | Description |
|------|-------------|
| **Action Type** | Selected quest action. |
| **Quest** | Target quest for the quest actions. |
| **Objective Index** | 0-based objective index for `CompleteObjective`. |
| **Target Id** | Id matched against `QuestObjectiveData.TargetId` (ordinal) by the Notify actions. |
| **Amount** | Progress added by `CompleteObjective` and `NotifyKill` / `NotifyCollect` to counters (minimum 1, default 1). |
| **Flow Config** | Optional [QuestFlowConfig](QuestFlowConfig.md) gate for `Accept` / `Restart`. |

Runtime accessors: `Action`, `Quest`, `ObjectiveIndex`, `TargetId`, `Amount`, `FlowConfig`. `SetTargetId(string)` is a one-argument setter for UnityEvent wiring before `Execute()` (for example a trigger that passes the id of the entered point).

## Events

- **On Success** — the action was applied.
- **On Failed (string)** — the action was rejected; the argument is the reason.
- **On Result Message (string)** — one message for both outcomes.

Missing/inactive quests, invalid or already completed objectives, empty quest ids and unsupported action values emit `On Failed`. `CompleteObjective` can succeed while a counter remains below its required count. Completing or failing an already finished quest emits failure. Notify actions succeed when at least one objective was credited; an empty Target Id or no matching active objective emits failure (`No active KillCount objective matches 'orc'.`), which is expected for kills that no quest tracks. The result message is reused while the type, Target Id and credited count repeat, so repeated notifications from pooled prefabs do not allocate once warm; changing the Target Id before every call formats a new message. `QuestManager.NotifyKill(string)` called from code or bound directly is allocation-free in every case.

All Inspector action buttons are available in Play Mode. Unity's Edit Mode `Reset()` callback does not execute a quest action or invoke result events. `Restart` removes the old state and retries start conditions; failed acceptance does not restore the old progress. The optional flow gate (`QuestFlowConfig.IsChainSatisfied`) is checked on `Accept` and on `Restart` when there is no existing state; start conditions then run once inside `TryAcceptQuest`, and a rejected accept reports `Accept failed: <id> (<reason>)`.

## Typical scenarios

- Accept a quest from a UI button.
- Credit a kill from an enemy prefab: **Action Type** `NotifyKill`, **Target Id** `boar`, `Execute()` from the death event. The prefab does not need a scene reference to the manager.
- Complete a "reach the mill" objective from a trigger volume: `NotifyReachPoint` with **Target Id** `old_mill`.
- Re-check `CustomCondition` objectives at a checkpoint: `EvaluateObjectiveConditions`.
- Reset one quest during debug or restart flows.

For code, call `QuestManager` directly: `AcceptQuest`, `AddObjectiveProgress`, `NotifyKill`, `Notify`, `FailQuest`, `RestartQuest`, `ResetQuest`, `ResetAllQuests`.

## See also

- [README](README.md)
- [QuestManager](QuestManager.md)
- [QuestConfig](QuestConfig.md)
- [Inspector scenarios](Scenarios.md)
