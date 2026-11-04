# RpgContactDamage

**What it is:** a `MonoBehaviour` (`Scripts/Rpg/Components/Weapons/RpgContactDamage.cs`, namespace `Neo.Rpg`)
that damages one `IRpgCombatReceiver` by proximity on a cooldown. It uses distance checks instead of
physics contacts, so it works with NavMeshAgent-driven enemies.

**How to use:**
1. Add the component to the attacker (menu `Neoxider/RPG/RpgContactDamage`).
2. Pick the target: `targetTag` (searched with `FindGameObjectWithTag`), `targetOverride` (Transform) or
   `targetReceiverOverride` (an explicit receiver component).
3. Set `damage`, `damageRange`, `cooldown` and `damageType`; optionally assign `selfCharacter` so a dead
   attacker stops hitting.
4. Wire `OnTargetFound` / `OnTargetLost` / `OnAttack` / `OnDamageDealt` or bind `IsAttacking` /
   `TargetInRange` to animation and UI.

---

## Fields

| Field | Type | Purpose |
|-------|------|---------|
| `targetTag` | `string` | Tag searched when no override is set. Empty disables the search. |
| `targetOverride` | `Transform` | Explicit target transform; its receiver is resolved on the object, children or parents. |
| `targetReceiverOverride` | `Component` | Explicit `IRpgCombatReceiver`; takes precedence over the other options. |
| `targetFindInterval` | `float` | Seconds between searches while the target is missing or inactive (min 0.1). |
| `damage` | `int` | Damage per hit. |
| `damageRange` | `float` | Distance at which hits are applied. |
| `cooldown` | `float` | Seconds between hits. |
| `damageType` | `string` | Passed in `RpgDamageInfo` for resist modifiers (default `Contact`). |
| `selfCharacter` | `RpgCharacter` | Optional attacker; while it is dead no hits happen. Auto-filled from the same GameObject. |
| `debugLog` | `bool` | Logs found/lost/hit messages. |
| `IsAttacking` | `ReactivePropertyBool` | True after a hit while the target stays valid and in range. |
| `TargetInRange` | `ReactivePropertyBool` | Whether the cached target is within `damageRange`. |

## Methods

| Member | Description |
|--------|-------------|
| `SetTarget(Transform)` | Replaces the target transform and re-caches the receiver. |
| `SetTargetReceiver(Component)` | Replaces the explicit receiver; `null` falls back to transform/tag search. |
| `SetDamage(int)` / `Damage` | Damage per hit (min 1). |
| `SetDamageRange(float)` / `DamageRange` | Hit distance (min 0.1). |
| `SetCooldown(float)` / `Cooldown` | Seconds between hits (min 0.1). |
| `SetDamageType(string)` / `DamageType` | Damage type id for resist modifiers. |
| `CurrentTarget` | Cached target transform or null. |

## Events

| Event | When |
|-------|------|
| `OnTargetFound` | The current target is valid and in range for the first time (again after it was lost or replaced). |
| `OnTargetLost` | A found target is destroyed or deactivated. Raised once per loss, not on every search retry. |
| `OnAttack` | Every hit. |
| `OnDamageDealt(float)` | Every hit, with the HP the receiver reports as removed. |

Inactive targets suspend damage and clear `IsAttacking` / `TargetInRange`. Moving out of range does not
raise `OnTargetLost`; use `TargetInRange` for that. Destroyed receiver components are skipped.

## Example

```csharp
RpgContactDamage bite = enemy.GetComponent<RpgContactDamage>();
bite.SetTarget(player.transform);
bite.SetDamage(8);
bite.SetCooldown(0.75f);
bite.OnTargetFound.AddListener(() => animator.SetBool("Chasing", true));
```

## See Also

- [RpgCharacter damage pipeline](RpgCharacter.md#damage-pipeline)
- [Module Root](README.md)
