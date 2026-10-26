using System;

namespace Neo.Shop
{
    /// <summary>An observer error after settling a purchase; it never changes the committed or rolled-back outcome.</summary>
    [Serializable]
    public sealed class ShopNotificationFault
    {
        public ShopNotificationFault(string purchaseId, string itemId, string observer, string message)
        {
            PurchaseId = purchaseId;
            ItemId = itemId;
            Observer = observer;
            Message = message;
        }

        /// <summary>Committed item or bundle id; also identifies a rolled-back profile refresh when applicable.</summary>
        public string PurchaseId { get; }

        /// <summary>Item id for an item-specific notification, including a child of a bundle; otherwise empty.</summary>
        public string ItemId { get; }

        /// <summary>Observer stage, for example OnPurchasedId or OnShopChanged.</summary>
        public string Observer { get; }

        public string Message { get; }
    }
}
