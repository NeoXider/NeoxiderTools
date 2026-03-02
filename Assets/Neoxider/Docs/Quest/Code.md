# Использование квестов из C#

**Что это:** справочник по вызовам API модуля Quest из кода: QuestManager.Instance, AcceptQuest, CompleteObjective, GetState, события, NotifyKill/NotifyCollect. Не описывает настройку в инспекторе — см. [Scenarios](Scenarios.md), [QuestManager](QuestManager.md).

**Как использовать:** получить менеджер через `QuestManager.Instance`, вызывать методы принятия/завершения целей, подписаться на события; проверять null при отписке и в сценах без менеджера.

--- Описывает QuestManager.Instance, приём и завершение квестов, запрос состояния, события и интеграцию с боем/инвентарём. Не описывает настройку в инспекторе — см. [Scenarios](Scenarios.md) и [QuestManager](QuestManager.md).

**Как с этим работать:**
1. Получить менеджер: `QuestManager.Instance` (проверять на null в OnDisable и в сценах без менеджера).
2. Принимать квест: `AcceptQuest(QuestConfig)` или `AcceptQuest(string questId)`; при необходимости `TryAcceptQuest(id, out string reason)`.
3. Засчитывать цель: `CompleteObjective(quest, index)` или из геймплея `NotifyKill(enemyId)` / `NotifyCollect(itemId)`.
4. Читать состояние: `GetState(quest)`, `GetObjectiveProgress(quest, index)`, `IsActive(quest)`, `IsCompleted(quest)`.
5. Подписаться на события: QuestAccepted, ObjectiveProgress, QuestCompleted в OnEnable; отписаться в OnDisable.

---

## Доступ к менеджеру

```csharp
if (QuestManager.Instance == null) return;
var manager = QuestManager.Instance;
```

---

## Принятие квеста

```csharp
public QuestConfig mainQuest;

if (QuestManager.Instance.AcceptQuest(mainQuest))
    Debug.Log("Accepted: " + mainQuest.Title);

// по Id
bool ok = QuestManager.Instance.AcceptQuest("MainQuest_01");

// с причиной отказа
if (!QuestManager.Instance.TryAcceptQuest("SideQuest_Goblin", out string reason))
    ShowMessage(reason);
```

---

## Состояние и прогресс

```csharp
QuestState state = QuestManager.Instance.GetState(quest);
if (state == null) return;

bool isActive = QuestManager.Instance.IsActive(quest);
bool isDone = QuestManager.Instance.IsCompleted(quest);
int progress = QuestManager.Instance.GetObjectiveProgress(quest, 0);
bool goalDone = state.IsObjectiveCompleted(0);
```

---

## Зачёт цели и провал

```csharp
QuestManager.Instance.CompleteObjective(myQuestConfig, objectiveIndex: 1);
QuestManager.Instance.FailQuest(myQuestConfig);
```

---

## NotifyKill / NotifyCollect

Вызывать из своей логики (смерть врага, подбор предмета). Строка должна совпадать с TargetId в QuestObjectiveData.

```csharp
QuestManager.Instance?.NotifyKill("Goblin");
QuestManager.Instance?.NotifyCollect("Key_Red");
```

---

## C#-события

```csharp
void OnEnable()
{
    if (QuestManager.Instance != null)
    {
        QuestManager.Instance.QuestAccepted += OnQuestAccepted;
        QuestManager.Instance.ObjectiveProgress += OnObjectiveProgress;
        QuestManager.Instance.QuestCompleted += OnQuestCompleted;
    }
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

void OnQuestCompleted(QuestConfig config)
{
    GiveRewards(config);
    foreach (string nextId in config.NextQuestIds) UnlockQuestInUI(nextId);
}
```

Сигнатуры: QuestAccepted (QuestConfig), ObjectiveProgress (QuestConfig, int, int), QuestCompleted (QuestConfig).

---

## Обход квестов

```csharp
foreach (QuestState state in QuestManager.Instance.ActiveQuests) { ... }
foreach (QuestState state in QuestManager.Instance.AllQuests) { ... }
```

Полный список методов и событий — [QuestManager](QuestManager.md).
