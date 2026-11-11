# IState

**What it is:** the contract for state machine states: five lifecycle methods (enter, three update variants, exit). Path: `Scripts/StateMachine/IState.cs`, namespace `Neo.StateMachine`. `StateData` implements it for NoCode states.

**How to use:**
1. Implement `IState` in a plain C# class (leave unused hooks empty).
2. Enter it with `stateMachine.ChangeState(new PatrolState(...))`, `ChangeState<T>()` (parameterless constructor, cached instance) or as a transition target type.
3. For a scene object, host the machine in `StateMachineBehaviourBase` / `StateMachineBehaviour<TState>`, which drive `OnUpdate` / `OnFixedUpdate` / `OnLateUpdate`.

---

## API

| Method | Description |
|--------|-------------|
| `void OnEnter()` | Called once when entering the state. Use for setup: enable animations, components, UI. |
| `void OnUpdate()` | Called every frame while the state is active. Main state logic. |
| `void OnFixedUpdate()` | Called in `FixedUpdate` (physics). Can be left empty if no physics needed. |
| `void OnLateUpdate()` | Called in `LateUpdate`. Can be left empty. |
| `void OnExit()` | Called once when leaving the state. Cleanup: disable animations, components. |

---

## Examples

### No-Code (Inspector)
For No-Code, use `StateMachineBehaviour` + `StateData` — states are configured in Inspector without writing code.

### Code
```csharp
public class PatrolState : IState
{
    private readonly EnemyAI enemy;

    public PatrolState(EnemyAI enemy) => this.enemy = enemy;

    public void OnEnter()
    {
        enemy.Agent.isStopped = false;
        enemy.SetNextWaypoint();
    }

    public void OnUpdate()
    {
        if (enemy.Agent.remainingDistance < 0.5f)
            enemy.SetNextWaypoint();
    }

    public void OnExit()
    {
        enemy.Agent.isStopped = true;
    }

    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
}
```

---

## See Also
- [StateMachine](StateMachine.md) — state machine that uses this interface
- [StateMachineBehaviour](StateMachineBehaviour.md) — MonoBehaviour wrapper
- ← [StateMachine](README.md)
