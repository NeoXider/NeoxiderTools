# StateMachineBehaviour<TState>

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
- `stateMachineData` - NoCode конфигурация (опционально)
- `autoEvaluateTransitions` - автоматически оценивать переходы каждый кадр

## Runtime свойства (для условий/отладки)

- `CurrentStateName` (`string`)
- `PreviousStateName` (`string`)
- `CurrentStateElapsedTime` (`float`)
- `StateChangeCount` (`int`)
- `HasCurrentState` (`bool`)

Эти свойства можно читать через `ConditionEntry` (компонент `StateMachineBehaviourBase`) в переходах no-code.

## События (Inspector)

- `On Initialized`
- `On State Entered`
- `On State Exited`
- `On State Changed` (`from`, `to`)
- `On Transition Evaluated` (`transitionName`, `result`)

## Runtime Controls (Inspector)

- `Reload Data`
- `Evaluate Now`
- `Go To Initial State`
- `Change State` (ручной выбор состояния из `StateMachineData`)

## Основные методы

### ChangeState<T>()
Сменить состояние по типу.

```csharp
ChangeState<IdleState>();
```

### ChangeState(string stateName)
Сменить состояние по имени (для NoCode).

```csharp
ChangeState("Idle");
```

### LoadFromStateMachineData()
Загрузить конфигурацию из StateMachineData.

```csharp
LoadFromStateMachineData();
```

## No-Code использование

Полная настройка без кода: конфигурация в State Machine Data и условия переходов через Neoxider Condition. Пошаговая инструкция: [NoCode_StateMachine_Usage.md](NoCode_StateMachine_Usage.md).

## Примеры

См. основной README.md


