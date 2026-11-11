using System;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Serializable UnityEvent payload for an inventory grant performed by
    ///     <c>ShopInventoryGrantBridge</c>: the granted <see cref="InventoryItemData"/> and the
    ///     amount actually added.
    /// </summary>
    [Serializable]
    public sealed class ShopInventoryGrantEvent : UnityEvent<InventoryItemData, int>
    {
    }
}
