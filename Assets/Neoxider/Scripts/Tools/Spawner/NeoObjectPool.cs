using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Neo.Tools
{
    /// <summary>
    ///     Управляет пулом для одного конкретного префаба, вызывая методы интерфейса IPoolable.
    /// </summary>
    public class NeoObjectPool
    {
        private readonly Dictionary<GameObject, IPoolable[]> _cachedComponents = new();
        private readonly IObjectPool<GameObject> _pool;
        private readonly GameObject _prefab;

        public NeoObjectPool(GameObject prefab, int initialSize, bool expandPool)
        {
            _prefab = prefab;

            _pool = new ObjectPool<GameObject>(
                CreatePooledObject,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyObject,
                true, // Защита от двойного возврата в пул
                initialSize,
                expandPool ? 10000 : initialSize
            );

            // "Прогреваем" пул, чтобы избежать лагов при первом использовании
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
        ///     Получает компоненты IPoolable с кэшированием для оптимизации производительности.
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

            // Вызываем метод инициализации у всех компонентов, реализующих IPoolable
            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                poolable.OnPoolCreate();
            }

            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            // Вызываем метод "при взятии" у всех компонентов
            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                poolable.OnPoolGet();
            }

            instance.SetActive(true);
        }

        private void OnReleaseToPool(GameObject instance)
        {
            // Вызываем метод "при возврате" у всех компонентов
            IPoolable[] poolableComponents = GetPoolableComponents(instance);
            foreach (IPoolable poolable in poolableComponents)
            {
                poolable.OnPoolRelease();
            }

            instance.SetActive(false);
        }

        private void OnDestroyObject(GameObject instance)
        {
            // Очищаем кэш при уничтожении объекта
            _cachedComponents.Remove(instance);
            Object.Destroy(instance);
        }

        public GameObject GetObject(Vector3 position, Quaternion rotation)
        {
            GameObject instance = _pool.Get();
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