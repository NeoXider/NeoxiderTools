# NeoCondition

**What it is:** a no-code condition component (`MonoBehaviour`, networked through `NeoNetworkComponent`). It reads field/property/method values of any component (or GameObject state) via cached reflection, combines several checks with AND/OR, optionally inverts each, and fires `OnTrue` / `OnFalse` / `OnResult(bool)` / `OnInvertedResult(bool)`. Path: `Scripts/Condition/NeoCondition.cs`, namespace `Neo.Condition`, menu `Neoxider/Condition/NeoCondition`. Each check is a serializable `ConditionEntry` that also works from plain C# without the component.

**How to use:**
1. Add the component via `Add Component → Neoxider/Condition/NeoCondition` (or the create menu).
2. Add one or more conditions: choose a source object, component, member, compare operator and threshold (a constant or another object's member).
3. Wire `On True` / `On False` (or `On Result`) to the reactions.
4. Pick a Check Mode: `Manual` (call `Check()` from another UnityEvent or code), `EveryFrame`, or `Interval`.
5. From C#, read `LastResult`, call `Check()` / `Evaluate()`, or skip the component and call `ConditionEntry.Evaluate(context)` / `TryEvaluate(context, out bool)` directly.

---

## Fields (Inspector, defaults)

| Field | Default | Description |
|-------|---------|-------------|
| Authority | `ServerRevalidate` | Networked only (Mirror): `ServerRevalidate` re-evaluates on the server (secure); `TrustClient` trusts the client result (client-local conditions). |
| Logic Mode | `AND` | `AND` (all conditions true) or `OR` (at least one true). |
| Conditions | empty | List of `ConditionEntry` (source object → component/GameObject → member, compare op, threshold). |
| Check Mode | `Interval` | `Manual` (only `Check()`), `EveryFrame` (throttled to ~60 Hz on unscaled time), or `Interval`. |
| Check Interval | `0.2` | Seconds between checks in `Interval` mode (clamped ≥ `0.01`). |
| Use Unscaled Time | `false` | `Interval` mode waits in real time, so checks continue while `Time.timeScale` is 0 (pause menus). Drawn under **Check Interval** when Check Mode is `Interval`; from C# use `UseUnscaledTime`. |
| Check On Start | `true` | Run one check in `Start()`. |
| Only On Change | `true` | Invoke events only when the result changes, not every tick. |
| On True / On False | — | `UnityEvent` fired for the combined result. |
| On Result / On Inverted Result | — | `UnityEvent<bool>` with the result / `!result`; respect Only On Change. |

`EveryFrame` always throttles on `Time.unscaledTime`, so it keeps checking while the game is paused. `Interval` uses game time unless **Use Unscaled Time** is on.

> **Behavior change:** before this release `EveryFrame` stopped checking while `Time.timeScale` was 0. It now keeps checking and firing `OnTrue` / `OnFalse` during pause, the same as `Update`. A networked condition with `ServerRevalidate` authority therefore keeps sending its server check request (about 60 per second) while paused. Use `Interval` mode (game time by default) for checks that should freeze with the game, or switch the condition to `Manual` while paused.

## Methods and properties

| Member | Description |
|--------|-------------|
| `Check()` | Evaluate and invoke events (respects Only On Change). UnityEvent-callable. |
| `Evaluate()` | Evaluate without firing events; returns the combined `bool`. Does not update `LastResult`. |
| `ResetState()` | Clears the last-result memory so the next `Check()` fires regardless of change. |
| `LastResult` | Result of the last check (read-only). |
| `Logic` / `Mode` / `CheckInterval` / `UseUnscaledTime` / `OnlyOnChange` | Settable at runtime; `Mode`, `CheckInterval` and `UseUnscaledTime` restart the interval loop. |
| `AddCondition(entry)` / `RemoveCondition(entry)` | Mutate the condition list at runtime. |
| `InvalidateAllCaches()` | Clears reflection bindings on all entries (after retargeting serialized members at runtime). |

## Events

With the default `OnlyOnChange = true`, the first check and every change invoke `OnResult`, `OnInvertedResult` and exactly one of `OnTrue` / `OnFalse`. With `OnlyOnChange = false` they fire on every check.

## ConditionEntry from C#

Every Inspector setting has a public property, including the Other Object branch and method arguments:

```csharp
using Neo.Condition;
using UnityEngine;

ConditionEntry hasMoreThanEnemy = new()
{
    ComponentTypeName = typeof(Health).FullName,
    PropertyName = "Current",
    CurrentValueType = Neo.Condition.ValueType.Float,
    Compare = CompareOp.Greater,
    ThresholdSource = ThresholdSource.OtherObject,
    OtherSourceObject = enemy,
    OtherComponentTypeName = typeof(Health).FullName,
    OtherPropertyName = "Current"
};

bool passes = hasMoreThanEnemy.Evaluate(player);
if (hasMoreThanEnemy.TryEvaluate(player, out bool result))
{
    // A real comparison happened; result already includes Invert.
}
```

`Health` stands for any component of yours. `Evaluate(context)` keeps the legacy contract (an unavailable source is `false` before `Invert`). `TryEvaluate(context, out bool result)` returns `false` when the source, member or value is unavailable, so an inverted entry cannot turn a missing object into `true`.

| Group | Properties |
|-------|------------|
| Left source | `Source`, `UseSceneSearch`, `SearchObjectName`, `WaitForObject`, `FindRetryIntervalSeconds`, `SourceObject`, `ComponentTypeName`, `ComponentOccurrence`, `PropertyName` |
| Method argument | `IsMethodWithArgument`, `PropertyArgumentKind`, `PropertyArgumentInt` / `Float` / `String` |
| Comparison | `CurrentValueType`, `Compare`, `Invert`, `ThresholdSource`, `ThresholdInt` / `Float` / `Bool` / `String` |
| Other Object | `OtherSourceMode`, `OtherUseSceneSearch`, `OtherSearchObjectName`, `OtherWaitForObject`, `OtherFindRetryIntervalSeconds`, `OtherSourceObject`, `OtherComponentTypeName`, `OtherComponentOccurrence`, `OtherPropertyName`, `OtherIsMethodWithArgument`, `OtherPropertyArgumentKind`, `OtherPropertyArgument*` |

Source/member setters invalidate the relevant binding; argument and threshold values are read on every evaluation.

## Method conditions

`NeoCondition` can evaluate component methods that take one primitive argument. After selecting such a method in the Property dropdown, the Inspector shows an **Argument** field. Integer arguments support `int`, `long`, `short` and `byte`; float arguments `float` and `double`; text arguments `string`. Supported return types are `int`, `long`, `short`, `byte`, `float`, `double`, `bool` and `string`.

The argument is compared on every evaluation, so changing it in Play Mode affects the next check; the converted argument is reused while it is unchanged. Integer arguments are converted to the parameter type before invocation, and a value outside `short`/`byte` range fails the evaluation without invoking the method. Methods requiring generic type arguments are excluded. Avoid overloads with the same name and argument kind (for example `Read(int)` and `Read(long)`): serialized entries store the name and argument kind, so they cannot tell these overloads apart.

```text
Source Object: Wallet
Component: Money
Property: CanSpend (float) -> bool [method]
Argument (float): 100
Compare: == true
```

Use `== true` when the condition should pass only if the method returns `true`, or `== false` / `!= true` for the failure case. Bool members only use `==` and `!=`.

## Component occurrences

In Component source mode, `ConditionEntry.ComponentOccurrence` selects which matching component to read; `OtherComponentOccurrence` does the same for an Other Object threshold. Both default to `0`, including entries authored before 11.0.0. Selection counts only components matching the configured type, in `GameObject.GetComponents<Component>()` order: `0` is the first match, `1` the second.

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

A negative or missing occurrence makes that source unavailable; it never falls back to the first match. The serialized occurrence fields are independent of the legacy `_componentIndex` / `_otherComponentIndex` Inspector bookkeeping, which is preserved but not migrated. After adding, removing or reordering components, invalidate entry bindings to resolve the current order again.

## Sources and unavailable values

An explicit Source Object takes precedence over the evaluation context. Find By Name uses `GameObject.Find` and caches a successful result; a missing named object does not fall back to the owner. Missing-object retries use real time, with a default one-second interval on each source branch; set the retry interval to `0` for every check. Wait For Object suppresses the missing-name warning. Direct references also work for inactive objects.

An unset source uses the context on every call to `ConditionEntry.Evaluate(context)`, also when the empty field was saved in a scene, prefab or asset (since 12.0 the Editor's placeholder for an unassigned field no longer counts as destroyed in Play Mode). A destroyed explicit source remains unavailable; it is not replaced with the owner. Missing sources, members, null values, conversion failures and getter/method exceptions produce `false` before the entry's Invert flag; use `TryEvaluate` when absence must not count as a result.

Indexed properties (including `Item`) and ambiguous property names are rejected during binding without calling their getters; the ambiguity is cached, so a misconfigured entry does not throw on every check. Retargeting the entry to a valid member recovers normally.

## Performance

- Member lookups go through the shared `ReflectionCache`; an entry binds once and re-resolves only after invalidation or a changed context/occurrence.
- A missing component or object is retried without allocations (shared component scratch list, no per-check messages); warnings are logged once.
- A warm evaluation with a constant threshold allocates only the boxed value returned by reflection. Prefer `Interval` (default 0.2 s) or `Manual` over `EveryFrame` for many conditions.

## Networked checks

With Mirror and `isNetworked` enabled, a pure client's `Check()` requests server evaluation in ServerRevalidate mode on every call, even if its local value has not changed. The server applies Only On Change to its own result. Client-submitted results are accepted only in TrustClient mode. Both command handlers reject ingress when networking is disabled and use the shared per-connection rate limiter.

TrustClient is intended for client-local state. Choose ServerRevalidate for shared gameplay conditions. Mirror transport, authority and late-join behavior require integration verification in a networked scene.

## See also

- [Condition module](README.md)
- [Reusing conditions](Condition_Reuse.md)
- [Internal types](InternalTypes.md)
