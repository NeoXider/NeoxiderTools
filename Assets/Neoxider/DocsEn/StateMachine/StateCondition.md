# StateCondition

**Purpose:** Abstract base class for state transition conditions in `StateMachine`. Has a single method `Evaluate()` → `bool`. Three built-in implementations: `BoolStateCondition`, `FloatStateCondition`, `EventStateCondition`.

> For new projects, prefer `StatePredicate` — a more flexible replacement.

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

### No-Code (Inspector)
1. In `StateMachineBehaviour`, add a transition with `FloatStateCondition`.
2. Bind **Value** to the character's health.
3. Set **Comparison** = `LessThan`, **Threshold** = `0`.
4. The transition fires when health drops below zero.

### Code
```csharp
var condition = new FloatStateCondition();
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
