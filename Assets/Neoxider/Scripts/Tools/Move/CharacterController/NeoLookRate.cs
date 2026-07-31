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
    }
}
