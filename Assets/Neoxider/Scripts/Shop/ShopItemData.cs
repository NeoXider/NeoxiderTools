using Neoxider.UI;
using UnityEngine;

namespace Neoxider
{
    namespace Shop
    {
        [CreateAssetMenu(fileName = "shopItemData", menuName = "Neoxider/ShopItemData", order = 32)]
        public class ShopItemData : ScriptableObject
        {
            public bool isSinglePurchase => _isSinglePurchase;
            public string nameKey => _nameKey;
            public int[] price => _price;
            public Sprite[] spritesShop => _spritesShop;

            [SerializeField]
            private bool _isSinglePurchase = true;

            [SerializeField] private string _nameKey = "itemName";
            [SerializeField] private int[] _price = { 10 };
            [SerializeField] private Sprite[] _spritesShop;

            [Space, Header("save Price")]
            public int[] curentPrice;

            public void Load()
            {
                curentPrice = (int[])_price.Clone();

                for (int i = 0; i < curentPrice.Length; i++)
                {
                    if (_isSinglePurchase)
                    {
                        curentPrice[i] = PlayerPrefs.GetInt(nameof(ShopItemData) + _nameKey + i, _price[i]);
                    }
                    else
                    {
                        curentPrice[i] = _price[i];
                    }
                }
            }

            public void SaveAll()
            {
                for (int i = 0; i < curentPrice.Length; i++)
                {
                    PlayerPrefs.SetInt(nameof(ShopItemData) + _nameKey + i, curentPrice[i]);
                }
            }

            public void Save(int id, int price = 0)
            {
                if (_isSinglePurchase)
                {
                    curentPrice[id] = price;
                    PlayerPrefs.SetInt(nameof(ShopItemData) + _nameKey + id, curentPrice[id]);
                }
            }

            public void ResetSave()
            {
                curentPrice = (int[])_price.Clone();

                SaveAll();
            }
        }
    }
}
