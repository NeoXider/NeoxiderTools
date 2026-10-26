# PerkTreeDefinition

`PerkTreeDefinition` is a reusable ScriptableObject containing `PerkDefinition` entries. Create it with **Create > Neoxider > Progression > Perk Tree Definition**, then assign it to `ProgressionManager.PerkTree`.

| Entry field | Purpose |
|-------------|---------|
| `Id` | Stable, nonempty, case-sensitive identifier used in saves and APIs. |
| `DisplayName`, `Description`, `Icon` | Optional presentation data. |
| `PurchasedByDefault` | Applied on profile load/reset without charging points or dispatching rewards. |
| `Cost`, `RequiredLevel` | Nonnegative point cost and minimum Core level (at least 1). |
| `PrerequisitePerkIds` | All listed perks must already be purchased. |
| `RequiredUnlockNodeIds` | All listed unlock nodes must already be unlocked. |
| `Conditions` | Additional `ConditionEntry` checks evaluated with the manager's scene context. |
| `Rewards` | Dispatched after a successful purchase, once per perk identifier in the active profile. |

## Runtime API

`Perks` exposes authored entries and `TryGetPerk(string, out PerkDefinition)` resolves an exact identifier. `ValidateDefinition()` reports null/empty/duplicate entries, missing prerequisites, graph cycles, empty unlock references, and rewards on default perks. `ValidateDefinition(UnlockTreeDefinition)` additionally checks that required unlock nodes exist.

Use `manager.CanBuyPerk("dash", out string reason)` for UI gating and `manager.TryBuyPerk("dash", out reason)` for the actual purchase. For Inspector wiring, configure `ProgressionNoCodeAction` with `BuyPerk` and the same identifier. A UI availability check does not reserve points; `TryBuyPerk` checks again.

Validate definitions before shipping, retain identifiers when changing display text, and keep costs/level requirements within the Inspector ranges. Conditions and rewards in these assets must not depend on scene object references; use the manager's context and reward service fallbacks. Default perks with rewards are diagnosed because those rewards are intentionally not delivered automatically.

[Progression](../README.md) | [ProgressionManager](../ProgressionManager.md)
