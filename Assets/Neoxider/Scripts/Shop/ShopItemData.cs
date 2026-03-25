using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     ScriptableObject that holds shop item data.
    ///     Lets you configure item properties in the Inspector without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "Shop Item Data", menuName = "Neoxider/Shop/Shop Item Data", order = 32)]
    public class ShopItemData : ScriptableObject
    {
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
    }
}
