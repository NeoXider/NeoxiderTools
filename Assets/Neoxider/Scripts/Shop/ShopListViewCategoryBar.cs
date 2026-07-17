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
            if (_categoryBar != null)
            {
                _categoryBar.OnCategoryIdSelected.AddListener(ApplyCategory);
                if (_categoryBar.CurrentIndex >= 0)
                {
                    ApplyCategory(_categoryBar.CurrentCategoryId);
                }
            }
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
