using Neo.GridSystem;
using Neo.GridSystem.TicTacToe;
using TMPro;
using UnityEngine;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neo/Demo/GridSystem/GridSystemTicTacToeDemoUI")]
    public class GridSystemTicTacToeDemoUI : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private TicTacToeBoardService _board;
        [SerializeField] private FieldDebugDrawer _debugDrawer;
        [SerializeField] private TMP_Text _statusText;

        private void OnEnable()
        {
            if (_board == null)
            {
                return;
            }

            _board.OnPlayerChanged.AddListener(HandlePlayerChanged);
            _board.OnWinnerDetected.AddListener(HandleWinnerDetected);
            _board.OnDrawDetected.AddListener(HandleDraw);
        }

        private void OnDisable()
        {
            if (_board == null)
            {
                return;
            }

            _board.OnPlayerChanged.RemoveListener(HandlePlayerChanged);
            _board.OnWinnerDetected.RemoveListener(HandleWinnerDetected);
            _board.OnDrawDetected.RemoveListener(HandleDraw);
        }

        public void ResetBoard()
        {
            _board.ResetBoard();
            SetStatus("Board reset. Player X turn.");
        }

        public void MakeRandomMove()
        {
            for (int i = 0; i < 100; i++)
            {
                int x = Random.Range(0, _generator.Config.Size.x);
                int y = Random.Range(0, _generator.Config.Size.y);
                if (_board.TryMakeMove(new Vector2Int(x, y)))
                {
                    SetStatus($"Move placed at ({x},{y})");
                    return;
                }
            }

            SetStatus("No valid move found");
        }

        public void ToggleCenterBlocked()
        {
            Vector3Int center = new(_generator.Config.Size.x / 2, _generator.Config.Size.y / 2, 0);
            FieldCell cell = _generator.GetCell(center);
            if (cell == null)
            {
                return;
            }

            _generator.SetWalkable(center, !cell.IsWalkable);
            SetStatus($"Center walkable: {_generator.GetCell(center).IsWalkable}");
        }

        public void ToggleCornerDisabled()
        {
            Vector3Int corner = new(0, 0, 0);
            FieldCell cell = _generator.GetCell(corner);
            if (cell == null)
            {
                return;
            }

            _generator.SetEnabled(corner, !cell.IsEnabled);
            SetStatus($"Corner enabled: {_generator.GetCell(corner).IsEnabled}");
        }

        public void RunPathDemo()
        {
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

        private void HandlePlayerChanged(int player)
        {
            SetStatus(player == (int)TicTacToeCellState.PlayerX ? "Player X turn" : "Player O turn");
        }

        private void HandleWinnerDetected(int winner)
        {
            SetStatus(winner == (int)TicTacToeCellState.PlayerX ? "Winner: X" : "Winner: O");
        }

        private void HandleDraw()
        {
            SetStatus("Draw");
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }
    }
}