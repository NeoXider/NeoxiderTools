# ProgressionManager

**What it is:** a `MonoBehaviour` from `Scripts/Progression/Runtime/ProgressionManager.cs` that owns XP, levels, perk points, unlock nodes, perks, and persistent profile storage through `SaveProvider`.

**How to use:**
1. Add `ProgressionManager` to a scene object.
2. Assign `LevelCurveDefinition`, `UnlockTreeDefinition`, and `PerkTreeDefinition`.
3. Set `Save Key` if the project needs a separate profile namespace.
4. Call `AddXp`, `TryUnlockNode`, `TryBuyPerk`, `ResetProgression`, `LoadProfile`, or `SaveProfile`.
5. Bind UI to the reactive state fields or the exposed UnityEvents.

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
    [SerializeField] private int xpReward = 50;
    [SerializeField] private string unlockNodeId = "weapon-tier-2";

    public void GrantXp()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.AddXp(xpReward);
        }
    }

    public void UnlockTier()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.TryUnlockNode(unlockNodeId, out _);
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

### Narrative game

- light XP curve
- unlock tree used as the main content gate
- `ConditionContext` points to the object that owns story flags
