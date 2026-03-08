# QuestConfig

`QuestConfig` is a `ScriptableObject` that defines one quest. It stores identity, display data, objective definitions, optional start conditions, and optional next-quest ids. It does not store runtime progress. File: `Assets/Neoxider/Scripts/Quest/QuestConfig.cs`, namespace: `Neo.Quest`.

## Typical setup

1. Create an asset via `Create > Neoxider > Quest > Quest Config`.
2. Fill `Id`, `Title`, and `Description`.
3. Add entries to `Objectives`.
4. Add `Start Conditions` when the quest should be gated.
5. Register the asset in `QuestManager.Known Quests` so id-based acceptance works.

## Main fields

### Identity

| Field | Description |
|------|-------------|
| `Id` | Unique quest key used by runtime API and events. |

### Display

| Field | Description |
|------|-------------|
| `Title` | UI-facing quest title. |
| `Description` | Quest description. |
| `Icon` | Optional quest icon for journals and cards. |

### Objectives

| Field | Description |
|------|-------------|
| `Objectives` | Ordered list of quest objectives. The list index is used by `CompleteObjective(questId, objectiveIndex)`. |

### Start Conditions

| Field | Description |
|------|-------------|
| `Start Conditions` | `ConditionEntry` list evaluated by `QuestManager` against its `Condition Context`. Empty list means the quest is always available. |

### Optional flow

| Field | Description |
|------|-------------|
| `Next Quest Ids` | Optional quest ids for project-specific chaining logic. The module itself does not auto-activate them. |

## Objective entries

Each objective can contain:

| Field | Description |
|------|-------------|
| `Type` | Objective kind such as `KillCount`, `CollectCount`, `CustomCondition`, `ReachPoint`, or `Talk`. |
| `Target Id` | Matching id for `NotifyKill(...)` or `NotifyCollect(...)`. |
| `Required Count` | Required amount for count-based objectives. |
| `Display Text` | Optional UI-friendly objective text. |
| `Condition` | Optional `ConditionEntry`, mainly for condition-based objectives. |

## Notes

- `KillCount` objectives progress through `NotifyKill(enemyId)`.
- `CollectCount` objectives progress through `NotifyCollect(itemId)`.
- `CustomCondition`, `ReachPoint`, and `Talk` are commonly completed via explicit runtime calls or `QuestNoCodeAction`.
- If `Id` is empty, editor-side helper logic may derive it from the title when the asset is saved.

## Examples

- "Kill 3 goblins": one objective, `Type = KillCount`, `Target Id = Goblin`, `Required Count = 3`.
- "Bring the key after battle": one count objective plus one explicit completion objective triggered from gameplay or inspector flow.
- "Unlock at level 5": add one `Start Condition` against the player context.

## See also

- [README](./README.md)
- [QuestManager](./QuestManager.md)
- [Russian Quest docs](../../Docs/Quest/README.md)
