using System;
using UnityEngine.Events;

namespace Neo.Shop
{
    /// <summary>
    ///     Serializable UnityEvent wrapper for a <see cref="ShopBundleData"/> payload.
    ///     Used by <see cref="Shop"/>.OnPurchasedBundle so the event surfaces in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class ShopBundleEvent : UnityEvent<ShopBundleData>
    {
    }
}
