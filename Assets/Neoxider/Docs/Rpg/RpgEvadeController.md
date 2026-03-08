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
