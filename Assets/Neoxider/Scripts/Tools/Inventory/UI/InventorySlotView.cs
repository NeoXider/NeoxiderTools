using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Tools
{
    /// <summary>
    ///     One slot cell: shows <see cref="InventoryItemView" />, highlights selection, forwards clicks to <see cref="InventorySlotGridView" />.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventorySlotGridView.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventorySlotView")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventorySlotView))]
    public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] [Tooltip("Optional item presenter for icon and count.")]
        private InventoryItemView _itemView;

        [SerializeField] [Tooltip("Shown when this slot is the global transfer selection.")]
        private GameObject _selectionHighlight;

        [SerializeField] [Tooltip("Shown when the slot is empty (optional UX).")]
        private GameObject _emptyRoot;

        private InventorySlotGridView _owner;
        private int _slotIndex;

        /// <summary>Updates visuals for one physical slot index.</summary>
        public void Bind(InventorySlotGridView owner, int slotIndex, InventoryItemData itemData,
            InventorySlotState slotState,
            bool selected)
        {
            _owner = owner;
            _slotIndex = slotIndex;

            bool empty = slotState == null || slotState.IsEmpty;
            if (_selectionHighlight != null)
            {
                _selectionHighlight.SetActive(selected);
            }

            if (_emptyRoot != null)
            {
                _emptyRoot.SetActive(empty);
            }

            if (_itemView == null)
            {
                return;
            }

            if (empty)
            {
                _itemView.Clear();
                return;
            }

            _itemView.Bind(itemData, slotState.EffectiveItemId, slotState.EffectiveCount);
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            _owner?.HandleSlotClick(_slotIndex);
        }
    }
}
