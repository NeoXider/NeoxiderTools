# StateTransition

**Purpose:** Describes a transition between states in the state machine. Supports gated transitions via `StatePredicate` (AND logic — all predicates must pass), priority ordering, and two modes: code (CLR types) and No-Code (`StateData` references).

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Transition Name** | Debug/display name. Default `"Unnamed Transition"`. |
| **From State Data** | Source state (`StateData` reference). For No-Code machine. |
| **To State Data** | Target state (`StateData` reference). For No-Code machine. |
| **Priority** | Transition priority. Higher priority transitions are evaluated first. |
| **Is Enabled** | Whether this transition is active. If `false` — skipped during evaluation. |
| **Predicates** | `StatePredicate` list — all conditions must pass (AND). Empty = always allowed. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `Type FromStateType { get; set; }` | Source state type (code-driven). |
| `Type ToStateType { get; set; }` | Target state type (code-driven). |
| `StateData FromStateData { get; set; }` | Source state (No-Code). |
| `StateData ToStateData { get; set; }` | Target state (No-Code). |
| `string FromStateName { get; }` | Source `StateData` name. |
| `string ToStateName { get; }` | Target `StateData` name. |
| `List<StatePredicate> Predicates { get; }` | Transition conditions. |
| `int Priority { get; set; }` | Priority (higher = evaluated first). |
| `bool IsEnabled { get; set; }` | Enabled/disabled. |
| `string TransitionName { get; set; }` | Debug name. |
| `bool CanTransition(IState currentState)` | Check if transition is allowed from current state (type + predicates). |
| `bool Evaluate()` | Check predicates only (no source state check). |
| `bool EvaluatePredicates(IState currentState)` | Check predicates with current state context. |
| `void AddPredicate(StatePredicate)` | Add a condition. |
| `void RemovePredicate(StatePredicate)` | Remove a condition. |

`Priority` and `IsEnabled` can be changed after registration. After editing a source selector, unregister and register the transition again. CLR targets must be concrete compatible state classes with a public parameterless constructor; assign an asset to `ToStateData` for NoCode transitions. Unusable CLR targets are skipped so a lower priority valid transition can run.

Built-in duration predicates and composite children use a machine-owned timing scope when evaluated by `StateMachine`, so shared duration configurations are supported by default. Direct transition/predicate evaluation outside that scope uses the duration predicate's standalone anchor. Custom mutable predicate/action state is still shared; no graph cloning occurs. For a manual machine-time edit, use `machine.SetCurrentStateEnterTime(...)`; see [duration compatibility](StatePredicate.md).

---

## Examples

### No-Code (Inspector)
1. In `StateMachineData`, add an entry to **Transitions**.
2. Set **From State Data** = `Patrol`, **To State Data** = `Chase`.
3. In **Predicates**, add a `FloatComparisonPredicate` (e.g. distance < 10).
4. Set **Priority** = `1`.

### Code
```csharp
StateTransition transition = new StateTransition
{
    FromStateType = typeof(IdleState),
    ToStateType = typeof(ChaseState),
    Priority = 1
};
transition.AddPredicate(new FloatComparisonPredicate());
stateMachine.RegisterTransition(transition);
```

---

## See Also
- [StatePredicate](StatePredicate.md) — transition conditions
- [StateMachineData](NoCode/StateMachineData.md) — No-Code configuration
- [StateMachine](StateMachine.md) — state machine core
- ← [StateMachine](README.md)
