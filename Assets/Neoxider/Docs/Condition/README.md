# Condition module

The module exposes `IConditionEvaluator` and serializable `ConditionEntry` for C# callers. **NeoCondition** adds scene configuration, AND/OR composition, scheduled checks, and UnityEvents through the Inspector.

## Runtime API

Configure an entry and call `Evaluate(context)` without adding a NeoCondition component:

```csharp
ConditionEntry active = new()
{
    Source = SourceMode.GameObject,
    PropertyName = nameof(GameObject.activeInHierarchy),
    CurrentValueType = Neo.Condition.ValueType.Bool,
    ThresholdBool = true
};
bool isActive = active.Evaluate(actor);
```

Use `using Neo.Condition;` and `using UnityEngine;`; `actor` is the GameObject being checked. An entry without an explicit source follows the context passed on each evaluation, including when reused for several owners. See [Condition_Reuse](Condition_Reuse.md) for integration and source-resolution rules.

## Quick start

1. Add **NeoCondition** to a GameObject (Add Component → Neoxider → Condition → NeoCondition).
2. Add conditions (Conditions list): choose source object, component, field, operator, threshold.
3. Configure **On True** / **On False** events.
4. Set check mode: **Manual** (call `Check()` from UnityEvent), **EveryFrame**, or **Interval**.

## Main types

- **LogicMode** — AND (all true) or OR (at least one true).
- **CheckMode** — Manual, EveryFrame, Interval.
- **ConditionEntry** — One condition: source (Component or GameObject), property/method, compare operator, threshold or other object.

## API (NeoCondition)

- **Check()** — Evaluates conditions and invokes OnTrue/OnFalse/OnResult.
- **Evaluate()** — Returns result without invoking events.
- **LastResult** — Result of the last check.
- **OnTrue**, **OnFalse**, **OnResult**, **OnInvertedResult** — UnityEvents.
- **ResetState()**, **InvalidateAllCaches()**, **AddCondition(entry)**, **RemoveCondition(entry)** — Utility methods.

`Evaluate()` does not update `LastResult`. `Check()` updates it and invokes events, subject to `OnlyOnChange`. With the default `OnlyOnChange = true`, the first check and subsequent changes invoke `OnResult`, `OnInvertedResult`, and exactly one of `OnTrue` / `OnFalse`.

For repeated component types, use `ConditionEntry.ComponentOccurrence` and `OtherComponentOccurrence`: `0` selects the first matching component, `1` the second. These selectors count only matching types and are independent of the legacy Inspector component-list indices. See [NeoCondition](NeoCondition.md#component-occurrences) for C# and scene setup.

## See also

- [Condition_Reuse](./Condition_Reuse.md) — Reusing conditions in State Machine and custom systems.
- [NeoCondition](./NeoCondition.md) — Full documentation.
- [StateMachine](../StateMachine/README.md)
- [Tools/Components](../Tools/Components/README.md)
