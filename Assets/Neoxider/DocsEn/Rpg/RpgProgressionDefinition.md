# RpgProgressionDefinition

**What it is:** a ScriptableObject with `RpgCharacter` growth rules.

**Create:** `Create -> Neoxider -> RPG -> Progression Definition`.

## Growth Modes

| Mode | Purpose |
|------|---------|
| `AllStatsEveryLevel` | Dota-like: every stat with `affectedByLevel` grows on level-up |
| `ManualUpgradePoints` | Dark-Souls-like: level grants points, player spends them on selected stats |
| `Hybrid` | Automatic growth plus manual upgrade points |

## Fields

| Field | Purpose |
|-------|---------|
| `growthMode` | Selects the growth model |
| `upgradePointsPerLevel` | Points granted per level |
| `autoApplyGrowthOnLevelUp` | Apply growth immediately or defer it |
| `upgradeRules` | Upgrade catalogue: stat, cost, cap, and derived resource modifiers |

## NoCode

Use `RpgNoCodeAction.AddXp`, `AddLevel`, `AddUpgradePoints`, and `UpgradeStat`.
For conditions use `RpgConditionAdapter.UpgradePointsAtLeast` and `UpgradeLevelAtLeast`.
