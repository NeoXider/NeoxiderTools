using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Neo.Tools
{
    /// <summary>
    ///     Pool for a single prefab; invokes IPoolable on instances.
    /// </summary>
    public class NeoObjectPool
    {
        private const int MinCachePruneThreshold = 16;

        private readonly Dictionary<GameObject, IPoolable[]> _cachedComponents = new();
        private readonly IObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _storageRoot;
        private int _cachePruneThreshold = MinCachePruneThreshold;

        public NeoObjectPool(GameObject prefab, int initialSize, bool expandPool, int maxSize = 100,
            Transform storageRoot = null)
        {
            _prefab = prefab;
            _storageRoot = storageRoot;
            int effectiveMax = expandPool ? maxSize > 0 ? maxSize : 100 : initialSize;

            _pool = new ObjectPool<GameObject>(
                CreatePooledObject,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyObject,
                true,
                initialSize,
                effectiveMax
            );

            // WHY: Prewarm to avoid first-use hitch
            List<GameObject> prewarmList = new();
            for (int i = 0; i < initialSize; i++)
            {
                prewarmList.Add(_pool.Get());
            }

            foreach (GameObject item in prewarmList)
            {
                _pool.Release(item);
            }
        }

        public int CountInactive => _pool.CountInactive;

        /// <summary>
        ///     Resolves IPoolable components with per-instance caching.
        /// </summary>
        private IPoolable[] GetPoolableComponents(GameObject instance)
        {
            if (!_cachedComponents.TryGetValue(instance, out IPoolable[] components))
            {
                // WHY: Keys are GameObjects that can be destroyed externally (scene unload while the
                // pool lives in DontDestroyOnLoad, direct Destroy by game code); prune dead entries
                // before growing so the cache stays bounded by the number of live instances.
                if (_cachedComponents.Count >= _cachePruneThreshold)
                {
                    PruneDeadCacheEntries();
                    _cachePruneThreshold = Mathf.Max(MinCachePruneThreshold, _cachedComponents.Count * 2);
                }

                components = instance.GetComponentsInChildren<IPoolable>(true);
                _cachedComponents[instance] = components;
            }

            return components;
        }

        private void PruneDeadCacheEntries()
        {
            List<GameObject> dead = null;
            foreach (KeyValuePair<GameObject, IPoolable[]> entry in _cachedComponents)
            {
                if (entry.Key == null)
                {
                    dead ??= new List<GameObject>();
                    dead.Add(entry.Key);
                }
            }

            if (dead == null)
            {
                return;
            }

            foreach (GameObject key in dead)
            {
                _cachedComponents.Remove(key);
            }
        }

        private GameObject CreatePooledObject()
        {
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

            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                if (poolable as Object != null)
                {
                    poolable.OnPoolCreate();
                }
            }

            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            if (instance.TryGetComponent(out PooledObjectInfo info))
            {
                info.IsInPool = false;
                info.SpawnGeneration++;
            }

            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
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

            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                if (poolable as Object != null)
                {
                    poolable.OnPoolRelease();
                }
            }

            if (_storageRoot != null)
            {
                instance.transform.SetParent(_storageRoot, false);
            }

            instance.SetActive(false);
        }

        private void OnDestroyObject(GameObject instance)
        {
            _cachedComponents.Remove(instance);
            DestroyPoolObject(instance);
        }

        private static void DestroyPoolObject(GameObject instance)
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

        public GameObject GetObject(Vector3 position, Quaternion rotation)
        {
            GameObject instance = _pool.Get();
            if (_storageRoot != null && instance.transform.parent == _storageRoot)
            {
                instance.transform.SetParent(null, false);
            }

            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }

        public void ReturnObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            // WHY: The inner ObjectPool uses collectionCheck and throws on double release; external
            // callers (Despawner, Spawner.Clear, game code) may legitimately try to return an
            // instance that already went back to the pool, so make the return idempotent.
            if (instance.TryGetComponent(out PooledObjectInfo info) && info.IsInPool)
            {
                return;
            }

            _pool.Release(instance);
        }

        public void Clear()
        {
            _pool.Clear();
            _cachedComponents.Clear();
        }
    }
}
