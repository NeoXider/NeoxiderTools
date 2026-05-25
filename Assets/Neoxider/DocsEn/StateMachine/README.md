# StateMachine module

State machine runtime (IState, transitions), no-code ScriptableObject config, and visual editor. Scripts in `Scripts/StateMachine/`. Use this page as the English module entry.

## Current Architecture

- `StateMachine<TState>` is the C# runtime core. Use it directly when you do not need a scene component.
- `StateMachineBehaviour<TState>` and `StateMachineBehaviourBase` are scene wrappers: ticking, context slots, UnityEvents, reload, and enable/disable lifecycle.
- `StateMachineData` / `StateData` assets must not store scene object references. Put scene objects on the component context overrides and reference them from SO data by slot.
- Use `SetContextGameObjectActiveAction` for scene-object actions. `SetGameObjectActiveAction` is legacy compatibility for old assets with direct scene references.
- Runtime logs are gated by `Enable Debug Log` on the component.
- On disable, the component exits the current state; on re-enable it can reload `StateMachineData`.

## Russian docs

| Page | Description |
|------|-------------|
| [StateMachine README](../../Docs/StateMachine/README.md) | Module overview and when to use code-first vs no-code |
| [StateMachine](../../Docs/StateMachine/StateMachine.md) | Core runtime API |
| [StateMachineBehaviour](../../Docs/StateMachine/StateMachineBehaviour.md), [StateMachineBehaviourBase](../../Docs/StateMachine/StateMachineBehaviourBase.md) | MonoBehaviour integration |
| [NoCode_StateMachine_Usage](../../Docs/StateMachine/NoCode_StateMachine_Usage.md) | No-code workflow |

## See also

- [Condition](../Condition/README.md)
