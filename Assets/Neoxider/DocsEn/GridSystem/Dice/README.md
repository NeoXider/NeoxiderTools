# GridSystem Dice

`Neo.GridSystem.Dice` is a reusable dice-placement layer built on `FieldGenerator` and the generic `Neo.Merge` engine.

## Runtime API

- `DicePiece` describes a single die or a two-cell pair with local offsets and rotation helpers.
- `DicePieceGenerator` creates single/pair pieces from a value pool, with equal single/pair odds and no duplicate values inside a pair.
- `DiceBoardService` checks placement, writes values into `FieldCell.ContentId`, and resolves dice merges through `GridMergeResolver`.

Dice rules in this module are intentionally narrow: place dice, merge 3+ side-adjacent equal values, result value is `old + 1`, cascade from the result cell. Score, spawn pool progression, win/loss, and UI belong to the consuming game or sample.

## Sample

Playable sample scene:

`Assets/Neoxider/Samples/Demo/Scenes/GridSystem/GridSystemDiceMergeDemo.unity`

The sample uses sprites from `Assets/Neoxider/Sprites/Dice`, drag/drop input, pair rotation in the tray, score, pool progression, and game-over logic.
