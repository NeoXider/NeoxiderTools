using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     One equippable item (dress-up piece, skin, accessory): id + category + sprite.
    ///     Referenced by <see cref="EquipmentManager"/>; pair the id with a
    ///     <see cref="ShopItemData"/> id to gate equipping behind ownership.
    /// </summary>
    [NeoDoc("Shop/Equipment/EquipItemDefinition.md")]
    [CreateAssetMenu(fileName = "EquipItem", menuName = "Neoxider/Shop/Equip Item")]
    public sealed class EquipItemDefinition : ScriptableObject
    {
        [Tooltip("Unique item id. Convention: match the ShopItemData id when the item is sold.")]
        [SerializeField]
        private string _id = "";

        [Tooltip("Category id; one item per category can be equipped at a time (e.g. Hair, Dress).")]
        [SerializeField]
        private string _categoryId = "";

        [Tooltip("Sprite applied to the category slot when this item is equipped.")]
        [SerializeField]
        private Sprite _sprite;

        public string Id => _id;
        public string CategoryId => _categoryId;
        public Sprite Sprite => _sprite;
    }
}
