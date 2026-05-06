# RpgEvadeController

**Что это:** RPG-native evade ability с cooldown и временной неуязвимостью.

**Навигация:** [← К RPG](./README.md)

---

## Что умеет

- `TryStartEvade()` запускает evade.
- Даёт временный invulnerability lock на `RpgCombatant` или `RpgStatsManager`.
- Хранит `IsEvading`, `CanEvade`, `RemainingCooldownState`.
- Подходит как replacement для legacy `Evade`.

## Built-in input

- По умолчанию включён.
- Стандартный binding: `LeftShift`.
- Binding можно поменять на `MouseButton` или другой `KeyCode`.
- Для NPC/AI рекомендуется выключать `Enable Built-in Input`.

## Когда использовать

- Roll / dash / dodge.
- I-frames для boss-fight.
- Кнопка уворота через no-code (`RpgNoCodeAction`) или код.


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `EnableBuiltInInput` | Enable Built In Input. |
| `IsEvadingState` | Is Evading State. |
| `_combatant` | Combatant. |
| `_cooldown` | Cooldown. |
| `_evadeBinding` | Evade Binding. |
| `_evadeDuration` | Evade Duration. |
| `_onCooldownReady` | On Cooldown Ready. |
| `_onEvadeFinished` | On Evade Finished. |
| `_onEvadeStarted` | On Evade Started. |
| `_profileManager` | Profile Manager. |
| `true` | True. |