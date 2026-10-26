# Using Quests from C#

**Purpose:** API reference for calling Quest module methods from code: `QuestManager.Instance`, `AcceptQuest`, `CompleteObjective`, `GetState`, events, `NotifyKill`/`NotifyCollect`. Does not cover Inspector setup — see [Scenarios](Scenarios.md), [QuestManager](QuestManager.md).

**How to use:** obtain the manager via `QuestManager.Instance`, call accept/complete-objective methods, subscribe to events; check for null in `OnDisable` and in scenes without a manager.

---

## Accessing the Manager

```csharp
if (QuestManager.Instance == null) return;
QuestManager manager = QuestManager.Instance;
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

Start-condition getters/methods can call back into the manager. If one accepts the same quest, that nested acceptance wins and the outer attempt returns false, without another state or event sequence. `CanAcceptQuest` also returns false after such a predicate-created state. `TryAcceptQuest` evaluates start conditions once per attempt.

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

Each counter call adds one; other objective types store progress `1` on completion. Already completed objectives and inactive quests ignore further credit. To poll assigned custom objective conditions, call `QuestManager.Instance.EvaluateObjectiveConditions()` at a checkpoint or on a timer.

If a custom predicate restarts the quest, its result does not credit the replacement. Evaluate again to process that new active state.

---

## NotifyKill / NotifyCollect

Call from your own gameplay logic (enemy death, item pickup). The string must match `TargetId` in `QuestObjectiveData`.

```csharp
QuestManager.Instance?.NotifyKill("Goblin");
QuestManager.Instance?.NotifyCollect("Key_Red");
```

Configs accepted by reference are registered automatically. For configs created after loading, call `manager.RegisterQuest(config)` before progressing their restored states. Notifications apply to the current active states at call entry; quests accepted/restarted by listeners receive the next notification.

---

## Runtime config lifetime

Registration after loading grows objective lists and repairs completed legacy binary progress; it preserves counters and status without replaying events. The first config registered for an ID wins. Before destroying a config your code created, release that exact registration:

```csharp
void ReleaseOwnedQuest(QuestManager manager, QuestConfig ownedRuntimeQuest)
{
    if (ownedRuntimeQuest == null) return;
    if (manager != null)
    {
        manager.UnregisterQuest(ownedRuntimeQuest);
    }

    UnityEngine.Object.Destroy(ownedRuntimeQuest);
}
```

`UnregisterQuest` returns true only for the current instance. It leaves saved/runtime progress intact; a stale instance cannot remove another config with the same ID. Register the replacement after releasing the old config to resolve its retained state again. Use `ResetQuest` only when you intend to erase that progress.

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

Progress and objective completion flags are committed before progress callbacks. Events are synchronous, so nested operations can interleave delivery; refresh UI from current state. The manager does not automatically process `NextQuestIds`.

---

## Iterating Quests

```csharp
foreach (QuestState state in QuestManager.Instance.ActiveQuests) { ... }
foreach (QuestState state in QuestManager.Instance.AllQuests)    { ... }
```

Full method and event list — [QuestManager](QuestManager.md).
