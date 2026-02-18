using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Spawns prefabs into cells of a generated field.
    /// </summary>
    [NeoDoc("GridSystem/FieldSpawner.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [AddComponentMenu("Neo/" + "GridSystem/" + nameof(FieldSpawner))]
    public class FieldSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject[] Prefabs;

        /// <summary>
        /// Raised after an object is spawned. Provides spawned object and target cell.
        /// </summary>
        public UnityEvent<GameObject, FieldCell> OnObjectSpawned = new();

        private FieldGenerator generator;

        private void Awake()
        {
            generator = GetComponent<FieldGenerator>();
        }

        /// <summary>
        /// Spawns prefab instance at target cell center.
        /// </summary>
        /// <param name="cellPos">Target cell position.</param>
        /// <param name="prefabIndex">Index in <see cref="Prefabs"/> array.</param>
        /// <returns>Spawned GameObject or null when spawn fails.</returns>
        public GameObject SpawnAt(Vector3Int cellPos, int prefabIndex = 0)
        {
            FieldCell cell = generator.GetCell(cellPos);
            if (cell == null || Prefabs == null || prefabIndex < 0 || prefabIndex >= Prefabs.Length)
            {
                return null;
            }

            Vector3 worldPos = generator.GetCellWorldCenter(cell.Position);
            GameObject go = Instantiate(Prefabs[prefabIndex], worldPos, Quaternion.identity, transform);
            OnObjectSpawned.Invoke(go, cell);
            return go;
        }

        /// <summary>
        /// Spawns prefab on all currently passable cells.
        /// </summary>
        /// <param name="prefabIndex">Index in <see cref="Prefabs"/> array.</param>
        public void SpawnOnAllWalkable(int prefabIndex = 0)
        {
            Vector3Int size = generator.Config.Size;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = generator.Cells[x, y, z];
                if (generator.IsCellPassable(cell, true))
                {
                    SpawnAt(cell.Position, prefabIndex);
                }
            }
        }
    }
}