using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.GridSystem
{
    /// <summary>
    ///     Generates and manages a runtime grid field with shape, state and pathfinding helpers.
    /// </summary>
    [NeoDoc("GridSystem/FieldGenerator.md")]
    [RequireComponent(typeof(Grid))]
    [CreateFromMenu("Neoxider/GridSystem/FieldGenerator")]
    [AddComponentMenu("Neoxider/" + "GridSystem/" + nameof(FieldGenerator))]
    public class FieldGenerator : MonoBehaviour
    {
        [Header("Settings")] public FieldGeneratorConfig Config = new();

        [Header("Debug")] public bool DebugEnabled = true;
        public FieldGizmoSettings GizmoSettings = new();

        /// <summary>
        ///     Invoked after field generation is completed.
        /// </summary>
        public UnityEvent OnFieldGenerated = new();

        /// <summary>
        ///     Invoked when legacy cell data is changed via <see cref="SetCell(Vector3Int,int,bool)" />.
        /// </summary>
        public CellChangedEvent OnCellChanged = new();

        /// <summary>
        ///     Invoked when cell runtime state is changed.
        /// </summary>
        public CellStateChangedEvent OnCellStateChanged = new();

        private Grid unityGrid;

        // WHY: GetCell/InBounds run in every gameplay scan; caching the dimensions avoids three GetLength calls per
        // lookup. NonSerialized keeps a hot reload from restoring a size without the (unserializable) cell array.
        [NonSerialized] private Vector3Int _generatedSize;

        [NonSerialized] private PlacementScratch _placementScratch;

#if UNITY_EDITOR
        [NonSerialized] private string[] _gizmoLabels;
        [NonSerialized] private FieldCell[,,] _gizmoLabelCells;
#endif

        /// <summary>
        ///     Global singleton reference to the latest initialized generator.
        /// </summary>
        public static FieldGenerator I { get; private set; }

        // WHY: without this reset the next play session starts holding the previous session's destroyed
        // component when Enter Play Mode Options disable domain reload (MissingReferenceException).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            I = null;
        }

        /// <summary>
        ///     Backing 3D cell array.
        /// </summary>
        public FieldCell[,,] Cells { get; private set; }

        /// <summary>
        ///     Dimensions of the generated cell array, or zero before a field is available.
        ///     Changing Config.Size takes effect when GenerateField is called.
        /// </summary>
        public Vector3Int GeneratedSize => Cells != null ? _generatedSize : Vector3Int.zero;

        /// <summary>
        ///     Returns a 2D projection (z=0) of the current grid.
        /// </summary>
        /// <remarks>
        ///     Every access allocates a new array. Read it once into a local, or use <see cref="GetCell(int,int)" /> in
        ///     loops.
        /// </remarks>
        public FieldCell[,] Cells2D
        {
            get
            {
                Vector3Int size = GeneratedSize;
                FieldCell[,] arr = new FieldCell[size.x, size.y];
                if (Cells == null)
                {
                    return arr;
                }

                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                {
                    arr[x, y] = InBounds(new Vector3Int(x, y, 0))
                        ? Cells[x, y, 0]
                        : null;
                }

                return arr;
            }
        }

        /// <summary>
        ///     Cached Unity <see cref="Grid" /> component used for world-grid conversion.
        /// </summary>
        public Grid UnityGrid
        {
            get
            {
                if (unityGrid == null)
                {
                    unityGrid = GetComponent<Grid>();
                }

                return unityGrid;
            }
        }

        private void Awake()
        {
            I = this;
            if (unityGrid == null)
            {
                unityGrid = GetComponent<Grid>();
            }

            if (Cells == null || Cells.Length == 0)
            {
                GenerateField();
            }
        }

        private void OnDestroy()
        {
            if (I == this)
            {
                I = null;
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (unityGrid == null)
                {
                    unityGrid = GetComponent<Grid>();
                }

                GenerateField();
            }
        }

        private void OnDrawGizmos()
        {
            DrawFieldGizmos();
        }

        [Button("Regenerate Field")]
        public void RegenerateFieldButton()
        {
            GenerateField();
        }

        [Button("Apply Shape && Overrides")]
        public void ApplyShapeAndOverridesButton()
        {
            ApplyShapeMask();
            OnFieldGenerated?.Invoke();
        }

        [Button("Clear Manual Overrides")]
        public void ClearManualOverridesButton()
        {
            if (Config == null)
            {
                return;
            }

            Config.DisabledCells.Clear();
            Config.ForcedEnabledCells.Clear();
            Config.BlockedCells.Clear();
            Config.ForcedWalkableCells.Clear();

            ApplyShapeMask();
            OnFieldGenerated?.Invoke();
        }

        [Button("Set Origin Center")]
        public void SetOriginCenterButton()
        {
            if (Config == null)
            {
                return;
            }

            Config.Origin2D = GridOrigin2D.Center;
            Config.OriginDepth = GridOriginDepth.Center;
            Config.OriginOffset = Vector3Int.zero;
            GenerateField();
        }

        /// <summary>
        ///     Generates grid cells using the current or provided configuration.
        /// </summary>
        /// <param name="config">Optional config override.</param>
        public void GenerateField(FieldGeneratorConfig config = null)
        {
            if (config != null)
            {
                Config = config;
            }

            Config ??= new FieldGeneratorConfig();
            Config.ApplyMovementPreset();
            Vector3Int size = Config.Size;
            if (size.z == 0)
            {
                size.z = 1;
                if (Config != null)
                {
                    Config.Size = size;
                }
            }

            if (size.x <= 0 || size.y <= 0 || size.z < 0)
            {
                NeoDiagnostics.LogError("FieldGenerator: Invalid field size! Size: " + size);
                Cells = null;
                _generatedSize = Vector3Int.zero;
                return;
            }

            Cells = new FieldCell[size.x, size.y, size.z];
            _generatedSize = size;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                Vector3Int pos = new(x, y, z);
                bool enabled = ResolveCellEnabled(pos);
                Cells[x, y, z] = new FieldCell(pos, isEnabled: enabled, isWalkable: enabled);
            }

            ApplyStateOverrides();
            OnFieldGenerated?.Invoke();
        }

        /// <summary>
        ///     Reapplies current shape mask and state overrides to existing cells.
        /// </summary>
        public void ApplyShapeMask()
        {
            if (Cells == null)
            {
                return;
            }

            foreach (FieldCell cell in GetAllCells())
            {
                bool enabled = ResolveCellEnabled(cell.Position);
                cell.IsEnabled = enabled;
                if (!enabled)
                {
                    cell.IsWalkable = false;
                    cell.IsOccupied = false;
                }
            }

            ApplyStateOverrides();
        }

        /// <summary>
        ///     Gets cell by 3D position.
        /// </summary>
        /// <param name="pos">Cell coordinates.</param>
        /// <returns>Cell instance or null when out of bounds.</returns>
        public FieldCell GetCell(Vector3Int pos)
        {
            if (Cells == null)
            {
                return null;
            }

            if (InBounds(pos))
            {
                return Cells[pos.x, pos.y, pos.z];
            }

            return null;
        }

        /// <summary>
        ///     Gets cell by 2D coordinates (z=0).
        /// </summary>
        public FieldCell GetCell(int x, int y)
        {
            return GetCell(new Vector3Int(x, y, 0));
        }

        /// <summary>
        ///     Gets cell by <see cref="Vector2Int" /> (z=0).
        /// </summary>
        public FieldCell GetCell(Vector2Int pos)
        {
            return GetCell(new Vector3Int(pos.x, pos.y, 0));
        }

        /// <summary>
        ///     Gets cell from world position via Unity Grid conversion.
        /// </summary>
        /// <param name="worldPosition">World-space position.</param>
        /// <returns>Cell instance or null.</returns>
        public FieldCell GetCellFromWorld(Vector3 worldPosition)
        {
            if (UnityGrid == null)
            {
                return null;
            }

            Vector3Int gridPos = UnityGrid.WorldToCell(worldPosition - GetOriginWorldCorrection());
            Vector3Int logicalPos = GridToLogicalPosition(gridPos);
            return GetCell(logicalPos);
        }

        /// <summary>
        ///     Resolves the logical cell position nearest to a world-space point using this generator's grid conversion.
        ///     Useful for drag/drop placement, cursor previews, and board picking.
        /// </summary>
        public bool TryGetCellPositionFromWorld(Vector3 worldPosition, out Vector3Int position)
        {
            FieldCell cell = GetNearestCellFromWorld(worldPosition, false);
            if (cell == null)
            {
                position = default;
                return false;
            }

            position = cell.Position;
            return true;
        }

        /// <summary>
        ///     Snaps a world-space point to the center of the nearest valid cell.
        /// </summary>
        public bool TrySnapWorldToCellCenter(Vector3 worldPosition, out Vector3 snappedWorldPosition)
        {
            FieldCell cell = GetNearestCellFromWorld(worldPosition, false);
            if (cell == null)
            {
                snappedWorldPosition = worldPosition;
                return false;
            }

            snappedWorldPosition = GetCellWorldCenter(cell.Position);
            return true;
        }

        /// <summary>
        ///     Snaps a world-space point to a cell center when possible, otherwise returns the original point.
        /// </summary>
        public Vector3 SnapWorldToCellCenter(Vector3 worldPosition)
        {
            return TrySnapWorldToCellCenter(worldPosition, out Vector3 snappedWorldPosition)
                ? snappedWorldPosition
                : worldPosition;
        }

        /// <summary>
        ///     Finds the nearest generated cell center to a world-space point.
        ///     This is intended for cursor previews, touch drag/drop and shape placement.
        /// </summary>
        /// <remarks>
        ///     On rectangular Unity grids without cell gaps (and a positive cell size on every axis with several layers)
        ///     the cell containing the point is the nearest one, so an eligible containing cell is returned in constant
        ///     time. Other layouts, points outside the board and points over excluded (disabled) cells fall back to a
        ///     scan of all cells. A point exactly on an edge shared by two cells resolves to the floored
        ///     (positive-side) cell.
        /// </remarks>
        public FieldCell GetNearestCellFromWorld(Vector3 worldPosition, bool includeDisabled = false)
        {
            FieldCell[,,] cells = Cells;
            if (cells == null)
            {
                return null;
            }

            Grid grid = UnityGrid;
            if (grid == null)
            {
                return GetNearestCellByScan(cells, worldPosition, includeDisabled);
            }

            if (TryGetContainingCell(worldPosition, includeDisabled, out FieldCell containingCell))
            {
                return containingCell;
            }

            Vector3 originCorrection = GetOriginWorldCorrection();
            Vector3Int originOffset = GetGridOriginOffset();
            FieldCell nearest = null;
            float nearestDistance = float.PositiveInfinity;
            Vector3Int size = _generatedSize;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = cells[x, y, z];
                if (cell == null || (!includeDisabled && !cell.IsEnabled))
                {
                    continue;
                }

                Vector3 center = grid.GetCellCenterWorld(cell.Position + originOffset) + originCorrection;
                float distance = (center - worldPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = cell;
                }
            }

            return nearest;
        }

        /// <summary>
        ///     Constant-time fast path of <see cref="GetNearestCellFromWorld" />: resolves the eligible cell whose box
        ///     contains the point. A point on an edge shared by two cells resolves to the floored (positive-side) cell.
        /// </summary>
        /// <returns>
        ///     False when the layout is not supported (see <see cref="CanResolveContainingCellDirectly" />), the point
        ///     lies outside the board, or the containing cell is disabled and <paramref name="includeDisabled" /> is
        ///     false; the caller then falls back to the full scan.
        /// </returns>
        internal bool TryGetContainingCell(Vector3 worldPosition, bool includeDisabled, out FieldCell cell)
        {
            cell = null;
            FieldCell[,,] cells = Cells;
            Grid grid = UnityGrid;
            if (cells == null || grid == null || !CanResolveContainingCellDirectly(grid))
            {
                return false;
            }

            Vector3Int containing = grid.WorldToCell(worldPosition - GetOriginWorldCorrection()) -
                                    GetGridOriginOffset();
            if (!InBounds(containing))
            {
                return false;
            }

            FieldCell candidate = cells[containing.x, containing.y, containing.z];
            if (candidate == null || (!includeDisabled && !candidate.IsEnabled))
            {
                return false;
            }

            cell = candidate;
            return true;
        }

        // WHY: on a gapless rectangular lattice each axis is independent, so the cell whose box contains the point also
        // has the nearest center. Axes with several layers but zero cell size collapse those layers onto one point;
        // there the scan keeps its first-cell tie-break instead.
        private bool CanResolveContainingCellDirectly(Grid grid)
        {
            if (grid.cellLayout != GridLayout.CellLayout.Rectangle || grid.cellGap != Vector3.zero)
            {
                return false;
            }

            Vector3 cellSize = grid.cellSize;
            Vector3Int size = _generatedSize;
            return (size.x <= 1 || cellSize.x > 0f) &&
                   (size.y <= 1 || cellSize.y > 0f) &&
                   (size.z <= 1 || cellSize.z > 0f);
        }

        private FieldCell GetNearestCellByScan(FieldCell[,,] cells, Vector3 worldPosition, bool includeDisabled)
        {
            FieldCell nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (FieldCell cell in cells)
            {
                if (cell == null || (!includeDisabled && !cell.IsEnabled))
                {
                    continue;
                }

                float distance = (GetCellWorldCenter(cell.Position) - worldPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = cell;
                }
            }

            return nearest;
        }

        /// <summary>
        ///     Checks whether a multi-cell content footprint can be placed at the anchor position.
        /// </summary>
        /// <remarks>
        ///     Validation reuses internal buffers, so checking a list or array of entries allocates nothing. This keeps
        ///     per-frame previews and "can this piece fit anywhere" scans cheap.
        /// </remarks>
        public bool CanPlaceContentFootprint(
            Vector3Int anchor,
            IEnumerable<GridPlacementEntry> entries,
            bool requireEnabled = true,
            bool requireWalkable = true,
            bool requireUnoccupied = true)
        {
            PlacementScratch scratch = AcquirePlacementScratch();
            try
            {
                return ValidateContentFootprint(
                    anchor,
                    entries,
                    requireEnabled,
                    requireWalkable,
                    requireUnoccupied,
                    scratch);
            }
            finally
            {
                ReleasePlacementScratch(scratch);
            }
        }

        /// <summary>
        ///     Writes a multi-cell content footprint into FieldCell state.
        /// </summary>
        public GridPlacementResult PlaceContentFootprint(
            Vector3Int anchor,
            IEnumerable<GridPlacementEntry> entries,
            bool requireEnabled = true,
            bool requireWalkable = true,
            bool requireUnoccupied = true,
            bool notify = true)
        {
            GridPlacementResult result = new GridPlacementResult();
            PlacementScratch scratch = AcquirePlacementScratch();
            try
            {
                if (!ValidateContentFootprint(
                        anchor,
                        entries,
                        requireEnabled,
                        requireWalkable,
                        requireUnoccupied,
                        scratch))
                {
                    result.FailureReason = "Footprint contains invalid cells.";
                    return result;
                }

                List<GridPlacementResolvedEntry> resolved = scratch.Resolved;
                for (int i = 0; i < resolved.Count; i++)
                {
                    GridPlacementResolvedEntry entry = resolved[i];
                    entry.Cell.ContentId = entry.ContentId;
                    entry.Cell.IsOccupied = entry.OccupiesCell;
                    result.Cells.Add(entry.Cell);
                    result.Positions.Add(entry.Cell.Position);
                }
            }
            finally
            {
                // WHY: released before notifying so a listener can validate or place again without allocating.
                ReleasePlacementScratch(scratch);
            }

            if (notify)
            {
                foreach (FieldCell cell in result.Cells)
                {
                    OnCellStateChanged?.Invoke(cell);
                }
            }

            result.Placed = true;
            return result;
        }

        /// <summary>
        ///     Enumerates all cells in backing array.
        /// </summary>
        /// <param name="includeDisabled">Include disabled cells when true.</param>
        /// <returns>Cell sequence.</returns>
        public IEnumerable<FieldCell> GetAllCells(bool includeDisabled = true)
        {
            FieldCell[,,] cells = Cells;
            if (cells == null)
            {
                yield break;
            }

            Vector3Int size = new Vector3Int(cells.GetLength(0), cells.GetLength(1), cells.GetLength(2));
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = cells[x, y, z];
                if (cell == null)
                {
                    continue;
                }

                if (!includeDisabled && !cell.IsEnabled)
                {
                    continue;
                }

                yield return cell;
            }
        }

        /// <summary>
        ///     Checks whether 3D position is inside the generated cell array.
        /// </summary>
        public bool InBounds(Vector3Int pos)
        {
            Vector3Int s = GeneratedSize;
            return pos.x >= 0 && pos.x < s.x && pos.y >= 0 && pos.y < s.y && pos.z >= 0 && pos.z < s.z;
        }

        /// <summary>
        ///     Checks whether 2D coordinates (z=0) are inside configured bounds.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return InBounds(new Vector3Int(x, y, 0));
        }

        /// <summary>
        ///     Checks whether 2D vector (z=0) is inside configured bounds.
        /// </summary>
        public bool InBounds(Vector2Int pos)
        {
            return InBounds(new Vector3Int(pos.x, pos.y, 0));
        }

        /// <summary>
        ///     Sets legacy cell type and walkability.
        /// </summary>
        /// <param name="pos">Target position.</param>
        /// <param name="type">Custom type id.</param>
        /// <param name="isWalkable">Walkability state.</param>
        public void SetCell(Vector3Int pos, int type, bool isWalkable)
        {
            FieldCell cell = GetCell(pos);
            if (cell == null)
            {
                return;
            }

            cell.Type = type;
            cell.IsWalkable = isWalkable;
            OnCellChanged?.Invoke(cell);
            OnCellStateChanged?.Invoke(cell);
        }

        /// <summary>
        ///     Sets legacy cell type and walkability using 2D coordinates.
        /// </summary>
        public void SetCell(int x, int y, int type, bool isWalkable)
        {
            SetCell(new Vector3Int(x, y, 0), type, isWalkable);
        }

        /// <summary>
        ///     Sets legacy cell type and walkability using 2D vector.
        /// </summary>
        public void SetCell(Vector2Int pos, int type, bool isWalkable)
        {
            SetCell(new Vector3Int(pos.x, pos.y, 0), type, isWalkable);
        }

        /// <summary>
        ///     Sets walkability for a cell.
        /// </summary>
        public void SetWalkable(Vector3Int pos, bool isWalkable)
        {
            FieldCell cell = GetCell(pos);
            if (cell == null)
            {
                return;
            }

            cell.IsWalkable = isWalkable;
            OnCellStateChanged?.Invoke(cell);
        }

        /// <summary>
        ///     Enables or disables a cell in active board shape.
        /// </summary>
        public void SetEnabled(Vector3Int pos, bool isEnabled)
        {
            FieldCell cell = GetCell(pos);
            if (cell == null)
            {
                return;
            }

            cell.IsEnabled = isEnabled;
            if (!isEnabled)
            {
                cell.IsWalkable = false;
                cell.IsOccupied = false;
            }

            OnCellStateChanged?.Invoke(cell);
        }

        /// <summary>
        ///     Marks a cell as occupied or free.
        /// </summary>
        public void SetOccupied(Vector3Int pos, bool isOccupied)
        {
            FieldCell cell = GetCell(pos);
            if (cell == null)
            {
                return;
            }

            cell.IsOccupied = isOccupied;
            OnCellStateChanged?.Invoke(cell);
        }

        /// <summary>
        ///     Sets content state id for a cell.
        /// </summary>
        public void SetContentId(Vector3Int pos, int contentId)
        {
            FieldCell cell = GetCell(pos);
            if (cell == null)
            {
                return;
            }

            cell.ContentId = contentId;
            OnCellStateChanged?.Invoke(cell);
        }

        /// <summary>
        ///     Evaluates whether cell is passable under current passability settings.
        /// </summary>
        /// <param name="cell">Cell to evaluate.</param>
        /// <param name="ignoreOccupied">Ignore occupied state if true.</param>
        /// <param name="ignoreDisabled">Ignore enabled/disabled state if true.</param>
        /// <param name="ignoreWalkability">Ignore walkability flag if true.</param>
        /// <returns>True when passable.</returns>
        public bool IsCellPassable(
            FieldCell cell,
            bool ignoreOccupied = false,
            bool ignoreDisabled = false,
            bool ignoreWalkability = false)
        {
            if (cell == null)
            {
                return false;
            }

            if (!ignoreDisabled && !cell.IsEnabled)
            {
                return false;
            }

            if (!ignoreWalkability && !cell.IsWalkable)
            {
                return false;
            }

            bool checkOccupied = Config == null ||
                                 Config.PassabilityMode == CellPassabilityMode.WalkableEnabledAndUnoccupied;
            if (checkOccupied && !ignoreOccupied && cell.IsOccupied)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns neighbor cells for a source cell.
        /// </summary>
        /// <param name="cell">Source cell.</param>
        /// <param name="directions">Optional movement rule override.</param>
        /// <returns>Neighbor list.</returns>
        public List<FieldCell> GetNeighbors(FieldCell cell, IEnumerable<Vector3Int> directions = null)
        {
            List<FieldCell> neighbors = new();
            CollectNeighbors(cell, neighbors, directions);
            return neighbors;
        }

        /// <summary>
        ///     Non-allocating variant of <see cref="GetNeighbors(FieldCell,IEnumerable{Vector3Int})" />: clears
        ///     <paramref name="results" /> and fills it with the existing neighbor cells.
        /// </summary>
        /// <remarks>
        ///     Neighbors are written in direction order, exactly as
        ///     <see cref="GetNeighbors(FieldCell,IEnumerable{Vector3Int})" /> returns them; offsets that leave the board
        ///     are skipped. A list or array of directions is indexed, so a warm call with a reused
        ///     <paramref name="results" /> list does not allocate. Match3 swap search relies on this order and
        ///     allocation contract.
        /// </remarks>
        /// <param name="cell">Source cell.</param>
        /// <param name="results">Caller-owned output list; reuse it between calls.</param>
        /// <param name="directions">Optional movement rule override; the configured movement rule when null.</param>
        /// <returns>Number of neighbors written.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is null.</exception>
        public int CollectNeighbors(FieldCell cell, List<FieldCell> results, IEnumerable<Vector3Int> directions = null)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (cell == null || Config == null)
            {
                return 0;
            }

            IEnumerable<Vector3Int> dirs =
                directions ?? (Config.MovementRule != null ? Config.MovementRule.Directions : null);
            if (dirs == null)
            {
                return 0;
            }

            if (dirs is IReadOnlyList<Vector3Int> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AddNeighbor(cell.Position, list[i], results);
                }
            }
            else
            {
                foreach (Vector3Int dir in dirs)
                {
                    AddNeighbor(cell.Position, dir, results);
                }
            }

            return results.Count;
        }

        private void AddNeighbor(Vector3Int origin, Vector3Int direction, List<FieldCell> results)
        {
            FieldCell neighbor = GetCell(origin + direction);
            if (neighbor != null)
            {
                results.Add(neighbor);
            }
        }

        /// <summary>
        ///     Returns neighbors for cell position.
        /// </summary>
        public List<FieldCell> GetNeighbors(Vector3Int pos, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(GetCell(pos), directions);
        }

        /// <summary>
        ///     Returns neighbors for 2D coordinates.
        /// </summary>
        public List<FieldCell> GetNeighbors(int x, int y, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(new Vector3Int(x, y, 0), directions);
        }

        /// <summary>
        ///     Returns neighbors for 2D vector.
        /// </summary>
        public List<FieldCell> GetNeighbors(Vector2Int pos, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(new Vector3Int(pos.x, pos.y, 0), directions);
        }

        /// <summary>
        ///     Finds a path from start to end using current passability settings.
        /// </summary>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <param name="directions">Optional movement rule override.</param>
        /// <returns>Path cells, or an empty list when no path exists.</returns>
        public List<FieldCell> FindPath(Vector3Int start, Vector3Int end, IEnumerable<Vector3Int> directions = null)
        {
            GridPathRequest request = new()
            {
                Start = start,
                End = end,
                Directions = directions
            };

            GridPathResult result = GridPathfinder.FindPath(this, request);
            return result.Path;
        }

        /// <summary>
        ///     Finds path using cell references.
        /// </summary>
        public List<FieldCell> FindPath(FieldCell start, FieldCell end, IEnumerable<Vector3Int> directions = null)
        {
            return start != null && end != null
                ? FindPath(start.Position, end.Position, directions)
                : new List<FieldCell>();
        }

        /// <summary>
        ///     Finds path using 2D coordinates.
        /// </summary>
        public List<FieldCell> FindPath(Vector2Int start, Vector2Int end, IEnumerable<Vector3Int> directions = null)
        {
            return FindPath(new Vector3Int(start.x, start.y, 0), new Vector3Int(end.x, end.y, 0), directions);
        }

        /// <summary>
        ///     Finds a path and returns detailed reason metadata.
        /// </summary>
        public GridPathResult FindPathDetailed(GridPathRequest request)
        {
            return GridPathfinder.FindPath(this, request);
        }

        /// <summary>
        ///     Finds a path with direct request arguments and diagnostic output.
        /// </summary>
        public GridPathResult FindPathDetailed(
            Vector3Int start,
            Vector3Int end,
            IEnumerable<Vector3Int> directions = null,
            bool ignoreOccupied = false,
            bool ignoreDisabled = false,
            bool ignoreWalkability = false)
        {
            GridPathRequest request = new()
            {
                Start = start,
                End = end,
                Directions = directions,
                IgnoreOccupied = ignoreOccupied,
                IgnoreDisabled = ignoreDisabled,
                IgnoreWalkability = ignoreWalkability
            };

            return FindPathDetailed(request);
        }

        /// <summary>
        ///     Converts logical cell coordinates to Unity Grid coordinates with configured origin.
        /// </summary>
        /// <param name="logicalPos">Logical coordinates in field array space.</param>
        /// <returns>Unity Grid cell coordinates.</returns>
        public Vector3Int LogicalToGridPosition(Vector3Int logicalPos)
        {
            return logicalPos + GetGridOriginOffset();
        }

        /// <summary>
        ///     Converts Unity Grid coordinates to logical field coordinates.
        /// </summary>
        /// <param name="gridPos">Unity Grid cell coordinates.</param>
        /// <returns>Logical field coordinates.</returns>
        public Vector3Int GridToLogicalPosition(Vector3Int gridPos)
        {
            return gridPos - GetGridOriginOffset();
        }

        /// <summary>
        ///     Returns world position of cell center.
        /// </summary>
        /// <param name="cellPos">Cell coordinates.</param>
        /// <returns>World-space center point.</returns>
        public Vector3 GetCellWorldCenter(Vector3Int cellPos)
        {
            if (UnityGrid == null)
            {
                return Vector3.zero;
            }

            Vector3Int gridPos = LogicalToGridPosition(cellPos);
            return UnityGrid.GetCellCenterWorld(gridPos) + GetOriginWorldCorrection();
        }

        /// <summary>
        ///     Returns world-space cell corner position.
        /// </summary>
        public Vector3 GetCellCornerWorld(Vector3Int cellPos)
        {
            if (UnityGrid == null)
            {
                return Vector3.zero;
            }

            Vector3Int gridPos = LogicalToGridPosition(cellPos);
            return UnityGrid.CellToWorld(gridPos) + GetOriginWorldCorrection();
        }

        /// <summary>
        ///     Returns world-space cell corner position for 2D coordinates.
        /// </summary>
        public Vector3 GetCellCornerWorld(Vector2Int cellPos)
        {
            return GetCellCornerWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
        }

        internal static bool TryAddCoordinates(Vector3Int anchor, Vector3Int offset, out Vector3Int position)
        {
            // WHY: widen before addition: two invalid Int32 coordinates can otherwise wrap into a valid cell.
            long x = (long)anchor.x + offset.x;
            long y = (long)anchor.y + offset.y;
            long z = (long)anchor.z + offset.z;
            if (x < int.MinValue || x > int.MaxValue ||
                y < int.MinValue || y > int.MaxValue ||
                z < int.MinValue || z > int.MaxValue)
            {
                position = default;
                return false;
            }

            position = new Vector3Int((int)x, (int)y, (int)z);
            return true;
        }

        private bool ValidateContentFootprint(
            Vector3Int anchor,
            IEnumerable<GridPlacementEntry> entries,
            bool requireEnabled,
            bool requireWalkable,
            bool requireUnoccupied,
            PlacementScratch scratch)
        {
            if (entries == null)
            {
                return false;
            }

            if (entries is IReadOnlyList<GridPlacementEntry> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (!TryResolveFootprintEntry(anchor, list[i], requireEnabled, requireWalkable, requireUnoccupied,
                            scratch))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (GridPlacementEntry entry in entries)
                {
                    if (!TryResolveFootprintEntry(anchor, entry, requireEnabled, requireWalkable, requireUnoccupied,
                            scratch))
                    {
                        return false;
                    }
                }
            }

            return scratch.Resolved.Count > 0;
        }

        private bool TryResolveFootprintEntry(
            Vector3Int anchor,
            GridPlacementEntry entry,
            bool requireEnabled,
            bool requireWalkable,
            bool requireUnoccupied,
            PlacementScratch scratch)
        {
            if (entry == null)
            {
                return false;
            }

            if (!TryAddCoordinates(anchor, entry.Offset, out Vector3Int position))
            {
                return false;
            }

            if (!scratch.Positions.Add(position))
            {
                return false;
            }

            FieldCell cell = GetCell(position);
            if (cell == null)
            {
                return false;
            }

            if (requireEnabled && !cell.IsEnabled)
            {
                return false;
            }

            if (requireWalkable && !cell.IsWalkable)
            {
                return false;
            }

            if (requireUnoccupied && cell.IsOccupied)
            {
                return false;
            }

            // WHY: the entry values are captured at validation time, so the write phase cannot observe a shared or
            // mutated entry instance.
            scratch.Resolved.Add(new GridPlacementResolvedEntry(cell, entry.ContentId, entry.OccupiesCell));
            return true;
        }

        private PlacementScratch AcquirePlacementScratch()
        {
            PlacementScratch scratch = _placementScratch;
            if (scratch == null)
            {
                scratch = new PlacementScratch();
                _placementScratch = scratch;
            }
            else if (scratch.InUse)
            {
                scratch = new PlacementScratch();
            }

            scratch.InUse = true;
            return scratch;
        }

        private static void ReleasePlacementScratch(PlacementScratch scratch)
        {
            scratch.Positions.Clear();
            scratch.Resolved.Clear();
            scratch.InUse = false;
        }

        private void DrawFieldGizmos()
        {
            FieldGizmoSettings settings = GizmoSettings;
            if (!DebugEnabled || settings == null || !settings.DrawGizmos || Cells == null || Config == null ||
                UnityGrid == null)
            {
                return;
            }

            Vector3Int size = GeneratedSize;
            if (settings.DrawGrid)
            {
                Gizmos.color = settings.GridColor;
                for (int x = 0; x <= size.x; x++)
                {
                    Vector3 from = GetCellCornerWorld(new Vector3Int(x, 0, 0));
                    Vector3 to = GetCellCornerWorld(new Vector3Int(x, size.y, 0));
                    Gizmos.DrawLine(from, to);
                }

                for (int y = 0; y <= size.y; y++)
                {
                    Vector3 from = GetCellCornerWorld(new Vector3Int(0, y, 0));
                    Vector3 to = GetCellCornerWorld(new Vector3Int(size.x, y, 0));
                    Gizmos.DrawLine(from, to);
                }
            }

            if (settings.DrawCells)
            {
                Vector3 fillSize = UnityGrid.cellSize * Mathf.Clamp01(settings.CellFillScale);
#if UNITY_EDITOR
                GUIStyle coordinateStyle = null;
                string[] labels = null;
                if (settings.DrawCoordinates)
                {
                    coordinateStyle = new GUIStyle
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = settings.TextFontSize
                    };
                    coordinateStyle.normal.textColor = settings.TextColor;
                    labels = GetGizmoLabels();
                }
#endif
                int labelIndex = -1;
                for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                for (int z = 0; z < size.z; z++)
                {
                    labelIndex++;
                    FieldCell cell = Cells[x, y, z];
                    if (cell == null)
                    {
                        continue;
                    }

                    Vector3 pos = GetCellWorldCenter(cell.Position);
                    Gizmos.color = ResolveGizmoCellColor(cell, settings);
                    Gizmos.DrawCube(pos, fillSize);

#if UNITY_EDITOR
                    if (labels != null)
                    {
                        Handles.Label(pos, labels[labelIndex], coordinateStyle);
                    }
#endif
                }
            }

            if (settings.DrawPath && settings.DebugPath != null && settings.DebugPath.Count > 1)
            {
                Gizmos.color = settings.PathColor;
                for (int i = 1; i < settings.DebugPath.Count; i++)
                {
                    Vector3 a = GetCellWorldCenter(settings.DebugPath[i - 1]);
                    Vector3 b = GetCellWorldCenter(settings.DebugPath[i]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }

#if UNITY_EDITOR
        // WHY: building one coordinate string per cell on every Scene repaint allocated thousands of strings per frame on
        // large boards; labels only change when the field is regenerated.
        private string[] GetGizmoLabels()
        {
            FieldCell[,,] cells = Cells;
            if (_gizmoLabels != null && ReferenceEquals(_gizmoLabelCells, cells))
            {
                return _gizmoLabels;
            }

            Vector3Int size = GeneratedSize;
            string[] labels = new string[cells.Length];
            int index = 0;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = cells[x, y, z];
                Vector3Int position = cell != null ? cell.Position : new Vector3Int(x, y, z);
                labels[index++] = $"x:{position.x} y:{position.y} z:{position.z}";
            }

            _gizmoLabels = labels;
            _gizmoLabelCells = cells;
            return labels;
        }
#endif

        private static Color ResolveGizmoCellColor(FieldCell cell, FieldGizmoSettings settings)
        {
            if (!cell.IsEnabled)
            {
                return settings.DisabledCellColor;
            }

            if (cell.IsOccupied)
            {
                return settings.OccupiedCellColor;
            }

            return cell.IsWalkable ? settings.WalkableCellColor : settings.BlockedCellColor;
        }

        private bool ResolveCellEnabled(Vector3Int pos)
        {
            bool enabled = ResolveBaseShapeEnabled(pos);
            GridShapeMask shapeMask = Config != null ? Config.ShapeMask : null;
            if (shapeMask != null)
            {
                enabled = shapeMask.EvaluateCell(enabled, pos, Config.GridType);
            }

            return enabled;
        }

        private bool ResolveBaseShapeEnabled(Vector3Int pos)
        {
            if (Config == null)
            {
                return true;
            }

            switch (Config.GridType)
            {
                case GridType.Rectangular:
                    return true;
                case GridType.Custom:
                    return false;
                case GridType.Hexagonal:
                {
                    Vector3Int size = GeneratedSize;
                    int radius = Mathf.Max(1, Mathf.Min(size.x, size.y) / 2);
                    int q = pos.x - size.x / 2;
                    int r = pos.y - size.y / 2;
                    return Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(q + r) <= radius * 2;
                }
                default:
                    return true;
            }
        }

        private void ApplyStateOverrides()
        {
            if (Config == null)
            {
                return;
            }

            foreach (Vector3Int pos in Config.DisabledCells)
            {
                FieldCell cell = GetCell(pos);
                if (cell == null)
                {
                    continue;
                }

                cell.IsEnabled = false;
                cell.IsWalkable = false;
                cell.IsOccupied = false;
            }

            foreach (Vector3Int pos in Config.ForcedEnabledCells)
            {
                FieldCell cell = GetCell(pos);
                if (cell == null)
                {
                    continue;
                }

                cell.IsEnabled = true;
            }

            foreach (Vector3Int pos in Config.BlockedCells)
            {
                FieldCell cell = GetCell(pos);
                if (cell == null)
                {
                    continue;
                }

                cell.IsWalkable = false;
            }

            foreach (Vector3Int pos in Config.ForcedWalkableCells)
            {
                FieldCell cell = GetCell(pos);
                if (cell == null)
                {
                    continue;
                }

                if (cell.IsEnabled)
                {
                    cell.IsWalkable = true;
                }
            }
        }

        private Vector3Int GetGridOriginOffset()
        {
            if (Config == null)
            {
                return Vector3Int.zero;
            }

            Vector3Int size = GeneratedSize;
            int offsetX = GetAxisOffset(size.x, GetOriginX());
            int offsetY = GetAxisOffset(size.y, GetOriginY());
            int offsetZ = GetAxisOffset(size.z, GetOriginZ());

            return new Vector3Int(offsetX, offsetY, offsetZ) + Config.OriginOffset;
        }

        private Vector3 GetOriginWorldCorrection()
        {
            if (Config == null || UnityGrid == null)
            {
                return Vector3.zero;
            }

            Vector3Int size = GeneratedSize;
            Vector3 localCorrection = new Vector3(
                GetCenterAxisCorrection(size.x, GetOriginX()) * UnityGrid.cellSize.x,
                GetCenterAxisCorrection(size.y, GetOriginY()) * UnityGrid.cellSize.y,
                GetCenterAxisCorrection(size.z, GetOriginZ()) * UnityGrid.cellSize.z);

            return UnityGrid.transform.TransformVector(localCorrection);
        }

        private static float GetCenterAxisCorrection(int size, int anchor)
        {
            return anchor == 0 && size > 0 && size % 2 != 0 ? -0.5f : 0f;
        }

        private int GetAxisOffset(int size, int anchor)
        {
            if (size <= 0)
            {
                return 0;
            }

            switch (anchor)
            {
                case -1:
                    return 0;
                case 0:
                    return -(size / 2);
                case 1:
                    return -(size - 1);
                default:
                    return 0;
            }
        }

        private int GetOriginX()
        {
            if (Config == null)
            {
                return 0;
            }

            switch (Config.Origin2D)
            {
                case GridOrigin2D.BottomLeft:
                case GridOrigin2D.MiddleLeft:
                case GridOrigin2D.TopLeft:
                    return -1;
                case GridOrigin2D.BottomCenter:
                case GridOrigin2D.Center:
                case GridOrigin2D.TopCenter:
                    return 0;
                case GridOrigin2D.BottomRight:
                case GridOrigin2D.MiddleRight:
                case GridOrigin2D.TopRight:
                    return 1;
                default:
                    return 0;
            }
        }

        private int GetOriginY()
        {
            if (Config == null)
            {
                return 0;
            }

            switch (Config.Origin2D)
            {
                case GridOrigin2D.BottomLeft:
                case GridOrigin2D.BottomCenter:
                case GridOrigin2D.BottomRight:
                    return -1;
                case GridOrigin2D.MiddleLeft:
                case GridOrigin2D.Center:
                case GridOrigin2D.MiddleRight:
                    return 0;
                case GridOrigin2D.TopLeft:
                case GridOrigin2D.TopCenter:
                case GridOrigin2D.TopRight:
                    return 1;
                default:
                    return 0;
            }
        }

        private int GetOriginZ()
        {
            if (Config == null)
            {
                return 0;
            }

            switch (Config.OriginDepth)
            {
                case GridOriginDepth.Front:
                    return -1;
                case GridOriginDepth.Center:
                    return 0;
                case GridOriginDepth.Back:
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>
        ///     UnityEvent wrapper carrying changed <see cref="FieldCell" />.
        /// </summary>
        [Serializable]
        public class CellChangedEvent : UnityEvent<FieldCell>
        {
        }

        /// <summary>
        ///     UnityEvent wrapper for cell state changes.
        /// </summary>
        [Serializable]
        public class CellStateChangedEvent : UnityEvent<FieldCell>
        {
        }

        private readonly struct GridPlacementResolvedEntry
        {
            public readonly FieldCell Cell;
            public readonly int ContentId;
            public readonly bool OccupiesCell;

            public GridPlacementResolvedEntry(FieldCell cell, int contentId, bool occupiesCell)
            {
                Cell = cell;
                ContentId = contentId;
                OccupiesCell = occupiesCell;
            }
        }

        private sealed class PlacementScratch
        {
            public readonly HashSet<Vector3Int> Positions = new HashSet<Vector3Int>();
            public readonly List<GridPlacementResolvedEntry> Resolved = new List<GridPlacementResolvedEntry>();
            public bool InUse;
        }
    }
}
