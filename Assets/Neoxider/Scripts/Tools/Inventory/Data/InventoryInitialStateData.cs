using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     ScriptableObject describing initial inventory contents.
    /// </summary>
    [CreateAssetMenu(fileName = "Inventory Initial State",
        menuName = "Neoxider/Tools/Inventory/Inventory Initial State",
        order = 22)]
    public sealed class InventoryInitialStateData : ScriptableObject
    {
        [SerializeField] [Tooltip("Initial entries applied based on InventoryComponent load mode.")]
        private List<InventoryEntry> _entries = new();

        public IReadOnlyList<InventoryEntry> Entries => _entries;
    }
}
