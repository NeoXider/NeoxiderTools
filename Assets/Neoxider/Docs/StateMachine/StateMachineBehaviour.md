# StateMachineBehaviour<TState>

**Что это:** MonoBehaviour-версия автомата состояний на GameObject (пространство имён `Neo.StateMachine`, файл `Scripts/StateMachine/StateMachineBehaviour.cs`). Вызывает Update/FixedUpdate/LateUpdate для текущего состояния; переходы через StateMachineData и NeoCondition.

**Как использовать:** добавить компонент (Component → Neoxider → Tools → StateMachineBehaviour); задать initialStateTypeName или stateMachineData; при необходимости включить autoEvaluateTransitions. Свойства CurrentStateName, PreviousStateName и др. доступны для NeoCondition.

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

## Runtime свойства (для условий/отладки)

- `CurrentStateName` (`string`)
- `PreviousStateName` (`string`)
- `CurrentStateElapsedTime` (`float`)
- `StateChangeCount` (`int`)
- `HasCurrentState` (`bool`)

Эти свойства можно читать через `ConditionEntry` в переходах (StateMachineData).

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


