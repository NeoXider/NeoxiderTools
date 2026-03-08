# RpgTargetSelector

**Что это:** reusable `MonoBehaviour`-selector цели для AI, skill logic и spell casting.

**Навигация:** [← К RPG](./README.md)

---

## Что умеет

- Ищет цель по `RpgTargetQuery`
- Поддерживает `Nearest`, `Farthest`, `LowestCurrentHp`, `HighestCurrentHp`, `LowestHpPercent`, `HighestLevel`, `Random`
- Работает с `RpgCombatant` и `RpgStatsManager`
- Хранит `CurrentTarget`
- Имеет события выбора/очистки цели

## Inspector-тестирование

- `SelectTarget()` помечен `[Button]`
- `ClearTarget()` помечен `[Button]`

Это удобно для быстрой проверки AI target logic прямо в инспекторе.
