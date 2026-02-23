# Переиспользование условий в других системах

Условия Neoxider (объект → компонент → свойство → сравнение → порог) сделаны **универсальными**: один и тот же тип условия можно настраивать и использовать не только в **NeoCondition**, но и в **State Machine**, триггерах, квестах и любых своих системах.

## Контракт: IConditionEvaluator

Все «условия» в Neoxider приводятся к одному интерфейсу:

```csharp
namespace Neo.Condition
{
    public interface IConditionEvaluator
    {
        /// <param name="context">GameObject-владелец (fallback при пустом источнике).</param>
        /// <returns>true, если условие выполнено.</returns>
        bool Evaluate(GameObject context);
    }
}
```

- **NeoCondition** хранит список `ConditionEntry` и вызывает `entry.Evaluate(gameObject)`.
- **State Machine** использует предикат `ConditionEntryPredicate`, внутри которого вызывается `conditionEntry.Evaluate(context)`.
- Любая своя система может принимать `IConditionEvaluator` (или конкретно `ConditionEntry`) и вызывать `Evaluate(context)` с подходящим контекстом.

Контекст (`GameObject context`) используется как fallback, когда у условия не задан источник (Source Object пуст или объект ещё не найден по имени).

---

## Где уже переиспользуются условия

| Система | Как подключается | Контекст |
|--------|-------------------|----------|
| **NeoCondition** | Список `List<ConditionEntry>`, логика AND/OR, события On True/On False | `gameObject` (владелец NeoCondition) |
| **State Machine** | Предикат перехода `ConditionEntryPredicate`: поле `ConditionEntry` + опционально `contextObject` | `contextObject` или `(currentState as MonoBehaviour)?.gameObject` |

В State Machine: **Add Condition → Neoxider Condition**, затем настраивается одно условие (источник, компонент, свойство, сравнение, порог) так же, как в NeoCondition.

---

## Как добавить условия в свою систему

### 1. Подключить сборку Neo.Condition

В `.asmdef` своей сборки добавьте ссылку на `Neo.Condition` (GUID сборки можно взять из `Assets/Neoxider/Scripts/Condition/Neo.Condition.asmdef.meta`).

### 2. Хранить условие или список условий

Варианты:

- **Одно условие:** поле типа `ConditionEntry` (сериализуется Unity).
- **Несколько условий:** `List<ConditionEntry>` и при оценке комбинировать результаты (AND/OR), по аналогии с NeoCondition.
- **Абстракция:** поле типа, реализующего `IConditionEvaluator`; в инспекторе чаще всего будет именно `ConditionEntry`, т.к. он сериализуем и рисуется нашим редактором.

Пример для одного условия:

```csharp
using Neo.Condition;
using UnityEngine;

public class MyTrigger : MonoBehaviour
{
    [SerializeField] private ConditionEntry condition;

    public bool Check()
    {
        if (condition == null) return true;
        return condition.Evaluate(gameObject); // context = этот объект
    }
}
```

Пример для списка (AND):

```csharp
[SerializeField] private List<ConditionEntry> conditions = new();

public bool CheckAll()
{
    if (conditions == null || conditions.Count == 0) return true;
    foreach (var c in conditions)
    {
        if (c == null) continue;
        if (!c.Evaluate(gameObject)) return false;
    }
    return true;
}
```

### 3. Выбор контекста (GameObject)

В `Evaluate(context)` передавайте тот GameObject, который должен подставляться, когда у условия не задан **Source Object** и не используется **Find By Name**:

- Обычно это «владелец» логики: например, `gameObject` компонента, который проверяет условия.
- В State Machine контекст — либо явно заданный объект, либо объект текущего состояния (`currentState as MonoBehaviour`).

Если ваш компонент живёт на одном объекте, а проверять нужно «от лица» другого (например, NPC), передавайте в `Evaluate` этот другой объект.

### 4. Редактор (инспектор)

Для полей типа `ConditionEntry` уже заведён **CustomPropertyDrawer** (`ConditionEntryDrawer`). Как только в вашем компоненте есть поле `ConditionEntry` или `List<ConditionEntry>`, Unity автоматически подставит тот же блок настройки условия (источник, компонент, свойство, сравнение, порог), что и в NeoCondition и в переходе State Machine.

Ничего дополнительно в кастомном редакторе делать не нужно: достаточно объявить поле и вывести его через `EditorGUILayout.PropertyField(serializedProperty)` или стандартный инспектор.

---

## Предикат для State Machine (пример интеграции)

Чтобы использовать условие **в переходах** State Machine без своего кода:

1. Открыть переход (State Machine Data → переход).
2. **Add Condition → Neoxider Condition**.
3. В появившемся блоке настроить одно условие (как в NeoCondition).
4. При необходимости задать **Context Object** (если пусто, контекстом будет GameObject текущего состояния).

Реализация с вашей стороны не требуется: предикат `ConditionEntryPredicate` и редактор переходов уже поддерживают это.

---

## Сводка

| Задача | Действие |
|--------|----------|
| Использовать одно условие в своей системе | Поле `ConditionEntry`, вызов `entry.Evaluate(contextGameObject)`. |
| Несколько условий (AND/OR) | `List<ConditionEntry>`, цикл с `Evaluate`, объединение результатов по своей логике. |
| Контекст | Передавать в `Evaluate` тот GameObject, который должен быть fallback при пустом источнике. |
| Инспектор | Достаточно поля типа `ConditionEntry` (или списка); UI даёт `ConditionEntryDrawer`. |
| State Machine | Добавить условие через **Add Condition → Neoxider Condition** и настроить запись. |
| Сборка | В asmdef своей сборки добавить ссылку на `Neo.Condition`. |

Подробнее про настройку одного условия (Source, Component, Property, Compare, порог) см. [NeoCondition.md](./NeoCondition.md).
