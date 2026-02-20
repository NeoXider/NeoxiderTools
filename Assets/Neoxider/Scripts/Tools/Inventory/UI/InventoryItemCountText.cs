using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Inventory/InventoryComponent.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryItemCountText")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryItemCountText))]
    public sealed class InventoryItemCountText : MonoBehaviour
    {
        [SerializeField] private InventoryComponent _inventory;
        [SerializeField] private TMP_Text _targetText;
        [SerializeField] private int _itemId;
        [SerializeField] private string _format = "{0}";

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

        public void SetItemId(int itemId)
        {
            _itemId = itemId;
            Refresh();
        }

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

        public void Refresh()
        {
            if (_targetText == null)
            {
                return;
            }

            int value = _inventory != null ? _inventory.GetCount(_itemId) : 0;
            _targetText.text = string.Format(_format, value);
        }

        private void BindIfNeeded()
        {
            if (_inventory == null)
            {
                _inventory = InventoryComponent.FindDefault();
            }

            if (_targetText == null)
            {
                _targetText = GetComponent<TMP_Text>();
            }
        }
    }
}
