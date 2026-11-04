# ConditionEntryPredicate

**What it is:** a transition predicate (`StatePredicate`) that evaluates a NeoCondition `ConditionEntry` (component, property, comparison, threshold) against a context GameObject chosen by `ConditionContextSlot`. The ScriptableObject stores only the slot index, never scene references. Path: `Scripts/StateMachine/ConditionEntryPredicate.cs`, namespace `Neo.StateMachine`.

**How to use:**
1. In a transition, **Add Condition → Neoxider Condition** (or `transition.AddPredicate(new ConditionEntryPredicate { ... })` in C#).
2. Configure the entry exactly like a NeoCondition row.
3. Pick **Context Slot**: `Owner` reads the State Machine's GameObject; `Override1..5` read the behaviour's **Context Overrides**.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Condition Entry** | Condition to check — object, component, property, comparison type, and threshold (same format as `NeoCondition`). |
| **Context Slot** | Where to get the `GameObject` for evaluation. `Owner` (0) — the object with `StateMachine`. `Override1..5` — from **Context Overrides** on `StateMachineBehaviour`. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `ConditionEntry ConditionEntry { get; set; }` | Condition (object, property, comparison, threshold). |
| `ConditionContextSlot ContextSlot { get; set; }` | Context slot: `Owner`, `Override1`..`Override5`. |

An unassigned, destroyed, or out-of-range override produces false before `IsInverted` is applied. It never falls back to Owner. Negative slot indices also produce false. For `Owner`, a live MonoBehaviour state can supply its GameObject when no wrapper context is active. Keep **Source Object** empty when using context slots; a configured explicit source belongs to `ConditionEntry`'s own resolution rules. An empty Source Object also works in a saved `StateMachineData` asset in Editor Play Mode: the Editor's placeholder for the unassigned field is not taken for a destroyed source (before 12.0 such predicates never passed in the Editor).

---

## Enum: ConditionContextSlot

| Value | Description |
|-------|-------------|
| `Owner` (0) | GameObject that owns the StateMachine. |
| `Override1` (1) | First entry in Context Overrides on StateMachineBehaviour. |
| `Override2..5` | Second through fifth entries. |

---

## Examples

### No-Code (Inspector)
1. In `StateTransition`, add a **Predicate** → `ConditionEntryPredicate`.
2. In **Condition Entry**, set up the check (e.g.: `HealthComponent` → `HpPercentValue` → `LessThan` → `0.3`).
3. **Context Slot** = `Owner` — check health on the state machine's object.
4. Transition fires when HP < 30%.

### Code
```csharp
ConditionEntryPredicate predicate = new ConditionEntryPredicate
{
    ConditionEntry = new ConditionEntry { /* setup */ },
    ContextSlot = ConditionContextSlot.Owner
};
transition.AddPredicate(predicate);
```

---

## See Also
- [NeoCondition](../Condition/NeoCondition.md) — condition system
- [StateTransition](StateTransition.md) — transition with predicates
- ← [StateMachine](README.md)
