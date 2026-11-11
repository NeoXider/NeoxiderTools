namespace Neo.Shop
{
    /// <summary>
    ///     Shop purchase/equip orchestration mode. Replaces the historical
    ///     `_useSetItem` + `_activateSavedEquipped` boolean combinations.
    /// </summary>
    public enum ShopPurchaseFlow
    {
        /// <summary>Buy an item, then auto-select it (default historical behaviour).</summary>
        BuyAndEquip = 0,

        /// <summary>Purchase only; selection state is not maintained.</summary>
        BuyOnly = 1,

        /// <summary>No spending — only switch equipped item (cosmetic toggle UI).</summary>
        EquipOnly = 2,

        /// <summary>Read-only storefront; preview works, <see cref="Shop.Buy(string)"/> is a no-op.</summary>
        Browse = 3
    }
}
