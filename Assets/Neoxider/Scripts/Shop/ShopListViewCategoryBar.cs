using System.Collections.Generic;
using Neo.UI;
using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     Optional adapter that drives a <see cref="ShopListView"/> from a generic
    ///     <see cref="CategoryBar"/>: whenever the bar selection changes, the list view switches to the
    ///     selected category id (empty id shows all items). Keeps <see cref="CategoryBar"/> free of any
    ///     Shop dependency — attach this next to the bar (or anywhere) and assign both references.
    /// </summary>
    [NeoDoc("Shop/ShopListViewCategoryBar.md")]
    [CreateFromMenu("Neoxider/Shop/ShopListViewCategoryBar")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopListViewCategoryBar))]
    public sealed class ShopListViewCategoryBar : MonoBehaviour
    {
        [Tooltip("Category bar whose selection drives the list view; auto-resolved from this object.")]
        [SerializeField]
        private CategoryBar _categoryBar;

        [Tooltip("Shop list view switched on selection; auto-resolved from parents when empty.")]
        [SerializeField]
        private ShopListView _listView;

        [Header("Auto categories (optional)")]
        [Tooltip("Build the bar's category list from the Shop catalog (distinct ShopItemData.Category values) on enable.")]
        [SerializeField]
        private bool _buildCategoriesFromShop;

        [Tooltip("When building from the shop, prepend an entry with an empty id that shows all items.")]
        [SerializeField]
        private bool _includeAllEntry = true;

        [Tooltip("Display name of the show-all entry.")] [SerializeField]
        private string _allEntryName = "All";

        private void Awake()
        {
            if (_categoryBar == null)
            {
                _categoryBar = GetComponent<CategoryBar>();
            }

            if (_listView == null)
            {
                _listView = GetComponentInParent<ShopListView>();
            }
        }

        private void OnEnable()
        {
            if (_categoryBar == null)
            {
                return;
            }

            if (_buildCategoriesFromShop)
            {
                BuildCategoriesFromShop();
            }

            _categoryBar.OnCategoryIdSelected.AddListener(ApplyCategory);
            if (_categoryBar.CurrentIndex >= 0)
            {
                ApplyCategory(_categoryBar.CurrentCategoryId);
            }
        }

        /// <summary>
        ///     Rebuilds the bar's entries from the Shop catalog: one entry per distinct
        ///     <c>ShopItemData.Category</c> (first-seen order), optionally preceded by a show-all
        ///     entry with an empty id. Call again after <c>Shop.SetItems</c> to refresh.
        /// </summary>
        public void BuildCategoriesFromShop()
        {
            Shop shop = _listView != null ? _listView.Shop : null;
            if (shop == null || _categoryBar == null)
            {
                return;
            }

            var entries = new List<CategoryBar.Entry>();
            if (_includeAllEntry)
            {
                entries.Add(new CategoryBar.Entry { Id = "", DisplayName = _allEntryName });
            }

            foreach (string category in shop.GetCategories())
            {
                entries.Add(new CategoryBar.Entry { Id = category, DisplayName = category });
            }

            _categoryBar.SetCategories(entries);
        }

        private void OnDisable()
        {
            if (_categoryBar != null)
            {
                _categoryBar.OnCategoryIdSelected.RemoveListener(ApplyCategory);
            }
        }

        private void ApplyCategory(string categoryId)
        {
            if (_listView != null)
            {
                _listView.SetCategory(categoryId ?? "");
            }
        }
    }
}
