using System;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     Numbers of the client render clock (<see cref="SnapshotTimeline"/>). Plain data, so the timeline is
    ///     testable without a config asset. Use <see cref="Default"/> or <see cref="ForSendRate"/> and tune from there.
    /// </summary>
    public readonly struct SnapshotTimelineSettings
    {
        /// <summary>Render lag, in snapshot intervals, the clock tries to hold behind the newest frame.</summary>
        public readonly float BufferIntervals;

        /// <summary>Floor on the render lag, seconds. Below this a single late packet always freezes a body.</summary>
        public readonly float MinBufferSeconds;

        /// <summary>Ceiling on the render lag, seconds. Above this the world feels like it is on a delay.</summary>
        public readonly float MaxBufferSeconds;

        /// <summary>How many mean arrival-jitter deviations are added on top of the interval-based lag.</summary>
        public readonly float JitterWeight;

        /// <summary>Exponential-average weight of each new interval / jitter observation, 0..1.</summary>
        public readonly float Smoothing;

        /// <summary>
        ///     How hard the clock leans toward its target lag, per second of error. 0.5 means "close half the error
        ///     every second", applied as a speed change so the world never visibly jumps.
        /// </summary>
        public readonly float CatchUpGain;

        /// <summary>The render clock runs between <c>1 - this</c> and <c>1 + this</c> times real time (max 0.5).</summary>
        public readonly float MaxSpeedAdjust;

        /// <summary>An error bigger than this snaps the clock instead of slewing it (reconnect, long stall).</summary>
        public readonly float SnapThresholdSeconds;

        /// <summary>Assumed snapshot interval until two frames have been seen.</summary>
        public readonly float InitialIntervalSeconds;

        /// <summary>
        ///     Window over which the authority's clock rate is measured (server seconds per local second). An
        ///     authority clock that stops and starts (hit-stop, a paused simulation) would otherwise drift away from
        ///     the stamps and slide in front of the newest frame, which reads as bodies stepping instead of gliding.
        /// </summary>
        public readonly float RateWindowSeconds;

        public SnapshotTimelineSettings(
            float bufferIntervals,
            float minBufferSeconds,
            float maxBufferSeconds,
            float jitterWeight,
            float smoothing,
            float catchUpGain,
            float maxSpeedAdjust,
            float snapThresholdSeconds,
            float initialIntervalSeconds,
            float rateWindowSeconds = 0.5f)
        {
            BufferIntervals = bufferIntervals < 1f ? 1f : bufferIntervals;
            MinBufferSeconds = minBufferSeconds < 0f ? 0f : minBufferSeconds;
            MaxBufferSeconds = maxBufferSeconds < MinBufferSeconds ? MinBufferSeconds : maxBufferSeconds;
            JitterWeight = jitterWeight < 0f ? 0f : jitterWeight;
            Smoothing = smoothing <= 0f ? 0.01f : smoothing > 1f ? 1f : smoothing;
            CatchUpGain = catchUpGain < 0f ? 0f : catchUpGain;
            MaxSpeedAdjust = maxSpeedAdjust < 0f ? 0f : maxSpeedAdjust > 0.5f ? 0.5f : maxSpeedAdjust;
            SnapThresholdSeconds = snapThresholdSeconds <= 0f ? 0.5f : snapThresholdSeconds;
            InitialIntervalSeconds = initialIntervalSeconds <= 0f ? 1f / 12f : initialIntervalSeconds;
            RateWindowSeconds = rateWindowSeconds <= 0f ? 0.5f : rateWindowSeconds;
        }

        /// <summary>1.5 intervals of lag plus two jitter deviations, clamped 60..400 ms; assumes 12 Hz until measured.</summary>
        public static SnapshotTimelineSettings Default => ForSendRate(12f);

        /// <summary>
        ///     <see cref="Default"/> numbers for a server that sends <paramref name="snapshotsPerSecond"/> frames per second
        ///     (the initial interval assumption; the clock measures the real one after two frames).
        /// </summary>
        /// <param name="snapshotsPerSecond">Authority snapshot rate in hertz; values at or below 0 fall back to 12.</param>
        public static SnapshotTimelineSettings ForSendRate(float snapshotsPerSecond)
        {
            float rate = snapshotsPerSecond <= 0f ? 12f : snapshotsPerSecond;
            return new SnapshotTimelineSettings(
                bufferIntervals: 1.5f,
                minBufferSeconds: 0.06f,
                maxBufferSeconds: 0.4f,
                jitterWeight: 2f,
                smoothing: 0.1f,
                catchUpGain: 0.5f,
                maxSpeedAdjust: 0.1f,
                snapThresholdSeconds: 0.6f,
                initialIntervalSeconds: 1f / rate,
                rateWindowSeconds: 0.5f);
        }
    }

    /// <summary>
    ///     The client's render clock, expressed in the authority's time: an adaptive jitter buffer for snapshot
    ///     interpolation.
    ///     <para>
    ///         Reading the replicated server time straight off the newest frame is a staircase: it only moves when a
    ///         frame arrives, so a render time derived from it never sits between two frames and every body advances
    ///         in snapshot-rate steps instead of gliding.
    ///     </para>
    ///     <para>
    ///         This clock free-runs on the client's own delta time and is steered, not stepped: it is nudged a few
    ///         percent faster or slower so the lag behind the newest frame converges on <see cref="BufferSeconds"/>,
    ///         which itself follows the measured snapshot interval plus the arrival jitter. A burst of late packets
    ///         widens the buffer (smoothness over latency) and a clean link narrows it again; a long stall snaps the
    ///         clock instead of fast-forwarding visibly.
    ///     </para>
    ///     <para>
    ///         Usage: call <see cref="OnFrame(double,double)"/> for every received snapshot with the authority's stamp
    ///         (for example <c>NetworkTime.time</c> on the server when it wrote the frame) and the local arrival
    ///         time; call <see cref="Advance"/> once per rendered frame; sample your
    ///         <see cref="SnapshotBuffer{T}"/> at <see cref="RenderTime"/>. Pure C#: no Unity, no Mirror, no allocation
    ///         after construction.
    ///     </para>
    /// </summary>
    public sealed class SnapshotTimeline
    {
        private const double MinInterval = 0.005d;
        private const double MaxInterval = 1d;
        private const double MinRate = 0.05d;
        private const double MaxRate = 1.5d;
        private const double RateSmoothing = 0.5d;

        private readonly SnapshotTimelineSettings _settings;
        private double _newestRemote;
        private double _localTimeline;
        private double _lastServerTime;
        private double _lastArrival;
        private double _interval;
        private double _jitter;
        private double _rate = 1d;
        private double _rateAnchorServer;
        private double _rateAnchorLocal;
        private bool _hasFrame;
        private bool _hasInterval;

        public SnapshotTimeline(SnapshotTimelineSettings settings)
        {
            _settings = settings;
            _interval = settings.InitialIntervalSeconds;
        }

        /// <summary>True once the first frame has been observed.</summary>
        public bool HasTimeline => _hasFrame;

        /// <summary>Smoothed seconds between consecutive frames, as stamped by the authority.</summary>
        public float SendIntervalSeconds => (float)_interval;

        /// <summary>Smoothed mean deviation of frame arrival spacing from the authority's spacing: the link jitter.</summary>
        public float JitterSeconds => (float)_jitter;

        /// <summary>
        ///     Measured server seconds per local second (1 normally, below 1 while the authority's game clock is slowed
        ///     or stopped). The render clock advances at this rate, so an authority hit-stop does not push it ahead of
        ///     the frames.
        /// </summary>
        public float ServerClockRate => (float)_rate;

        /// <summary>The render lag the clock is currently steering toward.</summary>
        public float BufferSeconds => (float)Buffer;

        /// <summary>The time, in authority seconds, bodies are drawn at. Always at or behind the newest frame.</summary>
        public float RenderTime => (float)_localTimeline;

        /// <summary>Same as <see cref="RenderTime"/> with full double precision (long sessions).</summary>
        public double RenderTimeExact => _localTimeline;

        /// <summary>Estimate of the authority's current time: the render clock plus the lag it holds.</summary>
        public float ServerNow => (float)(_localTimeline + Buffer);

        /// <summary>How far the render clock currently trails the newest frame, seconds.</summary>
        public float LagSeconds => _hasFrame ? (float)(_newestRemote - _localTimeline) : 0f;

        private double Buffer
        {
            get
            {
                double wanted = _interval * _settings.BufferIntervals + _jitter * _settings.JitterWeight;
                return wanted < _settings.MinBufferSeconds ? _settings.MinBufferSeconds
                    : wanted > _settings.MaxBufferSeconds ? _settings.MaxBufferSeconds
                    : wanted;
            }
        }

        /// <summary>Float convenience overload of <see cref="OnFrame(double,double)"/>.</summary>
        public void OnFrame(float serverTime, float localTime)
        {
            OnFrame((double)serverTime, (double)localTime);
        }

        /// <summary>
        ///     A frame arrived. <paramref name="serverTime"/> is the authority's stamp, <paramref name="localTime"/> the
        ///     local clock at arrival (<c>Time.realtimeSinceStartupAsDouble</c> works). Out-of-order stamps are ignored
        ///     for the clock.
        /// </summary>
        public void OnFrame(double serverTime, double localTime)
        {
            if (!_hasFrame)
            {
                _hasFrame = true;
                _newestRemote = serverTime;
                _lastServerTime = serverTime;
                _lastArrival = localTime;
                _rateAnchorServer = serverTime;
                _rateAnchorLocal = localTime;
                _localTimeline = serverTime - Buffer;
                return;
            }

            if (serverTime <= _lastServerTime)
            {
                return;
            }

            double serverStep = serverTime - _lastServerTime;
            double arrivalStep = localTime - _lastArrival;
            if (arrivalStep < 0d)
            {
                arrivalStep = 0d;
            }

            double clampedStep = Clamp(serverStep, MinInterval, MaxInterval);
            if (!_hasInterval)
            {
                _interval = clampedStep;
                _hasInterval = true;
            }
            else
            {
                _interval += (clampedStep - _interval) * _settings.Smoothing;
            }

            // arrival spacing is compared with the spacing the authority's own clock implies at the measured rate
            double expectedArrival = _rate > MinRate ? serverStep / _rate : serverStep;
            double deviation = Math.Abs(arrivalStep - expectedArrival);
            _jitter += (deviation - _jitter) * _settings.Smoothing;

            double windowLocal = localTime - _rateAnchorLocal;
            if (windowLocal >= _settings.RateWindowSeconds)
            {
                double measured = Clamp((serverTime - _rateAnchorServer) / windowLocal, 0d, MaxRate);
                _rate += (measured - _rate) * RateSmoothing;
                _rateAnchorServer = serverTime;
                _rateAnchorLocal = localTime;
            }

            _newestRemote = serverTime;
            _lastServerTime = serverTime;
            _lastArrival = localTime;
        }

        /// <summary>Advances the render clock by one frame's worth of real time, steering it toward the target lag.</summary>
        /// <param name="deltaSeconds">Real (unscaled) seconds since the previous call.</param>
        public void Advance(float deltaSeconds)
        {
            if (!_hasFrame || deltaSeconds <= 0f)
            {
                return;
            }

            double target = Buffer;
            double error = (_newestRemote - _localTimeline) - target;

            if (Math.Abs(error) > _settings.SnapThresholdSeconds)
            {
                _localTimeline = _newestRemote - target;
                return;
            }

            double adjust = error * _settings.CatchUpGain;
            if (adjust > _settings.MaxSpeedAdjust)
            {
                adjust = _settings.MaxSpeedAdjust;
            }
            else if (adjust < -_settings.MaxSpeedAdjust)
            {
                adjust = -_settings.MaxSpeedAdjust;
            }

            _localTimeline += deltaSeconds * _rate * (1d + adjust);
        }

        /// <summary>Forgets everything. Call on disconnect and on a restarted match.</summary>
        public void Reset()
        {
            _hasFrame = false;
            _hasInterval = false;
            _newestRemote = 0d;
            _localTimeline = 0d;
            _lastServerTime = 0d;
            _lastArrival = 0d;
            _interval = _settings.InitialIntervalSeconds;
            _jitter = 0d;
            _rate = 1d;
            _rateAnchorServer = 0d;
            _rateAnchorLocal = 0d;
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
