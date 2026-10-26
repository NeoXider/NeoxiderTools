# StatusEffectDefinition

**Purpose:** reusable ScriptableObject for status damage, movement multiplier and action blocking.

## Setup

- Create an asset via `Create -> Neoxider -> RPG -> Status Effect Definition`, then call
  `RpgCharacter.ApplyStatus(definition)` or author an `ApplyStatus` NoCode action.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `BlocksActions` | Blocks Actions. |
| `DisplayName` | Display Name. |
| `Duration` | Duration. |
| `Id` | Id. |
| `MaxStacks` | Max Stacks. |
| `MovementSpeedMultiplier` | Movement Speed Multiplier. |
| `Stackable` | Stackable. |
| `TickDamagePerSecond` | Tick Damage Per Second. |
| `TickInterval` | Tick Interval. |
| `_blocksActions` | Blocks Actions. |
| `_displayName` | Display Name. |
| `_id` | Id. |
| `_stackable` | Stackable. |
| `_tickDamagePerSecond` | Tick Damage Per Second. |

## Damage timing

`RpgEffectShelf.Tick` delivers at most **1024 logical damage ticks per status entry per call**. Excess whole
intervals are discarded while fractional phase is retained. Damage per delivery is finite
`TickDamagePerSecond × TickInterval × stacks`; nonfinite delta/interval or overflowed amount produces no
damage callback. Ordinary cadence and finite negative delayed phase are retained. A nonpositive configured
interval still uses the existing one-second fallback.

UTC expiry remains independent of supplied delta time. Buff expiry precedes statuses; status damage
precedes its due expiry callback. Clearing/removing/applying an effect from a callback stops the old shelf
pass. Nested calls do not replay pending ticks; thrown callbacks release the guard.
See [runtime contracts](../InternalTypes.md).

## See Also

- [Module Root](../README.md)
