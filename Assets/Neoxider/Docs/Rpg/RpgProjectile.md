# RpgProjectile

**Что это:** lightweight projectile runtime для дальних атак в RPG-модуле.

**Навигация:** [← К RPG](./README.md)

---

## Как работает

- Спавнится из `RpgAttackController`, когда `DeliveryType = Projectile`.
- Двигается вперёд со скоростью из `RpgAttackDefinition`.
- Проверяет попадания между кадрами через physics cast.
- Может пробивать несколько целей до `ProjectileMaxHits`.
- Повторные попадания считаются по `IRpgCombatReceiver`, а не по отдельному `GameObject`, поэтому несколько коллайдеров одного персонажа не расходуют несколько hit slots.
- Повторный `Initialize(...)` сбрасывает lifetime, remaining hits и dedupe-наборы, поэтому projectile можно безопасно переинициализировать в pool-friendly сценариях.

## Когда использовать

- Стрелы, пули, магические снаряды.
- Heal projectile.
- Projectile AoE chain, если payload на цели должен быть единым и управляться definition.


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `_onExpired` | On Expired. |
| `_onHit` | On Hit. |
| `_onInitialized` | On Initialized. |
