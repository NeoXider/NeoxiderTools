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
        [Tooltip("Можно ли купить этот товар только один раз?")] [SerializeField]
        private bool _isSinglePurchase = true;

        [Tooltip("Название товара, которое будет отображаться в магазине")] [SerializeField]
        private string _nameItem;

        [Tooltip("Описание товара")] [SerializeField] [TextArea(2, 4)]
        private string _description;

        [Tooltip("Начальная цена товара")] [SerializeField]
        private int _price = 10;

        [Tooltip("Основное изображение товара (например, для превью)")] [SerializeField]
        private Sprite _sprite;

        [Tooltip("Иконка товара (например, для отображения в списке)")] [SerializeField]
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