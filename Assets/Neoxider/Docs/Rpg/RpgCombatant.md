# RpgCombatant

**Что это:** локальный `MonoBehaviour`-receiver для врагов, NPC, destructible-объектов и любых сценовых актёров без persistence.

**Навигация:** [← К RPG](./README.md)

---

## Когда использовать

- Нужен HP/level/buffs/statuses на объекте сцены, но не нужен `SaveProvider`.
- Нужна цель для `RpgAttackController`.
- Нужен combat actor, который можно убивать, лечить, баффать и делать временно неуязвимым.

## Что умеет

- Хранит `CurrentHp`, `MaxHp`, `Level`.
- Принимает `TakeDamage()` / `Heal()`.
- Поддерживает `TryApplyBuff()` и `TryApplyStatus()`.
- Учитывает `DefensePercent`, `DamagePercent`, `HpRegenPerSecond`, `MovementSpeedPercent`.
- Поддерживает invulnerability locks для evade и других способностей.

## Типичный сценарий

1. Добавьте `RpgCombatant` на врага.
2. Назначьте массивы `BuffDefinition[]` и `StatusEffectDefinition[]`.
3. Если враг умеет атаковать, добавьте рядом `RpgAttackController`.
4. Если нужен dodge/roll, добавьте `RpgEvadeController`.
