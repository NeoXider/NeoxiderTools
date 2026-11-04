# RpgStatGrowthDefinition

**What it is:** per-level growth rules for stats. `RpgStatGrowthRule` (serializable struct) computes a value for a
level from a formula or an `AnimationCurve`; `RpgStatGrowthDefinition` (`ScriptableObject`) groups the rules for
max HP, HP regen, damage and defense percent and XP reward. Script: `Scripts/Rpg/Data/RpgStatGrowthDefinition.cs`,
namespace `Neo.Rpg`.

**How to use:**
1. Create an asset via **Create > Neoxider > RPG > StatGrowthDefinition** and set each rule's **Mode**,
   **Formula Type** and values (the custom drawer shows the relevant fields).
2. Read values from your code with `rule.Evaluate(level)`, for example
   `growth.MaxHp.Evaluate(character.Level)`. The asset is plain data: no built-in component reads it.
3. The same `RpgStatGrowthRule` type is the `growth` field of each `RpgStatDefinition` in an
   [RpgCharacter](RpgCharacter.md)'s stats: with **Affected By Level** on, the stat adds `growth.Evaluate(level)`
   to its base value.

---

## RpgStatGrowthRule

| Field | Type | Description |
|-------|------|-------------|
| `Mode` | `RpgStatGrowthMode` | `Formula` or `Curve`. |
| `FormulaType` | `RpgStatFormulaType` | `Linear`, `Exponential`, `Quadratic`, `Power`, `Flat`. |
| `BaseValue` | `float` | Value at level 1. |
| `AddPerLevel` | `float` | Additive step. |
| `MultiplierPerLevel` | `float` | Growth factor (`Exponential`) or exponent (`Power`). |
| `Curve` | `AnimationCurve` | Evaluated at the level in `Curve` mode. |
| `UseCurve` | `bool` | Hidden legacy flag that also selects the curve. |

`float Evaluate(int level)` uses the curve when the mode is `Curve` (or `UseCurve` is set) and the curve has keys.
Otherwise, with `n = max(0, level - 1)`:

| Formula | Result |
|---------|--------|
| `Flat` | `BaseValue` |
| `Linear` | `BaseValue + AddPerLevel * n` |
| `Exponential` | `BaseValue * MultiplierPerLevel ^ n` (factor `1.1` when not positive) |
| `Quadratic` | `BaseValue + AddPerLevel * n * n` |
| `Power` | `BaseValue + AddPerLevel * max(1, level) ^ MultiplierPerLevel` (exponent `2` when not positive) |

## RpgStatGrowthDefinition fields

| Field | Default rule |
|-------|--------------|
| `MaxHp` | base `100`, `+10` per level |
| `HpRegen` | `0` |
| `DamagePercent` | `+1` per level |
| `DefensePercent` | `+0.5` per level |
| `XpReward` | base `10`, `+5` per level |

## Example

```csharp
float maxHp = growthDefinition.MaxHp.Evaluate(level);
int xpReward = Mathf.RoundToInt(growthDefinition.XpReward.Evaluate(enemyLevel));
```

## See also

- [RpgProgressionDefinition](RpgProgressionDefinition.md) — level growth modes of a character.
- [RPG module](README.md)
