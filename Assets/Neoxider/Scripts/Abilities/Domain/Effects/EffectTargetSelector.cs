namespace Neo.Abilities
{
    /// <summary>
    ///     Whom an effect node applies to, resolved inside the cast/tick/reaction context.
    /// </summary>
    public enum EffectTargetSelector
    {
        /// <summary>The resolved primary target(s) of the context (cast targets, modifier owner, event source).</summary>
        Target = 0,

        /// <summary>The caster of the context.</summary>
        Caster = 1,

        /// <summary>Units in Radius around the primary target (or target point), filtered by TeamFilter.</summary>
        AreaAroundTarget = 2,

        /// <summary>Units in Radius around the caster, filtered by TeamFilter.</summary>
        AreaAroundCaster = 3
    }
}
