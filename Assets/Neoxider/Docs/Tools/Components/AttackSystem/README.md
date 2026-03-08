# Система атаки (Attack System)

**Что это:** legacy-компоненты старой боевой системы: здоровье, исполнение атаки, уклонение, hit-collider логика.

**Важно:** для новых проектов рекомендуется использовать модуль [**RPG**](../../Rpg/README.md) (`RpgStatsManager`, баффы, статус-эффекты, persistence). Компоненты ниже помечены как legacy.

**Оглавление:** см. список ссылок ниже.

---

## Рекомендуемая замена

| Legacy | Замена |
|--------|--------|
| Health | [RpgStatsManager](../../Rpg/README.md) или `RpgCombatant` |
| AttackExecution | [RpgAttackController](../../Rpg/RpgAttackController.md) + [RpgAttackDefinition](../../Rpg/RpgAttackDefinition.md) |
| Evade | [RpgEvadeController](../../Rpg/RpgEvadeController.md) |
| AdvancedAttackCollider | [RpgAttackController](../../Rpg/RpgAttackController.md) + [RpgProjectile](../../Rpg/RpgProjectile.md) |

Для совместимости со старым `IDamageable` используйте [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md).

## Компоненты

- [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md) — мост legacy `IDamageable/IHealable` → новый RPG combat layer.
- [AdvancedAttackCollider](./AdvancedAttackCollider.md) — расширенный коллайдер атаки *(legacy)*.
- [AttackExecution](./AttackExecution.md) — исполнение атаки *(legacy)*.
- [Evade](./Evade.md) — уклонение/рывок с перезарядкой *(legacy)*.
- [Health](./Health.md) — здоровье, урон, лечение, авто-хил *(legacy)*.
