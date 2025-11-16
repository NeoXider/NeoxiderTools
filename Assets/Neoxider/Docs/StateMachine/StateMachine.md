# StateMachine<TState>

## Описание

Основной класс State Machine с поддержкой кэширования состояний и переходов для оптимизации производительности.

## Пространство имен

`Neo.StateMachine`

## Путь к файлу

`Assets/Neoxider/Scripts/StateMachine/StateMachine.cs`

## Конструктор

```csharp
public StateMachine(bool enableStateCaching = true, bool enableTransitionCaching = true)
```

**Параметры:**
- `enableStateCaching` - включить кэширование состояний (по умолчанию true)
- `enableTransitionCaching` - включить кэширование переходов (по умолчанию true)

## Основные методы

### ChangeState<T>()
Сменить состояние по типу.

```csharp
stateMachine.ChangeState<IdleState>();
```

### ChangeState(TState state)
Сменить состояние по экземпляру.

```csharp
stateMachine.ChangeState(idleState);
```

### TryChangeState<T>()
Попытаться сменить состояние с проверкой условий.

```csharp
if (stateMachine.TryChangeState<RunningState>())
{
    Debug.Log("State changed successfully");
}
```

### RegisterTransition(StateTransition transition)
Зарегистрировать переход в State Machine.

```csharp
var transition = new StateTransition { ... };
stateMachine.RegisterTransition(transition);
```

### EvaluateTransitions()
Оценить все доступные переходы и выполнить переход, если условия выполнены.

```csharp
stateMachine.EvaluateTransitions();
```

### Update() / FixedUpdate() / LateUpdate()
Обновить текущее состояние.

```csharp
stateMachine.Update();
stateMachine.FixedUpdate();
stateMachine.LateUpdate();
```

## Свойства

- `CurrentState` - текущее активное состояние
- `PreviousState` - предыдущее состояние
- `OnStateChanged` - событие смены состояния
- `OnStateEntered` - событие входа в состояние
- `OnStateExited` - событие выхода из состояния
- `OnTransitionEvaluated` - событие оценки перехода

## Примеры

См. основной README.md

