# StateMachineBehaviour

**Purpose:** Typed scene adapter for `StateMachine<TState>`. Define a concrete MonoBehaviour subclass of `StateMachineBehaviour<IState>` (or your own state interface) to attach it to a GameObject. Use `StateMachineBehaviourBase` when a directly attachable component is sufficient.

## Setup

- Add your concrete subclass, optionally assign `StateMachineData`, and assign scene objects in Context Overrides. `ChangeState<T>()`, `ChangeState(string)`, and automatic ticks supply this scene context to actions and predicates.
- `LoadFromStateMachineData()` exits the old state with its owner/overrides, clears transition registrations, and enters the configured initial state. Disable and destruction also preserve context for exit actions. Re-enable reloads data when the matching setting is enabled.
- Accessing the underlying `StateMachine` directly does not establish scene context.
- Built-in duration predicates/composites use machine-owned entry time by default, including shared `StateMachineData`. Custom mutable predicate/action fields remain shared. For a manual timer edit use `StateMachine.SetCurrentStateEnterTime(...)`; predicate `SetEnterTime` edits standalone timing.
- `ChangeState` / `Stop` requested during lifecycle callbacks run FIFO after the current event sequence. Owner disable during entry emits the entered state, then the queued stop. The core limits each outer dispatch to 128 requests; see [lifecycle and timing](StateMachine.md).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CurrentState` | Current State. |
| `PreviousState` | Previous State. |
| `StateMachine` | State Machine. |
| `contextOverrides` | Context Overrides. |
| `enableDebugLog` | Enable Debug Log. |
| `initialStateTypeName` | Initial State Type Name. |
| `stateMachineData` | State Machine Data. |

## See Also

- [Module Root](README.md)
