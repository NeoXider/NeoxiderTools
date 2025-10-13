using UnityEngine;
using UnityEngine.Pool;

namespace Neo.Tools
{
    /// <summary>
    /// Управляет пулом для одного конкретного префаба, вызывая методы интерфейса IPoolable.
    /// </summary>
    public class NeoObjectPool
    {
        private readonly GameObject _prefab;
        private readonly IObjectPool<GameObject> _pool;

        public int CountInactive => _pool.CountInactive;

        public NeoObjectPool(GameObject prefab, int initialSize, bool expandPool)
        {
            _prefab = prefab;

            _pool = new ObjectPool<GameObject>(
                createFunc: CreatePooledObject,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyObject,
                collectionCheck: true, // Защита от двойного возврата в пул
                defaultCapacity: initialSize,
                maxSize: expandPool ? 10000 : initialSize
            );

            // "Прогреваем" пул, чтобы избежать лагов при первом использовании
            var prewarmList = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < initialSize; i++)
            {
                prewarmList.Add(_pool.Get());
            }
            foreach (var item in prewarmList)
            {
                _pool.Release(item);
            }
        }

        private GameObject CreatePooledObject()
        {
            GameObject instance = Object.Instantiate(_prefab);

            // Вызываем метод инициализации у всех компонентов, реализующих IPoolable
            var poolableComponents = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolableComponents)
            {
                poolable.OnPoolCreate();
            }

            return instance;
        }

        private void OnGetFromPool(GameObject instance)
        {
            // Вызываем метод "при взятии" у всех компонентов
            var poolableComponents = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolableComponents)
            {
                poolable.OnPoolGet();
            }

            instance.SetActive(true);
        }

        private void OnReleaseToPool(GameObject instance)
        {
            // Вызываем метод "при возврате" у всех компонентов
            var poolableComponents = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolableComponents)
            {
                poolable.OnPoolRelease();
            }

            instance.SetActive(false);
        }

        private void OnDestroyObject(GameObject instance)
        {
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
        }
    }
}
