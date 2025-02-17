using Neo.UI;
using UnityEngine;

namespace Neo
{
    namespace Shop
    {
        [CreateAssetMenu(fileName = "shopItemData", menuName = "Neoxider/ShopItemData", order = 32)]
        public class ShopItemData : ScriptableObject
        {
            [SerializeField]
            private bool _isSinglePurchase = true;

            [SerializeField] private string _nameItem;
            [SerializeField, TextArea(2, 4)] private string _description;
            [SerializeField] private int _price = 10;
            [SerializeField] private Sprite _sprite;
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
