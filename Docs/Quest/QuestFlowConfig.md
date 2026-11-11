# QuestFlowConfig

**What it is:** a `ScriptableObject` (`Scripts/Quest/QuestFlowConfig.cs`) that groups `QuestConfig`s into quest chains (storylines) and standalone side quests, and answers whether a quest is available under strict chain order.

**How to use:**
1. Create the file via `Right Click > Create > Neoxider > Quest > Quest Flow Config`.
2. Add chains (enable **Strict Order** for linear storylines) and standalone quests.
3. Call `flow.CanAcceptQuest(manager, quest, out reason)` before accepting, or assign it to **Flow Config** on `QuestNoCodeAction`.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_chains` | A list of quest chains (`QuestChain`). Each chain has a name, a list of quests, and a `StrictOrder` toggle (if true, quest #2 cannot be accepted until #1 is completed). |
| `_standaloneQuests` | A list of independent quests that are not tied to any chain. |

## Code Usage

The flow is an optional gate. `QuestManager.AcceptQuest` does not apply it automatically. Call `CanAcceptQuest` before accepting, or assign Flow Config on `QuestNoCodeAction`.

```csharp
if (flow.CanAcceptQuest(manager, quest, out string reason))
{
    manager.AcceptQuest(quest);
}
```

The gate first calls `manager.CanAcceptQuest`, checking the ID, existing state, and start conditions against the manager's context. It then requires the nearest preceding non-null quest in a strict chain to be completed. Null slots do not unlock the next quest. A standalone quest, a quest outside strict chains, or the first non-null quest in a chain has no predecessor restriction.

`IsChainSatisfied(manager, quest, out reason)` checks only the strict-chain rule and runs no start-condition predicates. Combine it with `manager.TryAcceptQuest(quest, out reason)` to evaluate start conditions exactly once per attempt; `QuestNoCodeAction` does this for its Flow Config.

```csharp
if (flow.IsChainSatisfied(manager, quest, out string reason) && manager.TryAcceptQuest(quest, out reason))
{
    ShowAccepted(quest);
}
else
{
    ShowLocked(reason);
}
```

`TryGetPreviousInStrictChain` returns whether the quest belongs to a strict chain; its output is null for that chain's first quest. `BuildOrderedQuestList()` lists chain quests followed by standalone quests, skipping nulls and duplicate config references. If a quest is listed in multiple strict chains, the first matching chain supplies its rule; use unique chain membership for unambiguous progression.

## See Also

- [QuestConfig](QuestConfig.md)
- [Module Root](../README.md)
