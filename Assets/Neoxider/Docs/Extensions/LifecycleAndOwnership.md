# Extension lifecycle and ownership

This companion to the [Extensions entry point](README.md) describes cancellation, native-object ownership and coordinate contracts. Reference `Neo.Extensions` and use `using Neo.Extensions;` from C#.

## Coroutine ownership

| Entry point | Lifetime |
| --- | --- |
| `owner.Delay`, `NextFrame`, `WaitUntil`, `WaitWhile`, `RepeatUntil`, `DelayFrames`, `EndOfFrame` | The supplied `MonoBehaviour` owns the coroutine. Destroying that component cancels it even when its GameObject survives. |
| `gameObject.Delay`, `WaitUntil`, `WaitWhile`, `DelayFrames` | A package `CoroutineRunner` on that GameObject owns the coroutine. |
| Static `CoroutineExtensions.Delay`, `WaitUntil`, `WaitWhile`, `DelayFrames`, `Start` | A persistent package helper owns the coroutine across scene changes. |

Deactivating an owning GameObject or an ancestor cancels its operations. Re-enabling the hierarchy does not resume them. Starting on an inactive hierarchy returns a completed handle without running the action. Disabling only the owner's `enabled` property keeps its coroutines running, matching [Unity's coroutine lifecycle](https://docs.unity3d.com/6000.0/Documentation/Manual/Coroutines.html).

Null/destroyed owners use the global fallback; they do not provide scene-bound cancellation. `CoroutineExtensions.Start(null)` returns a completed handle and creates no helper. These APIs are for Unity's main thread and Play Mode.

Store and cancel the returned handle:

```csharp
private CoroutineExtensions.CoroutineHandle _pending;

public void Schedule()
{
    _pending?.Stop();
    _pending = this.Delay(2f, Refresh, useUnscaledTime: true);
}

private void OnDisable()
{
    // Optional policy: also cancel when this component alone is disabled.
    _pending?.Stop();
    _pending = null;
}

private void Refresh()
{
    // Update this component's state.
}
```

`Stop()` is idempotent. It clears `IsRunning` and `Coroutine`, releases tracked iterators from child to parent, and runs their `IDisposable.Dispose`/iterator `finally` cleanup. Cancellation from within the executing routine defers disposal until that advancement returns. Cleanup failures are reported through the existing `NeoDiagnostics` error gate while other tracked iterators continue to be released.

The same deferral applies if Unity finalizes a wrapper while its original iterator is executing `MoveNext` or `Current`: terminal state is set first, and owned iterators stay registered until advancement unwinds. When a root wrapper is finalized, the tracked tree is completed before releasing that root, preserving child-to-parent cleanup. This does not make arbitrary raw native stops observable on every Unity execution path; keep using the tracked cancellation API below.

An iterator that finishes synchronously, including `DelayFrames(0, ...)`, can execute its callback before the API call returns. Its returned handle stays completed. [Unity can return null from `StartCoroutine` for synchronous completion](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/monobehaviour/startcoroutine); callers should check the handle's state instead of expecting a non-null native `Coroutine`.

Nested `IEnumerator` values yielded by a routine belong to that operation. `CustomYieldInstruction`, including `WaitUntil`, `WaitWhile` and `WaitForSecondsRealtime`, retains Unity's native polling phase and participates in tracked fault/disposal handling. An independently started coroutine yielded as a native `Coroutine` has its own lifetime. At subsystem reset, cleanup cannot schedule replacement work on a helper that is being reset.

**Direct external stops:** `owner.StopCoroutine(handle.Coroutine)` and `owner.StopAllCoroutines()` provide no package completion notification. The handle can continue to report running and retain its iterators. Call `handle.Stop()` for cancellation; it remains safe to call after a raw external stop. `Coroutine` is exposed for Unity interoperation, including yielding, rather than automatic observation of every native stop.

Actions retain the existing exception behavior: delayed action failures use the diagnostics gate; predicate/custom iterator failures complete the tracked handle and propagate to Unity. End-of-frame and fixed-update operations retain their respective Unity scheduling requirements. Use valid, finite timing values and non-null predicates.

## Audio and transform operations

`AudioSource.FadeTo`, `FadeOut` and `FadeIn` return global coroutine handles. Destroying their source ends the fade; disabling the source GameObject alone does not cancel the global operation. Fades use scaled `Time.deltaTime`, so they pause at `Time.timeScale == 0`. A fade to zero stops playback at completion; cancelling a fade leaves the current volume in place. `FadeIn` starts playback at zero volume when the source is not already playing.

Concurrent fades remain independent operations. Keep the previous handle and stop it before replacing a fade on the same source:

```csharp
_fade?.Stop();
_fade = source.FadeTo(0.4f, 0.5f);
```

`transform.SmoothLookAtRoutine(target, speed)` returns an iterator to run with `StartCoroutine` or `CoroutineExtensions.Start`. Destroying the transform ends it. An absent transform or a target coinciding with its position completes without changing rotation. Use a positive, finite speed for convergence.

## Preview sprites and event subscriptions

`prefab.GetPreviewSprite()` first borrows an existing child `SpriteRenderer.sprite` or UI `Image.sprite`. The caller must not destroy those assets. In the Editor, the thumbnail fallback creates and caches a Sprite owned by this utility; repeated lookups of the same thumbnail reuse it. Thumbnail textures remain owned by Unity's `AssetPreview` service. In a player build there is no editor-thumbnail fallback.

`PrefabPreviewExtensions.ClearPreviewSpriteCache()` releases only generated Sprites and clears their cache. Destruction is immediate in Edit Mode and scheduled through Unity in Play Mode. Discard generated preview references after clearing and obtain fresh previews when needed. Cleanup also runs before editor assembly reload, on editor shutdown, when leaving Play Mode and at runtime subsystem initialization, including sessions with domain reload disabled. Generated Sprites have `HideAndDontSave`; existing renderer/image assets are untouched.

Keep generated thumbnails out of serialized item/configuration fields. Store authored Sprite assets there and request a temporary fallback in the view when binding or refreshing it. A cache clear invalidates the fallback; it must not invalidate the persistent authored icon.

`UnityEventDelegateCache.SubscribeAt` owns the subscription it tracks for a slot, replacing the previous source/listener pair. Explicitly call `UnsubscribeAt(index, originalEvent)` for each slot during view teardown. `Clear()` preserves its existing bookkeeping-only contract and does not unsubscribe. Delegates registered through `Add` require the caller to retain the original event. Prefer a distinct callback for each independently removable slot: Unity removes matching listeners by delegate identity, so sharing one callback across slots does not provide independent subscription ownership.

## Camera and canvas coordinates

`camera.GetWorldPositionAtScreenEdge(edge, offset, depth)` uses that camera's `pixelRect`, including its origin, rather than the full display dimensions. `offset` is measured in screen pixels; `depth` is measured forward from the near clip plane for both perspective and orthographic cameras. Supported placement values are the four edges, four corners and `Center`; `Front`/`Back` are depth checks for `IsOutOfScreenSide`, not placement locations.

Orthographic callers that previously treated `depth` as a distance from the camera should now pass `previousDepth - camera.nearClipPlane` to preserve the same world depth. This corrects the method's documented near-plane-relative contract. `ScreenPositioner` consumers with depth enabled should review that value.

`GetWorldScreenBounds(distance)` continues to use camera-relative distance and encloses all four viewport corners in a world-axis-aligned box. `IsOnScreen` checks normalized X/Y and non-negative camera depth; it does not test renderer visibility, occlusion or both clipping planes. `GetClosestScreenEdgePoint` clamps the projected viewport point and preserves its existing screen-boundary behavior; it is not a ray/plane intersection API.

`UIUtils.WorldToCanvasPoint(canvas, worldPoint, camera)` returns local coordinates in the canvas `RectTransform`. Supply the world projection camera explicitly. Overlay uses no UI camera; Camera/World Space canvases use `canvas.worldCamera` with the projection camera as fallback. Layout utilities write world positions; parent transforms do not turn their results into local coordinates.

For finite HDR `Color` values, `ToHexString()` clamps each channel to `[0, 1]` before producing `#RRGGBBAA`. Its existing byte truncation is preserved (`0.5f` becomes `7F`).

## UI raycasts with explicit pointer coordinates

`UIUtils.GetUIElementsAtPosition(Vector2 screenPosition)` returns a new caller-owned result list. `UIUtils.GetUIElementsAtPosition(Vector2 screenPosition, List<RaycastResult> results)` instead clears and fills the supplied list and returns its result count. Both take screen pixels, use the current EventSystem, and perform no input-backend read. They preserve EventSystem raycaster results and ordering. The supplied buffer is cleared even without an EventSystem; a null buffer throws `ArgumentNullException`.

```csharp
using System.Collections.Generic;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

private readonly List<RaycastResult> _pointerHits = new();

public bool CheckPointer(Vector2 screenPosition)
{
    return UIUtils.GetUIElementsAtPosition(screenPosition, _pointerHits) > 0;
}
```

Pass coordinates from the input system already used by the game. An optional upper input module can obtain them through `Neo.Tools.MouseInputCompat.TryGetPosition`; Extensions does not depend on that module. The caller-buffer form avoids allocating a new result list on every query, while `PointerEventData` is still created per call. The legacy parameterless `GetUIElementsUnderCursor()` and `IsPointerOverUI()` retain their legacy `Input.mousePosition` behavior; use the coordinate path with an appropriate input adapter in New Input System-only projects.

## Scene and NoCode integration

Coroutine runners and lifecycle trackers are implementation helpers; ordinary scene authoring does not require wiring them manually. A scene component can expose typed methods such as `Schedule`, `CancelFade` or `ClearPreview` to UnityEvents while retaining handles in C#. Cancel according to that component's lifecycle policy. Keep scene object references on the component, and keep the operation and cleanup logic in the same typed methods used by C# callers.

**По-русски:** отменяйте операции через сохранённый `CoroutineHandle.Stop()`. Деактивация иерархии и уничтожение компонента-владельца завершают связанные операции; глобальные операции переживают смену сцены. Сгенерированные preview Sprite принадлежат кэшу, а исходные Sprite и thumbnail-текстуры ему не принадлежат. Координаты экранных краёв учитывают viewport камеры, а `depth` отсчитывается от near clip plane в обоих режимах проекции. Для UI можно явно передать экранные координаты и свой список результатов: перегрузка очищает его перед запросом, не обращаясь к input backend. В serialized конфигурации храните авторские Sprite, временный preview запрашивайте при обновлении view.
