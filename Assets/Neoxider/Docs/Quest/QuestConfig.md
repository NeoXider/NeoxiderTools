# QuestConfig

ScriptableObject с описанием квеста: идентификатор, название, описание, список целей и условия доступности (Start Conditions). Контекст для проверки условий задаётся в [QuestManager](QuestManager.md) (Condition Context).

- **Пространство имён:** `Neo.Quest`
- **Путь:** `Assets/Neoxider/Scripts/Quest/QuestConfig.cs`

---

## Поля

| Поле | Описание |
|------|----------|
| **Id** | Уникальный идентификатор. Используется в AcceptQuest(questId), GetState(questId). При пустом Id в Editor подставляется из Title. |
| **Title** | Название для UI. |
| **Description** | Описание для UI (многострочное). |
| **Objectives** | Список целей (QuestObjectiveData). Порядок задаёт индекс цели (0, 1, …). |
| **Start Conditions** | Список ConditionEntry. Все должны быть true (AND) при AcceptQuest; контекст — GameObject из QuestManager.ConditionContext. |
| **Next Quest Ids** | ID квестов, которые логически открываются после завершения (использование — на усмотрение игры). |

---

## Типы целей (QuestObjectiveType)

| Тип | Как выполняется | Поля в QuestObjectiveData |
|-----|-----------------|----------------------------|
| **CustomCondition** | Внешний триггер (QuestObjectiveNotifier.NotifyComplete) и/или проверка ConditionEntry в менеджере по контексту. | Condition (опционально), иначе только Notifier. |
| **KillCount** | Вызовы QuestManager.NotifyKill(enemyId). Цель выполнена, когда счётчик >= RequiredCount. | TargetId (id врага), RequiredCount. |
| **CollectCount** | Вызовы QuestManager.NotifyCollect(itemId). Аналогично счётчику. | TargetId (id предмета), RequiredCount. |
| **ReachPoint** | Один вызов CompleteObjective (триггер зоны или QuestObjectiveNotifier). | — |
| **Talk** | Один вызов CompleteObjective (диалог или Notifier). | — |

---

## Условия старта

- Задаются списком **ConditionEntry** (тот же формат, что в [NeoCondition](../Condition/NeoCondition.md): объект → компонент/GameObject → свойство → оператор → порог.
- При **AcceptQuest** QuestManager вызывает `ConditionEntry.Evaluate(context)` для каждой записи; context — это **Condition Context** из QuestManager (обычно игрок или менеджер мира).
- Если объект в условии ищется по имени в сцене (Use Scene Search), убедитесь, что контекст и искомые объекты существуют в момент принятия квеста.

---

## Создание конфига

1. В Project: ПКМ → **Create → Neoxider → Quest → Quest Config**.
2. Заполните Id (или оставьте пустым — подставится из Title), Title, Description.
3. Добавьте элементы в **Objectives**; для каждого выберите Type и при необходимости TargetId, RequiredCount или Condition.
4. При необходимости добавьте **Start Conditions** (ConditionEntry).

---

## Пример

Квест «Победи 3 гоблинов и принеси ключ»:

- Цель 0: **KillCount**, TargetId = `Goblin`, RequiredCount = 3.
- Цель 1: **CustomCondition** — без Condition, только внешний триггер: при подборе ключа на зоне/предмете вызывается QuestObjectiveNotifier (квест + индекс 1) → NotifyComplete().

В **Known Quests** QuestManager должен содержать этот конфиг, чтобы AcceptQuest(questId) находил его по Id.
