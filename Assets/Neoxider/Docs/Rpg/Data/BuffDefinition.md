# BuffDefinition

**Purpose:** reusable ScriptableObject configuration for temporary stat/resource modifiers.

## Setup

- Create an asset via `Create -> Neoxider -> RPG -> Buff Definition`, author its modifiers, then call
  `RpgCharacter.ApplyBuff(definition)` or configure `ApplyBuffById` through `RpgNoCodeAction`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `DisplayName` | Display Name. |
| `Duration` | Duration. |
| `Id` | Id. |
| `MaxStacks` | Max Stacks. |
| `Modifiers` | Modifiers. |
| `Stackable` | Stackable. |
| `_displayName` | Display Name. |
| `_id` | Id. |
| `_modifiers` | Modifiers. |
| `_stackable` | Stackable. |

## Modifier consumers

`HpRegenPerSecond` is a legacy alias for canonical `"HP"` regen, in units per second. It ignores
`TargetId`, applies clamped active stacks and adds alongside universal `RegenFlat` before `RegenPercent`:
base 2 + alias 3 × 2 stacks + flat 1, with +50%, produces **13.5 HP/s**. It never enables disabled regen,
selects a mode, bypasses alive/full/pause gates or affects Mana.

`AddResourceCurrentFlat` is a signed raw resource grant per newly accepted effective stack. It first
refreshes derived limits, calls Increase/Decrease for the captured ID and finite signed amount, then emits
settled `OnBuffAppliedEvent`. Starting at 90/100, +50 Max and +40 Current results in **130/150**, visible
to the applied observer. Damage/defense/healing multipliers do not modify it. Unknown IDs and nonfinite
amounts are skipped. No pools are created implicitly.

Nonstack refresh and refresh at the stack cap remain accepted and emit one applied event, but grant
nothing. Removal, expiry and profile/load/network restore grant nothing and do not undo prior grants.
SO and `InlineBuffEntry` use the same path. Reentrant buff application during that path is rejected;
runtime replacement aborts its stale continuation. Exceptions propagate and release guards; committed
stacks/grants are not rolled back or replayed on refresh, including when an observer throws before
the remaining captured grants are delivered.

`AttackSpeedPercent` remains serialized enum data with **no runtime consumer**. It does not alter
cooldown, cast delay or a mandatory `IRpgCombatReceiver` contract. Choose an explicit attack timing policy
in gameplay code; a shared consumer is deferred.

По-русски: `AddResourceCurrentFlat` выдаётся только за новую запись или добавленный эффективный стек,
после пересчёта лимитов. Загрузка и обновление уже максимального стека не повторяют выдачу.

## See Also

- [Module Root](../README.md)
