# Inspector Scenarios

**Purpose:** Step-by-step scenarios for setting up quests in the Inspector: `QuestManager`, `QuestNoCodeAction`, `NeoCondition`. Which objects to create, where to attach components, and what to wire in `UnityEvent`.

**How to use:** Make sure the scene contains a `QuestManager` with the needed `QuestConfig`s in **Known Quests**; assign a **Condition Context** if quests have Start Conditions. Choose a scenario below and follow the steps; verify with the Inspector buttons or in Play mode.

---

## Scenario 1: Accept a Quest on Button Press

**Goal:** accept a selected quest when the player clicks a UI button.

1. Select the GameObject with a `Button` component (or the panel that holds the button).
2. **Add Component > Neoxider > Quest > Quest NoCode Action**.
3. Set **Action Type** to `Accept` and drag your `QuestConfig` into the **Quest** field.
4. In the Button's **On Click ()**, add a call: Object = this same GameObject, Function = **QuestNoCodeAction → Execute()**.

**Verify:** click **[Execute Action]** in the `QuestNoCodeAction` Inspector, or press the button in Play mode.  
If the quest is not accepted: check that the config is in **Known Quests**, the ID is not empty, the quest has not already been accepted, and all Start Conditions evaluate to `true` for the Condition Context.

---

## Scenario 2: Complete an Objective When a Condition Is Met (NeoCondition)

**Goal:** when a condition (health, counter, flag) becomes true — complete one quest objective.

1. Select or create a `GameObject`.
2. **Add Component > Neoxider > Condition > NeoCondition**. Configure **Conditions** (object, component, property, operator, threshold) per [NeoCondition](../Condition/NeoCondition.md).
3. On the same object: **Add Component > Neoxider > Quest > Quest NoCode Action**.
4. Set **Action Type** = `CompleteObjective`, **Quest** = your `QuestConfig`, **Objective Index** = the target index (0, 1, 2, …).
5. In `NeoCondition` under **Events → On True**, add a call: Object = this object, Function = **QuestNoCodeAction → Execute()**.

When `NeoCondition.On True` fires for the first time, the manager marks the specified objective complete.  
**Verify:** use the **[Execute Action]** button on `QuestNoCodeAction`.

---

## Scenario 3: Show UI When a Quest Is Completed

**Goal:** when any quest is completed (all objectives done), show a panel or play an animation.

1. Select `QuestManager` in the scene.
2. In the Inspector, find **On Any Quest Completed** (`UnityEvent` with no arguments).
3. Add a call: drag the UI object (panel, popup), choose the show/animation function. No parameters are passed.

If you need different reactions per `questId`: use **On Quest Completed** (`UnityEvent<string>`) and wire a method on your script that accepts one `string` parameter.

---

## Scenario 4: React to Quest Acceptance (Sound, Hint)

1. On `QuestManager`, find **On Any Quest Accepted** (no arguments).
2. Add a call: sound, animation, tooltip display. Fires on any quest acceptance.

---

## Scenario 5: Update UI When a Single Objective Is Completed

**Goal:** when one objective is completed (e.g. "collect the key") — immediately check it off in the objectives list.

1. On `QuestManager`, find **On Objective Completed** (`UnityEvent<string, int>`: questId, objectiveIndex).
2. Wire a method on your script with two parameters (`string`, `int`). In the method, use `questId` and `objectiveIndex` to update the correct UI element.

---

## Scenario 6: React to a Quest Failure

1. On `QuestManager`, find **On Quest Failed** (`UnityEvent<string>`).
2. Wire a call: "Quest failed" message, hide the quest from the journal, play a sound, etc. This event fires only when `FailQuest` is called explicitly (from code or another component).

---

## Scenario 7: Restart a Failed or Completed Quest

1. Add a "Restart" button to your UI.
2. In the button handler, call `QuestManager.RestartQuest(quest)` or `RestartQuest(questId)`.
3. The manager resets the old quest state and re-attempts acceptance with Start Condition checks.

---

## Scenario 8: Reset All Quests (New Game)

1. Add a "Reset All Quests" button to your menu/settings.
2. In the button handler, call `QuestManager.ResetAllQuests()`.
3. Afterwards, your UI re-reads state via `GetState`/`AllQuests` and shows all quests as `NotStarted`.

---

## Scenario 9: Linear vs. Independent Quests

1. Create a `QuestFlowConfig` and add quests to **Chains** (for linear sequences) and **Standalone Quests** (for independent quests).
2. For a linear chain, enable **Strict Order**.
3. Before calling `AcceptQuest` in the UI, check availability via `QuestFlowConfig.CanAcceptQuest(...)`.
4. In the journal, mark blocked quests as `Locked` (until the previous quest is completed).

---

## Scenario 10: Full No-Code Quest Management (via QuestNoCodeAction)

1. Add a `QuestNoCodeAction` component to a UI button.
2. Choose an **Action Type**:
   - `Accept` — accept a quest
   - `CompleteObjective` — complete an objective
   - `Fail` — fail a quest
   - `Restart` — restart a quest
   - `Reset` — reset one quest
   - `ResetAll` — reset all quests
3. Assign **Quest** (and **Objective Index** if needed).
4. Optionally assign a **Flow Config** to check sequence order on `Accept`.
5. In **Button.OnClick**, call `QuestNoCodeAction.Execute()`.
6. Wire **On Success / On Failed / On Result Message** to your UI (logs, popups, indicators).

---

## Inspector Buttons

- **QuestManager**, Editor block: **Editor Quest Id**, **Editor Objective Index**, and the buttons **Accept Quest (Editor Id)** / **Complete Objective (Editor)** — test acceptance and objective completion without gameplay actions.
- **QuestNoCodeAction:** **[Execute Action]** for the selected Action Type.

---

## Limitations

- Different reactions for different quests (by `questId`) — requires a method on your script with a `string` parameter, wired to **On Quest Completed**.
- Checking "is a quest available?" for display purposes — there is no "evaluate without accepting" method in the module; implement it in code (evaluate conditions or use custom flags).
- Complex reward distribution or dialogue branching — easier to handle in code via the `QuestCompleted` / `ObjectiveProgress` events.
