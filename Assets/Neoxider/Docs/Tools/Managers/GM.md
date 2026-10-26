# GM

## Overview
`GM` is the global game state manager. It stores the current and previous game state, controls pause behavior through `Time.timeScale`, sets the target FPS, and notifies other systems through `EM`.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/GM.cs`

## How to use
1. Add `GM` to the scene.
2. Configure the startup state and `Start On Awake`.
3. Enable or disable `Use Time Scale Pause` depending on your pause model.
4. Call `Menu()`, `Preparing()`, `StartGame()`, `Pause()`, `Resume()`, `Win()`, `Lose()`, and related methods from gameplay flow code or scene UnityEvents.
5. Use the Inspector action buttons in Play Mode. They are disabled in Edit Mode because these actions change runtime state and invoke scene listeners.

## Game states
`GM.GameState` includes:
- `NotStarted`
- `Menu`
- `Preparing`
- `Game`
- `Win`
- `Lose`
- `End`
- `Pause`
- `Other`

## Main methods
- `Menu()`
- `Preparing()`
- `StartGame(bool restart = false)`
- `StopGame()`
- `Lose()`
- `Win()`
- `End()`
- `Pause()`
- `Resume()`

## Important behavior
- `GM` respects the base singleton initialization contract and calls `base.Init()`. `fps` sets `Application.targetFrameRate` during initialization; `startOnAwake` controls the initial state notification.
- `Pause()` captures `Time.timeScale` and applies zero before notifying listeners. `Resume()` restores the previous state and the captured time scale before notifying listeners.
- Leaving `Pause` through `StopGame()`, `Preparing()`, `Menu()`, `StartGame()`, or a direct `State` assignment also restores a pause established by this manager. Destroying a paused manager releases its time scale pause.
- The captured value is restored exactly, including zero if another system had already paused time. Changing `useTimeScalePause` while paused does not discard an existing capture.
- When `Use Time Scale Pause` is disabled, `GM` still emits pause/resume events through `EM`.
- An explicit same-state assignment outside a state callback still emits `OnStateChange` and preserves the previous distinct state used by `Resume()`. Repeating the current state from a state callback does not recursively publish the same transition again.
- `State` is a low-level assignment API: it emits `OnStateChange` without the matching gameplay event. Assigning `State = Pause` alone does not capture or pause time; use `Pause()` for that workflow.
- `Win()`, `Lose()`, and `End()` only take effect from `Game`. `IsPlaying` / `IsNotPlaying` reflect the current state.

`GM` owns only its own pause capture. Multiple independent writers of `Time.timeScale` still need application-level coordination.

## Transition version and reentrant notifications

`public ulong TransitionVersion { get; }` identifies the latest effective transition on this exact manager instance. A distinct `State` assignment advances it once by `unchecked(previous + 1UL)` before changing state and before state callbacks. A normal `Pause()` therefore advances it by exactly one; nested effective transitions advance it further. Same-state assignments and guarded no-op `Pause()` / `Resume()` calls do not advance it. Compare versions for equality; unsigned overflow follows the documented unchecked arithmetic.

GM checks the captured transition before subsequent gameplay notifications, including after lazy EM initialization. If `OnStateChange(Pause)` calls `Resume()`, the nested Resume is published and the obsolete outer Pause is suppressed. A resume listener that starts another flow also stops the outer `Menu()` / `StartGame()` from overwriting it. For `Win()` / `Lose()` / `End()`, a transition from `OnStopGame` suppresses the old outcome notification.

This is an intentional 11.0 event behavior change: observers no longer receive the remaining GM gameplay broadcasts of a superseded transition. Normal state/gameplay event order stays synchronous. An already-entered UnityEvent invocation is not interrupted or rolled back; its state argument is the captured announced value, and later listeners can observe that reentry has already changed the manager. Observer exceptions still propagate, with the committed state/version retained and the state-notification guard released.

For ownership of one pause, capture the manager reference and expected generation before the call:

```csharp
GM pausedManager = game;
ulong expectedVersion = unchecked(pausedManager.TransitionVersion + 1UL);
pausedManager.Pause();
bool acquiredPause = pausedManager.State == GM.GameState.Pause &&
                     pausedManager.TransitionVersion == expectedVersion;

// When releasing, also require this exact instance to remain the current singleton.
if (acquiredPause && pausedManager != null &&
    GM.TryGetInstance(out GM current) && ReferenceEquals(current, pausedManager) &&
    pausedManager.State == GM.GameState.Pause &&
    pausedManager.TransitionVersion == expectedVersion)
{
    pausedManager.Resume();
}
```

Only attempt acquisition from a non-Pause state; an existing pause belongs to its existing owner. A later resume/re-pause changes the version even if the final state is Pause. The version is read-only, nonserialized, and never reset on the same instance. It is the same generation used by GM's notification fence, rather than a separate UI counter. For overlapping scene pause pages, use [PausePage](../../UI/PausePage.md), which already shares page ownership and consumes this API.

## C# access

```csharp
using Neo.Tools;

if (GM.TryGetInstance(out GM game))
{
    game.Pause();
    // Later, for example from a resume button:
    game.Resume();
}
```

For scene setup, assign a `GM` reference to the caller or connect a UI button's UnityEvent to `GM.Pause()` / `GM.Resume()`. `EM.Pause()` / `EM.Resume()` broadcast events only and do not perform these state changes.

## Typical flow
1. `GM` applies pause time effects when relevant and changes `State`.
2. `GM` triggers `EM.OnStateChange`.
3. If the same transition is still current, `GM` triggers the matching EM gameplay event.
4. Other systems react synchronously. A pause or resume listener may request another transition; time scale is already consistent before that notification, and remaining obsolete GM broadcasts are skipped.

## See also
- [`EM`](./EM.md)
- [`Bootstrap`](./Bootstrap.md)
- [`Singleton`](./Singleton.md)
