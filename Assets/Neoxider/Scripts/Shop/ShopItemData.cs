using Neo.UI;
using UnityEngine;

namespace Neo
{
    namespace Shop
    {
        [CreateAssetMenu(fileName = "shopItemData", menuName = "Neoxider/ShopItemData", order = 32)]
        public class ShopItemData : ScriptableObject
        {
            [Tooltip("Можно ли купить этот товар только один раз?")]
            [SerializeField] private bool _isSinglePurchase = true;

            [Tooltip("Название товара, которое будет отображаться в магазине.")]
            [SerializeField] private string _nameItem;
            
            [Tooltip("Описание товара.")]
            [SerializeField] [TextArea(2, 4)] private string _description;

            [Tooltip("Начальная цена товара.")]
            [SerializeField] private int _price = 10;

            [Tooltip("Основное изображение товара (например, для превью).")]
            [SerializeField] private Sprite _sprite;

            [Tooltip("Иконка товара (например, для отображения в списке).")]
            [SerializeField] private Sprite _icon;

            public bool isSinglePurchase => _isSinglePurchase;
            public string nameItem => _nameItem;
            public int price => _price;
            public Sprite sprite => _sprite;
            public Sprite icon => _icon;
            public string description => _description;
        }
    }
}