# StateTransition

**Назначение:** Описание перехода между состояниями в машине состояний. Поддерживает условия через `StatePredicate` (логика AND — все предикаты должны пройти), приоритеты, и два режима: код (CLR-типы) и No-Code (ссылки на `StateData`).

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Transition Name** | Имя перехода для отладки и отображения в Inspector. По умолчанию `"Unnamed Transition"`. |
| **From State Data** | Исходное состояние (ссылка на `StateData`). Для No-Code машины. |
| **To State Data** | Целевое состояние (ссылка на `StateData`). Для No-Code машины. |
| **Priority** | Приоритет перехода. Переходы с высшим приоритетом проверяются первыми. |
| **Is Enabled** | Включён ли переход. Если `false` — пропускается при оценке. |
| **Predicates** | Список `StatePredicate` — все условия должны пройти (AND). Если пустой — переход всегда разрешён. |

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `Type FromStateType { get; set; }` | Тип исходного состояния (для код-машины). |
| `Type ToStateType { get; set; }` | Тип целевого состояния (для код-машины). |
| `StateData FromStateData { get; set; }` | Исходное состояние (No-Code). |
| `StateData ToStateData { get; set; }` | Целевое состояние (No-Code). |
| `string FromStateName { get; }` | Имя исходного `StateData`. |
| `string ToStateName { get; }` | Имя целевого `StateData`. |
| `List<StatePredicate> Predicates { get; }` | Список условий перехода. |
| `int Priority { get; set; }` | Приоритет (выше = проверяется раньше). |
| `bool IsEnabled { get; set; }` | Включён/выключен. |
| `string TransitionName { get; set; }` | Имя для отладки. |
| `bool CanTransition(IState currentState)` | Проверить, разрешён ли переход из текущего состояния (тип + предикаты). |
| `bool Evaluate()` | Проверить только предикаты (без проверки исходного состояния). |
| `bool EvaluatePredicates(IState currentState)` | Проверить предикаты с передачей текущего состояния. |
| `void AddPredicate(StatePredicate)` | Добавить условие. |
| `void RemovePredicate(StatePredicate)` | Убрать условие. |
| `bool MatchesFromState(Type)` | Совпадает ли исходный тип (код). |
| `bool MatchesFromState(string)` | Совпадает ли имя исходного состояния (No-Code). |

---

## Примеры

### No-Code (Inspector)
1. В `StateMachineData` добавить элемент в **Transitions**.
2. Указать **From State Data** = `Patrol`, **To State Data** = `Chase`.
3. В **Predicates** добавить `FloatComparisonPredicate` (например, дистанция < 10).
4. Задать **Priority** = `1`.

### Код
```csharp
var transition = new StateTransition
{
    FromStateType = typeof(IdleState),
    ToStateType = typeof(ChaseState),
    Priority = 1
};
transition.AddPredicate(new FloatComparisonPredicate());
stateMachine.RegisterTransition(transition);
```

---

## См. также
- [StatePredicate](StatePredicate.md) — условия перехода
- [StateMachineData](NoCode/StateMachineData.md) — No-Code конфигурация
- [StateMachine](StateMachine.md) — ядро машины
- ← [StateMachine](README.md)
