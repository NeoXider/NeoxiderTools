# Система атаки (Attack System)

**Что это:** Компоненты для боевой системы: урон, здоровье, уклонение (Evade), коллайдеры атак.

**Важно:** для новых проектов рекомендуется использовать модуль [**RPG**](../../Rpg/README.md) (`RpgStatsManager`, баффы, статус-эффекты, persistence). Компоненты ниже помечены как legacy.

**Оглавление:** см. список ссылок ниже.

---

## Рекомендуемая замена

| Legacy | Замена |
|--------|--------|
| Health | [RpgStatsManager](../../Rpg/README.md) — HP, баффы, статусы, сохранение |
| AttackExecution, Evade, AdvancedAttackCollider | RpgStatsManager + [RpgNoCodeAction](../../Rpg/RpgNoCodeAction.md) + кастомная логика атаки/уклонения |

Для совместимости с `IDamageable` (например, из `AdvancedAttackCollider`) используйте [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md).

## Компоненты

- [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md) — мост IDamageable/IHealable → RpgStatsManager (рекомендуется для новых проектов).
- [AdvancedAttackCollider](./AdvancedAttackCollider.md) — расширенный коллайдер атаки *(legacy)*.
- [AttackExecution](./AttackExecution.md) — исполнение атаки *(legacy)*.
- [Evade](./Evade.md) — уклонение/рывок с перезарядкой *(legacy)*.
- [Health](./Health.md) — здоровье, урон, лечение, авто-хил *(legacy)*.
