# NeoCondition

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.
- Add one or more conditions, choose a source object, component, member, compare operator, and threshold.
- Component members can be fields, properties, or single-argument methods with an `int`, `float`, or `string` argument and an `int`, `float`, `bool`, or `string` return value.

## Method conditions

`NeoCondition` can evaluate component methods that take one primitive argument. After selecting such a method in the Property dropdown, the Inspector shows an **Argument** field. The argument is read on every evaluation, so changing it in Play Mode affects the next check.

Example:

```text
Source Object: Wallet
Component: Money
Property: CanSpend (float) -> bool [method]
Argument (float): 100
Compare: == true
```

Use `== true` when the condition should pass only if the method returns `true`, or `== false` / `!= true` when the condition should pass on failure. Bool members only use `==` and `!=`; numeric operators are not preserved for bool selections.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CheckMode` | Check Mode. |
| `Conditions` | Conditions. |
| `LastResult` | Last Result. |
| `Logic` | Logic. |
| `LogicMode` | Logic Mode. |
| `Mode` | Mode. |
| `OnFalse` | On False. |
| `OnInvertedResult` | On Inverted Result. |
| `OnResult` | On Result. |
| `OnTrue` | On True. |
| `_checkInterval` | Check Interval. |
| `_checkMode` | Check Mode. |
| `_conditions` | Conditions. |
| `_logicMode` | Logic Mode. |
| `_onFalse` | On False. |
| `_onInvertedResult` | On Inverted Result. |
| `_onResult` | On Result. |
| `_onTrue` | On True. |

## See Also

- [Module Root](../README.md)
