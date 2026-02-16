using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using TMPro;
using UnityEngine;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neo/Demo/GridSystem/GridSystemMatch3DemoUI")]
    public class GridSystemMatch3DemoUI : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private Match3BoardService _match3;
        [SerializeField] private FieldDebugDrawer _debugDrawer;
        [SerializeField] private TMP_Text _statusText;

        public void GenerateRectBoard()
        {
            _generator.Config.Size = new Vector3Int(8, 8, 1);
            _generator.Config.GridType = GridType.Rectangular;
            _generator.Config.ShapeMask = null;
            _generator.Config.DisabledCells.Clear();
            _generator.Config.BlockedCells.Clear();
            _generator.GenerateField();
            _match3.InitializeBoard();
            SetStatus("Rect 8x8 generated");
        }

        public void GenerateDiamondBoard()
        {
            _generator.Config.Size = new Vector3Int(8, 8, 1);
            _generator.Config.GridType = GridType.Custom;
            _generator.Config.ShapeMask = null;
            _generator.Config.DisabledCells.Clear();
            _generator.Config.ForcedEnabledCells.Clear();

            Vector3Int center = new(3, 3, 0);
            int radius = 3;
            for (int x = 0; x < _generator.Config.Size.x; x++)
            for (int y = 0; y < _generator.Config.Size.y; y++)
            {
                int distance = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                if (distance <= radius)
                {
                    _generator.Config.ForcedEnabledCells.Add(new Vector3Int(x, y, 0));
                }
            }

            _generator.GenerateField();
            _match3.InitializeBoard();
            SetStatus("Diamond shape generated");
        }

        public void ToggleRandomBlocked()
        {
            FieldCell cell = GetRandomEnabledCell();
            if (cell == null)
            {
                return;
            }

            _generator.SetWalkable(cell.Position, !cell.IsWalkable);
            SetStatus($"Walkable toggled at {cell.Position}");
        }

        public void ToggleRandomDisabled()
        {
            FieldCell cell = GetRandomEnabledCell();
            if (cell == null)
            {
                return;
            }

            _generator.SetEnabled(cell.Position, false);
            SetStatus($"Disabled cell {cell.Position}");
        }

        public void ToggleRandomOccupied()
        {
            FieldCell cell = GetRandomEnabledCell();
            if (cell == null)
            {
                return;
            }

            _generator.SetOccupied(cell.Position, !cell.IsOccupied);
            SetStatus($"Occupied toggled at {cell.Position}");
        }

        public void RunPathDemo()
        {
            Vector3Int start = new(0, 0, 0);
            Vector3Int end = new(_generator.Config.Size.x - 1, _generator.Config.Size.y - 1, 0);
            GridPathResult result = _generator.FindPathDetailed(start, end);
            _debugDrawer.DrawPath = true;
            _debugDrawer.DebugPath.Clear();

            if (result.HasPath)
            {
                foreach (FieldCell cell in result.Path)
                {
                    _debugDrawer.DebugPath.Add(cell.Position);
                }

                SetStatus($"Path found. Len: {result.Path.Count}");
            }
            else
            {
                SetStatus($"Path not found: {result.Reason}");
            }
        }

        public void SwapRandom()
        {
            FieldCell a = GetRandomEnabledCell();
            FieldCell b = GetRandomEnabledCell();
            if (a == null || b == null)
            {
                return;
            }

            bool resolved = _match3.TrySwapAndResolve(a.Position, b.Position);
            SetStatus(resolved ? "Swap resolved with matches" : "Swap reverted (no matches)");
        }

        public void RestartBoard()
        {
            _match3.InitializeBoard();
            SetStatus("Board restarted");
        }

        public void RefillBoard()
        {
            RestartBoard();
        }

        private FieldCell GetRandomEnabledCell()
        {
            List<FieldCell> cells = new(_generator.GetAllCells(false));
            if (cells.Count == 0)
            {
                return null;
            }

            return cells[Random.Range(0, cells.Count)];
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