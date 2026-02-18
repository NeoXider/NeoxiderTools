using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Extension methods для работы с пулом объектов (PoolManager).
    /// </summary>
    public static class PoolExtensions
    {
        /// <summary>
        ///     Возвращает объект в пул. Если объект из пула — возвращает в PoolManager; иначе уничтожает.
        /// </summary>
        public static void ReturnToPool(this GameObject go)
        {
            PoolManager.Release(go);
        }

        /// <summary>
        ///     Спавнит префаб из пула (если PoolManager есть) или через Instantiate. Родитель задаётся одним вызовом.
        /// </summary>
        public static GameObject SpawnFromPool(this GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            if (PoolManager.I != null)
            {
                return PoolManager.Get(prefab, position, rotation, parent);
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation);
            if (parent != null)
            {
                instance.transform.SetParent(parent, true);
            }

            return instance;
        }
    }
}