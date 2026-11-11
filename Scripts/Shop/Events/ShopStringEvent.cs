using System;
using UnityEngine.Events;

namespace Neo.Shop
{
    /// <summary>
    ///     Serializable UnityEvent wrapper for a stable item / bundle id. Used by Shop's
    ///     string-based selection / purchase events so they surface reliably in the Inspector.
    /// </summary>
    [Serializable]
    public sealed class ShopStringEvent : UnityEvent<string>
    {
    }
}
