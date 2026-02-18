using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Редкость коллекционного предмета.
    /// </summary>
    public enum ItemRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    /// <summary>
    ///     ScriptableObject для хранения данных одного коллекционного предмета.
    ///     Используется в системе коллекций для определения свойств предмета.
    /// </summary>
    [CreateAssetMenu(fileName = "Item Collection Data", menuName = "Neoxider/Bonus/Collection/Item Collection Data",
        order = 10)]
    public class ItemCollectionData : ScriptableObject
    {
        [Tooltip("Display name of the collection item")] [SerializeField]
        private string _itemName;

        [Tooltip("Item description")] [TextArea(1, 5)] [SerializeField]
        private string _description;

        [Tooltip("Item icon for display in collection")] [SerializeField]
        private Sprite _sprite;

        [Tooltip("Item type (numeric identifier)")] [SerializeField]
        private int _itemType;

        [Tooltip("Item rarity")] [SerializeField]
        private ItemRarity _rarity = ItemRarity.Common;

        [Tooltip("Item category (numeric identifier)")] [SerializeField]
        private int _category;

        public string ItemName => _itemName;
        public string Description => _description;
        public Sprite Sprite => _sprite;
        public int ItemType => _itemType;
        public ItemRarity Rarity => _rarity;
        public int Category => _category;
    }
}