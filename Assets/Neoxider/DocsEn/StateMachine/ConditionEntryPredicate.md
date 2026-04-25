# ConditionEntryPredicate

**Purpose:** State machine transition predicate using `NeoCondition`-style evaluation. Evaluates a `ConditionEntry` (component, property, comparison, threshold) against a context GameObject. Context slot is set via `ConditionContextSlot` — the ScriptableObject stores only the slot index, not scene references.

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
var predicate = new ConditionEntryPredicate
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
