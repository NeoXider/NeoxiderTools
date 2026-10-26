# Quest NoCode Action

`QuestNoCodeAction` is the UnityEvent bridge for calling quest runtime actions without custom code. It wraps the main quest API behind one scene-friendly component. File: `Assets/Neoxider/Scripts/Quest/Bridge/QuestNoCodeAction.cs`.

## Typical use

1. Add `QuestNoCodeAction` to a scene object.
2. Choose the `Action Type`.
3. Assign a `QuestConfig` when needed.
4. Fill `Objective Index` for objective completion flows.
5. Call `Execute()` from a button, trigger, condition, or another UnityEvent source.

## Supported actions

| Action | Description |
|--------|-------------|
| `Accept` | Accepts a quest |
| `CompleteObjective` | Adds one to a counter, or completes a binary objective; requires an active quest and unfinished valid objective |
| `Fail` | Changes an active quest to Failed |
| `Restart` | Restarts a quest |
| `Reset` | Clears one quest state |
| `ResetAll` | Clears all quest states |

## Main fields

| Field | Description |
|------|-------------|
| `Action Type` | Selected quest action |
| `Quest` | Target quest for all actions except `ResetAll` |
| `Objective Index` | Used by `CompleteObjective` |
| `Flow Config` | Optional flow restriction for quest acceptance |

## Events

- `On Success`
- `On Failed(string)`
- `On Result Message(string)`

Success means the requested operation was accepted. Missing/inactive quests, invalid or already completed objectives, empty quest IDs, and unsupported action values emit `On Failed` and a result message. `CompleteObjective` can succeed while a counter remains below its required count. Completing or failing an already finished quest emits failure.

All Inspector action buttons are available in Play Mode. Unity's Edit Mode `Reset()` callback does not execute a quest action or invoke result events. `ResetAll` does not need a quest assignment. `Restart` removes the old state and retries start conditions; failed acceptance does not restore the old progress. The optional flow gate is checked on Accept and on Restart when there is no existing state.

The same operations are available to programmers through `QuestManager.AcceptQuest`, `CompleteObjective`, `FailQuest`, `RestartQuest`, `ResetQuest`, and `ResetAllQuests`. Bind gameplay systems directly to that typed API when a scene bridge is unnecessary.

## Typical scenarios

- Accept quest from a UI button
- Complete an objective from a trigger volume or pickup
- Reset one quest during debug or restart flows
- Drive quest progression through inspector-only gameplay logic

## See also

- [README](./README.md)
- [QuestManager](./QuestManager.md)
- [QuestConfig](./QuestConfig.md)
- [Quest docs](./README.md)
