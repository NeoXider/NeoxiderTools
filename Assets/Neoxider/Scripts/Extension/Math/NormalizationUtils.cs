using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Neo
{
    /// <summary>
    /// Provides utility methods for normalizing values within different ranges
    /// </summary>
    public static class NormalizationUtils
    {
        /// <summary>
        /// Default minimum value for normalization operations
        /// </summary>
        public const float DefaultMinValue = -1000000f;

        /// <summary>
        /// Default maximum value for normalization operations
        /// </summary>
        public const float DefaultMaxValue = 1000000f;

        /// <summary>
        /// Normalizes a value to the range [0, 1] using default min/max values
        /// </summary>
        /// <param name="x">Value to normalize</param>
        /// <returns>Normalized value between 0 and 1</returns>
        public static float NormalizeToUnit(this float x)
        {
            return NormalizeToUnit(x, DefaultMinValue, DefaultMaxValue);
        }

        /// <summary>
        /// Normalizes a value to the range [-1, 1] using default min/max values
        /// </summary>
        /// <param name="x">Value to normalize</param>
        /// <returns>Normalized value between -1 and 1</returns>
        public static float NormalizeToRange(this float x)
        {
            return NormalizeToRange(x, DefaultMinValue, DefaultMaxValue);
        }

        /// <summary>
        /// Normalizes a value to the range [0, 1] using specified min/max values
        /// </summary>
        /// <param name="x">Value to normalize</param>
        /// <param name="min">Minimum value of the input range</param>
        /// <param name="max">Maximum value of the input range</param>
        /// <returns>Normalized value between 0 and 1</returns>
        /// <exception cref="ArgumentException">Thrown when min is greater than or equal to max</exception>
        public static float NormalizeToUnit(this float x, float min, float max)
        {
            if (min >= max)
                throw new ArgumentException($"Min value ({min}) must be less than max value ({max})");

            return Mathf.Clamp01((x - min) / (max - min));
        }

        /// <summary>
        /// Normalizes a value to the range [-1, 1] using specified min/max values
        /// </summary>
        /// <param name="x">Value to normalize</param>
        /// <param name="min">Minimum value of the input range</param>
        /// <param name="max">Maximum value of the input range</param>
        /// <returns>Normalized value between -1 and 1</returns>
        /// <exception cref="ArgumentException">Thrown when min is greater than or equal to max</exception>
        public static float NormalizeToRange(this float x, float min, float max)
        {
            return Mathf.Clamp(2.0f * NormalizeToUnit(x, min, max) - 1.0f, -1f, 1f);
        }

        /// <summary>
        /// Denormalizes a value from [0, 1] range to the specified range
        /// </summary>
        /// <param name="normalizedValue">Normalized value between 0 and 1</param>
        /// <param name="min">Target minimum value</param>
        /// <param name="max">Target maximum value</param>
        /// <returns>Denormalized value between min and max</returns>
        /// <exception cref="ArgumentException">Thrown when normalizedValue is not between 0 and 1</exception>
        public static float Denormalize(this float normalizedValue, float min, float max)
        {
            if (normalizedValue < 0f || normalizedValue > 1f)
                throw new ArgumentException("Normalized value must be between 0 and 1", nameof(normalizedValue));
            if (min >= max)
                throw new ArgumentException($"Min value ({min}) must be less than max value ({max})");

            return min + (max - min) * normalizedValue;
        }

        /// <summary>
        /// Remaps a value from one range to another
        /// </summary>
        /// <param name="value">Value to remap</param>
        /// <param name="fromMin">Original range minimum</param>
        /// <param name="fromMax">Original range maximum</param>
        /// <param name="toMin">Target range minimum</param>
        /// <param name="toMax">Target range maximum</param>
        /// <returns>Remapped value</returns>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float normalizedValue = NormalizeToUnit(value, fromMin, fromMax);
            return Denormalize(normalizedValue, toMin, toMax);
        }
    }
}