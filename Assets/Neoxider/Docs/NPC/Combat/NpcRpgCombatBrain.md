# NpcRpgCombatBrain

**Что это:** модульный combat brain для NPC, который соединяет `NpcNavigation`, `RpgTargetSelector`, `RpgAttackController` и `NpcCombatPreset` в один готовый автоматический боевой цикл.

**Подходит для:** melee NPC, ranged NPC, spell-caster NPC, patrol enemy с агро-переходом в бой.

---

## Идея

`NpcRpgCombatBrain` не хранит тяжёлую боевую логику внутри `NpcNavigation` и не заставляет делать отдельный скрипт под каждого врага. Вместо этого он:

- получает/обновляет цель через `RpgTargetSelector`
- принимает решение через `NpcCombatDecisionCore`
- переключает `NpcNavigation` в `FollowTarget`, когда цель далеко
- удерживает позицию в attack range
- вызывает `RpgAttackController` с конкретным `RpgAttackPreset`
- по завершении боя восстанавливает прошлый navigation mode, если это включено в `NpcCombatPreset`

## Обязательные зависимости

- `NpcNavigation`
- `RpgTargetSelector`
- `RpgAttackController`
- `NpcCombatPreset`

Обычно на том же объекте также нужен `RpgCombatant`.

## Основные настройки

- `Preset` — профиль поведения NPC
- `Auto Acquire Target` — автоматически переискать цель, если текущей нет
- `Disable Attack Controller Input` — удобно для NPC, чтобы отключить player-style input на `RpgAttackController`
- `Decision Interval` — как часто brain пересчитывает решение
- `Look Origin` — pivot, который разворачивается к цели перед атакой

## События

- `On Target Acquired`
- `On Target Lost`
- `On Chase Started`
- `On Holding Position`
- `On Attack Triggered`
- `On Attack Failed`
- `On Decision Changed`

## Кнопки Inspector

- `AutoResolveReferences()`
- `EvaluateNow()`
- `AcquireTarget()`
- `ClearCombatTarget()`
- `ForceAttack()`

## Готовые сценарии

Для пошаговой сборки конкретных типов врагов используйте **[NpcCombatScenarios](./NpcCombatScenarios.md)**.

## Рекомендуемая сборка melee NPC

1. Добавьте `NavMeshAgent`
2. Добавьте `NpcNavigation`
3. Добавьте `RpgCombatant`
4. Добавьте `RpgTargetSelector`
5. Добавьте `RpgAttackController`
6. Добавьте `NpcRpgCombatBrain`
7. Создайте `RpgAttackDefinition` для ближней атаки
8. Создайте `RpgAttackPreset`
9. Создайте `NpcCombatPreset` с небольшой `Preferred Attack Distance`

## Рекомендуемая сборка ranged NPC

Схема та же, но вместо отдельного скрипта меняются пресеты:

- у `RpgAttackDefinition` ставится `Projectile` или нужная ranged/AoE схема
- у `NpcCombatPreset` выставляется большая `Preferred Attack Distance`
- при необходимости NPC перестаёт двигаться внутри attack range

Так melee и ranged враги собираются из одного набора компонентов.
