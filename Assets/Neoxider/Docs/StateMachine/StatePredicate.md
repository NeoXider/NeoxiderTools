# StatePredicate

**Purpose:** Serializable base class for transition predicates: a predicate gates whether a transition may run. Concrete predicates are composed in code or (via `[SerializeReference]`) in Inspector-driven NoCode setups.

## Base API

| Member | Description |
|--------|-------------|
| `PredicateName` | Display name for debugging and custom editors. |
| `IsInverted` | When `true`, negates the evaluated result. |
| `Evaluate(IState currentState)` / `Evaluate()` | Runs the predicate (invert applied). |
| `EvaluateInternal(IState)` | Core logic — override in derived types. |

## Built-in predicates

| Type | Description |
|------|-------------|
| `BoolPredicate` | Returns a constant `Value`. |
| `FloatComparisonPredicate` | Compares `Value` to `Threshold` via `ComparisonType`. |
| `IntComparisonPredicate` | Integer version of the comparison predicate. |
| `StringComparisonPredicate` | String equality with optional `CaseSensitive`. |
| `EventPredicate` | Fires `OnEvaluate` (UnityEvent); listeners call `SetResult(bool)`. |
| `CustomPredicate` | Wraps a runtime `Func<bool>` via `SetEvaluator` (not serialized). |
| `StateDurationPredicate` | Compares time since state entry (`SetEnterTime`) to `RequiredDuration`. |
| `AndPredicate` / `OrPredicate` | Combine child predicates with AND / OR semantics. |
| `NotPredicate` | Negates another predicate's result. |

`ComparisonType`: `GreaterThan`, `LessThan`, `GreaterThanOrEqual`, `LessThanOrEqual`, `Equal`, `NotEqual`.

## Example

```csharp
var predicate = new CustomPredicate();
predicate.SetEvaluator(() => player.Health < 30);
```

## See Also
- [StateCondition](StateCondition.md)
- [StateMachine](StateMachine.md)
- ← [StateMachine](README.md)
