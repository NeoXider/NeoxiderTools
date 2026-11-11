namespace Neo.Core.Resources
{
    /// <summary>
    ///     One entry of a multi-resource payment for <see cref="ResourcePoolModel.TrySpendAll(System.Collections.Generic.IReadOnlyList{ResourceCost},out string)" />:
    ///     the pool id (case-insensitive) and the amount to pay. A value type, so a reused cost list does not allocate.
    /// </summary>
    public readonly struct ResourceCost
    {
        /// <summary>Id of the pool to pay from.</summary>
        public readonly string ResourceId;

        /// <summary>Amount to pay; a finite nonpositive amount is a no-op that needs no registered pool.</summary>
        public readonly float Amount;

        public ResourceCost(string resourceId, float amount)
        {
            ResourceId = resourceId;
            Amount = amount;
        }

        public override string ToString()
        {
            return ResourceId + ": " + Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
