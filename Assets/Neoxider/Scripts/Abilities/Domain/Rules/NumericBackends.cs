using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Numeric backend of the float domain: rounds every computed amount (damage, absorption, healing, lifesteal,
    ///     resource changes, reflected damage) and every aggregated property value before it is stored or published.
    /// </summary>
    public interface IAbilityNumeric
    {
        /// <summary>Short name for diagnostics and state hashes ("float", "q8").</summary>
        string Name { get; }

        float Quantize(float value);
    }

    /// <summary>Default: plain float, values pass through unchanged.</summary>
    public sealed class FloatNumeric : IAbilityNumeric
    {
        public static readonly FloatNumeric Instance = new FloatNumeric();

        public string Name => "float";

        public float Quantize(float value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Fixed-point grid: values are rounded (half away from zero) to multiples of 2^-FractionBits, so 8 bits gives
    ///     the Q8.8 grid where 1.0 = 256 raw. Non-finite values pass through. <see cref="ToRaw" /> /
    ///     <see cref="FromRaw" /> convert to and from raw integers for hashing and network payloads.
    /// </summary>
    public sealed class FixedPointNumeric : IAbilityNumeric
    {
        public static readonly FixedPointNumeric Q8 = new FixedPointNumeric(8);

        private readonly double _scale;

        public FixedPointNumeric(int fractionBits)
        {
            FractionBits = Math.Clamp(fractionBits, 0, 16);
            _scale = 1 << FractionBits;
            Name = "q" + FractionBits;
        }

        public int FractionBits { get; }
        public string Name { get; }

        /// <summary>Raw units per 1.0 (256 for Q8.8).</summary>
        public int OneRaw => 1 << FractionBits;

        public float Quantize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value;
            }

            return (float)(Math.Round(value * _scale, MidpointRounding.AwayFromZero) / _scale);
        }

        public int ToRaw(float value)
        {
            if (float.IsNaN(value))
            {
                return 0;
            }

            double raw = Math.Round(value * _scale, MidpointRounding.AwayFromZero);
            return raw >= int.MaxValue ? int.MaxValue : raw <= int.MinValue ? int.MinValue : (int)raw;
        }

        public float FromRaw(int raw)
        {
            return (float)(raw / _scale);
        }
    }
}
