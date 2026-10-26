# StateMachineEvaluationContext

**Purpose:** `internal static` context for `StateMachine` predicates. NoCode predicates resolve the owning `GameObject` and slot-based overrides supplied by the scene wrapper. A separate timing stack supplies machine-owned duration entry/evaluation times. Thread-static stacks support nested calls and are restored by their callers in `finally`; subsystem reset clears both kinds of context. This is internal implementation, not a public extension API.

## API

| Member | Description |
|--------|-------------|
| `static GameObject CurrentContextObject { get; }` | The GameObject of the state machine currently being evaluated. |
| `static GameObject GetContextBySlot(int slot)` | Resolves context by slot: `0` = owner (GameObject with State Machine), `1+` = entry from Context Overrides on the component. |
| `static void Push(GameObject contextObject, IReadOnlyList<GameObject> overrides = null)` | Pushes a new context (and its override list) onto the stack. |
| `static void Pop()` | Restores the previous context from the stack. |
| `static void PushDuration(float enterTime, float evaluationTime)` / `PopDuration()` | Scope and restore one machine's duration timing, independently of scene context. |
| `static bool TryGetDurationTimes(out float enterTime, out float evaluationTime)` | Distinguish machine timing from standalone predicate evaluation. |

## See Also
- [StateMachine](StateMachine.md)
- [StatePredicate](StatePredicate.md)
- ← [StateMachine](README.md)
