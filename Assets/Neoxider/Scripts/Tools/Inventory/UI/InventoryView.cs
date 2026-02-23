using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    public enum InventoryViewMode
    {
        SpawnFromPrefab = 0,
        ManualList = 1
    }

    public enum InventoryViewSourceMode
    {
        DatabaseItems = 0,
        SnapshotItems = 1,
        Hybrid = 2
    }

    [NeoDoc("Tools/Inventory/InventoryView.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryView")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryView))]
    public sealed class InventoryView : MonoBehaviour
    {
        [Header("Source")] [SerializeField] private InventoryComponent _inventory;
        [SerializeField] private InventoryViewMode _viewMode = InventoryViewMode.SpawnFromPrefab;
        [SerializeField] private InventoryViewSourceMode _sourceMode = InventoryViewSourceMode.Hybrid;
        [SerializeField] private bool _showOnlyNonZero = true;
        [SerializeField] private bool _refreshOnLoaded = true;
        [SerializeField] private bool _refreshNextFrameOnEnable = true;

        [Header("Spawn Mode")] [SerializeField] private InventoryItemView _itemViewPrefab;
        [SerializeField] private Transform _itemsRoot;

        [Header("Manual Mode")] [SerializeField] private List<InventoryItemView> _manualViews = new();

        private readonly Dictionary<int, InventoryItemView> _spawnedByItemId = new();

        private void OnEnable()
        {
            BindIfNeeded();
            Subscribe();
            Refresh();
            if (_refreshNextFrameOnEnable)
            {
                StartCoroutine(RefreshNextFrame());
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetInventory(InventoryComponent inventory)
        {
            if (_inventory == inventory)
            {
                return;
            }

            Unsubscribe();
            _inventory = inventory;
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            if (_inventory == null)
            {
                ClearAllViews();
                return;
            }

            if (_viewMode == InventoryViewMode.SpawnFromPrefab)
            {
                RefreshSpawnMode();
                return;
            }

            RefreshManualMode();
        }

        private void RefreshSpawnMode()
        {
            if (_itemViewPrefab == null || _itemsRoot == null)
            {
                return;
            }

            List<int> itemIds = BuildDisplayItemIds();
            for (int i = 0; i < itemIds.Count; i++)
            {
                int itemId = itemIds[i];
                InventoryItemData data = _inventory.GetItemData(itemId);
                int count = _inventory.GetCount(itemId);
                bool shouldShow = !_showOnlyNonZero || count > 0;

                if (!shouldShow)
                {
                    HideOrDestroySpawned(itemId);
                    continue;
                }

                InventoryItemView view = GetOrCreateSpawnedView(itemId);
                view.Bind(data, itemId, count);
            }
        }

        private void RefreshManualMode()
        {
            for (int i = 0; i < _manualViews.Count; i++)
            {
                InventoryItemView view = _manualViews[i];
                if (view == null)
                {
                    continue;
                }

                int itemId = view.BoundItemId;
                if (itemId < 0)
                {
                    view.Clear();
                    continue;
                }

                InventoryItemData data = _inventory.GetItemData(itemId);
                int count = _inventory.GetCount(itemId);
                bool shouldShow = !_showOnlyNonZero || count > 0;
                if (!shouldShow)
                {
                    view.Clear();
                    continue;
                }

                view.Bind(data, itemId, count);
            }
        }

        private InventoryItemView GetOrCreateSpawnedView(int itemId)
        {
            if (_spawnedByItemId.TryGetValue(itemId, out InventoryItemView cached))
            {
                if (cached != null)
                {
                    cached.gameObject.SetActive(true);
                    return cached;
                }

                _spawnedByItemId.Remove(itemId);
            }

            InventoryItemView created = Instantiate(_itemViewPrefab, _itemsRoot);
            _spawnedByItemId[itemId] = created;
            return created;
        }

        private void HideOrDestroySpawned(int itemId)
        {
            if (_spawnedByItemId.TryGetValue(itemId, out InventoryItemView view) && view != null)
            {
                view.gameObject.SetActive(false);
            }
        }

        private void ClearAllViews()
        {
            foreach (KeyValuePair<int, InventoryItemView> pair in _spawnedByItemId)
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _manualViews.Count; i++)
            {
                if (_manualViews[i] != null)
                {
                    _manualViews[i].Clear();
                }
            }
        }

        private void Subscribe()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged.AddListener(Refresh);
                if (_refreshOnLoaded)
                {
                    _inventory.OnLoaded.AddListener(Refresh);
                }
            }
        }

        private void Unsubscribe()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged.RemoveListener(Refresh);
                if (_refreshOnLoaded)
                {
                    _inventory.OnLoaded.RemoveListener(Refresh);
                }
            }
        }

        private void BindIfNeeded()
        {
            if (_inventory == null)
            {
                _inventory = InventoryComponent.FindDefault();
            }

            if (_itemsRoot == null)
            {
                _itemsRoot = transform;
            }
        }

        private List<int> BuildDisplayItemIds()
        {
            List<int> ordered = new();
            HashSet<int> seen = new();

            if ((_sourceMode == InventoryViewSourceMode.DatabaseItems || _sourceMode == InventoryViewSourceMode.Hybrid) &&
                _inventory.Database != null && _inventory.Database.Items != null)
            {
                for (int i = 0; i < _inventory.Database.Items.Count; i++)
                {
                    InventoryItemData data = _inventory.Database.Items[i];
                    if (data != null && seen.Add(data.ItemId))
                    {
                        ordered.Add(data.ItemId);
                    }
                }
            }

            if (_sourceMode == InventoryViewSourceMode.SnapshotItems || _sourceMode == InventoryViewSourceMode.Hybrid)
            {
                List<InventoryEntry> snapshot = _inventory.GetSnapshotEntries();
                for (int i = 0; i < snapshot.Count; i++)
                {
                    InventoryEntry entry = snapshot[i];
                    if (entry != null && entry.Count > 0 && seen.Add(entry.ItemId))
                    {
                        ordered.Add(entry.ItemId);
                    }
                }
            }

            return ordered;
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }
    }
}
