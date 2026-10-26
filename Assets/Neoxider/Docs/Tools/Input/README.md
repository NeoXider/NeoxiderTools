# Tools / Input

Keyboard polling, swipe gestures, mouse picking and cursor effects. Scripts live in `Scripts/Tools/Input/`; public types use namespace `Neo.Tools`. Custom game assemblies must reference `Neo.Tools.Input`.

## docs (per-component)

| Page | Description |
|------|-------------|
| [KeyInputCompat](./KeyInputCompat.md) | Legacy/new input keyboard compatibility |
| [SwipeController](./SwipeController.md) | Swipe gestures |
| [MultiKeyEventTrigger](./MultiKeyEventTrigger.md) | Multi-key shortcuts |
| [MouseInputManager](./MouseInputManager.md) | Mouse input singleton |
| [MouseEffect](./MouseEffect.md) | Mouse-driven effects |

## Runtime API

- `KeyInputCompat.GetKeyDown`, `GetKeyUp` and `GetKey` read a `KeyCode`.
- `MouseInputCompat.TryGetPosition`, `TryGetButtonDown`, `TryGetButton` and `TryGetButtonUp` return whether a read is available and write the sampled value to an `out` argument. Mouse button indices `0`, `1`, `2` mean left, right, middle.
- `MouseInputManager.I` provides typed mouse events and optional collider picking; inject a camera with `SetTargetCamera(Camera)`.
- `MouseInputManager.OnCanceled` reports an interrupted active press after clearing it, without a synthetic Release or Click.
- `SwipeController.OnSwipe` publishes `SwipeData`. `GetSwipeDirection(out SwipeDirection)` consumes the latest pending direction once.

`InputBackendCompat` shares selection across key, mouse and touch readers. Legacy-only and Both configurations prefer the legacy Input Manager; New-only configurations skip it. If an attempted legacy read rejects access, all compatible readers use the optional fallback until subsystem reset or an explicit `InputBackendCompat.ResetBackendSelection()`. Do not reset it per frame. Device connection changes do not invalidate backend configuration; optional reads consult the current device each time. `UsesLegacyInput` reports the selected preference, not device presence.

The Input System package remains optional; no direct `Unity.InputSystem` assembly reference is required. Missing devices produce unavailable/neutral reads. Swipe touch input uses legacy finger IDs or the Input System's primary touch. Mouse picking/effects do not automatically convert touch into mouse events.

`OptionalInputSystemAdapter` also exposes `ReadMove`, `ReadPointerDelta`, `ReadLookStick`, `ReadLookDelta`, `ReadJumpPressed`, `ReadJumpHeld` and `ReadRunHeld`. Pointer delta is a per-frame pixel displacement; a look stick is a continuous rate, so scale it by elapsed time separately. `ReadLookDelta` combines the sources for existing callers. `ReadMove` can return an unnormalized diagonal. `IsAvailable` describes available Input System types, not a connected device. Reflection fallback can allocate; validate stripping and performance in target player builds.

## UI picking with compatible mouse input

`UIInputCompat.IsPointerOverUI()` and `GetUIElementsUnderCursor()` sample through `MouseInputCompat`. For repeated queries, retain a result list:

```csharp
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine.EventSystems;

List<RaycastResult> hits = new();
int count = UIInputCompat.GetUIElementsUnderCursor(hits);
```

The caller-buffer overload clears the list, including when no mouse or EventSystem is available; a null buffer throws `ArgumentNullException`. It reuses the list but still creates per-query PointerEventData in the lower UI utility. Custom asmdefs using RaycastResult also reference `UnityEngine.UI`.

For touch, injected coordinates or other pointer policies, use the input-independent `Neo.Extensions.UIUtils.GetUIElementsAtPosition(Vector2, List<RaycastResult>)` directly. Its legacy `GetUIElementsUnderCursor()` / `IsPointerOverUI()` APIs retain legacy mouse semantics; migrate those calls to `UIInputCompat` for New-only projects. Extensions remains independent of the Input assembly. UI filtering is opt-in and is not automatically applied to manager events.

## Scene and Inspector workflow

Add `MouseInputManager` and assign its camera for mouse-based interaction. Add `MouseEffect` for trails, followers or prefab spawning; enable the manager event modes it needs. `SwipeController.OnSwipe` and each `MultiKeyEventTrigger.KeyEventPair.onKeyPressed` can be wired in the Inspector or subscribed from C#. No ScriptableObject or UnityEvent chain is required for the runtime polling API.

Manager event switches filter notifications independently. Disabling a component or losing application focus cancels its active gesture. Subsystem registration clears static polling/instance caches; the mouse bootstrap enables lazy singleton creation on first `I` access.

## See also

- [Tools/Move](../Move/README.md)
