# HealthComponent

**Purpose:** MonoBehaviour that manages one or more resource pools (HP, Mana, or any custom id): current/max, regen, discrete heal, per-operation limits, reactive states and events. Implements `IResourcePoolProvider`. Namespace `Neo.Core.Resources`.

## Setup

- Add Component → **Neoxider/Core/Health Component**.
- Configure the **Pools** list: each entry has an `id`, `current`, `max`, optional `regenPerSecond`/`regenInterval`, discrete `healAmount`/`healDelay`, per-op limits `maxDecreaseAmount`/`maxIncreaseAmount` (-1 = no limit), `restoreOnAwake`, and `ignoreCanHeal`.
- Optional persistence: set `_saveKey`, and toggle `_loadOnAwake` / `_autoSave` (auto-saves on disable).

## Read-only accessors (for NeoCondition)

These are C# properties, not inspector fields — bind them from conditions/logic:

| Property | Description |
|----------|-------------|
| `HpCurrentValue` | Current HP. |
| `HpMaxValue` | Max HP. |
| `HpPercentValue` | HP fraction 0–1. |
| `ManaCurrentValue` | Current mana. |
| `ManaMaxValue` | Max mana. |
| `ManaPercentValue` | Mana fraction 0–1. |

## Methods (IResourcePoolProvider)

| Signature | Returns | Description |
|-----------|---------|-------------|
| `GetCurrent(id)` | float | Current pool value. |
| `GetMax(id)` | float | Pool maximum. |
| `IsDepleted(id)` | bool | true when current ≤ 0. |
| `TrySpend(id, amount, out failReason)` | bool | Deducts only if enough; false + reason otherwise. |
| `Decrease(id, amount)` | float | Reduces the pool; returns the amount actually removed. |
| `Increase(id, amount)` | float | Raises the pool; returns the amount actually added. |
| `Restore(id)` | void | Fills the pool to its max. |
| `SetMax(id, max)` | void | Sets the pool maximum (clamps current down if needed). |
| `SetMaxHp(max)` | void | Convenience alias for `SetMax(RpgResourceId.Hp, max)`. |
| `Save()` / `Load()` | void | Persist / restore pool current+max via `_saveKey`. |

Id constants: `RpgResourceId.Hp`, `RpgResourceId.Mana`.

## Per-pool state and events (ResourceEntryInspector)

- Reactive states (bind UI or subscribe via `.OnChanged`): `CurrentState`, `PercentState`, `MaxState` (all `ReactivePropertyFloat`).
- Events: `OnChanged(current, max)`, `OnDamage(actual)`, `OnHeal(actual)`, `OnDeath`, `OnChangeMax(max)`.

## Component-level event

- `OnPoolsChanged` — fires when the pool list is rebuilt (init / pool set changes).

## Runtime Contract

- `Decrease` raises `OnDamage` for the actual amount removed and raises `OnDeath` exactly once when a pool crosses from `> 0` to `<= 0`. `OnDeath` works for any pool, not only HP.
- `Increase` does **not** heal a pool already at zero unless that pool has `ignoreCanHeal` enabled (dead-can't-regen semantics; enable it for regenerating mana/energy pools).
- Regen keeps its sub-interval remainder across long frames, so frame hitches never lose accumulated regen time.
- `ResourcePoolModel` is the pure C# core; `HealthComponent` syncs inspector pools, UnityEvents, and reactive states around it.

## See Also

- [Module Root](../../README.md)
