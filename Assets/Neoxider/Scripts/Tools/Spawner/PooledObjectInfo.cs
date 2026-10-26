using System;
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
        private Action<GameObject> _releaseCleanup;
        private NeoObjectPool _cleanupOwner;
        private int _cleanupGeneration;

        public NeoObjectPool OwnerPool { get; set; }

        /// <summary>True while the instance sits inactive inside its owner pool.</summary>
        public bool IsInPool { get; internal set; }

        /// <summary>
        ///     Incremented every time the pool hands this instance out.
        ///     Lets deferred handles (delayed despawns, projectiles) detect that the instance
        ///     was returned and re-issued for a newer spawn.
        /// </summary>
        public int SpawnGeneration { get; internal set; }

        /// <summary>
        ///     Registers one cleanup for this exact outstanding rental. It runs once on release or destruction,
        ///     outside the cached IPoolable set. A pending cleanup cannot be replaced by a later owner.
        ///     The callback follows the pool's nonthrowing, non-reentrant lifecycle contract.
        /// </summary>
        public bool TryRegisterReleaseCleanup(NeoObjectPool expectedOwner, int expectedGeneration,
            Action<GameObject> cleanup)
        {
            if (cleanup == null)
            {
                throw new ArgumentNullException(nameof(cleanup));
            }

            if (this == null || expectedOwner == null || OwnerPool != expectedOwner || IsInPool
                                      || SpawnGeneration != expectedGeneration || _releaseCleanup != null)
            {
                return false;
            }

            _cleanupOwner = expectedOwner;
            _cleanupGeneration = expectedGeneration;
            _releaseCleanup = cleanup;
            return true;
        }

        internal void InvokeReleaseCleanup()
        {
            Action<GameObject> cleanup = _releaseCleanup;
            NeoObjectPool owner = _cleanupOwner;
            int generation = _cleanupGeneration;
            // Detach first: return, eviction and Unity OnDestroy must not replay this generation's work.
            _releaseCleanup = null;
            _cleanupOwner = null;

            if (cleanup != null && OwnerPool == owner && SpawnGeneration == generation)
            {
                cleanup(gameObject);
            }
        }

        private void OnDestroy()
        {
            IsInPool = true;
            InvokeReleaseCleanup();
        }

        /// <summary>Returns the object to the pool. Same as PoolManager.Release(gameObject).</summary>
        [Button("Return to pool", PlayModeOnly = true)]
        public void Return()
        {
            if (OwnerPool != null)
            {
                OwnerPool.ReturnObject(gameObject);
            }
            else
            {
                NeoObjectPool.DestroyPoolObject(gameObject);
            }
        }
    }
}
