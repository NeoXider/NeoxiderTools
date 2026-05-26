using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using TMPro;
using UnityEngine;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemMatch3DemoUI")]
    public class GridSystemMatch3DemoUI : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private Match3BoardService _match3;
        [SerializeField] private FieldDebugDrawer _debugDrawer;
        [SerializeField] private TMP_Text _statusText;

        public void Configure(
            FieldGenerator generator,
            Match3BoardService match3,
            FieldDebugDrawer debugDrawer,
            TMP_Text statusText)
        {
            _generator = generator;
            _match3 = match3;
            _debugDrawer = debugDrawer;
            _statusText = statusText;
        }

        public void GenerateRectBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _generator.Config.Size = new Vector3Int(8, 8, 1);
            _generator.Config.GridType = GridType.Rectangular;
            ClearShapeOverrides();
            _generator.GenerateField();
            _match3.InitializeBoard();
            SetStatus("Rect 8x8 generated");
        }

        public void GenerateDiamondBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _generator.Config.Size = new Vector3Int(8, 8, 1);
            _generator.Config.GridType = GridType.Custom;
            ClearShapeOverrides();

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
            if (!HasRequiredServices())
            {
                return;
            }

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
            if (!HasRequiredServices())
            {
                return;
            }

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
            if (!HasRequiredServices())
            {
                return;
            }

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
            if (!HasRequiredServices() || _debugDrawer == null)
            {
                return;
            }

            if (!TryGetPathEndpoints(out Vector3Int start, out Vector3Int end))
            {
                SetStatus("Path not found: no usable endpoints");
                return;
            }

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
            if (!HasRequiredServices())
            {
                return;
            }

            if (!_match3.TryFindValidSwap(out Vector3Int a, out Vector3Int b))
            {
                bool shuffled = _match3.ShuffleIfNoMoves();
                SetStatus(shuffled ? "Board shuffled: try swap again" : "No valid swaps available");
                return;
            }

            bool resolved = _match3.TrySwapAndResolve(a, b);
            SetStatus(resolved ? $"Swap resolved: {a.x},{a.y} -> {b.x},{b.y}" : "Swap reverted");
        }

        public void RestartBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

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

        private bool TryGetPathEndpoints(out Vector3Int start, out Vector3Int end)
        {
            start = default;
            end = default;

            List<FieldCell> cells = new();
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                if (cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied)
                {
                    cells.Add(cell);
                }
            }

            if (cells.Count < 2)
            {
                return false;
            }

            start = cells[0].Position;
            end = cells[cells.Count - 1].Position;
            return true;
        }

        private void ClearShapeOverrides()
        {
            _generator.Config.ShapeMask = null;
            _generator.Config.DisabledCells.Clear();
            _generator.Config.ForcedEnabledCells.Clear();
            _generator.Config.BlockedCells.Clear();
            _generator.Config.ForcedWalkableCells.Clear();
        }

        private bool HasRequiredServices()
        {
            if (_generator != null && _match3 != null)
            {
                return true;
            }

            SetStatus("Demo is not wired");
            return false;
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
