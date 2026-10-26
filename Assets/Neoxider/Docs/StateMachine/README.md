# StateMachine module

State machine runtime (IState, transitions), no-code ScriptableObject config, and visual editor. Scripts in `Scripts/StateMachine/`. Use this page as the English module entry.

Demo in the current release layout: `Samples~/Demo/Scenes/StateMachine/StateMachineDemo.unity`, controller `Samples~/Demo/Scripts/Shell/StateMachineDemoController.cs`. During sample development, these paths use `Samples/` instead of `Samples~/`. Package Manager imports Demo Scenes into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/` in the consuming project. The demo UI is created at runtime by `NeoDemoShell`.

## Current Architecture

- `StateMachine<TState>` is the C# runtime core. Use it directly when you do not need a scene component.
- `StateMachineBehaviour<TState>` and `StateMachineBehaviourBase` are scene wrappers: ticking, context slots, UnityEvents, reload, and enable/disable lifecycle.
- `StateMachineData` / `StateData` assets must not store scene object references. Put scene objects on the component context overrides and reference them from SO data by slot.
- Use `SetContextGameObjectActiveAction` for scene-object actions. `SetGameObjectActiveAction` is legacy compatibility for old assets with direct scene references.
- Runtime logs are gated by `Enable Debug Log` on the component.
- On disable, the component exits the current state; on re-enable it can reload `StateMachineData`.
- Enter, update, and exit actions receive the component's owner and override slots, including exit during disable, destruction, and reload.
- Transition priorities can change at runtime. After changing a source selector, unregister and register the transition again to rebuild its source index.
- A missing condition override returns false before optional predicate inversion; it does not substitute the owner. `StateMachineData.Validate(true)` returns false for missing initial state or invalid NoCode endpoints without logging.

## Choosing a workflow

For code, implement `IState`, register `StateTransition` rules, and call `Update()` and `EvaluateTransitions()` on `StateMachine<IState>`. For scene authoring, attach `StateMachineBehaviourBase`, assign `StateMachineData`, and put scene references in **Context for conditions**. See the [runtime API](StateMachine.md) and [NoCode guide](NoCode_StateMachine_Usage.md).

`StateMachineData.LoadIntoStateMachine` registers the configured transition instances. Built-in duration predicates, including children of AND/OR/NOT, use each machine's own entry time by default, so components can share a duration configuration. Custom mutable predicates/actions are still shared; loading does not clone the graph.

`ChangeState` and `Stop` called from lifecycle callbacks are queued FIFO until the current exit/enter/events finish. Each outer dispatch processes at most 128 requests, then discards pending requests with a gated diagnostic. Normal calls remain synchronous. See the [lifecycle and timing contracts](StateMachine.md) for ordering, exception behavior, clock injection and the addressed `SetCurrentStateEnterTime` migration.

В 11.0 длительность состояния изолирована по машине по умолчанию. Для ручной правки времени активной машины используйте `machine.SetCurrentStateEnterTime(...)`; `predicate.SetEnterTime(...)` сохраняет контракт самостоятельного вычисления. Вложенные смены состояния выполняются после текущей последовательности lifecycle-событий.

## docs

| Page | Description |
|------|-------------|
| [README](./README.md) | Module overview and when to use code-first vs no-code |
| [StateMachine](./StateMachine.md) | Core runtime API |
| [StateMachineBehaviour](./StateMachineBehaviour.md), [StateMachineBehaviourBase](./StateMachineBehaviourBase.md) | MonoBehaviour integration |
| [NoCode_StateMachine_Usage](./NoCode_StateMachine_Usage.md) | No-code workflow |
| [Editor](./Editor.md) | Visual graph editor and authoring tools |

## See also

- [Condition](../Condition/README.md)
