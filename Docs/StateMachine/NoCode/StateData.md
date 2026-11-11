# StateData

**What it is:** a `ScriptableObject` state for the NoCode state machine: a name plus enter, update and exit action lists. It implements `IState`, so it also works directly with `StateMachine<IState>`. Path: `Scripts/StateMachine/NoCode/StateData.cs`, namespace `Neo.StateMachine.NoCode`.

**How to use:**
1. **Create → Neoxider → State Machine → State Data** and give the asset a unique **State Name**.
2. Fill **On Enter / On Update / On Exit Actions** with `StateAction`s.
3. Add the asset to a `StateMachineData` **States** array, or change to it from C# (`behaviour.ChangeState(stateData)`).

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **State Name** | State name used by name-based lookups (`ChangeState(string)`, legacy initial name). Default `"New State"`; keep names unique per `StateMachineData`. |
| **On Enter Actions** | `StateAction` list — executed once when entering the state. |
| **On Update Actions** | `StateAction` list — executed every frame while the state is active. |
| **On Exit Actions** | `StateAction` list — executed once when exiting the state. |

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

Each action receives the StateMachineBehaviour owner as context. An exception in one action is logged (gated by the debug log of the machine running the state) and the remaining actions still run; actions may edit the list they run from without aborting it. A pass runs only the entries present when it started (an appended action first runs on the next pass); removing an earlier entry shifts the rest, so the next action is skipped for that pass. `OnFixedUpdate` / `OnLateUpdate` do nothing for NoCode states.

---

## Examples

### NoCode (Inspector)
1. **Create → Neoxider → State Machine → State Data** — create an `IdleState` asset.
2. Set **State Name** = `"Idle"`.
3. In **On Enter Actions**, add a `StateAction` (e.g. `SetContextGameObjectActiveAction` for the Owner).
4. In **On Update Actions**, add per-frame actions.
5. Assign this asset to `StateMachineData` (in the **States** array).

### Code
```csharp
StateMachine<IState> machine = new();
machine.ChangeState(idleStateData); // StateData implements IState
```

---

## See also
- [StateMachineData](StateMachineData.md) — machine configuration (states + transitions)
- [StateAction](../StateAction.md) — base action class
- [StateMachine](../StateMachine.md) — state machine core
- ← [StateMachine](../README.md)
