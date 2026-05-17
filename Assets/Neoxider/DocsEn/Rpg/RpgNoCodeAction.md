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
| `SetMaxResource` / `AddMaxResource` | Changes the max value of any resource through `_resource` (`HP`, `Mana`, `Stamina`, `Shield`, or a custom ID) |
| `SpendResource` / `RefillResource` / `RestoreResource` | Spends, refills, or fully restores the selected resource |
| `RestoreAllResources` | Restores every resource on the character to max |
| `AddStatBase` / `SetStatBase` | Changes the base value of any stat through `_stat` |
| `AddLevel` / `AddXp` / `AddUpgradePoints` | Drives level, XP, and manual upgrade points |
| `UpgradeStat` | Spends upgrade points on the selected stat when its upgrade rule allows it |
| `ApplyInlineBuff` | Applies an inline buff by `_inlineBuffIndex` |
| `ClearAllBuffs` / `ClearAllStatuses` | Clears all active buffs or statuses |
| `LockInvulnerable` / `UnlockInvulnerable` / `SetInvulnerable` | Controls character invulnerability |

## Events

- `OnSuccess` — when action succeeds.
- `OnFailed` — when action fails (e.g. `ApplyBuff` / `ApplyStatus`).
- `OnResultMessage` — result message (success or error).

## Usage

1. Add `RpgNoCodeAction` to an object.
2. Select `Action Type`.
3. Fill `Amount`, `Level`, `Buff Id`, `Status Id`, `Attack Id`, `_resource`, or `_stat` depending on type.
4. Bind `Execute()` to `Button.onClick` or another UnityEvent.
