# ProgressionConditionAdapter

**What it is:** a scene component implementing `IConditionEvaluator` for progression gates (`Scripts/Progression/Bridge/ProgressionConditionAdapter.cs`). Evaluator-based systems such as `NeoCondition` use it to ask "is this node unlocked / perk bought / level reached / premium owned".

**How to use:**
1. Add the adapter to a scene object.
2. Assign `Manager`, or supply a context GameObject containing a manager; the singleton is the final fallback.
3. Choose a mode and fill `Node Id`, `Perk Id`, or `Threshold`.
4. Optionally enable `Invert` and reference the adapter from an evaluator-based flow.

---

## Modes

| Mode | Check |
|------|-------|
| `HasUnlockedNode` | The node identifier has been unlocked. |
| `HasPurchasedPerk` | The perk identifier has been purchased. |
| `LevelAtLeast` | Current Core level is at least `Threshold`. |
| `XpAtLeast` | Total Core XP is at least `Threshold`. |
| `PerkPointsAtLeast` | Available perk points are at least `Threshold`. |
| `HasPremium` | The profile owns the premium track (`ActivatePremium()` was called). |

Mode values are serialized; new modes are appended.

## Runtime API

`Evaluate(GameObject context)` initializes the resolved manager before reading its profile, applies inversion, and stores the returned value in `LastResult`. `EvaluateCurrent()` uses the adapter's GameObject as context. Without a manager the base result is false; `Invert` still applies and `LastResult` matches the returned result. Evaluation allocates nothing, so polling condition systems can call it every frame.

The configuration is also available from code: `Manager`, `Mode`, `NodeId`, `PerkId`, `Threshold` (clamped to zero) and `Invert`.

Code can call the typed manager APIs directly, for example `manager.HasPurchasedPerk("dash")`, `manager.HasPremium` or `manager.CurrentLevel >= 5`. Keep this adapter on scene objects; reusable ScriptableObjects should not reference it or another scene component.

[Progression](./README.md) | [ProgressionManager](./ProgressionManager.md)
