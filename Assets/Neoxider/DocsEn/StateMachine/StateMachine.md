# StateMachine\<TState\>

**Purpose:** Core state machine with state and transition caching. Manages the state lifecycle (`OnEnter` → `OnUpdate` → `OnExit`), automatic transition evaluation, and provides observation events. `TState` must implement `IState`.

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)` | Constructor. Caching speeds up repeated state/transition usage. |
| `TState CurrentState { get; }` | Currently active state. |
| `TState PreviousState { get; }` | Previous state (before the last transition). |
| `void ChangeState<T>()` | Switch state by type. Calls `OnExit` on old, `OnEnter` on new. |
| `void ChangeState(TState newState)` | Switch state by instance. |
| `bool TryChangeState<T>()` | Try to switch state. Returns `true` if transition conditions allow it. |
| `bool CanTransitionTo<T>()` | Check if transition to the given type is allowed (without executing). |
| `T GetOrCreateState<T>()` | Get state from cache or create a new one. |
| `void RegisterTransition(StateTransition)` | Register a transition rule. |
| `void UnregisterTransition(StateTransition)` | Remove a transition rule. |
| `IReadOnlyList<StateTransition> GetAvailableTransitions(Type)` | All available transitions from the given state type. |
| `void EvaluateTransitions()` | Evaluate all transitions and execute the first matching one. |
| `void Update()` | Call `OnUpdate()` on the current state. Call from `MonoBehaviour.Update()`. |
| `void FixedUpdate()` | Call `OnFixedUpdate()` on the current state. |
| `void LateUpdate()` | Call `OnLateUpdate()` on the current state. |
| `void ClearStateCache()` | Clear the state instance cache. |
| `void ClearTransitionCache()` | Clear the transition cache. |

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
