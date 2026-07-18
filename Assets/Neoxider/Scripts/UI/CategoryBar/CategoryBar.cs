using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.UI
{
    /// <summary>
    ///     Reusable horizontal/tab category bar that owns selection state and a configurable selected
    ///     visual. Works with any consumer through id/index events — it has no dependency on the Shop
    ///     module (see <c>Neo.Shop.ShopListViewCategoryBar</c> for the optional adapter).
    ///     Item views are either authored children carrying <see cref="CategoryBarItem"/> (matched to
    ///     categories by index) or spawned from an optional prefab; the shared selection marker is
    ///     re-parented onto the selected item with an offset, so authored graphics are never resized or
    ///     repositioned. Supports initial selection, runtime selection by index/id, previous/next
    ///     navigation, disabled entries, and Inspector-authored or runtime-provided category lists.
    /// </summary>
    [NeoDoc("UI/CategoryBar.md")]
    [CreateFromMenu("Neoxider/UI/CategoryBar")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(CategoryBar))]
    public sealed class CategoryBar : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Category id reported through OnCategoryIdSelected (also the Select(string) key).")]
            public string Id = "";

            [Tooltip("Label bound to the item view (optional).")]
            public string DisplayName = "";

            [Tooltip("Icon bound to the item view (optional).")]
            public Sprite Icon;

            [Tooltip("Disabled entries stay visible but cannot be selected and are skipped by Next/Prev.")]
            public bool Disabled;
        }

        [Header("Categories")]
        [Tooltip("Inspector-authored categories; replaceable at runtime via SetCategories.")]
        [SerializeField]
        private List<Entry> _categories = new();

        [Tooltip("Entry selected on Start (clamped; disabled entries fall through to the next enabled one).")]
        [SerializeField]
        [Min(0)]
        private int _startIndex;

        [Tooltip("Next/Prev wrap around the ends of the list.")] [SerializeField]
        private bool _wrapNavigation = true;

        [Header("Item views")]
        [Tooltip("Authored item views matched to categories by index. Auto-collected from children when empty and no prefab is set.")]
        [SerializeField]
        private List<CategoryBarItem> _items = new();

        [Tooltip("Optional prefab; when set, item views are spawned under Items Root (authored list is ignored).")]
        [SerializeField]
        private CategoryBarItem _itemPrefab;

        [Tooltip("Parent for spawned item views; defaults to this transform.")] [SerializeField]
        private Transform _itemsRoot;

        [Header("Selected visual")]
        [Tooltip("Shared marker/frame moved onto the selected item (re-parented, never resized).")]
        [SerializeField]
        private RectTransform _selectionMarker;

        [Tooltip("Anchored offset of the marker relative to the selected item.")] [SerializeField]
        private Vector2 _selectionMarkerOffset;

        [Space] public UnityEvent<int> OnCategorySelected = new();
        public UnityEvent<string> OnCategoryIdSelected = new();

        private readonly List<CategoryBarItem> _spawnedItems = new();
        private readonly Dictionary<CategoryBarItem, UnityAction> _clickHandlers = new();
        private int _index = -1;
        private bool _started;

        /// <summary>Selected entry index, or -1 when nothing is selected.</summary>
        public int CurrentIndex => _index;

        /// <summary>Selected category id, or empty when nothing is selected.</summary>
        public string CurrentCategoryId =>
            _index >= 0 && _index < _categories.Count && _categories[_index] != null
                ? _categories[_index].Id
                : "";

        /// <summary>Current category list (Inspector-authored or the last SetCategories call).</summary>
        public IReadOnlyList<Entry> Categories => _categories;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        ///     Builds item views and applies the initial selection. Runs automatically on Start;
        ///     call it earlier when configuring the bar from code before its first frame. Idempotent.
        /// </summary>
        public void Initialize()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            RebuildItems();
            SelectFirstEnabledFrom(_startIndex);
        }

        /// <summary>
        ///     Replaces the category list at runtime and rebuilds item views. Pass a negative
        ///     <paramref name="initialIndex"/> to keep the current selection when its id still exists.
        /// </summary>
        public void SetCategories(IEnumerable<Entry> categories, int initialIndex = -1)
        {
            string previousId = CurrentCategoryId;

            _categories.Clear();
            if (categories != null)
            {
                foreach (Entry entry in categories)
                {
                    if (entry != null)
                    {
                        _categories.Add(entry);
                    }
                }
            }

            _index = -1;
            if (!_started)
            {
                return; // WHY: Start will rebuild and apply the initial selection.
            }

            RebuildItems();

            if (initialIndex >= 0)
            {
                SelectFirstEnabledFrom(initialIndex);
                return;
            }

            if (!string.IsNullOrEmpty(previousId) && Select(previousId))
            {
                return;
            }

            SelectFirstEnabledFrom(0);
        }

        /// <summary>Enables/disables one entry; a disabled current selection stays until changed.</summary>
        public void SetEntryDisabled(int index, bool disabled)
        {
            if (index < 0 || index >= _categories.Count || _categories[index] == null)
            {
                return;
            }

            _categories[index].Disabled = disabled;
            CategoryBarItem item = GetItem(index);
            if (item != null)
            {
                item.SetInteractable(!disabled);
            }
        }

        /// <summary>Selects by index. Returns false for out-of-range or disabled entries.</summary>
        public bool Select(int index)
        {
            if (index < 0 || index >= _categories.Count)
            {
                return false;
            }

            Entry entry = _categories[index];
            if (entry == null || entry.Disabled)
            {
                return false;
            }

            bool changed = _index != index;
            _index = index;
            ApplySelectionVisuals();

            if (changed)
            {
                OnCategorySelected?.Invoke(_index);
                OnCategoryIdSelected?.Invoke(entry.Id);
            }

            return true;
        }

        /// <summary>Selects by category id (ordinal match). Returns false when absent or disabled.</summary>
        public bool Select(string categoryId)
        {
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i] != null &&
                    string.Equals(_categories[i].Id, categoryId, StringComparison.Ordinal))
                {
                    return Select(i);
                }
            }

            return false;
        }

        /// <summary>Selects the next enabled entry (wraps when Wrap Navigation is on).</summary>
        [Button]
        public void Next()
        {
            Step(1);
        }

        /// <summary>Selects the previous enabled entry (wraps when Wrap Navigation is on).</summary>
        [Button]
        public void Prev()
        {
            Step(-1);
        }

        private void Step(int direction)
        {
            int count = _categories.Count;
            if (count == 0)
            {
                return;
            }

            int start = _index < 0 ? (direction > 0 ? -1 : count) : _index;
            int steps = _wrapNavigation ? count : direction > 0 ? count - 1 - start : start;

            int candidate = start;
            for (int i = 0; i < steps; i++)
            {
                candidate += direction;
                if (_wrapNavigation)
                {
                    candidate = (candidate + count) % count;
                }
                else if (candidate < 0 || candidate >= count)
                {
                    return;
                }

                Entry entry = _categories[candidate];
                if (entry != null && !entry.Disabled)
                {
                    Select(candidate);
                    return;
                }
            }
        }

        private void SelectFirstEnabledFrom(int index)
        {
            int count = _categories.Count;
            if (count == 0)
            {
                ApplySelectionVisuals();
                return;
            }

            index = Mathf.Clamp(index, 0, count - 1);
            for (int i = 0; i < count; i++)
            {
                int candidate = (index + i) % count;
                if (Select(candidate))
                {
                    return;
                }
            }

            ApplySelectionVisuals(); // WHY: everything disabled: keep no selection, hide the marker
        }

        private void RebuildItems()
        {
            if (_itemPrefab != null)
            {
                RebuildSpawnedItems();
            }
            else if (_items.Count == 0)
            {
                _items.AddRange(GetComponentsInChildren<CategoryBarItem>(true));
            }

            for (int i = 0; i < _categories.Count; i++)
            {
                CategoryBarItem item = GetItem(i);
                Entry entry = _categories[i];
                if (item == null || entry == null)
                {
                    continue;
                }

                item.Bind(entry.DisplayName, entry.Icon);
                item.SetInteractable(!entry.Disabled);
                BindClick(item, i);
            }

            ApplySelectionVisuals();
        }

        private void RebuildSpawnedItems()
        {
            // WHY: the shared marker is re-parented under the selected item; without detaching it
            // first, destroying the spawned items would destroy the marker with them.
            if (_selectionMarker != null && _selectionMarker.transform.parent != transform)
            {
                _selectionMarker.SetParent(transform, false);
                _selectionMarker.gameObject.SetActive(false);
            }

            foreach (CategoryBarItem spawned in _spawnedItems)
            {
                if (spawned != null)
                {
                    _clickHandlers.Remove(spawned);
                    Destroy(spawned.gameObject);
                }
            }

            _spawnedItems.Clear();

            Transform root = _itemsRoot != null ? _itemsRoot : transform;
            for (int i = 0; i < _categories.Count; i++)
            {
                CategoryBarItem item = Instantiate(_itemPrefab, root);
                item.gameObject.SetActive(true);
                _spawnedItems.Add(item);
            }
        }

        private void BindClick(CategoryBarItem item, int index)
        {
            if (item.Button == null)
            {
                return;
            }

            if (_clickHandlers.TryGetValue(item, out UnityAction previous))
            {
                item.Button.onClick.RemoveListener(previous);
            }

            UnityAction handler = () => Select(index);
            _clickHandlers[item] = handler;
            item.Button.onClick.AddListener(handler);
        }

        private CategoryBarItem GetItem(int index)
        {
            List<CategoryBarItem> source = _itemPrefab != null ? _spawnedItems : _items;
            return index >= 0 && index < source.Count ? source[index] : null;
        }

        private void ApplySelectionVisuals()
        {
            List<CategoryBarItem> source = _itemPrefab != null ? _spawnedItems : _items;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    source[i].SetSelected(i == _index);
                }
            }

            if (_selectionMarker == null)
            {
                return;
            }

            CategoryBarItem selected = GetItem(_index);
            if (selected == null)
            {
                _selectionMarker.gameObject.SetActive(false);
                return;
            }

            _selectionMarker.gameObject.SetActive(true);
            _selectionMarker.SetParent(selected.transform, false);
            _selectionMarker.anchoredPosition = _selectionMarkerOffset;
        }
    }
}
