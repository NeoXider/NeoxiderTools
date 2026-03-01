# Квесты из кода (C#)

Типобезопасный API по ссылке на QuestConfig, проверка результата принятия, подписка на события и интеграция с боем/инвентарём.

---

## Типобезопасность

Используйте перегрузки с **QuestConfig**, чтобы не опечататься в строковом Id:

```csharp
public QuestConfig myQuestConfig; // назначьте в инспекторе или загрузите из Resources

void Start()
{
    if (QuestManager.Instance.AcceptQuest(myQuestConfig))
    {
        Debug.Log("Quest accepted: " + myQuestConfig.Title);
    }

    QuestState state = QuestManager.Instance.GetState(myQuestConfig);
    int progress = QuestManager.Instance.GetObjectiveProgress(myQuestConfig, 0);
    bool done = QuestManager.Instance.IsCompleted(myQuestConfig);
}
```

---

## Проверка результата принятия

**TryAcceptQuest** возвращает причину отказа:

```csharp
if (!QuestManager.Instance.TryAcceptQuest("MainQuest_01", out string reason))
{
    ShowMessage(reason); // "Quest not found.", "Already accepted or completed.", "Start conditions not met."
}
```

---

## Подписка на события из C#

Используйте C#-события с **QuestConfig**:

```csharp
void OnEnable()
{
    QuestManager.Instance.QuestAccepted += OnQuestAccepted;
    QuestManager.Instance.ObjectiveProgress += OnObjectiveProgress;
    QuestManager.Instance.QuestCompleted += OnQuestCompleted;
}

void OnDisable()
{
    if (QuestManager.Instance != null)
    {
        QuestManager.Instance.QuestAccepted -= OnQuestAccepted;
        QuestManager.Instance.ObjectiveProgress -= OnObjectiveProgress;
        QuestManager.Instance.QuestCompleted -= OnQuestCompleted;
    }
}

void OnQuestAccepted(QuestConfig config)
{
    Debug.Log($"Accepted: {config.Title}");
}

void OnObjectiveProgress(QuestConfig config, int objectiveIndex, int currentCount)
{
    // Обновить UI прогресса по цели
}

void OnQuestCompleted(QuestConfig config)
{
    GiveRewards(config);
    UnlockNextQuests(config.NextQuestIds);
}
```

---

## Запрос прогресса

- **GetObjectiveProgress(QuestConfig quest, int index)** — текущее значение счётчика или 1 для уже выполненной цели.
- **IsActive(QuestConfig quest)** / **IsCompleted(QuestConfig quest)** — статус квеста.
- **GetState(quest)** — полное состояние (Status, все цели).

---

## Интеграция с боем и инвентарём

Для целей типа **KillCount** и **CollectCount** вызывайте из своей логики:

```csharp
// При убийстве врага
QuestManager.Instance.NotifyKill("Goblin");

// При подборе предмета
QuestManager.Instance.NotifyCollect("Key_Red");
```

Id должен совпадать с **TargetId** в QuestObjectiveData. Менеджер сам найдёт активные квесты с подходящей целью и обновит прогресс (и завершит цель при достижении RequiredCount).
