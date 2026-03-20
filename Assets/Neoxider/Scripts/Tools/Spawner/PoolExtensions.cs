using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Extension methods для работы с пулом объектов (PoolManager).
    /// </summary>
    public static class PoolExtensions
    {
        /// <summary>
        ///     Деспавнит объект: возвращает в пул или уничтожает (через <see cref="SpawnUtility.Despawn" />).
        /// </summary>
        public static void ReturnToPool(this GameObject go)
        {
            SpawnUtility.Despawn(go);
        }

        /// <summary>
        ///     Спавнит префаб через единую точку входа: пул (если есть PoolManager) или Instantiate.
        /// </summary>
        public static GameObject SpawnFromPool(this GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            return SpawnUtility.Spawn(prefab, position, rotation, parent);
        }
    }
}
