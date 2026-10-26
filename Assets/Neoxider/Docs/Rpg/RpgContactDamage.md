# RpgContactDamage

**Purpose:** Applies contact damage to an `IRpgCombatReceiver` within range on a cooldown.

## Setup

- Add the component via the Unity menu and configure damage, range, cooldown, and a target tag.
- Assign `targetOverride` for a Transform or `targetReceiverOverride` for an explicit receiver.
- In C#, use `SetTarget(Transform)`, `SetTargetReceiver(Component)`, and `SetDamage(int)`.
- Inactive targets suspend damage, clear attacking/range state, and raise `OnTargetLost`. Search is
  retried at `targetFindInterval`; clearing a receiver override clears the cached receiver before fallback search.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Damage` | Damage. |
| `DamageRange` | Damage Range. |
| `IsAttacking` | Is Attacking. |
| `OnAttack` | On Attack. |
| `OnDamageDealt` | On Damage Dealt. |
| `OnTargetFound` | On Target Found. |
| `OnTargetLost` | On Target Lost. |
| `TargetInRange` | Target In Range. |
| `_onAttack` | On Attack. |
| `_onDamageDealt` | On Damage Dealt. |
| `_onTargetFound` | On Target Found. |
| `_onTargetLost` | On Target Lost. |
| `damageRange` | Damage Range. |
| `damageType` | Damage Type. |
| `debugLog` | Debug Log. |
| `selfCharacter` | Optional source character; stops contact attacks while that character is dead. |
| `targetOverride` | Target Override. |
| `targetReceiverOverride` | Target Receiver Override. |
| `targetTag` | Target Tag. |

## See Also

- [Module Root](README.md)
