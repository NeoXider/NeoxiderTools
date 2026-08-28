using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Converts frame-based pointer deltas into the per-second rate the CMF camera controller expects.
    /// </summary>
    /// <remarks>
    ///     CMF's <c>CameraController</c> multiplies the value it reads by <c>cameraSpeed * Time.deltaTime</c>. A mouse
    ///     delta is already accumulated per frame, so it must be divided by delta time first — otherwise the two
    ///     multiplications cancel out differently at different frame rates and sensitivity drifts with FPS. A gamepad
    ///     stick is a continuous rate and must <em>not</em> go through this conversion.
    /// </remarks>
    public static class NeoLookRate
    {
        /// <summary>
        ///     Discards an implausible pointer jump, converts the remaining frame delta to a rate and combines it with
        ///     the continuous gamepad-stick rate.
        /// </summary>
        /// <param name="pointerDelta">Raw pointer delta accumulated during this frame, in pixels.</param>
        /// <param name="maxPointerDeltaPerFrame">
        ///     Maximum accepted pointer-delta magnitude in pixels. Zero or less disables outlier filtering.
        /// </param>
        /// <param name="pointerScale">Scale applied to the accepted pointer delta.</param>
        /// <param name="stickRate">Continuous stick rate. This value is never filtered as a pointer delta.</param>
        /// <param name="deltaTime">Scaled frame time.</param>
        /// <param name="timeScale">Current <c>Time.timeScale</c>.</param>
        /// <returns>The combined pointer and stick look rate.</returns>
        public static Vector2 FromPointerDeltaAndStick(
            Vector2 pointerDelta,
            float maxPointerDeltaPerFrame,
            float pointerScale,
            Vector2 stickRate,
            float deltaTime,
            float timeScale)
        {
            Vector2 filteredPointerDelta = DiscardPointerDeltaOutlier(pointerDelta, maxPointerDeltaPerFrame);
            Vector2 scaledPointerDelta = filteredPointerDelta * pointerScale;
            Vector2 pointerRate = new Vector2(
                FromFrameDelta(scaledPointerDelta.x, deltaTime, timeScale),
                FromFrameDelta(scaledPointerDelta.y, deltaTime, timeScale));

            return pointerRate + stickRate;
        }

        /// <summary>
        ///     Converts a per-frame delta into a per-second rate.
        /// </summary>
        /// <param name="frameDelta">Delta accumulated during this frame (e.g. mouse pixels).</param>
        /// <param name="deltaTime">Scaled frame time.</param>
        /// <param name="timeScale">Current <c>Time.timeScale</c>.</param>
        /// <returns>
        ///     The equivalent rate, or 0 when time is stopped — a paused game must not produce an infinite (NaN) look
        ///     delta, which is the bug CMF 2.2 fixed in its own mouse input script.
        /// </returns>
        public static float FromFrameDelta(float frameDelta, float deltaTime, float timeScale)
        {
            if (timeScale <= 0f || deltaTime <= 0f)
            {
                return 0f;
            }

            return frameDelta / deltaTime * timeScale;
        }

        private static Vector2 DiscardPointerDeltaOutlier(Vector2 pointerDelta, float maxPointerDeltaPerFrame)
        {
            if (maxPointerDeltaPerFrame <= 0f)
            {
                return pointerDelta;
            }

            float maxPointerDeltaSquared = maxPointerDeltaPerFrame * maxPointerDeltaPerFrame;
            return pointerDelta.sqrMagnitude > maxPointerDeltaSquared ? Vector2.zero : pointerDelta;
        }
    }
}
