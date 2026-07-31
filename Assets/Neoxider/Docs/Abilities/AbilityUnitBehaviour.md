# AbilityUnitBehaviour

**What it is:** the scene presence of one ability unit. On enable it creates a domain `AbilityUnit` in the hub, applies its [UnitTemplate](./UnitTemplate.md), registers itself for world queries, and surfaces gameplay receipts (damage, heal, death, modifier applied/removed, cast) as `UnityEvent`s for UI/VFX wiring.

**How to use:** add it to any GameObject that participates in combat, assign a **Template**, and press Play. Add an [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) alongside it if the unit casts. Read `Unit` for the underlying domain entity.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Components/AbilityUnitBehaviour.cs`
- Component menu: `Neoxider/Abilities/Ability Unit`

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Template** | `UnitTemplate` | Archetype applied on registration (pools, base properties, granted abilities). |
| **Team Override** | int | Team id override. `-1` = use the template team. |

### Unity Events

Each fires only for events targeting this unit.

| Event | Fires when | Passes |
|-------|-----------|--------|
| **On Damaged** `UnityEvent<float>` | This unit takes damage (after mitigation/shields). | HP damage dealt. |
| **On Healed** `UnityEvent<float>` | This unit is healed. | Effective healing. |
| **On Died** `UnityEvent` | This unit's health reaches zero. | — |
| **On Modifier Applied** `UnityEvent<string>` | A modifier is applied to this unit. | Modifier id. |
| **On Modifier Removed** `UnityEvent<string>` | A modifier is removed or expires. | Modifier id. |
| **On Ability Cast** `UnityEvent<string>` | This unit's cast is accepted. | Ability id. |

## Key API

| Member | Description |
|--------|-------------|
| `AbilityUnit Unit` | The underlying domain entity (properties, states, resources, `System`). Null before enable / after disable. |
| `UnitId UnitId` | The unit id, or `UnitId.None`. |
| `UnitTemplate Template` | The assigned template. |
| `float CurrentHealth` / `float MaxHealth` | Health pool readouts. |
| `float HealthNormalized` | `CurrentHealth / MaxHealth` (0 when no health pool). |
| `bool IsAlive` | Alive flag. |
| `void ApplyDamage(float amount)` | Convenience entry that applies `pure` damage from no source, through the full pipeline. |
| `void DebugDamage25()` | Inspector button: applies 25 damage. |
| `On*` properties | Accessors for the UnityEvents above. |

## Example

**Inspector:** assign a Mage **Template**, leave **Team Override** at `-1`. Wire **On Damaged** to a health bar and **On Died** to a death VFX.

**Code (read state, apply raw damage, subscribe):**

```csharp
var unit = GetComponent<AbilityUnitBehaviour>();

// Domain access:
float armor = unit.Unit.GetProperty(AbilityProperties.Armor);
bool stunned = unit.Unit.HasState(AbilityStates.Stunned);

// Fire a test hit:
unit.ApplyDamage(25f);

// Or code-level subscription instead of the UnityEvents:
unit.OnDied.AddListener(() => Debug.Log($"{unit.UnitId} died"));
```

## Pitfalls

- **The unit exists only between `OnEnable` and `OnDisable`.** Disabling destroys the domain unit and clears its modifiers; re-enabling creates a fresh unit with a new id.
- **`ApplyDamage` uses `pure` damage and no source** — good for debugging, but it bypasses armor/resist and awards no kill credit. Route real combat through abilities / `DamageService`.
- **UnityEvents only fire for this unit's events.** For a global stream (all units) subscribe to `System.Events` directly.
- A unit with no `health` pool in its template reports `MaxHealth = 0` and cannot be damaged.

## See also

- [UnitTemplate](./UnitTemplate.md) — what gets applied on enable
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — add it to cast abilities
- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — the hub units register into
- Back: [Abilities module](./README.md)
