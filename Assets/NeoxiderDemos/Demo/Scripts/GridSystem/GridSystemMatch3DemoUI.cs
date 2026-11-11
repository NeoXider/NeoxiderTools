using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Samples;
using TMPro;
using UnityEngine;

namespace Neo.Demo.GridSystem
{
    /// <summary>
    ///     Controller of the authored GridSystem Match3 demo. The UI, its persistent listeners and the module
    ///     event wiring are saved in GridSystemMatch3Demo.unity; the buttons call the public
    ///     methods below, and <see cref="Match3BoardService" /> / <see cref="FieldGenerator" /> events call the
    ///     Handle* methods. Every method forwards to the real GridSystem API and logs what it did. It builds no UI.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemMatch3DemoUI")]
    public class GridSystemMatch3DemoUI : MonoBehaviour
    {
        private const int ReachSteps = 3;

        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private Match3BoardService _match3;
        [SerializeField] private FieldDebugDrawer _debugDrawer;
        [SerializeField] private TMP_Text _statusText;

        [Header("Authored demo references")]
        [SerializeField] private GridSystemMatch3BoardView _boardView;

        [Tooltip("Shape asset applied by GenerateCutCornersBoard (a GridShapeMask in DisabledCells mode).")]
        [SerializeField] private GridShapeMask _cutCornersMask;

        [SerializeField] private TMP_Text _boardValue;
        [SerializeField] private TMP_Text _clearedValue;
        [SerializeField] private TMP_Text _pathValue;
        [SerializeField] private DemoLogFeed _log;

        private readonly List<FieldCell> _cellBuffer = new();
        private readonly List<FieldCell> _pathBuffer = new();
        private readonly List<Vector3Int> _positionBuffer = new();
        private readonly GridPathRequest _pathRequest = new();
        private GridShapeMask _diamondMask;
        private string _shapeName = "custom";
        private int _cleared;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _generator != null && _match3 != null && _boardView != null && _log != null;

        /// <summary>Tiles cleared since the last restart (fed by Match3BoardService.OnMatchesResolved).</summary>
        public int ClearedTiles => _cleared;

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

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            _shapeName = _generator != null && _generator.Config.ShapeMask != null ? "cut corners" : "full";
            RefreshBoardLabel();
            RefreshClearedLabel();
            Log("Click two neighbouring tiles to swap them");
        }

        private void OnDestroy()
        {
            if (_diamondMask != null)
            {
                Destroy(_diamondMask);
            }
        }

        /// <summary>Full rectangle: no shape mask, no overrides.</summary>
        public void GenerateRectBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _generator.Config.GridType = GridType.Rectangular;
            ClearShapeOverrides();
            Regenerate("full");
        }

        /// <summary>Rectangle minus the cells listed in the authored GridShapeMask asset.</summary>
        public void GenerateCutCornersBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _generator.Config.GridType = GridType.Rectangular;
            ClearShapeOverrides();
            _generator.Config.ShapeMask = _cutCornersMask;
            Regenerate(_cutCornersMask != null ? "cut corners" : "full (no mask asset)");
        }

        /// <summary>Custom grid whose cells come from a code-built mask (GridShapeMask.Create).</summary>
        public void GenerateDiamondBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _generator.Config.GridType = GridType.Custom;
            ClearShapeOverrides();
            _generator.Config.ShapeMask = GetDiamondMask();
            Regenerate("diamond");
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
            SetStatus($"Walkable {cell.IsWalkable} at {Format(cell.Position)}");
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
            RefreshBoardLabel();
            SetStatus($"Disabled cell {Format(cell.Position)}");
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
            SetStatus($"Occupied {cell.IsOccupied} at {Format(cell.Position)}");
        }

        /// <summary>Shortest path between the first and last passable cells (GridPathfinder.TryFindPath).</summary>
        public void RunPathDemo()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            if (!TryGetPathEndpoints(out Vector3Int start, out Vector3Int end))
            {
                ShowCells(null, "no endpoints");
                SetStatus("Path not found: no usable endpoints");
                return;
            }

            _pathRequest.Start = start;
            _pathRequest.End = end;
            bool found = GridPathfinder.TryFindPath(_generator, _pathRequest, _pathBuffer, out NoPathReason reason);
            if (_debugDrawer != null)
            {
                _debugDrawer.DrawPath = true;
                _debugDrawer.DebugPath.Clear();
            }

            if (!found)
            {
                ShowCells(null, reason.ToString());
                SetStatus($"Path not found: {reason}");
                return;
            }

            _positionBuffer.Clear();
            foreach (FieldCell cell in _pathBuffer)
            {
                _positionBuffer.Add(cell.Position);
                if (_debugDrawer != null)
                {
                    _debugDrawer.DebugPath.Add(cell.Position);
                }
            }

            ShowCells(_positionBuffer, $"{_pathBuffer.Count} cells");
            SetStatus($"Path {Format(start)} -> {Format(end)}: {_pathBuffer.Count} cells");
        }

        /// <summary>Cells reachable within three moves of the first passable cell (GridPathfinder.CollectReachableCells).</summary>
        public void ShowReachableArea()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            if (!TryGetPathEndpoints(out Vector3Int start, out _))
            {
                ShowCells(null, "no start cell");
                return;
            }

            _pathRequest.Start = start;
            _pathRequest.End = start;
            int count = GridPathfinder.CollectReachableCells(_generator, _pathRequest, ReachSteps, _cellBuffer);
            _positionBuffer.Clear();
            foreach (FieldCell cell in _cellBuffer)
            {
                _positionBuffer.Add(cell.Position);
            }

            ShowCells(_positionBuffer, $"{count} cells in {ReachSteps} moves");
            SetStatus($"Reachable from {Format(start)} in {ReachSteps} moves: {count}");
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
            SetStatus(resolved ? $"Swap resolved: {Format(a)} -> {Format(b)}" : "Swap reverted");
        }

        public void RestartBoard()
        {
            if (!HasRequiredServices())
            {
                return;
            }

            _cleared = 0;
            ClearHighlight();
            _match3.InitializeBoard();
            RefreshClearedLabel();
            SetStatus("Board restarted · Match3BoardService.InitializeBoard");
        }

        public void RefillBoard()
        {
            RestartBoard();
        }

        /// <summary>Persistent target of <see cref="Match3BoardService.OnMatchesResolved" />.</summary>
        public void HandleMatchesResolved(int cleared)
        {
            if (cleared <= 0)
            {
                return;
            }

            _cleared += cleared;
            RefreshClearedLabel();
            Log($"Cleared {cleared} tiles · OnMatchesResolved");
        }

        /// <summary>Persistent target of <see cref="Match3BoardService.OnBoardShuffled" />.</summary>
        public void HandleBoardShuffled()
        {
            Log("No moves left: board shuffled · OnBoardShuffled");
        }

        /// <summary>Persistent target of <see cref="FieldGenerator.OnFieldGenerated" />.</summary>
        public void HandleFieldGenerated()
        {
            RefreshBoardLabel();
        }

        private void Regenerate(string shapeName)
        {
            _shapeName = shapeName;
            ClearHighlight();
            _generator.GenerateField();
            _match3.InitializeBoard();
            RefreshBoardLabel();
            SetStatus($"Field generated: {shapeName}");
        }

        private GridShapeMask GetDiamondMask()
        {
            if (_diamondMask != null)
            {
                return _diamondMask;
            }

            Vector3Int size = _generator.Config.Size;
            Vector3Int center = new((size.x - 1) / 2, (size.y - 1) / 2, 0);
            int radius = Mathf.Max(1, Mathf.Min(size.x, size.y) / 2 - 1);
            _positionBuffer.Clear();
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                if (Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y) <= radius)
                {
                    _positionBuffer.Add(new Vector3Int(x, y, 0));
                }
            }

            _diamondMask = GridShapeMask.Create(GridShapeMaskMode.EnabledCells, _positionBuffer);
            _diamondMask.name = "Diamond (runtime mask)";
            return _diamondMask;
        }

        private void ShowCells(IReadOnlyList<Vector3Int> cells, string summary)
        {
            if (_boardView != null)
            {
                if (cells == null)
                {
                    _boardView.ClearHighlight();
                }
                else
                {
                    _boardView.SetHighlightedCells(cells);
                }
            }

            if (_pathValue != null)
            {
                _pathValue.text = summary;
            }
        }

        private void ClearHighlight()
        {
            ShowCells(null, "-");
        }

        private FieldCell GetRandomEnabledCell()
        {
            _cellBuffer.Clear();
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                _cellBuffer.Add(cell);
            }

            return _cellBuffer.Count == 0 ? null : _cellBuffer[Random.Range(0, _cellBuffer.Count)];
        }

        private bool TryGetPathEndpoints(out Vector3Int start, out Vector3Int end)
        {
            start = default;
            end = default;
            bool hasStart = false;
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                if (!cell.IsEnabled || !cell.IsWalkable || cell.IsOccupied)
                {
                    continue;
                }

                if (!hasStart)
                {
                    start = cell.Position;
                    hasStart = true;
                }

                end = cell.Position;
            }

            return hasStart && start != end;
        }

        private void ClearShapeOverrides()
        {
            _generator.Config.ShapeMask = null;
            _generator.Config.DisabledCells.Clear();
            _generator.Config.ForcedEnabledCells.Clear();
            _generator.Config.BlockedCells.Clear();
            _generator.Config.ForcedWalkableCells.Clear();
        }

        private void RefreshBoardLabel()
        {
            if (_boardValue == null || _generator == null)
            {
                return;
            }

            int enabled = 0;
            foreach (FieldCell _ in _generator.GetAllCells(false))
            {
                enabled++;
            }

            Vector3Int size = _generator.GeneratedSize;
            _boardValue.text = $"{size.x}×{size.y} {_shapeName} · {enabled} cells";
        }

        private void RefreshClearedLabel()
        {
            if (_clearedValue != null)
            {
                _clearedValue.text = _cleared.ToString();
            }
        }

        private bool HasRequiredServices()
        {
            if (_generator != null && _match3 != null)
            {
                return true;
            }

            SetStatus("Demo is not wired");
            WarnUnbuilt();
            return false;
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }

            Log(text);
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
                "[GridSystemMatch3DemoUI] Scene references are missing; re-import the scene from the Demo download.",
                this);
        }

        private static string Format(Vector3Int position)
        {
            return position.x + "," + position.y;
        }
    }
}
