using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Helper component storing the pool that owns this instance.
    ///     Added automatically when the pool creates the object. Use Return() or PoolManager.Release to return it.
    /// </summary>
    [NeoDoc("Tools/Spawner/PooledObjectInfo.md")]
    [AddComponentMenu("")] // Hidden from Add Component menu
    public class PooledObjectInfo : MonoBehaviour
    {
        public NeoObjectPool OwnerPool { get; set; }

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
