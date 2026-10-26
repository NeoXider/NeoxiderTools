# StateMachine\<TState\>

**Purpose:** Core state machine with state and transition caching. Manages the state lifecycle (`OnEnter` → `OnUpdate` → `OnExit`), automatic transition evaluation, and provides observation events. `TState` must implement `IState`.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)` | Constructor. Caching speeds up repeated state/transition usage. |
| `StateMachine(bool enableStateCaching, bool enableTransitionCaching, Func<float> timeProvider)` | Optional clock in seconds for duration timing; null uses `Time.time`. |
| `TState CurrentState { get; }` | Currently active state. |
| `TState PreviousState { get; }` | Previous state (before the last transition). |
| `void ChangeState<T>()` | Switch state by type. Calls `OnExit` on old, `OnEnter` on new. |
| `void ChangeState(TState newState)` | Switch state by instance. |
| `bool TryChangeState<T>()` | Evaluate conditions and dispatch/queue the change. False if a callback invalidated that evaluation. |
| `bool CanTransitionTo<T>()` | Evaluate conditions without applying the target. Custom predicates may still have side effects. |
| `bool SetCurrentStateEnterTime(float enterTime)` | Edit only this machine's active duration anchor without callbacks; false when stopped. |
| `T GetOrCreateState<T>()` | Get state from cache or create a new one. |
| `void RegisterTransition(StateTransition)` | Register a transition rule. |
| `void UnregisterTransition(StateTransition)` | Remove a transition rule. |
| `IReadOnlyList<StateTransition> GetAvailableTransitions(Type)` | All available transitions from the given state type. |
| `void EvaluateTransitions()` | Evaluate all transitions and execute the first matching one. |
| `void Update()` | Call `OnUpdate()` on the current state. Call from `MonoBehaviour.Update()`. |
| `void FixedUpdate()` | Call `OnFixedUpdate()` on the current state. |
| `void LateUpdate()` | Call `OnLateUpdate()` on the current state. |
| `void Stop()` | Exit the current state without entering another (sets `CurrentState` to null). |
| `void ClearStateCache()` | Clear the state instance cache. |
| `void ClearTransitionCache()` | Clear the transition cache. |

## Transition evaluation contracts

- Higher `Priority` wins, including changes made after the transition cache was populated. Equal priorities have no specified ordering.
- After editing `FromStateType` or `FromStateData`, unregister and register the transition again. Unregistering also removes a registration whose source selector has already changed.
- NoCode source indexing uses the concrete `StateData` type, including subclasses. `CanTransition` still checks the exact source asset.
- CLR targets must be concrete, closed types implementing `TState` with a public parameterless constructor. Use `ToStateData` for an existing ScriptableObject. An unusable target does not block later valid transitions.
- A predicate or `OnTransitionEvaluated` listener that stops the machine or selects another state cancels application of the old evaluation result. A monotonic generation also catches departure and re-entry of the same cached instance. `CanTransitionTo` and `TryChangeState` reject callback-invalidated results, including lifecycle requests still waiting in the queue.
- Call `GetAvailableTransitions` with a non-null type. `ClearTransitionCache` removes registered rules as well as cached views.

## Lifecycle dispatch

Outside a lifecycle callback, `ChangeState` and `Stop` finish synchronously, including queued work. During exit, enter or their events, requests are appended FIFO and run after the current notification sequence. For A → B the order is `A.OnExit`, `OnStateExited(A)`, `B.OnEnter`, `OnStateEntered(B)`, `OnStateChanged(A, B)`. Entry hooks for predicates run before `B.OnEnter`. If B requests Stop on entry (including owner disable), B's entered/changed notifications finish first, then B exits and `OnStateChanged(B, null)` fires once. Event arguments use captured instances.

`ChangeState` keeps the old `CurrentState` during its exit hook. `Stop` preserves its prior contract: `CurrentState` is already null during exit. Initial entry still raises `OnStateExited(null)`. Changing to the current instance outside dispatch and stopping an idle machine are no-ops. An instance request queued during exit is checked when processed, so requesting A while leaving A for B can deliberately re-enter A afterward.

The outer dispatch processes at most **128 requests**, including queued no-ops. At the limit, pending requests are discarded and the last completed state remains; the diagnostic uses the existing debug log gate. Exceptions release the dispatch guard and discard pending requests in `finally`; completed work is not rolled back and later lifecycle stages are not guaranteed after a fault. This bound does not make a custom hook that recurses independently safe.

Calling the core through a wrapper's `StateMachine` property bypasses that wrapper's scene context; use the wrapper's change/reload methods for context-dependent actions.

## Duration timing and 11.0 migration

Each machine captures entry time and supplies it with a sampled evaluation time to built-in `StateDurationPredicate`, including composite children. Shared `StateMachineData` therefore has independent duration timing in the default core and both scene wrappers. Re-entry restarts only that machine's clock. Registration after entry uses the active state's existing anchor. Custom virtual `Evaluate(IState)` and entry hooks still run; custom mutable predicate/action state is not isolated or cloned.

The existing constructor uses scaled Unity `Time.time`. To supply a deterministic or unscaled clock, use the additive overload. Entry and manual anchors must use that clock's seconds:

```csharp
float now = 0f;
StateMachine<IState> machine = new StateMachine<IState>(true, true, () => now);
// Register transitions, then enter a state as usual.
machine.ChangeState<IdleState>();
now = 5f;
machine.EvaluateTransitions();
bool updated = machine.SetCurrentStateEnterTime(now - 2f);
```

`StateDurationPredicate.SetEnterTime` and `OnOwnerStateEntered` still anchor standalone evaluation outside a machine. **Migration:** code that used `predicate.SetEnterTime(...)` after machine entry to edit a machine timer must call `machine.SetCurrentStateEnterTime(...)`. It returns false while stopped and otherwise changes only the active machine anchor, without re-entry, custom entry hooks or wrapper display-time changes. Standalone setters no longer retime an active machine. Nested evaluations restore the previous timing context even when a callback throws. Direct transition/predicate evaluation outside the machine uses standalone timing.

---

## Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `OnStateChanged` | `(TState previous, TState current)` | State changed (after exit/enter). |
| `OnStateEntered` | `(TState entered)` | Entered a new state. |
| `OnStateExited` | `(TState exited)` | Exited a state. |
| `OnTransitionEvaluated` | `(StateTransition, bool passed)` | Result of a transition evaluation. |

---

## Examples

### No-Code (Inspector)
For No-Code usage, use `StateMachineBehaviour` — a MonoBehaviour wrapper. Configure states and transitions in Inspector, wire actions via `StateAction`.

### Code
```csharp
public class EnemyAI : MonoBehaviour
{
    private StateMachine<IState> sm;

    void Awake()
    {
        sm = new StateMachine<IState>();

        // Register transition Idle → Chase when player detected
        sm.RegisterTransition(new StateTransition
        {
            FromStateType = typeof(IdleState),
            ToStateType = typeof(ChaseState)
        });

        sm.ChangeState<IdleState>();
    }

    void Update()
    {
        sm.Update();
        sm.EvaluateTransitions();
    }
}
```

---

## See Also
- [IState](IState.md) — state interface
- [StateCondition](StateCondition.md) — transition conditions
- [StateMachineBehaviour](StateMachineBehaviour.md) — MonoBehaviour wrapper
- ← [StateMachine](README.md)
