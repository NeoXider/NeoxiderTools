# RpgAttackController

**Что это:** универсальный runtime-компонент для запуска melee, ranged и area атак по `RpgAttackDefinition`.

**Навигация:** [← К RPG](./README.md)

---

## Поддерживаемые режимы

| Режим | Как работает |
|------|---------------|
| `Direct` | Raycast / SphereCast / CircleCast по направлению вперёд |
| `Area` | OverlapSphere / OverlapCircle в точке атаки |
| `Projectile` | Спавнит `RpgProjectile` с тем же definition |

## Основной API

- `UsePrimaryAttack()` — запускает первую атаку из массива.
- `TryUseAttack(string attackId, out string failReason)` — запуск по id.
- `TryUseAttack(int attackIndex, out string failReason)` — запуск по индексу.
- `CanUseAttack(string attackId, out string failReason)` — проверка cooldown/lock.
- `GetRemainingCooldown(string attackId)` — остаток кулдауна.

## Built-in input

- По умолчанию включён.
- Primary attack по умолчанию висит на ЛКМ.
- Binding можно переключить между `MouseButton` и `KeyCode`.
- Для NPC/AI рекомендуется выключать `Enable Built-in Input`.

## Что важно

- Источником статов может быть `RpgCombatant` или `RpgStatsManager`.
- Для урона учитывается `GetOutgoingDamageMultiplier()`.
- Дополнительные эффекты берутся из `RpgAttackDefinition.Effects`.
- Для новых проектов это основной replacement для `AttackExecution` и `AdvancedAttackCollider`.
