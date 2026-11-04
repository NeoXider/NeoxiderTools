# StateTransition

**What it is:** a `[Serializable]` transition rule between states: source/target (CLR types for code states or `StateData` assets for NoCode), priority, enabled flag and `StatePredicate`s combined with AND. Path: `Scripts/StateMachine/StateTransition.cs`, namespace `Neo.StateMachine`.

**How to use:**
1. NoCode: add an entry to `StateMachineData` **Transitions**, set **From/To State Data**, add predicates.
2. Code: create a `StateTransition` with `FromStateType` / `ToStateType`, add predicates and call `stateMachine.RegisterTransition(transition)` (or the behaviour's `RegisterTransition`).
3. Give transitions a **Transition Name**; unnamed ones are reported as a cached `"From -> To"` label by `StateMachineBehaviourBase.OnTransitionEvaluated`.

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
| `int Priority { get; set; }` | Priority (higher = evaluated first; equal priorities keep registration order). |
| `bool IsEnabled { get; set; }` | Enabled/disabled. |
| `string TransitionName { get; set; }` | Debug name. |
| `bool CanTransition(IState currentState)` | Check if transition is allowed from current state (type + predicates). |
| `bool Evaluate()` | Check predicates only (no source state check). |
| `bool EvaluatePredicates(IState currentState)` | Check predicates with current state context. |
| `void AddPredicate(StatePredicate)` | Add a condition. |
| `void RemovePredicate(StatePredicate)` | Remove a condition. |
| `void NotifyFromStateEntered(float enterTime)` | Forwards a source-state entry to every predicate's `OnOwnerStateEntered` (the machine calls it only for transitions leaving the entered state). |
| `static void MarkRoutingChanged()` | Invalidates the sorted transition lists of every machine. Setters and Unity deserialization already call it; use it after writing the serialized priority/source fields another way (reflection, a custom serializer). |

`Priority` and `IsEnabled` can be changed after registration. A changed `Priority`, `FromStateType` or `FromStateData` (setter, Inspector, Undo) bumps a process-wide routing version, and each machine revalidates its cached list on the next lookup instead of scanning priorities on every evaluation. Moving `FromStateData` to another asset, changing `FromStateType` or clearing it (the transition becomes source-less) works without re-registering: each machine reads the current selectors when it rebuilds a list. CLR targets must be concrete compatible state classes with a public parameterless constructor; assign an asset to `ToStateData` for NoCode transitions. Unusable CLR targets are skipped so a lower priority valid transition can run.

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
