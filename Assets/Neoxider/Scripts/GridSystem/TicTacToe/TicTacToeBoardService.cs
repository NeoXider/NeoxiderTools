using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.TicTacToe
{
    /// <summary>
    ///     Runtime service that controls turns, moves, win checks and reset flow for TicTacToe.
    /// </summary>
    [NeoDoc("GridSystem/TicTacToe/TicTacToeBoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/TicTacToe/TicTacToeBoardService")]
    [AddComponentMenu("Neoxider/GridSystem/TicTacToe/TicTacToeBoardService")]
    public class TicTacToeBoardService : MonoBehaviour
    {
        [SerializeField] private bool _resetOnStart = true;

        /// <summary>
        ///     Invoked when active player changes. Argument is <see cref="TicTacToeCellState" /> as int.
        /// </summary>
        public UnityEvent<int> OnPlayerChanged = new();

        /// <summary>
        ///     Invoked when a winner is detected. Argument is winner state as int.
        /// </summary>
        public UnityEvent<int> OnWinnerDetected = new();

        /// <summary>
        ///     Invoked when the board ends with a draw.
        /// </summary>
        public UnityEvent OnDrawDetected = new();

        /// <summary>
        ///     Invoked after board reset is complete.
        /// </summary>
        public UnityEvent OnBoardReset = new();

        private FieldGenerator _generator;

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

        /// <summary>
        ///     Current player that is allowed to make the next move.
        /// </summary>
        public TicTacToeCellState CurrentPlayer { get; private set; } = TicTacToeCellState.PlayerX;

        /// <summary>
        ///     Winner state. Remains <see cref="TicTacToeCellState.Empty" /> until someone wins.
        /// </summary>
        public TicTacToeCellState Winner { get; private set; } = TicTacToeCellState.Empty;

        /// <summary>
        ///     True when game is finished (win or draw).
        /// </summary>
        public bool IsFinished { get; private set; }

        private void Awake()
        {
            _generator = GetComponent<FieldGenerator>();
        }

        private void Start()
        {
            if (_resetOnStart)
            {
                ResetBoard();
            }
        }

        /// <summary>
        ///     Clears board state and starts a new round from player X.
        /// </summary>
        public void ResetBoard()
        {
            if (Generator == null)
            {
                return;
            }

            foreach (FieldCell cell in Generator.GetAllCells())
            {
                if (!cell.IsEnabled || !cell.IsWalkable || cell.IsOccupied)
                {
                    continue;
                }

                cell.ContentId = (int)TicTacToeCellState.Empty;
            }

            CurrentPlayer = TicTacToeCellState.PlayerX;
            Winner = TicTacToeCellState.Empty;
            IsFinished = false;

            OnBoardReset.Invoke();
            OnPlayerChanged.Invoke((int)CurrentPlayer);
        }

        /// <summary>
        ///     Attempts to place a mark at a 2D grid position.
        /// </summary>
        /// <param name="pos">Target position in board coordinates.</param>
        /// <returns>True if move is accepted; otherwise false.</returns>
        public bool TryMakeMove(Vector2Int pos)
        {
            return TryMakeMove(new Vector3Int(pos.x, pos.y, 0));
        }

        /// <summary>
        ///     Attempts to place a mark at a 3D grid position.
        /// </summary>
        /// <param name="pos">Target position in board coordinates.</param>
        /// <returns>True if move is accepted; otherwise false.</returns>
        public bool TryMakeMove(Vector3Int pos)
        {
            if (IsFinished || Generator == null)
            {
                return false;
            }

            FieldCell cell = Generator.GetCell(pos);
            if (cell == null || !cell.IsEnabled || !cell.IsWalkable || cell.IsOccupied)
            {
                return false;
            }

            if ((TicTacToeCellState)cell.ContentId != TicTacToeCellState.Empty)
            {
                return false;
            }

            cell.ContentId = (int)CurrentPlayer;

            Winner = TicTacToeWinChecker.GetWinner(Generator);
            if (Winner != TicTacToeCellState.Empty)
            {
                IsFinished = true;
                OnWinnerDetected.Invoke((int)Winner);
                return true;
            }

            if (IsBoardFull())
            {
                IsFinished = true;
                OnDrawDetected.Invoke();
                return true;
            }

            CurrentPlayer = CurrentPlayer == TicTacToeCellState.PlayerX
                ? TicTacToeCellState.PlayerO
                : TicTacToeCellState.PlayerX;

            OnPlayerChanged.Invoke((int)CurrentPlayer);
            return true;
        }

        /// <summary>
        ///     Checks whether every enabled, walkable, unoccupied cell already contains a move.
        /// </summary>
        /// <returns>True when no empty playable cells remain.</returns>
        public bool IsBoardFull()
        {
            if (Generator == null)
            {
                return false;
            }

            foreach (FieldCell cell in Generator.GetAllCells(false))
            {
                if (cell.IsWalkable && !cell.IsOccupied &&
                    (TicTacToeCellState)cell.ContentId == TicTacToeCellState.Empty)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
