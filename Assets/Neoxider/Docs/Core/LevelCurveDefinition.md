# Level Curve Definition

**What it is:** a ScriptableObject that defines how level depends on accumulated XP. Implements `ILevelCurveDefinition`. Three modes: **Formula** (a formula by type), **Curve** (an AnimationCurve graph), **Custom** (a manual level → XP table). Used in `LevelComponent` and via `LevelModel.SetCurveDefinition()`. Path: `Scripts/Core/Level/Data/LevelCurveDefinition.cs`, namespace `Neo.Core.Level`. Asset creation: menu **Create → Neoxider → Core → Level Curve Definition**.

**How to use:**
1. Create an asset via the menu above.
2. Choose a **Mode**: Formula, Curve, or Custom.
3. Depending on the mode, set the formula parameters, the curve keys, or the list of entries (level, required XP).
4. Assign the asset to the **Level Curve** field on `LevelComponent`; the level and XP to the next level will be calculated from this definition.

---

## Modes (LevelCurveMode)

| Mode | Description |
|-------|----------|
| **Formula** | Level by formula. Choose a **formula type** (LevelFormulaType) and parameters; the cumulative XP for level L is given by an expression. |
| **Curve** | Level by graph. **AnimationCurve**: the X axis is the level number (1, 2, 3…), the Y axis is the cumulative XP up to that level. Convenient for an arbitrary progression curve. |
| **Custom** | Manual table. A list of **LevelCurveEntry** entries (level, required XP). Full control over the thresholds. |

---

## Formula Types (LevelFormulaType)

Used in **Formula** mode. In all cases: **RequiredXp(level)** is the cumulative XP needed to reach the level; the current level for a given totalXp is the maximum level for which RequiredXp(level) ≤ totalXp.

| Type | RequiredXp(level) formula | Inspector parameters |
|-----|----------------------------|-------------------------|
| **Linear** | (level − 1) × xpPerLevel | Xp Per Level |
| **LinearWithOffset** | constantOffset + (level − 1) × xpPerLevel | Constant Offset, Xp Per Level |
| **Quadratic** | quadraticBase × (level − 1)² | Quadratic Base |
| **Exponential** | expBase × expFactor^(level − 1) | Exp Base, Exp Factor |
| **Power** | powerBase × (level − 1)^powerExponent | Power Base, Power Exponent |
| **PolynomialSingle** | same as Power | Power Base, Power Exponent |

The formulas can be extracted into a separate reusable module (shop, upgrades); the domain logic in `LevelCurveEvaluator` does not depend on Unity.

---

## Fields (Inspector)

### Mode

| Field | Type | Purpose |
|------|-----|------------|
| Mode | LevelCurveMode | Formula / Curve / Custom. |
| Formula Type | LevelFormulaType | The formula type (when Mode = Formula). |

### Formula Parameters (Mode = Formula)

| Field | Type | Purpose |
|------|-----|------------|
| Xp Per Level | int | XP per level (Linear, LinearWithOffset). |
| Constant Offset | float | Offset (LinearWithOffset only). |
| Quadratic Base | float | Base for level² (Quadratic). |
| Exp Base, Exp Factor | float | Base and multiplier for the exponent (Exponential). |
| Power Base, Power Exponent | float | Base and exponent for Power / PolynomialSingle. |

### Curve (Mode = Curve)

| Field | Type | Purpose |
|------|-----|------------|
| Animation Curve | AnimationCurve | X = level (1, 2, 3…), Y = cumulative XP up to the level. |

### Manual Table (Mode = Custom)

| Field | Type | Purpose |
|------|-----|------------|
| Custom Entries | List&lt;LevelCurveEntry&gt; | A list of (level, required XP) pairs. |

---

## API (ILevelCurveDefinition)

| Method / property | Returns | Description |
|------------------|---------|----------|
| EvaluateLevel(int totalXp, int maxLevel = 0) | int | Calculates the level from accumulated XP; maxLevel = 0 means no cap. |
| GetXpToNextLevel(int totalXp, int maxLevel = 0) | int | XP to the next level; 0 if at max level. |
| Mode | LevelCurveMode | The current mode. |
| FormulaType | LevelFormulaType | The formula type (when Formula). |
| XpPerLevel | int | Formula parameter (get/set). |
| SetLinear(int xpPerLevel) | void | Set Formula mode, Linear type, and XP per level (for tests/runtime). |
| TryGetDefinition(int level, out LevelCurveEntry entry) | bool | Get the entry for a level (meaningful for Custom). |

---

## Examples

**No-Code:** create a Level Curve Definition asset → choose Formula, Linear, Xp Per Level = 100 → assign it to a LevelComponent. The level will be computed as 1 + totalXp / 100.

**Code:**

```csharp
// Assigning in a component
LevelComponent levelComponent = go.GetComponent<LevelComponent>();
levelComponent.LevelCurveDefinition = myCurveAsset;

// Direct calculation from the definition
ILevelCurveDefinition curve = myCurveAsset;
int level = curve.EvaluateLevel(totalXp, maxLevel: 50);
int xpToNext = curve.GetXpToNextLevel(totalXp, maxLevel: 50);

// Runtime: a linear curve for testing
var def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
def.SetLinear(100);
```

---

## See Also

- [Level.md](./Level.md) — LevelComponent, ILevelProvider, using the curve.
- [HealthComponent.md](./HealthComponent.md) — HP/Mana pools.
- [Progression/README.md](../Progression/README.md) — level rewards, perks (Progression uses its own LevelCurveDefinition for the progression tree).
