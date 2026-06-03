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
        [SerializeField] private int _minMergeGroupSize = 3;

        public UnityEvent OnBoardChanged = new();
        public UnityEvent<GridMergeResult> OnMergesResolved = new();

        private FieldGenerator _generator;

        public int EmptyContentId
        {
            get => _emptyContentId;
            set => _emptyContentId = value;
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

            return Generator.CanPlaceContentFootprint(anchor, CreatePlacementEntries(piece));
        }

        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            var result = new DicePlacementResult();
            if (piece == null || Generator == null)
            {
                return result;
            }

            GridPlacementResult placement = Generator.PlaceContentFootprint(anchor, CreatePlacementEntries(piece));
            result.Placed = placement.Placed;
            result.PlacedPositions.AddRange(placement.Positions);

            if (!placement.Placed)
            {
                return result;
            }

            if (resolveMerges)
            {
                result.MergeResult = ResolveMerges(placement.Positions);
            }

            OnBoardChanged.Invoke();
            return result;
        }

        public GridMergeResult ResolveMerges(IEnumerable<Vector3Int> seeds)
        {
            GridMergeResult result = GridMergeResolver.Resolve(Generator, new GridMergeRequest
            {
                Seeds = seeds,
                EmptyContentId = _emptyContentId,
                MinGroupSize = _minMergeGroupSize,
                RequireEnabled = true,
                RequireWalkable = true,
                IgnoreOccupied = true,
                Mutate = true,
                CascadeMode = Neo.Merge.MergeCascadeMode.FromResult,
                IsEmptyContent = value => value == _emptyContentId,
                GetMergedContent = (value, count) => value + 1,
                SelectResultCell = (group, seed) => seed
            });

            foreach (FieldCell changed in result.ChangedCells)
            {
                changed.IsOccupied = changed.ContentId != _emptyContentId;
                Generator.OnCellStateChanged.Invoke(changed);
            }

            if (result.HasChanges)
            {
                OnMergesResolved.Invoke(result);
                OnBoardChanged.Invoke();
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
            foreach (DicePieceCell pieceCell in piece.Cells)
            {
                entries.Add(new GridPlacementEntry(pieceCell.Offset, pieceCell.Value));
            }

            return entries;
        }
    }
}
