# Condition module

**NeoCondition** is a No-Code condition system. It evaluates field/property values of any component via the Inspector without code. Supports AND/OR logic, inversion, and manual or automatic checking.

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
- **ResetResult()**, **ClearReflectionCache()**, **AddCondition()**, **RemoveCondition()** — Utility methods.

## See also

- [Condition_Reuse](Condition_Reuse.md) — Reusing conditions in State Machine and custom systems.
- [StateMachine](../StateMachine/README.md)
- [Tools/Components](../Tools/Components/README.md)
