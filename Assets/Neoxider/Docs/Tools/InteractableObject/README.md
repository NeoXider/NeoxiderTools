# Tools / InteractableObject

Click/hover interactions and 2D/3D physics events. Scripts live in
`Scripts/Tools/InteractableObject/`. Use this page as the English module entry.

The reusable contracts, hit-order math, and camera resolver are isolated in the leaf assembly
`Neo.Tools.InteractableObject.Core` (`Scripts/Tools/InteractableObject/Core/`). It has no package
assembly references, so custom input, AI, XR, and proximity code can use the `Neo.Tools` APIs without
pulling in Mirror or `Neo.Network`. The scene-facing `Neo.Tools.InteractableObject` assembly adds the
MonoBehaviour, UnityEvent, input, and optional network integration layer.

## docs (per-component)

| Page | Description |
|------|-------------|
| [InteractiveObject](./InteractiveObject.md) | Interaction component with hover, click, keyboard, and distance checks |
| [InteractionRayProvider](./InteractionRayProvider.md) | Camera viewport, cursor, and crosshair ray modes |
| [Shared queries](./SharedQueries.md) | Complete shared physics hits, epoch lifetime, fresh queries and counters |
| [Core contracts](./Core/README.md) | Independent C# target, range, hit selection and camera contracts |
| [PhysicsEvents2D](./PhysicsEvents2D.md), [PhysicsEvents3D](./PhysicsEvents3D.md) | 2D/3D collision and trigger events |
| [ToggleObject](./ToggleObject.md) | Boolean toggle state and events |

## C# and scene workflows

For custom input, call `IInteractiveTarget.InteractDown()` / `InteractUp()`, or the concrete
`InteractiveObject.Click(MouseButton, bool)` API. Manual commands check `interactable` and use the
configured local/network route; the caller owns range and aim validation. Inspector test buttons
are available in Play Mode. Automatic mouse and keyboard input applies the component's own checks.

For scene setup, add a collider to a world target, configure checkpoints and distance, then connect
the interaction UnityEvents. UI targets use an `EventSystem` and a `GraphicRaycaster` on their canvas;
they do not need physics colliders. Set UI interaction distance to `0` when no world-distance rule is
needed. Automatic setup can create an EventSystem and install the required 2D/3D camera raycasters;
both dimensions are supported on the same camera.

Add `InteractionRayProvider` to the main camera to choose cursor or viewport-center rays. Its default
`Both` mode hovers at the crosshair but clicks at the cursor. Use `ScreenCenter` for crosshair clicks
as well. A per-object `UseScreenCenterRay` override selects the center for both operations.

Automatic targets now share identical raw physics queries during a stable interaction epoch.
Each target retains its own colliders, range, hierarchy, obstacle and event rules. No new controller,
registration or scene wiring is required. Complete overflow handling replaces the old 16-hit limit.
Different masks, physics kinds, cameras or genuinely different LOS rays remain separate queries.
See [Shared queries](./SharedQueries.md) for lifetime and measurement details.

After application-controlled collider movement, manual simulation or `SyncTransforms` inside a frame,
call `InteractionRayProvider.InvalidateQueries()` before the next shared interaction pass. Interaction
callbacks invalidate before and after dispatch; arbitrary external world changes require that explicit
boundary. The synchronous `IsInInteractionRange` property keeps a fresh-check path.

`PhysicsEvents2D` / `PhysicsEvents3D` expose both UnityEvents and C# events. With `isNetworked` enabled,
an inactive or unavailable network still delivers local physics callbacks. An active network uses
server-authoritative dispatch; remote collision callbacks receive `null` instead of a native collision.

## See also

- [Tools/Components](../Components/README.md)
