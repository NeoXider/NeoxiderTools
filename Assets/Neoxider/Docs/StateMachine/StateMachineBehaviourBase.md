# StateMachineBehaviourBase

**What it is:** a directly attachable scene adapter (`MonoBehaviour`) for `StateMachine<IState>`, with Inspector events and runtime status. It runs both C# states and optional NoCode `StateMachineData`, and supplies the scene context (Owner + Context Overrides) to predicates and actions. Path: `Scripts/StateMachine/StateMachineBehaviourBase.cs`, namespace `Neo.StateMachine`.

**How to use:**
1. Add **Neoxider → Tools → State Machine Behaviour** to a GameObject.
2. Optionally assign a `StateMachineData` asset; it is loaded in `Start()` and enters its initial state.
3. Put scene references for condition/action slots into **Context Overrides** (element 0 = Override1).
4. Drive it from UnityEvents or C#: `ChangeState(string)`, `ChangeState(StateData)`, `ChangeState<T>()`, `EvaluateTransitionsNow()`, `ReloadFromStateMachineData()`, `GoToInitialState()`.

---

## Fields (Inspector)

| Field | Default | Description |
|-------|---------|-------------|
| `enableDebugLog` | `false` | This component's debug log: its own logs, its machine's warnings (`DebugLogEnabled`) and the NoCode actions its machine runs (see Notes). |
| `exitCurrentStateOnDisable` | `true` | Exit the current state when the component is disabled. |
| `reloadDataOnEnable` | `true` | Reload `StateMachineData` when the component is enabled again after a disable. |
| `showStateInInspector` | `true` | Show the runtime state in the custom inspector. |
| `autoEvaluateTransitions` | `true` | Evaluate transitions every `Update`. |
| `stateMachineData` | none | Optional NoCode configuration asset. |
| `contextOverrides` | empty | Scene objects for the Override1..5 slots. |
| `onInitialized`, `onStateEntered`, `onStateExited` | — | `UnityEvent`s. |
| `onStateChanged` | — | `UnityEvent<string, string>` (previous name, current name). |
| `onTransitionEvaluated` | — | `UnityEvent<string, bool>` (transition name, result), every evaluated transition (only transitions leaving the current state). |
| Runtime State | — | `currentStateName`, `previousStateName`, `stateChangeCount`, `stateEnterTime` (display only). |

## Methods and properties

| Member | Description |
|--------|-------------|
| `StateMachine` | The underlying `StateMachine<IState>`. Created on first access, so it can be configured before `Awake` (for example on an inactive object); `Awake` keeps it. |
| `ChangeState<T>()` | Change to a C# state type (cached instance). |
| `ChangeState(string stateName)` | Change to the `StateData` with that name in the assigned `StateMachineData`. |
| `ChangeState(StateData state)` | Change to a specific asset (also when it is not listed in the data). |
| `RegisterTransition(transition)` | Register a code transition. |
| `EvaluateTransitionsNow()` | Evaluate transitions once with this component's context. |
| `LoadFromStateMachineData()` / `ReloadFromStateMachineData()` | Stop, re-register the data's transitions and enter its `InitialState` asset (or the legacy initial name). |
| `GoToInitialState()` | Enter the data's initial state without reloading transitions. |
| `CurrentState`, `PreviousState`, `HasCurrentState` | State queries. |
| `CurrentStateName`, `PreviousStateName`, `StateChangeCount`, `CurrentStateElapsedTime` | Runtime status (names are StateData names or C# type names). |
| `AutoEvaluateTransitions`, `StateMachineData`, `ContextOverrides` | Runtime configuration. |
| `EnableDebugLog` | C# path for **Enable Debug Log**; setting it also updates the running machine's `DebugLogEnabled`. |
| `OnInitialized`, `OnStateEntered`, `OnStateExited`, `OnStateChanged`, `OnTransitionEvaluated` | The Inspector events, for C# subscribers. |

```csharp
using Neo.StateMachine;
using Neo.StateMachine.NoCode;
using UnityEngine;

public sealed class DoorController : MonoBehaviour
{
    [SerializeField] private StateMachineBehaviourBase _machine;
    [SerializeField] private StateData _open;
    [SerializeField] private Animator _animator;

    private void OnEnable()
    {
        _machine.OnStateChanged.AddListener(OnStateChanged);
    }

    private void OnDisable()
    {
        _machine.OnStateChanged.RemoveListener(OnStateChanged);
    }

    public void Open()
    {
        _machine.ChangeState(_open);
    }

    private void OnStateChanged(string previous, string current)
    {
        _animator.SetBool("IsOpen", current == _open.StateName);
    }
}
```

## Events

`onStateEntered` / `onStateExited` fire for every enter/exit (Stop included); `onStateChanged` follows with the names. `onTransitionEvaluated` fires for every evaluated transition; unnamed transitions report a cached `"From -> To"` label. Since 12.0 a NoCode machine evaluates only the transitions that leave the active `StateData` (plus source-less ones), so the event no longer reports every transition of the data with `false` each frame.

## Notes

- Exit actions receive Owner and Override slots when disabling, destroying, or reloading the component. Reload exits the old state before entering the initial state; re-enable can reload data according to the setting.
- Loading enters the assigned `InitialState` asset itself; two states sharing a name (for example the default "New State") no longer make the machine start in the wrong one.
- Direct calls through `StateMachine` bypass the component's context scope. Use the component methods for context-dependent actions.
- The wrapper adds no per-frame allocations to `Update` / `FixedUpdate` / `LateUpdate` and transition evaluation (no context delegates, cached transition labels). Predicates and actions you attach may still allocate: for example a `ConditionEntryPredicate` reading a value-type field or property through reflection boxes the value on every evaluation, and `StateData` On Update actions run whatever code they contain.
- `ChangeState(string)` and `ChangeState(StateData)` are overloads, so a literal `ChangeState(null)` no longer compiles (ambiguous call); cast it (`ChangeState((string)null)`) or call the overload you mean.
- Built-in duration conditions and composite children have independent timing when multiple components share `StateMachineData`. For manual active-machine timing use `StateMachine.SetCurrentStateEnterTime(...)`; standalone predicate setters do not edit machine timing. `CurrentStateElapsedTime` remains the component's observed entry-time display and is unaffected by this manual anchor API.
- Nested `ChangeState` / `Stop` requests are queued FIFO after the current lifecycle event sequence. Owner disable during entry publishes that state's entered/changed events before stopping it. Each outer dispatch processes at most 128 requests; see [the core contract](StateMachine.md) for discarded requests and exceptions.
- `enableDebugLog` is per component since 12.0: `OnEnable` assigns it to this component's machine (`StateMachine.DebugLogEnabled`), which also gates the `LogStateAction` output and action errors of the states it runs. A component enabled later with the opposite setting no longer changes it. The shared switch the components still write in `OnEnable` only serves logs that run outside any machine (automatic `StateMachineData` validation in Play Mode) and plain machines whose `DebugLogEnabled` is unassigned.

## See also

- [Module root](README.md)
- [StateMachineBehaviour&lt;TState&gt;](StateMachineBehaviour.md)
- [NoCode setup](NoCode_StateMachine_Usage.md)
