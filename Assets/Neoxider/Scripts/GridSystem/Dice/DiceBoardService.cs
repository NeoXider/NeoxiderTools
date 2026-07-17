using System.Collections.Generic;
using Neo.GridSystem.Merge;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Scene wrapper for placing dice pieces on a FieldGenerator and resolving dice merges.
    ///     All logic lives in the plain C# <see cref="DiceBoard"/> core (usable without a scene);
    ///     this component forwards its Inspector settings and re-raises the core's events as
    ///     UnityEvents. The scene-facing API is unchanged.
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

        private DiceBoard _board;

        public int EmptyContentId
        {
            get => _emptyContentId;
            set
            {
                _emptyContentId = value;
                if (_board != null)
                {
                    _board.EmptyContentId = value;
                }
            }
        }

        public int MinMergeGroupSize
        {
            get => _minMergeGroupSize;
            set
            {
                _minMergeGroupSize = Mathf.Max(1, value);
                if (_board != null)
                {
                    _board.MinMergeGroupSize = value;
                }
            }
        }

        public int MergeStep
        {
            get => _mergeStep;
            set
            {
                _mergeStep = Mathf.Max(1, value);
                if (_board != null)
                {
                    _board.MergeStep = value;
                }
            }
        }

        /// <summary>Upper bound for merged content. 0 means no cap.</summary>
        public int MaxContentId
        {
            get => _maxContentId;
            set
            {
                _maxContentId = value;
                if (_board != null)
                {
                    _board.MaxContentId = value;
                }
            }
        }

        public bool RequireWalkable
        {
            get => _requireWalkable;
            set
            {
                _requireWalkable = value;
                if (_board != null)
                {
                    _board.RequireWalkable = value;
                }
            }
        }

        /// <summary>
        ///     The plain C# core behind this component (created on first use). Prefer it for code-heavy
        ///     flows; its C# events fire alongside this component's UnityEvents.
        /// </summary>
        public DiceBoard Board
        {
            get
            {
                if (_board == null)
                {
                    FieldGenerator generator = GetComponent<FieldGenerator>();
                    if (generator == null)
                    {
                        return null;
                    }

                    _board = new DiceBoard(generator)
                    {
                        EmptyContentId = _emptyContentId,
                        MinMergeGroupSize = _minMergeGroupSize,
                        MergeStep = _mergeStep,
                        MaxContentId = _maxContentId,
                        RequireWalkable = _requireWalkable
                    };
                    _board.BoardChanged += () => OnBoardChanged.Invoke();
                    _board.MergesResolved += result => OnMergesResolved.Invoke(result);
                }

                return _board;
            }
        }

        private void Awake()
        {
            _ = Board;
        }

        public bool CanPlace(DicePiece piece, Vector3Int anchor)
        {
            return Board != null && Board.CanPlace(piece, anchor);
        }

        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            return Board != null ? Board.Place(piece, anchor, resolveMerges) : new DicePlacementResult();
        }

        public GridMergeResult ResolveMerges(IEnumerable<Vector3Int> seeds)
        {
            return Board != null ? Board.ResolveMerges(seeds) : new GridMergeResult();
        }

        public void ClearBoard()
        {
            Board?.ClearBoard();
        }
    }
}
