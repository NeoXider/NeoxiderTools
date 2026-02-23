# StateMachineBehaviourBase

## Описание

`StateMachineBehaviourBase` — основной no-code компонент автомата состояний для Unity (`MonoBehaviour`).
Работает с `StateMachineData`, поддерживает события, runtime-управление и свойства текущего состояния.

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

## Примечание для no-code

Для настройки переходов откройте `StateMachineData` и используйте кнопку `Edit Conditions` у каждого перехода.
Для предикатов на основе Neoxider Conditions используйте тип `Condition Entry`.
