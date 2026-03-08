# GM

## Overview
`GM` is the global game state manager. It stores the current and previous game state, controls pause behavior through `Time.timeScale`, sets the target FPS, and notifies other systems through `EM`.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/GM.cs`

## How to use
1. Add `GM` to the scene.
2. Configure the startup state and `Start On Awake`.
3. Enable or disable `Use Time Scale Pause` depending on your pause model.
4. Call `Menu()`, `Preparing()`, `StartGame()`, `Pause()`, `Resume()`, `Win()`, `Lose()`, and related methods from gameplay flow code.

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
- `GM` now respects the base singleton initialization contract and calls `base.Init()`.
- Pause stores the previous `Time.timeScale` and restores it on resume.
- When `Use Time Scale Pause` is disabled, `GM` still emits pause/resume events through `EM`.

## Typical flow
1. `GM` changes `State`.
2. `GM` triggers `EM.OnStateChange`.
3. `GM` triggers the matching event helper such as `EM.GameStart()` or `EM.Win()`.
4. Other systems react independently.

## See also
- [`EM`](./EM.md)
- [`Bootstrap`](./Bootstrap.md)
- [`Singleton`](./Singleton.md)
