# Система атаки (AttackSystem)

**Что это:** legacy-компоненты старой боевой системы: здоровье, выполнение атаки, уклонение и hit-collider логика.

**Важно:** для новых проектов используйте модуль [RPG](../../../Rpg/README.md): `RpgCharacter`, `RpgAttackController`, `RpgProjectile`, `RpgEvadeController`, баффы, статусы, сохранение и Mirror-синхронизацию. AttackSystem остаётся только для совместимости старых сцен и префабов.

## Рекомендуемая замена

| Legacy | Замена |
|--------|--------|
| `Health` | [RpgCharacter](../../../Rpg/RpgCharacter.md) |
| `AttackExecution` | [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgAttackDefinition](../../../Rpg/RpgAttackDefinition.md) |
| `Evade` | [RpgEvadeController](../../../Rpg/RpgEvadeController.md) |
| `AdvancedAttackCollider` | [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgProjectile](../../../Rpg/RpgProjectile.md) |
| `IDamageable` / `IHealable` совместимость | [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md) |

## Legacy bridge

`RpgStatsDamageableBridge` нужен, когда старый компонент вызывает `IDamageable.TakeDamage(int)` или `IHealable.Heal(int)`, а целевой объект уже живёт на новом `RpgCharacter`.

Типовой сценарий:

1. На объекте или родителе есть `RpgCharacter`.
2. На этом же объекте или дочернем объекте добавлен `RpgStatsDamageableBridge`.
3. Legacy-компонент бьёт по `IDamageable`.
4. Bridge пересылает вызов в `RpgCharacter.Damage(...)` или `RpgCharacter.Heal(...)`.

Bridge поддерживает отдельные множители урона и лечения. Значения ниже нуля обрезаются до `0`.

## Компоненты

- [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md) - мост legacy `IDamageable/IHealable` -> новый RPG combat layer.
- [AdvancedAttackCollider](./AdvancedAttackCollider.md) - расширенный коллайдер атаки *(legacy)*.
- [AttackExecution](./AttackExecution.md) - выполнение атаки *(legacy)*.
- [Evade](./Evade.md) - уклонение/рывок с перезарядкой *(legacy)*.
- [Health](./Health.md) - здоровье, урон, лечение, авто-хил *(legacy)*.
