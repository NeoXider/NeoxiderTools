using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>Optional Inspector adapter for using an exact selection, with optional consumption.</summary>
    [NeoDoc("Tools/Inventory/InventoryUseAction.md")]
    [AddComponentMenu("Neoxider/Tools/Inventory/InventoryUseAction")]
    public sealed class InventoryUseAction : MonoBehaviour
    {
        [SerializeField] private InventoryHand _hand;
        [SerializeField] [Tooltip("Subscribe to the hand's exact-selection use event while enabled.")]
        private bool _listenToHand = true;
        [SerializeField] [Tooltip("-1 accepts every item type; otherwise only this item id is accepted.")]
        private int _itemId = -1;
        [SerializeField] [Min(0)] [Tooltip("0 = reusable; positive = consume exactly this many before the effect.")]
        private int _consumeAmount = 1;

        /// <summary>Committed item and its payload, for healing, placing, equipping or other game effects.</summary>
        public UnityEvent<InventoryItemRecord> OnUsed = new();
        /// <summary>Invalid/stale selection or insufficient quantity; no effect and no consumption.</summary>
        public UnityEvent OnRejected = new();

        private bool _using;

        private void OnEnable()
        {
            if (_listenToHand && _hand != null)
            {
                _hand.OnUseSelectionRequested.AddListener(UseSelection);
            }
        }

        private void OnDisable()
        {
            if (_hand != null)
            {
                _hand.OnUseSelectionRequested.RemoveListener(UseSelection);
            }
        }

        /// <summary>UnityEvent/button entry point. Uses the hand's current exact selection.</summary>
        public void UseEquipped()
        {
            if (_hand != null && _hand.TryGetEquippedSelection(out InventorySelection selection))
            {
                UseSelection(selection);
            }
            else
            {
                OnRejected?.Invoke();
            }
        }

        /// <summary>Dynamic UnityEvent entry point for InventoryHand.OnUseSelectionRequested.</summary>
        public void UseSelection(InventorySelection selection)
        {
            TryUse(_hand != null ? _hand.Inventory : null, selection);
        }

        /// <summary>
        ///     C# entry point with explicit dependencies. Commits consumption before the effect;
        ///     notification faults do not undo ownership or prevent the effect from being attempted.
        /// </summary>
        public bool TryUse(InventoryComponent inventory, InventorySelection selection)
        {
            if (_using)
            {
                return false;
            }

            if (inventory == null || selection == null || (_itemId >= 0 && selection.ItemId != _itemId) ||
                !inventory.TryGetSelectionRecord(selection, out InventoryItemRecord current))
            {
                OnRejected?.Invoke();
                return false;
            }

            InventoryComponent.MutationReceipt receipt = null;
            InventoryItemRecord record = current;
            if (_consumeAmount > 0)
            {
                receipt = inventory.CommitSelectionTake(selection, _consumeAmount);
                if (receipt == null)
                {
                    OnRejected?.Invoke();
                    return false;
                }

                record = receipt.Record;
            }

            _using = true;
            try
            {
                // Publish the committed inventory even if an effect listener throws.
                if (receipt != null)
                {
                    try
                    {
                        OnUsed?.Invoke(record);
                    }
                    catch (Exception exception)
                    {
                        receipt.CaptureFault(exception);
                    }

                    receipt.Publish();
                    receipt.ThrowIfFaulted();
                }
                else
                {
                    OnUsed?.Invoke(record);
                }

                return true;
            }
            finally
            {
                _using = false;
            }
        }
    }
}
