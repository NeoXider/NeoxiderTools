using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo
{
    [Serializable]
    public class ObjectPool<T> where T : Object
    {
        [Header("Object Pool Settings")] [SerializeField]
        private T _item;

        [SerializeField] private int _initialPoolSize = 10;
        [SerializeField] private bool _expandPool = true;
        public List<T> items;

        private Queue<T> pool;

        public void Init(T item)
        {
            _item = item;
            pool = new Queue<T>();
            items = new List<T>();
            InitializePool();
        }

        private void InitializePool()
        {
            for (var i = 0; i < _initialPoolSize; i++) CreateNewObject();
        }

        public T GetObject(Vector3 position = default, Quaternion rotation = default)
        {
            T obj;

            if (pool.Count > 0)
                obj = pool.Dequeue();
            else if (_expandPool)
                obj = CreateNewObject();
            else
                return null;

            if (obj is GameObject gameObject)
            {
                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
                gameObject.SetActive(true);
            }

            items.Add(obj);

            return obj;
        }

        public void ReturnObject(T obj)
        {
            items.Remove(obj);
            if (obj is GameObject gameObject) gameObject.SetActive(false);
            pool.Enqueue(obj);
        }

        private T CreateNewObject()
        {
            var obj = Object.Instantiate(_item);
            if (obj is GameObject gameObject) gameObject.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        public void SetPrefab(T newPrefab)
        {
            _item = newPrefab;
            ClearPool();
            InitializePool();
        }

        public void ClearPool()
        {
            while (items.Count > 0) ReturnObject(items[0]);
        }

        public int GetPoolSize()
        {
            return pool.Count;
        }
    }
}