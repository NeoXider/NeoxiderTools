# QuestContext

**What it is:** an optional marker component (`Scripts/Quest/QuestContext.cs`) for the scene GameObject used to evaluate quest conditions. It stores no quest state and has no fields or automatic registration behavior.

**How to use:**
1. Add the component to your player or world object via `Add Component > Neoxider > Quest > QuestContext`.
2. Drag that GameObject into **Condition Context** on `QuestManager`. Adding the marker alone does not assign the context.
3. From C#, assign `manager.ConditionContext = playerObject`. The marker is optional for this API.

---

## Fields

None.

Start conditions and explicit `EvaluateObjectiveConditions()` calls use this context as the fallback for condition entries without an assigned source. If Condition Context is unassigned, the manager uses its own GameObject. Keep scene references on the manager/scene components; quest assets should describe reusable condition data.

## See Also

- [QuestManager](QuestManager.md)
- [Module Root](README.md)
