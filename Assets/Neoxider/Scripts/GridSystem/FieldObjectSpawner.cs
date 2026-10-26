using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Runtime info about a spawned object bound to a cell.
    /// </summary>
    public class SpawnedObjectInfo
    {
        /// <summary>
        ///     Cell where object is registered. It may belong to an earlier field generation.
        /// </summary>
        public FieldCell Cell;

        /// <summary>
        ///     Spawned GameObject instance.
        /// </summary>
        public GameObject GameObject;

        /// <summary>
        ///     True if object blocks cell occupancy.
        /// </summary>
        public bool OccupiesSpace;

        /// <summary>
        ///     Creates spawned object metadata container.
        /// </summary>
        /// <param name="go">Spawned object instance.</param>
        /// <param name="cell">Target grid cell.</param>
        /// <param name="occupiesSpace">Whether object should occupy this cell.</param>
        public SpawnedObjectInfo(GameObject go, FieldCell cell, bool occupiesSpace)
        {
            GameObject = go;
            Cell = cell;
            OccupiesSpace = occupiesSpace;
        }
    }

    /// <summary>
    ///     Spawner with per-cell object tracking and occupancy support.
    /// </summary>
    [NeoDoc("GridSystem/FieldObjectSpawner.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/FieldObjectSpawner")]
    [AddComponentMenu("Neoxider/" + "GridSystem/" + nameof(FieldObjectSpawner))]
    public class FieldObjectSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject[] Prefabs;

        /// <summary>
        ///     Raised when a new object is spawned.
        /// </summary>
        public UnityEvent<SpawnedObjectInfo> OnObjectSpawned = new();

        /// <summary>
        ///     Raised after object is removed.
        /// </summary>
        public UnityEvent<SpawnedObjectInfo> OnObjectRemoved = new();

        /// <summary>
        ///     Raised when cell becomes occupied.
        /// </summary>
        public UnityEvent<FieldCell> OnCellOccupied = new();

        /// <summary>
        ///     Raised when cell is no longer occupied.
        /// </summary>
        public UnityEvent<FieldCell> OnCellFreed = new();

        private readonly Dictionary<FieldCell, List<SpawnedObjectInfo>> cellObjects = new();

        private readonly Dictionary<GameObject, SpawnedObjectInfo> objectLookup = new();

        private FieldGenerator generator;

        private FieldGenerator Generator
        {
            get
            {
                if (generator == null)
                {
                    generator = GetComponent<FieldGenerator>();
                }

                return generator;
            }
        }

        private void Awake()
        {
            generator = GetComponent<FieldGenerator>();
        }

        /// <summary>
        ///     Spawns prefab into target cell and registers tracking metadata.
        /// </summary>
        /// <param name="cellPos">Target cell position.</param>
        /// <param name="prefabIndex">Index in <see cref="Prefabs" /> array.</param>
        /// <param name="occupiesSpace">Whether spawned object marks cell occupied.</param>
        /// <param name="layer">Reserved for compatibility.</param>
        /// <returns>Spawn metadata or null when spawn fails.</returns>
        public SpawnedObjectInfo SpawnAt(Vector3Int cellPos, int prefabIndex = 0, bool occupiesSpace = true,
            string layer = "Default")
        {
            FieldCell cell = Generator != null ? Generator.GetCell(cellPos) : null;
            if (cell == null || Prefabs == null || prefabIndex < 0 || prefabIndex >= Prefabs.Length ||
                Prefabs[prefabIndex] == null)
            {
                return null;
            }

            bool wasOccupiedAtEntry = cell.IsOccupied;
            Vector3 worldPos = Generator.GetCellWorldCenter(cell.Position);
            GameObject go = Instantiate(Prefabs[prefabIndex], worldPos, Quaternion.identity, transform);
            SpawnedObjectInfo info = new(go, cell, occupiesSpace);
            // Preserve the legacy first-registration notification on an already occupied cell.
            bool notifyExistingOccupancy = wasOccupiedAtEntry && !HasBlockingObjects(cell);
            if (!cellObjects.ContainsKey(cell))
            {
                cellObjects[cell] = new List<SpawnedObjectInfo>();
            }

            cellObjects[cell].Add(info);
            objectLookup[go] = info;
            OnObjectSpawned.Invoke(info);
            // Another blocker may still be inside its spawn callback and not have published occupancy yet.
            if (occupiesSpace && info.OccupiesSpace && IsCurrentRegistration(go, info, cell) &&
                (!cell.IsOccupied || (notifyExistingOccupancy && !HasBlockingObjects(cell, info))))
            {
                generator.SetOccupied(cell.Position, true);
                if (IsCurrentRegistration(go, info, cell) && cell.IsOccupied)
                {
                    OnCellOccupied.Invoke(cell);
                }
            }

            return info;
        }

        /// <summary>
        ///     Returns all tracked objects assigned to a cell.
        /// </summary>
        /// <param name="cellPos">Cell position.</param>
        /// <returns>Copy of object list for that cell.</returns>
        public List<SpawnedObjectInfo> GetObjectsInCell(Vector3Int cellPos)
        {
            FieldCell cell = Generator != null ? Generator.GetCell(cellPos) : null;
            if (cell == null || !cellObjects.ContainsKey(cell))
            {
                return new List<SpawnedObjectInfo>();
            }

            return new List<SpawnedObjectInfo>(cellObjects[cell]);
        }

        /// <summary>
        ///     Checks whether a cell is occupied by at least one blocking object.
        /// </summary>
        /// <param name="cellPos">Cell position.</param>
        /// <returns>True when occupied; otherwise false.</returns>
        public bool IsCellOccupied(Vector3Int cellPos)
        {
            FieldCell cell = Generator != null ? Generator.GetCell(cellPos) : null;
            if (cell == null)
            {
                return false;
            }

            if (cell.IsOccupied)
            {
                return true;
            }

            if (!cellObjects.ContainsKey(cell))
            {
                return false;
            }

            return cellObjects[cell].Exists(o => o.OccupiesSpace);
        }

        /// <summary>
        ///     Removes object from tracking and destroys it.
        ///     Detached registrations never change occupancy in a replacement field generation.
        /// </summary>
        /// <param name="go">Tracked object instance.</param>
        public void RemoveObject(GameObject go)
        {
            if (ReferenceEquals(go, null) || !objectLookup.TryGetValue(go, out SpawnedObjectInfo info))
            {
                return;
            }

            FieldCell cell = info.Cell;
            bool occupiesSpace = info.OccupiesSpace;
            if (cell != null && cellObjects.TryGetValue(cell, out List<SpawnedObjectInfo> objects))
            {
                objects.Remove(info);
                if (objects.Count == 0)
                {
                    cellObjects.Remove(cell);
                }
            }

            objectLookup.Remove(go);
            Destroy(go);
            OnObjectRemoved.Invoke(info);
            if (occupiesSpace && IsCurrentCell(cell) && !HasBlockingObjects(cell))
            {
                generator.SetOccupied(cell.Position, false);
                if (IsCurrentCell(cell) && !cell.IsOccupied && !HasBlockingObjects(cell))
                {
                    OnCellFreed.Invoke(cell);
                }
            }
        }

        private bool IsCurrentCell(FieldCell cell)
        {
            return cell != null && Generator != null && ReferenceEquals(generator.GetCell(cell.Position), cell);
        }

        private bool IsCurrentRegistration(GameObject go, SpawnedObjectInfo info, FieldCell cell)
        {
            return IsCurrentCell(cell) && ReferenceEquals(info.Cell, cell) &&
                   objectLookup.TryGetValue(go, out SpawnedObjectInfo registered) && ReferenceEquals(registered, info) &&
                   cellObjects.TryGetValue(cell, out List<SpawnedObjectInfo> objects) && objects.Contains(info);
        }

        private bool HasBlockingObjects(FieldCell cell, SpawnedObjectInfo except = null)
        {
            return cell != null && cellObjects.TryGetValue(cell, out List<SpawnedObjectInfo> objects) &&
                   objects.Exists(info => !ReferenceEquals(info, except) && info.OccupiesSpace);
        }

        /// <summary>
        ///     Returns all tracked objects across all cells.
        /// </summary>
        /// <returns>Flat list of tracked objects.</returns>
        public List<SpawnedObjectInfo> GetAllObjects()
        {
            List<SpawnedObjectInfo> all = new();
            foreach (List<SpawnedObjectInfo> list in cellObjects.Values)
            {
                all.AddRange(list);
            }

            return all;
        }
    }
}
