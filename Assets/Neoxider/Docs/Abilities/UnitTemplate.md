# UnitTemplate

**What it is:** the ScriptableObject authoring asset for a unit archetype — team, resource pools, base property values, and granted abilities. It is the reusable definition of "what a Mage / Goblin / Turret is" before any modifiers apply.

**How to use:** create the asset via `Assets > Create > Neoxider > Abilities > Unit Template`, configure it, then assign it to an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md). On enable, the behaviour creates a domain unit and calls `ApplyTo` to stamp the template onto it.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Data/UnitTemplate.cs`

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Display Name** | string | Name for the unit. Falls back to the asset name if blank. |
| **Team** | int | Team id. `0` = neutral. Allies share a non-zero id; everyone else is an enemy. |
| **Start Level** | int | Starting `AbilityUnit.Level` (min 1) for per-unit-level [leveled values](./README.md#leveled-values-per-level-scaling). |
| **Resources** | `List<UnitResourceConfig>` | Resource pools. Include a `health` pool for damageable units. Each pool starts full. |
| **Base Properties** | `List<UnitPropertyDefault>` | Base property values before modifiers (id + value). |
| **Abilities** | `List<AbilityDefinition>` | Abilities registered and granted on application. |

**UnitResourceConfig** — `{ ResourceId, Max, RegenPerSecond }`. **UnitPropertyDefault** — `{ PropertyId, Value }`.

Default new templates ship with `health` (max 100), `mana` (max 100, regen 5/s), and base `move_speed = 5`, `attack_damage = 10`.

## Key API

| Member | Description |
|--------|-------------|
| `string DisplayName` | Display name, or the asset name if unset. |
| `TeamId Team` | Team id wrapped as a `TeamId`. |
| `int StartLevel` | Configured start level, clamped to ≥ 1. |
| `IReadOnlyList<UnitResourceConfig> Resources` | Configured pools. |
| `IReadOnlyList<UnitPropertyDefault> BaseProperties` | Configured base properties. |
| `IReadOnlyList<AbilityDefinition> Abilities` | Granted abilities. |
| `void ApplyTo(AbilityUnit unit)` | Sets the level, adds pools (started full), sets base properties, and registers + grants the abilities on the unit. |

## Example

**Inspector (Mage template):** Team = `1`; Resources = `{ health, 120, 0 }`, `{ mana, 100, 0 }`; Base Properties = `{ spell_power, 25 }`, `{ move_speed, 5 }`; Abilities = your `fireball` and `frost_nova` assets.

Assign it to the unit's `AbilityUnitBehaviour` and press Play. To build a unit from a template in code:

```csharp
AbilityUnit mage = system.CreateUnit(mageTemplate.Team, mageTemplate.DisplayName);
mageTemplate.ApplyTo(mage); // pools, base properties, granted abilities
```

## Pitfalls

- **Two regen paths stack.** A pool's `RegenPerSecond` regenerates the pool itself: `ApplyTo` gives such pools a fixed 0.1 s regen interval, and `AbilitySystem.Tick` ticks them. This is a flat baseline modifiers cannot touch. Separately, the `health_regen` / `mana_regen` **properties** are applied per second in the same tick — and, being properties, they can be contributed by modifiers. Use the template field for a flat archetype baseline and the properties for regen that should scale or be buffed. (See design note in the [module README](./README.md#design-notes).)
- **No `health` pool means the unit is not damageable** — `Health`/`MaxHealth` read `0` and the damage pipeline has nothing to subtract.
- Pools start **full** (`Current = Max`). There is no separate starting-value field.
- Abilities are registered into the system as a side effect of `ApplyTo`, so a template can be the only place an ability is registered — but sharing one [AbilityLibrary](./AbilityLibrary.md) across units is usually cleaner.

## See also

- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — the scene component that applies the template
- [AbilityDefinition](./AbilityDefinition.md) — the abilities it grants
- [Core / Resources](../Core/README.md) — the `ResourcePoolModel` the pools build on
- Back: [Abilities module](./README.md)
