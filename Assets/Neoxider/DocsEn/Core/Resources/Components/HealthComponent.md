# HealthComponent

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `HpCurrentValue` | Hp Current Value. |
| `HpMaxValue` | Hp Max Value. |
| `HpPercentValue` | Hp Percent Value. |
| `ManaCurrentValue` | Mana Current Value. |
| `ManaMaxValue` | Mana Max Value. |
| `ManaPercentValue` | Mana Percent Value. |
| `OnPoolsChanged` | On Pools Changed. |
| `_loadOnAwake` | Load On Awake. |
| `_onPoolsChanged` | On Pools Changed. |
| `_saveKey` | Save Key. |
| `restoreOnAwake` | Restore On Awake. |
| `true` | True. |

## Runtime Contract

- `Decrease(resourceId, amount)` reduces the selected pool, raises `OnDamage` for actual damage, and raises `OnDeath` exactly once when the resource crosses from `> 0` to `<= 0`.
- `OnDeath` works for any resource pool, not only `HP`, so Mana/Stamina/Shield can have their own depleted events.
- `Increase(resourceId, amount)` does not heal a zero-value resource unless the pool has `ignoreCanHeal` enabled.
- `ResourcePoolModel` remains the pure C# core; `HealthComponent` synchronizes inspector pools, UnityEvents, and reactive states.

## See Also

- [Module Root](../../README.md)
