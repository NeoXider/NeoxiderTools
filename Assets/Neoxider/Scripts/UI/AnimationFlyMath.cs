using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    ///     Engine-free number rules behind <see cref="AnimationFly" />: item count, duration, item delay,
    ///     stage ratios and the magnet approach point. Kept apart from the component so the rules are
    ///     testable without a scene, DOTween or a singleton.
    /// </summary>
    internal static class AnimationFlyMath
    {
        internal const float MinDuration = 0.001f;
        internal const float MinStageRatio = 0.05f;
        internal const float MaxStageRatio = 0.85f;

        /// <summary>True for every value that is neither NaN nor infinite.</summary>
        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        ///     Final number of spawned items: <c>ceil(count * requestMultiplier * globalMultiplier)</c> clamped
        ///     to <c>[0, cap]</c>, where the cap is the request limit (when positive) bounded by the global one.
        ///     A NaN product spawns nothing; a product beyond the cap (infinity included) spawns the cap.
        /// </summary>
        internal static int ResolveCount(int count, float requestMultiplier, float globalMultiplier,
            int requestMaxCount, int globalMaxCount)
        {
            int cap = requestMaxCount > 0 ? Mathf.Min(requestMaxCount, globalMaxCount) : globalMaxCount;
            float product = count * requestMultiplier * globalMultiplier;
            if (cap <= 0 || float.IsNaN(product) || product <= 0f)
            {
                return 0;
            }

            // WHY: the cap is compared before the int conversion; a product above int.MaxValue would
            // otherwise wrap to int.MinValue in CeilToInt and spawn nothing instead of the cap.
            if (product >= cap)
            {
                return cap;
            }

            return Mathf.Min(cap, Mathf.CeilToInt(product));
        }

        /// <summary>A positive finite speed multiplier, or 1 for anything else.</summary>
        internal static float ResolveSpeedMultiplier(float speedMultiplier)
        {
            return IsFinite(speedMultiplier) && speedMultiplier > 0f ? speedMultiplier : 1f;
        }

        /// <summary>
        ///     Flight duration in seconds: the request override or <paramref name="fallback" />, never below
        ///     <see cref="MinDuration" />, divided by the speed multiplier.
        /// </summary>
        internal static float ResolveDuration(float? requested, float fallback, float speedMultiplier)
        {
            float duration = requested ?? fallback;
            if (!IsFinite(duration))
            {
                duration = IsFinite(fallback) ? fallback : 1f;
            }

            duration = Mathf.Max(MinDuration, duration);
            return duration / ResolveSpeedMultiplier(speedMultiplier);
        }

        /// <summary>
        ///     Pause between two spawned items: the request override or <paramref name="fallback" />, never
        ///     negative, divided by the speed multiplier.
        /// </summary>
        internal static float ResolveItemDelay(float? requested, float fallback, float speedMultiplier)
        {
            float delay = requested ?? fallback;
            if (!IsFinite(delay))
            {
                delay = IsFinite(fallback) ? fallback : 0f;
            }

            delay = Mathf.Max(0f, delay);
            return delay / ResolveSpeedMultiplier(speedMultiplier);
        }

        /// <summary>Clamps a burst/magnet stage share of the total duration to the supported range.</summary>
        internal static float ClampStageRatio(float ratio)
        {
            return Mathf.Clamp(ratio, MinStageRatio, MaxStageRatio);
        }

        /// <summary>
        ///     Point at <paramref name="distance" /> from <paramref name="end" /> on the way back to
        ///     <paramref name="start" />. Degenerate input falls back to 85% of the straight path.
        /// </summary>
        internal static Vector3 MagnetApproachPoint(Vector3 start, Vector3 end, float distance)
        {
            Vector3 direction = start - end;
            if (direction.sqrMagnitude < 0.0001f || distance <= 0f)
            {
                return Vector3.Lerp(start, end, 0.85f);
            }

            return end + direction.normalized * distance;
        }

        /// <summary>Random offset inside <c>[-|maxAbs|, |maxAbs|]</c> on every axis.</summary>
        internal static Vector3 RandomOffset(Vector3 maxAbs)
        {
            return new Vector3(
                Random.Range(-Mathf.Abs(maxAbs.x), Mathf.Abs(maxAbs.x)),
                Random.Range(-Mathf.Abs(maxAbs.y), Mathf.Abs(maxAbs.y)),
                Random.Range(-Mathf.Abs(maxAbs.z), Mathf.Abs(maxAbs.z)));
        }
    }
}
