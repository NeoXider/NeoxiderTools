using System.Collections.Generic;
using Neo.GridSystem.Merge;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Scene wrapper for placing dice pieces on a FieldGenerator and resolving dice merges.
    /// </summary>
    [NeoDoc("GridSystem/Dice/DiceBoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [AddComponentMenu("Neoxider/GridSystem/Dice/DiceBoardService")]
    public class DiceBoardService : MonoBehaviour
    {
        [SerializeField] private int _emptyContentId = -1;
        [SerializeField, Min(1)] private int _minMergeGroupSize = 3;
        [SerializeField, Min(1)] private int _mergeStep = 1;

        [Tooltip("Optional upper bound for merged content (0 = unlimited). Useful to cap to the available visuals.")]
        [SerializeField] private int _maxContentId;

        [Tooltip("Restrict placement and merges to walkable cells. Disable for pure puzzle boards where the " +
                 "pathfinding 'walkable' flag is irrelevant.")]
        [SerializeField] private bool _requireWalkable = true;

        public UnityEvent OnBoardChanged = new();
        public UnityEvent<GridMergeResult> OnMergesResolved = new();

        private FieldGenerator _generator;

        public int EmptyContentId
        {
            get => _emptyContentId;
            set => _emptyContentId = value;
        }

        public int MinMergeGroupSize
        {
            get => _minMergeGroupSize;
            set => _minMergeGroupSize = Mathf.Max(1, value);
        }

        public int MergeStep
        {
            get => _mergeStep;
            set => _mergeStep = Mathf.Max(1, value);
        }

        /// <summary>Upper bound for merged content. 0 means no cap.</summary>
        public int MaxContentId
        {
            get => _maxContentId;
            set => _maxContentId = value;
        }

        public bool RequireWalkable
        {
            get => _requireWalkable;
            set => _requireWalkable = value;
        }

        private FieldGenerator Generator
        {
            get
            {
                if (_generator == null)
                {
                    _generator = GetComponent<FieldGenerator>();
                }

                return _generator;
            }
        }

        private void Awake()
        {
            _generator = Generator;
        }

        public bool CanPlace(DicePiece piece, Vector3Int anchor)
        {
            if (piece == null || Generator == null)
            {
                return false;
            }

            List<GridPlacementEntry> entries = CreatePlacementEntries(piece);
            return entries.Count > 0 &&
                   Generator.CanPlaceContentFootprint(anchor, entries, requireWalkable: _requireWalkable);
        }

        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            var result = new DicePlacementResult();
            if (piece == null || Generator == null)
            {
                return result;
            }

            List<GridPlacementEntry> entries = CreatePlacementEntries(piece);
            if (entries.Count == 0)
            {
                return result;
            }

            GridPlacementResult placement = Generator.PlaceContentFootprint(
                anchor, entries, requireWalkable: _requireWalkable);
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
                    OnMergesResolved.Invoke(result.MergeResult);
                }
            }

            // Placement and any follow-up merges are a single logical change: raise OnBoardChanged exactly once.
            OnBoardChanged.Invoke();
            return result;
        }

        public GridMergeResult ResolveMerges(IEnumerable<Vector3Int> seeds)
        {
            GridMergeResult result = ResolveMergesInternal(seeds);
            if (result.HasChanges)
            {
                OnMergesResolved.Invoke(result);
                OnBoardChanged.Invoke();
            }

            return result;
        }

        private GridMergeResult ResolveMergesInternal(IEnumerable<Vector3Int> seeds)
        {
            GridMergeRequest request = GridMergeRequest.Increment(
                seeds, _emptyContentId, _minMergeGroupSize, _mergeStep, _requireWalkable);

            // Let this service apply occupancy and raise a single, fully-consistent notification per cell instead of
            // the resolver notifying mid-mutation while IsOccupied is still stale.
            request.NotifyOnContentChanged = false;
            if (_maxContentId > 0)
            {
                int cap = _maxContentId;
                int step = _mergeStep;
                request.GetMergedContent = (value, count) => Mathf.Min(value + step, cap);
            }

            GridMergeResult result = GridMergeResolver.Resolve(Generator, request);
            foreach (FieldCell changed in result.ChangedCells)
            {
                changed.IsOccupied = changed.ContentId != _emptyContentId;
                Generator.OnCellStateChanged.Invoke(changed);
            }

            return result;
        }

        public void ClearBoard()
        {
            if (Generator == null)
            {
                return;
            }

            foreach (FieldCell cell in Generator.GetAllCells(false))
            {
                cell.ContentId = _emptyContentId;
                cell.IsOccupied = false;
                Generator.OnCellStateChanged.Invoke(cell);
            }

            OnBoardChanged.Invoke();
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
