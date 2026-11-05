using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     How host time advances the domain: <see cref="AbilitySystem.Advance" /> asks the source how many steps of
    ///     which length a host delta is worth, then ticks the system once per step.
    /// </summary>
    public interface IAbilityTimeSource
    {
        /// <summary>Length of one fixed step in seconds, or 0 for a variable-step source.</summary>
        float StepSeconds { get; }

        /// <summary>Consumes a host delta; returns the number of domain steps to run and their length.</summary>
        int Consume(float hostDeltaSeconds, out float stepSeconds);

        /// <summary>Drops any accumulated time.</summary>
        void Reset();
    }

    /// <summary>Default: one domain step per host frame, exactly the host delta (classic behaviour).</summary>
    public sealed class VariableStepTimeSource : IAbilityTimeSource
    {
        public float StepSeconds => 0f;

        public int Consume(float hostDeltaSeconds, out float stepSeconds)
        {
            stepSeconds = hostDeltaSeconds;
            return hostDeltaSeconds > 0f && !float.IsInfinity(hostDeltaSeconds) ? 1 : 0;
        }

        public void Reset()
        {
        }
    }

    /// <summary>
    ///     Fixed ticks: host time accumulates and the domain advances in whole ticks of 1 / TicksPerSecond seconds,
    ///     at most <see cref="MaxStepsPerCall" /> per call (the rest is dropped and counted in
    ///     <see cref="DiscardedSeconds" />, so a hitch cannot snowball). Every step has the same float length, so the
    ///     same tick sequence replays identically.
    /// </summary>
    public sealed class FixedStepTimeSource : IAbilityTimeSource
    {
        private double _accumulated;

        public FixedStepTimeSource(int ticksPerSecond = 30, int maxStepsPerCall = 8)
        {
            TicksPerSecond = ticksPerSecond < 1 ? 1 : ticksPerSecond;
            MaxStepsPerCall = maxStepsPerCall < 1 ? 1 : maxStepsPerCall;
            StepSeconds = 1f / TicksPerSecond;
        }

        public int TicksPerSecond { get; }
        public int MaxStepsPerCall { get; }
        public float StepSeconds { get; }

        /// <summary>Host time that exceeded the catch-up budget and was dropped.</summary>
        public double DiscardedSeconds { get; private set; }

        /// <summary>Accumulated host time not yet turned into a tick.</summary>
        public double PendingSeconds => _accumulated;

        public int Consume(float hostDeltaSeconds, out float stepSeconds)
        {
            stepSeconds = StepSeconds;
            if (!(hostDeltaSeconds > 0f) || float.IsInfinity(hostDeltaSeconds))
            {
                return 0;
            }

            double step = 1d / TicksPerSecond;
            _accumulated += hostDeltaSeconds;
            double budget = step * MaxStepsPerCall;
            if (_accumulated > budget + step)
            {
                DiscardedSeconds += _accumulated - budget;
                _accumulated = budget;
            }

            int steps = (int)Math.Floor(_accumulated / step + 1e-9);
            if (steps > MaxStepsPerCall)
            {
                steps = MaxStepsPerCall;
            }

            _accumulated -= steps * step;
            if (_accumulated < 0d)
            {
                _accumulated = 0d;
            }

            return steps;
        }

        public void Reset()
        {
            _accumulated = 0d;
            DiscardedSeconds = 0d;
        }

        /// <summary>Converts seconds to whole ticks of this source, rounding half away from zero.</summary>
        public int ToTicks(float seconds)
        {
            return (int)Math.Round(seconds * (double)TicksPerSecond, MidpointRounding.AwayFromZero);
        }
    }
}
