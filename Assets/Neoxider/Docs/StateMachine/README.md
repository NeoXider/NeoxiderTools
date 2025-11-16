# State Machine System

## 1. Введение

State Machine System — это полнофункциональная система управления состояниями для Unity, которая поддерживает как код-реализацию, так и NoCode конфигурацию через ScriptableObject. Система включает в себя кэширование состояний и переходов для оптимизации производительности, систему предикатов для сложных условий переходов, и визуальный редактор для удобной настройки.

---

## 2. Основные компоненты

### 2.1. IState
**Пространство имен**: `Neo.StateMachine`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/IState.cs`

Интерфейс для всех состояний в системе. Определяет жизненный цикл состояния:
- `OnEnter()` - вызывается при входе в состояние
- `OnUpdate()` - вызывается каждый кадр
- `OnExit()` - вызывается при выходе из состояния
- `OnFixedUpdate()` - для физики (опционально)
- `OnLateUpdate()` - для поздних обновлений (опционально)

**Пример использования:**
```csharp
public class IdleState : IState
{
    public void OnEnter()
    {
        Debug.Log("Entered Idle State");
    }
    
    public void OnUpdate()
    {
        // Логика обновления
    }
    
    public void OnExit()
    {
        Debug.Log("Exited Idle State");
    }
    
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
}
```

### 2.2. StateMachine<TState>
**Пространство имен**: `Neo.StateMachine`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/StateMachine.cs`

Основной класс State Machine с поддержкой кэширования состояний и переходов.

**Ключевые особенности:**
- Кэширование экземпляров состояний (по умолчанию включено)
- Кэширование переходов для быстрого поиска
- Автоматическая оценка переходов
- События для отслеживания изменений состояний

**Пример использования:**
```csharp
var stateMachine = new StateMachine<IState>();

// Регистрация перехода
var transition = new StateTransition
{
    FromStateType = typeof(IdleState),
    ToStateType = typeof(RunningState)
};
stateMachine.RegisterTransition(transition);

// Смена состояния
stateMachine.ChangeState<IdleState>();

// Обновление
stateMachine.Update();
stateMachine.EvaluateTransitions(); // Автоматическая оценка переходов
```

### 2.3. StateMachineBehaviour<TState>
**Пространство имен**: `Neo.StateMachine`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs`

MonoBehaviour версия State Machine для использования на GameObject.

**Ключевые поля:**
- `initialStateTypeName` - тип начального состояния
- `enableDebugLog` - включить логирование
- `showStateInInspector` - показывать текущее состояние в инспекторе
- `stateMachineData` - NoCode конфигурация (опционально)
- `autoEvaluateTransitions` - автоматически оценивать переходы каждый кадр

**Пример использования:**
```csharp
public class PlayerStateMachine : StateMachineBehaviour<IState>
{
    private void Start()
    {
        ChangeState<IdleState>();
    }
}
```

---

## 3. Система переходов

### 3.1. StateTransition
**Пространство имен**: `Neo.StateMachine`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/StateTransition.cs`

Класс для определения переходов между состояниями.

**Ключевые свойства:**
- `FromStateType` / `FromStateName` - исходное состояние
- `ToStateType` / `ToStateName` - целевое состояние
- `Predicates` - список предикатов для условий
- `Priority` - приоритет перехода
- `IsEnabled` - включен ли переход

**Пример использования:**
```csharp
var transition = new StateTransition
{
    FromStateType = typeof(IdleState),
    ToStateType = typeof(RunningState),
    Priority = 1
};

transition.AddPredicate(new FloatComparisonPredicate
{
    Value = player.Health,
    Comparison = ComparisonType.GreaterThan,
    Threshold = 50f
});

stateMachine.RegisterTransition(transition);
```

### 3.2. StatePredicate
**Пространство имен**: `Neo.StateMachine`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs`

Базовый класс для предикатов условий переходов.

**Доступные предикаты:**
- `BoolPredicate` - проверка bool значения
- `FloatComparisonPredicate` - сравнение float значений
- `IntComparisonPredicate` - сравнение int значений
- `StringComparisonPredicate` - сравнение строк
- `EventPredicate` - условие по UnityEvent
- `CustomPredicate` - кастомный предикат через делегат
- `StateDurationPredicate` - проверка времени в состоянии
- `AndPredicate` - комбинация предикатов через AND
- `OrPredicate` - комбинация предикатов через OR
- `NotPredicate` - инверсия предиката

**Пример использования:**
```csharp
// Комбинирование предикатов
var andPredicate = new AndPredicate();
andPredicate.AddPredicate(new FloatComparisonPredicate
{
    Value = player.Health,
    Comparison = ComparisonType.GreaterThan,
    Threshold = 50f
});
andPredicate.AddPredicate(new BoolPredicate { Value = player.IsAlive });

transition.AddPredicate(andPredicate);
```

---

## 4. NoCode конфигурация

### 4.1. StateData
**Пространство имен**: `Neo.StateMachine.NoCode`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/NoCode/StateData.cs`

ScriptableObject для создания состояний без кода.

**Создание:**
1. В меню Unity: `Create > Neo > State Machine > State Data`
2. Настроить имя состояния
3. Добавить действия при входе, обновлении и выходе

**Доступные действия:**
- `LogStateAction` - логирование
- `SetGameObjectActiveAction` - включение/выключение GameObject
- `InvokeUnityEventAction` - вызов UnityEvent
- `ChangeSceneAction` - смена сцены

### 4.2. StateMachineData
**Пространство имен**: `Neo.StateMachine.NoCode`  
**Путь к файлу**: `Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs`

ScriptableObject для полной конфигурации State Machine.

**Создание:**
1. В меню Unity: `Create > Neo > State Machine > State Machine Data`
2. Добавить состояния (StateData)
3. Установить начальное состояние
4. Настроить переходы между состояниями
5. Присвоить в `StateMachineBehaviour.stateMachineData`

**Использование:**
```csharp
// StateMachineBehaviour автоматически загрузит конфигурацию в Start()
// Или вызвать вручную:
stateMachineBehaviour.LoadFromStateMachineData();
```

---

## 5. Кэширование

### 5.1. Кэширование состояний
По умолчанию включено. Экземпляры состояний создаются один раз и переиспользуются.

**Отключение:**
```csharp
var stateMachine = new StateMachine<IState>(enableStateCaching: false);
```

**Очистка кэша:**
```csharp
stateMachine.ClearStateCache();
```

### 5.2. Кэширование переходов
По умолчанию включено. Переходы кэшируются по типу исходного состояния для быстрого поиска.

**Отключение:**
```csharp
var stateMachine = new StateMachine<IState>(enableTransitionCaching: false);
```

**Очистка кэша:**
```csharp
stateMachine.ClearTransitionCache();
```

---

## 6. События

StateMachine предоставляет следующие события:

- `OnStateChanged` - вызывается при смене состояния (параметры: from, to)
- `OnStateEntered` - вызывается при входе в состояние
- `OnStateExited` - вызывается при выходе из состояния
- `OnTransitionEvaluated` - вызывается при оценке перехода

**Пример использования:**
```csharp
stateMachine.OnStateChanged.AddListener((from, to) =>
{
    Debug.Log($"State changed: {from?.GetType().Name} -> {to?.GetType().Name}");
});
```

---

## 7. Кастомный редактор Inspector

Кастомный редактор для `StateMachineData` и `StateMachineBehaviour` предоставляет:
- Улучшенный инспектор с валидацией
- Отображение текущего состояния в Play Mode
- Кнопки для тестирования переходов

---

## 8. Примеры использования

### 8.1. Простая State Machine для игрока

```csharp
public class IdleState : IState
{
    public void OnEnter() { }
    public void OnUpdate() { }
    public void OnExit() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
}

public class RunningState : IState
{
    public void OnEnter() { }
    public void OnUpdate() { }
    public void OnExit() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
}

public class PlayerStateMachine : StateMachineBehaviour<IState>
{
    private void Start()
    {
        ChangeState<IdleState>();
        
        // Регистрация перехода
        var transition = new StateTransition
        {
            FromStateType = typeof(IdleState),
            ToStateType = typeof(RunningState)
        };
        
        transition.AddPredicate(new BoolPredicate { Value = Input.GetKey(KeyCode.Space) });
        RegisterTransition(transition);
    }
}
```

### 8.2. NoCode конфигурация

1. Создать `StateMachineData` через меню
2. Создать несколько `StateData` для состояний
3. Настроить переходы в инспекторе
4. Присвоить `StateMachineData` в `StateMachineBehaviour`
5. State Machine автоматически загрузится при старте

---

## 9. Рекомендации

1. **Используйте кэширование** - по умолчанию включено, не отключайте без необходимости
2. **Группируйте переходы** - используйте приоритеты для управления порядком проверки
3. **Комбинируйте предикаты** - используйте AndPredicate/OrPredicate для сложных условий
4. **Валидируйте конфигурацию** - используйте `StateMachineData.Validate()` перед использованием
5. **Используйте события** - подписывайтесь на события для отслеживания изменений состояний

---

## 10. Производительность

- Кэширование состояний уменьшает аллокации
- Кэширование переходов ускоряет поиск доступных переходов
- Автоматическая оценка переходов может быть отключена через `autoEvaluateTransitions = false`
- Используйте приоритеты переходов для оптимизации проверки условий

---

## 11. Известные ограничения

- NoCode состояния не поддерживают `OnFixedUpdate` и `OnLateUpdate` по умолчанию
- Переходы по именам (NoCode) требуют наличия `StateMachineData`

---

## 12. Версия

Текущая версия: 1.0.0

---

## 13. Дополнительная информация

### Документация

- [StateMachine](StateMachine.md) - Документация по основному классу StateMachine
- [StateMachineBehaviour](StateMachineBehaviour.md) - Документация по MonoBehaviour компоненту

### XML документация

Для более подробной информации смотрите XML документацию в исходном коде.

### Требования

- Unity 2021.3 LTS или выше

---

## 14. История изменений

### Версия 2.0.0 (текущая)
- ✅ Удален старый GraphView редактор
- ✅ Упрощенная архитектура - только ScriptableObject и код
- ✅ Улучшенный кастомный редактор Inspector
- ✅ Обновлена документация

### Версия 1.0.0
- Базовая функциональность State Machine
- NoCode конфигурация через ScriptableObject
- Граф редактор на основе GraphView (устарел)

