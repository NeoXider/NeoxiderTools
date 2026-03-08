# StateMachine

**Что это:** модуль автомата состояний с двумя режимами работы: code-first через `StateMachine<TState>` и inspector/no-code через `StateMachineData` + `StateMachineBehaviourBase`. Скрипты находятся в `Scripts/StateMachine/`.

**Навигация:** [← К Docs](../README.md) · обзор ниже

---

## Когда что использовать

- Нужен чистый кодовый автомат с типизированными состояниями: `StateMachine<TState>` и `StateMachineBehaviour<TState>`.
- Нужна настройка через инспектор и `ScriptableObject`: `StateMachineBehaviourBase` и `StateMachineData`.
- Нужны условия переходов без жёстких ссылок на объекты сцены: `ConditionEntryPredicate` и контекстные слоты в `StateMachineEvaluationContext`.

## Основные части

| Часть | Назначение |
|------|------------|
| `IState` | Базовый жизненный цикл состояния: `OnEnter`, `OnUpdate`, `OnExit`, опционально `OnFixedUpdate`, `OnLateUpdate`. |
| `StateMachine<TState>` | Ядро автомата: смена состояния, регистрация переходов, кэширование, события. |
| `StateTransition` | Описание перехода между состояниями с приоритетом и списком предикатов. |
| `StatePredicate` | Базовый тип условий перехода; есть bool/int/float/string/composite и др. |
| `StateMachineBehaviour<TState>` | Generic `MonoBehaviour`-обёртка над кодовым автоматом. |
| `StateMachineBehaviourBase` | Inspector-friendly runtime-компонент для `StateMachineData`. |
| `StateData` | Описание одного no-code состояния через `ScriptableObject`. |
| `StateMachineData` | Конфигурация состояний, переходов и начального состояния. |

## Типовой поток

### Code-first
1. Реализовать `IState`.
2. Создать `StateMachine<TState>` или наследник `StateMachineBehaviour<TState>`.
3. Зарегистрировать `StateTransition`.
4. Вызывать `EvaluateTransitions()` вручную или включить автоматическую оценку в behaviour-обёртке.

### No-code
1. Создать `StateData` и `StateMachineData`.
2. Добавить на сцену `StateMachineBehaviourBase`.
3. Назначить `StateMachineData`.
4. Заполнить `Context for conditions`, если условиям нужны объекты сцены.
5. Использовать inspector events и runtime-кнопки компонента.

## Что важно в текущей версии

- `StateMachineBehaviour<TState>` и `StateMachineBehaviourBase` не равнозначны по API.
- Generic-версия удобна для кода, но не даёт inspector-события и runtime-поля из `Base`.
- `StateMachineData` не хранит прямые ссылки на scene object; для этого используются context slots.
- Кэширование состояний и переходов встроено в ядро и включено по умолчанию.

## Куда идти дальше

- [StateMachine](./StateMachine.md) — API кодового ядра.
- [StateMachineBehaviour](./StateMachineBehaviour.md) — generic `MonoBehaviour`-обёртка.
- [StateMachineBehaviourBase](./StateMachineBehaviourBase.md) — inspector/no-code компонент.
- [NoCode_StateMachine_Usage](./NoCode_StateMachine_Usage.md) — настройка `StateMachineData` и условий.

## См. также

- [Condition](../Condition/README.md)
- [Tools](../Tools/README.md)
**Пример использования:**
```csharp
stateMachine.OnStateChanged.AddListener((from, to) =>
{
    Debug.Log($"State changed: {from?.GetType().Name} -> {to?.GetType().Name}");
});
```

---

## 7. Кастомный редактор Inspector

Кастомный редактор для `StateMachineData`, `StateData` и `StateMachineBehaviourBase` предоставляет:
- Улучшенный no-code workflow для состояний/переходов
- Меню добавления полиморфных `StateAction` и `StatePredicate`
- Runtime controls и runtime-информацию о состоянии
- Секции UnityEvents для интеграции без кода

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
- [StateMachineBehaviourBase](StateMachineBehaviourBase.md) - Документация по no-code компоненту
- [NoCode_StateMachine_Usage](NoCode_StateMachine_Usage.md) - Пошаговая настройка без кода

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

