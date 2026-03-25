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
        private readonly Dictionary<GameObject, IPoolable[]> _cachedComponents = new();
        private readonly IObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _storageRoot;

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

            // Prewarm to avoid first-use hitch
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
                components = instance.GetComponentsInChildren<IPoolable>(true);
                _cachedComponents[instance] = components;
            }

            return components;
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
                poolable.OnPoolCreate();
            }

            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            // OnPoolGet on all poolable components
            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                poolable.OnPoolGet();
            }

            instance.SetActive(true);
        }

        private void OnReleaseToPool(GameObject instance)
        {
            // OnPoolRelease on all poolable components
            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                poolable.OnPoolRelease();
            }

            if (_storageRoot != null)
            {
                instance.transform.SetParent(_storageRoot, false);
            }

            instance.SetActive(false);
        }

        private void OnDestroyObject(GameObject instance)
        {
            // Drop cache when instance is destroyed
            _cachedComponents.Remove(instance);
            Object.Destroy(instance);
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
            _pool.Release(instance);
        }

        public void Clear()
        {
            _pool.Clear();
            _cachedComponents.Clear();
        }
    }
}
