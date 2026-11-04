namespace Neo.Shop
{
    /// <summary>
    ///     Typed outcome of <see cref="Shop.TryBuy(string)"/> and <see cref="Shop.TryBuyBundle(string)"/>.
    ///     The same purchase events fire as for <see cref="Shop.Buy(string)"/>; the status lets C# callers
    ///     branch without subscribing to UnityEvents.
    /// </summary>
    public enum ShopPurchaseStatus
    {
        /// <summary>Spending (if any) and the profile commit succeeded; purchase events were raised.</summary>
        Purchased = 0,

        /// <summary>
        ///     The item, or a single-purchase bundle, was already owned and nothing was spent. An item was also
        ///     selected (BuyAndEquip) and previewed; an owned bundle has no side effects.
        /// </summary>
        AlreadyOwned = 1,

        /// <summary>EquipOnly flow: the item was selected and previewed without spending.</summary>
        Selected = 2,

        /// <summary>
        ///     The wallet rejected the debit (usually not enough balance, or no wallet resolved).
        ///     <c>OnPurchaseFailedId</c> was raised.
        /// </summary>
        InsufficientFunds = 3,

        /// <summary>
        ///     The debit succeeded but persisting the profile failed; the profile was rolled back and the
        ///     debit refunded. <c>OnPurchaseFailedId</c> was raised.
        /// </summary>
        CommitFailed = 4,

        /// <summary>
        ///     The wallet cannot confirm the debit locally (networked client). Nothing was spent or granted locally,
        ///     no failure event was raised, and Shop does not finish this purchase later; run authoritative
        ///     purchases on the server.
        /// </summary>
        PendingServerAuthority = 5,

        /// <summary>Nothing happened: empty or unknown id, or the Browse flow (or EquipOnly for bundles).</summary>
        Unavailable = 6,

        /// <summary>Rejected because another purchase on this shop is still running (nested call from a callback).</summary>
        Busy = 7
    }
}
