# StateMachineBehaviourBase

**Что это:** компонент автомата состояний. Работает с `StateMachineData` (ScriptableObject), события в инспекторе, runtime-управление и свойства текущего состояния. Пространство имён `Neo.StateMachine`, файл `Scripts/StateMachine/StateMachineBehaviourBase.cs`.

**Как использовать:** добавить на GameObject, назначить State Machine Data; в Context for conditions указать объекты сцены для условий переходов. Методы: ChangeState, LoadFromStateMachineData, EvaluateTransitionsNow, GoToInitialState. См. [NoCode_StateMachine_Usage](NoCode_StateMachine_Usage.md).

---

## Пространство имен

`Neo.StateMachine`

## Путь к файлу

`Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs`

## Основные возможности

- Автоматическая оценка переходов (`Auto Evaluate Transitions`)
- Загрузка конфигурации из `StateMachineData`
- События в инспекторе (`On State Changed`, `On Transition Evaluated` и др.)
- Runtime кнопки управления (`Reload Data`, `Evaluate Now`, `Go To Initial State`, `Change State`)
- Runtime свойства для `NeoCondition`:
  - `CurrentStateName`
  - `PreviousStateName`
  - `CurrentStateElapsedTime`
  - `StateChangeCount`
  - `HasCurrentState`

## Публичные методы

- `ChangeState<T>()`
- `ChangeState(string stateName)`
- `LoadFromStateMachineData()`
- `ReloadFromStateMachineData()`
- `EvaluateTransitionsNow()`
- `GoToInitialState()`

## Настройка переходов

Откройте `StateMachineData`, у каждого перехода — кнопка `Edit Conditions`. Для предикатов на основе Neoxider Conditions используйте тип `Condition Entry`.
