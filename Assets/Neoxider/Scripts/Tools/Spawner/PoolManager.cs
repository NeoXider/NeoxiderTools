using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Neo.Tools
{
    [Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
        public bool expandPool = true;

        [Tooltip("Maximum retained inactive objects when expandPool is true. 0 = no limit (use with care).")]
        public int maxSize = 100;
    }

    /// <summary>
    ///     Central manager for all object pools in the game.
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/Spawner/PoolManager")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(PoolManager))]
    [NeoDoc("Tools/Spawner/PoolManager.md")]
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Defaults")] [SerializeField] private int _defaultInitialSize = 10;

        [SerializeField] private bool _defaultExpandPool = true;

        [SerializeField] [Tooltip("Maximum retained inactive objects when pool can expand. 0 = no limit.")]
        private int _defaultMaxSize = 100;

        [Header("Preconfigured Pools")] [SerializeField]
        private List<PoolConfig> _preconfiguredPools;

        private readonly Dictionary<GameObject, NeoObjectPool> _pools = new();
        private readonly ConditionalWeakTable<GameObject, object> _retiredSources = new();
        private static readonly object RetiredSourceMarker = new();

        protected override bool DontDestroyOnLoadEnabled => true;

        protected override void Awake()
        {
            base.Awake();
            if (!IsCurrentSingletonInstance)
            {
                return;
            }

            PrewarmPools();
        }

        private void PrewarmPools()
        {
            if (_preconfiguredPools == null)
            {
                return;
            }

            foreach (PoolConfig config in _preconfiguredPools)
            {
                if (config != null && config.prefab != null && !_pools.ContainsKey(config.prefab))
                {
                    CreateRegisteredPool(config.prefab, config.initialSize, config.expandPool, config.maxSize);
                }
            }
        }

        private NeoObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (_retiredSources.TryGetValue(prefab, out _))
            {
                return null;
            }

            if (!_pools.TryGetValue(prefab, out NeoObjectPool pool))
            {
                return CreateRegisteredPool(prefab, _defaultInitialSize, _defaultExpandPool, _defaultMaxSize);
            }

            return pool.IsRetired ? null : pool;
        }

        private NeoObjectPool CreateRegisteredPool(GameObject prefab, int initialSize, bool expandPool, int maxSize)
        {
            if (_retiredSources.TryGetValue(prefab, out _))
            {
                return null;
            }

            NeoObjectPool pool = new(prefab, initialSize, expandPool, maxSize,
                GetOrCreatePoolStorageRoot(prefab), false);
            // A first create/get callback must be able to retire this exact in-flight registration.
            _pools[prefab] = pool;
            pool.Prewarm(initialSize);
            // Prewarm may have removed the registration. Never reinsert it or publish a rental afterward.
            return pool.IsRetired ? null : pool;
        }

        private Transform GetOrCreatePoolStorageRoot(GameObject prefab)
        {
            string rootName = prefab != null ? $"{prefab.name}_Pool" : "Pool";
            Transform existing = transform.Find(rootName);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new(rootName);
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            if (I == null)
            {
                NeoDiagnostics.LogError("PoolManager not found in the scene!");
                return null;
            }

            NeoObjectPool pool = I.GetOrCreatePool(prefab);
            if (pool == null)
            {
                return null;
            }

            GameObject instance = pool.GetObject(position, rotation);
            if (instance != null)
            {
                instance.transform.SetParent(parent, true);
            }

            return instance;
        }

        /// <summary>
        ///     Retires only this exact source registration. Shared storage roots are preserved.
        ///     Returns true when a registration was removed; even an unregistered source is marked retired.
        ///     Use only for a source whose lifetime the caller exclusively owns.
        /// </summary>
        public static bool RetirePool(GameObject source)
        {
            if (ReferenceEquals(source, null) || !TryGetInstance(out PoolManager manager))
            {
                return false;
            }

            // Weak keys prevent the retirement marker itself from retaining a destroyed generated source.
            if (!manager._retiredSources.TryGetValue(source, out _))
            {
                manager._retiredSources.Add(source, RetiredSourceMarker);
            }

            if (!manager._pools.TryGetValue(source, out NeoObjectPool pool))
            {
                return false;
            }

            manager._pools.Remove(source);
            pool.Retire();
            return true;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.TryGetComponent(out PooledObjectInfo info) && info.OwnerPool != null)
            {
                info.OwnerPool.ReturnObject(instance);
            }
            else
            {
                NeoDiagnostics.LogWarning($"Object {instance.name} is not from a pool. It will be destroyed.",
                    instance);
                NeoObjectPool.DestroyPoolObject(instance);
            }
        }
    }
}
