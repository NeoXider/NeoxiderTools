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

Following reads screen position through `MouseInputCompat`, so legacy-disabled projects can use the optional Input System fallback. Press placement and spawned effects use the manager's hit/fallback `WorldPosition`; subsequent following uses this component's camera and `followDepth`. Depth is a distance from the camera, not an absolute world Z coordinate.

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

Configure the public fields from C# before enabling the component, or use the same fields and `onStartFollow` / `onStopFollow` / `onSpawn` hooks in the Inspector. A first `Hold` trigger spawns once, then `spawnDuringHold` repeats at `holdInterval`; a zero interval permits one spawn per later hold notification. The first hold never creates both the initial and repeat instances at once. Follow/spawn intervals use scaled game time.

## Lifecycle and ownership

The component subscribes to the current `MouseInputManager.I` when enabled and unsubscribes when disabled. Enable the manager's Press/Release notifications for normal follow start/stop and Hold notifications for hold spawning. It also subscribes to `OnCanceled`: disabling/destroying the manager or losing its camera/input stops subscribed visuals even while the effect itself remains enabled. Cancellation never spawns a Release effect. Replacing the manager requires re-enabling the effect to bind it again.

Release stops active following and emits `onStopFollow` once per started follow. `disableOnRelease = false` retains visible trail/follower objects after normal release. Disabling the component, losing application focus, pausing the application or setting `interactable = false` stops following and hides the visuals; an unmatched later release does not emit another stop. Changing `interactable` is applied on the next update or release.

In 11.0, visual activation and `onSpawn` callbacks cannot resume an interrupted or superseded Press. A callback that disables the effect before follow starts produces no orphan `onStartFollow`; a callback that disables it from `onStartFollow` produces one start/stop pair. A newer independent gesture keeps its own follow state when the older callback returns. Normal successful Press ordering remains prefab spawn/`onSpawn`, then `onStartFollow`. Existing spawned instances keep their lifetime even when their initiating gesture is canceled.

The manager also fences obsolete notifications between subscribers. An earlier OnPress listener disabling the manager cannot let a late effect restart following; a replacement Press started by an earlier OnCanceled/OnRelease listener is not stopped by the old event tail. Hold/Click handlers check that this effect is still live and enabled when entered. Thus an earlier listener disabling only the effect cannot make its already captured Hold/Click callback spawn after OnDisable. Re-enabling subscribes normally for future valid input; existing spawned instances are retained.

Spawned instances belong to `spawnParent`, or this transform when no parent is assigned. `spawnLifetime > 0` schedules destruction; `0` leaves lifetime management to that parent/caller. The component uses Instantiate/Destroy rather than a pool. Disabling it does not destroy previously spawned effects.

## Dependencies

`MouseEffect` requires an active `MouseInputManager` in the scene. If the manager is missing, no input events are received. Missing-manager warnings are disabled by default and can be enabled with `_logMissingManagerWarning`.
