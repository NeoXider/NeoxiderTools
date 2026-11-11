# GridGameBuilder

**What it is:** `GridGameBuilder` (`Scripts/GridSystem/GridGameBuilder.cs`, namespace `Neo.GridSystem`) is the scene-facing GridSystem constructor. It adds and keeps selected grid modules on one GameObject: debug drawer, spawners, Match3, TicTacToe, and SlidingMerge.

**How to use:**
1. Add `GridGameBuilder` to a GameObject (a `Grid` and `FieldGenerator` are required and added with it).
2. Select `Features` and press **Ensure Grid Components** (also runs in `Awake`).
3. Configure the added components; `Generate Field On Awake` regenerates the field at startup.

---

**Use it when:** you want fast Inspector setup without manually adding every component. Production code can still use the same components directly.

## API

- `Features` - selected module flags.
- `EnsureConfigured()` - adds selected components without removing existing components.
- `Generator` - current `FieldGenerator` reference.

## Principle

`GridGameBuilder` does not own gameplay rules. It only assembles the scene object. Rules stay in focused services:

- `Match3BoardService`
- `TicTacToeBoardService`
- `SlidingMergeBoardService`
- custom runtime services

## Example

For a 2048-like game:

1. Add `GridGameBuilder`.
2. Enable `DebugDrawer` and `SlidingMerge`.
3. Set `FieldGenerator.Config.Size = (4, 4, 1)`.
4. Connect input/view to `SlidingMergeBoardService.Slide(...)`.

For Match3:

1. Enable `DebugDrawer` and `Match3`.
2. Configure size and shape mask.
3. Connect the board view to `Match3BoardService.OnBoardChanged`.
