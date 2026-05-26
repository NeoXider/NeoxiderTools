# GridGameBuilder

**Purpose:** scene-facing GridSystem constructor. It adds and keeps selected grid modules on one GameObject: debug drawer, spawners, Match3, TicTacToe, and SlidingMerge.

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
