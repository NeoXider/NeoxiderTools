# VariantView

`Neo.UI.VariantView` applies an indexed visual state to Images (sprites/colors), TMP text (colors and
optional strings), and GameObject groups. It is a presentation component; the caller owns game rules.

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
view.SetCurrentState(1);
view.NextState();
view.PreviousState();
int selected = view.currentStateIndex;
int available = view.MaxStates;
```

Only indices from `0` through `MaxStates - 1` are applied. Navigation stops at the ends instead of wrapping.
`ClearAllStates()` clears the configured state arrays and returns the current index to `0`.

Each GameObject group activates its selected object and deactivates its other objects. The same object
may be shared by several slots in one group (including copies made by `AddState`); selecting any such slot
keeps the object active. Missing target GameObjects are skipped. Configure state arrays in the Inspector;
state changes are available through the same public methods from C# and UnityEvents.

See [UI module](./README.md) and [VisualToggle](./VisualToggle.md) for a two-state alternative.
