using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Pool for a single prefab; invokes IPoolable on instances.
    /// </summary>
    /// <remarks>
    ///     <see cref="GetObject(Vector3, Quaternion)" /> and <see cref="ReturnObject" /> are O(1) and allocation-free
    ///     once the pool is warm.
    ///     Returning an instance twice, returning a foreign instance or returning a destroyed instance is a no-op.
    /// </remarks>
    public class NeoObjectPool
    {
        private readonly IObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _storageRoot;
        private bool _storageTeardown;

        /// <summary>Creates a pool for <paramref name="prefab" /> and prewarms <paramref name="initialSize" /> instances.</summary>
        /// <param name="prefab">Source object cloned for every new instance.</param>
        /// <param name="initialSize">Instances created up front (negative values count as zero).</param>
        /// <param name="expandPool">When false the pool retains at most max(1, initialSize) inactive instances.</param>
        /// <param name="maxSize">Maximum retained inactive instances when expanding; 0 = unlimited.</param>
        /// <param name="storageRoot">Optional parent for inactive instances; it is never destroyed by the pool.</param>
        public NeoObjectPool(GameObject prefab, int initialSize, bool expandPool, int maxSize = 100,
            Transform storageRoot = null)
            : this(prefab, initialSize, expandPool, maxSize, storageRoot, true)
        {
        }

        // WHY: Registry owners defer callbacks until this fully initialized pool is registered for retirement.
        internal NeoObjectPool(GameObject prefab, int initialSize, bool expandPool, int maxSize,
            Transform storageRoot, bool prewarm)
        {
            if (prefab == null)
            {
                throw new System.ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;
            _storageRoot = storageRoot;
            initialSize = Mathf.Max(0, initialSize);
            int effectiveMax = expandPool ? maxSize > 0 ? maxSize : int.MaxValue : Mathf.Max(1, initialSize);

            // WHY: ObjectPool's collectionCheck scans every inactive instance on each Release (O(n) per return),
            // but only in the Editor (Edit and Play Mode); Player builds never run it. ReturnObject already rejects
            // double, foreign and destroyed returns through PooledObjectInfo.IsInPool in every build, so the scan
            // is redundant and only slowed Editor returns as the pool grew.
            _pool = new ObjectPool<GameObject>(
                CreatePooledObject,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyObject,
                false,
                initialSize,
                effectiveMax
            );

            if (prewarm)
            {
                Prewarm(initialSize);
            }
        }

        internal void Prewarm(int initialSize)
        {
            // WHY: Prewarm to avoid first-use hitch
            List<GameObject> prewarmList = new();
            for (int i = 0; i < initialSize && !IsRetired; i++)
            {
                GameObject item = _pool.Get();
                if (item != null)
                {
                    prewarmList.Add(item);
                }
            }

            foreach (GameObject item in prewarmList)
            {
                ReturnObject(item);
            }
        }

        /// <summary>Number of inactive instances currently stored and ready for reuse.</summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>Source object this pool clones.</summary>
        public GameObject Prefab => _prefab;

        /// <summary>Retired pools reject retrieval and destroy outstanding rentals on their valid return.</summary>
        public bool IsRetired { get; private set; }

        internal Transform StorageRoot => _storageRoot;

        /// <summary>
        ///     IPoolable components of one instance, cached on its <see cref="PooledObjectInfo" /> at creation.
        /// </summary>
        private static IPoolable[] GetPoolableComponents(GameObject instance, PooledObjectInfo info)
        {
            if (info == null)
            {
                // WHY: Only reachable when game code removed the auto-added PooledObjectInfo; callbacks keep
                // working, just without the per-instance cache.
                return instance.GetComponentsInChildren<IPoolable>(true);
            }

            return info.CachedPoolables ??= instance.GetComponentsInChildren<IPoolable>(true);
        }

        private static void InvokeCreate(IPoolable[] poolables)
        {
            for (int i = 0; i < poolables.Length; i++)
            {
                IPoolable poolable = poolables[i];
                if (poolable as Object != null)
                {
                    poolable.OnPoolCreate();
                }
            }
        }

        private GameObject CreatePooledObject()
        {
            if (IsRetired)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(_prefab);
            if (_storageRoot != null)
            {
                instance.transform.SetParent(_storageRoot, false);
            }

            if (!instance.TryGetComponent(out PooledObjectInfo info))
            {
                info = instance.AddComponent<PooledObjectInfo>();
            }

            info.OwnerPool = this;
            info.CachedPoolables = instance.GetComponentsInChildren<IPoolable>(true);
            InvokeCreate(info.CachedPoolables);
            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            // WHY: A scene unload or caller may have destroyed an inactive rental.
            if (instance == null)
            {
                return;
            }

            if (instance.TryGetComponent(out PooledObjectInfo info))
            {
                info.InvokeReleaseCleanup();
                info.IsInPool = false;
                info.SpawnGeneration++;
            }

            IPoolable[] poolableComponents = GetPoolableComponents(instance, info);
            for (int i = 0; i < poolableComponents.Length; i++)
            {
                IPoolable poolable = poolableComponents[i];
                if (poolable as Object != null)
                {
                    poolable.OnPoolGet();
                }
            }

            instance.SetActive(true);
        }

        private void OnReleaseToPool(GameObject instance)
        {
            if (instance.TryGetComponent(out PooledObjectInfo info))
            {
                info.IsInPool = true;
            }

            IPoolable[] poolableComponents = GetPoolableComponents(instance, info);
            for (int i = 0; i < poolableComponents.Length; i++)
            {
                IPoolable poolable = poolableComponents[i];
                if (poolable as Object != null)
                {
                    poolable.OnPoolRelease();
                }
            }

#if MIRROR
            // WHY: Preserve release callbacks' network state, then unregister before storage/reuse.
            if (NetworkServer.active && instance.TryGetComponent(out NetworkIdentity identity)
                                     && identity.netId != 0
                                     && NetworkServer.spawned.TryGetValue(identity.netId, out NetworkIdentity registered)
                                     && registered == identity)
            {
                NetworkServer.UnSpawn(instance);
            }
#endif

            if (info != null)
            {
                info.InvokeReleaseCleanup();
            }

            if (_storageRoot != null)
            {
                instance.transform.SetParent(_storageRoot, false);
            }

            instance.SetActive(false);
        }

        private void OnDestroyObject(GameObject instance)
        {
            if (instance != null && instance.TryGetComponent(out PooledObjectInfo info))
            {
                info.InvokeReleaseCleanup();
            }

            if (_storageTeardown && DiesWithStorage(instance))
            {
                return;
            }

            DestroyPoolObject(instance);
        }

        /// <summary>
        ///     True when <paramref name="instance" /> goes down with the storage root that is being destroyed.
        /// </summary>
        private bool DiesWithStorage(GameObject instance)
        {
            if (ReferenceEquals(_storageRoot, null))
            {
                return false;
            }

            // WHY: A root already reported destroyed takes its stored children with it in the same teardown.
            return instance == null || _storageRoot == null || instance.transform.IsChildOf(_storageRoot);
        }

        internal static void DestroyPoolObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(instance);
            }
            else
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        ///     Rents an active instance at the given world pose, creating one when no inactive instance is stored.
        ///     Inactive instances destroyed externally (scene unload, direct Destroy) are skipped.
        ///     In Play Mode an instance taken out of the storage root lands unparented in the active scene, like
        ///     <c>Object.Instantiate</c> without a parent, so a storage root kept in DontDestroyOnLoad does not carry
        ///     the rental across scene loads.
        /// </summary>
        /// <returns>The rented instance, or null when the pool is retired.</returns>
        public GameObject GetObject(Vector3 position, Quaternion rotation)
        {
            GameObject instance = Rent(position, rotation, out bool leftStorage);
            if (leftStorage)
            {
                AdoptIntoActiveScene(instance);
            }

            return instance;
        }

        /// <summary>
        ///     Rents an instance and parents it to <paramref name="parent" /> keeping the world pose; a null parent
        ///     leaves it unparented in the active scene (Play Mode).
        /// </summary>
        internal GameObject GetObject(Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject instance = Rent(position, rotation, out _);
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, true);
            if (parent == null)
            {
                AdoptIntoActiveScene(instance);
            }

            return instance;
        }

        private GameObject Rent(Vector3 position, Quaternion rotation, out bool leftStorage)
        {
            leftStorage = false;
            GameObject instance;
            do
            {
                if (IsRetired)
                {
                    return null;
                }

                instance = _pool.Get();
            } while (instance == null);

            if (IsRetired)
            {
                ReturnObject(instance);
                return null;
            }

            if (_storageRoot != null && instance.transform.parent == _storageRoot)
            {
                instance.transform.SetParent(null, false);
                leftStorage = true;
            }

            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }

        /// <summary>
        ///     Moves an unparented rental into the active scene when its former storage root lives elsewhere.
        /// </summary>
        /// <remarks>
        ///     WHY: Unity keeps an unparented object in its former root's scene. PoolManager stores in
        ///     DontDestroyOnLoad, so a rental without a parent would otherwise survive Single scene loads and leak
        ///     enemies or projectiles of the old scene into the next one. Edit Mode is left alone: moving objects
        ///     between open scenes there would dirty them.
        /// </remarks>
        private static void AdoptIntoActiveScene(GameObject instance)
        {
            if (!Application.isPlaying || instance.transform.parent != null)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded || instance.scene == activeScene)
            {
                return;
            }

            SceneManager.MoveGameObjectToScene(instance, activeScene);
        }

        /// <summary>
        ///     Returns a rental to this pool. Null, destroyed, foreign or already returned instances are ignored.
        /// </summary>
        public void ReturnObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            // WHY: This check is the pool's only double-release guard (the inner collectionCheck is off for O(1)
            // returns); external callers (Despawner, Spawner.Clear, game code) may legitimately return an
            // instance that already went back to the pool, so the return must stay idempotent.
            if (!instance.TryGetComponent(out PooledObjectInfo info) || info.OwnerPool != this || info.IsInPool)
            {
                return;
            }

            if (IsRetired)
            {
                OnReleaseToPool(instance);
                OnDestroyObject(instance);
                return;
            }

            _pool.Release(instance);
            if (IsRetired)
            {
                // WHY: Retirement may have been requested by a release observer before ObjectPool stored this item.
                _pool.Clear();
            }
        }

        /// <summary>
        ///     Permanently stops this pool, clears its inactive instances, and preserves active rentals until return.
        ///     The optional storage root is never destroyed: another source may share it.
        /// </summary>
        public void Retire()
        {
            if (IsRetired)
            {
                return;
            }

            IsRetired = true;
            _pool.Clear();
        }

        /// <summary>
        ///     Retires this pool while its storage root is being destroyed by its owner in Edit Mode. Inactive
        ///     instances stored under the root are left to die with it; outstanding rentals are destroyed on their
        ///     valid return. In Play Mode owners call <see cref="Retire" />, whose deferred destroy is harmless on a
        ///     dying child.
        /// </summary>
        internal void RetireWithStorage()
        {
            if (IsRetired)
            {
                return;
            }

            IsRetired = true;
            // WHY: Destroying the stored children again from the owner's OnDestroy would be a second destroy of the
            // same objects, which DestroyImmediate reports as an error in Edit Mode.
            _storageTeardown = true;
            try
            {
                _pool.Clear();
            }
            finally
            {
                _storageTeardown = false;
            }
        }

        /// <summary>Destroys every stored inactive instance. Outstanding rentals stay valid and can still return.</summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }
}
