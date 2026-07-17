namespace Neo.Abilities
{
    /// <summary>
    ///     Implemented by prefab components that react to a domain spawn request
    ///     (projectiles, zones, summons) when instantiated by <see cref="AbilitySystemBehaviour" />.
    /// </summary>
    public interface ISpawnedAbilityEntity
    {
        void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub);
    }
}
