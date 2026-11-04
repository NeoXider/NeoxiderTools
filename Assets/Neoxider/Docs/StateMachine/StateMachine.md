# StateMachine\<TState\>

**What it is:** Core state machine with state and transition caching (`Scripts/StateMachine/StateMachine.cs`, namespace `Neo.StateMachine`). Manages the state lifecycle (`OnEnter` → `OnUpdate` → `OnExit`), automatic transition evaluation, and provides observation events. `TState` must implement `IState`.

**How to use:**
1. Create `new StateMachine<IState>()` in your owner (or use the scene wrapper
   [StateMachineBehaviourBase](StateMachineBehaviourBase.md)).
2. Register `StateTransition` rules and enter the first state with `ChangeState<TState>()`.
3. Call `Update()` and `EvaluateTransitions()` every frame; see the [code example](#code).

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)` | Constructor. Caching speeds up repeated state/transition usage. |
| `StateMachine(bool enableStateCaching, bool enableTransitionCaching, Func<float> timeProvider)` | Optional clock in seconds for duration timing; null uses `Time.time`. |
| `TState CurrentState { get; }` | Currently active state. |
| `TState PreviousState { get; }` | Previous state (before the last transition). |
| `bool DebugLogEnabled { get; set; }` | Per-machine diagnostics: gates this machine's warnings and errors and the logs of the NoCode actions it runs (see [Diagnostics](#diagnostics)). Until assigned it follows the shared switch written by the scene wrappers. |
| `void ChangeState<T>()` | Switch state by type. Calls `OnExit` on old, `OnEnter` on new. |
| `void ChangeState(TState newState)` | Switch state by instance. |
| `bool TryChangeState<T>()` | Evaluate conditions and dispatch/queue the change. False if a callback invalidated that evaluation. |
| `bool CanTransitionTo<T>()` | Evaluate conditions without applying the target. Custom predicates may still have side effects. |
| `bool SetCurrentStateEnterTime(float enterTime)` | Edit only this machine's active duration anchor without callbacks; false when stopped. |
| `T GetOrCreateState<T>()` | Get state from cache or create a new one. An instance whose constructor changed or stopped the machine is returned but not cached. |
| `void RegisterTransition(StateTransition)` | Register a transition rule. Its source selectors are read when the sorted lists are rebuilt, so they may change after registration. |
| `void UnregisterTransition(StateTransition)` | Remove a transition rule. |
| `IReadOnlyList<StateTransition> GetAvailableTransitions(Type)` | Transitions registered for the state type plus source-less ones, sorted by priority. For `typeof(StateData)` this lists every NoCode transition of the machine. |
| `IReadOnlyList<StateTransition> GetAvailableTransitionsFrom(TState state)` | What evaluation actually walks for `state`: a `StateData` asset gets only the transitions leaving that asset (plus source-less ones); a C# state gets its type's list. Empty for null. |
| `void EvaluateTransitions()` | Evaluate the transitions leaving the current state and execute the first matching one. |
| `void Update()` | Call `OnUpdate()` on the current state. Call from `MonoBehaviour.Update()`. |
| `void FixedUpdate()` | Call `OnFixedUpdate()` on the current state. |
| `void LateUpdate()` | Call `OnLateUpdate()` on the current state. |
| `void Stop()` | Exit the current state without entering another (sets `CurrentState` to null). |
| `void ClearStateCache()` | Clear the state instance cache. |
| `void ClearTransitionCache()` | Clear the transition cache. |

## Transition evaluation contracts

- Higher `Priority` wins, including changes made after the transition cache was populated. Equal priorities keep registration order (transitions with a source first, then source-less ones).
- Sorted lists are cached per C# state type and per `StateData` asset. They are checked against `StateTransition.RoutingVersion`, a counter that changes when any transition's `Priority`, `FromStateType` or `FromStateData` changes through its setter or through Unity deserialization (Inspector edit, Undo, `JsonUtility`). A warm evaluation therefore does not re-check priority order, and a list is replaced only when its order or membership really changed; a routing edit in one machine does not make other machines allocate. After writing those serialized fields another way (reflection, a custom serializer), call `StateTransition.MarkRoutingChanged()`.
- NoCode evaluation is O(transitions leaving the active asset): `EvaluateTransitions`, `CanTransitionTo` and `OnTransitionEvaluated` see only transitions whose current `FromStateData` is the active asset or that have no source. Before 12.0 every NoCode transition of the machine was evaluated and reported (with `false`) on every frame. Entry hooks (`StatePredicate.OnOwnerStateEntered`) likewise run only for transitions leaving the entered state.
- Editing `FromStateType` or `FromStateData` after registration takes effect on the next lookup: a rebuild reads the current selectors, so a transition moved to another C# type or `StateData` subclass routes from its new source, and a transition whose `FromStateType` is cleared becomes source-less. Before 12.0 a C# source was indexed at registration and needed unregister/register. Unregistering removes a registration whatever its current selectors are.
- NoCode source indexing uses the concrete `StateData` type, including subclasses. `CanTransition` still checks the exact source asset.
- Returned lists are cached and must not be modified. They are replaced, never re-sorted in place, so a pass that is iterating one (for example while a listener edits a priority) finishes on its snapshot; the next lookup sees the new order.
- CLR targets must be concrete, closed types implementing `TState` with a public parameterless constructor. Use `ToStateData` for an existing ScriptableObject. An unusable target does not block later valid transitions.
- Each machine checks a CLR target type once and remembers the result, including a rejected target. With state caching enabled, repeated automatic transitions between cached CLR states perform no reflection, and the machine itself makes no managed allocation; your predicates, states and event listeners may still allocate. With caching disabled every activation still constructs a new instance.
- A predicate or `OnTransitionEvaluated` listener that stops the machine or selects another state cancels application of the old evaluation result. A monotonic generation also catches departure and re-entry of the same cached instance. `CanTransitionTo` and `TryChangeState` reject callback-invalidated results, including lifecycle requests still waiting in the queue.
- Successful state construction is also a reentry boundary. If a target constructor changes the active state, including stopping a running state, that newer explicit choice wins: the older automatic, conditional or generic activation (`EvaluateTransitions`, `TryChangeState<T>`, `ChangeState<T>`) does not enter its target, cache the superseded instance or fall through to a lower-priority automatic rule. `TryChangeState<T>()` returns false for that superseded request, and `GetOrCreateState<T>()` returns such an instance without caching it. Ordinary construction, cache reuse and failed-constructor fallback without a newer state choice retain their existing behavior. A failed constructor is reported through the gated debug log with the constructor's own exception message.
- Call `GetAvailableTransitions` with a non-null type. `ClearTransitionCache` removes registered rules as well as cached views.
- A warm `EvaluateTransitions` on cached lists makes no managed allocation in the machine itself (code and NoCode states); your predicates, states and listeners may allocate.

## Lifecycle dispatch

Outside a lifecycle callback, `ChangeState` and `Stop` finish synchronously, including queued work. During exit, enter or their events, requests are appended FIFO and run after the current notification sequence. For A → B the order is `A.OnExit`, `OnStateExited(A)`, `B.OnEnter`, `OnStateEntered(B)`, `OnStateChanged(A, B)`. Entry hooks for predicates run before `B.OnEnter`. If B requests Stop on entry (including owner disable), B's entered/changed notifications finish first, then B exits and `OnStateChanged(B, null)` fires once. Event arguments use captured instances.

`ChangeState` keeps the old `CurrentState` during its exit hook. `Stop` preserves its prior contract: `CurrentState` is already null during exit. Initial entry still raises `OnStateExited(null)`. Changing to the current instance outside dispatch and stopping an idle machine are no-ops. An instance request queued during exit is checked when processed, so requesting A while leaving A for B can deliberately re-enter A afterward.

The outer dispatch processes at most **128 requests**, including queued no-ops. At the limit, pending requests are discarded and the last completed state remains; the diagnostic uses the machine's `DebugLogEnabled`. Exceptions release the dispatch guard and discard pending requests in `finally`; completed work is not rolled back and later lifecycle stages are not guaranteed after a fault. This bound does not make a custom hook that recurses independently safe.

Calling the core through a wrapper's `StateMachine` property bypasses that wrapper's scene context; use the wrapper's change/reload methods for context-dependent actions.

## Diagnostics

Every log of the module is debug-only. `DebugLogEnabled` belongs to one machine:

- the machine's own warnings and errors (null state or transition, the 128-request limit, a failing CLR target constructor) use it;
- while the machine runs a state's `OnEnter` / `OnExit` / `OnUpdate` / `OnFixedUpdate` / `OnLateUpdate` or a lifecycle listener, the NoCode code that has no machine reference (`LogStateAction`, `StateData` action errors, `ChangeSceneAction` warnings) logs by this machine's setting, so one `StateData` asset shared by a loud and a quiet machine logs only in the loud one;
- outside any machine callback that code follows the shared switch that `StateMachineBehaviour` components write in `OnEnable`.

Both wrappers assign their **Enable Debug Log** to their machine in `OnEnable` (and when it changes in the Inspector or through `EnableDebugLog`), so a component enabled later with the opposite setting no longer changes another machine's logs. A plain machine keeps the legacy behaviour (it follows the shared switch) until you assign `DebugLogEnabled`:

```csharp
StateMachine<IState> machine = new StateMachine<IState> { DebugLogEnabled = true };
```

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
| `OnTransitionEvaluated` | `(StateTransition, bool passed)` | Result of each evaluated transition (only transitions leaving the current state). |

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
