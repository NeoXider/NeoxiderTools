# ProgressionConditionAdapter

`ProgressionConditionAdapter` is a scene component implementing `IConditionEvaluator` for progression gates. Source: `Scripts/Progression/Bridge/ProgressionConditionAdapter.cs`.

## Inspector workflow

1. Add the adapter to a scene object.
2. Assign `Manager`, or supply a context GameObject containing a manager; the singleton is the final fallback.
3. Choose a mode and fill `Node Id`, `Perk Id`, or `Threshold`.
4. Optionally enable `Invert` and reference the adapter from an evaluator-based flow.

| Mode | Check |
|------|-------|
| `HasUnlockedNode` | The node identifier has been unlocked. |
| `HasPurchasedPerk` | The perk identifier has been purchased. |
| `LevelAtLeast` | Current Core level is at least `Threshold`. |
| `XpAtLeast` | Total Core XP is at least `Threshold`. |
| `PerkPointsAtLeast` | Available perk points are at least `Threshold`. |

## Runtime API

`Evaluate(GameObject context)` initializes the resolved manager before reading its profile, applies inversion, and stores the returned value in `LastResult`. `EvaluateCurrent()` uses the adapter's GameObject as context. Without a manager the base result is false; `Invert` still applies and `LastResult` matches the returned result.

Code can call the typed manager APIs directly, for example `manager.HasPurchasedPerk("dash")` or `manager.CurrentLevel >= 5`. Keep this adapter on scene objects; reusable ScriptableObjects should not reference it or another scene component.

[Progression](./README.md) | [ProgressionManager](./ProgressionManager.md)
