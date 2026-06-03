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

            foreach (DicePieceCell pieceCell in piece.Cells)
            {
                FieldCell cell = Generator.GetCell(anchor + pieceCell.Offset);
                if (!CanUsePlacementCell(cell))
                {
                    return false;
                }
            }

            return true;
        }

        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            var result = new DicePlacementResult();
            if (!CanPlace(piece, anchor))
            {
                return result;
            }

            var seeds = new List<Vector3Int>();
            foreach (DicePieceCell pieceCell in piece.Cells)
            {
                Vector3Int position = anchor + pieceCell.Offset;
                FieldCell cell = Generator.GetCell(position);
                cell.ContentId = pieceCell.Value;
                cell.IsOccupied = true;
                Generator.OnCellStateChanged.Invoke(cell);
                result.PlacedPositions.Add(position);
                seeds.Add(position);
            }

            result.Placed = true;
            if (resolveMerges)
            {
                result.MergeResult = ResolveMerges(seeds);
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

        private bool CanUsePlacementCell(FieldCell cell)
        {
            return cell != null &&
                   cell.IsEnabled &&
                   cell.IsWalkable &&
                   !cell.IsOccupied;
        }
    }
}
