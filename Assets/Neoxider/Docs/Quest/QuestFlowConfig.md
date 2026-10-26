# QuestFlowConfig

**Purpose:** Configuration for defining quest chains (storylines) and standalone side quests. Created as a `ScriptableObject`.

## Setup

Create the file via `Right Click > Create > Neoxider > Quest > Quest Flow Config`. This file helps organize individual `QuestConfig`s into logical structures.

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

`TryGetPreviousInStrictChain` returns whether the quest belongs to a strict chain; its output is null for that chain's first quest. `BuildOrderedQuestList()` lists chain quests followed by standalone quests, skipping nulls and duplicate config references. If a quest is listed in multiple strict chains, the first matching chain supplies its rule; use unique chain membership for unambiguous progression.

## See Also

- [QuestConfig](QuestConfig.md)
- [Module Root](../README.md)
