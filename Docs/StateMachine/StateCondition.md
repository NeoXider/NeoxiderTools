# StateCondition

**What it is:** the legacy abstract base class for state conditions (`Evaluate()` → `bool`) with three built-in implementations: `BoolStateCondition`, `FloatStateCondition`, `EventStateCondition`. Path: `Scripts/StateMachine/StateCondition.cs`, namespace `Neo.StateMachine`. Transitions do not consume it; it is kept for backward compatibility.

**How to use:** evaluate it yourself from code (see the example). For transition gating use `StatePredicate` — the more flexible replacement that `StateTransition` evaluates.

---

## API

| Method | Description |
|--------|-------------|
| `abstract bool Evaluate()` | Check the condition. `true` — transition is allowed. |

---

## Built-in Implementations

### BoolStateCondition
Returns a stored bool value.

| Field | Description |
|-------|-------------|
| **Value** | `bool` — the value returned by `Evaluate()`. Can be changed from code or Inspector. |

### FloatStateCondition
Compares a float value against a threshold.

| Field | Description |
|-------|-------------|
| **Value** | `float` — left-hand side of the comparison. |
| **Comparison** | Operator: `>`, `<`, `>=`, `<=`, `==`, `!=`. |
| **Threshold** | `float` — right-hand side of the comparison. |

### EventStateCondition
Condition via UnityEvent: on `Evaluate()`, fires `OnEvaluate`; a listener calls `SetResult(bool)`.

| Field | Description |
|-------|-------------|
| **On Evaluate** | `UnityEvent` — fires during condition check. |

| Method | Description |
|--------|-------------|
| `void SetResult(bool result)` | Set the result for the current evaluation. Called from `OnEvaluate` listener. |

---

## Examples

### Code
```csharp
FloatStateCondition condition = new();
condition.Value = player.Health;
condition.Comparison = ComparisonType.LessThan;
condition.Threshold = 0f;

if (condition.Evaluate())
    stateMachine.ChangeState<DeadState>();
```

---

## See Also
- [StatePredicate](StatePredicate.md) — more powerful alternative
- [StateMachine](StateMachine.md)
- ← [StateMachine](README.md)
