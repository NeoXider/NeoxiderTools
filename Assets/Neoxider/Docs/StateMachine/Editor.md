# State Machine editor tools

The `Neo.StateMachine.Editor` assembly provides the inspectors for `StateData`, `StateMachineData`, and `StateMachineBehaviourBase`, plus the transition condition window. Start with the [module overview](README.md) for the runtime C# API or the [scene setup guide](NoCode_StateMachine_Usage.md) for a complete asset workflow.

## Configure assets and scene context

1. Create state and machine assets through **Assets > Create > Neoxider > State Machine**.
2. In each `StateData`, add actions under **On Enter**, **On Update**, or **On Exit**. An added action has its own configuration. Available action types must be serializable, concrete, non-generic classes with a public parameterless constructor. A failing constructor leaves the list unchanged and reports an editor error.
3. In `StateMachineData`, assign **States** and **Initial State**, then add transitions and set their endpoints, priority, and enabled flag.
4. Click **Edit Conditions** on one transition. Use **Add Condition**, then expand the condition's **Settings** to edit its serialized fields. This includes names, inversion, boolean values, comparison operands and thresholds, duration settings, and fields on custom serializable predicates.
5. For **Neoxider/Condition Entry**, configure **Context Slot** and **Condition Entry** under Settings. Put scene objects on the component's **References > Context Overrides** array: element 0 is `Override1`, element 1 is `Override2`, through element 4 for `Override5`. `Owner` uses the GameObject hosting the component. Leave the condition's source object empty when using a context slot.

Both Add menus accept concrete, serializable, non-generic classes with a public parameterless constructor. Put `[Serializable]` on each concrete custom action or predicate. Implement `StateAction.Execute()` for an action, or `StatePredicate.EvaluateInternal(IState)` for a predicate that uses the base class's inversion behavior. The menu creates a new instance; configure its serialized fields in the inspector. Runtime delegates configured through `CustomPredicate.SetEvaluator` are not serialized asset configuration.

Keep scene references on `StateMachineBehaviourBase`. Reusable state and machine assets store configuration and context slot selections. Use `SetContextGameObjectActiveAction` for scene object activation through a slot.

The component inspector also exposes events, read-only runtime state, and **Reload Data**, **Evaluate Now**, **Go To Initial State**, and **Change State** controls. Runtime controls are available in Play Mode.

## Undo, selection, and validation

Transition names, enabled flags, priorities, predicate fields, and condition additions/removals support Unity Undo/Redo. Action additions use Unity's serialized property Undo workflow. Changes dirty the owning asset so normal project saving persists them.

The transition window checks that its selected transition still belongs to its owner. When Undo reconstructs that object, or the transition is removed, reopen **Edit Conditions** from the inspector. The window refuses to edit a detached transition or a different row. Selection must also be reopened after a domain reload.

The machine inspector supports editing multiple assets. Its validation result combines `StateMachineData.Validate()` results for every selected asset. **Edit Conditions** is available only with one machine asset selected, so each condition edit has an unambiguous owner. Manual validation first applies pending inspector edits.

For a shared machine asset, edit conditions with that single asset selected, then return to multi-selection for common configuration changes. Runtime timing and callback contracts are described in the [runtime API](StateMachine.md) and [predicate reference](StatePredicate.md); the transition window edits their serialized configuration.

## Open the window from editor C#

Custom editor tools can call the existing public API. An editor assembly using this method needs a reference to `Neo.StateMachine.Editor`; runtime assemblies use `Neo.StateMachine` independently.

```csharp
#if UNITY_EDITOR
using Neo.StateMachine.NoCode;
using Neo.StateMachine.NoCode.Editor;

public static class StateMachineAuthoring
{
    public static void OpenConditions(StateMachineData configuration, int transitionIndex)
    {
        if (configuration == null || configuration.Transitions == null ||
            transitionIndex < 0 || transitionIndex >= configuration.Transitions.Count ||
            configuration.Transitions[transitionIndex] == null)
        {
            return;
        }

        TransitionEditorWindow.ShowWindow(configuration.Transitions[transitionIndex], configuration);
    }
}
#endif
```

Pass the asset that owns the transition to enable serialized field editing, persistence, and Undo. The legacy ownerless call can edit in-memory transition details and inversion; it has no asset persistence or Undo target. Custom predicates should expose serializable configuration and implement `StatePredicate.EvaluateInternal`; delegates on `CustomPredicate` remain a runtime C# configuration path.

With an owner asset, `ShowWindow` requires the supplied transition to belong to that asset. After its owner is destroyed, the window refuses further edits. When both arguments are valid, the existing window selects that transition; callers do not need to register custom editors manually.

`StateMachineEditorRegistrar.IsInitialized` reports that its delayed editor setup pass has completed. To validate a configuration from editor C#, call `configuration.Validate(true)` for a result without log output, or use **Validate Configuration** in the asset inspector. The setup flag is not an asset validation result.

## Related API and scene guides

- [StateData](NoCode/StateData.md): state identity and enter/update/exit action lists.
- [StateMachineData](NoCode/StateMachineData.md): state/transition configuration and runtime loading.
- [StateMachineBehaviourBase](StateMachineBehaviourBase.md): scene context, events, and runtime controls.
- [ConditionEntryPredicate](ConditionEntryPredicate.md): context slots and condition configuration.
- [StateAction](StateAction.md) and [StatePredicate](StatePredicate.md): typed C# extension contracts.
