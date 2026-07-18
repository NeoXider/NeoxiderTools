# NeoCondition

**Purpose:** No-code condition component. Reads field/property/method values off any component (or GameObject state) via reflection, combines several checks with AND/OR, optionally inverts each, and fires `OnTrue` / `OnFalse` / `OnResult(bool)` / `OnInvertedResult(bool)` events. Namespace `Neo.Condition`, menu `Neoxider/Condition/NeoCondition`.

## Setup

- Add the component via `Add Component → Neoxider/Condition/NeoCondition` (or the create menu).
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

## Inspector fields (defaults)

| Field | Default | Description |
|-------|---------|-------------|
| Authority | `ServerRevalidate` | Networked only (Mirror): `ServerRevalidate` re-evaluates on the server (secure); `TrustClient` trusts the client result (client-local conditions). |
| Logic Mode | `AND` | `AND` (all conditions true) or `OR` (at least one true). |
| Conditions | empty | List of condition entries (source object → component/GameObject → member, compare op, threshold). |
| Check Mode | `Interval` | `Manual` (only `Check()`), `EveryFrame` (throttled to ~60 Hz), or `Interval`. |
| Check Interval | `0.2` | Seconds between checks in `Interval` mode (clamped ≥ `0.01`). |
| Check On Start | `true` | Run one check in `Start()`. |
| Only On Change | `true` | Invoke events only when the result changes, not every tick. |
| On True | — | `UnityEvent` fired when the combined result is true. |
| On False | — | `UnityEvent` fired when the combined result is false. |
| On Result | — | `UnityEvent<bool>` fired every check with the result. |
| On Inverted Result | — | `UnityEvent<bool>` fired every check with `!result`. |

## API

| Member | Description |
|--------|-------------|
| `Check()` | Evaluate and invoke events (respects Only On Change). UnityEvent-callable. |
| `Evaluate()` | Evaluate without firing events; returns the combined `bool`. |
| `ResetState()` | Clears the last-result memory so the next `Check()` fires regardless of change. |
| `LastResult` | Result of the last check (read-only). |
| `Logic` / `Mode` / `CheckInterval` / `OnlyOnChange` | Settable at runtime; setting `Mode`/`CheckInterval` restarts the interval loop. |
| `AddCondition(entry)` / `RemoveCondition(entry)` | Mutate the condition list at runtime. |
| `InvalidateAllCaches()` | Clears reflection caches on all entries (after retargeting members at runtime). |

## See Also

- [Module Root](../README.md)
