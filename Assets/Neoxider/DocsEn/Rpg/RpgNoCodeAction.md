# RpgNoCodeAction

**What it is:** a `MonoBehaviour` bridge from `Scripts/Rpg/Bridge/RpgNoCodeAction.cs` for invoking RPG actions without code via UnityEvent.

**Navigation:** [← RPG](./README.md)

---

## Action types

| Type | Description |
|------|-------------|
| `TakeDamage` | Applies damage (uses `_amount`) |
| `Heal` | Heals (uses `_amount`) |
| `SetMaxHp` | Sets maximum HP |
| `SetLevel` | Sets level |
| `ApplyBuff` | Applies buff by `_buffId` |
| `ApplyStatus` | Applies status by `_statusId` |
| `RemoveBuff` | Removes buff |
| `RemoveStatus` | Removes status |
| `UseAttackById` | Uses an attack through `RpgAttackController` and `_attackId` |
| `UsePrimaryAttack` | Uses the first configured attack |
| `UsePresetById` | Uses a preset through `RpgAttackController` and `_presetId` |
| `UsePrimaryPreset` | Uses the first configured preset |
| `StartEvade` | Starts `RpgEvadeController` |
| `ResetProfile` | Resets profile |
| `SaveProfile` | Saves profile |
| `LoadProfile` | Loads profile |

## Events

- `OnSuccess` — when action succeeds.
- `OnFailed` — when action fails (e.g. `ApplyBuff` / `ApplyStatus`).
- `OnResultMessage` — result message (success or error).

## Usage

1. Add `RpgNoCodeAction` to an object.
2. Select `Action Type`.
3. Fill `Amount`, `Level`, `Buff Id`, `Status Id`, or `Attack Id` depending on type.
4. Bind `Execute()` to `Button.onClick` or another UnityEvent.
