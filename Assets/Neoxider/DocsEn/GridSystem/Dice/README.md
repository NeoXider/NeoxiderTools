# GridSystem Dice

`Neo.GridSystem.Dice` is a reusable dice-placement layer built on `FieldGenerator` and the generic `Neo.Merge` engine.

## Runtime API

- `DicePiece` describes a single die, a pair, or a larger footprint via local offsets, with `CellCount` and rotation helpers that work for any cell count (rotated around the anchor).
- `DicePieceGenerator` creates single/pair pieces from a value pool, with equal single/pair odds and no duplicate values inside a pair.
- `DiceBoardService` checks placement, writes values into `FieldCell.ContentId`, and resolves dice merges through `GridMergeResolver`. Merge behaviour is tunable: `MinMergeGroupSize`, `MergeStep`, `MaxContentId` (0 = unlimited), and `RequireWalkable`.

Dice rules in this module default to: place dice, merge 3+ side-adjacent equal values, result value is `old + step`, cascade from the result cell. The service applies cell occupancy and raises a single consistent `OnCellStateChanged` per affected cell, and `OnBoardChanged` once per placement. Score, spawn pool progression, win/loss, and UI belong to the consuming game or sample.

## Sample

Playable sample scene:

`Assets/Neoxider/Samples/Demo/Scenes/GridSystem/GridSystemDiceMergeDemo.unity`

The sample uses sprites from `Assets/Neoxider/Sprites/Dice`, drag/drop input, pair rotation in the tray, score, pool progression, and game-over logic.
