# RpgAttackDefinition

**Что это:** `ScriptableObject`-описание атаки для `RpgAttackController`.

**Навигация:** [← К RPG](./README.md)

---

## Что задаётся в asset

- `Id`, `DisplayName`
- `DeliveryType`: `Direct`, `Area`, `Projectile`
- `HitMode`: `Damage` или `Heal`
- `Power`, `Range`, `Radius`
- `CastDelay`, `Cooldown`
- `TargetLayers`, `MaxTargets`
- `ProjectilePrefab`, `ProjectileSpeed`, `ProjectileLifetime`, `ProjectileMaxHits`
- `Effects`: self buffs, target buffs, target statuses

## Идея

Одна attack definition описывает не конкретную анимацию, а боевой payload. Анимация, VFX, тайминги вызова и no-code orchestration могут жить отдельно.
