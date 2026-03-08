# EM

## Overview
`EM` is the global event manager for gameplay states, pause flow, and a few application-level callbacks.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/EM.cs`

## How to use
1. Add `EM` as one of your global managers.
2. Connect UI, audio, analytics, and other systems to its `UnityEvent` fields.
3. Trigger events through static helpers such as `EM.Menu()`, `EM.GameStart()`, `EM.Pause()`, and so on.
4. In most projects `GM` changes the state and `EM` broadcasts reactions.

## Main events
- `OnMenu`
- `OnPreparing`
- `OnGameStart`
- `OnRestart`
- `OnStopGame`
- `OnWin`
- `OnLose`
- `OnEnd`
- `OnStateChange`
- `OnPause`
- `OnResume`
- `OnPlayerDeath`
- `OnAwake`
- `OnFocusApplication`
- `OnPauseApplication`
- `OnQuitApplication`

## Static helpers
- `Preparing()`
- `GameStart()`
- `Lose()`
- `Win()`
- `End()`
- `StopGame()`
- `PlayerDied()`
- `Pause()`
- `Resume()`
- `Menu()`
- `Restart()`

## Typical flow
1. `GM` changes its state.
2. `GM` invokes the matching `EM` helper.
3. `EM` broadcasts the event to all listeners.

## See also
- [`GM`](./GM.md)
- [`Singleton`](./Singleton.md)
