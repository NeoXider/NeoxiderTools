using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Tools
{
    [System.Serializable]
    public class PoolConfig
    {
        public GameObject prefab;
        public int initialSize = 10;
        public bool expandPool = true;
    }

    /// <summary>
    /// Центральный менеджер для управления всеми пулами объектов в игре.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Настройки пула по умолчанию")]
        [SerializeField] private int _defaultInitialSize = 10;
        [SerializeField] private bool _defaultExpandPool = true;

        [Header("Предварительные конфигурации пулов")]
        [SerializeField] private List<PoolConfig> _preconfiguredPools;

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
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            PrewarmPools();
        }

        private void PrewarmPools()
        {
            foreach (var config in _preconfiguredPools)
            {
                if (config.prefab != null && !_pools.ContainsKey(config.prefab))
                {
                    var pool = new NeoObjectPool(config.prefab, config.initialSize, config.expandPool);
                    _pools[config.prefab] = pool;
                }
            }
        }

        private NeoObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out NeoObjectPool pool))
            {
                pool = new NeoObjectPool(prefab, _defaultInitialSize, _defaultExpandPool);
                _pools[prefab] = pool;
            }
            return pool;
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (I == null) { Debug.LogError("PoolManager не найден на сцене!"); return null; }

            NeoObjectPool pool = I.GetOrCreatePool(prefab);
            GameObject instance = pool.GetObject(position, rotation);
            
            // Если родитель не указан, делаем объект дочерним для самого PoolManager
            instance.transform.SetParent(parent == null ? I.transform : parent);

            if (!instance.TryGetComponent(out PooledObjectInfo info))
            {
                info = instance.AddComponent<PooledObjectInfo>();
            }
            info.OwnerPool = pool;

            return instance;
        }

        public static void Release(GameObject instance)
        {
            if (instance == null) return;

            if (instance.TryGetComponent(out PooledObjectInfo info) && info.OwnerPool != null)
            {
                info.OwnerPool.ReturnObject(instance);
            }
            else
            {
                Debug.LogWarning($"Объект {instance.name} не является объектом из пула. Он будет уничтожен (Destroy).", instance);
                Destroy(instance);
            }
        }
    }
}
