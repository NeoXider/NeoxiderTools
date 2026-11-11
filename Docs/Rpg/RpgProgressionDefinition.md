# RpgProgressionDefinition

**What it is:** a ScriptableObject with `RpgCharacter` growth rules (`Scripts/Rpg/Data/RpgProgressionDefinition.cs`, namespace `Neo.Rpg`): the growth mode, upgrade points and per-stat upgrade rules.

**How to use:**
1. Create an asset via **Create > Neoxider > RPG > Progression Definition** and pick a growth mode below.
2. Assign it to the `progression` field of an [RpgCharacterTemplate](RpgCharacterTemplate.md), or to the
   character's **Progression (optional)** field.
3. Drive levels with `AddLevel` / `AddXp`, and spend manual points with `UpgradeStat` from code or
   [RpgNoCodeAction](RpgNoCodeAction.md).

---

## Growth Modes

| Mode | Purpose |
|------|---------|
| `None` | Level changes without automatic growth or point grants |
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

Use `RpgNoCodeAction.AddXp`, `AddLevel`, `ApplyLevelGrowth`, `AddUpgradePoints`, and `UpgradeStat`.
For conditions use `RpgConditionAdapter.UpgradePointsAtLeast` and `UpgradeLevelAtLeast`.

## Applied growth and profiles

Automatic `AllStatsEveryLevel`/`Hybrid` growth follows both upward and downward level changes when
`autoApplyGrowthOnLevelUp` is enabled. Other modes keep their previously applied growth level across
buff, upgrade, and base-stat refreshes. Call `RpgCharacter.ApplyLevelGrowth()` to commit the current level
explicitly, including in manual/None modes. Points are granted only on upward manual/hybrid changes.

```csharp
character.SetLevel(5);         // With auto-apply disabled, growth keeps its previous anchor.
character.ApplyLevelGrowth(); // Commits level 5 growth; later refreshes reuse it.
```

Profiles remain **v2** and do not serialize a separate applied-growth anchor. Initialization and profile
restore derive the anchor from the current/saved level in automatic modes, and reset it to 1 in all
other modes. Deferred/manual games must call `ApplyLevelGrowth()` after loading if that is their policy.
Changing the schema to persist this anchor is deferred.

По-русски: ручной и отложенный рост больше не применяется случайно при обновлении баффов или статов.
Явное применение доступно из C# и `RpgNoCodeAction`. После загрузки v2 в этих режимах его нужно повторить.
