namespace Neo.Abilities
{
    /// <summary>
    ///     Small deterministic PRNG (xorshift32). Same seed → same sequence on every platform.
    /// </summary>
    public sealed class XorShiftRandom : IRandomSource
    {
        private uint _state;

        public XorShiftRandom(uint seed)
        {
            _state = seed == 0 ? 2463534242u : seed;
        }

        public float NextFloat()
        {
            return (NextUInt() & 0xFFFFFF) / (float)0x1000000;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                return 0;
            }

            return (int)(NextUInt() % (uint)maxExclusive);
        }

        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }
    }
}
