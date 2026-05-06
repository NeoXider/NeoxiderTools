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

The manager can use the `CanAcceptQuest` method, which automatically checks:
1. Whether the start conditions inside the `QuestConfig` are met.
2. If the quest is in a strict-order chain, whether the previous quest has been completed.

## See Also
- [QuestConfig](QuestConfig.md)
- [Module Root](../README.md)
