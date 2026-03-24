# CursorLockController

## Overview

`CursorLockController` manages cursor lock state (`Cursor.lockState`) and visibility (`Cursor.visible`). It supports start state, optional apply on `OnEnable` / `OnDisable`, a master enable switch, toggle hotkey, optional cursor access key, and **stacked ownership**: the most recent controller wins until it releases control or is destroyed.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`

---

## Preset (inspector quick setup)

At the top of the inspector, **Preset** applies a bundle of lifecycle/toggle fields. Changing a non-**Custom** preset in the editor overwrites those fields in `OnValidate`. Use **Custom** for full manual control.

| Preset | Purpose |
|--------|---------|
| **Custom** | All fields manual. |
| **Gameplay_Default** | Start locked+hidden, Escape toggle, no lifecycle on Enable/Disable. |
| **UI_Page_ShowCursorWhileActive** | Overlay: **SaveOnEnable** + **After Disable = RestorePrevious** (PausePage-like when stack is empty); with gameplay below, the stack restores the lower controller. |
| **UI_MenuScene_Standalone** | Menu-only: show on `OnEnable`; **snapshot None**; `Apply On Disable` off — no forced lock when the object disables. |

---

## Lifecycle snapshot (optional)

- **None** — only `_lockOnEnable` / `_lockOnDisable`, no snapshot (legacy).
- **SaveOnEnable** — before applying OnEnable, save `Cursor.lockState` and `Cursor.visible`. After `ReleaseControl` on OnDisable, if **no controller is on top of the stack**, **After Lifecycle Disable** runs: **RestorePrevious**, **ForceLockedHidden**, or **ApplyConfigured** (`_lockOnDisable`). If another controller sits below, it reapplies — the snapshot flag is cleared.
- **SaveOnDisable** — reverse: at the start of OnDisable (when apply is enabled), save cursor; on the next **OnEnable**, **After Lifecycle Enable** chooses **RestorePrevious** or normal **ApplyConfigured** (`Acquire` via `_lockOnEnable`).

---

## Controller stack and scene changes

Active controllers are kept in a **static list**. The top entry drives `Cursor`; `ReleaseControl()`, disable, or **destroy** of the top controller restores the previous one.

- Destroyed instances are **removed** when the stack is accessed and on **`SceneManager.sceneLoaded`**, then the new top controller reapplies its state — fixes stale stack entries after `LoadScene` (e.g. menu with no player).
- **Additive** loads: the list is not fully cleared; only invalid entries are removed so a gameplay controller can remain under additive UI.

---

## Mode

- **LockAndHide** — locked = lock + hide; unlocked = unlock + show.
- **OnlyHide** — `Cursor.visible` only.
- **OnlyLock** — `Cursor.lockState` only.

### Control Mode

- **AutomaticAndManual** — lifecycle, hotkey, and direct API calls.
- **AutomaticOnly** — `Start` / `OnEnable` / `OnDisable` / hotkey only.
- **ManualOnly** — only `SetCursorLocked`, `ShowCursor`, `HideCursor`, `ToggleCursorState`, etc.

### Cursor Access Key

Optional shortcut (e.g. **Z**) for temporary cursor access; off until **Allow Cursor Access Key** is enabled. **Hold** or **Toggle** modes restore the previous state when released/toggled back.

---

## UI-only scene (no player)

1. Add `CursorLockController` to the UI root / Canvas.
2. Set **Preset = UI_MenuScene_Standalone** (or equivalent manual flags: show on enable, do not force lock on disable, `Lock On Start = false`).
3. Keep **Controller Enabled** on and avoid **ManualOnly** if you rely on lifecycle without code calls.

---

## Typical use: gameplay + UI page

Put gameplay `CursorLockController` on the player (or use **Gameplay_Default** preset) and a second controller on the menu/pause object (**UI_Page_ShowCursorWhileActive** or manual: show on enable, lock on disable).

If the cursor controller is not on the player, assign it to **PlayerController3DPhysics → External Cursor Lock Controller**.

---

## PausePage

**PausePage** with **Control Cursor** shows the cursor while paused. On disable, **After Pause Cursor** defaults to **RestorePrevious**; use **ForceLockedHidden** for classic FPS after closing pause. See [`PausePage`](../../../Docs/UI/PausePage.md) (Russian doc; no EN mirror in this package).

---

## Public API

- `bool IsLocked`
- `bool ControllerEnabled`
- `bool HasCursorOwnership`
- `ControlMode Mode`
- `ConfigurationPreset Preset`
- `LifecycleSnapshotMode SnapshotMode`
- `void SetCursorLocked(bool locked)`
- `void ToggleCursorState()`
- `void ShowCursor()` / `void HideCursor()`
- `void ReleaseControl()`
- `void SetControllerEnabled(bool enabled)`
- `void EnableController()` / `void DisableController()`

---

## See also

- [`PlayerController3DPhysics`](./PlayerController3DPhysics.md)
- [`Move`](./README.md)
