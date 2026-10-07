using System;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     Fixed-point packing helpers for hand-written snapshot formats: they keep a few hundred entities inside a few
    ///     kilobytes. Every packer <b>saturates</b> instead of wrapping (a coordinate beyond the range pins to the
    ///     edge rather than teleporting across the map) and rounds to nearest, so a pack/unpack round trip is off by at
    ///     most half a step.
    ///     <para>
    ///         The <c>PackShort</c>/<c>PackUShort</c> family takes the step density explicitly (<c>stepsPerUnit</c>), the
    ///         named presets (<see cref="PackPosition"/>, <see cref="PackScale"/>, <see cref="PackSpeed"/>,
    ///         <see cref="PackSeconds"/>, <see cref="PackFraction"/>) are the densities that proved themselves in a
    ///         shipped .io game: 1/32 world unit for positions (about 3 cm, range +-1023 units), 1/64 for scale, 1/256
    ///         for speed, 1/32 s for timers. Use <see cref="FitsShort"/> to detect a world that outgrew the range.
    ///     </para>
    /// </summary>
    public static class NetQuantization
    {
        /// <summary>World units are packed in 1/32 steps by <see cref="PackPosition"/>.</summary>
        public const float PositionStepsPerUnit = 32f;

        /// <summary>Largest world coordinate <see cref="PackPosition"/> can carry.</summary>
        public const float MaxPackedPosition = short.MaxValue / PositionStepsPerUnit;

        /// <summary>Visual scale is packed in 1/64 steps in an unsigned 16-bit field.</summary>
        public const float ScaleStepsPerUnit = 64f;

        /// <summary>Speed (units per second) is packed in 1/256 steps.</summary>
        public const float SpeedStepsPerUnit = 256f;

        /// <summary>Timers (rage, cooldown) are packed in 1/32 second steps.</summary>
        public const float SecondsStepsPerUnit = 32f;

        /// <summary>A 0..1 fraction is packed into one byte.</summary>
        public const float FractionSteps = 255f;

        private const float TwoPi = (float)(Math.PI * 2d);

        // ---- generic, explicit density -----------------------------------------------------------

        /// <summary>Largest magnitude <see cref="PackShort"/> can carry at the given density.</summary>
        public static float MaxShort(float stepsPerUnit)
        {
            return stepsPerUnit <= 0f ? 0f : short.MaxValue / stepsPerUnit;
        }

        /// <summary>True when <paramref name="value"/> survives <see cref="PackShort"/> without saturating.</summary>
        public static bool FitsShort(float value, float stepsPerUnit)
        {
            float max = MaxShort(stepsPerUnit);
            return value >= -max && value <= max;
        }

        /// <summary>Signed 16-bit fixed point: <c>round(value * stepsPerUnit)</c>, saturated.</summary>
        public static short PackShort(float value, float stepsPerUnit)
        {
            return (short)SaturatedRound(value * stepsPerUnit, short.MinValue, short.MaxValue);
        }

        /// <summary>Inverse of <see cref="PackShort"/>.</summary>
        public static float UnpackShort(short packed, float stepsPerUnit)
        {
            return stepsPerUnit <= 0f ? 0f : packed / stepsPerUnit;
        }

        /// <summary>Unsigned 16-bit fixed point (negative values clamp to 0), saturated at 65535 steps.</summary>
        public static ushort PackUShort(float value, float stepsPerUnit)
        {
            return (ushort)SaturatedRound(value * stepsPerUnit, 0, ushort.MaxValue);
        }

        /// <summary>Inverse of <see cref="PackUShort"/>.</summary>
        public static float UnpackUShort(ushort packed, float stepsPerUnit)
        {
            return stepsPerUnit <= 0f ? 0f : packed / stepsPerUnit;
        }

        // ---- presets -------------------------------------------------------------------------------

        /// <summary>True when the coordinate survives <see cref="PackPosition"/> without saturating.</summary>
        public static bool FitsPosition(float value)
        {
            return value >= -MaxPackedPosition && value <= MaxPackedPosition;
        }

        /// <summary>Position in 1/32 unit steps (range +-1023.97).</summary>
        public static short PackPosition(float value)
        {
            return PackShort(value, PositionStepsPerUnit);
        }

        public static float UnpackPosition(short packed)
        {
            return packed / PositionStepsPerUnit;
        }

        public static ushort PackScale(float value)
        {
            return PackUShort(value, ScaleStepsPerUnit);
        }

        public static float UnpackScale(ushort packed)
        {
            return packed / ScaleStepsPerUnit;
        }

        public static ushort PackSpeed(float value)
        {
            return PackUShort(value, SpeedStepsPerUnit);
        }

        public static float UnpackSpeed(ushort packed)
        {
            return packed / SpeedStepsPerUnit;
        }

        public static ushort PackSeconds(float value)
        {
            return PackUShort(value, SecondsStepsPerUnit);
        }

        public static float UnpackSeconds(ushort packed)
        {
            return packed / SecondsStepsPerUnit;
        }

        /// <summary>A 0..1 fraction into one byte (values outside 0..1 saturate).</summary>
        public static byte PackFraction(float value)
        {
            return (byte)SaturatedRound(value * FractionSteps, 0, byte.MaxValue);
        }

        public static float UnpackFraction(byte packed)
        {
            return packed / FractionSteps;
        }

        /// <summary>A -1..1 value (a stick axis) into one byte; 128 is exactly 0.</summary>
        public static byte PackSignedUnit(float value)
        {
            return (byte)SaturatedRound(value * 127f + 128f, 1, 255);
        }

        public static float UnpackSignedUnit(byte packed)
        {
            return (packed - 128) / 127f;
        }

        // ---- angles ----------------------------------------------------------------------------------

        /// <summary>An angle in radians (any range, wrapped to 0..2pi) into 16 bits: 0.0055 degree resolution.</summary>
        public static ushort PackAngle(float radians)
        {
            float turns = Wrap01(radians / TwoPi);
            return (ushort)SaturatedRound(turns * 65536f, 0, ushort.MaxValue);
        }

        /// <summary>Inverse of <see cref="PackAngle"/>, result in 0..2pi.</summary>
        public static float UnpackAngle(ushort packed)
        {
            return packed / 65536f * TwoPi;
        }

        /// <summary>An angle in radians into one byte: 1.4 degree resolution, enough for a facing arrow.</summary>
        public static byte PackAngleByte(float radians)
        {
            float turns = Wrap01(radians / TwoPi);
            return (byte)(SaturatedRound(turns * 256f, 0, 256) & 0xFF);
        }

        /// <summary>Inverse of <see cref="PackAngleByte"/>, result in 0..2pi.</summary>
        public static float UnpackAngleByte(byte packed)
        {
            return packed / 256f * TwoPi;
        }

        // ---- hit points ------------------------------------------------------------------------------

        /// <summary>
        ///     Hit points as a whole number rounded <i>up</i>: an entity with 0.3 HP left must still read as alive on
        ///     a client, never as a zero that looks like a corpse. NaN and non-positive values give 0.
        /// </summary>
        public static uint PackHitPoints(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0u;
            }

            double ceiling = Math.Ceiling(value);
            return ceiling >= uint.MaxValue ? uint.MaxValue : (uint)ceiling;
        }

        /// <summary>A max-HP field: nearest whole number, at least 1 for a real entity; non-positive gives 0.</summary>
        public static uint PackMaxHitPoints(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0u;
            }

            double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded < 1d)
            {
                rounded = 1d;
            }

            return rounded >= uint.MaxValue ? uint.MaxValue : (uint)rounded;
        }

        private static float Wrap01(float turns)
        {
            if (float.IsNaN(turns) || float.IsInfinity(turns))
            {
                return 0f;
            }

            float wrapped = turns - (float)Math.Floor(turns);
            return wrapped >= 1f ? 0f : wrapped;
        }

        private static int SaturatedRound(float value, int min, int max)
        {
            if (float.IsNaN(value))
            {
                return 0;
            }

            double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded > max)
            {
                return max;
            }

            return rounded < min ? min : (int)rounded;
        }
    }
}
