using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     База предметов инвентаря для валидации itemId и получения данных по id.
    /// </summary>
    [CreateAssetMenu(fileName = "Inventory Database", menuName = "Neoxider/Tools/Inventory/Inventory Database",
        order = 21)]
    public sealed class InventoryDatabase : ScriptableObject
    {
        [SerializeField] [Tooltip("List of item definitions available in this inventory domain.")]
        private List<InventoryItemData> _items = new();

        private Dictionary<int, InventoryItemData> _cacheById;

        public IReadOnlyList<InventoryItemData> Items => _items;

        public bool ContainsId(int itemId)
        {
            BuildCacheIfNeeded();
            return _cacheById.ContainsKey(itemId);
        }

        public InventoryItemData GetItemData(int itemId)
        {
            BuildCacheIfNeeded();
            _cacheById.TryGetValue(itemId, out InventoryItemData data);
            return data;
        }

        public bool TryGetItemData(int itemId, out InventoryItemData data)
        {
            BuildCacheIfNeeded();
            return _cacheById.TryGetValue(itemId, out data);
        }

        private void OnValidate()
        {
            _cacheById = null;
            ValidateDuplicateIds();
        }

        private void BuildCacheIfNeeded()
        {
            if (_cacheById != null)
            {
                return;
            }

            _cacheById = new Dictionary<int, InventoryItemData>();
            if (_items == null)
            {
                return;
            }

            foreach (InventoryItemData item in _items)
            {
                if (item == null)
                {
                    continue;
                }

                _cacheById[item.ItemId] = item;
            }
        }

        private void ValidateDuplicateIds()
        {
#if UNITY_EDITOR
            if (_items == null)
            {
                return;
            }

            HashSet<int> seen = new();
            for (int i = 0; i < _items.Count; i++)
            {
                InventoryItemData item = _items[i];
                if (item == null)
                {
                    continue;
                }

                if (!seen.Add(item.ItemId))
                {
                    Debug.LogWarning($"[InventoryDatabase] Duplicate item id: {item.ItemId}", item);
                }
            }
#endif
        }
    }
}
