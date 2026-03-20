using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Neo.Tools
{
    /// <summary>
    ///     Единая точка входа для спавна и деспавна объектов.
    ///     Всегда использует пул: при наличии <see cref="PoolManager" /> на сцене — его пулы;
    ///     если PoolManager нет — для каждого префаба создаётся свой пул автоматически. Объекты всегда в пуле.
    /// </summary>
    public static class SpawnUtility
    {
        private const int DefaultInitialSize = 10;
        private const bool DefaultExpandPool = true;
        private const int DefaultMaxSize = 100;

        private static readonly Dictionary<GameObject, NeoObjectPool> FallbackPools = new();
        private static Transform _fallbackRoot;
        private static bool _helperCreated;

        /// <summary>
        ///     Если true (по умолчанию), при смене сцены все fallback-пулы и их объекты уничтожаются.
        ///     Если false — корень пулов помечается DontDestroyOnLoad и переживает смену сцены.
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
                else
                {
                    Object.DontDestroyOnLoad(go);
                }

                return _fallbackRoot;
            }
        }

        /// <summary>
        ///     Возвращает true, если спавн идёт через пул (есть PoolManager или используются внутренние пулы).
        /// </summary>
        public static bool IsPoolAvailable => true;

        private static void EnsureSceneHelper()
        {
            if (_helperCreated)
            {
                return;
            }

            _helperCreated = true;
            GameObject helperGo = new("[SpawnUtility Scene Helper]");
            helperGo.AddComponent<SpawnUtilitySceneHelper>();
            Object.DontDestroyOnLoad(helperGo);
        }

        /// <summary>
        ///     Спавнит префаб в позиции (0,0,0), поворот identity, без родителя (или в корень пулов).
        /// </summary>
        public static GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        ///     Спавнит префаб в заданной позиции, поворот identity.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position)
        {
            return Spawn(prefab, position, Quaternion.identity, null);
        }

        /// <summary>
        ///     Спавнит префаб в заданной позиции и повороте.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Spawn(prefab, position, rotation, null);
        }

        /// <summary>
        ///     Спавнит префаб в заданной позиции и повороте с указанным родителем.
        /// </summary>
        /// <param name="prefab">Префаб для спавна.</param>
        /// <param name="position">Мировая позиция.</param>
        /// <param name="rotation">Мировой поворот.</param>
        /// <param name="parent">Родитель (null — объекты складываются в общий корень пулов).</param>
        /// <returns>Экземпляр объекта или null, если prefab == null.</returns>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            if (PoolManager.I != null)
            {
                GameObject fromManager = PoolManager.Get(prefab, position, rotation, parent);
                if (fromManager != null)
                {
                    fromManager.SetActive(true);
                }

                return fromManager;
            }

            NeoObjectPool pool = GetOrCreateFallbackPool(prefab);
            GameObject pooled = pool.GetObject(position, rotation);
            pooled.SetActive(true);
            pooled.transform.SetParent(parent != null ? parent : FallbackRoot, true);
            return pooled;
        }

        /// <summary>
        ///     Спавнит префаб как дочерний объект parent в локальной позиции (0,0,0) и повороте identity.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent)
        {
            return Spawn(prefab, parent != null ? parent.position : Vector3.zero,
                parent != null ? parent.rotation : Quaternion.identity, parent);
        }

        private static NeoObjectPool GetOrCreateFallbackPool(GameObject prefab)
        {
            if (!FallbackPools.TryGetValue(prefab, out NeoObjectPool pool))
            {
                pool = new NeoObjectPool(prefab, DefaultInitialSize, DefaultExpandPool);
                FallbackPools[prefab] = pool;
            }

            return pool;
        }

        /// <summary>
        ///     Деспавнит объект: возвращает в пул (если объект из пула) или уничтожает.
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
                Object.Destroy(instance);
            }
        }

        /// <summary>
        ///     Очищает все fallback-пулы (созданные при отсутствии PoolManager). Уничтожает корневой объект пулов.
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
                Object.Destroy(_fallbackRoot.gameObject);
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
            SpawnUtility.OnSceneLoadedForHelper();
        }
    }
}
