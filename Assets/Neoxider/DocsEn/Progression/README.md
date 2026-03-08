# Progression

**What it is:** a meta-progression module for `XP`, levels, unlock trees, perk trees, and a persistent player profile. Scripts live in `Scripts/Progression/`.

**Contents:**
- [ProgressionManager](./ProgressionManager.md) — main runtime entry point and save/profile coordinator.
- [ProgressionNoCodeAction](./ProgressionNoCodeAction.md) — UnityEvent bridge for no-code flows.
- [ProgressionConditionAdapter](./ProgressionConditionAdapter.md) — progression checks for `NeoCondition` and other evaluator-based systems.

**Navigation:** [← DocsEn](../README.md)

---

## How to use

1. Create `LevelCurveDefinition`, `UnlockTreeDefinition`, and `PerkTreeDefinition` assets from the `Neoxider/Progression` menu.
2. Add `ProgressionManager` to a scene object and assign the definitions.
3. Configure `Save Key` when the project needs a custom or separate profile.
4. Bind UI to `XpState`, `LevelState`, `PerkPointsState`, `XpToNextLevelState`, or the manager UnityEvents.
5. Trigger `AddXp`, `TryUnlockNode`, and `TryBuyPerk` from code or via `ProgressionNoCodeAction`.
6. Use `ProgressionConditionAdapter` for no-code gating rules.

## Module scope

- `ProgressionManager` manages loading, saving, level math, node unlocks, and perk purchases.
- `LevelCurveDefinition` stores XP thresholds, perk point grants, and level rewards.
- `UnlockTreeDefinition` stores unlock nodes, prerequisites, conditions, and rewards.
- `PerkTreeDefinition` stores purchasable perks, costs, dependencies, and rewards.
- `ProgressionReward` dispatches rewards into `Money`, `Collection`, `Quest`, XP, and perk points.

## Game scenarios

### Arcade / Hypercasual

Use a short XP curve, a very small unlock tree, and optional perks for lightweight meta retention.

Recommended setup:
- 5-15 levels
- quick XP thresholds
- rewards focused on `Money` and cosmetic unlocks
- minimal prerequisite chains

### RPG / Action RPG

Use the full stack: level curve, unlock tree, and perk tree.

Recommended setup:
- long level curve with regular perk point grants
- unlock nodes for specializations or systems
- perk tree as the main build layer
- optional quest rewards through `ProgressionReward`

### Strategy / Base Builder

Use unlock nodes as tech or building gates and perks as faction-wide passive bonuses.

Recommended setup:
- unlock tree for research branches
- perks for commander or empire bonuses
- level rewards for economy boosts and content access

### Narrative / Adventure

Use progression mainly as a content gate instead of a combat stat layer.

Recommended setup:
- light XP usage
- unlock tree for chapters, locations, and story flags
- `ConditionEntry` for story requirements

### Roguelite / Meta Run

Use `Progression V2` for permanent profile upgrades between runs, not for run-local state.

Recommended setup:
- account-wide XP
- permanent perks and unlocks
- separate run-state save outside this module

## Recommended configuration

### LevelCurveDefinition

- Keep level 1 at `0 XP`.
- For short games, use frequent rewards and compact thresholds.
- For longer games, slow down perk point grants after the early levels.

### UnlockTreeDefinition

- Reserve `UnlockedByDefault` for baseline nodes only.
- Use prerequisite nodes for structure, not for unnecessary depth.
- Add `ConditionEntry` only when the unlock depends on external game state.

### PerkTreeDefinition

- Scale `Cost` slower than XP growth so the tree keeps moving.
- Use `RequiredUnlockNodeIds` to gate specializations or branches.
- Keep the first release of the perk tree small and readable.

## Usage examples

### Grant XP from code

```csharp
using Neo.Progression;

public class EnemyRewardExample : MonoBehaviour
{
    [SerializeField] private int xpReward = 25;

    public void GrantReward()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.AddXp(xpReward);
        }
    }
}
```

### Unlock a node from a scripted event

```csharp
using Neo.Progression;

public class QuestUnlockExample : MonoBehaviour
{
    [SerializeField] private string nodeId = "chapter-2";

    public void UnlockChapter()
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        ProgressionManager.Instance.TryUnlockNode(nodeId, out _);
    }
}
```

### No-code flow

1. Add `ProgressionNoCodeAction`.
2. Set `Action Type = AddXp`.
3. Set `XP Amount = 100`.
4. Bind `Execute()` to a `Button`, quest event, or any UnityEvent source.
