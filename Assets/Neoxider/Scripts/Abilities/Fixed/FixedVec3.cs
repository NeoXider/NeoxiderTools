using System;

namespace Neo.Abilities.Fixed
{
    /// <summary>
    ///     A position or direction in raw fixed-point coordinates (same format as the system). Squared lengths are
    ///     unsigned 64-bit and saturate, so radius tests never overflow; keep coordinates within ±2^30 raw for exact
    ///     dot products.
    /// </summary>
    public readonly struct FixedVec3 : IEquatable<FixedVec3>
    {
        public static readonly FixedVec3 Zero = new FixedVec3(0, 0, 0);

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public FixedVec3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool IsZero => X == 0 && Y == 0 && Z == 0;

        public static FixedVec3 operator +(FixedVec3 a, FixedVec3 b)
        {
            return new FixedVec3(FixedMath.SaturatingAdd(a.X, b.X), FixedMath.SaturatingAdd(a.Y, b.Y),
                FixedMath.SaturatingAdd(a.Z, b.Z));
        }

        public static FixedVec3 operator -(FixedVec3 a, FixedVec3 b)
        {
            return new FixedVec3(FixedMath.SaturatingSubtract(a.X, b.X), FixedMath.SaturatingSubtract(a.Y, b.Y),
                FixedMath.SaturatingSubtract(a.Z, b.Z));
        }

        public static bool operator ==(FixedVec3 a, FixedVec3 b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(FixedVec3 a, FixedVec3 b)
        {
            return !(a == b);
        }

        /// <summary>Squared length in raw² units, saturating at <see cref="ulong.MaxValue" />.</summary>
        public ulong LengthSquared => SquaredDistance(this, Zero);

        /// <summary>floor(length) in raw units.</summary>
        public long LengthFloor => (long)FixedMath.SqrtFloor(LengthSquared);

        /// <summary>Squared distance in raw² units, saturating; computed on 64-bit differences so it never wraps.</summary>
        public static ulong SquaredDistance(FixedVec3 a, FixedVec3 b)
        {
            ulong x = Square((long)a.X - b.X);
            ulong y = Square((long)a.Y - b.Y);
            ulong z = Square((long)a.Z - b.Z);
            ulong sum = x + y;
            if (sum < x)
            {
                return ulong.MaxValue;
            }

            ulong total = sum + z;
            return total < sum ? ulong.MaxValue : total;
        }

        /// <summary>True when <paramref name="point" /> is within <paramref name="radiusRaw" /> of <paramref name="center" /> (inclusive).</summary>
        public static bool IsWithinRadius(FixedVec3 point, FixedVec3 center, int radiusRaw)
        {
            if (radiusRaw < 0)
            {
                return false;
            }

            ulong radius = (ulong)radiusRaw;
            return SquaredDistance(point, center) <= radius * radius;
        }

        /// <summary>Dot product in raw² units (exact while coordinates stay within ±2^30 raw).</summary>
        public static long Dot(FixedVec3 a, FixedVec3 b)
        {
            return (long)a.X * b.X + (long)a.Y * b.Y + (long)a.Z * b.Z;
        }

        /// <summary>This vector scaled to <paramref name="lengthRaw" /> (zero for a zero vector).</summary>
        public FixedVec3 WithLength(int lengthRaw)
        {
            long length = LengthFloor;
            if (length <= 0L)
            {
                return Zero;
            }

            return new FixedVec3(FixedMath.MulDivToInt(X, lengthRaw, length), FixedMath.MulDivToInt(Y, lengthRaw, length),
                FixedMath.MulDivToInt(Z, lengthRaw, length));
        }

        public bool Equals(FixedVec3 other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is FixedVec3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397 ^ Y) * 397 ^ Z;
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        private static ulong Square(long value)
        {
            ulong magnitude = FixedMath.Magnitude(value);
            return magnitude * magnitude;
        }
    }
}
