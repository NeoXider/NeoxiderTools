using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    public enum InventoryCountViewMode
    {
        Total = 0,
        Unique = 1,
        Selected = 2
    }

    [NeoDoc("Tools/Inventory/InventoryComponent.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryTotalCountText")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryTotalCountText))]
    public sealed class InventoryTotalCountText : MonoBehaviour
    {
        [SerializeField] private InventoryComponent _inventory;
        [SerializeField] private TMP_Text _targetText;
        [SerializeField] private InventoryCountViewMode _mode = InventoryCountViewMode.Total;
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

        public void SetMode(InventoryCountViewMode mode)
        {
            _mode = mode;
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

            int value = GetValue();
            _targetText.text = string.Format(_format, value);
        }

        private int GetValue()
        {
            if (_inventory == null)
            {
                return 0;
            }

            return _mode switch
            {
                InventoryCountViewMode.Unique => _inventory.UniqueItemCount,
                InventoryCountViewMode.Selected => _inventory.SelectedItemCount,
                _ => _inventory.TotalItemCount
            };
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
