using System;
using System.Runtime.InteropServices;

namespace Neo.Rpg.Runtime
{
    /// <summary>Shared bounded drain for the RPG resource and status clocks.</summary>
    internal static class RpgTickUtility
    {
        internal const int MaxTicksPerEntry = 1024;

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatBits
        {
            [FieldOffset(0)] public float Value;
            [FieldOffset(0)] public int Bits;
        }

        private static float RoundedPhase(double phase, float interval)
        {
            float rounded = (float)phase;
            if (rounded >= interval)
            {
                FloatBits bits = new() { Value = interval };
                bits.Bits--;
                return bits.Value;
            }
            return rounded;
        }

        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static int Consume(float previous, float deltaTime, float interval, out float remainder)
        {
            remainder = IsFinite(previous) ? previous : 0f;
            if (!IsFinite(previous) || !IsFinite(deltaTime) || deltaTime == 0f ||
                !IsFinite(interval) || interval <= 0f)
            {
                return 0;
            }

            double width = interval;
            double total = (double)previous + deltaTime;
            if (deltaTime < 0f)
            {
                remainder = total < -float.MaxValue ? -float.MaxValue : (float)total;
                return 0;
            }
            if (total < width)
            {
                remainder = RoundedPhase(total, interval);
                return 0;
            }

            // Reduce each contribution separately: adding a huge delta first loses a small prior phase.
            double phase = (double)previous % width + (double)deltaTime % width;
            double ticks = Math.Truncate((double)previous / width) + Math.Floor((double)deltaTime / width);
            if (phase < 0d)
            {
                phase += width;
                ticks -= 1d;
            }
            else if (phase >= width)
            {
                phase -= width;
                ticks += 1d;
            }

            // Do not invent a logical tick when casting a just-below-boundary phase back to float.
            remainder = RoundedPhase(phase, interval);

            return ticks <= 0d ? 0 : (int)Math.Min(MaxTicksPerEntry, ticks);
        }

        internal static bool TryAmount(double value, out float amount)
        {
            amount = 0f;
            if (double.IsNaN(value) || double.IsInfinity(value) || value > float.MaxValue || value < -float.MaxValue)
            {
                return false;
            }

            amount = (float)value;
            return true;
        }
    }
}
