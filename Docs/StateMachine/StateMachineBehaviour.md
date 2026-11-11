# StateMachineBehaviour

**What it is:** the typed scene adapter `StateMachineBehaviour<TState>` (`MonoBehaviour`) for `StateMachine<TState>`. Derive a concrete class (for example `public sealed class EnemyBrain : StateMachineBehaviour<IState> { }`) to attach it to a GameObject. Path: `Scripts/StateMachine/StateMachineBehaviour.cs`, namespace `Neo.StateMachine`. Use `StateMachineBehaviourBase` when a directly attachable untyped component is sufficient.

**How to use:**
1. Create a concrete subclass and add it to a GameObject.
2. Either assign `StateMachineData` (NoCode) or set **Initial State Type Name** to a C# state's full name (`MyGame.States.IdleState`) or assembly-qualified name.
3. Assign scene objects in **Context Overrides**.
4. From C#: `ChangeState<T>()`, `ChangeState(string)`, `ChangeState(StateData)`, `RegisterTransition(...)`, `EvaluateTransitionsNow()`, `LoadFromStateMachineData()`.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| `initialStateTypeName` | C# state entered in `Start()` when no `StateMachineData` is set. Full name (`Namespace.State`) or assembly-qualified name; resolved in `Awake` across loaded assemblies. A name that cannot be resolved skips the initial state and logs a warning when this component's **Enable Debug Log** is on. |
| `enableDebugLog` | This component's debug log: its own logs, its machine's warnings (`DebugLogEnabled`) and the NoCode actions its machine runs. Another component's setting does not change it. |
| `exitCurrentStateOnDisable` | Exit the current state when disabled (default `true`). |
| `reloadDataOnEnable` | Reload `StateMachineData` when re-enabled (default `true`). |
| `showStateInInspector` | Show the current state in the inspector. |
| `autoEvaluateTransitions` | Evaluate transitions every `Update` (default `true`). |
| `stateMachineData` | Optional NoCode configuration. |
| `contextOverrides` | Scene objects for Override1..5. |

## Methods and properties

| Member | Description |
|--------|-------------|
| `StateMachine` | Underlying `StateMachine<TState>`; created on first access and kept by `Awake`, so configuration made before activation survives. |
| `CurrentState`, `PreviousState` | Typed state queries. |
| `ChangeState<T>()` / `ChangeState(string)` / `ChangeState(StateData)` | Change state with this component's scene context. StateData must be compatible with `TState`. |
| `RegisterTransition(transition)`, `EvaluateTransitionsNow()` | Register code transitions; evaluate once immediately. |
| `LoadFromStateMachineData()` | Stop, re-register the data's transitions and enter its `InitialState` asset. |
| `AutoEvaluateTransitions`, `StateMachineData`, `ContextOverrides`, `InitialStateTypeName` | Runtime configuration. |
| `EnableDebugLog` | C# path for **Enable Debug Log**; setting it also updates the running machine's `DebugLogEnabled`. |

## Notes

- `ChangeState*`, `EvaluateTransitionsNow()` and the automatic ticks supply the scene context to actions and predicates; accessing `StateMachine` directly does not.
- `LoadFromStateMachineData()` exits the old state with its owner/overrides, clears transition registrations, and enters the configured initial asset. Disable and destruction also preserve context for exit actions. Re-enable reloads data when the matching setting is enabled.
- The wrapper adds no per-frame allocations to the ticks and transition evaluation (no context delegates). Predicates and actions you attach may still allocate: for example a `ConditionEntryPredicate` reading a value-type member through reflection boxes the value on every evaluation.
- `ChangeState(string)` and `ChangeState(StateData)` are overloads, so a literal `ChangeState(null)` no longer compiles (ambiguous call); cast it or call the overload you mean.
- Built-in duration predicates/composites use machine-owned entry time by default, including shared `StateMachineData`. Custom mutable predicate/action fields remain shared. For a manual timer edit use `StateMachine.SetCurrentStateEnterTime(...)`; predicate `SetEnterTime` edits standalone timing.
- `ChangeState` / `Stop` requested during lifecycle callbacks run FIFO after the current event sequence. Owner disable during entry emits the entered state, then the queued stop. The core limits each outer dispatch to 128 requests; see [lifecycle and timing](StateMachine.md).

## See also

- [Module root](README.md)
- [StateMachineBehaviourBase](StateMachineBehaviourBase.md)
