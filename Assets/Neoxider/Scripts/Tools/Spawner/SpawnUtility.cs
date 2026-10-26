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
        ///     If true (default), fallback pools and their objects are destroyed on scene load.
        ///     If false, the pool root is DontDestroyOnLoad and survives scene changes.
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
        /// <param name="parent">Parent transform (null = pooled under shared root).</param>
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

            GameObject pooled = pool.GetObject(position, rotation);
            if (pooled != null)
            {
                pooled.transform.SetParent(parent != null ? parent : FallbackRoot, true);
            }

            return pooled;
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

            if (!FallbackPools.TryGetValue(prefab, out NeoObjectPool pool))
            {
                pool = new NeoObjectPool(prefab, DefaultInitialSize, DefaultExpandPool, DefaultMaxSize, FallbackRoot,
                    false);
                // Register before any prewarm callback can retire this exact source.
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
        /// </summary>
        public static void ClearFallbackPools()
        {
            foreach (NeoObjectPool pool in FallbackPools.Values)
            {
                pool.Clear();
            }

            FallbackPools.Clear();

            if (_fallbackRoot != null)
            {
                NeoObjectPool.DestroyPoolObject(_fallbackRoot.gameObject);
                _fallbackRoot = null;
            }
        }

        internal static void OnSceneLoadedForHelper()
        {
            if (DestroyFallbackPoolsOnSceneLoad)
            {
                ClearFallbackPools();
            }
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

            SpawnUtility.OnSceneLoadedForHelper();
        }
    }
}
