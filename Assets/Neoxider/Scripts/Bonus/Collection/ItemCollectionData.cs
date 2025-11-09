using UnityEngine;

namespace Neo.Bonus
{
    public enum ItemRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    [CreateAssetMenu(fileName = "ItemClnData", menuName = "Neoxider/ItemCollectionData")]
    public class ItemCollectionData : ScriptableObject
    {
        [SerializeField] private string _itemName;
        [TextArea(1, 5)] [SerializeField] private string _description;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int _itemType;
        [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
        [SerializeField] private int _category = 0;

        public string itemName => _itemName;
        public string description => _description;
        public Sprite sprite => _sprite;
        public int itemType => _itemType;
        public ItemRarity rarity => _rarity;
        public int category => _category;
    }
}