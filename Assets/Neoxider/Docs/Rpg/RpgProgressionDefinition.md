# RpgProgressionDefinition

**Что это:** ScriptableObject с правилами роста `RpgCharacter`.

**Создание:** `Create -> Neoxider -> RPG -> Progression Definition`.

## Режимы роста

| Режим | Назначение |
|------|------------|
| `AllStatsEveryLevel` | Dota-like: все статы с `affectedByLevel` растут при повышении уровня |
| `ManualUpgradePoints` | Dark-Souls-like: уровень выдаёт очки, игрок тратит их на выбранные статы |
| `Hybrid` | Авто-рост + ручные очки улучшений |

## Поля

| Поле | Назначение |
|------|------------|
| `growthMode` | Выбирает модель роста |
| `upgradePointsPerLevel` | Сколько очков даётся за уровень |
| `autoApplyGrowthOnLevelUp` | Применять рост сразу или отложить |
| `upgradeRules` | Каталог улучшений: какой стат растёт, цена, лимит, derived resource modifiers |

## NoCode

Используйте `RpgNoCodeAction.AddXp`, `AddLevel`, `AddUpgradePoints`, `UpgradeStat`.
Для условий используйте `RpgConditionAdapter.UpgradePointsAtLeast` и `UpgradeLevelAtLeast`.
