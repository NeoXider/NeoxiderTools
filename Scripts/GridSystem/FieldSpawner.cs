using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Spawns prefabs into cells of a generated field.
    /// </summary>
    [NeoDoc("GridSystem/FieldSpawner.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/FieldSpawner")]
    [AddComponentMenu("Neoxider/" + "GridSystem/" + nameof(FieldSpawner))]
    public class FieldSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject[] Prefabs;

        /// <summary>
        ///     Raised after an object is spawned. Provides spawned object and target cell.
        /// </summary>
        public UnityEvent<GameObject, FieldCell> OnObjectSpawned = new();

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
        ///     Spawns prefab instance at target cell center.
        /// </summary>
        /// <param name="cellPos">Target cell position.</param>
        /// <param name="prefabIndex">Index in <see cref="Prefabs" /> array.</param>
        /// <returns>Spawned GameObject or null when spawn fails.</returns>
        public GameObject SpawnAt(Vector3Int cellPos, int prefabIndex = 0)
        {
            FieldCell cell = Generator != null ? Generator.GetCell(cellPos) : null;
            if (cell == null || Prefabs == null || prefabIndex < 0 || prefabIndex >= Prefabs.Length ||
                Prefabs[prefabIndex] == null)
            {
                return null;
            }

            Vector3 worldPos = Generator.GetCellWorldCenter(cell.Position);
            GameObject go = Instantiate(Prefabs[prefabIndex], worldPos, Quaternion.identity, transform);
            OnObjectSpawned.Invoke(go, cell);
            return go;
        }

        /// <summary>
        ///     Spawns prefab on all currently passable cells.
        /// </summary>
        /// <param name="prefabIndex">Index in <see cref="Prefabs" /> array.</param>
        [Button(PlayModeOnly = true)]
        public void SpawnOnAllWalkable(int prefabIndex = 0)
        {
            if (Generator == null)
            {
                return;
            }

            foreach (FieldCell cell in Generator.GetAllCells(false))
            {
                if (Generator.IsCellPassable(cell, true))
                {
                    SpawnAt(cell.Position, prefabIndex);
                }
            }
        }
    }
}
