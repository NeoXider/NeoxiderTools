using System;
using System.Collections.Generic;
using Neo.GridSystem.Merge;
using UnityEngine;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Plain C# dice-board core: places <see cref="DicePiece"/> footprints on a
    ///     <see cref="FieldGenerator"/> and resolves dice merges. No MonoBehaviour — construct it
    ///     anywhere (tests, server logic, custom update loops) over a generated field;
    ///     <see cref="DiceBoardService"/> is the scene wrapper that forwards its Inspector settings and
    ///     UnityEvents to this class. C# events <see cref="BoardChanged"/> /
    ///     <see cref="MergesResolved"/> mirror the wrapper's UnityEvents.
    /// </summary>
    public sealed class DiceBoard
    {
        private readonly FieldGenerator _generator;

        // WHY: validation-only queries (CanPlace, placement search) run per frame for previews and once per cell for
        // game-over checks; reusing entry objects keeps them allocation-free. FieldGenerator copies entry values
        // during validation, so a re-entrant call cannot corrupt an in-flight placement.
        private readonly List<GridPlacementEntry> _entryScratch = new();
        private readonly List<GridPlacementEntry> _entryPool = new();
        private int _minMergeGroupSize = 3;
        private int _mergeStep = 1;

        /// <summary>Creates a dice board over a generated field.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="generator" /> is null.</exception>
        public DiceBoard(FieldGenerator generator)
        {
            _generator = generator != null ? generator : throw new ArgumentNullException(nameof(generator));
        }

        /// <summary>The field this board plays on.</summary>
        public FieldGenerator Generator => _generator;

        /// <summary>Content id that marks an empty cell.</summary>
        public int EmptyContentId { get; set; } = -1;

        /// <summary>Minimum connected group size that merges (at least 1).</summary>
        public int MinMergeGroupSize
        {
            get => _minMergeGroupSize;
            set => _minMergeGroupSize = Mathf.Max(1, value);
        }

        /// <summary>Content increment applied by a merge (at least 1).</summary>
        public int MergeStep
        {
            get => _mergeStep;
            set => _mergeStep = Mathf.Max(1, value);
        }

        /// <summary>Optional upper bound for merged content (0 = no explicit cap; Int32.MaxValue still applies).</summary>
        public int MaxContentId { get; set; }

        /// <summary>Restrict placement and merges to walkable cells.</summary>
        public bool RequireWalkable { get; set; } = true;

        /// <summary>Raised once per logical board change (placement incl. merges, merge pass, clear).</summary>
        public event Action BoardChanged;

        /// <summary>Raised when a merge pass changed the board.</summary>
        public event Action<GridMergeResult> MergesResolved;

        /// <summary>True when the whole piece footprint fits at the anchor. Allocation-free.</summary>
        public bool CanPlace(DicePiece piece, Vector3Int anchor)
        {
            return FillEntryScratch(piece, 0) &&
                   _generator.CanPlaceContentFootprint(anchor, _entryScratch, requireWalkable: RequireWalkable);
        }

        /// <summary>
        ///     True when the piece fits somewhere on the board, optionally in any of its four rotations. Use it as the
        ///     "no move left" check after spawning the next piece. Allocation-free.
        /// </summary>
        public bool CanPlaceAnywhere(DicePiece piece, bool allowRotations = true)
        {
            return TryFindPlacement(piece, allowRotations, out _, out _);
        }

        /// <summary>
        ///     Finds the first anchor (and rotation) where the piece fits, scanning rotations first and then cells in
        ///     field order. Allocation-free.
        /// </summary>
        /// <param name="piece">Piece to fit.</param>
        /// <param name="allowRotations">Also try the piece rotated 90, 180 and 270 degrees clockwise.</param>
        /// <param name="anchor">Anchor to pass to <see cref="Place" /> with the rotated piece.</param>
        /// <param name="clockwiseTurns">
        ///     How many times to call <see cref="DicePiece.RotateClockwise" /> on <paramref name="piece" /> to get the
        ///     orientation that fits (0-3).
        /// </param>
        /// <returns>True when a placement exists.</returns>
        public bool TryFindPlacement(
            DicePiece piece,
            bool allowRotations,
            out Vector3Int anchor,
            out int clockwiseTurns)
        {
            anchor = default;
            clockwiseTurns = 0;
            FieldCell[,,] cells = _generator.Cells;
            if (piece == null || piece.CellCount == 0 || cells == null)
            {
                return false;
            }

            int turnCount = allowRotations && piece.CellCount > 1 ? 4 : 1;
            for (int turns = 0; turns < turnCount; turns++)
            {
                FillEntryScratch(piece, turns);
                // WHY: every valid placement puts the first piece cell on a generated cell, so deriving anchors from
                // cells covers all footprints, including ones whose offsets do not contain the origin.
                Vector3Int firstOffset = _entryScratch[0].Offset;
                foreach (FieldCell cell in cells)
                {
                    if (cell == null || !cell.IsEnabled)
                    {
                        continue;
                    }

                    Vector3Int candidate = cell.Position - firstOffset;
                    if (_generator.CanPlaceContentFootprint(candidate, _entryScratch,
                            requireWalkable: RequireWalkable))
                    {
                        anchor = candidate;
                        clockwiseTurns = turns;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Places the piece and (optionally) resolves follow-up merges. One
        ///     <see cref="BoardChanged"/> per successful call.
        /// </summary>
        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            DicePlacementResult result = new();
            if (!FillEntryScratch(piece, 0))
            {
                return result;
            }

            GridPlacementResult placement = _generator.PlaceContentFootprint(
                anchor, _entryScratch, requireWalkable: RequireWalkable);
            result.Placed = placement.Placed;
            result.PlacedPositions.AddRange(placement.Positions);

            if (!placement.Placed)
            {
                return result;
            }

            if (resolveMerges)
            {
                result.MergeResult = ResolveMergesInternal(placement.Positions);
                if (result.MergeResult.HasChanges)
                {
                    MergesResolved?.Invoke(result.MergeResult);
                }
            }

            // WHY: placement and any follow-up merges are a single logical change: notify exactly once.
            BoardChanged?.Invoke();
            return result;
        }

        /// <summary>Runs a merge pass from the given seed positions.</summary>
        public GridMergeResult ResolveMerges(IEnumerable<Vector3Int> seeds)
        {
            GridMergeResult result = ResolveMergesInternal(seeds);
            if (result.HasChanges)
            {
                MergesResolved?.Invoke(result);
                BoardChanged?.Invoke();
            }

            return result;
        }

        /// <summary>
        ///     Empties every enabled cell, then raises one cell notification per cleared cell and one
        ///     <see cref="BoardChanged" />. Listeners observe the fully cleared board.
        /// </summary>
        public void ClearBoard()
        {
            FieldCell[,,] cells = _generator.Cells;
            if (cells != null)
            {
                foreach (FieldCell cell in cells)
                {
                    if (cell != null && cell.IsEnabled)
                    {
                        cell.ContentId = EmptyContentId;
                        cell.IsOccupied = false;
                    }
                }

                foreach (FieldCell cell in cells)
                {
                    if (cell != null && cell.IsEnabled)
                    {
                        _generator.OnCellStateChanged?.Invoke(cell);
                    }
                }
            }

            BoardChanged?.Invoke();
        }

        private GridMergeResult ResolveMergesInternal(IEnumerable<Vector3Int> seeds)
        {
            GridMergeRequest request = GridMergeRequest.Increment(
                seeds, EmptyContentId, _minMergeGroupSize, _mergeStep, RequireWalkable);

            // WHY: apply occupancy here and raise one fully-consistent notification per cell instead of
            // the resolver notifying mid-mutation while IsOccupied is still stale.
            request.NotifyOnContentChanged = false;
            // WHY: widen before adding so overflow cannot bypass the configured cap or wrap an uncapped value.
            int cap = MaxContentId > 0 ? MaxContentId : int.MaxValue;
            int step = _mergeStep;
            request.GetMergedContent = (value, count) => (int)Math.Min((long)value + step, cap);

            GridMergeResult result = GridMergeResolver.Resolve(_generator, request);
            foreach (FieldCell changed in result.ChangedCells)
            {
                changed.IsOccupied = changed.ContentId != EmptyContentId;
                _generator.OnCellStateChanged.Invoke(changed);
            }

            return result;
        }

        private bool FillEntryScratch(DicePiece piece, int clockwiseTurns)
        {
            _entryScratch.Clear();
            if (piece == null)
            {
                return false;
            }

            IReadOnlyList<DicePieceCell> pieceCells = piece.Cells;
            for (int i = 0; i < pieceCells.Count; i++)
            {
                if (i == _entryPool.Count)
                {
                    _entryPool.Add(new GridPlacementEntry(Vector3Int.zero, 0));
                }

                GridPlacementEntry entry = _entryPool[i];
                Vector3Int offset = pieceCells[i].Offset;
                for (int turn = 0; turn < clockwiseTurns; turn++)
                {
                    offset = new Vector3Int(offset.y, -offset.x, offset.z);
                }

                entry.Offset = offset;
                entry.ContentId = pieceCells[i].Value;
                entry.OccupiesCell = true;
                _entryScratch.Add(entry);
            }

            return _entryScratch.Count > 0;
        }
    }
}
