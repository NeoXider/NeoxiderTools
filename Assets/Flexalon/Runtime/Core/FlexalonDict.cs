using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    [Serializable]
    internal class FlexalonDict<K, V> : ISerializationCallbackReceiver
    {
        private Dictionary<K, V> _dict = new Dictionary<K, V>();

        [SerializeField]
        private List<K> _keys = new List<K>();

        [SerializeField]
        private List<V> _values = new List<V>();

        public void Add(K key, V value)
        {
            _dict.Add(key, value);
        }

        public bool TryGetValue(K key, out V value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public int Count => _dict.Count;

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            foreach (var kvp in _dict)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            _dict.Clear();
            for (int i = 0; i < _keys.Count; i++)
            {
                _dict.Add(_keys[i], _values[i]);
            }

            _keys.Clear();
            _values.Clear();
        }
    }
}