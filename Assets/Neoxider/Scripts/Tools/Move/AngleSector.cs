using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Angle-sector clamp shared by <see cref="Follow" /> and <see cref="UniversalRotator" />, so an authored limit
    ///     range means the same thing on both. A sector runs from <c>min</c> to <c>max</c> in increasing angle (degrees,
    ///     any representation): <c>[170, 190]</c> and <c>[170, -170]</c> are the same 20-degree sector across 180, and a
    ///     range whose <c>max</c> is below its <c>min</c> wraps the same way, so <c>[90, -90]</c> is the rear half
    ///     (from 90 through 180 to 270). A range spanning 360 degrees or more covers every angle.
    /// </summary>
    internal static class AngleSector
    {
        private const float FullTurnTolerance = 0.0001f;

        /// <summary>
        ///     True when [<paramref name="min" />, <paramref name="max" />] covers a full turn ([0, 360], [-180, 180],
        ///     [-360, 360] ...), so no angle can be outside it.
        /// </summary>
        internal static bool SpansFullTurn(float min, float max)
        {
            // WHY: both ends of such a range normalize to the same angle, which would otherwise pin every aim to it.
            return Mathf.Abs(max - min) >= 360f - FullTurnTolerance;
        }

        /// <summary>
        ///     Returns <paramref name="angle" /> unchanged when it lies in the sector (or the sector spans a full turn);
        ///     otherwise the nearer edge by angular distance, so an aim crossing behind the sector does not flip to the
        ///     opposite edge. An edge is returned as <c>min</c> or <c>min + span</c>; callers that need a canonical
        ///     range normalize the result.
        /// </summary>
        internal static float Clamp(float angle, float min, float max)
        {
            if (SpansFullTurn(min, max))
            {
                return angle;
            }

            float halfSpan = Mathf.Repeat(max - min, 360f) * 0.5f;
            float centre = min + halfSpan;
            // WHY: measuring from the sector centre makes the result independent of how the aim angle wraps.
            float offset = Mathf.DeltaAngle(centre, angle);
            if (offset >= -halfSpan && offset <= halfSpan)
            {
                return angle;
            }

            return offset > 0f ? centre + halfSpan : centre - halfSpan;
        }
    }
}
