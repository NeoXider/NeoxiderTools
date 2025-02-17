using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "ItemClnData", menuName = "Neoxider/ItemCollectionData")]
    public class ItemCollectionData : ScriptableObject
    {
        [SerializeField] private string _itemName;
        [TextArea(1, 5)]
        [SerializeField] private string _description;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private int _itemType = 0;

        public string itemName => _itemName;
        public string description => _description;
        public Sprite sprite => _sprite;
        public int itemType => _itemType;
    }
}
