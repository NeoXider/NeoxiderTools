using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Компонент для размещения игровых объектов на поле. Не зависит от структуры поля.
    /// </summary>
    [RequireComponent(typeof(FieldGenerator))]
    [AddComponentMenu("Neo/" + "GridSystem/" + nameof(FieldSpawner))]
    public class FieldSpawner : MonoBehaviour
    {
        [Header("Prefabs")] public GameObject[] Prefabs;

        public UnityEvent<GameObject, FieldCell> OnObjectSpawned = new();

        private FieldGenerator generator;

        private void Awake()
        {
            generator = GetComponent<FieldGenerator>();
        }

        /// <summary>
        ///     Спавнит объект на указанной ячейке
        /// </summary>
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
        ///     Спавнит объекты на всех проходимых ячейках (пример массового спавна)
        /// </summary>
        public void SpawnOnAllWalkable(int prefabIndex = 0)
        {
            Vector3Int size = generator.Config.Size;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            for (int z = 0; z < size.z; z++)
            {
                FieldCell cell = generator.Cells[x, y, z];
                if (cell.IsWalkable)
                {
                    SpawnAt(cell.Position, prefabIndex);
                }
            }
        }
    }
}