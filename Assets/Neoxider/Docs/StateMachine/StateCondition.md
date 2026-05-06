# StateCondition

**Назначение:** Базовый абстрактный класс условия перехода для `StateMachine`. Содержит единственный метод `Evaluate()` → `bool`. Три готовые реализации: `BoolStateCondition`, `FloatStateCondition`, `EventStateCondition`.

> Для новых проектов рекомендуется `StatePredicate` — более гибкая замена.

---

## API

| Метод | Описание |
|-------|----------|
| `abstract bool Evaluate()` | Проверить условие. `true` — переход разрешён. |

---

## Готовые реализации

### BoolStateCondition
Возвращает хранимое bool-значение.

| Поле | Описание |
|------|----------|
| **Value** | `bool` — значение, которое вернёт `Evaluate()`. Можно менять из кода или Inspector. |

### FloatStateCondition
Сравнивает float-значение с порогом.

| Поле | Описание |
|------|----------|
| **Value** | `float` — левая часть сравнения. |
| **Comparison** | Оператор: `>`, `<`, `>=`, `<=`, `==`, `!=`. |
| **Threshold** | `float` — правая часть сравнения. |

### EventStateCondition
Условие через UnityEvent: при `Evaluate()` вызывается `OnEvaluate`, слушатель задаёт результат через `SetResult(bool)`.

| Поле | Описание |
|------|----------|
| **On Evaluate** | `UnityEvent` — вызывается при проверке условия. |

| Метод | Описание |
|-------|----------|
| `void SetResult(bool result)` | Задать результат для текущей проверки. Вызывается из слушателя `OnEvaluate`. |

---

## Примеры

### No-Code (Inspector)
1. В `StateMachineBehaviour` добавить переход с `FloatStateCondition`.
2. Привязать поле **Value** к здоровью персонажа.
3. Задать **Comparison** = `LessThan`, **Threshold** = `0`.
4. Переход сработает, когда здоровье опустится ниже нуля.

### Код
```csharp
var condition = new FloatStateCondition();
condition.Value = player.Health;
condition.Comparison = ComparisonType.LessThan;
condition.Threshold = 0f;

if (condition.Evaluate())
    stateMachine.ChangeState<DeadState>();
```

---

## См. также
- [StatePredicate](StatePredicate.md) — более мощная альтернатива
- [StateMachine](StateMachine.md)
- ← [StateMachine](README.md)
