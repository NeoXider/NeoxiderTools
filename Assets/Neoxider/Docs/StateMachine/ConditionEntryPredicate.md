# ConditionEntryPredicate

**Назначение:** Предикат перехода машины состояний, который использует `NeoCondition`-стиль проверки условий. Оценивает `ConditionEntry` (компонент, свойство, сравнение, порог) в контексте указанного GameObject. Слот контекста задаётся через `ConditionContextSlot` — ScriptableObject не хранит ссылки на сцену, только номер слота.

---

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Condition Entry** | Условие для проверки — объект, компонент, свойство, тип сравнения и пороговое значение (тот же формат, что в `NeoCondition`). |
| **Context Slot** | Откуда брать `GameObject` для проверки. `Owner` (0) — объект с `StateMachine`. `Override1..5` — из списка **Context Overrides** на `StateMachineBehaviour`. |

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `ConditionEntry ConditionEntry { get; set; }` | Условие (объект, свойство, сравнение, порог). |
| `ConditionContextSlot ContextSlot { get; set; }` | Слот контекста: `Owner`, `Override1`..`Override5`. |

---

## Enum: ConditionContextSlot

| Значение | Описание |
|----------|----------|
| `Owner` (0) | GameObject, на котором висит StateMachine. |
| `Override1` (1) | Первый элемент в списке Context Overrides на StateMachineBehaviour. |
| `Override2..5` | Второй..пятый элементы. |

---

## Примеры

### No-Code (Inspector)
1. В `StateTransition` добавить **Predicate** → `ConditionEntryPredicate`.
2. В **Condition Entry** настроить проверку (например: `HealthComponent` → `HpPercentValue` → `LessThan` → `0.3`).
3. **Context Slot** = `Owner` — проверять здоровье на объекте с машиной состояний.
4. Переход сработает, когда HP < 30%.

### Код
```csharp
var predicate = new ConditionEntryPredicate
{
    ConditionEntry = new ConditionEntry { /* настройка */ },
    ContextSlot = ConditionContextSlot.Owner
};
transition.AddPredicate(predicate);
```

---

## См. также
- [NeoCondition](../Condition/NeoCondition.md) — система условий
- [StateTransition](StateTransition.md) — переход с предикатами
- ← [StateMachine](README.md)
