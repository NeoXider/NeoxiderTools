# StatePredicate

**Purpose:** Serializable base class for transition predicates: a predicate gates whether a transition may run. Concrete predicates are composed in code or (via `[SerializeReference]`) in Inspector-driven NoCode setups.

## Base API

| Member | Description |
|--------|-------------|
| `PredicateName` | Display name for debugging and custom editors. |
| `IsInverted` | When `true`, negates the evaluated result. |
| `Evaluate(IState currentState)` / `Evaluate()` | Runs the predicate (invert applied). |
| `EvaluateInternal(IState)` | Core logic — override in derived types. |
| `OnOwnerStateEntered(float enterTime)` | Virtual entry hook; composites forward it. Time is in the owning machine's clock domain. |

## Built-in predicates

| Type | Description |
|------|-------------|
| `BoolPredicate` | Returns a constant `Value`. |
| `FloatComparisonPredicate` | Compares `Value` to `Threshold` via `ComparisonType`. |
| `IntComparisonPredicate` | Integer version of the comparison predicate. |
| `StringComparisonPredicate` | String equality with optional `CaseSensitive`. |
| `EventPredicate` | Fires `OnEvaluate` (UnityEvent); listeners call `SetResult(bool)`. |
| `CustomPredicate` | Wraps a runtime `Func<bool>` via `SetEvaluator` (not serialized). |
| `StateDurationPredicate` | Compares machine-owned elapsed time to `RequiredDuration`; standalone calls use `SetEnterTime` or first evaluation. |
| `AndPredicate` / `OrPredicate` | Combine child predicates with AND / OR semantics. |
| `NotPredicate` | Negates another predicate's result. |

`ComparisonType`: `GreaterThan`, `LessThan`, `GreaterThanOrEqual`, `LessThanOrEqual`, `Equal`, `NotEqual`.

## Duration ownership

Machine evaluation supplies an entry/evaluation timing scope for built-in duration predicates, including AND/OR/NOT children. Two machines sharing one predicate/configuration no longer reset one another's wait. Base evaluation applies `IsInverted` once and still dispatches the existing virtual `Evaluate(IState)` overload; custom predicates keep their public contract. Custom mutable fields/actions remain shared.

Outside a machine scope, both `Evaluate()` and `Evaluate(IState)` use the predicate's standalone `Time.time` anchor. `SetEnterTime(float)` and a direct `OnOwnerStateEntered(float)` still set that anchor; first evaluation initializes it if absent. Machine entry does not overwrite it. To edit an active machine's anchor in 11.0, migrate a former global `predicate.SetEnterTime(...)` call to `machine.SetCurrentStateEnterTime(...)`. Use that machine's clock domain, normally `Time.time`; the [core API](StateMachine.md) also accepts an injected clock. Nested evaluation and entry-hook scopes restore previous timing in `finally`.

## Example

```csharp
CustomPredicate predicate = new CustomPredicate();
predicate.SetEvaluator(() => player.Health < 30);
```

## See Also
- [StateCondition](StateCondition.md)
- [StateMachine](StateMachine.md)
- ← [StateMachine](README.md)
