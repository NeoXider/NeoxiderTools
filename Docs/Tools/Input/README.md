# Tools / Input

**What it is:** keyboard polling, swipe gestures, mouse picking and cursor effects that work with the legacy Input Manager and the Input System alike. Scripts live in `Scripts/Tools/Input/` (namespace `Neo.Tools`); custom game assemblies must reference `Neo.Tools.Input`.

**How to work with it:**
1. From code, read input through `KeyInputCompat`, `MouseInputCompat` and `MoveInputCompat` instead of `UnityEngine.Input`.
2. In a scene, add `MouseInputManager` (assign its camera), `SwipeController` or `MultiKeyEventTrigger` and wire their events.
3. On touch devices the first finger acts as the mouse; see [Touch as mouse](#touch-as-mouse).

**Contents:** the [per-component pages](#docs-per-component), the [runtime API](#runtime-api), the
[polling model and cost](#polling-model-and-cost), [UI picking](#ui-picking-with-compatible-mouse-input) and the
[scene and Inspector workflow](#scene-and-inspector-workflow).

**Navigation:** [← Tools](../README.md)

---

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
- `MouseInputCompat.TryGetScrollDelta(out Vector2)` reads the mouse wheel this frame (`y` is the vertical wheel) and `TryGetPointerDelta(out Vector2)` the pointer movement this frame in pixels, on both backends and every supported Unity version (legacy: `Input.mouseScrollDelta` and `Input.mousePositionDelta`; Input System: `Mouse.scroll` and `Mouse.delta`). A pointer delta already accumulates per frame, so do not multiply it by `Time.deltaTime`. One wheel notch reads about 1 on both backends by default (on the Input System with its default **Scroll Delta Behavior**, Uniform Across All Platforms); the exact step is platform-dependent, so scale it by your own sensitivity.
- `MoveInputCompat.ReadMove()` reads two-axis movement on both backends: the legacy Input Manager's raw `Horizontal` / `Vertical` axes (`ReadMove(horizontalAxis, verticalAxis)` names others), or WASD, the arrow keys and the gamepad left stick on the Input System. Each component stays in -1..1 and the diagonal is not normalized. An axis name that the Input Manager does not define throws `ArgumentException`, like `Input.GetAxisRaw`; the Input System path ignores the names.

  ```csharp
  Vector2 move = MoveInputCompat.ReadMove();
  if (move.sqrMagnitude > 1f)
  {
      move.Normalize();
  }

  transform.position += new Vector3(move.x, move.y, 0f) * speed * Time.deltaTime;
  ```

- `MouseInputManager.I` provides typed mouse events and optional collider picking; inject a camera with `SetTargetCamera(Camera)`.
- `MouseInputManager.OnCanceled` reports an interrupted active press after clearing it, without a synthetic Release or Click.
- `SwipeController.OnSwipe` publishes `SwipeData`. `GetSwipeDirection(out SwipeDirection)` consumes the latest pending direction once.

`InputBackendCompat` shares selection across key, mouse and touch readers. Legacy-only and Both configurations prefer the legacy Input Manager; New-only configurations skip it. If an attempted legacy read rejects access, all compatible readers use the optional fallback until subsystem reset or an explicit `InputBackendCompat.ResetBackendSelection()`. Do not reset it per frame. Device connection changes do not invalidate backend configuration; optional reads consult the current device each time. `UsesLegacyInput` reports the selected preference, not device presence. `IsLegacyInputConfigured` reports only the Player Settings choice (Input Manager or Both), regardless of later read failures; use it to skip legacy-only setup in Input System-only projects without a throwing probe.

The Input System package remains optional; no direct `Unity.InputSystem` assembly reference is required. Missing devices produce unavailable/neutral reads. Swipe touch input uses legacy finger IDs or the Input System's primary touch.

### Touch as mouse

On a device without a mouse, `MouseInputCompat` reports the first finger as the pointer and the left button (index `0`) on both backends, so `MouseInputManager`, `MouseEffect`, `InteractiveObject` clicks and any other `MouseInputCompat` reader work on phones and tablets:

- the legacy Input Manager does it through Unity's `Input.simulateMouseWithTouches` (on by default);
- the Input System path reads the current touchscreen's primary touch (`Touchscreen.position`, `delta` and `press`) whenever no `Mouse` is connected, while `OptionalInputSystemAdapter.SimulateMouseWithTouches` is on (the default; set it to `false` to keep touch out of mouse reads; subsystem registration restores `true`).

A connected mouse always wins. The right and middle buttons and the scroll wheel never come from touch. Like the legacy backend, the position stays at the last touch after the finger lifts. `SwipeController` reads touches directly and needs none of this.

Touch-as-mouse is not UI-aware: a tap on a HUD button or a virtual joystick is also a left click for every world reader. Gate world input yourself, for example with `UIInputCompat.IsPointerOverUI()`.

#### Upgrading from 11.x (behaviour change in 12.0)

In Input System-only projects (**Active Input Handling** = *Input System Package (New)*) every `MouseInputCompat` reader follows the primary touch as the left button and pointer; earlier versions ignored fingers there. Legacy Input Manager and *Both* projects see no change, because `Input.simulateMouseWithTouches` already did the same. Readers that follow touch in Input System-only projects:

- Tools/Input: `MouseInputManager` (Press/Hold/Release/Click and picking), `MouseEffect`, `UIInputCompat.IsPointerOverUI` / `GetUIElementsUnderCursor` (a finger counts as the cursor) and the mouse path of `SwipeController` (its touch path keeps priority, so a swipe is not detected twice).
- Tools/InteractableObject: `InteractiveObject` mouse interaction and its pointer ray.
- Tools/Move: `CameraRotationController` drag rotation, `MouseMover2D` / `MouseMover3D` moving to the tapped point, `UniversalRotator` pointer aiming, and `FreeFlyCameraController` only when its look button is set to the left button (the default is the right button).
- Tools/Draw: `Drawer`.
- Rpg: `RpgButtonBinding` with a mouse-button trigger on the left button, which is the default attack binding, so every tap attacks, including taps on on-screen controls, unless you gate it.
- Samples: the GridSystem demo board views, `DemoPointerGate` and `FollowDemoController`.

To keep touch out of mouse reads, switch the fallback off once per run. Subsystem registration restores it to `true`, so set it from a `BeforeSceneLoad` hook (or later); there is no Inspector switch:

```csharp
using Neo.Tools;
using UnityEngine;

public static class KeepTouchOutOfMouse
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        OptionalInputSystemAdapter.SimulateMouseWithTouches = false;
    }
}
```

`SimulateMouseWithTouches` affects only the Input System path. In legacy or *Both* projects set Unity's `Input.simulateMouseWithTouches = false` instead.

`OptionalInputSystemAdapter` also exposes `ReadMove`, `ReadPointerDelta`, `ReadLookStick`, `ReadLookDelta`, `ReadJumpPressed`, `ReadJumpHeld` and `ReadRunHeld`, plus `TryGetMouseDelta` and `TryGetMouseScroll`, which back the two compatible reads above. Pointer delta is a per-frame pixel displacement; a look stick is a continuous rate, so scale it by elapsed time separately. `ReadPointerDelta` and the pointer part of `ReadLookDelta` read only a mouse (never touch) and give zero without one; `TryGetMouseDelta` follows the same pointer as `TryGetMousePosition`, including touch, and reports whether one exists. `ReadLookDelta` combines the sources for existing callers. `ReadMove` can return an unnormalized diagonal. `IsAvailable` describes available Input System types, not a connected device. See [Polling model and cost](#polling-model-and-cost) for allocation and CPU behaviour.

## Polling model and cost

Every compatibility read samples the input backend at the moment of the call; nothing is cached per frame, so a second Input System update inside one Unity frame is observed. With the legacy Input Manager the readers forward to `UnityEngine.Input` and do not allocate.

The Input System path (`OptionalInputSystemAdapter`, used by New-only projects and after a rejected legacy read) has no compile-time dependency on the package. It works in three layers:

1. **Device**: the static `current` accessor of `Keyboard`, `Mouse`, `Gamepad` and `Touchscreen` is bound once to a delegate and invoked on every read, so device replacement and removal are seen immediately.
2. **Control**: the control property (`eKey`, `position`, `primaryTouch`, ...) is looked up through a per-type property cache and read from the live device on every call. A replaced control is never served from an older binding.
3. **Value**: per control, the `isPressed` / `wasPressedThisFrame` / `wasReleasedThisFrame` getters and the `ReadValue()` method of `Vector2`, `int` and `int`-based enum controls (touch phase) are bound once to typed delegates. The binding table holds controls weakly: removed devices and controls are collected without a reset.

The first read of a control binds it and allocates once. After that, reads of keys, mouse buttons, mouse position and delta, gamepad sticks and buttons and the primary touch make no managed allocation. A control whose getter cannot be bound (an unusual signature, a non-`int` enum, an object-returning `ReadValue`, or a runtime that rejects delegate binding) keeps the previous reflected read, which boxes and therefore allocates on every call; results and exceptions are identical in both paths (getter faults surface as `TargetInvocationException`). `SubsystemRegistration` drops all cached metadata and bindings.

The zero-allocation guarantee holds on the Mono scripting backend. IL2CPP player builds are not measured: the per-read control lookup goes through `PropertyInfo.GetValue`, which Mono serves from a cached getter delegate but IL2CPP serves as a reflected invoke.

### Managed code stripping (IL2CPP)

No `link.xml` is needed. The adapter binds only members of Input System devices and controls (`Keyboard.current`, `Mouse.current`, key and button properties, `isPressed` / `wasPressedThisFrame` / `wasReleasedThisFrame`, `ReadValue()`), and the Input System package preserves them itself: its build step `LinkFileGenerator` (an `IUnityLinkerProcessor`) writes a link file that keeps every `InputControl` and `InputDevice` type in full, at any stripping level. If a custom build pipeline drops that generated file, keep the package by hand in a `link.xml` anywhere under `Assets`:

```xml
<linker>
  <assembly fullname="Unity.InputSystem" preserve="all" />
</linker>
```

A getter that is stripped anyway makes its control report unavailable (a `false` read) instead of throwing; a runtime that rejects delegate binding keeps the reflected read, with the same results but an allocation per read.

`MouseInputManager` adds picking and event dispatch on top of polling; a warmed Press/Hold/Release/Click cycle with physics picking also allocates nothing, apart from what your own handlers allocate.

To measure on your machine, run the explicit `Performance` test category (`-testCategory Performance`); it prints one `NEO_PERF` line per case. Compare lines only from the same machine and Unity version.

### Measured results

One Play Mode run on virtual Input System devices (Input System path, Unity 6000.3.14f1 Editor, Mono, batch mode, profiler off). Times are medians per operation, where an operation is the named consumer's reads for one frame; none allocates. Read the ratios, not the absolute times: the Editor adds overhead.

| Operation | Time |
|---|---:|
| Key down + key up (`InteractiveObject` keyboard check) | 0.41 µs |
| Mouse position plus left-button down, held and up (`MouseInputManager.Update`) | 1.02 µs |
| Primary touch (`SwipeController.Update`) | 0.92 µs |
| `ReadMove` (eight keys and the left stick) | 3.55 µs |

For scale: three `KeyInputCompat` reads cost 0.09 µs through the legacy Input Manager and 0.64 µs through the adapter. A warmed `MouseInputManager` Press/Hold/Hold/Release/Click cycle with eight `Action` and eight `in` subscribers per event takes 12.8 µs.

## UI picking with compatible mouse input

`UIInputCompat.IsPointerOverUI()` and `GetUIElementsUnderCursor()` sample through `MouseInputCompat`. `IsPointerOverUI()` reuses the shared pointer and result buffer of `UIUtils.IsPointerOverUI(Vector2)`, so a per-frame "is the cursor over UI" check creates no result list or `PointerEventData` per call; raycaster work inside the EventSystem remains its own cost. When you need the hits themselves, retain a result list:

```csharp
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine.EventSystems;

List<RaycastResult> hits = new();
int count = UIInputCompat.GetUIElementsUnderCursor(hits);
```

The caller-buffer overload clears the list, including when no mouse or EventSystem is available; a null buffer throws `ArgumentNullException`. It reuses your list and the shared `PointerEventData` of the lower UI utility, so with a retained list it adds no allocation of its own; only a query issued from inside another query (for example by a custom raycaster) creates a temporary pointer. Custom asmdefs using RaycastResult also reference `UnityEngine.UI`.

The shared pointer belongs to the EventSystem it last served. After that EventSystem is destroyed, the pointer keeps referencing its managed wrapper until a query runs with another EventSystem or subsystem registration resets it; the native object is gone, so this is not a growing leak, but a memory snapshot can list the destroyed EventSystem.

For touch, injected coordinates or other pointer policies, use the input-independent `Neo.Extensions.UIUtils.GetUIElementsAtPosition(Vector2, List<RaycastResult>)` directly. Its legacy `GetUIElementsUnderCursor()` / `IsPointerOverUI()` APIs retain legacy mouse semantics; migrate those calls to `UIInputCompat` for New-only projects. Extensions remains independent of the Input assembly. UI filtering is opt-in and is not automatically applied to manager events.

## Scene and Inspector workflow

Add `MouseInputManager` and assign its camera for mouse-based interaction. Add `MouseEffect` for trails, followers or prefab spawning; enable the manager event modes it needs. `SwipeController.OnSwipe` and each `MultiKeyEventTrigger.KeyEventPair.onKeyPressed` can be wired in the Inspector or subscribed from C#. No ScriptableObject or UnityEvent chain is required for the runtime polling API.

Manager event switches filter notifications independently. Disabling a component or losing application focus cancels its active gesture. Subsystem registration clears static polling/instance caches; the mouse bootstrap enables lazy singleton creation on first `I` access.

## Кратко по-русски

`KeyInputCompat`, `MouseInputCompat` и `SwipeController` читают ввод в момент вызова: при Legacy Input Manager — напрямую через `UnityEngine.Input`, в New-only проектах — через `OptionalInputSystemAdapter` без жёсткой зависимости от пакета Input System. Адаптер один раз привязывает к делегатам `current` устройства и геттеры контролов (`isPressed`, `wasPressedThisFrame`, `wasReleasedThisFrame`, `ReadValue()` для `Vector2`, `int` и фазы касания), но каждое чтение берёт живое устройство и живой контрол, поэтому замена устройства и несколько обновлений Input System за кадр видны сразу. Таблица привязок держит контролы слабыми ссылками.

После первого чтения контрола опрос клавиатуры, мыши, геймпада и primary touch не выделяет управляемую память (бэкенд Mono; IL2CPP не измерялся). Нестандартные контролы, которые нельзя привязать, читаются через рефлексию с тем же результатом, но с аллокацией.

`MouseInputCompat.TryGetScrollDelta` читает колесо мыши, `TryGetPointerDelta` — смещение указателя за кадр в пикселях, оба на обоих бэкендах. `MoveInputCompat.ReadMove()` читает движение по двум осям: в Legacy — сырые оси `Horizontal`/`Vertical` (имена можно передать), в Input System — WASD, стрелки и левый стик; диагональ не нормализуется. На устройствах без мыши первый палец считается указателем и левой кнопкой: в Legacy через `Input.simulateMouseWithTouches`, в Input System через `OptionalInputSystemAdapter.SimulateMouseWithTouches` (по умолчанию включено). Подключённая мышь всегда в приоритете; правая и средняя кнопки и колесо от касаний не приходят. Касание не учитывает UI: тап по кнопке HUD или виртуальному джойстику — это и левый клик для всех читателей мира, фильтруйте сами, например через `UIInputCompat.IsPointerOverUI()`.

В проектах только с Input System на касание реагируют все читатели `MouseInputCompat` (список выше), включая `RpgButtonBinding` с левой кнопкой мыши: каждый тап атакует. Чтобы касания не попадали в чтение мыши, выставьте `OptionalInputSystemAdapter.SimulateMouseWithTouches = false` из метода с `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`; в Legacy/Both — `Input.simulateMouseWithTouches = false`.

`InputBackendCompat.IsLegacyInputConfigured` сообщает, включён ли Legacy Input Manager в Player Settings. Для IL2CPP `link.xml` не нужен: пакет Input System сам сохраняет при сборке все типы контролов и устройств.

## See also

- [Tools/Move](../Move/README.md)
