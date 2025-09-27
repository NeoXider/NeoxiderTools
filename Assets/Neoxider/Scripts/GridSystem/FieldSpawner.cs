using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem
{
    /// <summary>
    ///     Компонент для размещения игровых объектов на поле. Не зависит от структуры поля.
    /// </summary>
    [RequireComponent(typeof(FieldGenerator))]
    public class FieldSpawner : MonoBehaviour
    {
        [Header("Префабы для спавна")] public GameObject[] Prefabs;

        [Header("Событие: объект заспавнен")] public UnityEvent<GameObject, FieldCell> OnObjectSpawned = new();

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
            var cell = generator.GetCell(cellPos);
            if (cell == null || Prefabs == null || prefabIndex < 0 || prefabIndex >= Prefabs.Length) return null;
            var worldPos = generator.GetCellWorldCenter(cell.Position);
            var go = Instantiate(Prefabs[prefabIndex], worldPos, Quaternion.identity, transform);
            OnObjectSpawned.Invoke(go, cell);
            return go;
        }

        /// <summary>
        ///     Спавнит объекты на всех проходимых ячейках (пример массового спавна)
        /// </summary>
        public void SpawnOnAllWalkable(int prefabIndex = 0)
        {
            var size = generator.Config.Size;
            for (var x = 0; x < size.x; x++)
            for (var y = 0; y < size.y; y++)
            for (var z = 0; z < size.z; z++)
            {
                var cell = generator.Cells[x, y, z];
                if (cell.IsWalkable)
                    SpawnAt(cell.Position, prefabIndex);
            }
        }
    }
}