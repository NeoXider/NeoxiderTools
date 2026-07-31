using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Helper component storing the pool that owns this instance.
    ///     Added automatically when the pool creates the object. Use Return() or PoolManager.Release to return it.
    /// </summary>
    [NeoDoc("Tools/Spawner/PooledObjectInfo.md")]
    [AddComponentMenu("")] // WHY: Hidden from Add Component menu
    public class PooledObjectInfo : MonoBehaviour
    {
        public NeoObjectPool OwnerPool { get; set; }

        /// <summary>True while the instance sits inactive inside its owner pool.</summary>
        public bool IsInPool { get; internal set; }

        /// <summary>
        ///     Incremented every time the pool hands this instance out.
        ///     Lets deferred handles (delayed despawns, projectiles) detect that the instance
        ///     was returned and re-issued for a newer spawn.
        /// </summary>
        public int SpawnGeneration { get; internal set; }

        /// <summary>Returns the object to the pool. Same as PoolManager.Release(gameObject).</summary>
        [Button("Return to pool")]
        public void Return()
        {
            if (OwnerPool != null)
            {
                OwnerPool.ReturnObject(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
