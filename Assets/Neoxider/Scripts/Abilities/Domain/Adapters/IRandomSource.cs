namespace Neo.Abilities
{
    /// <summary>
    ///     Injectable random source so casts are deterministic per seed (replays, tests, server authority).
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Uniform float in [0, 1).</summary>
        float NextFloat();

        /// <summary>Uniform int in [0, maxExclusive).</summary>
        int NextInt(int maxExclusive);
    }
}
