using System;
using System.Collections.Generic;
using Neo;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    /// Generates and manages a runtime grid field with shape, state and pathfinding helpers.
    /// </summary>
    [NeoDoc("GridSystem/FieldGenerator.md")]
    [RequireComponent(typeof(Grid))]
    [CreateFromMenu("Neoxider/GridSystem/FieldGenerator")]
    [AddComponentMenu("Neoxider/" + "GridSystem/" + nameof(FieldGenerator))]
    public class FieldGenerator : MonoBehaviour
    {
        [Header("Settings")] public FieldGeneratorConfig Config = new();

        [Header("Debug")] public bool DebugEnabled = true;

        /// <summary>
        /// Invoked after field generation is completed.
        /// </summary>
        public UnityEvent OnFieldGenerated = new();

        /// <summary>
        /// Invoked when legacy cell data is changed via <see cref="SetCell(Vector3Int,int,bool)"/>.
        /// </summary>
        public CellChangedEvent OnCellChanged = new();

        /// <summary>
        /// Invoked when cell runtime state is changed.
        /// </summary>
        public CellStateChangedEvent OnCellStateChanged = new();

        private Grid unityGrid;

        /// <summary>
        /// Global singleton reference to the latest initialized generator.
        /// </summary>
        public static FieldGenerator I { get; private set; }

        /// <summary>
        /// Backing 3D cell array.
        /// </summary>
        public FieldCell[,,] Cells { get; private set; }

        /// <summary>
        /// Returns a 2D projection (z=0) of the current grid.
        /// </summary>
        public FieldCell[,] Cells2D
        {
            get
            {
                Vector3Int size = Config != null ? Config.Size : Vector3Int.zero;
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
        /// Cached Unity <see cref="Grid"/> component used for world-grid conversion.
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

        /// <summary>
        /// Generates grid cells using the current or provided configuration.
        /// </summary>
        /// <param name="config">Optional config override.</param>
        public void GenerateField(FieldGeneratorConfig config = null)
        {
            if (config != null)
            {
                Config = config;
            }

            Vector3Int size = Config != null ? Config.Size : Vector3Int.one;
            if (size.x <= 0 || size.y <= 0 || size.z <= 0)
            {
                Debug.LogError("FieldGenerator: Некорректный размер поля! Size: " + size);
                Cells = null;
                return;
            }

            Cells = new FieldCell[size.x, size.y, size.z];
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
        /// Reapplies current shape mask and state overrides to existing cells.
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
        /// Gets cell by 3D position.
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
        /// Gets cell by 2D coordinates (z=0).
        /// </summary>
        public FieldCell GetCell(int x, int y)
        {
            return GetCell(new Vector3Int(x, y, 0));
        }

        /// <summary>
        /// Gets cell by <see cref="Vector2Int"/> (z=0).
        /// </summary>
        public FieldCell GetCell(Vector2Int pos)
        {
            return GetCell(new Vector3Int(pos.x, pos.y, 0));
        }

        /// <summary>
        /// Gets cell from world position via Unity Grid conversion.
        /// </summary>
        /// <param name="worldPosition">World-space position.</param>
        /// <returns>Cell instance or null.</returns>
        public FieldCell GetCellFromWorld(Vector3 worldPosition)
        {
            if (UnityGrid == null)
            {
                return null;
            }

            Vector3Int gridPos = UnityGrid.WorldToCell(worldPosition);
            Vector3Int logicalPos = GridToLogicalPosition(gridPos);
            return GetCell(logicalPos);
        }

        /// <summary>
        /// Enumerates all cells in backing array.
        /// </summary>
        /// <param name="includeDisabled">Include disabled cells when true.</param>
        /// <returns>Cell sequence.</returns>
        public IEnumerable<FieldCell> GetAllCells(bool includeDisabled = true)
        {
            if (Cells == null || Config == null)
            {
                yield break;
            }

            Vector3Int size = Config.Size;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = Cells[x, y, z];
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
        /// Checks whether 3D position is inside configured bounds.
        /// </summary>
        public bool InBounds(Vector3Int pos)
        {
            Vector3Int s = Config != null ? Config.Size : Vector3Int.zero;
            return pos.x >= 0 && pos.x < s.x && pos.y >= 0 && pos.y < s.y && pos.z >= 0 && pos.z < s.z;
        }

        /// <summary>
        /// Checks whether 2D coordinates (z=0) are inside configured bounds.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return InBounds(new Vector3Int(x, y, 0));
        }

        /// <summary>
        /// Checks whether 2D vector (z=0) is inside configured bounds.
        /// </summary>
        public bool InBounds(Vector2Int pos)
        {
            return InBounds(new Vector3Int(pos.x, pos.y, 0));
        }

        /// <summary>
        /// Sets legacy cell type and walkability.
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
        /// Sets legacy cell type and walkability using 2D coordinates.
        /// </summary>
        public void SetCell(int x, int y, int type, bool isWalkable)
        {
            SetCell(new Vector3Int(x, y, 0), type, isWalkable);
        }

        /// <summary>
        /// Sets legacy cell type and walkability using 2D vector.
        /// </summary>
        public void SetCell(Vector2Int pos, int type, bool isWalkable)
        {
            SetCell(new Vector3Int(pos.x, pos.y, 0), type, isWalkable);
        }

        /// <summary>
        /// Sets walkability for a cell.
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
        /// Enables or disables a cell in active board shape.
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
        /// Marks a cell as occupied or free.
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
        /// Sets content state id for a cell.
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
        /// Evaluates whether cell is passable under current passability settings.
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
        /// Returns neighbor cells for a source cell.
        /// </summary>
        /// <param name="cell">Source cell.</param>
        /// <param name="directions">Optional movement rule override.</param>
        /// <returns>Neighbor list.</returns>
        public List<FieldCell> GetNeighbors(FieldCell cell, IEnumerable<Vector3Int> directions = null)
        {
            List<FieldCell> neighbors = new();
            if (cell == null || Config == null)
            {
                return neighbors;
            }

            IEnumerable<Vector3Int> dirs =
                directions ?? (Config.MovementRule != null ? Config.MovementRule.Directions : null);
            if (dirs == null)
            {
                return neighbors;
            }

            foreach (Vector3Int dir in dirs)
            {
                Vector3Int np = cell.Position + dir;
                FieldCell ncell = GetCell(np);
                if (ncell != null)
                {
                    neighbors.Add(ncell);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Returns neighbors for cell position.
        /// </summary>
        public List<FieldCell> GetNeighbors(Vector3Int pos, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(GetCell(pos), directions);
        }

        /// <summary>
        /// Returns neighbors for 2D coordinates.
        /// </summary>
        public List<FieldCell> GetNeighbors(int x, int y, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(new Vector3Int(x, y, 0), directions);
        }

        /// <summary>
        /// Returns neighbors for 2D vector.
        /// </summary>
        public List<FieldCell> GetNeighbors(Vector2Int pos, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(new Vector3Int(pos.x, pos.y, 0), directions);
        }

        /// <summary>
        /// Finds a path from start to end using current passability settings.
        /// </summary>
        /// <param name="start">Start position.</param>
        /// <param name="end">End position.</param>
        /// <param name="directions">Optional movement rule override.</param>
        /// <returns>Path cells, or null when path does not exist.</returns>
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
        /// Finds path using cell references.
        /// </summary>
        public List<FieldCell> FindPath(FieldCell start, FieldCell end, IEnumerable<Vector3Int> directions = null)
        {
            return FindPath(start != null ? start.Position : Vector3Int.zero,
                end != null ? end.Position : Vector3Int.zero, directions);
        }

        /// <summary>
        /// Finds path using 2D coordinates.
        /// </summary>
        public List<FieldCell> FindPath(Vector2Int start, Vector2Int end, IEnumerable<Vector3Int> directions = null)
        {
            return FindPath(new Vector3Int(start.x, start.y, 0), new Vector3Int(end.x, end.y, 0), directions);
        }

        /// <summary>
        /// Finds a path and returns detailed reason metadata.
        /// </summary>
        public GridPathResult FindPathDetailed(GridPathRequest request)
        {
            return GridPathfinder.FindPath(this, request);
        }

        /// <summary>
        /// Finds a path with direct request arguments and diagnostic output.
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
        /// Converts logical cell coordinates to Unity Grid coordinates with configured origin.
        /// </summary>
        /// <param name="logicalPos">Logical coordinates in field array space.</param>
        /// <returns>Unity Grid cell coordinates.</returns>
        public Vector3Int LogicalToGridPosition(Vector3Int logicalPos)
        {
            return logicalPos + GetGridOriginOffset();
        }

        /// <summary>
        /// Converts Unity Grid coordinates to logical field coordinates.
        /// </summary>
        /// <param name="gridPos">Unity Grid cell coordinates.</param>
        /// <returns>Logical field coordinates.</returns>
        public Vector3Int GridToLogicalPosition(Vector3Int gridPos)
        {
            return gridPos - GetGridOriginOffset();
        }

        /// <summary>
        /// Returns world position of cell center.
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
            return UnityGrid.GetCellCenterWorld(gridPos);
        }

        /// <summary>
        /// Returns world-space cell corner position.
        /// </summary>
        public Vector3 GetCellCornerWorld(Vector3Int cellPos)
        {
            if (UnityGrid == null)
            {
                return Vector3.zero;
            }

            Vector3Int gridPos = LogicalToGridPosition(cellPos);
            return UnityGrid.CellToWorld(gridPos);
        }

        /// <summary>
        /// Returns world-space cell corner position for 2D coordinates.
        /// </summary>
        public Vector3 GetCellCornerWorld(Vector2Int cellPos)
        {
            return GetCellCornerWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
        }

        /// <summary>
        /// UnityEvent wrapper carrying changed <see cref="FieldCell"/>.
        /// </summary>
        [Serializable]
        public class CellChangedEvent : UnityEvent<FieldCell>
        {
        }

        /// <summary>
        /// UnityEvent wrapper for cell state changes.
        /// </summary>
        [Serializable]
        public class CellStateChangedEvent : UnityEvent<FieldCell>
        {
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
                    int radius = Mathf.Max(1, Mathf.Min(Config.Size.x, Config.Size.y) / 2);
                    int q = pos.x - Config.Size.x / 2;
                    int r = pos.y - Config.Size.y / 2;
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

            Vector3Int size = Config.Size;
            int offsetX = GetAxisOffset(size.x, GetOriginX());
            int offsetY = GetAxisOffset(size.y, GetOriginY());
            int offsetZ = GetAxisOffset(size.z, GetOriginZ());

            return new Vector3Int(offsetX, offsetY, offsetZ) + Config.OriginOffset;
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
    }
}
