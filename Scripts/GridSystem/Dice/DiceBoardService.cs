using System.Collections.Generic;
using Neo.GridSystem.Merge;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.Dice
{
    /// <summary>
    ///     Scene wrapper for placing dice pieces on a FieldGenerator and resolving dice merges.
    ///     All logic lives in the plain C# <see cref="DiceBoard"/> core (usable without this wrapper);
    ///     this component forwards its Inspector settings and re-raises the core's events as
    ///     UnityEvents. The scene-facing API is unchanged.
    /// </summary>
    [NeoDoc("GridSystem/Dice/DiceBoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/Dice/DiceBoardService")]
    [AddComponentMenu("Neoxider/GridSystem/Dice/DiceBoardService")]
    public class DiceBoardService : MonoBehaviour
    {
        [Tooltip("Content id that marks an empty cell. Matches FieldCell's default (-1).")]
        [SerializeField] private int _emptyContentId = -1;

        [Tooltip("Minimum number of connected equal dice that merge.")]
        [SerializeField, Min(1)] private int _minMergeGroupSize = 3;

        [Tooltip("Amount added to the merged die value.")]
        [SerializeField, Min(1)] private int _mergeStep = 1;

        [Tooltip("Optional upper bound for merged content (0 = unlimited). Useful to cap to the available visuals.")]
        [SerializeField] private int _maxContentId;

        [Tooltip("Restrict placement and merges to walkable cells. Disable for pure puzzle boards where the " +
                 "pathfinding 'walkable' flag is irrelevant.")]
        [SerializeField] private bool _requireWalkable = true;

        /// <summary>Raised once per logical board change (placement incl. merges, merge pass, clear).</summary>
        public UnityEvent OnBoardChanged = new();

        /// <summary>Raised when a merge pass changed the board.</summary>
        public UnityEvent<GridMergeResult> OnMergesResolved = new();

        private DiceBoard _board;

        /// <summary>Content id that marks an empty cell.</summary>
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

        /// <summary>Minimum connected group size that merges (at least 1).</summary>
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

        /// <summary>Content increment applied by a merge (at least 1).</summary>
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

        /// <summary>Upper bound for merged content. 0 means no explicit cap; Int32.MaxValue still applies.</summary>
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

        /// <summary>Restrict placement and merges to walkable cells.</summary>
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

        // WHY: Inspector edits in Play Mode change only the serialized fields; push them into the live core so the
        // running board uses what the designer sees.
        private void OnValidate()
        {
            _minMergeGroupSize = Mathf.Max(1, _minMergeGroupSize);
            _mergeStep = Mathf.Max(1, _mergeStep);
            if (_board == null)
            {
                return;
            }

            _board.EmptyContentId = _emptyContentId;
            _board.MinMergeGroupSize = _minMergeGroupSize;
            _board.MergeStep = _mergeStep;
            _board.MaxContentId = _maxContentId;
            _board.RequireWalkable = _requireWalkable;
        }

        /// <summary>True when the whole piece footprint fits at the anchor.</summary>
        public bool CanPlace(DicePiece piece, Vector3Int anchor)
        {
            return Board != null && Board.CanPlace(piece, anchor);
        }

        /// <summary>True when the piece fits somewhere on the board, optionally in any rotation.</summary>
        public bool CanPlaceAnywhere(DicePiece piece, bool allowRotations = true)
        {
            return Board != null && Board.CanPlaceAnywhere(piece, allowRotations);
        }

        /// <summary>
        ///     Finds the first anchor and clockwise rotation count where the piece fits. See
        ///     <see cref="DiceBoard.TryFindPlacement" />.
        /// </summary>
        public bool TryFindPlacement(
            DicePiece piece,
            bool allowRotations,
            out Vector3Int anchor,
            out int clockwiseTurns)
        {
            anchor = default;
            clockwiseTurns = 0;
            return Board != null && Board.TryFindPlacement(piece, allowRotations, out anchor, out clockwiseTurns);
        }

        /// <summary>Places the piece and (optionally) resolves follow-up merges; see <see cref="DiceBoard.Place" />.</summary>
        public DicePlacementResult Place(DicePiece piece, Vector3Int anchor, bool resolveMerges = true)
        {
            return Board != null ? Board.Place(piece, anchor, resolveMerges) : new DicePlacementResult();
        }

        /// <summary>Runs a merge pass from the given seed positions.</summary>
        public GridMergeResult ResolveMerges(IEnumerable<Vector3Int> seeds)
        {
            return Board != null ? Board.ResolveMerges(seeds) : new GridMergeResult();
        }

        /// <summary>
        ///     Empties every enabled cell, then raises one cell notification per enabled cell and one
        ///     <see cref="OnBoardChanged" />.
        /// </summary>
        public void ClearBoard()
        {
            Board?.ClearBoard();
        }
    }
}
