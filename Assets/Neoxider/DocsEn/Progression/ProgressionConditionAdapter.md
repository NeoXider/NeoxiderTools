# ProgressionConditionAdapter

`ProgressionConditionAdapter` is a bridge component that exposes progression state as an evaluatable condition for `NeoCondition` and other systems using an evaluator-style workflow. File: `Assets/Neoxider/Scripts/Progression/Bridge/ProgressionConditionAdapter.cs`.

## Typical use

1. Add `ProgressionConditionAdapter` to a scene object.
2. Assign `ProgressionManager`, or leave it empty when singleton access is expected.
3. Choose an evaluation mode.
4. Fill `Node Id`, `Perk Id`, or `Threshold`.
5. Use it from `NeoCondition` or call evaluation methods directly.

## Evaluation modes

| Mode | What it checks |
|------|----------------|
| `HasUnlockedNode` | Whether a node is unlocked |
| `HasPurchasedPerk` | Whether a perk is purchased |
| `LevelAtLeast` | Whether the player level is at least the threshold |
| `XpAtLeast` | Whether XP is at least the threshold |
| `PerkPointsAtLeast` | Whether enough perk points are available |

## Main fields

| Field | Purpose |
|------|---------|
| `_manager` | Explicit `ProgressionManager` reference |
| `_mode` | Selected evaluation mode |
| `_nodeId` | Node identifier |
| `_perkId` | Perk identifier |
| `_threshold` | Numeric threshold |
| `_invert` | Inverts the result |

## Typical scenarios

- Locking or unlocking UI by level
- Gating perk buttons behind prerequisite unlock nodes
- Reusing existing `Condition`-based flows with progression state

## See also

- [README](./README.md)
- [ProgressionManager](./ProgressionManager.md)
- [Russian Progression docs](../../Docs/Progression/README.md)
# ProgressionConditionAdapter

**What it is:** a `MonoBehaviour` adapter from `Scripts/Progression/Bridge/ProgressionConditionAdapter.cs` that exposes progression checks to `NeoCondition` and other systems using `IConditionEvaluator`.

**How to use:**
1. Add `ProgressionConditionAdapter` to a scene object.
2. Assign `ProgressionManager`, or leave it empty to use the singleton.
3. Choose the `Evaluation Mode`.
4. Fill `Node Id`, `Perk Id`, or `Threshold`.
5. Call `EvaluateCurrent()` directly or reference the adapter from another no-code flow.

**Navigation:** [← Progression](./README.md)

---

## Evaluation modes

| Evaluation Mode | Meaning |
|----------------|---------|
| `HasUnlockedNode` | Checks whether the node is unlocked |
| `HasPurchasedPerk` | Checks whether the perk is purchased |
| `LevelAtLeast` | Checks whether the current level is at least `Threshold` |
| `XpAtLeast` | Checks whether total XP is at least `Threshold` |
| `PerkPointsAtLeast` | Checks whether available perk points are at least `Threshold` |
