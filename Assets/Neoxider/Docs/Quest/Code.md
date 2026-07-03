# Using Quests from C#

**Purpose:** API reference for calling Quest module methods from code: `QuestManager.Instance`, `AcceptQuest`, `CompleteObjective`, `GetState`, events, `NotifyKill`/`NotifyCollect`. Does not cover Inspector setup — see [Scenarios](Scenarios.md), [QuestManager](QuestManager.md).

**How to use:** obtain the manager via `QuestManager.Instance`, call accept/complete-objective methods, subscribe to events; check for null in `OnDisable` and in scenes without a manager.

---

## Accessing the Manager

```csharp
if (QuestManager.Instance == null) return;
var manager = QuestManager.Instance;
```

---

## Accepting a Quest

```csharp
public QuestConfig mainQuest;

if (QuestManager.Instance.AcceptQuest(mainQuest))
    Debug.Log("Accepted: " + mainQuest.Title);

// by ID
bool ok = QuestManager.Instance.AcceptQuest("MainQuest_01");

// with a rejection reason
if (!QuestManager.Instance.TryAcceptQuest("SideQuest_Goblin", out string reason))
    ShowMessage(reason);
```

---

## State and Progress

```csharp
QuestState state = QuestManager.Instance.GetState(quest);
if (state == null) return;

bool isActive    = QuestManager.Instance.IsActive(quest);
bool isDone      = QuestManager.Instance.IsCompleted(quest);
int  progress    = QuestManager.Instance.GetObjectiveProgress(quest, 0);
bool goalDone    = state.IsObjectiveCompleted(0);
```

---

## Completing an Objective / Failing a Quest

```csharp
QuestManager.Instance.CompleteObjective(myQuestConfig, objectiveIndex: 1);
QuestManager.Instance.FailQuest(myQuestConfig);
```

---

## NotifyKill / NotifyCollect

Call from your own gameplay logic (enemy death, item pickup). The string must match `TargetId` in `QuestObjectiveData`.

```csharp
QuestManager.Instance?.NotifyKill("Goblin");
QuestManager.Instance?.NotifyCollect("Key_Red");
```

---

## C# Events

```csharp
void OnEnable()
{
    if (QuestManager.Instance != null)
    {
        QuestManager.Instance.QuestAccepted     += OnQuestAccepted;
        QuestManager.Instance.ObjectiveProgress += OnObjectiveProgress;
        QuestManager.Instance.QuestCompleted    += OnQuestCompleted;
    }
}

void OnDisable()
{
    if (QuestManager.Instance != null)
    {
        QuestManager.Instance.QuestAccepted     -= OnQuestAccepted;
        QuestManager.Instance.ObjectiveProgress -= OnObjectiveProgress;
        QuestManager.Instance.QuestCompleted    -= OnQuestCompleted;
    }
}

void OnQuestCompleted(QuestConfig config)
{
    GiveRewards(config);
    foreach (string nextId in config.NextQuestIds) UnlockQuestInUI(nextId);
}
```

Event signatures:
- `QuestAccepted` — `(QuestConfig)`
- `ObjectiveProgress` — `(QuestConfig, int, int)`
- `QuestCompleted` — `(QuestConfig)`

---

## Iterating Quests

```csharp
foreach (QuestState state in QuestManager.Instance.ActiveQuests) { ... }
foreach (QuestState state in QuestManager.Instance.AllQuests)    { ... }
```

Full method and event list — [QuestManager](QuestManager.md).
