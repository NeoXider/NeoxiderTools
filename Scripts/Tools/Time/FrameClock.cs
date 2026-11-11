namespace Neo.Tools
{
    /// <summary>
    ///     Per-frame time source for frame-paced timers (<see cref="Timer" />, <see cref="TimerObject" />).
    ///     The default is <see cref="UnityFrameClock.Instance" />; inject <see cref="ManualFrameClock" /> or a custom
    ///     implementation to drive pacing from a replay, a server simulation or a deterministic test.
    ///     Wall-clock (UTC) time is a separate seam: <see cref="INeoClock" />.
    /// </summary>
    public interface IFrameClock
    {
        /// <summary>Scaled seconds since the previous frame.</summary>
        float DeltaTime { get; }

        /// <summary>Seconds since the previous frame, ignoring the game speed.</summary>
        float UnscaledDeltaTime { get; }

        /// <summary>Current game speed multiplier; 0 means the game is paused.</summary>
        float TimeScale { get; }
    }

    /// <summary>Default <see cref="IFrameClock" /> backed by <see cref="UnityEngine.Time" />.</summary>
    public sealed class UnityFrameClock : IFrameClock
    {
        /// <summary>Shared stateless instance.</summary>
        public static readonly UnityFrameClock Instance = new();

        private UnityFrameClock()
        {
        }

        /// <inheritdoc />
        public float DeltaTime => UnityEngine.Time.deltaTime;

        /// <inheritdoc />
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;

        /// <inheritdoc />
        public float TimeScale => UnityEngine.Time.timeScale;
    }

    /// <summary>
    ///     Settable <see cref="IFrameClock" /> for tests, replays and fixed-step simulations. The timer reads the current
    ///     values once per frame, so set them before the frame you want to simulate.
    /// </summary>
    public sealed class ManualFrameClock : IFrameClock
    {
        /// <summary>Creates a clock that reports <paramref name="unscaledDeltaTime" /> every frame.</summary>
        public ManualFrameClock(float unscaledDeltaTime = 0f, float timeScale = 1f)
        {
            TimeScale = timeScale;
            SetFrame(unscaledDeltaTime);
        }

        /// <inheritdoc />
        public float DeltaTime { get; set; }

        /// <inheritdoc />
        public float UnscaledDeltaTime { get; set; }

        /// <inheritdoc />
        public float TimeScale { get; set; }

        /// <summary>
        ///     Sets the unscaled frame delta and derives <see cref="DeltaTime" /> from <see cref="TimeScale" />,
        ///     matching how Unity relates the two values.
        /// </summary>
        public void SetFrame(float unscaledDeltaTime)
        {
            UnscaledDeltaTime = unscaledDeltaTime;
            DeltaTime = unscaledDeltaTime * TimeScale;
        }
    }

    /// <summary>Helpers shared by frame-paced timers.</summary>
    public static class FrameClockExtensions
    {
        /// <summary>Returns the unscaled or scaled frame delta of <paramref name="clock" />.</summary>
        public static float GetDeltaTime(this IFrameClock clock, bool unscaled)
        {
            return unscaled ? clock.UnscaledDeltaTime : clock.DeltaTime;
        }
    }
}
