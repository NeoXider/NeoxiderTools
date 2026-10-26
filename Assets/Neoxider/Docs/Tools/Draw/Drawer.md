# Drawer

`Neo.Tools.Drawer` draws world-space polylines from C# calls or legacy mouse input, with optional Chaikin smoothing and `EdgeCollider2D` collision. Reference assembly `Neo.Tools.Draw` and namespace `Neo.Tools`.

## Scene and Inspector setup

1. Add **Neoxider > Tools > Drawer** to a GameObject.
2. Assign the input camera in the Inspector, or set `InputCamera` from C#. If unset during `Awake`, Drawer tries `Camera.main`, then the first scene camera. If neither exists, it reports an error and disables automatic input; the drawing methods remain callable.
3. Set `fixedZ` to the drawing plane. The camera must face that plane. Orthographic and perspective projection both use a screen ray/plane intersection.
4. Configure `lineMaterial`, optional `lineSprite` / `lineTexture`, `color`, and `lineWidth`. A sprite's full texture is used; sprite atlas UV rectangles are not extracted.
5. Configure point spacing, smoothing, creation thresholds, and optional collision or pooling below. Press/drag/release the primary mouse button to draw.

Automatic input uses Unity's legacy `Input` API; touch input depends on the project's touch-to-mouse simulation. `isActive = false` pauses automatic input and its drawing timer, while public C# methods still work. Disabling the component also pauses automatic input; it does not finalize a stroke.

## C# drawing API

```csharp
using Neo.Tools;
using UnityEngine;

// Inside a MonoBehaviour with a Drawer component:
Drawer drawer = GetComponent<Drawer>();
drawer.isActive = false;
drawer.fixedZ = 0f;
drawer.minPointDistance = 0.1f;
drawer.OnLineCreated.AddListener(line => Debug.Log(line.positionCount));

drawer.BeginLine(Vector3.zero);
drawer.AppendPoint(new Vector3(1f, 0f, 0f));
drawer.AppendPoint(new Vector3(2f, 1f, 0f));
drawer.EndLine();
```

`BeginLine(position)` finishes an existing stroke, creates or acquires a renderer, resets the counters, and renders the first point. Every point, including `OnLineStarted`'s position, uses `fixedZ`. `AppendPoint` does nothing without a current stroke. `EndLine` discards strokes below the creation thresholds; otherwise it stores the renderer in `lines` and invokes `OnLineCreated`. Calling it without a stroke does nothing. An `OnLineCreated` listener may start another stroke or delete the completed one.

`ResetPoints()` clears current raw/rendered points, disables its live collider, and commits both reactive counters to zero before notifying changed values in Distance-then-PointCount order. It is safe while idle. A reset or startup listener may begin a replacement stroke: the superseded operation stops before modifying it or emitting a stale `OnLineStarted`. `CreateCollider()` creates or updates the current line's edge collider, returning `null` while idle. `CreateLineRenderer()` only creates/configures a renderer; it does not start a stroke or add it to `lines`. Call `Delete(renderer)` when finished with such a renderer; owner destruction also cleans these standalone acquisitions.

`TryScreenToWorld(screen, out world)` returns `false` if the camera is missing or its forward ray cannot intersect the drawing plane. `ScreenToWorld(screen)` returns the intersection, or throws `InvalidOperationException` in those cases. Automatic input ignores screen positions that cannot be projected.

## Geometry and creation settings

| Fields | Behavior |
|--------|----------|
| `minPointDistance` | Minimum spacing of accepted raw points when `fixedLength` is zero. |
| `fixedLength` | Positive values append one segment of this length once the pointer is at least that far from the last raw point. |
| `smoothing` | Number of Chaikin passes; Inspector range 0-10. More passes increase intermediate point count before limiting. |
| `maxPoints` | Limits processed renderer/collider points, preserving endpoints. Values 0 or 1 mean no limit. It does not limit `rawPoints` or smoothing's intermediate allocation. |
| `useFolow` | Moves the rendered endpoint with the pointer between accepted raw points. Live collision follows that endpoint too. The serialized spelling is retained for compatibility. |
| `minCountCreate`, `minDistanceCreate` | Required raw point count and raw polyline length for keeping a stroke; defaults are 2 and 0.2 world units. |
| `maxDistanceCreate` | Positive values finish the stroke when its raw distance reaches or exceeds the threshold; the last segment can overshoot it. Zero disables this check. |
| `drawRelease` | Positive values finish a mouse-held stroke after this many seconds. Zero disables the drawing-time limit. |
| `deleteAfterRelease` | Positive values schedule deletion after completion; zero keeps completed lines until explicit deletion or owner destruction. The delay uses the Drawer coroutine owner and scaled game time. |

Static helpers require non-null inputs. Use finite coordinates and sensible nonnegative settings; Inspector attributes do not validate assignments from C#. `Smooth(points, passes, fixedZ)` returns a new list; a non-null `fixedZ` applies to every output point even with zero passes or fewer than three inputs. Pass `null` to preserve 3D coordinates. `LimitPoints(points, max)` returns the input list when no reduction is needed, so do not assume a copy. `GetDistance` accepts either `Vector3[]` or `List<Vector3>` and measures consecutive segments.

## Visuals and templates

`lineMaterial` is cloned for each acquired line. `lineSprite` takes precedence over `lineTexture` for its texture override. With no explicit material or texture override, an existing template/prefab material is borrowed; otherwise a default material is created when needed. Drawer destroys only materials it creates. Each pooled acquisition registers one exact-owner/generation cleanup on `PooledObjectInfo`: return, eviction, or destruction restores the borrowed material and frees that generation's clone once, before reuse. An externally assigned replacement material is preserved.

`useTemplateSettings` with `templateRenderer` clones that renderer when no pooled renderer is acquired. Drawer then applies its own `color`, width, `textureMode`, `textureScale`, `alignment`, `cornerVerts`, `capVerts`, `loop`, and sorting settings. A pool prefab takes precedence over this template. All acquired renderers use world-space positions, even if the source used local space. `useWidthCurve = false` restores a constant-width curve on reuse.

## Collision and pooling

Enable `addCollider`; use `colliderAfterCreation` for completion-only collision, or leave it off for live updates. Drawer reuses one `EdgeCollider2D` on each renderer, converts world vertices into collider-local coordinates, and disables collision until at least two rendered points exist. `UpdateColliderPoints(collider, worldPoints)` exposes the same conversion for C# callers. The edge radius uses the configured endpoint width, including the width multiplier; variable width and nonuniform scaling are approximations with a single-radius edge collider. `loop` closes the rendered line; it does not add a closing edge to the edge collider.

Enable `usePooling` and provide a scene `PoolManager`. Assign a `LineRenderer` as `poolPrefab` for reusable pools; if absent, Drawer creates an inactive source automatically. Without a manager, renderer creation falls back to normal instantiation. On destruction, Drawer returns its own rentals and calls `SpawnUtility.RetirePool` for only its unique automatic source before destroying the source. Retirement removes that exact registration, clears its inactive objects and prevents new creation. Outstanding external rentals remain alive until a valid return destroys them. Assigned/shared prefab pools and shared storage containers are preserved, including containers shared by sources with the same name.

Use `Delete(renderer)`, `DeleteFirst()`, `DeleteLast()`, or `DeleteAll()` to remove lines. The captured pool owner and spawn generation determine whether a rental is still accessible, even if `usePooling` has changed. Manual deletion cancels the previous expiry. Every later Draw mutation/return, including style/collider updates and expiry, rejects an expired rental. After an external return and reissue to another Drawer, the former Drawer cannot delete or alter the new line/material. `Delete` and `ApplyFallbackSettings` ignore pooled renderers not currently acquired by that Drawer; call the pool API for independently acquired rentals.

`DeleteAll` removes the lines/current stroke captured at entry and resets counters; a listener-created replacement stroke is preserved. External pool cleanup removes the expired line from local tracking and silently clears raw points/counters if it was active. It emits no completion or reactive callbacks while the pool is transitioning that instance. Prefer drawing methods over direct public-list edits to keep logical stroke state coherent.

The cleanup hook has one slot per outstanding generation. If another pool participant already owns that slot, Drawer returns the untouched rental and falls back to ordinary renderer creation. Existing pool lifecycle callbacks and cleanup hooks must not throw or return/re-rent their own transitioning instance; see [PooledObjectInfo](../Spawner/PooledObjectInfo.md).

Destroying Drawer removes its tracked strokes and standalone acquisitions, returns its current pooled generations, and frees its owned materials and automatic source. New drawing requests during destruction are ignored. Keep the coroutine owner active for scheduled expiry; stopping its coroutines or deactivating its GameObject can cancel the delay.

## Events and reactive state

| API | Meaning |
|-----|---------|
| `OnLineStarted(Vector3)` | Starting position on the drawing plane. |
| `OnLineCreated(LineRenderer)` | A completed line that passed creation thresholds. |
| `Distance`, `DistanceValue` | Reactive raw polyline distance and its readable value. |
| `PointCount`, `PointCountValue` | Reactive raw point count and its readable value. |
| `rawPoints`, `CountPoints` | Mutable current-stroke points and their count; the list is cleared on completion. Prefer the drawing methods to keep metrics synchronized. |
| `Timer`, `OnTimerChanged(float)` | Elapsed automatic drawing time; assigning `Timer` invokes the event. |
| `OnRemainingPercent(float)` | Clamped remaining fraction, `1 - Timer / drawRelease`, when the time limit is positive. |

After successful completion, reactive distance/count retain the finished stroke's totals until reset or another stroke begins. Discarding the active stroke resets them. Inspector UnityEvents can call the same public methods, and reactive `OnChanged` listeners can update UI. These are optional scene hooks; C# callers use the same API without event chains.

Кратко: сброс сначала согласованно обновляет точки и оба счётчика, затем публикует события. Новый штрих из обработчика сохраняется. Возврат в пул освобождает материал только прежнего поколения; уничтожение Drawer завершает только его собственный автоматический источник.

## See Also

- [Draw module](./README.md)
- [Tools](../README.md)
