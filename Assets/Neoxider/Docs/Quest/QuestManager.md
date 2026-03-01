# QuestManager

Единая точка входа для квестов: реестр состояний, принятие квестов, зачёт целей, проверка условий старта через ConditionEntry. Удобен и для NoCode (UnityEvent), и для кода (перегрузки с QuestConfig).

- **Пространство имён:** `Neo.Quest`
- **Путь:** `Assets/Neoxider/Scripts/Quest/QuestManager.cs`

---

## Роль

- Хранит список **QuestState** (принятые и завершённые квесты).
- При **AcceptQuest** проверяет StartConditions по **Condition Context** (GameObject).
- При **CompleteObjective** обновляет прогресс/завершает цель и при выполнении всех целей помечает квест Completed и вызывает OnQuestCompleted.
- Предоставляет **NotifyKill** / **NotifyCollect** для целей типа KillCount / CollectCount.

---

## Доступ

- **QuestManager.Instance** — синглтон (первый экземпляр в сцене).
- В сцене должен быть один GameObject с компонентом QuestManager. В **Known Quests** перетащите все QuestConfig, которые будут приниматься по Id.

---

## Condition Context

- Поле **Condition Context** (GameObject) — объект, передаваемый в `ConditionEntry.Evaluate(context)` при проверке условий старта квеста.
- Обычно указывают **игрока** или объект «менеджер мира». На него можно добавить компонент [QuestContext](QuestContext.md) как маркер.
- Если не назначен, при проверке используется gameObject самого QuestManager.

---

## API (краткая таблица)

| Метод | Описание |
|-------|----------|
| **AcceptQuest(string questId)** | Принять квест по Id (для UnityEvent). Возвращает true при успехе. |
| **AcceptQuest(QuestConfig quest)** | Принять квест по конфигу (типобезопасно). |
| **TryAcceptQuest(string questId, out string failReason)** | Принять с причиной отказа. |
| **CompleteObjective(string questId, int objectiveIndex)** | Зачесть цель (для Notifier/кода). |
| **CompleteObjective(QuestConfig quest, int index)** | То же по конфигу. |
| **NotifyKill(string enemyId)** | Уведомить об убийстве (для KillCount). |
| **NotifyCollect(string itemId)** | Уведомить о сборе предмета (для CollectCount). |
| **GetState(string questId)** | Получить состояние квеста или null. |
| **GetState(QuestConfig quest)** | То же по конфигу. |
| **IsActive(QuestConfig quest)** | Квест в статусе Active. |
| **IsCompleted(QuestConfig quest)** | Квест в статусе Completed. |
| **GetObjectiveProgress(QuestConfig quest, int index)** | Текущий прогресс по цели. |

---

## События

### UnityEvent (NoCode)

- **OnQuestAccepted** (string questId) — квест принят.
- **OnObjectiveProgress** (string questId, int objectiveIndex, int currentCount) — обновлён прогресс по цели.
- **OnQuestCompleted** (string questId) — все цели выполнены, квест завершён.

### C# (код)

- **QuestAccepted** (QuestConfig)
- **ObjectiveProgress** (QuestConfig, int objectiveIndex, int currentCount)
- **QuestCompleted** (QuestConfig)

Пример подписки из кода:

```csharp
QuestManager.Instance.QuestCompleted += (config) =>
{
    Debug.Log($"Quest completed: {config.Title}");
};
```

---

## Проверка StartConditions

- Выполняется при вызове **AcceptQuest** (по id или по конфигу).
- Для каждой записи в QuestConfig.StartConditions вызывается `entry.Evaluate(context)`; context = Condition Context или gameObject менеджера.
- Если хотя бы одно условие false — квест не принимается, метод возвращает false.
- Если список пуст — квест доступен без проверки.
