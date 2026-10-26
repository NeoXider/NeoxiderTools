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

**11.0.0 compatibility:** Curve mode floors the final key's X coordinate to determine the last
reachable integer level, within the existing 1–10,000 scan range. A final key at X = 2.6 now ends at
level 2; the former rounded endpoint could expose level 3. XP to the next level is zero at the
authored end. Review assets that relied on the rounded endpoint; no serialized asset rewrite is
performed. See [Level curve evaluation boundaries](./Level/Data/CurveEvaluation.md) for fractional
threshold rounding, terminal levels, preview limits, and Inspector guidance.

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

The evaluator uses the same sanitized parameters for level lookup and next-level XP: XP per level is
at least 1; quadratic/exponential/power bases are at least 0.01; the exponential factor is at least 1.01;
the power exponent is at least 0.1. Non-finite values use those minima, and a non-finite constant offset
uses zero. Level 1 remains the starting level, including before the first exponential threshold.
Large thresholds are calculated in `double`; levels and reported remaining XP saturate at `int.MaxValue`.
See [Core runtime APIs](./README.md) for integer XP limits and direct `LevelModel` usage.

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
LevelCurveDefinition def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
def.SetLinear(100);
```

---

## See Also

- [Level curve evaluation boundaries](./Level/Data/CurveEvaluation.md) — Curve domain, integer XP and 11.0.0 endpoint behavior.
- [Level.md](./Level.md) — LevelComponent, ILevelProvider, using the curve.
- [HealthComponent.md](./HealthComponent.md) — HP/Mana pools.
- [Progression/README.md](../Progression/README.md) — level rewards, perks (Progression uses its own LevelCurveDefinition for the progression tree).
