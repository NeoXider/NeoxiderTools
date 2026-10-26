# Core module

## Purpose

Core gameplay runtime primitives: levels, XP, and health-style resources. Reference assembly `Neo.Core`
for the plain C# models and interfaces. Domain logic has no UnityEngine dependency; serializable profile
DTOs and `UnityEventFloatFloat` use Unity types. Scene components, curve assets, and NoCode bridges live
in separate assemblies and are optional.

## Entry points

- [Level](./Level.md) — level/XP component and scene setup
- [LevelCurveDefinition](./LevelCurveDefinition.md) — level curve formulas and curve/script configuration
- [Level bridges](./Level/Bridge/README.md) — optional condition and UnityEvent adapters over the level API
- [HealthComponent](./Resources/Components/HealthComponent.md) — runtime health/mana-style resource component
- [Diagnostics](./Diagnostics.md) — shared runtime logging gate for package modules

## Levels from C#

```csharp
using Neo.Core.Level;

LevelModel level = new LevelModel();
level.SetCurve(LevelCurveType.Linear, xpPerLevel: 100);
level.SetMaxLevel(50);
level.OnLevelChanged += (previous, current) => UpdateLevelLabel(current);
level.OnXpGained += (added, total) => UpdateXpLabel(total);
level.AddXp(250); // CurrentLevel = 3, TotalXp = 250, XpToNextLevel = 50
level.SetLevel(5); // TotalXp = 400: synchronize to the first XP threshold for level 5
```

`LevelModel` starts at level 1, zero XP, and 100 XP to the next level. `SetState(totalXp, level)` loads
state without gameplay events; with XP enabled it derives the level from XP. `SetUseXp(false)` clears
the next-level counter and enables direct levels; switching back immediately recalculates from stored XP.
`SetLevelDirect` bypasses XP synchronization; a later XP recalculation uses the curve again.

`SetLevel` searches the complete nonnegative `int` XP range. If a requested level cannot be reached,
it selects the highest reachable level and its first XP threshold. `AddXp` ignores nonpositive amounts
and saturates stored XP at `int.MaxValue`; `OnXpGained(added, total)` reports the amount actually stored.
Further additions at that ceiling emit no XP event. Levels also saturate at `int.MaxValue`.

`AddXp` captures its resulting level before `OnXpGained` callbacks, then delivers `OnLevelChanged`
with that operation's previous/resulting levels. XP callbacks may add XP or reset state synchronously.
For two nested 100-XP additions from zero, the nested level event is `(2, 3)`, followed by the outer
event `(1, 2)`; the live level is already 3. A callback reset preserves the captured transition and
cannot create a level event for an addition that did not itself change the level.

Use `SetCurveDefinition(ILevelCurveDefinition)` for custom curve implementations, including the optional
`LevelCurveDefinition` asset. Implementations must return a nondecreasing level as XP increases, return
at least 1, and honor a positive max-level cap so that the model can invert the curve in `SetLevel`.
`SetCurve` clears the definition and selects the built-in curve. A zero max level means no configured cap.

`LevelCurveEvaluator` exposes both `LevelCurveType` and `LevelFormulaType` calculations without storing
state. Formula thresholds use `(level - 1)`, round fractional XP requirements upward, and calculate the
remaining XP before converting to `int`. Negative total XP is treated as zero; remaining XP beyond the
integer range saturates at `int.MaxValue`. A level that needs total XP above `int.MaxValue` remains
unreachable by `LevelModel`. Custom entries can include nulls or repeated levels: the next-level counter
uses the earliest future XP threshold that actually raises the level.

## Resource pools from C#

```csharp
using Neo.Core.Resources;

ResourcePoolModel resources = new ResourcePoolModel();
resources.AddPool(RpgResourceId.Mana, new ResourcePoolEntry
{
    Current = 50f,
    Max = 100f,
    MaxDecreaseAmount = -1f,
    MaxIncreaseAmount = -1f,
    RegenPerSecond = 5f,
    RegenInterval = 1f,
    IgnoreCanHeal = true
});

if (resources.TrySpend(RpgResourceId.Mana, 20f, out string reason))
    CastSpell();

resources.Tick(deltaTime); // Call once per update from the owning component/service.
```

Resource ids are case-insensitive. Set per-operation limits explicitly: negative means unlimited,
zero blocks the operation. `TrySpend` requires the balance and decrease limit to cover the complete
positive cost, then subtracts it from the stored float. Failure returns false with a reason and leaves
state unchanged, including when the decrease limit is below the cost. `Decrease` and
`Increase` allow partial changes and return the actual delta applied by that operation, excluding
changes made by event listeners. `IgnoreCanHeal` allows increasing a depleted pool.

The concrete model also provides `CanSpend(string resourceId, float amount, out string failReason)`.
It checks the same finite-input, id, balance, decrease-limit, and effective stored-decrease rules as
`TrySpend`, including the same failure reasons. It changes no values, timers, or events. For example,
`resources.CanSpend(RpgResourceId.Mana, 20f, out string failure)` checks affordability without paying.
This is a preflight of the current state; call `TrySpend` to pay when executing the action, since
`CanSpend` does not reserve resources. Finite nonpositive amounts succeed even for missing ids.
`IResourcePoolProvider` retains its existing contract; this additional API is on `ResourcePoolModel`.

Balances use `float` (binary32). A positive spend must lower the stored balance: at current
100,000,000, cost 1 rounds to no decrease, so both `CanSpend` and `TrySpend` reject it with
`Amount does not decrease resource.` after the existing input/id/balance/limit checks. Ordinary
fractional costs remain valid when the stored balance decreases; the stored delta need not equal
the requested cost exactly.

`Decrease` clamps the requested amount, then returns previous minus stored new current, captured
before callbacks. At current 100,000,000, decreasing 5 stores 99,999,992 and returns 8; decreasing 1
returns zero and retains its existing Changed notification. A listener's later balance change does
not affect that return value. Requested-amount limits and stored deltas are subject to float rounding.

`Max > 0` caps current value; `Max == 0` means uncapped. `SetMax(0)` preserves the current value.
`SetCurrent` bypasses healing gates for loading/reviving, and `Restore` assigns the maximum value
(zero for an uncapped pool). `AddPool` normalizes invalid current/max values and clamps initial values
to a positive maximum. The supplied `ResourcePoolEntry` remains mutable and shared with the caller;
use model methods for runtime mutations so that invariants and events are maintained.

Missing/null ids read as zero, report depleted, and reject positive spending; mutations are ignored.
Finite nonpositive costs remain successful no-ops. Non-finite operation amounts reject spending or
return zero; non-finite `SetCurrent`/`SetMax` values are ignored.

`Tick` ignores nonpositive/non-finite delta times and recursive calls from its own callbacks. It keeps
fractional timer remainders and applies the increase limit per elapsed regen/heal cycle, including
cycles recovered after a long frame. Recovery is aggregated without looping over every missed cycle.
`OnResourceChanged(id, current, max)` and `OnResourceDepleted(id)` are synchronous. Callbacks can add,
remove, or replace pools: added/replaced entries start ticking on the next call, and an entry removed
during regen cannot apply its old heal configuration to its replacement.
Every successful `AddPool` call creates a fresh registration and resets both timers, including when
the same mutable entry is registered again with the same id or different id casing. `Tick` checks
the captured registration before regen and before heal, so a callback's new registration receives
no elapsed time from the old snapshot. Its timers begin advancing on the next `Tick`.
`Decrease` and `TrySpend` capture their own positive-to-zero transition before notifying listeners,
then deliver Changed before Depleted. Nested operations cannot duplicate or erase that operation's
depletion event; a listener may already have revived the pool when its depletion notification arrives.

## Persistence and scene workflows

`LevelProfileData` stores XP, level, max level, and a version. `ResourceProfileData` stores resource ids,
current values, and maxima. Call `Sanitize` after deserialization: resource sanitation removes empty
entries/ids, repairs non-finite values, and preserves uncapped pools. `Clone` makes independent data
copies; resource clones skip null entries and do not mutate the source. Persistence transport belongs
to the consuming component or save service.

For scene authoring, use [LevelComponent](./Level/Components/LevelComponent.md) with a curve asset, or
[HealthComponent](./Resources/Components/HealthComponent.md) with per-pool settings and events.
For Inspector workflows, [LevelNoCodeAction](./Level/Bridge/LevelNoCodeAction.md) forwards typed level/XP
operations and [LevelConditionAdapter](./Level/Bridge/LevelConditionAdapter.md) exposes conditions.
Game code can consume `ILevelProvider` / `IResourcePoolProvider` through explicit references without
using NoCode components or searching the scene.

