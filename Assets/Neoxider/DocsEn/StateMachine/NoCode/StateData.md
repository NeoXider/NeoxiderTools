# StateData

**Purpose:** ScriptableObject state for No-Code state machine. Contains a name and lists of enter, update, and exit actions. Implements `IState` — can be used directly in `StateMachine`. Fully configured in Inspector without code.

**Create:** Create → Neoxider → State Machine → State Data.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **State Name** | State name for identification. Default `"New State"`. |
| **On Enter Actions** | `StateAction` list — actions executed once when entering the state. |
| **On Update Actions** | `StateAction` list — actions executed every frame while the state is active. |
| **On Exit Actions** | `StateAction` list — actions executed once when exiting the state. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `string StateName { get; set; }` | State name. |
| `List<StateAction> OnEnterActions { get; }` | Enter actions. |
| `List<StateAction> OnUpdateActions { get; }` | Update actions. |
| `List<StateAction> OnExitActions { get; }` | Exit actions. |
| `void OnEnter()` | Called by the state machine on enter. Executes all `OnEnterActions`. |
| `void OnUpdate()` | Called every frame. Executes all `OnUpdateActions`. |
| `void OnExit()` | Called on exit. Executes all `OnExitActions`. |

---

## Examples

### No-Code (Inspector)
1. **Create → Neoxider → State Machine → State Data** — create an `IdleState` asset.
2. Set **State Name** = `"Idle"`.
3. In **On Enter Actions**, add a `StateAction` (e.g. `LogAction` with "Entered Idle").
4. In **On Update Actions**, add per-frame actions.
5. Assign this asset to `StateMachineData` (in the **States** array).

### Code
```csharp
var sm = new StateMachine<IState>();
sm.ChangeState(idleStateData); // StateData implements IState
```

---

## See Also
- [StateMachineData](StateMachineData.md) — machine configuration (states + transitions)
- [StateAction](../StateAction.md) — base action class
- [StateMachine](../StateMachine.md) — state machine core
- ← [StateMachine](../README.md)
