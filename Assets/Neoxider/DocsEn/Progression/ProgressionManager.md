# ProgressionManager

**What it is:** a `MonoBehaviour` from `Scripts/Progression/Runtime/ProgressionManager.cs` that owns XP, levels, perk points, unlock nodes, perks, and persistent profile storage through `SaveProvider`.

**How to use:**
1. Add `ProgressionManager`, `LevelComponent` (and optionally `UnlockContext`, `PerkContext`) to a scene object (such as a Player or Weapon).
2. Assign `LevelCurveDefinition`, `UnlockTreeDefinition`, and `PerkTreeDefinition`.
3. Set `Save Key` if the project needs a separate profile namespace (e.g. `Weapon_Sword_Progression`).
4. Call `AddXp`, `TryUnlockNode`, `TryBuyPerk`, `ResetProgression`, `LoadProfile`, or `SaveProfile`.
5. Bind UI to the reactive state fields or the exposed UnityEvents.
6. Support for `Premium` (BattlePass-like) progressions is available via `ActivatePremium()`.

**Navigation:** [← Progression](./README.md)

---

## Main fields

| Field | Purpose |
|------|---------|
| `_levelCurve` | XP thresholds, level rewards, and perk point grants |
| `_unlockTree` | Unlock node graph |
| `_perkTree` | Perk graph |
| `_saveKey` | Persistent profile key |
| `_conditionContext` | Context object for `ConditionEntry` evaluation |

## Reactive state

| Field | Purpose |
|------|---------|
| `XpState` | Current XP |
| `LevelState` | Current resolved level |
| `PerkPointsState` | Current unspent perk points |
| `XpToNextLevelState` | Remaining XP to the next defined level |

## Typical use cases

- Persistent player progression outside scene-local saves.
- Runtime UI for XP bars, level labels, perk counters, and unlock trees.
- No-code gameplay flows driven by progression state.

## Code example

```csharp
using Neo.Progression;
using UnityEngine;

public class ProgressionRewardExample : MonoBehaviour
{
    [SerializeField] private ProgressionManager targetProgression;
    [SerializeField] private int xpReward = 50;
    [SerializeField] private string unlockNodeId = "weapon-tier-2";

    public void GrantXp()
    {
        if (targetProgression != null)
        {
            targetProgression.AddXp(xpReward);
        }
    }

    public void UnlockTier()
    {
        if (targetProgression != null)
        {
            targetProgression.TryUnlockNode(unlockNodeId, out _);
        }
    }
    
    public void PurchaseBattlepass()
    {
        if (targetProgression != null)
        {
            // Activate Premium track retroactively granting rewards for past levels
            targetProgression.ActivatePremium();
        }
    }
}
```

## Common setup patterns

### Minimal metagame

- `LevelCurveDefinition` only
- no unlock tree
- no perk tree
- level rewards focused on currency or cosmetics

### Classic RPG

- `LevelCurveDefinition` + `UnlockTreeDefinition` + `PerkTreeDefinition`
- `GrantedPerkPoints` awarded by level ups
- perks purchased with perk points
- unlock nodes open new branches or systems

### Battle Pass

- `LevelCurveDefinition` with multiple `ProgressionRewards` 
- Some rewards marked as `IsPremiumOnly = true`
- Use `ProgressionManager.ActivatePremium()` when purchased by user
- Independent profiles per season by switching `Save Key`

### Narrative game

- light XP curve
- unlock tree used as the main content gate
- `ConditionContext` points to the object that owns story flags
