# IState

**Purpose:** Contract for state machine states. Defines five lifecycle methods: enter, update (three variants), exit. All concrete states must implement this interface.

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
