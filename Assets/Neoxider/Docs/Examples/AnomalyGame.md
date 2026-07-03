# Example: Anomaly Game

**What it is:** A step-by-step example of building an anomaly game (survival, random spawning, fixing anomalies, win/lose) using NeoxiderTools components.

**How to use:** Follow the steps in sections 1–5; the required components are listed below.

---

## What You Need

- **TimerObject** (2 instances): one for the time-to-victory (e.g., survive 6 hours), the other for the anomaly spawn interval.
- **Selector** + **SelectorItem** on each anomaly object.
- **VisualToggle** or **ToggleObject** for the anomaly visuals (optional).
- **NeoCondition** on **Selector.CountActive** for the lose condition (e.g., 4 active anomalies → defeat).
- **GM** / **EM** for win and lose (optional).
- **RandomRange** — for the "0 to N anomalies per level" scenario (optional).

---

## 1. "Time to Victory" Timer

Goal: the player must survive, for example, **6 hours** of game time.

1. Create an empty GameObject (e.g., `TimerVictory`).
2. Add the **TimerObject** component.
3. Configure:
   - **Duration** = `21600` (6 × 3600 seconds).
   - **Count Up** = `true`.
   - **Looping** = `false`.
   - **Time Scale** = game speed multiplier (e.g., `24` if 1 real minute = 24 in-game minutes).
   - **Auto Start** = `true` (or start it on the game start event).
4. In **On Timer Completed**, wire the victory call (e.g., **GM.Win()** or your own UnityEvent that switches to the win screen).

---

## 2. "Anomaly Spawn" Timer

Goal: "enable" the next anomaly at **random intervals**.

1. Create a GameObject (e.g., `TimerAnomalySpawn`).
2. Add **TimerObject**.
3. Configure:
   - **Use Random Duration** = `true`.
   - **Random Duration Min** / **Max** = desired range in seconds (e.g., 5–30).
   - **Looping** = `true`.
   - **Time Scale** = the same multiplier as the victory timer (so intervals run in game time).
   - **Auto Start** = `true` (or on game start).
4. In **On Timer Completed**, wire a call to **Selector.SetRandom()** on your **Selector** with the anomalies. The timer will automatically trigger the selection of the next anomaly when the interval elapses.

---

## 3. Anomaly Manager (Selector + SelectorItem)

1. Create a parent object (e.g., `AnomalyManager`).
2. Make the anomaly objects its children (each one is a prefab or an object with visuals and, if needed, **VisualToggle** / **ToggleObject**).
3. Add the **SelectorItem** component to **each** child object. The index is assigned automatically when the children list is refreshed.
4. Add **Selector** to the `AnomalyManager` parent:
   - Enable **Auto Update From Children** (enabled by default).
   - Enable **Use Random Selection**.
   - Enable **Notify Selector Items Only**: on a child object **with a `SelectorItem`**, the selector only calls **`SelectorItem.SetActive`** (the component itself does **not** call `GameObject.SetActive` on that object — only the reactive field and the UnityEvent). The selector uses direct **`GameObject.SetActive`** **only** for items **without** a `SelectorItem`, if any exist in the list.
   - The **Control Game Object Active** flag in the inspector is the overall "allow propagating selection to items" switch. For the "SelectorItem only" scenario, keep it **enabled**; otherwise the selector will call neither `GameObject.SetActive` nor `SelectorItem.SetActive` (only the index and `OnSelectionChanged*` will remain).
5. On each **SelectorItem**:
   - In **On Activated**, wire showing the anomaly (e.g., calling **VisualToggle.SetActive(true)** on the same object).
   - When the player "fixes" an anomaly, call **ExcludeFromSelector()** (e.g., via a button or another event) so this index no longer participates in random selection until reset.
6. If you need to reset the pool (new game or new level), call **Selector.IncludeAllIndices()**.

---

## 4. Lose Condition and Fixing Anomalies

If too many anomalies are active at once — defeat; the player must "fix" anomalies to return to a safe state.

1. Use **NeoCondition** (or an equivalent field-based check).
2. Set your **Selector** as the object being checked.
3. Configure the condition on the **CountActive** field (e.g., "greater than or equal to 4").
4. When the condition is met, trigger the defeat (e.g., **GM.Lose()**).

### Fixing Anomalies via InteractiveObject

To let the player fix anomalies with a click/key:

1. Add a collider (Collider/Collider2D) of an appropriate shape to each anomaly object.
2. Add the **InteractiveObject** component (`Scripts/Tools/InteractableObject/InteractiveObject.cs`):
   - Enable **Use Mouse Interaction** (enabled by default) or **Use Keyboard Interaction** (key, e.g., `E`).
   - Make sure `Include Trigger Colliders In Mouse Raycast` is enabled if you use an `Is Trigger` collider.
3. In InteractiveObject's **On Interact Down** event, wire a call to **SelectorItem.ExcludeFromSelector()** (on the same object or via a reference), and optionally hide the visuals (e.g., VisualToggle.SetInactive).
4. When ExcludeFromSelector is called, that anomaly will no longer be picked by Selector.SetRandom(), and CountActive will decrease, pushing back the defeat.

---

## 5. Both Timers Together

- **"Victory" TimerObject**: duration = 21600 (6 h), countUp, not looping; On Timer Completed → victory.
- **"Spawn" TimerObject**: useRandomDuration (e.g., 5–30 s), looping; On Timer Completed → **Selector.SetRandom()**.
- If needed, both timers can share the same **Time Scale** so both the time to victory and the anomaly spawn intervals run on the same "game" time scale. This is optional: the victory timer and the spawn timer can be sped up differently, or one of them can be left in real time.

---

## 6. Castle: 0–5 Anomalies per Level and a New Day (No Repeats)

If a level (e.g., a "castle") should enable a **random number of anomalies from 0 to 5**, and on the transition to the next day the level resets and anomalies **must not repeat during the day**:

1. **RandomRange**: add the **RandomRange** component to an object (e.g., the level or a manager). Mode = Int, Min = 0, Max = 5. On level start, call **Generate()** (from the scene start event or a timer).
2. **NeoCondition on RandomRange**: create a **NeoCondition** with the condition "object — RandomRange, field **ValueInt**, operator GreaterOrEqual, threshold 1". On **On True**, enable the anomaly spawn timer (e.g., a call to **TimerAnomalySpawn** or activating the GameObject with the timer). This way the timer only runs if at least 1 anomaly was rolled.
3. **Limiting the number of active anomalies**: you can add a second condition on **Selector.CountActive** (e.g., "less than ValueInt") and call **Selector.SetRandom()** from the timer only when CountActive &lt; ValueInt (via NeoCondition or separate logic).
4. For a new day, use the **Selector** itself: on transitioning to the next day or reloading the level, call **Selector.IncludeAllIndices()** and, if needed, **Selector.ResetAll()**. This restarts the anomaly pool without a separate helper component.
5. If anomalies must not repeat within a single day, exclude them as they get fixed via **SelectorItem.ExcludeFromSelector()** or directly via **Selector.ExcludeIndex(int)**. At the end of the day, clear the exclusions with **IncludeAllIndices()**.

## See Also

- [TimerObject](../Tools/Time/TimerObject.md)
- [Selector](../Tools/View/Selector.md)
- [SelectorItem](../Tools/View/SelectorItem.md)
- [RandomRange](../Tools/Components/RandomRange.md)
