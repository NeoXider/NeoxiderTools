# RpgStatsManager

**What it is:** a persistent `MonoBehaviour` player-profile manager for HP, level, buffs, statuses, invulnerability locks, and `SaveProvider` storage.

**How to use:**
1. Add `RpgStatsManager` to a scene object.
2. Assign `BuffDefinition` and `StatusEffectDefinition` arrays.
3. Set `Save Key` if the project needs a separate profile namespace.
4. Enable `Auto Save` when runtime mutations should be written automatically. It is off by default.
5. Combine it with `RpgAttackController` and `RpgEvadeController` when the player can attack or dodge.
6. Call `TakeDamage`, `Heal`, `TryApplyBuff`, `TryApplyStatus`, `ResetProfile`, `LoadProfile`, or `SaveProfile`.
7. Bind UI to the reactive state fields or the exposed UnityEvents.

**Navigation:** [← RPG](./README.md)

---

## Main fields

| Field | Purpose |
|------|---------|
| `_buffDefinitions` | Buff definitions |
| `_statusDefinitions` | Status effect definitions |
| `_saveKey` | Persistent profile key |
| `_hpRegenPerSecond` | HP regeneration per second |
| `_regenInterval` | Interval for regen and status tick processing |

## Reactive state

| Field | Purpose |
|------|---------|
| `HpState` | Current HP |
| `HpPercentState` | HP as percentage (0..1) |
| `LevelState` | Current level |

## Main API

| Method | Description |
|--------|-------------|
| `TakeDamage(float amount)` | Applies damage, returns actual damage dealt |
| `Heal(float amount)` | Restores HP, returns actual amount healed |
| `SetMaxHp(float maxHp, bool clampCurrent)` | Sets maximum HP |
| `SetLevel(int level)` | Sets level |
| `TryApplyBuff(string buffId, out string failReason)` | Applies a buff |
| `TryApplyStatus(string statusId, out string failReason)` | Applies a status effect |
| `RemoveBuff(string buffId)` | Removes a buff |
| `RemoveStatus(string statusId)` | Removes a status effect |
| `HasBuff(string buffId)` | Checks if buff is active |
| `HasStatus(string statusId)` | Checks if status is active |
| `ResetProfile()` | Resets profile to defaults |
| `LoadProfile()` / `SaveProfile()` | Loads or saves profile |

## Events

- `OnDamaged` — when damage is taken (float = actual damage).
- `OnHealed` — when healed (float = actual heal amount).
- `OnDeath` — when HP reaches zero.
- `OnBuffApplied` / `OnBuffExpired` — when buff is applied or expires.
- `OnStatusApplied` / `OnStatusExpired` — when status is applied or expires.

## Code example

```csharp
using Neo.Rpg;
using UnityEngine;

public class EnemyDamageExample : MonoBehaviour
{
    [SerializeField] private float damageAmount = 25f;

    public void DealDamage()
    {
        if (RpgStatsManager.Instance != null)
        {
            RpgStatsManager.Instance.TakeDamage(damageAmount);
        }
    }
}
```
