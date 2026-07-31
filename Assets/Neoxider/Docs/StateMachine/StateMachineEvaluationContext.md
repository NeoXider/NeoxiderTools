# StateMachineEvaluationContext

**Purpose:** `internal static` transition evaluation context for `StateMachine` predicates. Not user-facing API — it is consumed internally by NoCode predicates to resolve the owning `GameObject` and slot-based context overrides during evaluation. A thread-static stack lets nested/recursive evaluations push and pop context without interfering with each other.

## API

| Member | Description |
|--------|-------------|
| `static GameObject CurrentContextObject { get; }` | The GameObject of the state machine currently being evaluated. |
| `static GameObject GetContextBySlot(int slot)` | Resolves context by slot: `0` = owner (GameObject with State Machine), `1+` = entry from Context Overrides on the component. |
| `static void Push(GameObject contextObject, IReadOnlyList<GameObject> overrides = null)` | Pushes a new context (and its override list) onto the stack. |
| `static void Pop()` | Restores the previous context from the stack. |

## See Also
- [StateMachine](StateMachine.md)
- [StatePredicate](StatePredicate.md)
- ← [StateMachine](README.md)
