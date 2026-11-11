using System.Collections;
using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.TicTacToe;
using Neo.Samples;
using TMPro;
using UnityEngine;

namespace Neo.Demo.GridSystem
{
    /// <summary>
    ///     Controller of the authored GridSystem TicTacToe demo. The UI and every listener are saved in
    ///     GridSystemTicTacToeDemo.unity by its scene builder: buttons call the public methods below (Reset calls
    ///     <see cref="TicTacToeBoardService.ResetBoard" /> directly), and the service events
    ///     (<see cref="TicTacToeBoardService.OnPlayerChanged" />, OnWinnerDetected, OnDrawDetected) call the Handle*
    ///     methods through persistent UnityEvents. An optional bot answers as O. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemTicTacToeDemoUI")]
    public class GridSystemTicTacToeDemoUI : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private TicTacToeBoardService _board;
        [SerializeField] private FieldDebugDrawer _debugDrawer;
        [SerializeField] private TMP_Text _statusText;

        [Header("Authored demo references")]
        [SerializeField] private TMP_Text _rulesValue;
        [SerializeField] private DemoLogFeed _log;

        [Tooltip("When on, the demo answers every X move with a random O move.")]
        [SerializeField] private bool _botPlaysO = true;

        [Min(0f)] [SerializeField] private float _botDelay = 0.35f;

        private readonly List<FieldCell> _freeCells = new();
        private Coroutine _botRoutine;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _generator != null && _board != null && _statusText != null && _log != null;

        public bool BotPlaysO => _botPlaysO;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            RefreshRulesLabel();
            Log("Click a cell to place X" + (_botPlaysO ? "; the bot answers as O" : string.Empty));
        }

        private void OnDisable()
        {
            StopBot();
        }

        public void ResetBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            StopBot();
            _board.ResetBoard();
            SetStatus("Board reset. Player X turn.");
        }

        public void MakeRandomMove()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _freeCells.Clear();
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                if (cell.IsWalkable && !cell.IsOccupied && cell.ContentId == (int)TicTacToeCellState.Empty)
                {
                    _freeCells.Add(cell);
                }
            }

            if (_freeCells.Count == 0 || _board.IsFinished)
            {
                SetStatus("No valid move found");
                return;
            }

            FieldCell target = _freeCells[Random.Range(0, _freeCells.Count)];
            TicTacToeCellState mover = _board.CurrentPlayer;
            if (_board.TryMakeMove(target.Position))
            {
                Log($"{Mark(mover)} played {target.Position.x},{target.Position.y} · TryMakeMove");
            }
        }

        /// <summary>Classic 3×3 board, three in a row wins (WinLineLength 3).</summary>
        public void UseClassicBoard()
        {
            ApplyRules(3, 3);
        }

        /// <summary>5×5 board where four in a row wins (WinLineLength 4).</summary>
        public void UseFourInARowBoard()
        {
            ApplyRules(5, 4);
        }

        /// <summary>Toggle target: lets the bot answer as O.</summary>
        public void SetBotPlaysO(bool enabled)
        {
            _botPlaysO = enabled;
            if (!enabled)
            {
                StopBot();
            }

            Log(enabled ? "Bot plays O" : "Two players on one mouse");
            if (enabled && _board != null && !_board.IsFinished && _board.CurrentPlayer == TicTacToeCellState.PlayerO)
            {
                StartBot();
            }
        }

        public void ToggleCenterBlocked()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            Vector3Int center = new(_generator.Config.Size.x / 2, _generator.Config.Size.y / 2, 0);
            FieldCell cell = _generator.GetCell(center);
            if (cell == null)
            {
                return;
            }

            _generator.SetWalkable(center, !cell.IsWalkable);
            SetStatus($"Center walkable: {_generator.GetCell(center).IsWalkable} · SetWalkable");
        }

        public void ToggleCornerDisabled()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            Vector3Int corner = new(0, 0, 0);
            FieldCell cell = _generator.GetCell(corner);
            if (cell == null)
            {
                return;
            }

            _generator.SetEnabled(corner, !cell.IsEnabled);
            SetStatus($"Corner enabled: {_generator.GetCell(corner).IsEnabled} · SetEnabled");
        }

        public void RunPathDemo()
        {
            if (!HasRequiredServices() || _debugDrawer == null)
            {
                return;
            }

            GridPathResult result = _generator.FindPathDetailed(new Vector3Int(0, 0, 0),
                new Vector3Int(_generator.Config.Size.x - 1, _generator.Config.Size.y - 1, 0));
            _debugDrawer.DrawPath = true;
            _debugDrawer.DebugPath.Clear();

            if (result.HasPath)
            {
                foreach (FieldCell step in result.Path)
                {
                    _debugDrawer.DebugPath.Add(step.Position);
                }

                SetStatus($"Path len: {result.Path.Count}");
            }
            else
            {
                SetStatus($"Path missing: {result.Reason}");
            }
        }

        /// <summary>Persistent target of <see cref="TicTacToeBoardService.OnPlayerChanged" />.</summary>
        public void HandlePlayerChanged(int player)
        {
            TicTacToeCellState current = (TicTacToeCellState)player;
            SetStatusText($"Player {Mark(current)} turn");
            if (_botPlaysO && current == TicTacToeCellState.PlayerO)
            {
                StartBot();
            }
        }

        /// <summary>Persistent target of <see cref="TicTacToeBoardService.OnWinnerDetected" />.</summary>
        public void HandleWinnerDetected(int winner)
        {
            StopBot();
            int length = _board != null ? _board.WinningLine.Count : 0;
            SetStatus($"Winner: {Mark((TicTacToeCellState)winner)} · {length} in a row");
        }

        /// <summary>Persistent target of <see cref="TicTacToeBoardService.OnDrawDetected" />.</summary>
        public void HandleDraw()
        {
            StopBot();
            SetStatus("Draw · OnDrawDetected");
        }

        private void ApplyRules(int size, int winLength)
        {
            if (!HasRequiredServices())
            {
                return;
            }

            StopBot();
            _generator.Config.Size = new Vector3Int(size, size, 1);
            _board.WinLineLength = winLength;
            _generator.GenerateField();
            _board.ResetBoard();
            RefreshRulesLabel();
            SetStatus($"{size}×{size} board, {winLength} in a row · GenerateField + WinLineLength");
        }

        private void StartBot()
        {
            if (!isActiveAndEnabled || _botRoutine != null)
            {
                return;
            }

            _botRoutine = StartCoroutine(BotMove());
        }

        private void StopBot()
        {
            if (_botRoutine != null)
            {
                StopCoroutine(_botRoutine);
                _botRoutine = null;
            }
        }

        private IEnumerator BotMove()
        {
            if (_botDelay > 0f)
            {
                yield return new WaitForSeconds(_botDelay);
            }

            _botRoutine = null;
            if (_board != null && !_board.IsFinished && _board.CurrentPlayer == TicTacToeCellState.PlayerO)
            {
                MakeRandomMove();
            }
        }

        private void RefreshRulesLabel()
        {
            if (_rulesValue == null || _generator == null || _board == null)
            {
                return;
            }

            Vector3Int size = _generator.Config.Size;
            int length = TicTacToeWinChecker.ResolveWinLineLength(_generator, _board.WinLineLength);
            _rulesValue.text = $"{size.x}×{size.y} · {length} in a row";
        }

        private bool HasRequiredServices()
        {
            if (_generator != null && _board != null)
            {
                return true;
            }

            SetStatusText("Demo is not wired");
            WarnUnbuilt();
            return false;
        }

        private void SetStatus(string text)
        {
            SetStatusText(text);
            Log(text);
        }

        private void SetStatusText(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }

        private void Log(string text)
        {
            if (_log != null)
            {
                _log.Log(text);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[GridSystemTicTacToeDemoUI] Scene references are missing; run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }

        private static string Mark(TicTacToeCellState state)
        {
            return state == TicTacToeCellState.PlayerX ? "X" : state == TicTacToeCellState.PlayerO ? "O" : "-";
        }
    }
}
