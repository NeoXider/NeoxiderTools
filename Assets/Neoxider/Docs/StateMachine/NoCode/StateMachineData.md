# StateMachineData

**Назначение:** ScriptableObject-конфигурация полной машины состояний для No-Code. Содержит массив `StateData` (состояний) и список `StateTransition` (переходов). Загружается в `StateMachineBehaviour` при старте. Настраивается целиком в Inspector.

**Создать:** Create → Neoxider → State Machine → State Machine Data.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **States** | Массив `StateData` — все состояния машины. Каждое — отдельный ScriptableObject. |
| **Initial State** | Ссылка на начальное `StateData`. Машина войдёт в это состояние при старте. |
| **Initial State Name** | Имя начального состояния (legacy, используется если `Initial State` не задан). |
| **Transitions** | Список `StateTransition` — правила переходов между состояниями с условиями. |

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `StateData[] States { get; set; }` | Все состояния. |
| `StateData InitialState { get; set; }` | Начальное состояние (ссылка). |
| `string InitialStateName { get; set; }` | Имя начального состояния. |
| `List<StateTransition> Transitions { get; }` | Список переходов. |
| `void LoadIntoStateMachine<TState>(StateMachine<TState>)` | Загрузить конфигурацию в машину состояний (зарегистрировать все переходы). |
| `StateData GetStateByName(string name)` | Найти состояние по имени. |
| `bool Validate(bool silent = false)` | Проверить корректность конфигурации (есть ли initial state, все ли переходы валидны). |

---

## Примеры

### No-Code (Inspector)
1. **Create → Neoxider → State Machine → State Machine Data** — создать ассет `EnemyAI_SM`.
2. Создать несколько `StateData`: `Patrol`, `Chase`, `Attack`.
3. В **States** перетащить все три ассета.
4. В **Initial State** указать `Patrol`.
5. В **Transitions** добавить переход `Patrol → Chase` с условием (Predicate).
6. Назначить `EnemyAI_SM` в `StateMachineBehaviour` на GameObject.

### Код
```csharp
// Загрузить конфигурацию в runtime
var sm = new StateMachine<IState>();
stateMachineData.LoadIntoStateMachine(sm);

StateData initial = stateMachineData.InitialState;
sm.ChangeState(initial);
```

---

## См. также
- [StateData](StateData.md) — отдельное состояние
- [StateTransition](../StateTransition.md) — правило перехода
- [StateMachineBehaviour](../StateMachineBehaviour.md) — MonoBehaviour-обёртка
- ← [StateMachine](../README.md)
