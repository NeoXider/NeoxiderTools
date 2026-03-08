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
| `CompleteObjective` | Completes one quest objective |
| `Fail` | Fails a quest |
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

## Typical scenarios

- Accept quest from a UI button
- Complete an objective from a trigger volume or pickup
- Reset one quest during debug or restart flows
- Drive quest progression through inspector-only gameplay logic

## See also

- [README](./README.md)
- [QuestManager](./QuestManager.md)
- [QuestConfig](./QuestConfig.md)
- [Russian Quest docs](../../Docs/Quest/README.md)
