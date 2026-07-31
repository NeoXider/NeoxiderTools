# AbilityLibrary

**What it is:** a project-level catalog asset that bundles many [AbilityDefinition](./AbilityDefinition.md) and [ModifierDefinition](./ModifierDefinition.md) assets and registers them into a live `AbilitySystem` in one call. Multiple libraries can be registered (base game + DLC + mods).

**How to use:** create the asset via `Assets > Create > Neoxider > Abilities > Ability Library`, drag your ability and modifier assets in, then reference the library from [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) (registered on Awake) or call `RegisterInto(system)` yourself.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Data/AbilityLibrary.cs`

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Abilities** | `List<AbilityDefinition>` | Ability assets registered into the system. |
| **Modifiers** | `List<ModifierDefinition>` | Modifier assets registered into the system. |

## Key API

| Member | Description |
|--------|-------------|
| `IReadOnlyList<AbilityDefinition> Abilities` | The bundled abilities. |
| `IReadOnlyList<ModifierDefinition> Modifiers` | The bundled modifiers. |
| `void RegisterInto(AbilitySystem system)` | Registers every modifier, then every ability, into the system. Null entries are skipped. Modifiers register first so `apply_modifier` nodes resolve. |

Registering only makes the blueprints known to the system; grant abilities to units separately (`AbilitySystem.GrantAbility`, a [UnitTemplate](./UnitTemplate.md), or an [AbilityCasterBehaviour](./AbilityCasterBehaviour.md)).

## Example

**Inspector:** put `fireball`, `frost_nova` into Abilities and `burn`, `frost_slow`, `magic_shield` into Modifiers. Add the library to the scene hub's **Libraries** list.

**Code:**

```csharp
AbilitySystem system = AbilitySystemBehaviour.I.System;
myLibrary.RegisterInto(system);          // now all ids are known
system.GrantAbility(unitId, "fireball"); // grant to a specific unit
```

Add a library at runtime (DLC, generated content):

```csharp
AbilitySystemBehaviour.I.AddLibrary(dlcLibrary);
```

## Pitfalls

- **Registering is not granting.** A unit cannot cast an ability until it has been granted to that unit, even if the library is registered.
- **Re-registering an id overwrites the previous blueprint** (last write wins). Keep ids unique across libraries unless you intend to override.
- Modifiers referenced by `apply_modifier` must be in some registered library (or otherwise registered) or the op silently does nothing.

## See also

- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — registers libraries on Awake
- [AbilityDefinition](./AbilityDefinition.md) / [ModifierDefinition](./ModifierDefinition.md)
- Back: [Abilities module](./README.md)
