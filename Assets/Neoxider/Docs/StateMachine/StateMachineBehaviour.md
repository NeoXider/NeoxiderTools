# StateMachineBehaviour<TState>

## Описание

MonoBehaviour версия State Machine для использования на GameObject. Автоматически вызывает Update, FixedUpdate и LateUpdate для текущего состояния.

## Пространство имен

`Neo.StateMachine`

## Путь к файлу

`Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs`

## Добавление компонента

В меню Unity: `Component > Neo > Tools > StateMachineBehaviour`

## Ключевые поля

- `initialStateTypeName` - тип начального состояния (для кода)
- `enableDebugLog` - включить логирование переходов
- `showStateInInspector` - показывать текущее состояние в инспекторе
- `stateMachineData` - NoCode конфигурация (опционально)
- `autoEvaluateTransitions` - автоматически оценивать переходы каждый кадр

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

## Примеры

См. основной README.md


