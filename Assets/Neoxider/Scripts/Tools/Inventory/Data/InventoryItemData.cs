using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     ScriptableObject describing one item type (id, UI, stack rules, optional instance state).
    /// </summary>
    [CreateAssetMenu(fileName = "Inventory Item Data", menuName = "Neoxider/Tools/Inventory/Inventory Item Data",
        order = 20)]
    public sealed class InventoryItemData : ScriptableObject
    {
        [SerializeField] [Tooltip("Unique integer id used in runtime inventory storage.")]
        private int _itemId;

        [SerializeField] [Tooltip("Display name used in UI.")]
        private string _displayName;

        [SerializeField] [Tooltip("Item description.")] [TextArea(1, 5)]
        private string _description;

        [SerializeField] [Tooltip("Icon used in UI.")]
        private Sprite _icon;

        [SerializeField] [Tooltip("Optional world prefab used when item is dropped from inventory.")]
        private GameObject _worldDropPrefab;

        [SerializeField] [Tooltip("Max stack for this item (-1 = infinite, 1 = non-stackable).")]
        private int _maxStack = -1;

        [SerializeField] [Tooltip("Optional category id for filtering/grouping.")]
        private int _category;

        [SerializeField] [Tooltip("Treat this item as an instance-based item with unique per-item state payload.")]
        private bool _supportsInstanceState;

        /// <summary>Runtime item identifier.</summary>
        public int ItemId => _itemId;

        /// <summary>UI display name.</summary>
        public string DisplayName => _displayName;

        /// <summary>Longer description text.</summary>
        public string Description => _description;

        /// <summary>UI icon sprite.</summary>
        public Sprite Icon => _icon;

        /// <summary>Prefab spawned when dropping from inventory.</summary>
        public GameObject WorldDropPrefab => _worldDropPrefab;

        /// <summary>Max stack (-1 = unlimited, 1 = non-stackable).</summary>
        public int MaxStack => _maxStack;

        /// <summary>Optional grouping/filter id.</summary>
        public int Category => _category;

        /// <summary>When true, items are stored as <see cref="InventoryItemInstance" /> with separate state payloads.</summary>
        public bool SupportsInstanceState => _supportsInstanceState;

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
