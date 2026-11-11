using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Tools
{
    /// <summary>
    ///     Unified entry for spawning and despawning objects.
    ///     Always uses a pool: with <see cref="PoolManager" /> in the scene, its pools are used;
    ///     otherwise a per-prefab pool is created automatically. Instances are always pooled.
    /// </summary>
    public static class SpawnUtility
    {
        private const int DefaultInitialSize = 10;
        private const bool DefaultExpandPool = true;
        private const int DefaultMaxSize = 100;

        private static readonly Dictionary<GameObject, NeoObjectPool> FallbackPools = new();
        private static ConditionalWeakTable<GameObject, object> _retiredSources = new();
        private static readonly object RetiredSourceMarker = new();
        private static Transform _fallbackRoot;
        private static bool _helperCreated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            FallbackPools.Clear();
            _retiredSources = new ConditionalWeakTable<GameObject, object>();
            _fallbackRoot = null;
            _helperCreated = false;
            DestroyFallbackPoolsOnSceneLoad = true;
        }

        /// <summary>
        ///     If true (default), fallback pools and their objects are destroyed on a Single scene load; additive
        ///     loads keep them, and pools the incoming scene created in its own Awake/OnEnable survive with their
        ///     rentals. If false, the pool root is DontDestroyOnLoad and survives scene changes.
        ///     Read when the pool root is created; set it before the first <see cref="Spawn(GameObject)" />.
        /// </summary>
        public static bool DestroyFallbackPoolsOnSceneLoad { get; set; } = true;

        private static Transform FallbackRoot
        {
            get
            {
                if (_fallbackRoot != null)
                {
                    return _fallbackRoot;
                }

                GameObject go = new("[SpawnUtility Pools]");
                _fallbackRoot = go.transform;

                if (DestroyFallbackPoolsOnSceneLoad)
                {
                    EnsureSceneHelper();
                }
                else if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(go);
                }

                return _fallbackRoot;
            }
        }

        /// <summary>
        ///     True when spawning uses pooling (PoolManager or internal fallback pools).
        /// </summary>
        public static bool IsPoolAvailable => true;

        private static void EnsureSceneHelper()
        {
            if (_helperCreated || !Application.isPlaying)
            {
                return;
            }

            _helperCreated = true;
            GameObject helperGo = new("[SpawnUtility Scene Helper]");
            helperGo.AddComponent<SpawnUtilitySceneHelper>();
            Object.DontDestroyOnLoad(helperGo);
        }

        /// <summary>
        ///     Spawns prefab at (0,0,0), identity rotation, no parent (or under pool root).
        /// </summary>
        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        ///     Spawns prefab at the given position, identity rotation.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position)
        {
            return Spawn(prefab, position, Quaternion.identity, null);
        }

        /// <summary>
        ///     Spawns prefab at the given position and rotation.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null);
        }

        /// <summary>
        ///     Spawns prefab at position and rotation with an optional parent.
        /// </summary>
        /// <param name="prefab">Prefab to spawn.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        /// <param name="parent">
        ///     Parent transform. Null parents the rental under the shared fallback root without a PoolManager, and
        ///     leaves it unparented in the active scene with one.
        /// </param>
        /// <returns>Spawned instance or null if the source is null or retired, including during prewarm.</returns>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null || _retiredSources.TryGetValue(prefab, out _))
            {
                return null;
            }

            if (PoolManager.I != null)
            {
                GameObject fromManager = PoolManager.Get(prefab, position, rotation, parent);
                return fromManager;
            }

            NeoObjectPool pool = GetOrCreateFallbackPool(prefab);
            if (pool == null)
            {
                return null;
            }

            return pool.GetObject(position, rotation, parent != null ? parent : FallbackRoot);
        }

        /// <summary>
        ///     Retires exact registrations for an exclusively owned generated source in both routing paths.
        ///     Future Spawn calls for this source return null. Active rentals survive until their valid return,
        ///     then are destroyed. No storage root is deleted. Returns true if an existing pool was removed.
        /// </summary>
        public static bool RetirePool(GameObject source)
        {
            if (ReferenceEquals(source, null))
            {
                return false;
            }

            if (!_retiredSources.TryGetValue(source, out _))
            {
                _retiredSources.Add(source, RetiredSourceMarker);
            }

            bool retired = PoolManager.RetirePool(source);
            if (FallbackPools.TryGetValue(source, out NeoObjectPool pool))
            {
                FallbackPools.Remove(source);
                pool.Retire();
                retired = true;
            }

            return retired;
        }

        /// <summary>
        ///     Spawns prefab as child of parent at local (0,0,0) and identity rotation.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, parent != null ? parent.position : Vector3.zero,
                parent != null ? parent.rotation : Quaternion.identity, parent);
        }

        private static NeoObjectPool GetOrCreateFallbackPool(GameObject prefab)
        {
            if (_retiredSources.TryGetValue(prefab, out _))
            {
                return null;
            }

            if (FallbackPools.TryGetValue(prefab, out NeoObjectPool pool) && HasLostStorage(pool))
            {
                // WHY: The incoming scene's Awake/OnEnable run before sceneLoaded, so a pool whose storage root
                // died with the previous scene is still registered. Reusing it would park every return inactive
                // where it stands, never to be reused; a fresh pool stores under the live root instead.
                DropFallbackPool(prefab, pool);
                pool = null;
            }

            if (pool == null)
            {
                pool = new NeoObjectPool(prefab, DefaultInitialSize, DefaultExpandPool, DefaultMaxSize, FallbackRoot,
                    false);
                // WHY: Register before any prewarm callback can retire this exact source.
                FallbackPools[prefab] = pool;
                pool.Prewarm(DefaultInitialSize);
            }

            return pool.IsRetired ? null : pool;
        }

        /// <summary>
        ///     Despawns: returns to pool if pooled, otherwise destroys.
        /// </summary>
        public static void Despawn(GameObject instance)
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
                NeoObjectPool.DestroyPoolObject(instance);
            }
        }

        /// <summary>
        ///     Clears all fallback pools (created when PoolManager is missing). Destroys the pool root object.
        ///     The dropped pools are retired: a rental still out (for example under a persistent parent) is
        ///     destroyed on its later return instead of being parked inactive with no pool to reuse it.
        ///     The prefabs stay spawnable; the next <see cref="Spawn(GameObject)" /> builds a fresh pool.
        /// </summary>
        public static void ClearFallbackPools()
        {
            if (FallbackPools.Count > 0)
            {
                // WHY: Copy first: a release cleanup run by the retirement may call RetirePool and edit the registry.
                NeoObjectPool[] pools = new NeoObjectPool[FallbackPools.Count];
                FallbackPools.Values.CopyTo(pools, 0);
                FallbackPools.Clear();
                for (int i = 0; i < pools.Length; i++)
                {
                    pools[i].Retire();
                }
            }

            if (_fallbackRoot != null)
            {
                NeoObjectPool.DestroyPoolObject(_fallbackRoot.gameObject);
                _fallbackRoot = null;
            }
        }

        internal static void OnSceneLoadedForHelper(Scene loadedScene)
        {
            if (!DestroyFallbackPoolsOnSceneLoad)
            {
                return;
            }

            Transform liveRoot = _fallbackRoot;
            if (liveRoot == null || liveRoot.gameObject.scene != loadedScene)
            {
                ClearFallbackPools();
                return;
            }

            // WHY: sceneLoaded fires after the incoming scene's Awake/OnEnable. A root that already belongs to the
            // loaded scene was created by those callbacks and parents their fresh rentals; destroying it would
            // delete objects the new scene just spawned. Only pools whose storage died with the old scene go.
            RemoveFallbackPoolsOutside(liveRoot);
        }

        private static void RemoveFallbackPoolsOutside(Transform liveRoot)
        {
            List<GameObject> stale = null;
            foreach (KeyValuePair<GameObject, NeoObjectPool> pair in FallbackPools)
            {
                if (pair.Value.StorageRoot != liveRoot)
                {
                    stale ??= new List<GameObject>();
                    stale.Add(pair.Key);
                }
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                GameObject source = stale[i];
                if (FallbackPools.TryGetValue(source, out NeoObjectPool pool))
                {
                    DropFallbackPool(source, pool);
                }
            }
        }

        private static bool HasLostStorage(NeoObjectPool pool)
        {
            Transform storage = pool.StorageRoot;
            return !ReferenceEquals(storage, null) && storage == null;
        }

        private static void DropFallbackPool(GameObject source, NeoObjectPool pool)
        {
            FallbackPools.Remove(source);
            // WHY: Retire rather than Clear: the pool's storage is gone, so a rental still out (for example under a
            // persistent parent) is destroyed on its return instead of being parked inactive forever. The source
            // itself is not retired; the next Spawn builds a fresh pool for it.
            pool.Retire();
        }
    }

    internal class SpawnUtilitySceneHelper : MonoBehaviour
    {
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
            // WHY: Additive loads (UI overlays, streamed chunks) keep gameplay running; wiping the
            // fallback pools then would orphan live pooled instances and leak every despawned object.
            if (mode != LoadSceneMode.Single)
            {
                return;
            }

            SpawnUtility.OnSceneLoadedForHelper(scene);
        }
    }
}
