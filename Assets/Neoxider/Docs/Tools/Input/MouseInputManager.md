# MouseInputManager

**Purpose:** a singleton mouse input component. It emits `Press`, `Hold`, `Release`, and `Click`, performs 2D/3D raycasts by layer mask, and stores the latest event in `MouseEventData`. Event data is a struct; legacy polling avoids per-frame allocations, while the optional reflection fallback can allocate.

File: `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs`

## Module Principle

`MouseInputManager` can be used as a scene component or as an auto-created runtime singleton. Production scenes should assign `targetCamera` in the Inspector or inject it through `SetTargetCamera(Camera)`. `Camera.main` is only an optional fallback and is retried on an interval instead of being queried every frame.

## Setup

1. Add `MouseInputManager` once to the scene or access `MouseInputManager.I` to create it lazily at runtime.
2. Assign `targetCamera` explicitly. Keep `useMainCameraFallback` only for simple scenes.
3. Configure `interactableLayers` and `fallbackDepth`.
4. Enable the event modes you need: `enablePress`, `enableHold`, `enableRelease`, `enableClick`.
5. Subscribe to events or poll `LastEventData` / `HasEventData`.

## Camera Binding

| Field/API | Description |
|-----------|-------------|
| `targetCamera` | Camera used for `ScreenPointToRay` and `ScreenToWorldPoint`. |
| `useMainCameraFallback` | Allows resolving `Camera.main` when no explicit camera is assigned. |
| `cameraFallbackRetryInterval` | Seconds between `Camera.main` fallback attempts while the camera is missing. |
| `logMissingCamera` | Allows a warning through `NeoDiagnostics`; the global diagnostics gate still controls output. |
| `SetTargetCamera(Camera)` | Explicit injection point for C# and scene setup code. |
| `TargetCamera` | Current active camera reference. |

## Events

- `OnPress`, `OnHold`, `OnRelease`, `OnClick`
- `OnPressIn`, `OnHoldIn`, `OnReleaseIn`, `OnClickIn`
- `OnCanceled`: parameterless C# event when an active press is interrupted.

`MouseEventData` contains `ScreenPosition`, `WorldPosition`, `HitObject`, `Hit3D`, and `Hit2D`.

Each event mode is independent. For example, `enablePress = false` suppresses press notifications while preserving enabled Hold and Click events. Hold requires a press observed by the manager; Click requires press and release over the same non-null hit GameObject. Release can notify even without a matching press. Releasing over empty space does not click.

For uninterrupted input, the order is Press, PressIn, Hold, HoldIn, Release, ReleaseIn, Click, ClickIn, with only enabled/eligible phases emitted. Each ordinary event captures its own subscribers when that event starts; its `in` counterpart captures separately afterward. Adding an `in` listener during an ordinary callback therefore affects that same sample's `in` notification. Removing an ordinary listener during its event does not change the already captured batch. Duplicate registrations and removing the last matching registration retain normal C# delegate semantics; a subscriber exception propagates and stops the batch.

In 11.0, the manager checks the current input dispatch between subscribers and before every later phase. A callback that disables the manager, cancels its gesture or processes a replacement gesture stops the obsolete tail, including late MouseEffect callbacks. A new Press started from OnCanceled/OnRelease keeps its own hit, position and pending press; old callbacks cannot subsequently stop or clear it. If lifecycle loss interrupts an unfinished Release, its remaining visual ownership is canceled once and its old Click is suppressed. Repeated cancellation without a new gesture does not interrupt the current cleanup batch. Cancellation never synthesizes Release or Click.

If an OnRelease or OnReleaseIn subscriber throws, that same exception propagates synchronously and the remaining Release/Click notifications are skipped. The interrupted gesture keeps its unfinished Release ownership for a later `OnCanceled` on manager disable, camera/input loss or another lifecycle interruption; this lets a still-enabled MouseEffect stop even when its Release callback was skipped. Successfully completed Release phases consume that ownership before Click, so a later lifecycle interruption does not cancel an already released gesture. An older call's exception/finally cannot consume a replacement gesture's ownership.

Subscriber snapshots are cached per event until an effective subscription change, so a stable Hold subscription does not rebuild an invocation array each frame. Explicit add/remove accessors immediately discard the old cached reference. An active dispatch keeps its own immutable array until it returns; accessors never clear that active array. Unsubscribed targets do not remain in an idle cache awaiting the next event. Physics queries, user callbacks and optional backend allocations remain separate costs.

3D picking uses the nearest 3D collider. If there is no 3D hit, it tries the nearest 2D intersection; it does not sort mixed 2D/3D hits together. When nothing is hit, `fallbackDepth` is the distance from the camera for screen-to-world conversion. Camera and layer configuration apply to both ordinary and `in` events.

## C# usage

```csharp
MouseInputManager manager = MouseInputManager.I;
manager.SetTargetCamera(gameCamera);
manager.enablePress = false;
manager.enableHold = true;
manager.OnClick += HandleClick;
```

`HandleClick` takes a `MouseInputManager.MouseEventData` argument. Unsubscribe with `manager.OnClick -= HandleClick` when the subscriber is disabled or disposed. `OnClickIn` instead accepts an `in MouseEventData` handler. Subscribe a parameterless cleanup handler to `OnCanceled` when the subscriber owns visuals or another drag resource. The pending press is already cleared when that handler runs; repeated cancellation does not notify again until another press starts. Normal release does not emit cancellation. `LastEventDataRef` exposes a readonly reference to the same latest cached struct; copy it if a later input event must not change the value being observed.

Polling data describes the last emitted/sampled event, not continuous cursor motion. `HasEventData` remains true until subsystem reset. Read mouse position directly through `MouseInputCompat` when continuous motion is needed. Mouse reads support both legacy input and the optional Input System; this component does not synthesize touch events or block UI overlap.

## Lifecycle

- `MouseInputManagerSubsystemRegistration` enables `CreateInstance = true` before scene load.
- Subsystem/domain reload clears `LastEventData` and `HasEventData`.
- Runtime singleton cache is cleared by the shared `SingletonRuntimeReset`.
- Disabling/destroying the component, losing focus, pausing the application, missing input devices or losing the camera cancels an active press once through `OnCanceled`. A later release cannot click an object from before that interruption. Cancellation does not synthesize Release/Click, update static polling data or trigger a release prefab in MouseEffect.
- UI-overlap blocking is opt-in. Use `UIInputCompat` or explicit-coordinate `UIUtils` queries in a filter above the events; see the [module entry](README.md).

## See Also

- [MouseEffect](./MouseEffect.md)
- [Module Root](./README.md)
