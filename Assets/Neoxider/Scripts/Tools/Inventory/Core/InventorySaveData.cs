using System;
using System.Collections.Generic;

namespace Neo.Tools
{
    /// <summary>
    ///     DTO для сериализации/десериализации состояния инвентаря.
    /// </summary>
    [Serializable]
    public sealed class InventorySaveData
    {
        public List<InventoryEntry> Entries = new();
    }
}
