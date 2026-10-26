# GridSystem Dice

`Neo.GridSystem.Dice` is a reusable dice-placement layer built on `FieldGenerator` and the generic `Neo.Merge` engine.

## Runtime API

- `DicePiece` describes a single die, a pair, or a larger footprint via local offsets. Rotation returns a new piece and rotates every cell around the anchor in XY, including an off-anchor single cell; Z offsets and dice values are preserved.
- `DicePieceGenerator` creates single/pair pieces from a value pool, with equal single/pair odds when two distinct values are available. Pair values differ even when the input repeats values; a pool with only one distinct value produces a single. Repeated entries retain their selection frequency, and the second draw excludes every occurrence of the first value. Use `GenerateWeighted` for explicit positive integer weights.
- `CreateDefaultPool()` returns the original 1-5 merge pool, `CreateD6Pool()` returns classic 1-6 faces, and `CreateSequentialPool(minValue, maxValue)` creates an inclusive custom pool. Ranges ending at `int.MaxValue` are supported. Reversed ranges throw `ArgumentException`; counts exceeding `int.MaxValue` throw `ArgumentOutOfRangeException` before allocation. Large representable pools still require enough memory.
- `DiceBoard` is the plain C# placement/merge core. It requires a live, generated `FieldGenerator`; it does not require a `DiceBoardService` component. It writes `FieldCell.ContentId` and occupancy and delegates merges to `GridMergeResolver`.
- `DiceBoardService` exposes that core as `Board`, copies Inspector settings on first use, and forwards C# events to UnityEvents. Merge behaviour is tunable through `MinMergeGroupSize`, `MergeStep`, `MaxContentId`, and `RequireWalkable`. A positive `MaxContentId` caps merged values; without an explicit cap, addition saturates at `int.MaxValue`.

With an already generated `FieldGenerator field`:

```csharp
DiceBoard board = new(field) { MaxContentId = 6 };
DicePieceGenerator pieces = new();
DicePiece piece = pieces.Generate(DicePieceGenerator.CreateD6Pool(), true);
DicePlacementResult result = board.Place(piece, Vector3Int.zero);
```

Use `Neo.GridSystem`, `Neo.GridSystem.Dice`, and `UnityEngine` namespaces; consumers with an asmdef need references to `Neo.GridSystem` and `Neo.GridSystem.Dice`.

Dice rules default to merging 3+ connected equal values along the field's movement directions (side-adjacent with `FourDirections2D`), incrementing the result, and cascading from the result cell. Merge notifications carry matching content and occupancy. `BoardChanged` / `OnBoardChanged` fires once per successful placement, including its merges. `ClearBoard()` clears enabled cells and preserves disabled cells. Score, spawn pool progression, win/loss, and UI belong to the consuming game or sample.

## Scene workflow

Add `DiceBoardService` beside `FieldGenerator`, configure the field and dice rules, and generate the field before placement. Call `ClearBoard()` to initialize enabled cells to the chosen `EmptyContentId` when needed. Use public service setters for rule changes after initialization. Connect `OnBoardChanged` and `OnMergesResolved` to views or other scene actions; placement uses the typed `DicePiece`/anchor API. See [DiceBoardService](./DiceBoardService.md) for settings, weighted pools, and UnityEvent wiring.

## Sample

Playable sample scene:

`<Demo sample>/Scenes/GridSystem/GridSystemDiceMergeDemo.unity`

The sample ships under `Assets/Neoxider/Samples~/Demo` in release layout (`Assets/Neoxider/Samples/Demo` during sample development). Import Demo Scenes through Package Manager to use it at `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/...`.

The sample uses sprites from `Assets/Neoxider/Sprites/Dice`, drag/drop input, pair rotation in the tray, score, pool progression, and game-over logic.

## Related

- [GridSystem module](../README.md)
- [DiceBoard and DiceBoardService contracts](./DiceBoardService.md)
- [FieldGenerator](../FieldGenerator.md)
- [Generic merge engine](../../Merge/README.md)
