using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     ScriptableObject для хранения данных товара в магазине.
    ///     Позволяет настраивать свойства товара через инспектор без изменения кода.
    /// </summary>
    [CreateAssetMenu(fileName = "Shop Item Data", menuName = "Neo/Shop/Shop Item Data", order = 32)]
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
        ///     Можно ли купить этот товар только один раз?
        /// </summary>
        public bool isSinglePurchase => _isSinglePurchase;

        /// <summary>
        ///     Название товара.
        /// </summary>
        public string nameItem => _nameItem;

        /// <summary>
        ///     Цена товара.
        /// </summary>
        public int price => _price;

        /// <summary>
        ///     Основное изображение товара.
        /// </summary>
        public Sprite sprite => _sprite;

        /// <summary>
        ///     Иконка товара.
        /// </summary>
        public Sprite icon => _icon;

        /// <summary>
        ///     Описание товара.
        /// </summary>
        public string description => _description;
    }
}