using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Shop
{
    /// <summary>
    ///     NoCode single-pill category selector with prev/next arrows.
    ///     Cycles through a serialized category list and drives a <see cref="ShopListView"/>.
    ///     Complements <see cref="ShopCategoryButton"/> (one button per category) for shops where
    ///     categories are browsed sequentially instead of via tabs.
    /// </summary>
    [NeoDoc("Shop/ShopCategorySelector.md")]
    [CreateFromMenu("Neoxider/Shop/ShopCategorySelector")]
    [AddComponentMenu("Neoxider/Shop/" + nameof(ShopCategorySelector))]
    public sealed class ShopCategorySelector : MonoBehaviour
    {
        [Serializable]
        public sealed class Category
        {
            [Tooltip("Category id matching ShopItemData.Category. Empty = show all items.")]
            public string id = "";

            [Tooltip("Label shown in the pill.")]
            public string displayName = "";

            [Tooltip("Optional icon shown next to the label.")]
            public Sprite icon;
        }

        [Header("Target")] [SerializeField] private ShopListView _listView;

        [Header("UI")] [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("Categories")] [SerializeField]
        private Category[] _categories = Array.Empty<Category>();

        [SerializeField] [Min(0)] private int _startIndex;

        private int _index;

        /// <summary>Currently selected category id (empty when the list is empty).</summary>
        public string CurrentCategoryId =>
            _categories.Length > 0 ? _categories[_index].id : "";

        private void Start()
        {
            if (_prevButton != null)
            {
                _prevButton.onClick.AddListener(Prev);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(Next);
            }

            _index = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, _categories.Length - 1));
            Apply();
        }

        private void OnDestroy()
        {
            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveListener(Prev);
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(Next);
            }
        }

        /// <summary>Select the next category (wraps around).</summary>
        [Button]
        public void Next()
        {
            if (_categories.Length == 0)
            {
                return;
            }

            _index = (_index + 1) % _categories.Length;
            Apply();
        }

        /// <summary>Select the previous category (wraps around).</summary>
        [Button]
        public void Prev()
        {
            if (_categories.Length == 0)
            {
                return;
            }

            _index = (_index - 1 + _categories.Length) % _categories.Length;
            Apply();
        }

        /// <summary>Select a category by id; ignored when the id is not in the list.</summary>
        public void Select(string categoryId)
        {
            for (int i = 0; i < _categories.Length; i++)
            {
                if (_categories[i] != null && string.Equals(_categories[i].id, categoryId, StringComparison.Ordinal))
                {
                    _index = i;
                    Apply();
                    return;
                }
            }
        }

        private void Apply()
        {
            if (_categories.Length == 0)
            {
                return;
            }

            Category category = _categories[_index];
            if (category == null)
            {
                return;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = category.icon;
                _iconImage.enabled = category.icon != null;
            }

            if (_nameText != null)
            {
                _nameText.text = category.displayName;
            }

            if (_listView != null)
            {
                _listView.SetCategory(category.id);
            }
        }
    }
}
