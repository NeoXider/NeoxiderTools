# Neo.NoCode

No-code UI bindings for scenes where designers should connect data to text and progress visuals without writing one-off view scripts.

Demo: `Assets/Neoxider/Samples~/Demo/Scenes/NoCode/NoCodeBindingDemo.unity` — runtime-built UI via `NeoDemoShell`. The release layout uses `Samples~`; active sample development uses `Samples`. Importing **Demo Scenes** through Package Manager copies the sample into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`.

## UX Discipline And C# Contract

NoCode is a data-binding layer for UI, not hidden gameplay logic in the Inspector. New behavior must expose a testable C# contract and have EditMode or PlayMode coverage before adding Inspector convenience around it.

NoCode runs on scene components. ScriptableObject assets must not store scene object references; if a setup lives in an SO, store only data, keys, or context slots, and assign concrete `GameObject` references on the scene component.

Current `ComponentFloatBinding` contract:

- data sources are readable fields and readable non-indexed properties only;
- methods addressed by string name are not valid sources and are never invoked;
- `ReactivePropertyFloat`, `ReactivePropertyInt`, and `ReactivePropertyBool` are supported source values;
- `ComponentOccurrence` selects a zero-based matching component instance; the default `0` preserves existing first-match bindings, and invalid/missing occurrences never fall back to that first instance;
- `GameObject.Find` is allowed only as an explicit `Find By Name` fallback with a retry interval, not as the primary architecture.

For business rules, keep the rule in a normal C# component or service, test it there, and expose only the final field, property, or reactive state to NoCode.

## Components

| Component | Purpose |
|-----------|---------|
| `NoCodeBindText` | Reads a numeric source and writes to `SetText`, then `TimeToText`, then TMP, in that priority order. Unassigned targets are looked up on the same GameObject. |
| `NoCodeFormattedText` | Formats multiple sources with invariant-culture `string.Format`, for example `HP {0:0}/{1:0}`. Writes to `SetText`, TMP, or an existing legacy uGUI Text target, in that order. |
| `SetProgress` | Maps a value from Min/Max into the assigned `Slider.normalizedValue` and/or `Image.fillAmount`, clamped to 0–1. Equal or approximately equal endpoints leave the targets unchanged. |

## Source Flow

1. Select the source object.
2. Select the source component type and its field/property. For repeated components of that type, choose **Component Occurrence** (`0` = first match, `1` = second); unrelated component types are ignored.
3. Select the target UI component.
4. Configure formatting, clamping, or progress range when needed.

The module is intended for Inspector-driven prototypes and reusable UI screens. It intentionally does not support arbitrary method invocation by string.

## Updates and object lifetime

- **Once** reads when enabled. **Reactive** subscribes to supported reactive values and polls ordinary or unresolved sources. **Poll** refreshes in `LateUpdate`.
- Polling uses unscaled time, defaults to 0.16 seconds, and clamps the interval to at least 0.016 seconds. Disabling **Poll In Late Update** also disables polling for missing sources and ordinary values in Reactive mode; reactive events still work.
- Inspector validation marks configuration dirty without reading source getters. The next active `LateUpdate` reconciles it before the polling decision, so editing an interval/source does not stop Reactive fallback. Once remains nonpolling, and disabling **Poll In Late Update** still prevents periodic UI refreshes.
- Subscriptions belong to enabled, active components. Disabling releases them. An explicit refresh while disabled can update the view once but does not subscribe. An event already being dispatched when another listener disables the view is ignored.
- At the next refresh, replacing a reactive instance or changing its binding releases the previous subscription and connects the current source. For immediate runtime rewiring, refresh explicitly or disable/re-enable the view. A purely reactive view does not continuously search for replacement objects while its existing source is idle.
- If a Slider callback changes a `SetProgress` binding from reactive to an ordinary field, the binding uses its final subscription state and continues through the polling fallback.
- If no cached text target is alive, newly added or recreated targets are resolved at the next refresh. `NoCodeFormattedText` writes the current text even when the value is unchanged; `OnTextChanged` remains a value-change event and does not fire again solely because a target appeared. Inspector changes to `NoCodeBindText` targets invalidate its cached target selection.

`NoCodeFormattedText.OnTextChanged` compares the formatted result with the previous result for the current enable period. Interval-only edits, target reassignment and equivalent format strings can redraw the current text without repeating that event. A source or format change that produces a different string emits one notification; repeated unchanged refreshes emit none. Enabling the component still publishes its initial result once. In Once mode, validation keeps the redraw pending until an explicit refresh or re-enable.

`NoCodeFormattedText` treats missing/null bindings as zero. Invalid format strings are displayed literally. `NoCodeBindText` and `SetProgress` retain their previous output when their source cannot be read.

## C# API

Use [ComponentFloatBinding](./ComponentFloatBinding.md) directly to read component data from C#. Its `SourceRoot`, `ComponentTypeName`, `ComponentOccurrence`, and `MemberName` setters invalidate cached resolution automatically. `TryGetReactiveProperty` exposes the typed reactive instance; callers that subscribe directly own their listener cleanup.

For custom views, derive from [NoCodeFloatBindingBehaviour](./NoCodeFloatBindingBehaviour.md), override `ApplyFloat(float)`, and expose a method calling the protected `RefreshFromSource()` when runtime rewiring needs an immediate refresh. Existing text and progress components expose their source through `Binding`. `NoCodeFormattedText.RefreshFromSource()` is public and can also be connected to a UnityEvent.

## Related docs

- [Scene setup walkthrough](./GettingStarted.md)
- [ComponentFloatBinding API](./ComponentFloatBinding.md)
- [Custom float views](./NoCodeFloatBindingBehaviour.md)
- [Condition](../Condition/README.md)
- [Reactive](../Reactive/README.md)
- [Tools/Text](../Tools/Text/README.md)
