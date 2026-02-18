using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Tools
{
    [Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
        public bool expandPool = true;

        [Tooltip("Max pool size when expandPool is true. 0 = no limit (use with care).")]
        public int maxSize = 100;
    }

    /// <summary>
    ///     Центральный менеджер для управления всеми пулами объектов в игре.
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/Spawner/PoolManager")]
    [AddComponentMenu("Neoxider/Tools/" + nameof(PoolManager))]
    [NeoDoc("Tools/Spawner/PoolManager.md")]
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Defaults")] [SerializeField] private int _defaultInitialSize = 10;

        [SerializeField] private bool _defaultExpandPool = true;

        [SerializeField] [Tooltip("Max size when pool can expand. 0 = no limit.")]
        private int _defaultMaxSize = 100;

        [Header("Preconfigured Pools")] [SerializeField]
        private List<PoolConfig> _preconfiguredPools;

        private readonly Dictionary<GameObject, NeoObjectPool> _pools = new();

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            PrewarmPools();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (NeoObjectPool pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
            PrewarmPools();
        }

        private void PrewarmPools()
        {
            foreach (PoolConfig config in _preconfiguredPools)
            {
                if (config.prefab != null && !_pools.ContainsKey(config.prefab))
                {
                    int max = config.expandPool ? config.maxSize > 0 ? config.maxSize : 100 : config.initialSize;
                    NeoObjectPool pool = new(config.prefab, config.initialSize, config.expandPool, max);
                    _pools[config.prefab] = pool;
                }
            }
        }

        private NeoObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out NeoObjectPool pool))
            {
                int max = _defaultExpandPool ? _defaultMaxSize > 0 ? _defaultMaxSize : 100 : _defaultInitialSize;
                pool = new NeoObjectPool(prefab, _defaultInitialSize, _defaultExpandPool, max);
                _pools[prefab] = pool;
            }

            return pool;
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (I == null)
            {
                Debug.LogError("PoolManager не найден на сцене!");
                return null;
            }

            NeoObjectPool pool = I.GetOrCreatePool(prefab);
            GameObject instance = pool.GetObject(position, rotation);
            instance.transform.SetParent(parent == null ? I.transform : parent);
            return instance;
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
                Debug.LogWarning($"Объект {instance.name} не является объектом из пула. Он будет уничтожен (Destroy).",
                    instance);
                Destroy(instance);
            }
        }
    }
}