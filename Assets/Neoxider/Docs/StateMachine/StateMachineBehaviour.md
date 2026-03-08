# StateMachineBehaviour<TState>

**Что это:** `StateMachineBehaviour<TState>` — generic `MonoBehaviour`-обёртка над `StateMachine<TState>` для кодовых state machine сценариев. Она управляет жизненным циклом состояний, может загружать `StateMachineData`, но не является тем же самым API, что `StateMachineBehaviourBase`. Файл: `Scripts/StateMachine/StateMachineBehaviour.cs`, пространство имён: `Neo.StateMachine`.

**Как использовать:**
1. Наследуйте свой компонент от `StateMachineBehaviour<IState>` или другого совместимого типа состояний.
2. Для code-first сценария задайте `initialStateTypeName` или вызывайте `ChangeState<T>()`.
3. Для no-code/state-data сценария задайте `stateMachineData`.
4. Если нужен inspector-driven runtime API с событиями и runtime-свойствами, используйте [StateMachineBehaviourBase](./StateMachineBehaviourBase.md).

---

## Описание

MonoBehaviour версия State Machine для использования на GameObject. Автоматически вызывает Update, FixedUpdate и LateUpdate для текущего состояния.

## Пространство имен

`Neo.StateMachine`

## Путь к файлу

`Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs`

## Добавление компонента

В меню Unity: `Component > Neoxider > Tools > StateMachineBehaviour`

## Ключевые поля

- `initialStateTypeName` - тип начального состояния (для кода)
- `enableDebugLog` - включить логирование переходов
- `showStateInInspector` - показывать текущее состояние в инспекторе
- `stateMachineData` - конфигурация через ScriptableObject (опционально)
- `autoEvaluateTransitions` - автоматически оценивать переходы каждый кадр

## Что реально есть в generic-версии

- `StateMachine` — доступ к экземпляру `StateMachine<TState>`.
- `CurrentState` — текущее состояние типа `TState`.
- `PreviousState` — предыдущее состояние типа `TState`.
- `ChangeState<T>()` — смена состояния по типу.
- `ChangeState(string stateName)` — смена состояния по имени из `StateMachineData`.
- `RegisterTransition(StateTransition transition)` — регистрация перехода.
- `LoadFromStateMachineData()` — загрузка конфигурации из `StateMachineData`.

## Чего здесь нет

В этой странице раньше были перечислены runtime-свойства и inspector events, которые на самом деле относятся к `StateMachineBehaviourBase`.

В `StateMachineBehaviour<TState>` нет:
- `CurrentStateName`
- `PreviousStateName`
- `CurrentStateElapsedTime`
- `StateChangeCount`
- `HasCurrentState`
- inspector-событий `On Initialized`, `On State Entered`, `On State Exited`, `On State Changed`, `On Transition Evaluated`
- runtime-кнопок `Reload Data`, `Evaluate Now`, `Go To Initial State`, `Change State`

Для этого используйте [StateMachineBehaviourBase](./StateMachineBehaviourBase.md).

## Основные методы

### ChangeState<T>()
Сменить состояние по типу.

```csharp
ChangeState<IdleState>();
```

### ChangeState(string stateName)
Сменить состояние по имени (при использовании StateMachineData).

```csharp
ChangeState("Idle");
```

### LoadFromStateMachineData()
Загрузить конфигурацию из StateMachineData.

```csharp
LoadFromStateMachineData();
```

## Настройка через StateMachineData

Конфигурация в State Machine Data, условия переходов через Neoxider Condition. Пошаговая инструкция: [NoCode_StateMachine_Usage.md](NoCode_StateMachine_Usage.md).

## Примеры

См. основной README.md

## См. также

- [StateMachineBehaviourBase](./StateMachineBehaviourBase.md)
- [StateMachine](./StateMachine.md)
- [README](./README.md)


