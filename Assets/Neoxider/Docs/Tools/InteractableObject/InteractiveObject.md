# InteractiveObject

`InteractiveObject` is a no-code interaction component for UI, 2D, and 3D objects. It combines hover, mouse clicks, keyboard interaction, distance checks, optional look-direction checks, and UnityEvent callbacks in one scene-facing component. File: `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `InteractiveObject` to a UI element or to a scene object with a collider.
2. Decide whether interaction should use mouse, keyboard, or both.
3. Set interaction distance and checkpoints if range-limited interaction is needed.
4. Configure UnityEvents such as hover, click, enter-range, or interact-down.
5. Ensure the scene has the needed `EventSystem` and raycasters.

## Main settings

### Interaction mode

- `useHoverDetection`
- `interactable`
- `useMouseInteraction`
- `useKeyboardInteraction`
- `keyboardInteractionMode`
- `requireViewForKeyboardInteraction`
- `requireDirectLookRay`
- `includeTriggerCollidersInLookRay`
- `includeTriggerCollidersInMouseRaycast`
- `useScreenCenterRay` (per-object override for both hover and clicks)
- `targetCollider3D` (optional; if unset, uses `Collider` on this object or the first child collider)
- `targetCollider2D` (optional; if unset, uses `Collider2D` on this object or the first child collider)

### Distance and checkpoints

- `interactionDistance`
- `distanceCheckPoint`
- `viewCheckPoint`
- `ignoreDistancePointHierarchyColliders`
- `checkObstacles`
- `obstacleLayers`
- `includeTriggerCollidersInObstacleCheck`

When `checkObstacles` is disabled, obstacle blocking is skipped for distance validation, keyboard direct-look ray checks, **and** the mouse hover/click ray (a hit on this object counts even if a non-trigger collider is closer to the camera). When enabled, **both** the mouse ray and the keyboard look ray require this object to be the nearest non-ignored hit before any foreign **non-trigger** collider along the ray.

The keyboard look ray and the mouse ray read the same settings on purpose: `E` and the cursor must reach the same objects, and any difference between them has to come from a flag you can see in the Inspector. Foreign **trigger** volumes never block either ray — a door's trigger zone is not a wall. Before `10.13.5` the keyboard branch ignored `checkObstacles` and treated foreign triggers as blockers, so a pickup standing inside a trigger volume was clickable while the key silently did nothing.

`ignoreDistancePointHierarchyColliders` excludes the checkpoint/view rig from physics queries, while
always retaining the selected target collider. When the target and checkpoint share a level root,
that common root does not cause every sibling target/wall to be ignored: only the checkpoint's own
ancestors/descendants are excluded. A separate player-rig root retains the existing whole-rig exclusion.

### Input bindings

- `downUpMouseButton`
- `keyboardKey`
- `doubleClickThreshold`

The camera's [InteractionRayProvider](./InteractionRayProvider.md) chooses the ray source. `Both`
(the automatically added default) uses the viewport center for hover and the cursor for mouse clicks,
down, and up. These can target different objects. `ScreenCenter` uses the center for both; the
per-object `UseScreenCenterRay` override does the same regardless of the provider mode.

### Debug

- `drawInteractionRayForOneSecond`
- `interactionRayDrawDuration`

## Events

### Hover

- `onHoverEnter`
- `onHoverExit`
- `onHoverChanged(bool)`

Hover callbacks report state transitions: repeated enter/exit notifications do not repeat events.
Disabling hover detection or the component clears an existing hover and emits one exit. Setting
`interactable` to `false` clears input/hover state on the next `Update`; a pointer exit can clear it
immediately. Losing the camera or an enabled collider also ends a raycast-driven hover.

Enter/exit listeners may change availability or reenter interaction. A superseded transition stops:
enter → hover-off/disable produces exit and changed(false), without a trailing changed(true).
Exit → reenter produces the new enter/change notification without a trailing obsolete changed(false).
Ordinary order remains enter/exit first, then `onHoverChanged`.

Automatic processing also has a target-local reset generation. Disable/destroy/reset from a mouse
or range observer stops the rest of that operation, including disable → enable within the observer.
Range state, sampled mouse-button state and double-click history are committed before notifications,
so a nested disable reset remains authoritative. Reopening in range produces a new enter; its first
click starts new double-click history. A same-frame keyboard down/up rechecks aim, range and input
eligibility after the down observer. Reentrant requests for the same automatic Update are bounded.
Ordinary hover callbacks only refresh raw physics eligibility; their physics epoch changes do not
cancel an available target's remaining input. Manual `InteractDown`, `InteractUp` and `Click` keep
their documented `interactable` gate and deliberate bypass of automatic checks, even when disabled.

### Click

- `onClick`
- `onDoubleClick`
- `onRightClick`
- `onMiddleClick`

### Interact down/up

- `onInteractDown`
- `onInteractUp`

### Range

- `onEnterRange`
- `onExitRange`

## Runtime API

| API | Description |
|-----|-------------|
| `InteractionDistance` | Gets or sets the interaction distance (`0` means unlimited). |
| `DistanceCheckPoint` | Gets or sets the transform used for distance checks. |
| `UseHoverDetection` | Enables or disables hover detection (cursor over collider). |
| `UseMouseInteraction` | Enables or disables mouse click/down/up interaction. |
| `UseKeyboardInteraction` | Enables or disables keyboard interaction. |
| `UseScreenCenterRay` | Overrides the provider with viewport-center hover and click rays. |
| `IsNetworked` / `AuthorityMode` | Controls optional Mirror replication and sender authority. |
| `IsInteractable` | Typed `IInteractiveTarget` state exposed for custom interaction sources. |
| `InteractDown()` | Triggers interact-down through the normal local or Mirror network route. No-op when `interactable` is disabled. |
| `InteractUp()` | Triggers interact-up through the normal local or Mirror network route. No-op when `interactable` is disabled. |
| `Click(MouseButton, bool)` | Triggers left, double-left, right, or middle click through the normal local or Mirror network route. No-op when `interactable` is disabled. |
| `InvalidateCachedColliders()` | Re-resolves target colliders on the next interaction check after runtime collider changes. |
| `IsInInteractionRange` | Whether the object is currently in valid range. |
| `DistanceToCheckPoint` | Current measured distance to the check point. |
| `IsHovered` | Whether the object is currently hovered. |

The Inspector exposes Play Mode-only test buttons for interact down, interact up, and click. `Test Click` also shows button and double-click parameters. These manual methods intentionally bypass hover, range, look-direction, and input checks; they only require `interactable`, then use the normal local or Mirror authority/rate-limit dispatch path. Use `Invalidate Colliders` after adding, removing, replacing, or reassigning colliders at runtime.

Without an active network session, manual interaction runs locally even when `IsNetworked` is enabled.
Server command handlers reject requests when `interactable` or networking is disabled, validate sender
authority before consuming the command budget, and rate-limit remote connections independently.

## Shared physics queries

Automatic mouse, keyboard-look and range/LOS queries use the shared
[`InteractionPhysicsQueries`](./SharedQueries.md) backend. Identical raw physical contexts share a
complete borrowed hit set during a stable epoch. Target identity, hierarchy exclusions and target
classification are evaluated locally and do not split identical raw queries. A full initial buffer
uses a complete fallback; the projected target buffer also grows, so later hits are not dropped.

All original masks and trigger policies remain: mouse/look use their existing physical masks,
`obstacleLayers` applies to range/LOS, 2D global queries retain global trigger/start-inside behavior,
and explicit `ContactFilter2D` LOS keeps its own trigger policy. Unique directed LOS rays remain
unique work. Both mode can require separate center and pointer contexts.

Existing components and scene wiring continue to work. There is no mandatory controller or target
registry. Interaction events, physics relay events and ToggleObject callbacks end the epoch before
and after dispatch, including an exception. A nested callback query is fresh. Cached click hits are
revalidated after an epoch boundary before they authorize input.

Call `InteractionRayProvider.InvalidateQueries()` after external movement, pooling/simulation or
`Physics.SyncTransforms()` inside the current pass. Frame advance automatically ends the epoch;
frameCount alone cannot identify arbitrary between-Update world mutations. Invalidation does not
perform `SyncTransforms` for the application. `IsInInteractionRange` always uses a fresh physical
query when one is needed, even if a previous automatic query was cached.

## Reusable C# interaction core

Custom input, AI, XR, or proximity controllers can depend on `IInteractiveTarget` instead of the
scene component. `InteractiveObject` implements this contract without changing its existing
`InteractDown()` / `InteractUp()` network dispatch:

These APIs live in the named leaf assembly `Neo.Tools.InteractableObject.Core` and retain the
`Neo.Tools` namespace for source compatibility. A custom asmdef that uses them should reference
`Neo.Tools.InteractableObject.Core` directly. The leaf has no `Mirror` or `Neo.Network` reference;
reference `Neo.Tools.InteractableObject` only when the concrete scene component is needed.

```csharp
public void UseTarget(IInteractiveTarget target)
{
    if (target != null && target.IsInteractable)
    {
        target.InteractDown();
        target.InteractUp();
    }
}
```

`InteractionQueryMath` contains the scene-free rules used by the component:

- `IsWithinRange(...)` uses the same inclusive distance boundary (`0` means unlimited).
- `GetObstacleCheckDistance(...)` applies the target padding used by obstacle rays.
- `TryGetNearestHit(...)` and `TrySelectTarget(...)` resolve unsorted `InteractionRayHit` buffers,
  including the nearest-target-versus-nearest-blocker rule.

The query APIs accept reusable arrays and do not allocate. `InteractionCameraResolver` centralizes
the existing cached-camera policy (`Camera.main`, with an optional first-camera fallback) for custom
interaction components.

## Input rules

- World hover is driven by the configured cursor/viewport-center ray hit on the target collider.
- If `interactionDistance > 0`, hover requires a ray hit and the **hit point** to be within range (same as click eligibility), not only the collider center—so “already aimed, then walk into range” works at the distance boundary. If `interactionDistance == 0`, hover has no distance limit.
- World mouse click / down / up require a current click-ray hit on the target collider. Mouse up also
  requires that the configured button was originally pressed on this object. Hover detection can be off.
- With `checkObstacles` enabled: non-trigger colliders in front of the object block hover/click **and the keyboard look ray**; foreign trigger colliders block neither. With `checkObstacles` disabled, only a ray hit on this object (and distance rules) matters — again for both inputs.
- In `ViewOrMouse`, keyboard interaction no longer relies on hover; it uses view direction and optional direct look ray. `requireDirectLookRay` decides whether an aim ray is required at all; `checkObstacles` decides whether obstacles along that ray block the aim. The two flags stay independent.
- The collider cache prefers the same GameObject and then the first child collider (including inactive
  children). Assign `targetCollider3D` / `targetCollider2D` explicitly to select another collider, and call
  `InvalidateCachedColliders()` after changing or replacing it at runtime.
- `includeTriggerCollidersInMouseRaycast` applies to 3D and 2D alike: without it the mouse ray cannot see an object whose own collider is a trigger. The two dimensions reach that result differently, because `Physics2D.GetRayIntersectionNonAlloc` accepts no `QueryTriggerInteraction` — 3D excludes triggers inside the query, 2D filters them out of the result afterwards. One consequence is 2D-only: the global `Physics2D.queriesHitTriggers` is consulted first, so the flag can only narrow what that global already returned and never widen it. If a 2D trigger stays invisible to the ray with the flag on, check the global before the component.

## Scene requirements

- UI interaction requires an `EventSystem`.
- A canvas needs a `GraphicRaycaster`. A valid pointer hit on this UI object or its child can click
  without a physics collider. Set distance to `0` for UI that should ignore world distance.
- Non-UI interaction requires colliders.
- The component can rely on `Physics Raycaster` or `Physics2D Raycaster` for scene objects.
- Automatic raycaster installation checks each camera independently and supports both 2D and 3D
  raycasters on the same camera. If cameras spawn after the target's `Awake`, configure their raycasters
  explicitly; automatic setup is performed during `Awake`.

## Typical scenarios

- Doors or chests that can be opened with `E` in range.
- Pickups that react to click or keyboard interaction.
- NPCs that require both range and look direction before dialogue starts.
- UI buttons that still want richer hover/click event wiring.

## See also

- [README](./README.md)
- [InteractableObject docs](./README.md)
- [Tools/Components](../Components/README.md)
