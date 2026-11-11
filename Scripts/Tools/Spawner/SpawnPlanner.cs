using System.Collections.Generic;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>Plane a ring spawn is laid out in: <see cref="XY" /> for 2D scenes, <see cref="XZ" /> for 3D ground.</summary>
    public enum SpawnPlane
    {
        XY,
        XZ
    }

    /// <summary>
    ///     Engine-light spawn math shared by <see cref="Spawner" /> and game code: weighted choice with unlock times,
    ///     ring positions around a target and a delay multiplier over time. Every method is pure and allocation-free,
    ///     so it can be unit-tested and reused outside the component.
    /// </summary>
    public static class SpawnPlanner
    {
        /// <summary>
        ///     Picks an index among <paramref name="count" /> entries by weight, ignoring entries whose unlock time is
        ///     later than <paramref name="elapsedSeconds" />. A missing weight counts as 1 and a missing unlock time as
        ///     0, so empty lists mean a uniform pick. Returns -1 when nothing is available.
        /// </summary>
        /// <param name="roll">Uniform random value in [0, 1).</param>
        public static int PickWeighted(int count, IReadOnlyList<float> weights, IReadOnlyList<float> unlockSeconds,
            float elapsedSeconds, float roll)
        {
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += AvailableWeight(i, weights, unlockSeconds, elapsedSeconds);
            }

            if (total <= 0f)
            {
                return -1;
            }

            float target = Mathf.Clamp01(roll) * total;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                float weight = AvailableWeight(i, weights, unlockSeconds, elapsedSeconds);
                if (weight <= 0f)
                {
                    continue;
                }

                last = i;
                target -= weight;
                if (target < 0f)
                {
                    return i;
                }
            }

            return last;
        }

        /// <summary>Weight of entry <paramref name="index" /> at <paramref name="elapsedSeconds" />, 0 while locked.</summary>
        public static float AvailableWeight(int index, IReadOnlyList<float> weights, IReadOnlyList<float> unlockSeconds,
            float elapsedSeconds)
        {
            float unlock = unlockSeconds != null && index < unlockSeconds.Count ? unlockSeconds[index] : 0f;
            if (elapsedSeconds < unlock)
            {
                return 0f;
            }

            float weight = weights != null && index < weights.Count ? weights[index] : 1f;
            return weight > 0f ? weight : 0f;
        }

        /// <summary>
        ///     Point on a ring between <paramref name="minRadius" /> and <paramref name="maxRadius" /> around
        ///     <paramref name="center" />, uniform over the ring's area. The axis off the plane keeps the center value.
        /// </summary>
        /// <param name="angle01">Uniform random value in [0, 1) for the angle.</param>
        /// <param name="radius01">Uniform random value in [0, 1) for the distance.</param>
        public static Vector3 RingPoint(Vector3 center, float minRadius, float maxRadius, SpawnPlane plane,
            float angle01, float radius01)
        {
            float inner = Mathf.Max(0f, Mathf.Min(minRadius, maxRadius));
            float outer = Mathf.Max(inner, Mathf.Max(minRadius, maxRadius));
            float radius = Mathf.Sqrt(Mathf.Lerp(inner * inner, outer * outer, Mathf.Clamp01(radius01)));
            float angle = Mathf.Clamp01(angle01) * Mathf.PI * 2f;
            float a = Mathf.Cos(angle) * radius;
            float b = Mathf.Sin(angle) * radius;
            return plane == SpawnPlane.XY
                ? new Vector3(center.x + a, center.y + b, center.z)
                : new Vector3(center.x + a, center.y, center.z + b);
        }

        /// <summary>
        ///     Multiplier on the spawn delay at <paramref name="elapsedSeconds" />: the curve value (x = seconds since
        ///     the start), or 1 for a missing or empty curve. Never below 0.01, so the spawn loop cannot spin.
        /// </summary>
        public static float DelayMultiplier(AnimationCurve curve, float elapsedSeconds)
        {
            if (curve == null || curve.length == 0)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, curve.Evaluate(elapsedSeconds));
        }
    }
}
