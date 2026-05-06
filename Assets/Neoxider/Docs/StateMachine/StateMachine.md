# StateMachine\<TState\>

**Назначение:** Ядро машины состояний с кешированием состояний и переходов. Управляет жизненным циклом состояний (`OnEnter` → `OnUpdate` → `OnExit`), автоматической оценкой переходов по условиям, и предоставляет события для наблюдения. `TState` должен реализовывать `IState`.

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)` | Конструктор. Кеширование ускоряет повторное использование состояний и переходов. |
| `TState CurrentState { get; }` | Текущее активное состояние. |
| `TState PreviousState { get; }` | Предыдущее состояние (до последнего перехода). |
| `void ChangeState<T>()` | Сменить состояние по типу. Вызывает `OnExit` у старого, `OnEnter` у нового. |
| `void ChangeState(TState newState)` | Сменить состояние по экземпляру. |
| `bool TryChangeState<T>()` | Попытаться сменить состояние. Вернёт `true`, если переход разрешён условиями. |
| `bool CanTransitionTo<T>()` | Проверить, разрешён ли переход в указанный тип (без выполнения). |
| `T GetOrCreateState<T>()` | Получить состояние из кеша или создать новое. |
| `void RegisterTransition(StateTransition)` | Зарегистрировать правило перехода. |
| `void UnregisterTransition(StateTransition)` | Убрать правило перехода. |
| `IReadOnlyList<StateTransition> GetAvailableTransitions(Type)` | Все доступные переходы из указанного типа состояния. |
| `void EvaluateTransitions()` | Оценить все переходы и выполнить первый подходящий. |
| `void Update()` | Вызвать `OnUpdate()` текущего состояния. Вызывайте из `MonoBehaviour.Update()`. |
| `void FixedUpdate()` | Вызвать `OnFixedUpdate()` текущего состояния. |
| `void LateUpdate()` | Вызвать `OnLateUpdate()` текущего состояния. |
| `void ClearStateCache()` | Очистить кеш экземпляров состояний. |
| `void ClearTransitionCache()` | Очистить кеш переходов. |

---

## Unity Events

| Событие | Параметры | Описание |
|---------|-----------|----------|
| `OnStateChanged` | `(TState previous, TState current)` | Состояние изменилось (после exit/enter). |
| `OnStateEntered` | `(TState entered)` | Вход в новое состояние. |
| `OnStateExited` | `(TState exited)` | Выход из состояния. |
| `OnTransitionEvaluated` | `(StateTransition, bool passed)` | Результат проверки перехода. |

---

## Примеры

### No-Code (Inspector)
Для работы без кода используйте `StateMachineBehaviour` — MonoBehaviour-обёртку. Настройте состояния и переходы через Inspector, привяжите действия в `StateAction`.

### Код
```csharp
public class EnemyAI : MonoBehaviour
{
    private StateMachine<IState> sm;

    void Awake()
    {
        sm = new StateMachine<IState>();

        // Зарегистрировать переход Idle → Chase при обнаружении игрока
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

## См. также
- [IState](IState.md) — интерфейс состояния
- [StateCondition](StateCondition.md) — условия переходов
- [StateMachineBehaviour](StateMachineBehaviour.md) — MonoBehaviour-обёртка
- ← [StateMachine](README.md)
