using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Optional base for <see cref="IInventoryItemState" /> on a world/dropped item prefab: implement what to save (ammo, durability) and how to apply it back.
    /// </summary>
    /// <remarks>
    ///     Inventory does not know your fields; this class is where you define capture/restore. The engine calls
    ///     <see cref="InventoryItemStateUtility" /> to collect all such behaviours on pickup and restore them on spawn/drop.
    /// </remarks>
    public abstract class InventoryItemStateBehaviour : MonoBehaviour, IInventoryItemState
    {
        [SerializeField] [Tooltip("Optional override key used inside inventory instance payload.")]
        private string _inventoryStateKey;

        /// <inheritdoc />
        public string InventoryStateKey =>
            string.IsNullOrWhiteSpace(_inventoryStateKey) ? GetType().FullName : _inventoryStateKey;

        /// <inheritdoc />
        public abstract string CaptureInventoryState();

        /// <inheritdoc />
        public abstract void RestoreInventoryState(string json);
    }
}
