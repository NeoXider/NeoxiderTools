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
| **Level Source** | `Neo.Core.Level.LevelComponent` | Optional scene component: its current level and `OnLevelUp` events drive the domain unit while registered. |

### Unity Events

Each fires only for events targeting this unit.

The wrapper subscribes through `System.Events.SubscribeTarget(UnitId, ...)`, so other units' events do not
enter its receipt handler. It detaches that exact Target registration on disable; a new enable uses the
new domain id. A captured old event still passes the wrapper's current identity check before any UnityEvent.

| Event | Fires when | Passes |
|-------|-----------|--------|
| **On Damaged** `UnityEvent<float>` | This unit takes damage (after mitigation/shields). | HP damage dealt. |
| **On Healed** `UnityEvent<float>` | This unit is healed. | Effective healing. |
| **On Died** `UnityEvent` | This unit's health reaches zero. | — |
| **On Modifier Applied** `UnityEvent<string>` | A modifier is applied to this unit. | Modifier id. |
| **On Modifier Removed** `UnityEvent<string>` | A modifier is removed or expires. | Modifier id. |
| **On Ability Cast** `UnityEvent<string>` | This unit's cast is accepted. | Ability id. |

Damage/heal amounts are the pool operation's accepted delta after caps/headroom, independent of later
resource-callback changes. For example, `ApplyHeal(10)` can accept 10 while a resource listener drains 20:
`OnHealed` reports 10 even though the final health is lower. Read `CurrentHealth` for the final balance.
Death decisions still use live state; this does not make callbacks transactional.

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
| `void ApplyHeal(float amount)` | Honors `healing_received_mul`, never revives, and emits `heal_received` for effective healing. |
| `void SetTemplate(UnitTemplate)` / `SetTeamOverride(int)` | Configure the next registration; set them before enabling a pooled unit. |
| `void SetLevel(int)` | Set the registered domain unit's level (minimum 1). A bound level source can update it again. |
| `void SetLevelSource(LevelComponent)` | Configure the source used on the next enable. The currently subscribed source stays active until disable and is correctly detached then, even after this setting changes. |
| `void EnsureRegistered()` | Idempotent registration for active components; useful when a sibling's `OnEnable` needs the unit before Unity calls this component. |
| `void DebugDamage25()` | Inspector button: applies 25 damage. |
| `On*` properties | Accessors for the UnityEvents above. |

## Example

**Inspector:** assign a Mage **Template**, leave **Team Override** at `-1`. Wire **On Damaged** to a health bar and **On Died** to a death VFX.

**Code (read state, apply raw damage, subscribe):**

```csharp
AbilityUnitBehaviour unit = GetComponent<AbilityUnitBehaviour>();

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
- **`SetLevelSource` is deferred until the next enable.** To switch immediately, disable the unit, assign the source, then enable it. Reassigning or clearing the setting while active does not detach the current source early; disable still removes the listener from the actual subscribed source. Other components' `OnEnable` order is unspecified, so siblings may call `EnsureRegistered()`.
- **`ApplyDamage` uses `pure` damage and no source** — good for debugging, but it bypasses armor/resist and awards no kill credit. Route real combat through abilities / `DamageService`.
- **UnityEvents only fire for this unit's events.** For a global stream use `System.Events.SubscribeAny`; for a code-level unit stream use `SubscribeTarget` and retain/unsubscribe the same delegate. Specific handlers precede the observer snapshot, internal modifier reactions follow its capture, then matching Target/global observers run in registration order. See the [event contract](./README.md#target-observers-and-dispatch-counters).
- A unit with no `health` pool in its template reports `MaxHealth = 0` and cannot be damaged.

## See also

- [UnitTemplate](./UnitTemplate.md) — what gets applied on enable
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — add it to cast abilities
- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — the hub units register into
- Back: [Abilities module](./README.md)
