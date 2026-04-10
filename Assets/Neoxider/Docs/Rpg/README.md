# RPG

**Что это:** полноценный RPG runtime-модуль для persistent player profile, локальных combatant-актеров, melee/ranged/aoe атак, target selectors, attack presets для AI/skills/spells, evade, баффов, статус-эффектов, built-in input и no-code интеграции. Скрипты находятся в `Scripts/Rpg/`.

**Оглавление:**
- [RpgStatsManager](./RpgStatsManager.md) — persistent профиль игрока, save/load, баффы, статусы.
- [RpgCombatant](./RpgCombatant.md) — scene-local актёр для врагов, NPC и объектов.
- [RpgAttackController](./RpgAttackController.md) — единая точка запуска melee/ranged/aoe атак.
- [RpgAttackDefinition](./RpgAttackDefinition.md) — ScriptableObject-описание атаки.
- [RpgAttackPreset](./RpgAttackPreset.md) — preset для AI/skills/spells с таргетингом.
- [RpgProjectile](./RpgProjectile.md) — projectile runtime для дальних атак.
- [RpgTargetSelector](./RpgTargetSelector.md) — selector цели для AI и ability logic.
- [RpgEvadeController](./RpgEvadeController.md) — evade/invulnerability/cooldown.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — no-code bridge для UnityEvent.
- [RpgConditionAdapter](./RpgConditionAdapter.md) — адаптер условий для `NeoCondition`.

**Навигация:** [← К Docs](../README.md)

---

## Как использовать

1. Создайте asset'ы `BuffDefinition`, `StatusEffectDefinition` и `RpgAttackDefinition` через меню `Neoxider/RPG`.
2. Для игрока используйте `RpgStatsManager`, если нужны persistence и глобальный профиль.
3. При необходимости включите `Auto Save` у `RpgStatsManager`. По умолчанию он выключен.
4. Для врагов/NPC/объектов используйте `RpgCombatant`.
5. Для атак добавьте `RpgAttackController` и назначьте `RpgAttackDefinition[]`. По умолчанию primary attack работает на ЛКМ.
6. Для дальних атак используйте `RpgProjectile`, для dodge/i-frames — `RpgEvadeController`.
7. Built-in input у атаки и уклонения можно отключить, что полезно для NPC/AI.
8. Для no-code сценариев используйте `RpgNoCodeAction` и `RpgConditionAdapter`.

## Что входит в модуль

- `RpgStatsManager` — persistent HP/level/buffs/statuses профиль игрока.
- `RpgCombatant` — локальная версия combat receiver без persistence.
- `RpgAttackController` — одна система для direct, area и projectile атак.
- `RpgAttackDefinition` — SO с power/range/radius/cooldown/effects/delivery mode.
- `RpgAttackPreset` — preset, который связывает атаку и target query.
- `RpgProjectile` — runtime projectile с hit detection и max hits.
- `RpgTargetSelector` — reusable selector ближайшей/случайной/приоритетной цели.
- `RpgEvadeController` — evade с cooldown и invulnerability lock.
- `BuffDefinition` — временные баффы с длительностью и модификаторами статов.
- `StatusEffectDefinition` — статус-эффекты (яд, замедление, stun/action lock, DoT).
- `RpgProfileData` — сериализуемый профиль для persistence.

## Persistence

- Профиль хранится через `SaveProvider`, а не через сценовый `SaveManager`.
- По умолчанию используется ключ `RpgV1.Profile`, но его можно поменять в `RpgStatsManager`.

## Интеграция с Progression

- Уровень в `RpgStatsManager` хранится отдельно от `ProgressionManager`.
- Для синхронизации можно вызывать `SetLevel(ProgressionManager.Instance.CurrentLevel)` при изменении прогрессии.

## Рекомендуемая схема

- Игрок: `RpgStatsManager` + `RpgAttackController` + `RpgEvadeController`.
- Враги/NPC: `RpgCombatant` + `RpgAttackController` + `RpgTargetSelector`.
- Skills/spells/AI routines: `RpgAttackPreset` для выбора attack + targeting policy.
- Legacy `IDamageable` совместимость: `RpgStatsDamageableBridge`.
- Primary attack: по умолчанию ЛКМ, binding настраивается в `RpgAttackController`.
- Evade: binding настраивается в `RpgEvadeController`, built-in input можно выключить.
- `Auto Save` у `RpgStatsManager` по умолчанию выключен и включается явно.
- Новые проекты: не использовать `AttackExecution`, `AdvancedAttackCollider`, `Evade`, `Health` как основную боевую архитектуру.

## Демонстрация

Работа модуля RPG в связке с `RpgCombatant` и интерактивным интерфейсом представлена в сцене `Samples~/Demo/RPG_Demo.unity`.
