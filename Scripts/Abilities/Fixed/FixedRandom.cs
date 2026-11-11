namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     Seeded integer random source of the fixed core. Chances compare raw draws: a roll of
    ///     <see cref="NextRaw" /> below a raw chance succeeds.
    /// </summary>
    public interface IFixedRandomSource
    {
        void Reseed(uint seed);

        uint NextUInt();

        /// <summary>A raw value in [0, <paramref name="oneRaw" />).</summary>
        int NextRaw(int oneRaw);

        /// <summary>An integer in [0, <paramref name="maxExclusive" />); 0 for a non-positive bound.</summary>
        int NextInt(int maxExclusive);
    }

    /// <summary>Creates the random source of each effect context; implementations must not allocate after warm-up.</summary>
    public interface IFixedRandomFactory
    {
        IFixedRandomSource Create(uint seed);
    }

    /// <summary>
    ///     xorshift32, the same generator and seeding as the float domain's XorShiftRandom: identical streams, and
    ///     <see cref="NextRaw" /> takes the same 24 bits as its NextFloat, so a raw chance on the grid rolls exactly
    ///     like the float chance.
    /// </summary>
    public sealed class FixedXorShiftRandom : IFixedRandomSource
    {
        private uint _state;

        public FixedXorShiftRandom(uint seed)
        {
            Reseed(seed);
        }

        public void Reseed(uint seed)
        {
            _state = seed == 0u ? 2463534242u : seed;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int NextRaw(int oneRaw)
        {
            if (oneRaw <= 0)
            {
                return 0;
            }

            return (int)(((ulong)(NextUInt() & 0xFFFFFFu) * (ulong)oneRaw) >> 24);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                return 0;
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }
    }

    /// <summary>Default factory: <see cref="FixedXorShiftRandom" />.</summary>
    public sealed class FixedXorShiftRandomFactory : IFixedRandomFactory
    {
        public static readonly FixedXorShiftRandomFactory Instance = new FixedXorShiftRandomFactory();

        public IFixedRandomSource Create(uint seed)
        {
            return new FixedXorShiftRandom(seed);
        }
    }

    /// <summary>Seed derivation shared with the float domain, so both cores roll the same streams.</summary>
    public static class FixedSeeds
    {
        /// <summary>Seed of a cast: the request seed, else derived from the cast id and the caster.</summary>
        public static uint ForCast(uint requestSeed, uint castId, int caster)
        {
            return requestSeed != 0u ? requestSeed : unchecked(castId * 2654435761u ^ (uint)caster);
        }

        /// <summary>The first roll keeps the base seed; later rolls get an avalanche-mixed seed.</summary>
        public static uint Roll(uint baseSeed, uint index)
        {
            return index == 0u ? baseSeed : Mix(baseSeed, index);
        }

        /// <summary>Murmur-style avalanche of a seed and a salt.</summary>
        public static uint Mix(uint seed, uint salt)
        {
            unchecked
            {
                uint h = seed + 0x9E3779B9u + salt * 0x85EBCA6Bu;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
