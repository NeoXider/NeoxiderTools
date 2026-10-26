# State Machine: Setup via StateMachineData

**What it is:** step-by-step setup of a state machine via `StateMachineData` (ScriptableObject), transition predicates, and the StateMachineBehaviour component. Logic (states, transitions) lives in the SO; scene object references for conditions are set on the component (Context for conditions).

**How to use:** follow the steps below: component in the scene → component settings → creating StateMachineData → states and transitions.

---
**Important:** a ScriptableObject cannot store references to scene objects — so the SO only configures logic (states, transitions, which property to check), while **which GameObject to read** is set on the component in the scene (the **Context for conditions** section).

The same rule applies to actions (`StateAction`): the SO stores only the action type and a context slot. To enable/disable a scene object, use `SetContextGameObjectActiveAction` with `Context Slot = Owner / Override1..5`. The old `SetGameObjectActiveAction` with a direct `GameObject target` is kept only for compatibility with old assets.

---

## 1. Component in the Scene

1. Select a GameObject (e.g., a character).
2. Add the component: **Component -> Neoxider -> Tools -> State Machine Behaviour**.
3. In `References`, assign the `StateMachineData`.

---

## 2. Component Settings

- `State Machine Data` — reference to the configuration (SO).
- **Context for conditions** — array of scene GameObjects for transition conditions. Element 0 = slot Override1, element 1 = Override2, … (up to 5). Scene references are set **only here**, not in the SO.
- `Auto Evaluate Transitions` — automatic transition evaluation every frame.
- `Show State In Insp` — display the current state in the inspector.
- `Enable Debug Log` — transition logging.
- `Exit Current State On Disable` — when the component is disabled, call `OnExit` of the current state and clear the current state.
- `Reload Data On Enable` — reload `StateMachineData` when the component is re-enabled.

Exit actions keep the same Owner and Override slots during disable, destruction, and explicit reload. For example, an `On Exit` → `SetContextGameObjectActiveAction` using Override1 can disable a scene effect assigned to element 0. Reload exits the old state before entering the configured initial state.

### 2.1 Component Events

The `Events` section:

- `On Initialized`
- `On State Entered`
- `On State Exited`
- `On State Changed` (`from`, `to`)
- `On Transition Evaluated` (`transitionName`, `result`)

### 2.2 Runtime controls

The `Controls` section:

- `Reload Data`
- `Evaluate Now`
- `Go To Initial State`
- `Change State` (manual state selection from the list)

---

## 3. StateMachineData Configuration

Select the `StateMachineData` asset and configure:

### 3.1 States (`States`)

- Add list elements.
- Assign a `StateData` to each element.
- In the `StateData`, configure the `On Enter`, `On Update`, `On Exit` actions.

### 3.2 Initial State (`Initial State`)

- Set the starting `StateData`.

### 3.3 Transitions (`Transitions`)

- Add transitions with the `Add Transition` button.
- For each transition, fill in:
  - `Name`
  - `From State`
  - `To State`
  - `Priority`
  - `Is Enabled`
- Click `Edit Conditions` to configure the conditions.

---

## 4. Transition Conditions

Conditions determine when a transition from one state to another is allowed. Without conditions, a transition is considered **always available** (fires first by priority).

### 4.1 Adding Conditions (everything in the SO, no scene references)

1. In the `StateMachineData` asset, click **Edit Conditions** on the desired transition.
2. In the **Edit Transition** window, click **Add Condition** → **Neoxider/Condition Entry**.
3. Configure the predicate **using only data in the SO**:
   - **Context Slot** — which object to read from: **Owner** = the object with the `StateMachineBehaviour`; **Override1** … **Override5** = elements from the **Context for conditions** list on the component in the scene (indices 0…4). The SO stores only the slot number, not a scene reference.
   - **Condition Entry** — same as in NeoCondition: **leave Source Object empty** (the context from the slot will be used), select the **Component** and **Property**, an operator, and a threshold.

Scene object references are set **on the component** in the inspector: the **Context for conditions** section — drag the needed GameObjects there (e.g., the player, an enemy, a point). In the conditions in the SO, select **Override1**, **Override2**, etc., to use those objects.

An empty or destroyed Override slot fails the condition before optional inversion. It does not read the Owner as a substitute. Check that every selected slot has the intended object.

All conditions of a transition are combined with **AND** logic.

### 4.2 Current State Properties for NeoCondition

A `ConditionEntry` can read properties of the `StateMachineBehaviourBase` component:

- `CurrentStateName` (string)
- `PreviousStateName` (string)
- `CurrentStateElapsedTime` (float)
- `StateChangeCount` (int)
- `HasCurrentState` (bool)

Example (in a Condition Entry in the SO):
- **Context Slot** = Owner
- Source Object = empty
- Source Mode = Component, Component = StateMachineBehaviourBase, Property = CurrentStateName
- Compare = Equal, Threshold String = "Run"

### 4.3 If Conditions Don't Fire

- **Context Slot** = Owner — reads from the object hosting the StateMachine. For a different object: add that object to **Context for conditions** on the component in the scene and select **Override1** (or Override2, … by index) in the condition.
- In the **Condition Entry**, leave **Source Object** empty — the context will come from the slot. Fill in only Component, Property, Compare, and the threshold.
- Make sure the transition's **From State** and **To State** match states in the States list.
- Enable **Enable Debug Log** and watch the console; **On Transition Evaluated** shows the result of the transition check.
- If logging is disabled, the StateMachine writes no runtime warnings/logs/errors to the console; use `On Transition Evaluated` and Events for UI/tools.

---

## 5. Example Idle/Run Scenario

- `idle` and `run` — two `StateData` assets.
- Transition `idle -> run`: condition `Speed > 0`.
- Transition `run -> idle`: condition `Speed <= 0`.
- `Auto Evaluate Transitions = ON`.

From C#, call `configuration.Validate(true)` before loading an asset to check its initial state and NoCode transition endpoints without console output. `LoadIntoStateMachine(machine)` only registers transitions; enter `configuration.InitialState` separately, or use the component's reload method to do both. Assigning `States = null` is treated as an empty configuration.

Multiple components can share built-in duration conditions: each machine owns its entry time, including duration children of AND/OR/NOT. Loading still shares custom mutable predicates/actions. In C#, use `component.StateMachine.SetCurrentStateEnterTime(...)` for a manual active-machine timing edit; `predicate.SetEnterTime(...)` remains standalone timing.

Lifecycle actions may request `ChangeState` or `Stop`: nested requests run FIFO after the current exit/enter/event sequence, with a limit of 128 requests per outer dispatch and a gated diagnostic when pending requests are discarded. Disabling the owner during entry completes that state's entered/changed events before the queued stop exits it. See the [core contract](StateMachine.md) for exception behavior.

---

## Summary

| What to do | Where |
|-------------|-----|
| Attach the state machine to an object | `StateMachineBehaviour` |
| Assign the configuration | `References -> State Machine Data` |
| Enable auto transitions | `Settings -> Auto Evaluate Transitions` |
| Configure states | `StateMachineData -> States` |
| Configure transitions | `StateMachineData -> Transitions` |
| Configure conditions | On a transition, click **Edit Conditions** → **Add Condition** → Neoxider Condition |
| Which object a condition reads from | **Context Slot** in the condition (Owner / Override1..5); objects for Override — in **Context for conditions** on the component in the scene |
| Subscribe to events | `StateMachineBehaviour -> Events` |
| Manual debugging | `StateMachineBehaviour -> Controls` (in Play Mode) |
