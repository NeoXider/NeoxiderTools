using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     TMP label bound to the count of one item id in an <see cref="InventoryComponent" />.
    ///     Refreshes on inventory changes and rewrites the text only when the count changes.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventoryItemCountText.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryItemCountText")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryItemCountText))]
    public sealed class InventoryItemCountText : MonoBehaviour
    {
        [SerializeField] [Tooltip("Inventory to read. If empty, InventoryComponent.FindDefault() is used on enable.")]
        private InventoryComponent _inventory;

        [SerializeField] [Tooltip("Label to write. If empty, the TMP_Text on this object is used.")]
        private TMP_Text _targetText;

        [SerializeField] [Tooltip("Item id whose total count is shown.")]
        private int _itemId;

        [SerializeField] [Tooltip("string.Format pattern; {0} is the count.")]
        private string _format = "{0}";

        private readonly InventoryCountLabel _label = new();

        private void OnEnable()
        {
            BindIfNeeded();
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged.AddListener(Refresh);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged.RemoveListener(Refresh);
            }
        }

        /// <summary>Shows another item id and refreshes immediately.</summary>
        public void SetItemId(int itemId)
        {
            _itemId = itemId;
            Refresh();
        }

        /// <summary>Rebinds to another inventory (null shows 0) and refreshes.</summary>
        public void SetInventory(InventoryComponent inventory)
        {
            if (_inventory == inventory)
            {
                return;
            }

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged.RemoveListener(Refresh);
            }

            _inventory = inventory;
            if (_inventory != null && isActiveAndEnabled)
            {
                _inventory.OnInventoryChanged.AddListener(Refresh);
            }

            Refresh();
        }

        /// <summary>Writes the current count; skipped when the label already shows it.</summary>
        public void Refresh()
        {
            _label.Write(_targetText, _format, _inventory != null ? _inventory.GetCount(_itemId) : 0);
        }

        private void BindIfNeeded()
        {
            if (_inventory == null)
            {
                _inventory = InventoryComponent.FindDefault();
            }

            if (_targetText == null)
            {
                TryGetComponent(out _targetText);
            }
        }
    }
}
