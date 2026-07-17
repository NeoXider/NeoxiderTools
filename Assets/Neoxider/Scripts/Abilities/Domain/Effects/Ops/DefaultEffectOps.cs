namespace Neo.Abilities
{
    /// <summary>
    ///     Installs the built-in effect operations into a registry. Called by <see cref="AbilitySystem" />;
    ///     games may re-register ids afterwards to override behavior.
    /// </summary>
    public static class DefaultEffectOps
    {
        public static void RegisterAll(EffectOpRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            registry.Register(new DamageEffectOperation());
            registry.Register(new HealEffectOperation());
            registry.Register(new ApplyModifierEffectOperation());
            registry.Register(new RemoveModifierEffectOperation());
            registry.Register(new DispelEffectOperation());
            registry.Register(new ResourceChangeEffectOperation());
            registry.Register(new SpawnEffectOperation());
            registry.Register(new KnockbackEffectOperation());
            registry.Register(new PullEffectOperation());
            registry.Register(new TeleportEffectOperation());
            registry.Register(new ExecuteEffectOperation());
            registry.Register(new ChainEffectOperation());
        }
    }
}
