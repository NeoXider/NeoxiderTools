namespace Neo.Abilities
{
    /// <summary>
    ///     Outcome of applying a modifier blueprint to a unit.
    /// </summary>
    public readonly struct ModifierApplyResult
    {
        public readonly ModifierInstance Instance;

        /// <summary>True when a new instance was created; false when an existing one refreshed/stacked.</summary>
        public readonly bool CreatedNew;

        public ModifierApplyResult(ModifierInstance instance, bool createdNew)
        {
            Instance = instance;
            CreatedNew = createdNew;
        }

        public bool Succeeded => Instance != null;
    }
}
