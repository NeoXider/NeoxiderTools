# StateMachine module

**What it is:** State machine runtime (IState, transitions), no-code ScriptableObject config, and visual editor. Scripts in `Scripts/StateMachine/`. Use this page as the English module entry.

<!-- demo-shots:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/StateMachineDemo.jpg" alt="StateMachineDemo: a C# state machine agent next to a NoCode guard" width="100%" /></p>

*Demo scene: `Scenes/StateMachine/StateMachineDemo.unity`, in the **Demo Scenes** sample (Package Manager > NeoxiderTools > Samples). The scenes are saved with their UI and wiring, so you can inspect them without entering Play Mode.*
<!-- demo-shots:end -->

**Contents:** [current architecture](#current-architecture), [scene wrapper API](#scene-wrapper-api),
[choosing a workflow](#choosing-a-workflow) and the [page index](#docs).

**Navigation:** [← Docs](../README.md)

---

Demo in the current release layout: `Samples~/Demo/Scenes/StateMachine/StateMachineDemo.unity`, controller `Samples~/Demo/Scripts/Shell/StateMachineDemoController.cs`. During sample development, these paths use `Samples/` instead of `Samples~/`. Package Manager imports Demo Scenes into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/` in the consuming project. The scene is authored in the Editor by `StateMachineDemoBuilder` (`Samples~/Demo/Editor/Builders/StateMachine/`) and saved with its UI; nothing is built at runtime. The **Agent** shows the C# path (`StateMachine<IState>` driven by `StateMachineDemoController`); the **Guard** shows the NoCode path: a `StateMachineBehaviourBase` playing `Data/StateMachine/Guard StateMachine.asset` (Sleep / Alert), where Sleep → Alert reads the Noise toggle through a `ConditionEntryPredicate` in context slot Override1 and Alert → Sleep also needs 1.5 s in Alert (`StateDurationPredicate`).

## Current Architecture

- `StateMachine<TState>` is the C# runtime core. Use it directly when you do not need a scene component.
- `StateMachineBehaviour<TState>` and `StateMachineBehaviourBase` are scene wrappers: ticking, context slots, UnityEvents, reload, and enable/disable lifecycle.
- `StateMachineData` / `StateData` assets must not store scene object references. Put scene objects on the component context overrides and reference them from SO data by slot.
- Use `SetContextGameObjectActiveAction` for scene-object actions. `SetGameObjectActiveAction` is legacy compatibility for old assets with direct scene references.
- Runtime logs are gated per machine. A component's **Enable Debug Log** (C#: `EnableDebugLog`) drives its own logs and its machine's `DebugLogEnabled`, which also gates the NoCode actions that machine runs (`LogStateAction`, action errors). Another component's setting no longer changes them. A plain `StateMachine<TState>` follows the shared switch (the last enabled component) until you assign `DebugLogEnabled`.
- On disable, the component exits the current state; on re-enable it can reload `StateMachineData`.
- Enter, update, and exit actions receive the component's owner and override slots, including exit during disable, destruction, and reload.
- Transition priorities and sources can change at runtime. Moving `FromStateData` to another asset, or changing `FromStateType` (including clearing it to make the transition source-less), takes effect on the next evaluation without re-registering.
- A NoCode machine evaluates only the transitions leaving the active `StateData` (plus source-less ones): per-frame cost is O(outgoing transitions), `OnTransitionEvaluated` reports only those, and predicate entry hooks run only for transitions leaving the entered state. Equal priorities keep registration order. Sorted lists are revalidated through `StateTransition.RoutingVersion`, so a warm evaluation neither re-sorts nor allocates.
- A missing condition override returns false before optional predicate inversion; it does not substitute the owner. `StateMachineData.Validate(true)` returns false for missing initial state or invalid NoCode endpoints without logging; `Validate(false)` (the Inspector's **Validate Configuration** button) always writes those problems to the console.
- A `ConditionEntryPredicate` whose **Source Object** is empty reads its context slot also from a saved asset in Editor Play Mode (before 12.0 the Editor's placeholder for the empty field was taken for a destroyed source, so such predicates never passed).

## Scene wrapper API

- `StateMachineBehaviourBase.ChangeState(StateData)` enters a specific asset (also one not listed in the data); `ChangeState(string)` resolves a name in `StateMachineData`. A literal `ChangeState(null)` is ambiguous at compile time.
- C# event accessors on `StateMachineBehaviourBase`: `OnInitialized`, `OnStateEntered`, `OnStateExited`, `OnStateChanged(previous, current)`, `OnTransitionEvaluated(name, result)`, plus `CurrentStateName`, `PreviousStateName`, `StateChangeCount`, `CurrentStateElapsedTime`, `HasCurrentState`.
- The generic `StateMachineBehaviour<TState>` exposes `EvaluateTransitionsNow()`, `AutoEvaluateTransitions`, `StateMachineData` and `ContextOverrides`; its `initialStateTypeName` accepts a full (`Namespace.State`) or assembly-qualified type name.
- Both wrappers expose `EnableDebugLog`; setting it (or the Inspector toggle in Play Mode) updates the running machine's `DebugLogEnabled`.
- The `StateMachine` property creates the machine on first access, so code can configure it before `Awake`; `Awake` keeps that instance.
- Loading `StateMachineData` enters the assigned `InitialState` asset itself, not the first state with the same name.
- The wrappers' `Update` / `FixedUpdate` / `LateUpdate` and transition evaluation add no per-frame allocations.

## Choosing a workflow

For code, implement `IState`, register `StateTransition` rules, and call `Update()` and `EvaluateTransitions()` on `StateMachine<IState>`. For scene authoring, attach `StateMachineBehaviourBase`, assign `StateMachineData`, and put scene references in **Context for conditions**. See the [runtime API](StateMachine.md) and [NoCode guide](NoCode_StateMachine_Usage.md).

`StateMachineData.LoadIntoStateMachine` registers the configured transition instances. Built-in duration predicates, including children of AND/OR/NOT, use each machine's own entry time by default, so components can share a duration configuration. Custom mutable predicates/actions are still shared; loading does not clone the graph.

`ChangeState` and `Stop` called from lifecycle callbacks are queued FIFO until the current exit/enter/events finish. Each outer dispatch processes at most 128 requests, then discards pending requests with a gated diagnostic. Normal calls remain synchronous. See the [lifecycle and timing contracts](StateMachine.md) for ordering, exception behavior, clock injection and the addressed `SetCurrentStateEnterTime` migration.

В 11.0 длительность состояния изолирована по машине по умолчанию. Для ручной правки времени активной машины используйте `machine.SetCurrentStateEnterTime(...)`; `predicate.SetEnterTime(...)` сохраняет контракт самостоятельного вычисления. Вложенные смены состояния выполняются после текущей последовательности lifecycle-событий.

If a target state's constructor changes or stops the machine, that newer choice wins: the interrupted activation does not enter or cache its target and does not fall through to a lower-priority automatic rule. `TryChangeState<T>()` returns false for it.

Если конструктор нового состояния меняет активное состояние машины, в том числе останавливает его, этот более новый выбор
сохраняется. Прерванная активация не входит в старый target, не кладёт его в кэш и не продолжает
старую цепочку автоматических переходов. `TryChangeState<T>()` возвращает false для такой активации.

NoCode-переходы ищутся по конкретному ассету `StateData`: кадр проверяет только переходы, выходящие из активного состояния (и переходы без источника), `OnTransitionEvaluated` сообщает только о них, а `OnOwnerStateEntered` вызывается только для переходов из вошедшего состояния. При равном приоритете сохраняется порядок регистрации. Смена `FromStateData` на другой ассет и смена (или сброс) `FromStateType` действуют со следующей проверки, перерегистрировать переход не нужно.

Логи включаются для каждой машины отдельно: **Enable Debug Log** компонента (`EnableDebugLog` в C#) управляет его логами, предупреждениями его машины (`DebugLogEnabled`) и выводом NoCode-действий, которые выполняет эта машина (`LogStateAction`). Компонент, включённый позже с другой настройкой, на них больше не влияет. `Validate(false)` (кнопка **Validate Configuration**) всегда пишет найденные проблемы в консоль.

Automatic transitions to a CLR type (`ToStateType`) check the target type once per machine, including targets it rejects. With state caching on (the default), repeated transitions between cached states use no reflection, and the machine itself does not allocate; your predicates, states and event listeners may. Автоматические переходы по типу проверяют тип один раз на машину, в том числе отклонённый; при включённом кэше состояний повторные переходы не используют reflection, и сама машина не выделяет память (ваши предикаты, состояния и подписчики событий могут).

## docs

| Page | Description |
|------|-------------|
| [README](./README.md) | Module overview and when to use code-first vs no-code |
| [StateMachine](./StateMachine.md) | Core runtime API |
| [StateMachineBehaviour](./StateMachineBehaviour.md), [StateMachineBehaviourBase](./StateMachineBehaviourBase.md) | MonoBehaviour integration |
| [NoCode_StateMachine_Usage](./NoCode_StateMachine_Usage.md) | No-code workflow |
| [Editor](./Editor.md) | Visual graph editor and authoring tools |
| [IState](./IState.md), [StateTransition](./StateTransition.md) | State contract and transition rules |
| [StateAction](./StateAction.md), [StateCondition](./StateCondition.md), [StatePredicate](./StatePredicate.md), [ConditionEntryPredicate](./ConditionEntryPredicate.md) | NoCode actions, conditions and predicates |
| [StateMachineData](./NoCode/StateMachineData.md), [StateData](./NoCode/StateData.md) | NoCode ScriptableObject assets |
| [StateMachineEvaluationContext](./StateMachineEvaluationContext.md) | Internal owner/context scope for predicates and actions |

## See also

- [Condition](../Condition/README.md)
