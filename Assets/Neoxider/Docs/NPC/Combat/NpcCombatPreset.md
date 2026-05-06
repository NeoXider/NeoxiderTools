# NpcCombatPreset

**Что это:** ScriptableObject-профиль поведения для `NpcRpgCombatBrain`.

Он задаёт не сам урон, а тактическое поведение NPC вокруг атаки: как близко подходить, когда терять цель, бежать ли во время преследования, останавливать ли движение перед атакой и возвращать ли старый режим навигации после боя.

---

## Что хранит

- `Attack Preset` — ссылка на `RpgAttackPreset`
- `Preferred Attack Distance` — желаемая дистанция атаки
- `Lose Target Distance` — дистанция, после которой цель сбрасывается
- `Run While Chasing` — бежать ли в фазе преследования
- `Stop Movement Inside Attack Range` — стопать ли агент, когда цель уже в радиусе атаки
- `Face Target Before Attack` — разворачивать ли NPC к цели перед атакой
- `Auto Restore Navigation Mode` — вернуть ли прошлый `NpcNavigation.Mode`, когда бой закончился

## Зачем нужен отдельный preset

Без `NpcCombatPreset` часто появляются большие скрипты с кучей `if` под melee/ranged/spell NPC. Здесь это вынесено в data-driven слой:

- один и тот же `NpcRpgCombatBrain`
- разные `NpcCombatPreset`
- разные `RpgAttackPreset`

Готовые сочетания параметров для melee/ranged/stationary NPC собраны на странице **[NpcCombatScenarios](./NpcCombatScenarios.md)**.

## Типовые примеры

### Melee enemy

- `Preferred Attack Distance`: `1.5 - 2.2`
- `Lose Target Distance`: `10 - 15`
- `Run While Chasing`: `true`
- `Stop Movement Inside Attack Range`: `true`

### Ranged enemy

- `Preferred Attack Distance`: `6 - 12`
- `Lose Target Distance`: `15 - 25`
- `Run While Chasing`: `true`
- `Stop Movement Inside Attack Range`: `true`

### Stationary turret / mage

- `Preferred Attack Distance`: под дальность атаки
- `Run While Chasing`: `false`
- `Stop Movement Inside Attack Range`: `true`
- `Auto Restore Navigation Mode`: по ситуации


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `15f` | 15f. |
| `2f` | 2f. |
| `AutoRestoreNavigationMode` | Auto Restore Navigation Mode. |
| `DisplayName` | Display Name. |
| `FaceTargetBeforeAttack` | Face Target Before Attack. |
| `Id` | Id. |
| `LoseTargetDistance` | Lose Target Distance. |
| `PreferredAttackDistance` | Preferred Attack Distance. |
| `RunWhileChasing` | Run While Chasing. |
| `StopMovementInsideAttackRange` | Stop Movement Inside Attack Range. |
| `_attackPreset` | Attack Preset. |
| `_displayName` | Display Name. |
| `_id` | Id. |