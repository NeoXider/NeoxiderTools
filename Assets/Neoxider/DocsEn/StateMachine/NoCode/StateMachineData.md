# StateMachineData

**Purpose:** ScriptableObject configuration for a complete No-Code state machine. Contains a `StateData[]` array (states) and a `StateTransition` list (transitions). Loaded into `StateMachineBehaviour` at startup. Fully configured in Inspector.

**Create:** Create → Neoxider → State Machine → State Machine Data.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **States** | `StateData[]` — all states in the machine. Each is a separate ScriptableObject. |
| **Initial State** | Reference to the starting `StateData`. The machine enters this state on startup. |
| **Initial State Name** | Starting state name (legacy, used when `Initial State` is not set). |
| **Transitions** | `StateTransition` list — transition rules between states with conditions. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `StateData[] States { get; set; }` | All states. |
| `StateData InitialState { get; set; }` | Initial state (reference). |
| `string InitialStateName { get; set; }` | Initial state name. |
| `List<StateTransition> Transitions { get; }` | Transition list. |
| `void LoadIntoStateMachine<TState>(StateMachine<TState>)` | Load configuration into a state machine (register all transitions). |
| `StateData GetStateByName(string name)` | Find a state by name. |
| `bool Validate(bool silent = false)` | Validate configuration (initial state exists, transitions are valid). |

---

## Examples

### No-Code (Inspector)
1. **Create → Neoxider → State Machine → State Machine Data** — create `EnemyAI_SM`.
2. Create several `StateData` assets: `Patrol`, `Chase`, `Attack`.
3. Drag all three into **States**.
4. Set **Initial State** to `Patrol`.
5. In **Transitions**, add `Patrol → Chase` with a Predicate condition.
6. Assign `EnemyAI_SM` to `StateMachineBehaviour` on a GameObject.

### Code
```csharp
var sm = new StateMachine<IState>();
stateMachineData.LoadIntoStateMachine(sm);

StateData initial = stateMachineData.InitialState;
sm.ChangeState(initial);
```

---

## See Also
- [StateData](StateData.md) — individual state
- [StateTransition](../StateTransition.md) — transition rule
- [StateMachineBehaviour](../StateMachineBehaviour.md) — MonoBehaviour wrapper
- ← [StateMachine](../README.md)
