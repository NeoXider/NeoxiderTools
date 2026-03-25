using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Extension methods for object pooling (PoolManager).
    /// </summary>
    public static class PoolExtensions
    {
        /// <summary>
        ///     Despawns the object: returns to pool or destroys (via <see cref="SpawnUtility.Despawn" />).
        /// </summary>
        public static void ReturnToPool(this GameObject go)
        {
            SpawnUtility.Despawn(go);
        }

        /// <summary>
        ///     Spawns prefab through the unified entry: pool if PoolManager exists, otherwise Instantiate.
        /// </summary>
        public static GameObject SpawnFromPool(this GameObject prefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            return SpawnUtility.Spawn(prefab, position, rotation, parent);
        }
    }
}
