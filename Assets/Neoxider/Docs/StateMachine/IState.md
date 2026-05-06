# IState

**Назначение:** Контракт для состояний машины состояний. Определяет пять методов жизненного цикла: вход, обновление (три варианта), выход. Все конкретные состояния должны реализовать этот интерфейс.

---

## API

| Метод | Описание |
|-------|----------|
| `void OnEnter()` | Вызывается один раз при входе в состояние. Используйте для инициализации: включить анимации, компоненты, UI. |
| `void OnUpdate()` | Вызывается каждый кадр, пока состояние активно. Основная логика состояния. |
| `void OnFixedUpdate()` | Вызывается в `FixedUpdate` (физика). Можно оставить пустым, если не нужна физика. |
| `void OnLateUpdate()` | Вызывается в `LateUpdate`. Можно оставить пустым. |
| `void OnExit()` | Вызывается один раз при выходе из состояния. Очистка: отключить анимации, компоненты. |

---

## Примеры

### No-Code (Inspector)
Для No-Code используйте `StateMachineBehaviour` + `StateData` — состояния настраиваются через Inspector без написания кода.

### Код
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

## См. также
- [StateMachine](StateMachine.md) — машина состояний, использующая этот интерфейс
- [StateMachineBehaviour](StateMachineBehaviour.md) — MonoBehaviour-обёртка
- ← [StateMachine](README.md)
