# NeoCondition

**Purpose:** No-code condition component. Reads field/property/method values off any component (or GameObject state) via reflection, combines several checks with AND/OR, optionally inverts each, and fires `OnTrue` / `OnFalse` / `OnResult(bool)` / `OnInvertedResult(bool)` events. Namespace `Neo.Condition`, menu `Neoxider/Condition/NeoCondition`.

## Setup

- Add the component via `Add Component → Neoxider/Condition/NeoCondition` (or the create menu).
- Add one or more conditions, choose a source object, component, member, compare operator, and threshold.
- Component members can be fields, properties, or non-generic single-argument methods. Integer arguments support `int`, `long`, `short`, and `byte`; float arguments support `float` and `double`; text arguments use `string`. Supported method return types are `int`, `long`, `short`, `byte`, `float`, `double`, `bool`, and `string`.

## Method conditions

`NeoCondition` can evaluate component methods that take one primitive argument. After selecting such a method in the Property dropdown, the Inspector shows an **Argument** field. The argument is read on every evaluation, so changing it in Play Mode affects the next check.

Integer arguments are converted to the selected parameter type before invocation. A value outside `short`/`byte` range fails the evaluation without invoking the method. Methods requiring generic type arguments are excluded. Avoid overloads with the same name and argument kind (for example, `Read(int)` and `Read(long)`): serialized entries store the name and argument kind, so they cannot identify these overloads separately.

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
| On Result | — | `UnityEvent<bool>` fired with the result; respects Only On Change. |
| On Inverted Result | — | `UnityEvent<bool>` fired with `!result`; respects Only On Change. |

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

## Component occurrences

In Component source mode, `ConditionEntry.ComponentOccurrence` selects which matching component to read. `OtherComponentOccurrence` does the same for an Other Object threshold. Both default to `0`, including entries authored before 11.0.0. Selection counts only components matching the configured type, in `GameObject.GetComponents<Component>()` order: `0` is the first match, `1` the second. Other component types do not consume an occurrence.

```csharp
ConditionEntry entry = new()
{
    ComponentTypeName = typeof(AudioSource).FullName,
    ComponentOccurrence = 1,
    PropertyName = nameof(AudioSource.volume),
    CurrentValueType = Neo.Condition.ValueType.Float,
    Compare = CompareOp.Greater,
    ThresholdFloat = 0.5f
};
bool secondSourceIsLoud = entry.Evaluate(actor);
```

Use `using Neo.Condition;` and `using UnityEngine;`; the example checks the second AudioSource on `actor`. For scene/NoCode authoring, choose the source object and component type, then set the corresponding Component Occurrence (or Other Component Occurrence) field. A negative or missing occurrence makes that source unavailable; it never falls back to the first match. The existing Invert policy still applies to the resulting false evaluation.

The serialized `_componentOccurrence` / `_otherComponentOccurrence` fields and public setters are independent of legacy `_componentIndex` / `_otherComponentIndex` Inspector bookkeeping. Existing indices are preserved and are not migrated into occurrences. The setters invalidate the respective binding; serialized occurrence edits are also detected on the next evaluation. After adding, removing, or reordering components, invalidate entry bindings to resolve the current component order again.

## Sources and unavailable values

An explicit Source Object takes precedence over the evaluation context. Find By Name uses `GameObject.Find` and caches a successful result; a missing named object does not fall back to the owner. Missing-object retries use real time, with a default one-second interval on each source branch; set the retry interval to `0` for every check. Wait For Object suppresses the missing-name warning. Direct references also work for inactive objects.

An unset source uses the context on every call to `ConditionEntry.Evaluate(context)`. A destroyed explicit source remains unavailable; it is not replaced with the owner. This also applies to the Other Object source during startup binding. Missing sources, missing members, null values, conversion failures, and getter/method exceptions produce `false` before applying the entry's Invert flag. Thus an inverted unavailable entry returns `true`; configure required references explicitly when absence must block an action.

Indexed properties (including `Item`) and ambiguous property-name lookups are rejected during binding. Their getters are not called. Retargeting the same entry to a valid ordinary field/property can recover normally; reflection lookup exceptions for ambiguous properties do not escape `ConditionEntry.Evaluate`.

Use `ConditionEntry.InvalidateCacheFull()` or `NeoCondition.InvalidateAllCaches()` after changing serialized source/member settings directly. Public source/member setters invalidate the relevant cache automatically. Entry invalidation clears binding state; the shared `ReflectionCache` retains reflection metadata with separate keys for each `BindingFlags` selection.

## Networked checks

With Mirror and `isNetworked` enabled, a pure client's `Check()` requests server evaluation in ServerRevalidate mode on every call, even if its local value has not changed. The server applies Only On Change to its own result. Client-submitted results are accepted only in TrustClient mode. Both command handlers reject ingress when networking is disabled and use the shared per-connection rate limiter.

TrustClient is intended for client-local state. It permits client-provided results on this component; choose ServerRevalidate for shared gameplay conditions. Mirror transport, authority, and late-join behavior require integration verification in a networked scene.

## See Also

- [Module Root](../README.md)
