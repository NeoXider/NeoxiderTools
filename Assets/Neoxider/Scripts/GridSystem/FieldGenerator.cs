using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Генератор и управляющий класс для универсального поля (2D/3D) на базе UnityEngine.Grid. Singleton.
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class FieldGenerator : MonoBehaviour
    {
        [Header("Конфиг поля")] public FieldGeneratorConfig Config = new();

        [Header("Debug/Визуализация")] public bool DebugEnabled = true;

        public UnityEvent OnFieldGenerated = new();

        public CellChangedEvent OnCellChanged = new();

        private Grid unityGrid;
        public static FieldGenerator I { get; private set; }

        public FieldCell[,,] Cells { get; private set; }

        /// <summary>
        ///     Удобный доступ к двумерному массиву (z=0)
        /// </summary>
        public FieldCell[,] Cells2D
        {
            get
            {
                var size = Config != null ? Config.Size : Vector3Int.zero;
                var arr = new FieldCell[size.x, size.y];
                if (Cells == null) return arr;
                for (var x = 0; x < size.x; x++)
                for (var y = 0; y < size.y; y++)
                    arr[x, y] = Cells[x, y, 0];
                return arr;
            }
        }

        public Grid UnityGrid
        {
            get
            {
                if (unityGrid == null)
                    unityGrid = GetComponent<Grid>();
                return unityGrid;
            }
        }

        private void Awake()
        {
            I = this;
            if (unityGrid == null)
                unityGrid = GetComponent<Grid>();
            if (Cells == null || Cells.Length == 0)
                GenerateField();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (unityGrid == null)
                    unityGrid = GetComponent<Grid>();
                GenerateField();
            }
        }

        /// <summary>
        ///     Генерирует поле по текущему или переданному конфигу
        /// </summary>
        public void GenerateField(FieldGeneratorConfig config = null)
        {
            if (config != null)
                Config = config;
            var size = Config != null ? Config.Size : Vector3Int.one;
            if (size.x <= 0 || size.y <= 0 || size.z <= 0)
            {
                Debug.LogError("FieldGenerator: Некорректный размер поля! Size: " + size);
                Cells = null;
                return;
            }

            Cells = new FieldCell[size.x, size.y, size.z];
            for (var x = 0; x < size.x; x++)
            for (var y = 0; y < size.y; y++)
            for (var z = 0; z < size.z; z++)
            {
                var pos = new Vector3Int(x, y, z);
                Cells[x, y, z] = new FieldCell(pos);
            }

            OnFieldGenerated?.Invoke();
        }

        // --- Получение ячеек ---
        public FieldCell GetCell(Vector3Int pos)
        {
            if (Cells == null) return null;
            if (InBounds(pos))
                return Cells[pos.x, pos.y, pos.z];
            return null;
        }

        public FieldCell GetCell(int x, int y)
        {
            return GetCell(new Vector3Int(x, y, 0));
        }

        public FieldCell GetCell(Vector2Int pos)
        {
            return GetCell(new Vector3Int(pos.x, pos.y, 0));
        }

        public FieldCell GetCellFromWorld(Vector3 worldPosition)
        {
            if (UnityGrid == null) return null;
            var gridPos = UnityGrid.WorldToCell(worldPosition);
            return GetCell(gridPos);
        }

        // --- Проверка границ ---
        public bool InBounds(Vector3Int pos)
        {
            var s = Config != null ? Config.Size : Vector3Int.zero;
            return pos.x >= 0 && pos.x < s.x && pos.y >= 0 && pos.y < s.y && pos.z >= 0 && pos.z < s.z;
        }

        public bool InBounds(int x, int y)
        {
            return InBounds(new Vector3Int(x, y, 0));
        }

        public bool InBounds(Vector2Int pos)
        {
            return InBounds(new Vector3Int(pos.x, pos.y, 0));
        }

        // --- Установка ячеек ---
        public void SetCell(Vector3Int pos, int type, bool isWalkable)
        {
            var cell = GetCell(pos);
            if (cell != null)
            {
                cell.Type = type;
                cell.IsWalkable = isWalkable;
                OnCellChanged?.Invoke(cell);
            }
        }

        public void SetCell(int x, int y, int type, bool isWalkable)
        {
            SetCell(new Vector3Int(x, y, 0), type, isWalkable);
        }

        public void SetCell(Vector2Int pos, int type, bool isWalkable)
        {
            SetCell(new Vector3Int(pos.x, pos.y, 0), type, isWalkable);
        }

        // --- Соседи ---
        public List<FieldCell> GetNeighbors(FieldCell cell, IEnumerable<Vector3Int> directions = null)
        {
            var neighbors = new List<FieldCell>();
            if (cell == null || Config == null) return neighbors;
            var dirs = directions ?? (Config.MovementRule != null ? Config.MovementRule.Directions : null);
            if (dirs == null) return neighbors;
            foreach (var dir in dirs)
            {
                var np = cell.Position + dir;
                var ncell = GetCell(np);
                if (ncell != null)
                    neighbors.Add(ncell);
            }

            return neighbors;
        }

        public List<FieldCell> GetNeighbors(Vector3Int pos, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(GetCell(pos), directions);
        }

        public List<FieldCell> GetNeighbors(int x, int y, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(new Vector3Int(x, y, 0), directions);
        }

        public List<FieldCell> GetNeighbors(Vector2Int pos, IEnumerable<Vector3Int> directions = null)
        {
            return GetNeighbors(new Vector3Int(pos.x, pos.y, 0), directions);
        }

        // --- Поиск пути ---
        public List<FieldCell> FindPath(Vector3Int start, Vector3Int end, IEnumerable<Vector3Int> directions = null)
        {
            if (Cells == null) return null;
            var queue = new Queue<FieldCell>();
            var visited = new HashSet<FieldCell>();
            var prev = new Dictionary<FieldCell, FieldCell>();
            var startCell = GetCell(start);
            var endCell = GetCell(end);
            if (startCell == null || endCell == null) return null;
            queue.Enqueue(startCell);
            visited.Add(startCell);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == endCell) break;
                foreach (var neighbor in GetNeighbors(current, directions))
                {
                    if (!neighbor.IsWalkable || visited.Contains(neighbor)) continue;
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    prev[neighbor] = current;
                }
            }

            if (!visited.Contains(endCell)) return null;
            var path = new List<FieldCell>();
            var at = endCell;
            while (at != null && prev.ContainsKey(at))
            {
                path.Add(at);
                at = prev[at];
            }

            path.Add(startCell);
            path.Reverse();
            return path;
        }

        public List<FieldCell> FindPath(FieldCell start, FieldCell end, IEnumerable<Vector3Int> directions = null)
        {
            return FindPath(start != null ? start.Position : Vector3Int.zero,
                end != null ? end.Position : Vector3Int.zero, directions);
        }

        public List<FieldCell> FindPath(Vector2Int start, Vector2Int end, IEnumerable<Vector3Int> directions = null)
        {
            return FindPath(new Vector3Int(start.x, start.y, 0), new Vector3Int(end.x, end.y, 0), directions);
        }

        /// <summary>
        ///     Получить мировую позицию центра ячейки через UnityEngine.Grid с учётом Origin
        /// </summary>
        public Vector3 GetCellWorldCenter(Vector3Int cellPos)
        {
            if (UnityGrid == null) return Vector3.zero;
            return UnityGrid.GetCellCenterWorld(cellPos);
        }

        public Vector3 GetCellCornerWorld(Vector3Int cellPos)
        {
            if (UnityGrid == null) return Vector3.zero;
            return UnityGrid.CellToWorld(cellPos);
        }

        public Vector3 GetCellCornerWorld(Vector2Int cellPos)
        {
            return GetCellCornerWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
        }

        [Serializable]
        public class CellChangedEvent : UnityEvent<FieldCell>
        {
        }
    }
}