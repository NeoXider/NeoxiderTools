using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.TicTacToe
{
    /// <summary>
    ///     Runtime service that controls turns, moves, win checks and reset flow for TicTacToe and n-in-a-row games.
    /// </summary>
    [NeoDoc("GridSystem/TicTacToe/TicTacToeBoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/TicTacToe/TicTacToeBoardService")]
    [AddComponentMenu("Neoxider/GridSystem/TicTacToe/TicTacToeBoardService")]
    public class TicTacToeBoardService : MonoBehaviour
    {
        [Tooltip("Call ResetBoard in Start.")]
        [SerializeField] private bool _resetOnStart = true;

        [Tooltip("Marks in a row needed to win. 0 = automatic: 3, or the smallest board dimension when every " +
                 "dimension exceeds 3. Set 5 for Gomoku-style boards.")]
        [SerializeField, Min(0)] private int _winLineLength;

        private readonly List<Vector3Int> _winningLine = new();

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

        // WHY: bumped by every committed move and reset, so a call can tell that a listener replaced the round
        // state while it was notifying and must not publish its own, now stale, follow-up events.
        private int _stateVersion;

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

        /// <summary>
        ///     Marks in a row needed to win; 0 uses the automatic rule of
        ///     <see cref="TicTacToeWinChecker.ResolveWinLineLength" />. Takes effect on the next move.
        /// </summary>
        public int WinLineLength
        {
            get => _winLineLength;
            set => _winLineLength = Mathf.Max(0, value);
        }

        /// <summary>
        ///     Cells of the winning run after a win, from one end to the other; empty otherwise. Use it to highlight
        ///     the line in a view.
        /// </summary>
        public IReadOnlyList<Vector3Int> WinningLine => _winningLine;

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
        ///     Clears board state and starts a new round from player X. Raises
        ///     <see cref="FieldGenerator.OnCellStateChanged" /> for every cleared mark.
        /// </summary>
        public void ResetBoard()
        {
            FieldGenerator generator = Generator;
            if (generator == null)
            {
                return;
            }

            List<FieldCell> cleared = null;
            foreach (FieldCell cell in generator.GetAllCells())
            {
                if (!cell.IsEnabled || !cell.IsWalkable || cell.IsOccupied ||
                    cell.ContentId == (int)TicTacToeCellState.Empty)
                {
                    continue;
                }

                cell.ContentId = (int)TicTacToeCellState.Empty;
                cleared ??= new List<FieldCell>();
                cleared.Add(cell);
            }

            CurrentPlayer = TicTacToeCellState.PlayerX;
            Winner = TicTacToeCellState.Empty;
            IsFinished = false;
            _winningLine.Clear();
            _stateVersion++;

            // WHY: notify after every mark is cleared and the round state is reset, so generator-driven views
            // observe the final empty board.
            if (cleared != null)
            {
                for (int i = 0; i < cleared.Count; i++)
                {
                    generator.OnCellStateChanged?.Invoke(cleared[i]);
                }
            }

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
        ///     Attempts to place a mark at a 3D grid position. On success the whole round state is committed first
        ///     (<see cref="Winner" />, <see cref="IsFinished" />, <see cref="WinningLine" /> and
        ///     <see cref="CurrentPlayer" />), then <see cref="FieldGenerator.OnCellStateChanged" /> is raised for the
        ///     marked cell, then the winner, draw or player-changed event.
        /// </summary>
        /// <remarks>
        ///     During the cell notification <see cref="CurrentPlayer" /> already holds the player who moves next, or
        ///     the player who just moved when the round finished. A cell listener may call
        ///     <see cref="TryMakeMove(Vector3Int)" /> (the reply is made for the next player) or
        ///     <see cref="ResetBoard" />; this call then skips its own winner, draw and player-changed events because
        ///     the nested call has already replaced that state.
        /// </remarks>
        /// <param name="pos">Target position in board coordinates.</param>
        /// <returns>True if move is accepted; otherwise false.</returns>
        public bool TryMakeMove(Vector3Int pos)
        {
            FieldGenerator generator = Generator;
            if (IsFinished || generator == null)
            {
                return false;
            }

            FieldCell cell = generator.GetCell(pos);
            if (cell == null || !cell.IsEnabled || !cell.IsWalkable || cell.IsOccupied)
            {
                return false;
            }

            if ((TicTacToeCellState)cell.ContentId != TicTacToeCellState.Empty)
            {
                return false;
            }

            TicTacToeCellState mover = CurrentPlayer;
            cell.ContentId = (int)mover;

            TicTacToeWinChecker.TryGetWinningLine(generator, _winningLine, out TicTacToeCellState winner,
                _winLineLength);
            Winner = winner;
            bool draw = winner == TicTacToeCellState.Empty && IsBoardFull();
            IsFinished = winner != TicTacToeCellState.Empty || draw;
            if (!IsFinished)
            {
                CurrentPlayer = mover == TicTacToeCellState.PlayerX
                    ? TicTacToeCellState.PlayerO
                    : TicTacToeCellState.PlayerX;
            }

            int version = ++_stateVersion;
            generator.OnCellStateChanged?.Invoke(cell);
            if (version != _stateVersion)
            {
                return true;
            }

            if (winner != TicTacToeCellState.Empty)
            {
                OnWinnerDetected.Invoke((int)winner);
                return true;
            }

            if (draw)
            {
                OnDrawDetected.Invoke();
                return true;
            }

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
