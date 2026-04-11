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
- Поддерживает `SetMaxHp(float)` и `IncreaseMaxHp(float)` для динамического изменения запаса здоровья (например, при левелапах).
- Поддерживает `TryApplyBuff()` и `TryApplyStatus()`.
- Учитывает `DefensePercent`, `DamagePercent`, `HpRegenPerSecond`, `MovementSpeedPercent`.
- Поддерживает invulnerability locks для evade и других способностей.

## Типичный сценарий

1. Добавьте `RpgCombatant` на врага.
2. Назначьте массивы `BuffDefinition[]` и `StatusEffectDefinition[]`.
3. Если враг умеет атаковать, добавьте рядом `RpgAttackController`.
4. Если нужен dodge/roll, добавьте `RpgEvadeController`.

## Интеграция API (Типы Урона и Резисты)

`RpgCombatant` принимает объект `RpgDamageInfo` вместо устаревшего `float`, что позволяет передавать источник урон и его тип (стихию).

### Пример передачи урона

```csharp
RpgCombatant target = GetComponent<RpgCombatant>();

// Создаем контекст урона
var damageInfo = new RpgDamageInfo(
    amount: 50f, 
    source: this.gameObject, 
    damageType: "Fire"
);

// Применяем урон с учетом стихийных резистов
float actualDamageTaken = target.TakeDamage(damageInfo);
```

### Стихийные Резисты (Elemental Resistances)

Чтобы создать бафф для защиты от стихии:
1. Создайте пресет `BuffDefinition`.
2. Добавьте `BuffStatModifier` и установите Stat Type в `SpecificDefensePercent`.
3. Задайте `SpecificDamageType` (например `Fire` или `Ice`).
4. Ядро `RpgCombatMath` автоматически извлечет `damageType` из `RpgDamageInfo` при атаке и снизит входящий урон на указанный `SpecificDefensePercent`.
