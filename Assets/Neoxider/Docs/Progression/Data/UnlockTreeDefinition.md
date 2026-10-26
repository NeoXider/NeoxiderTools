# UnlockTreeDefinition

`UnlockTreeDefinition` is a reusable ScriptableObject containing `UnlockNodeDefinition` entries. Create it with **Create > Neoxider > Progression > Unlock Tree Definition**, then assign it to `ProgressionManager.UnlockTree`.

| Entry field | Purpose |
|-------------|---------|
| `Id` | Stable, nonempty, case-sensitive identifier persisted in the profile. |
| `DisplayName`, `Description`, `Icon` | Optional presentation data. |
| `UnlockedByDefault` | Applied on load/reset without dispatching node rewards. |
| `RequiredLevel` | Minimum Core level (at least 1); provider-free setups report level 1. |
| `PrerequisiteNodeIds` | Every listed node must already be unlocked. |
| `Conditions` | Additional checks evaluated with the manager's scene `ConditionContext`. |
| `Rewards` | Dispatched once when this node is successfully unlocked in the active profile. |

## Runtime API

`Nodes` exposes authored entries and `TryGetNode(string, out UnlockNodeDefinition)` resolves an exact identifier. Call `ValidateDefinition()` to detect null entries, empty/duplicate identifiers, missing prerequisites, cycles, and reward lists on default nodes.

Use `manager.CanUnlockNode("chapter-2", out string reason)` for availability and `manager.TryUnlockNode("chapter-2", out reason)` to change state. The NoCode equivalent is `ProgressionNoCodeAction` with `UnlockNode` and that identifier; use `ProgressionConditionAdapter` with `HasUnlockedNode` to gate later actions.

Keep identifiers stable when renaming display text. Default-node rewards are intentionally not delivered automatically. Keep conditions and reward targets independent of scene object references inside this asset: use the manager's scene context and reward service fallbacks.

[Progression](../README.md) | [ProgressionManager](../ProgressionManager.md)
