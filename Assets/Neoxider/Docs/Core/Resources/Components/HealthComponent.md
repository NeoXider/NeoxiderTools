# HealthComponent

**Purpose:** MonoBehaviour that manages one or more resource pools (HP, Mana, or any custom id): current/max, regen, discrete heal, per-operation limits, reactive states and events. Implements `IResourcePoolProvider`. Namespace `Neo.Core.Resources`.

## Setup

- Add Component → **Neoxider/Core/Health Component**.
- Configure the **Pools** list: each entry has an `id`, `current`, `max`, optional `regenPerSecond`/`regenInterval`, discrete `healAmount`/`healDelay`, per-op limits `maxDecreaseAmount`/`maxIncreaseAmount` (-1 = no limit), `restoreOnAwake`, and `ignoreCanHeal`.
- Optional persistence: set `_saveKey`, and toggle `_loadOnAwake` / `_autoSave` (auto-saves on disable).
- Use unique, nonblank ids; matching is case-insensitive. The list and its numeric fields are authoring configuration, read once during initialization. Use the runtime methods below to change live values.
- Null entries and blank ids are ignored; a null list becomes an empty pool set. Missing reactive objects are recreated during initialization. These guards do not validate arbitrary numeric configuration: keep values finite and respect the Inspector's minimums.

## C# workflow

Given an existing `HealthComponent health` reference:

```csharp
using Neo.Core.Resources;

health.EnsureInitialized(); // Optional: public methods and HP/Mana accessors initialize lazily.
IResourcePoolProvider resources = health;
float damage = resources.Decrease(RpgResourceId.Hp, 15f);
float healing = resources.Increase(RpgResourceId.Hp, 5f);
float hpFraction = health.HpPercentValue;
```

For custom ids, use `GetCurrent`, `GetMax`, `Decrease`, and `Increase` through the same interface. `ResourcePoolModel` is available for plain C# consumers that do not need a scene component.

## Scene / NoCode workflow

Connect a damage source's `UnityEvent<float>` to `HealthComponent.Damage`, and a healing source to `HealthComponent.Heal`. Both are one-argument `void` methods available in the UnityEvent dropdown and operate on HP. For a health bar, bind the HP entry's `PercentState.OnChanged` to a normalized UI value such as `Slider.value`; use `CurrentState` and `MaxState` for numeric displays. Per-pool `OnDamage`, `OnHeal`, and `OnDeath` can trigger scene effects for custom pools as well as HP.

Use `OnPoolsChanged` to query the complete initial state or perform an initial UI refresh. Code subscribers to a `ResourceEntryInspector` should prefer `CurrentState.Subscribe(handler, invokeImmediately: true)` and dispose the returned subscription when its owner is disabled or destroyed. The component does not currently expose a public lookup for inspector entries; those entries are configured in its private serialized list.

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
| `Damage(amount)` / `Heal(amount)` | void | UnityEvent-callable HP convenience methods. |
| `EnsureInitialized()` | void | Builds and optionally loads the model once; safe before `Awake` and from initialization callbacks. |
| `Save()` / `Load()` | void | Persist / restore pool current+max via `_saveKey`. |

Id constants: `RpgResourceId.Hp`, `RpgResourceId.Mana`.

## Per-pool state and events (ResourceEntryInspector)

- Reactive states (bind UI or subscribe via `.OnChanged`): `CurrentState`, `PercentState`, `MaxState` (all `ReactivePropertyFloat`).
- Events: `OnChanged(current, max)`, `OnDamage(actual)`, `OnHeal(actual)`, `OnDeath`, `OnChangeMax(max)`.
- Initialization publishes one initial notification for each reactive state. Later operations notify only states whose value changed; unchanged maxima and zero-effect operations do not repeatedly refresh UI.
- All pools' mirrors, including saved values and missing reactive objects, are prepared before the first initial notification. An HP listener can immediately read Mana or another pool through its convenience accessors or reactive state. Initial notifications follow pool-list order, with current, percent, then max for each pool, followed by `OnPoolsChanged`.
- Changes made from an initialization listener take effect immediately and retain their normal change notifications. A later pool's initial notification reads its latest model value, including spending performed by an earlier pool's listener.
- All three reactive values for a pool are assigned before its reactive listeners run, and before `OnChanged(current, max)`. A listener can read the same pool's current/max/percent values immediately.
- `OnChangeMax` reports the maximum accepted by the model (for example, `SetMaxHp(-5)` reports `0`). It is emitted by `SetMax` / `SetMaxHp`; loading uses model change notifications instead.

## Component-level event

- `OnPoolsChanged` fires once after initialization, optional auto-load, event subscriptions, and initial reactive synchronization. Its listeners can call the public API without rebuilding the model. Editing the serialized list at runtime does not rebuild it automatically; there is no public add/remove/rebuild API.

## Persistence and lifecycle

`Awake` initializes once; `Update` advances regeneration and discrete healing with scaled `Time.deltaTime`. Disabled components stop ticking, retain their model, and resume on enable. If `_autoSave` and a nonblank `_saveKey` are set, `OnDisable` saves the initialized model. `OnDestroy` detaches the component's model listeners. Choose a separate save key for each independently persisted component.

`Save` writes current/max for configured valid ids through `Neo.Save.SaveProvider`. `Load` restores known pools without applying damage/healing limits or emitting `OnDamage`, `OnHeal`, or `OnDeath`; it preserves each pool's authoring rules. Unknown saved ids do not create pools. Initial auto-load completes before the initial reactive notifications and `OnPoolsChanged`; a later explicit `Load` synchronizes live reactive values and emits model `OnChanged` notifications. It applies maximum and current separately, so observers of an explicit load can see intermediate values.

## Runtime Contract

- `Decrease` raises `OnDamage` for the actual amount removed and raises `OnDeath` exactly once when a pool crosses from `> 0` to `<= 0`. `OnDeath` works for any pool, not only HP.
- `Increase` does **not** heal a pool already at zero unless that pool has `ignoreCanHeal` enabled (dead-can't-regen semantics; enable it for regenerating mana/energy pools).
- Regen keeps its sub-interval remainder across long frames, so frame hitches never lose accumulated regen time.
- `ResourcePoolModel` is the pure C# core; `HealthComponent` syncs inspector pools, UnityEvents, and reactive states around it.

## See Also

- [Module Root](../../README.md)
