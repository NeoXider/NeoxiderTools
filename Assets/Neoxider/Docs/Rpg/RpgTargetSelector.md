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


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `HasTarget` | Has Target. |
| `HasTargetState` | Has Target State. |
| `_combatantSource` | Combatant Source. |
| `_onTargetCleared` | On Target Cleared. |
| `_onTargetSelected` | On Target Selected. |
| `_origin` | Origin. |
| `_profileSource` | Profile Source. |
| `_query` | Query. |