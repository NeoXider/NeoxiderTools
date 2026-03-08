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
- `UsePrimaryPreset()` — запускает первый preset из массива.
- `TryUseAttack(string attackId, out string failReason)` — запуск по id.
- `TryUseAttack(int attackIndex, out string failReason)` — запуск по индексу.
- `TryUsePreset(string presetId, out string failReason)` — запуск preset по id.
- `TryUsePreset(int presetIndex, out string failReason)` — запуск preset по индексу.
- `TryUsePreset(RpgAttackPreset preset, out string failReason)` — запуск конкретного preset asset.
- `TryUsePreset(RpgAttackPreset preset, GameObject forcedTarget, out string failReason)` — запуск preset по уже выбранной цели, удобно для AI/NPC brain.
- `CanUseAttack(string attackId, out string failReason)` — проверка cooldown/lock.
- `CanUsePreset(RpgAttackPreset preset, out string failReason)` — проверка готовности preset без ручного поиска attack id.
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
- Для AI/skills/spells можно использовать `RpgAttackPreset` и `RpgTargetSelector`.
- Для новых проектов это основной replacement для `AttackExecution` и `AdvancedAttackCollider`.
