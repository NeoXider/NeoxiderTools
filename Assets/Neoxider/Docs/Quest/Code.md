# Using Quests from C#

**What it is:** a C# guide for the Quest module: `QuestManager.Instance`, `AcceptQuest`, `CompleteObjective` / `AddObjectiveProgress`, target notifications (`NotifyKill`, `NotifyCollect`, `NotifyReachPoint`, `NotifyTalk`, `Notify`), `GetState`, and C# events. Inspector setup is covered by [Scenarios](Scenarios.md) and [QuestManager](QuestManager.md).

**How to use:** obtain the manager via `QuestManager.Instance`, call accept/progress methods, subscribe to events in `OnEnable` and unsubscribe in `OnDisable`; check for null in scenes without a manager.

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
bool credited = QuestManager.Instance.AddObjectiveProgress(myQuestConfig, objectiveIndex: 0, amount: 10);
QuestManager.Instance.FailQuest(myQuestConfig);
```

`CompleteObjective` adds one to a counter; `AddObjectiveProgress` adds any positive amount, clamped to `RequiredCount`. Other objective types store progress `1` on completion. Already completed objectives and inactive quests ignore further credit. To poll assigned custom objective conditions, call `QuestManager.Instance.EvaluateObjectiveConditions()` at a checkpoint or on a timer.

If a custom predicate restarts the quest, its result does not credit the replacement. Evaluate again to process that new active state.

---

## Target notifications

Call from your own gameplay logic (enemy death, item pickup, trigger volume, dialogue end). The string must match `TargetId` in `QuestObjectiveData` (ordinal, case-sensitive); every active objective of that type and id is credited.

```csharp
QuestManager manager = QuestManager.Instance;
if (manager != null)
{
    manager.NotifyKill("Goblin");          // KillCount +1
    manager.NotifyKill("Goblin", 5);       // KillCount +5 (area attack), clamped to RequiredCount
    manager.NotifyCollect("Key_Red");      // CollectCount +1
    manager.NotifyReachPoint("OldMill");   // completes ReachPoint objectives
    manager.NotifyTalk("Elder");           // completes Talk objectives
    int credited = manager.Notify(QuestObjectiveType.CustomCondition, "Door_Opened");
}
```

`Notify(type, targetId, amount)` is the general form and returns how many objectives received credit (0 when nothing matched). It also addresses `CustomCondition` objectives that use `TargetId` as an event name. A null or empty target id credits nothing. To credit one objective by index with an amount, use `AddObjectiveProgress(quest, index, amount)`; it returns false when nothing was credited.

The notifications allocate nothing when warm, so they can be called for every kill or pickup. Configs accepted by reference are registered automatically. For configs created after loading, call `manager.RegisterQuest(config)` before progressing their restored states. Notifications apply to the current active states at call entry; quests accepted/restarted by listeners receive the next notification.

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
private QuestManager _quests;

void OnEnable()
{
    _quests = QuestManager.Instance;
    if (_quests != null)
    {
        _quests.QuestAccepted      += OnQuestAccepted;
        _quests.ObjectiveProgress  += OnObjectiveProgress;
        _quests.ObjectiveCompleted += OnObjectiveCompleted;
        _quests.QuestCompleted     += OnQuestCompleted;
        _quests.QuestFailed        += OnQuestFailed;
        _quests.QuestReset         += OnQuestReset;
        _quests.QuestsLoaded       += RefreshJournal;
    }
}

void OnDisable()
{
    if (_quests != null)
    {
        _quests.QuestAccepted      -= OnQuestAccepted;
        _quests.ObjectiveProgress  -= OnObjectiveProgress;
        _quests.ObjectiveCompleted -= OnObjectiveCompleted;
        _quests.QuestCompleted     -= OnQuestCompleted;
        _quests.QuestFailed        -= OnQuestFailed;
        _quests.QuestReset         -= OnQuestReset;
        _quests.QuestsLoaded       -= RefreshJournal;
    }

    _quests = null;
}

void OnQuestCompleted(QuestConfig config)
{
    GiveRewards(config);
    foreach (string nextId in config.NextQuestIds) UnlockQuestInUI(nextId);
}
```

Event signatures:
- `QuestAccepted` — `(QuestConfig)`
- `ObjectiveProgress` — `(QuestConfig config, int objectiveIndex, int progress)`
- `ObjectiveCompleted` — `(QuestConfig config, int objectiveIndex)`, once per objective, after `ObjectiveProgress`
- `QuestCompleted` — `(QuestConfig)`
- `QuestFailed` — `(string questId)`
- `QuestReset` — `(string questId)`, for `ResetQuest`, every state removed by `ResetAllQuests`, and the reset step of `RestartQuest`
- `QuestsLoaded` — `()`, after `Load()` replaced the registry (no gameplay events are replayed)

Progress and objective completion flags are committed before progress callbacks. Events are synchronous, so nested operations can interleave delivery; refresh UI from current state. The manager does not automatically process `NextQuestIds`.

---

## Iterating Quests

```csharp
private readonly List<QuestState> _activeBuffer = new();

void RefreshJournal()
{
    QuestManager manager = QuestManager.Instance;
    if (manager == null) return;

    manager.GetActiveQuests(_activeBuffer);              // no allocation
    foreach (QuestState state in _activeBuffer) { ... }
    foreach (QuestState state in manager.AllQuests) { ... }
}
```

`ActiveQuests` returns a new list on every access; prefer `GetActiveQuests(buffer)` in UI that refreshes often.

---

## Validating configs

```csharp
IReadOnlyList<string> issues = questConfig.ValidateDefinition();
foreach (string issue in issues) Debug.LogWarning($"{questConfig.name}: {issue}");
```

Reports an empty or whitespace-only id, no objectives, null objectives, and target-driven objectives (`KillCount`, `CollectCount`, `ReachPoint`, `Talk`) without a `TargetId`.

Full method and event list — [QuestManager](QuestManager.md).
