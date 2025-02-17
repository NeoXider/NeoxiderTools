using System.Runtime.InteropServices;
using UnityEngine;

namespace Neo
{
    public static class NormalizationUtils
    {
        public const float DefaultMinValue = -1000000f;
        public const float DefaultMaxValue = 1000000f;

        public static float NormalizeToUnit(this float x)
        {
            return NormalizeToUnit(x, DefaultMinValue, DefaultMaxValue);
        }

        public static float NormalizeToRangeMinusOneToOne(this float x)
        {
            return NormalizeToRangeMinusOneToOne(x, DefaultMinValue, DefaultMaxValue);
        }

        public static float NormalizeToUnit(this float x, float a, float b)
        {
            return (x - a) / (b - a);
        }

        public static float NormalizeToRangeMinusOneToOne(this float x, float a, float b)
        {
            return 2.0f * x.NormalizeToUnit() - 1.0f;
        }
    }
}