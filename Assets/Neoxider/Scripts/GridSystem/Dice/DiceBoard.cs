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
        private int _minMergeGroupSize = 3;
        private int _mergeStep = 1;

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

        /// <summary>Optional upper bound for merged content (0 = unlimited).</summary>
        public int MaxContentId { get; set; }

        /// <summary>Restrict placement and merges to walkable cells.</summary>
        public bool RequireWalkable { get; set; } = true;

        /// <summary>Raised once per logical board change (placement incl. merges, merge pass, clear).</summary>
        public event Action BoardChanged;

        /// <summary>Raised when a merge pass changed the board.</summary>
        public event Action<GridMergeResult> MergesResolved;

        /// <summary>True when the whole piece footprint fits at the anchor.</summary>
        public bool CanPlace(DicePiece piece, Vector3Int anchor)
        {
            List<GridPlacementEntry> entries = CreatePlacementEntries(piece);
            return entries.Count > 0 &&
                   _generator.CanPlaceContentFootprint(anchor, entries, requireWalkable: RequireWalkable);
        }

        /// <summary>
        ///     Places the piece and (optionally) resolves follow-up merges. One
        ///     <see cref="BoardChanged"/> per successful call.
        /// </summary>
        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            var result = new DicePlacementResult();
            List<GridPlacementEntry> entries = CreatePlacementEntries(piece);
            if (entries.Count == 0)
            {
                return result;
            }

            GridPlacementResult placement = _generator.PlaceContentFootprint(
                anchor, entries, requireWalkable: RequireWalkable);
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

        /// <summary>Empties every enabled cell and notifies once.</summary>
        public void ClearBoard()
        {
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                cell.ContentId = EmptyContentId;
                cell.IsOccupied = false;
                _generator.OnCellStateChanged.Invoke(cell);
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
            if (MaxContentId > 0)
            {
                int cap = MaxContentId;
                int step = _mergeStep;
                request.GetMergedContent = (value, count) => Mathf.Min(value + step, cap);
            }

            GridMergeResult result = GridMergeResolver.Resolve(_generator, request);
            foreach (FieldCell changed in result.ChangedCells)
            {
                changed.IsOccupied = changed.ContentId != EmptyContentId;
                _generator.OnCellStateChanged.Invoke(changed);
            }

            return result;
        }

        private static List<GridPlacementEntry> CreatePlacementEntries(DicePiece piece)
        {
            var entries = new List<GridPlacementEntry>();
            if (piece == null)
            {
                return entries;
            }

            foreach (DicePieceCell pieceCell in piece.Cells)
            {
                entries.Add(new GridPlacementEntry(pieceCell.Offset, pieceCell.Value));
            }

            return entries;
        }
    }
}
