# MouseInputManager

**Purpose:** a singleton mouse input component with no per-frame event allocations. It emits `Press`, `Hold`, `Release`, and `Click`, performs 2D/3D raycasts by layer mask, and stores the latest event in `MouseEventData`.

File: `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs`

## Module Principle

`MouseInputManager` can be used as a scene component or as an auto-created runtime singleton. Production scenes should assign `targetCamera` in the Inspector or inject it through `SetTargetCamera(Camera)`. `Camera.main` is only an optional fallback and is retried on an interval instead of being queried every frame.

## Setup

1. Add `MouseInputManager` once to the scene or let the bootstrap create it automatically.
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

`MouseEventData` contains `ScreenPosition`, `WorldPosition`, `HitObject`, `Hit3D`, and `Hit2D`.

## Lifecycle

- `MouseInputManagerSubsystemRegistration` enables `CreateInstance = true` before scene load.
- Subsystem/domain reload clears `LastEventData` and `HasEventData`.
- Runtime singleton cache is cleared by the shared `SingletonRuntimeReset`.
- UI-overlap blocking through `EventSystem` is not built in; add a separate filter on top of the events when needed.

## See Also

- [MouseEffect](./MouseEffect.md)
- [Module Root](./README.md)
