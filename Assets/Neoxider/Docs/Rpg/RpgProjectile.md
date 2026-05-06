# RpgProjectile

**Что это:** lightweight projectile runtime для дальних атак в RPG-модуле.

**Навигация:** [← К RPG](./README.md)

---

## Как работает

- Спавнится из `RpgAttackController`, когда `DeliveryType = Projectile`.
- Двигается вперёд со скоростью из `RpgAttackDefinition`.
- Проверяет попадания между кадрами через physics cast.
- Может пробивать несколько целей до `ProjectileMaxHits`.

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