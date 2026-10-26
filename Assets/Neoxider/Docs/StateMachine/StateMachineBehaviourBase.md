# StateMachineBehaviourBase

**Purpose:** Directly attachable scene adapter for `StateMachine<IState>`, with Inspector events and runtime status. It supports both C# states and optional NoCode `StateMachineData`.

## Setup

- Add **Neoxider → Tools → State Machine Behaviour**, assign optional `StateMachineData`, and put scene references in Context Overrides.
- Call `ChangeState<T>()` for C# states or `ChangeState(string)` for named StateData. `EvaluateTransitionsNow()`, `ReloadFromStateMachineData()`, and `GoToInitialState()` are also public C# / UnityEvent entry points.
- Exit actions receive Owner and Override slots when disabling, destroying, or reloading the component. Reload exits the old state before entering the initial state; re-enable can reload data according to the setting.
- Direct calls through `StateMachine` bypass the component's context scope. Use the component methods for context-dependent actions.
- Built-in duration conditions and composite children have independent timing when multiple components share `StateMachineData`. For manual active-machine timing use `StateMachine.SetCurrentStateEnterTime(...)`; standalone predicate setters no longer edit machine timing. `CurrentStateElapsedTime` remains the component's observed entry-time display and is unaffected by this manual anchor API.
- Nested `ChangeState` / `Stop` requests are queued FIFO after the current lifecycle event sequence. Owner disable during entry publishes that state's entered/changed events before stopping it. Each outer dispatch processes at most 128 requests; see [the core contract](StateMachine.md) for discarded requests and exceptions.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `ContextOverrides` | Context Overrides. |
| `CurrentState` | Current State. |
| `CurrentStateElapsedTime` | Current State Elapsed Time. |
| `CurrentStateName` | Current State Name. |
| `HasCurrentState` | Has Current State. |
| `PreviousState` | Previous State. |
| `PreviousStateName` | Previous State Name. |
| `StateChangeCount` | State Change Count. |
| `StateMachine` | State Machine. |
| `contextOverrides` | Context Overrides. |
| `currentStateName` | Current State Name. |
| `enableDebugLog` | Enable Debug Log. |
| `onInitialized` | On Initialized. |
| `onStateChanged` | On State Changed. |
| `onStateEntered` | On State Entered. |
| `onStateExited` | On State Exited. |
| `onTransitionEvaluated` | On Transition Evaluated. |
| `previousStateName` | Previous State Name. |
| `stateChangeCount` | State Change Count. |
| `stateEnterTime` | State Enter Time. |
| `stateMachineData` | State Machine Data. |

## See Also

- [Module Root](README.md)
