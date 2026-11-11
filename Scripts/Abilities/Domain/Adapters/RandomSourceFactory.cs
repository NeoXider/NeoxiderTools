namespace Neo.Abilities
{
    /// <summary>A random source that can restart its sequence from a seed without a new object.</summary>
    public interface IReseedableRandomSource : IRandomSource
    {
        void Reseed(uint seed);
    }

    /// <summary>
    ///     Creates the seeded generators of an <see cref="AbilitySystem" />. Pooled effect contexts create one generator
    ///     each and reseed it per cast, tick, reaction and zone pulse, so a factory is called rarely; reseeding must
    ///     not allocate.
    /// </summary>
    public interface IRandomSourceFactory
    {
        IReseedableRandomSource Create(uint seed);
    }

    /// <summary>Default factory: <see cref="XorShiftRandom" />.</summary>
    public sealed class XorShiftRandomFactory : IRandomSourceFactory
    {
        public static readonly XorShiftRandomFactory Instance = new XorShiftRandomFactory();

        public IReseedableRandomSource Create(uint seed)
        {
            return new XorShiftRandom(seed);
        }
    }
}
