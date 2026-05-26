# MouseEffect

`MouseEffect` is a cursor visual-effects component driven by `MouseInputManager` events: press, hold, release and click. It does not own input rules; it is a scene-facing wrapper for trails, follower objects and prefab spawn effects.

## Features

- drives a `TrailRenderer` behind the cursor;
- moves a separate `followObject` to the cursor world position;
- spawns `spawnPrefab` on `Press`, `Hold`, `Release` or `Click`;
- supports single-shot and repeated spawn while holding;
- exposes `onStartFollow`, `onStopFollow` and `onSpawn` UnityEvents.

## Setup

1. Add `MouseInputManager` to the scene.
2. Add `MouseEffect` to an effect object.
3. Assign `trail`, `followObject` and/or `spawnPrefab`.
4. Assign `Target Camera` directly, or inject it from code with `SetTargetCamera(Camera)`.

## Camera Resolution

Cursor screen-to-world conversion uses this order:

1. `MouseEffect.TargetCamera`;
2. `MouseInputManager.TargetCamera`;
3. `Camera.main`, only when `Use Main Camera Fallback` is enabled.

`Camera.main` is not queried every frame. Missing-camera retry is throttled by `Camera Fallback Retry Interval`. Missing-camera warnings are disabled by default and are routed through `NeoDiagnostics` when `_logMissingCameraWarning` is enabled.

## Inspector Fields

| Field | Description |
| --- | --- |
| `interactable` | When false, the component ignores input events. |
| `disableOnRelease` | Disables trail/follower on mouse release. |
| `trail` | TrailRenderer that follows the cursor. |
| `followObject` | Object moved to the cursor world position. |
| `spawnPrefab` | Prefab spawned by the selected trigger. |
| `spawnTrigger` | Spawn event: `Press`, `Hold`, `Release` or `Click`. |
| `spawnDuringHold` | Repeats spawn while the button is held. |
| `holdInterval` | Repeat-spawn interval while holding. |
| `spawnLifetime` | Auto-destroy delay for spawned prefabs. `0` keeps them alive. |
| `followInterval` | Position update interval for trail/follower. |
| `followDepth` | Z-depth used by `ScreenToWorldPoint`. |
| `spawnParent` | Parent for spawned prefabs. Defaults to this component's transform. |

## API

```csharp
public Camera TargetCamera { get; }
public void SetTargetCamera(Camera camera);
```

Prefer `SetTargetCamera` from scene setup/bootstrap code when cameras are created dynamically. This keeps samples and game scenes independent from `Camera.main`.

## Dependencies

`MouseEffect` requires an active `MouseInputManager` in the scene. If the manager is missing, no input events are received. Missing-manager warnings are disabled by default and can be enabled with `_logMissingManagerWarning`.
