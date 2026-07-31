# ProgressionNoCodeAction

`ProgressionNoCodeAction` is a UnityEvent-friendly bridge component for triggering progression actions without writing code. File: `Assets/Neoxider/Scripts/Progression/Bridge/ProgressionNoCodeAction.cs`.

## Typical use

1. Add `ProgressionNoCodeAction` to a scene object.
2. Assign `ProgressionManager`, or leave it empty if the project uses singleton access.
3. Choose an `Action Type`.
4. Fill fields such as XP amount, perk points, node id, or perk id depending on the action.
5. Call `Execute()` from a `Button`, animation event, quest event, or other UnityEvent source.

## Supported actions

| Action Type | Description |
|------------|-------------|
| `AddXp` | Adds XP |
| `GrantPerkPoints` | Adds perk points |
| `UnlockNode` | Attempts to unlock one node by `Node Id` |
| `BuyPerk` | Attempts to buy one perk by `Perk Id` |
| `ResetProgression` | Resets the profile |
| `SaveProfile` | Forces profile save |
| `LoadProfile` | Forces profile load |

## Events

| Event | When it is raised |
|------|-------------------|
| `_onSuccess` | The action succeeded |
| `_onFailed(string)` | The action failed and returns a reason |
| `_onResultMessage(string)` | Unified result message for UI or logging |

## Typical scenarios

- Reward button that grants XP
- Inspector-driven perk purchase button
- Unlock node trigger after mission completion
- Debug scene button for reset/save/load actions

## API for UnityEvent wiring

- `Execute()` — runs the configured `Action Type`.
- `SetNodeId(string)` / `SetPerkId(string)` — 1-arg setters so a UnityEvent can supply the target id before `Execute()`.
- `XpAmount`, `PerkPointsAmount`, `NodeId`, `PerkId`, `ActionType`, `Manager` — public accessors for runtime configuration.

## See also

- [README](./README.md)
- [ProgressionManager](./ProgressionManager.md)
