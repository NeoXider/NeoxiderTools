# QuestConfig

**What it is:** a `ScriptableObject` (`Scripts/Quest/QuestConfig.cs`) describing one quest: id, UI text, objectives, start conditions and optional next-quest metadata.

**How to use:**
1. Create a quest via `Right Click > Create > Neoxider > Quest > Quest Config`.
2. Set the `_id` (a unique identifier, e.g., `kill_boars`). An empty id is filled from the title in the Editor.
3. Fill in `_title` and `_description`.
4. Configure the `_objectives` list (see [QuestObjectiveData](QuestObjectiveData.md)).
5. If needed, specify `_startConditions` (e.g., "Player must be level 5").
6. Register the asset in `QuestManager` **Known Quests** and check it with `ValidateDefinition()`.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_id` | Unique quest ID. Used in code for accepting/saving. |
| `_title`, `_description` | Name and detailed description for the UI. |
| `_icon` | (Optional) Quest sprite. |
| `_objectives` | A list of tasks that must be completed to finish the quest. |
| `_startConditions` | A list of conditions (NeoCondition) checked before giving the quest. All must pass. |
| `_nextQuestIds` | Optional next-quest metadata for UI/game code; no automatic acceptance or unlock behavior. |

Use unique, non-empty IDs and define at least one objective. Keep objective indices stable in released content; appending objectives is supported during load, while reordering requires your own save migration. Start conditions are evaluated against `QuestManager.ConditionContext` (or the manager's GameObject if unassigned); avoid scene object references in reusable quest assets.

## Methods

| Method | Description |
|--------|-------------|
| `IReadOnlyList<string> ValidateDefinition()` | Returns human-readable authoring issues: empty or whitespace-only id, no objectives, null objectives, and `KillCount` / `CollectCount` / `ReachPoint` / `Talk` objectives without a `TargetId`. An empty list means valid. |

```csharp
foreach (string issue in questConfig.ValidateDefinition())
{
    Debug.LogWarning($"{questConfig.name}: {issue}", questConfig);
}
```

## See Also

- [QuestFlowConfig](QuestFlowConfig.md) - For linking quests into chains.
- [QuestManager](QuestManager.md) - Main manager.
- [Module Root](README.md)
