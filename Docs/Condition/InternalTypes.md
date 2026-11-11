# Condition — Internal Types

**What it is:** reference for the helper types behind `ConditionEntry` (scripts in `Scripts/Condition/`). `ConditionValueSource` is internal; `ReflectionCache` and `BindingSourceGameObjectResolver` are public and shared with Neo.NoCode bindings.

**How to use:** game code normally only needs `ConditionEntry`. Use the public helpers when you build your own binding component and want the same lookup rules (name search, occurrences, cached reflection).

---

| Type | Description |
|------|-------------|
| `ConditionEntry` | Condition entry (component, property, operator, value); see [NeoCondition](NeoCondition.md#conditionentry-from-c). |
| `ConditionValueSource` | Internal value reader for fields, properties and single-argument methods; binds to the current resolved context and reuses the converted method argument while it is unchanged. |
| `IConditionEvaluator` | Condition evaluator interface: `bool Evaluate(GameObject context)`. |
| `ReflectionCache` | Public metadata cache for fields, properties and supported methods; keys include binding flags and cache misses. `TryGetProperty` is the exception-free lookup that caches ambiguous names (overloaded indexers). |
| `BindingSourceGameObjectResolver` | Public shared source resolver: direct reference / host fallback, cached `GameObject.Find` with real-time retry throttling, the allocation-free `FindComponentOccurrence(root, typeName, occurrence)` / `MatchesComponentTypeName(type, name)` used by conditions and NoCode bindings, and `IsDestroyedReference(reference)`, which tells a destroyed explicit reference (no fallback) from an unassigned one (falls back), including the Editor's placeholder for an unassigned serialized field. |

```csharp
Component second = BindingSourceGameObjectResolver.FindComponentOccurrence(actor, "AudioSource", 1);
```

`MatchesComponentTypeName` accepts the full name (`UnityEngine.AudioSource`) or the short name (`AudioSource`), the same rule the Inspector pickers store.

In the Editor, Unity deserializes an unassigned object field of a component or ScriptableObject into a placeholder that compares equal to `null` but is not a C# `null`. `IsDestroyedReference` returns false for it (and for `null`), and true only for a reference whose object was destroyed or is missing; `Resolve` therefore falls back to the host for an empty saved field and returns null for a destroyed one.

## See also

- [NeoCondition](NeoCondition.md) — ← [Condition](README.md)
