# StateMachineData

**What it is:** a `ScriptableObject` configuration for a complete NoCode state machine: a `StateData[]` array (states), the initial state and a `StateTransition` list (transitions). Loaded by `StateMachineBehaviourBase` / `StateMachineBehaviour<TState>` in `Start()`. Path: `Scripts/StateMachine/NoCode/StateMachineData.cs`, namespace `Neo.StateMachine.NoCode`.

**How to use:**
1. **Create → Neoxider → State Machine → State Machine Data**.
2. Drag the `StateData` assets into **States** and set **Initial State**.
3. Add **Transitions** (From/To state, priority, predicates).
4. Assign the asset to a State Machine Behaviour; scene objects for conditions go into the behaviour's **Context Overrides**, never into this asset.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **States** | `StateData[]` — all states in the machine. Each is a separate ScriptableObject. |
| **Initial State** | Reference to the starting `StateData`. The wrappers enter this exact asset on load. |
| **Initial State Name** | Starting state name (legacy, used when **Initial State** is not set). |
| **Transitions** | `StateTransition` list — transition rules between states with predicates. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `StateData[] States { get; set; }` | All states. Null is treated as an empty array. |
| `StateData InitialState { get; set; }` | Initial state (reference). |
| `string InitialStateName { get; set; }` | Initial state name (the asset's name when `InitialState` is set). |
| `List<StateTransition> Transitions { get; }` | Transition list. |
| `void LoadIntoStateMachine<TState>(StateMachine<TState>)` | Register all non-null transitions in a state machine. |
| `StateData GetStateByName(string name)` | First state with that name (ordinal); null for an empty name. |
| `bool Validate(bool silent = false)` | False for an empty configuration, missing/unknown initial state, or missing/unknown NoCode source/target. `silent = true` suppresses diagnostics; `silent = false` is an explicit request and always writes them as warnings (the automatic Play Mode check in `OnValidate` stays silent unless a behaviour with **Enable Debug Log** is enabled). |

Validation checks NoCode asset endpoints; use the core API directly for CLR-only or source-less global rules. Loading does not require validation, clear previous registrations, enter a state, or clone predicates. Wrappers clear rules and enter the **Initial State** asset during reload. Built-in duration timing is isolated per machine by default, including composite children, so a shared configuration does not reset another machine's wait. Custom mutable predicates/actions still require their own ownership design. Manual machine timing uses `machine.SetCurrentStateEnterTime(...)`, while predicate setters retain standalone behavior.

---

## Examples

### NoCode (Inspector)
1. **Create → Neoxider → State Machine → State Machine Data** — create `EnemyAI_SM`.
2. Create several `StateData` assets: `Patrol`, `Chase`, `Attack`.
3. Drag all three into **States**.
4. Set **Initial State** to `Patrol`.
5. In **Transitions**, add `Patrol → Chase` with a predicate (for example **Neoxider Condition**).
6. Assign `EnemyAI_SM` to a State Machine Behaviour on a GameObject.

### Code
```csharp
StateMachine<IState> machine = new();
stateMachineData.LoadIntoStateMachine(machine);
machine.ChangeState(stateMachineData.InitialState);
```

---

## See also
- [StateData](StateData.md) — individual state
- [StateTransition](../StateTransition.md) — transition rule
- [StateMachineBehaviourBase](../StateMachineBehaviourBase.md) — MonoBehaviour wrapper
- ← [StateMachine](../README.md)
