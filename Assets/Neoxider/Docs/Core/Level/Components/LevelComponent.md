# LevelComponent

**Назначение:** Компонент уровня и XP — реактивные свойства, кривая уровня, сохранение, события.

## Подключение

- Добавить: **Add Component → Neoxider → Core → Level Component**.

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `1` | 1. |
| `HasMaxLevel` | Has Max Level. |
| `Level` | Level. |
| `LevelCurveDefinition` | Level Curve Definition. |
| `LevelState` | Level State. |
| `LevelStateValue` | Level State Value. |
| `MaxLevel` | Max Level. |
| `OnLevelUp` | On Level Up. |
| `OnProfileLoaded` | On Profile Loaded. |
| `OnProfileSaved` | On Profile Saved. |
| `OnXpGained` | On Xp Gained. |
| `TotalXp` | Total Xp. |
| `UseXp` | Use Xp. |
| `XpState` | Xp State. |
| `XpStateValue` | Xp State Value. |
| `XpToNextLevel` | Xp To Next Level. |
| `XpToNextLevelState` | Xp To Next Level State. |
| `XpToNextLevelStateValue` | Xp To Next Level State Value. |
| `_hasMaxLevel` | Has Max Level. |
| `_levelCurve` | Level Curve. |
| `_maxLevel` | Max Level. |
| `_onLevelUp` | On Level Up. |
| `_onProfileLoaded` | On Profile Loaded. |
| `_onProfileSaved` | On Profile Saved. |
| `_onXpGained` | On Xp Gained. |
| `_saveKey` | Save Key. |
| `_startXp` | Start Xp. |
| `true` | True. |

## Runtime контракт

- `AddXp(amount)` увеличивает общий XP, пересчитывает уровень по `LevelCurveDefinition` и вызывает `OnLevelUp` только если уровень действительно изменился.
- `SetLevel(level)` в режиме `UseXp = true` синхронизирует `TotalXp` с минимальным XP для указанного уровня, чтобы следующий пересчет кривой не откатывал уровень назад.
- `SetLevel(level)` в режиме `UseXp = false` работает как прямое выставление уровня без XP-прогресса.
- `LevelState`, `XpState` и `XpToNextLevelState` обновляются после изменений модели и подходят для UI/NoCode binding.
- `LevelNoCodeAction` использует тот же runtime API: действие `AddXp` вызывает событие level-up только при реальном повышении уровня.

## См. также

- [Корень модуля](../../README.md)
