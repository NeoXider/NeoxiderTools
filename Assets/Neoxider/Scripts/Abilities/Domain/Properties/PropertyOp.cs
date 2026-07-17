namespace Neo.Abilities
{
    /// <summary>
    ///     How a modifier contribution combines into a property value.
    ///     Aggregation order is fixed and deterministic: base + sum(Add), then * product(Mul), then max(Max).
    /// </summary>
    public enum PropertyOp
    {
        /// <summary>Flat additive bonus, summed with the base value.</summary>
        Add = 0,

        /// <summary>Multiplier applied after all additive bonuses. 1 = no change, 1.25 = +25%.</summary>
        Mul = 1,

        /// <summary>The final value is raised to at least this contribution (applied last).</summary>
        Max = 2
    }
}
