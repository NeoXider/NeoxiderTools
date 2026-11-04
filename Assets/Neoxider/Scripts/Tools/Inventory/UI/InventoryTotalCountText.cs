using TMPro;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>Which inventory number <see cref="InventoryTotalCountText" /> shows.</summary>
    public enum InventoryCountViewMode
    {
        /// <summary>Sum of all item counts.</summary>
        Total = 0,

        /// <summary>Number of distinct item ids.</summary>
        Unique = 1,

        /// <summary>Count of <see cref="InventoryComponent.SelectedItemId" />.</summary>
        Selected = 2
    }

    /// <summary>
    ///     TMP label bound to an <see cref="InventoryComponent" /> total, unique or selected-item count.
    ///     Refreshes on inventory and selection changes and rewrites the text only when the shown value changes.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventoryTotalCountText.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryTotalCountText")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryTotalCountText))]
    public sealed class InventoryTotalCountText : MonoBehaviour
    {
        [SerializeField] [Tooltip("Inventory to read. If empty, InventoryComponent.FindDefault() is used on enable.")]
        private InventoryComponent _inventory;

        [SerializeField] [Tooltip("Label to write. If empty, the TMP_Text on this object is used.")]
        private TMP_Text _targetText;

        [SerializeField] [Tooltip("Total item count, unique id count, or the count of the inventory's selected id.")]
        private InventoryCountViewMode _mode = InventoryCountViewMode.Total;

        [SerializeField] [Tooltip("string.Format pattern; {0} is the number.")]
        private string _format = "{0}";

        private readonly InventoryCountLabel _label = new();

        private void OnEnable()
        {
            BindIfNeeded();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Switches what is displayed and refreshes immediately.</summary>
        public void SetMode(InventoryCountViewMode mode)
        {
            _mode = mode;
            Refresh();
        }

        /// <summary>Rebinds to another inventory (null clears) and refreshes.</summary>
        public void SetInventory(InventoryComponent inventory)
        {
            if (_inventory == inventory)
            {
                return;
            }

            Unsubscribe();
            _inventory = inventory;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            Refresh();
        }

        /// <summary>Writes the current value; skipped when the label already shows it.</summary>
        public void Refresh()
        {
            _label.Write(_targetText, _format, GetValue());
        }

        private void Subscribe()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.OnInventoryChanged.AddListener(Refresh);
            _inventory.OnSelectedItemChanged.AddListener(OnSelectedItemChanged);
        }

        private void Unsubscribe()
        {
            if (_inventory == null)
            {
                return;
            }

            _inventory.OnInventoryChanged.RemoveListener(Refresh);
            _inventory.OnSelectedItemChanged.RemoveListener(OnSelectedItemChanged);
        }

        private void OnSelectedItemChanged(int itemId)
        {
            if (_mode == InventoryCountViewMode.Selected)
            {
                Refresh();
            }
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
                TryGetComponent(out _targetText);
            }
        }
    }
}
