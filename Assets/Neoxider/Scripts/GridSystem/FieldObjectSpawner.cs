using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Информация о заспавненном объекте на поле
    /// </summary>
    public class SpawnedObjectInfo
    {
        public FieldCell Cell;
        public GameObject GameObject;
        public bool OccupiesSpace;

        public SpawnedObjectInfo(GameObject go, FieldCell cell, bool occupiesSpace)
        {
            GameObject = go;
            Cell = cell;
            OccupiesSpace = occupiesSpace;
        }
    }

    /// <summary>
    ///     Универсальный спавнер для работы с сеткой. Хранит объекты по ячейкам, поддерживает флаг "занимает место".
    /// </summary>
    [RequireComponent(typeof(FieldGenerator))]
    public class FieldObjectSpawner : MonoBehaviour
    {
        [Header("Префабы для спавна")] public GameObject[] Prefabs;

        [Header("События")] public UnityEvent<SpawnedObjectInfo> OnObjectSpawned = new();
        public UnityEvent<SpawnedObjectInfo> OnObjectRemoved = new();
        public UnityEvent<FieldCell> OnCellOccupied = new();
        public UnityEvent<FieldCell> OnCellFreed = new();

        // Для каждой ячейки — список объектов
        private readonly Dictionary<FieldCell, List<SpawnedObjectInfo>> cellObjects = new();

        // Для быстрого поиска по объекту
        private readonly Dictionary<GameObject, SpawnedObjectInfo> objectLookup = new();

        private FieldGenerator generator;

        private void Awake()
        {
            generator = GetComponent<FieldGenerator>();
        }

        /// <summary>
        ///     Спавнит объект в ячейку
        /// </summary>
        public SpawnedObjectInfo SpawnAt(Vector3Int cellPos, int prefabIndex = 0, bool occupiesSpace = true,
            string layer = "Default")
        {
            var cell = generator.GetCell(cellPos);
            if (cell == null || Prefabs == null || prefabIndex < 0 || prefabIndex >= Prefabs.Length) return null;
            var worldPos = generator.GetCellWorldCenter(cell.Position);
            var go = Instantiate(Prefabs[prefabIndex], worldPos, Quaternion.identity, transform);
            var info = new SpawnedObjectInfo(go, cell, occupiesSpace);
            if (!cellObjects.ContainsKey(cell)) cellObjects[cell] = new List<SpawnedObjectInfo>();
            cellObjects[cell].Add(info);
            objectLookup[go] = info;
            OnObjectSpawned.Invoke(info);
            if (occupiesSpace && cellObjects[cell].FindAll(o => o.OccupiesSpace).Count == 1)
                OnCellOccupied.Invoke(cell);
            return info;
        }

        /// <summary>
        ///     Получить все объекты в ячейке
        /// </summary>
        public List<SpawnedObjectInfo> GetObjectsInCell(Vector3Int cellPos)
        {
            var cell = generator.GetCell(cellPos);
            if (cell == null || !cellObjects.ContainsKey(cell)) return new List<SpawnedObjectInfo>();
            return new List<SpawnedObjectInfo>(cellObjects[cell]);
        }

        /// <summary>
        ///     Проверить, занята ли ячейка (учитывая только объекты, которые занимают место)
        /// </summary>
        public bool IsCellOccupied(Vector3Int cellPos)
        {
            var cell = generator.GetCell(cellPos);
            if (cell == null || !cellObjects.ContainsKey(cell)) return false;
            return cellObjects[cell].Exists(o => o.OccupiesSpace);
        }

        /// <summary>
        ///     Удалить объект с поля
        /// </summary>
        public void RemoveObject(GameObject go)
        {
            if (!objectLookup.ContainsKey(go)) return;
            var info = objectLookup[go];
            var cell = info.Cell;
            cellObjects[cell].Remove(info);
            objectLookup.Remove(go);
            Destroy(go);
            OnObjectRemoved.Invoke(info);
            if (info.OccupiesSpace && !cellObjects[cell].Exists(o => o.OccupiesSpace))
                OnCellFreed.Invoke(cell);
        }

        /// <summary>
        ///     Получить все объекты на поле
        /// </summary>
        public List<SpawnedObjectInfo> GetAllObjects()
        {
            var all = new List<SpawnedObjectInfo>();
            foreach (var list in cellObjects.Values)
                all.AddRange(list);
            return all;
        }
    }
}