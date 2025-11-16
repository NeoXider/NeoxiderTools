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
    [CreateAssetMenu(fileName = "Item Collection Data", menuName = "Neo/Bonus/Collection/Item Collection Data", order = 10)]
    public class ItemCollectionData : ScriptableObject
    {
        [Tooltip("Название коллекционного предмета")]
        [SerializeField] private string _itemName;
        
        [Tooltip("Описание предмета")]
        [TextArea(1, 5)] 
        [SerializeField] private string _description;
        
        [Tooltip("Иконка предмета для отображения в коллекции")]
        [SerializeField] private Sprite _sprite;
        
        [Tooltip("Тип предмета (числовой идентификатор)")]
        [SerializeField] private int _itemType;
        
        [Tooltip("Редкость предмета")]
        [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
        
        [Tooltip("Категория предмета (числовой идентификатор)")]
        [SerializeField] private int _category;

        public string itemName => _itemName;
        public string description => _description;
        public Sprite sprite => _sprite;
        public int itemType => _itemType;
        public ItemRarity rarity => _rarity;
        public int category => _category;
    }
}