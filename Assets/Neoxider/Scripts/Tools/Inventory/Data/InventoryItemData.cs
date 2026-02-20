using UnityEngine;
using Neo.Extensions;

namespace Neo.Tools
{
    /// <summary>
    ///     ScriptableObject с описанием типа предмета для инвентаря.
    /// </summary>
    [CreateAssetMenu(fileName = "Inventory Item Data", menuName = "Neoxider/Tools/Inventory/Inventory Item Data",
        order = 20)]
    public sealed class InventoryItemData : ScriptableObject
    {
        [SerializeField] [Tooltip("Unique integer id used in runtime inventory storage.")]
        private int _itemId;

        [SerializeField] [Tooltip("Display name used in UI.")]
        private string _displayName;

        [SerializeField] [Tooltip("Item description.")]
        [TextArea(1, 5)]
        private string _description;

        [SerializeField] [Tooltip("Icon used in UI.")]
        private Sprite _icon;

        [SerializeField] [Tooltip("Optional world prefab used when item is dropped from inventory.")]
        private GameObject _worldDropPrefab;

        [SerializeField] [Tooltip("Max stack for this item (-1 = infinite, 1 = non-stackable).")]
        private int _maxStack = -1;

        [SerializeField] [Tooltip("Optional category id for filtering/grouping.")]
        private int _category;

        public int ItemId => _itemId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject WorldDropPrefab => _worldDropPrefab;
        public int MaxStack => _maxStack;
        public int Category => _category;

        private void OnValidate()
        {
            if (_maxStack == 0 || _maxStack < -1)
            {
                _maxStack = -1;
            }

            if (_icon == null && _worldDropPrefab != null)
            {
                _icon = _worldDropPrefab.GetPreviewSprite();
            }
        }
    }
}
