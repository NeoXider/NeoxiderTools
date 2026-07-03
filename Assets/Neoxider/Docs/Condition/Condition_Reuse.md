# Reusing Conditions in Other Systems

**What it is:** Neoxider conditions (object → component → property → comparison → threshold) are designed to be **universal**: the same condition type can be configured and used not only in **NeoCondition** but also in **St...

**How to use:** see the sections below.

---


Neoxider conditions (object → component → property → comparison → threshold) are designed to be **universal**: the same condition type can be configured and used not only in **NeoCondition** but also in the **State Machine**, triggers, quests, and any of your own systems.

## Contract: IConditionEvaluator

All "conditions" in Neoxider conform to a single interface:

```csharp
namespace Neo.Condition
{
    public interface IConditionEvaluator
    {
        /// <param name="context">Owner GameObject (fallback when the source is empty).</param>
        /// <returns>true if the condition is met.</returns>
        bool Evaluate(GameObject context);
    }
}
```

- **NeoCondition** stores a list of `ConditionEntry` and calls `entry.Evaluate(gameObject)`.
- The **State Machine** uses the `ConditionEntryPredicate` predicate, which internally calls `conditionEntry.Evaluate(context)`.
- Any custom system can accept an `IConditionEvaluator` (or specifically a `ConditionEntry`) and call `Evaluate(context)` with a suitable context.

The context (`GameObject context`) is used as a fallback when the condition has no source set (Source Object is empty or the object has not yet been found by name).

---

## Where Conditions Are Already Reused

| System | How it is wired | Context |
|--------|-------------------|----------|
| **NeoCondition** | A `List<ConditionEntry>`, AND/OR logic, On True/On False events | `gameObject` (the NeoCondition owner) |
| **State Machine** | Transition predicate `ConditionEntryPredicate`: a `ConditionEntry` field + an optional `contextObject` | `contextObject` or `(currentState as MonoBehaviour)?.gameObject` |

In the State Machine: **Add Condition → Neoxider Condition**, then configure a single condition (source, component, property, comparison, threshold) exactly as in NeoCondition.

---

## How to Add Conditions to Your Own System

### 1. Reference the Neo.Condition assembly

In your assembly's `.asmdef`, add a reference to `Neo.Condition` (the assembly GUID can be taken from `Assets/Neoxider/Scripts/Condition/Neo.Condition.asmdef.meta`).

### 2. Store a condition or a list of conditions

Options:

- **A single condition:** a field of type `ConditionEntry` (serialized by Unity).
- **Multiple conditions:** a `List<ConditionEntry>`; combine the results (AND/OR) during evaluation, just like NeoCondition does.
- **Abstraction:** a field of a type implementing `IConditionEvaluator`; in the Inspector this will most often be a `ConditionEntry`, since it is serializable and drawn by our editor.

Example for a single condition:

```csharp
using Neo.Condition;
using UnityEngine;

public class MyTrigger : MonoBehaviour
{
    [SerializeField] private ConditionEntry condition;

    public bool Check()
    {
        if (condition == null) return true;
        return condition.Evaluate(gameObject); // context = this object
    }
}
```

Example for a list (AND):

```csharp
[SerializeField] private List<ConditionEntry> conditions = new();

public bool CheckAll()
{
    if (conditions == null || conditions.Count == 0) return true;
    foreach (var c in conditions)
    {
        if (c == null) continue;
        if (!c.Evaluate(gameObject)) return false;
    }
    return true;
}
```

### 3. Choosing the context (GameObject)

In `Evaluate(context)`, pass the GameObject that should be substituted when the condition has no **Source Object** set and does not use **Find By Name**:

- Usually this is the "owner" of the logic: for example, the `gameObject` of the component that checks the conditions.
- In the State Machine, the context is either an explicitly assigned object or the object of the current state (`currentState as MonoBehaviour`).

If your component lives on one object but the check should be performed "on behalf of" another (for example, an NPC), pass that other object to `Evaluate`.

### 4. Editor (Inspector)

A **CustomPropertyDrawer** (`ConditionEntryDrawer`) is already provided for `ConditionEntry` fields. As soon as your component has a `ConditionEntry` or `List<ConditionEntry>` field, Unity automatically renders the same condition setup block (source, component, property, comparison, threshold) as in NeoCondition and in State Machine transitions.

Nothing extra is needed in a custom editor: just declare the field and render it via `EditorGUILayout.PropertyField(serializedProperty)` or the default Inspector.

---

## State Machine Predicate (Integration Example)

To use a condition **in State Machine transitions** without writing any code:

1. Open a transition (State Machine Data → transition).
2. **Add Condition → Neoxider Condition**.
3. In the block that appears, configure a single condition (just like in NeoCondition).
4. Optionally set a **Context Object** (if empty, the context will be the GameObject of the current state).

No implementation is required on your side: the `ConditionEntryPredicate` predicate and the transition editor already support this.

---

## Summary

| Task | Action |
|--------|----------|
| Use a single condition in your system | A `ConditionEntry` field, call `entry.Evaluate(contextGameObject)`. |
| Multiple conditions (AND/OR) | A `List<ConditionEntry>`, a loop calling `Evaluate`, combine results with your own logic. |
| Context | Pass to `Evaluate` the GameObject that should serve as the fallback when the source is empty. |
| Inspector | A `ConditionEntry` field (or a list) is enough; the UI is provided by `ConditionEntryDrawer`. |
| State Machine | Add a condition via **Add Condition → Neoxider Condition** and configure the entry. |
| Assembly | Add a reference to `Neo.Condition` in your assembly's asmdef. |

For details on configuring a single condition (Source, Component, Property, Compare, threshold), see [NeoCondition.md](./NeoCondition.md).
