using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     ScriptableObject that holds shop item data.
    ///     Lets you configure item properties in the Inspector without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "Shop Item Data", menuName = "Neoxider/Shop/Shop Item Data", order = 32)]
    [NeoDoc("Shop/ShopItemData.md")]
    public class ShopItemData : ScriptableObject
    {
        [Tooltip("Stable identifier used as save key and shop lookup id. Auto-filled from name on validate.")]
        [SerializeField]
        private string _id = "";

        [Tooltip("Can this item be bought only once?")] [SerializeField]
        private bool _isSinglePurchase = true;

        [Tooltip("Item name shown in shop")] [SerializeField]
        private string _nameItem;

        [Tooltip("Item description")] [SerializeField] [TextArea(2, 4)]
        private string _description;

        [Tooltip("Initial item price")] [SerializeField]
        private int _price = 10;

        [Tooltip("Main item image (e.g. for preview)")] [SerializeField]
        private Sprite _sprite;

        [Tooltip("Item icon (e.g. for list display)")] [SerializeField]
        private Sprite _icon;

        [Tooltip("Optional grouping label. Used by Shop.GetItemsInCategory(...) and UI filters.")] [SerializeField]
        private string _category = "";

        [Tooltip("Optional Money.SaveKey used for this item currency. Empty = use Shop default currency.")]
        [SerializeField]
        private string _currencyOverrideSaveKey = "";

        /// <summary>
        ///     Stable identifier. Auto-filled from <see cref="nameItem"/> on validate when empty.
        /// </summary>
        public string Id => _id;

        /// <summary>
        ///     Whether this item can be bought only once.
        /// </summary>
        public bool isSinglePurchase => _isSinglePurchase;

        /// <summary>
        ///     Item display name.
        /// </summary>
        public string nameItem => _nameItem;

        /// <summary>
        ///     Item price.
        /// </summary>
        public int price => _price;

        /// <summary>
        ///     Main item sprite (e.g. for preview).
        /// </summary>
        public Sprite sprite => _sprite;

        /// <summary>
        ///     Item icon sprite.
        /// </summary>
        public Sprite icon => _icon;

        /// <summary>
        ///     Item description text.
        /// </summary>
        public string description => _description;

        /// <summary>
        ///     Optional category. Empty string = no category.
        /// </summary>
        public string Category => _category;

        /// <summary>
        ///     Optional per-item Money.SaveKey. Empty means use the Shop default.
        /// </summary>
        public string CurrencyOverrideSaveKey => _currencyOverrideSaveKey;

        /// <summary>
        ///     Writes <paramref name="resolvedId"/> into <see cref="_id"/> only while it is empty.
        ///     Used by <see cref="Shop"/> so saves and ownership resolve even if the asset forgot Id.
        /// </summary>
        public void AssignIdIfEmpty(string resolvedId)
        {
            if (!string.IsNullOrWhiteSpace(_id))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(resolvedId))
            {
                return;
            }

            _id = resolvedId.Trim();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(_nameItem))
            {
                _id = _nameItem.Replace(" ", "_");
            }
        }
    }
}
