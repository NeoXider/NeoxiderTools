namespace Neo.Abilities
{
    /// <summary>
    ///     The deterministic profile of the float domain, built only from the regular extension points: fixed ticks
    ///     (<see cref="FixedStepTimeSource" />), fixed-point rounding (<see cref="FixedPointNumeric" />, Q8.8 by default)
    ///     and the seeded xorshift generator. Use it for replays, server-authoritative simulation and desync checks with
    ///     <see cref="AbilityStateHasher" />. It rounds float results; it does not make the domain integer-only (see
    ///     Docs/Abilities/DeterministicMode.md).
    /// </summary>
    public static class DeterministicAbilityProfile
    {
        public const int DefaultTicksPerSecond = 30;
        public const int DefaultFractionBits = 8;

        /// <summary>Installs the profile on <paramref name="rules" />. Other rules (formulas, stacking...) stay as they are.</summary>
        public static void Apply(AbilityRules rules, int ticksPerSecond = DefaultTicksPerSecond,
            int fractionBits = DefaultFractionBits, int maxStepsPerCall = 8)
        {
            if (rules == null)
            {
                return;
            }

            rules.Time = new FixedStepTimeSource(ticksPerSecond, maxStepsPerCall);
            rules.Numeric = fractionBits == DefaultFractionBits ? FixedPointNumeric.Q8 : new FixedPointNumeric(fractionBits);
            rules.Random = XorShiftRandomFactory.Instance;
        }

        /// <summary>True when the time source is fixed-step and the numeric backend is fixed-point.</summary>
        public static bool IsApplied(AbilityRules rules)
        {
            return rules != null && rules.Time is FixedStepTimeSource && rules.Numeric is FixedPointNumeric;
        }
    }
}
