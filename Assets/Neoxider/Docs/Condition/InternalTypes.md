# Condition — Internal Types

| Type | Description |
|------|-------------|
| `ConditionEntry` | Condition entry (component, property, operator, value). |
| `ConditionValueSource` | Internal value reader for fields, properties, and methods; binds to the current resolved context and converts supported method arguments. |
| `IConditionEvaluator` | Condition evaluator interface. |
| `ReflectionCache` | Public metadata cache for fields, properties, and supported methods; lookup keys include binding flags and cache misses. |
| `BindingSourceGameObjectResolver` | Public shared source resolver with direct-reference/host fallback, cached scene-name search, and real-time retry throttling. |

## See Also
- [NeoCondition](NeoCondition.md) — ← [Condition](README.md)
