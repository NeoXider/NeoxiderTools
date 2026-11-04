# VariantView

**What it is:** `Neo.UI.VariantView` (`Scripts/UI/View/VariantView.cs`) is a `MonoBehaviour` that applies an
indexed visual state to Images (sprites/colors), TMP text (colors and optional strings), and GameObject
groups. It is a presentation component; the caller owns game rules.

**How to use:**
1. Add `Neoxider > UI > VariantView` and fill the variant arrays (array index = state index).
2. Switch states with `SetCurrentState(int)`, `NextState()` or `PreviousState()` from code or UnityEvents.
3. React to changes through `StateChanged` (C#) or `OnStateChanged` (UnityEvent).

---

## Scene setup

1. Add `Neoxider > UI > VariantView`.
2. Add Image, TMP, or GameObject variant groups in the Inspector. Array index is the state index.
3. `OnValidate` computes `MaxStates` from the configured arrays and resizes the other arrays to match.
4. Choose `_currentStateIndex` to preview a state. With no states, the index stays `0` and navigation is inert.
5. Bind buttons or other UnityEvents to `SetCurrentState(int)`, `NextState()`, or `PreviousState()`.

Enable `_isBuildPhase` to author additional states with `AddState(newStateCount)`. It expands the arrays
and copies the current values into the final new slot. Inspector validation also captures current Image
and TMP values into the final slot while build mode is enabled; turn build mode off when authoring is done.

## Runtime API

```csharp
view.StateChanged += index => Debug.Log($"State {index}");
view.SetCurrentState(1);
view.NextState();
view.PreviousState();
int selected = view.CurrentStateIndex;
int available = view.MaxStates;
```

`StateChanged` (C#) and `OnStateChanged` (UnityEvent, Inspector) fire with the new index, C# event first,
when `SetCurrentState`, `NextState` or `PreviousState` change the state. Re-applying the current index only
refreshes the visuals. Inspector validation and the authoring helpers `AddState` and `ClearAllStates` move the
index without raising them. `currentStateIndex` is an obsolete alias of `CurrentStateIndex`.

`Awake` applies the saved state once, so a view spawned or loaded at runtime shows its **Current State Index**
before the first change (in the Editor `OnValidate` already does this while you edit).

Only indices from `0` through `MaxStates - 1` are applied. Navigation stops at the ends instead of wrapping.
`ClearAllStates()` clears the configured state arrays and returns the current index to `0`.

Each GameObject group activates its selected object and deactivates its other objects. The same object
may be shared by several slots in one group (including copies made by `AddState`); selecting any such slot
keeps the object active. Missing target GameObjects are skipped. Configure state arrays in the Inspector;
state changes are available through the same public methods from C# and UnityEvents.

See [UI module](./README.md) and [VisualToggle](./VisualToggle.md) for a two-state alternative.
