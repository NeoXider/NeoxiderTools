namespace Neo.Abilities
{
    /// <summary>
    ///     A property contribution resolved against a live modifier instance (stack scaling already applied).
    /// </summary>
    public readonly struct ResolvedContribution
    {
        public readonly PropertyOp Op;
        public readonly float Value;

        public ResolvedContribution(PropertyOp op, float value)
        {
            Op = op;
            Value = value;
        }
    }
}
