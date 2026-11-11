# Level - Internal Types

**What it is:** an index of the supporting types of the level/XP system in `Neo.Core.Level` (scripts under
`Scripts/Core/Level/`). These belong to the [Core module](../Core/README.md), not to the scene-flow
[Level module](README.md) that shares the name.

**How to use:** look a type up here; the behaviour is documented on [Level](../Core/Level.md),
[LevelComponent](../Core/Level/Components/LevelComponent.md), [LevelCurveDefinition](../Core/LevelCurveDefinition.md)
and the [level bridges](../Core/Level/Bridge/README.md).

---

| Type | File | Description |
|------|------|-------------|
| `LevelModel` | `Domain/LevelModel.cs` | Plain C# level/XP model behind `LevelComponent`. |
| `LevelCurveEvaluator` | `Domain/LevelCurveEvaluator.cs` | Evaluates XP thresholds for a curve definition. |
| `LevelCurveMode` | `Domain/LevelCurveMode.cs` | `Formula`, `Curve`, `Custom`. |
| `LevelFormulaType` | `Domain/LevelFormulaType.cs` | `Linear`, `Quadratic`, `Exponential`, `Power`, `LinearWithOffset`, `PolynomialSingle`. |
| `LevelCurveType` | `Domain/LevelCurveType.cs` | Legacy curve kind: `Linear`, `Quadratic`, `Exponential`, `Custom`. |
| `LevelCurveEntry` | `Data/LevelCurveEntry.cs` | One custom curve point (level and required XP). |
| `LevelProfileData` | `LevelProfileData.cs` | Serializable saved profile: `Version`, `TotalXp`, `CurrentLevel`, `MaxLevel`; `Sanitize()`, `Clone()`. |
| `ILevelProvider` | `Interfaces/ILevelProvider.cs` | Level/XP provider contract (`AddXp`, `SetLevel`, level and XP properties). |
| `ILevelCurveDefinition` | `Interfaces/ILevelCurveDefinition.cs` | Curve definition contract implemented by `LevelCurveDefinition`. |
| `ILevelCurveEntry` | `Interfaces/ILevelCurveEntry.cs` | Contract of one custom curve point. |
| `LevelConditionEvaluationMode` | `Bridge/LevelConditionEvaluationMode.cs` | `LevelAtLeast`, `XpAtLeast`, `XpToNextLevelAtMost`. |
| `LevelNoCodeActionType` | `Bridge/LevelNoCodeActionType.cs` | `AddXp`, `SetLevel`. |

## See Also

- [LevelComponent](../Core/Level/Components/LevelComponent.md)
- [Level curve evaluation boundaries](../Core/Level/Data/CurveEvaluation.md)
