using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Animation
{
    public enum AnimPhase
    {
        Before,
        Active,
        After
    }

    public readonly struct TimelineSample
    {
        public TimelineSample(AnimPhase phase, double progress, int iteration)
        {
            Phase = phase;
            Progress = progress;
            Iteration = iteration;
        }

        public AnimPhase Phase { get; }

        public double Progress { get; }

        public int Iteration { get; }
    }

    public static class AnimTimeline
    {
        public static double TotalDuration(double duration, double delay, double iterations)
        {
            return double.IsPositiveInfinity(iterations) ? double.PositiveInfinity : delay + duration * iterations;
        }

        public static TimelineSample Evaluate(double elapsed, double duration, double delay, double iterations, AnimDirection direction)
        {
            if (duration <= 0.0)
                duration = 0.0001;
            if (iterations <= 0.0)
                return new TimelineSample(AnimPhase.After, DirectedProgress(direction, 0, 1.0), 0);
            double active = elapsed - delay;
            if (active < 0.0)
                return new TimelineSample(AnimPhase.Before, DirectedProgress(direction, 0, 0.0), 0);
            double position = active / duration;
            if (!double.IsPositiveInfinity(iterations) && position >= iterations)
            {
                int last = (int)Math.Ceiling(iterations) - 1;
                double fraction = iterations - Math.Floor(iterations);
                double local = fraction > 1e-9 ? fraction : 1.0;
                return new TimelineSample(AnimPhase.After, DirectedProgress(direction, last, local), last);
            }

            int iteration = (int)Math.Floor(position);
            return new TimelineSample(AnimPhase.Active, DirectedProgress(direction, iteration, position - iteration), iteration);
        }

        public static double DirectedProgress(AnimDirection direction, int iteration, double local)
        {
            bool even = (iteration & 1) == 0;
            switch (direction)
            {
                case AnimDirection.Reverse:
                    return 1.0 - local;
                case AnimDirection.Alternate:
                    return even ? local : 1.0 - local;
                case AnimDirection.AlternateReverse:
                    return even ? 1.0 - local : local;
                default:
                    return local;
            }
        }

        public static bool FillApplies(AnimPhase phase, AnimFill fill)
        {
            switch (phase)
            {
                case AnimPhase.Before:
                    return fill == AnimFill.Backwards || fill == AnimFill.Both;
                case AnimPhase.After:
                    return fill == AnimFill.Forwards || fill == AnimFill.Both;
                default:
                    return true;
            }
        }

        public static double StaggerDelay(int index, double stagger)
        {
            return index <= 0 || stagger <= 0.0 ? 0.0 : index * stagger;
        }
    }

    public readonly struct AnimFrame
    {
        public AnimFrame(TimelineSample sample, bool justStarted, bool iterationCompleted, bool justFinished, bool applyValues, bool revert)
        {
            Sample = sample;
            JustStarted = justStarted;
            IterationCompleted = iterationCompleted;
            JustFinished = justFinished;
            ApplyValues = applyValues;
            Revert = revert;
        }

        public TimelineSample Sample { get; }

        public bool JustStarted { get; }

        public bool IterationCompleted { get; }

        public bool JustFinished { get; }

        public bool ApplyValues { get; }

        public bool Revert { get; }
    }

    public sealed class AnimInstance
    {
        private readonly AnimDef _def;
        private readonly double _delay;
        private bool _started;
        private int _lastIteration;

        public AnimInstance(AnimDef def, double extraDelay)
        {
            _def = def;
            _delay = def.Delay + Math.Max(0.0, extraDelay);
        }

        public AnimDef Definition => _def;

        public double Elapsed { get; private set; }

        public bool Finished { get; private set; }

        public AnimFrame Advance(double deltaSeconds)
        {
            if (Finished)
                return new AnimFrame(Sample(), false, false, false, false, false);
            Elapsed += Math.Max(0.0, deltaSeconds);
            TimelineSample sample = Sample();
            bool justStarted = false;
            if (!_started && sample.Phase != AnimPhase.Before)
            {
                _started = true;
                justStarted = true;
            }

            bool iterationCompleted = _started && sample.Phase != AnimPhase.Before && sample.Iteration > _lastIteration;
            if (iterationCompleted)
                _lastIteration = sample.Iteration;
            bool justFinished = sample.Phase == AnimPhase.After;
            if (justFinished)
                Finished = true;
            bool apply = sample.Phase == AnimPhase.Active || AnimTimeline.FillApplies(sample.Phase, _def.Fill);
            bool revert = justFinished && !AnimTimeline.FillApplies(AnimPhase.After, _def.Fill);
            return new AnimFrame(sample, justStarted, iterationCompleted, justFinished, apply, revert);
        }

        private TimelineSample Sample()
        {
            return AnimTimeline.Evaluate(Elapsed, _def.Duration, _delay, _def.Iterations, _def.Direction);
        }
    }

    public sealed class PreparedAnim
    {
        private sealed class Track
        {
            public string Property;
            public float[] Offsets;
            public AnimValue[] Values;
            public string[] Easings;
        }

        private readonly Track[] _tracks;
        private readonly string _defaultEasing;

        private PreparedAnim(AnimDef def, Track[] tracks)
        {
            Definition = def;
            _tracks = tracks;
            _defaultEasing = def.Easing;
        }

        public AnimDef Definition { get; }

        public static PreparedAnim Prepare(AnimDef def)
        {
            List<string> order = new List<string>();
            Dictionary<string, List<int>> map = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            Dictionary<string, List<AnimValue>> values = new Dictionary<string, List<AnimValue>>(StringComparer.Ordinal);
            for (int i = 0; i < def.Steps.Count; i++)
            {
                AnimStep step = def.Steps[i];
                for (int p = 0; p < step.Set.Count; p++)
                {
                    string property = step.Set[p].Key;
                    if (!map.TryGetValue(property, out List<int> indexes))
                    {
                        indexes = new List<int>();
                        map[property] = indexes;
                        values[property] = new List<AnimValue>();
                        order.Add(property);
                    }

                    indexes.Add(i);
                    values[property].Add(AnimValue.Parse(step.Set[p].Value));
                }
            }

            Track[] tracks = new Track[order.Count];
            for (int t = 0; t < order.Count; t++)
            {
                List<int> indexes = map[order[t]];
                Track track = new Track
                {
                    Property = order[t],
                    Offsets = new float[indexes.Count],
                    Values = values[order[t]].ToArray(),
                    Easings = new string[indexes.Count]
                };
                for (int k = 0; k < indexes.Count; k++)
                {
                    track.Offsets[k] = def.Steps[indexes[k]].Offset;
                    track.Easings[k] = def.Steps[indexes[k]].Easing;
                }

                tracks[t] = track;
            }

            return new PreparedAnim(def, tracks);
        }

        public int PropertyCount => _tracks.Length;

        public string PropertyAt(int index) => _tracks[index].Property;

        public AnimValue SampleProperty(int index, double progress, Func<string, AnimValue> baseResolver)
        {
            Track track = _tracks[index];
            int last = track.Offsets.Length - 1;
            float first = track.Offsets[0];
            if (progress <= first)
            {
                AnimValue start = first > 0f && baseResolver != null ? baseResolver(track.Property) : null;
                if (start == null || first <= 0f)
                    return track.Values[0];
                return AnimValue.Lerp(start, track.Values[0], (float)Ease(_defaultEasing, progress / first), track.Property);
            }

            if (progress >= track.Offsets[last])
            {
                float lastOffset = track.Offsets[last];
                AnimValue end = lastOffset < 1f && baseResolver != null ? baseResolver(track.Property) : null;
                if (end == null || lastOffset >= 1f)
                    return track.Values[last];
                double local = (progress - lastOffset) / (1.0 - lastOffset);
                return AnimValue.Lerp(track.Values[last], end, (float)Ease(track.Easings[last] ?? _defaultEasing, local), track.Property);
            }

            for (int i = 0; i < last; i++)
            {
                float from = track.Offsets[i];
                float to = track.Offsets[i + 1];
                if (progress >= from && progress < to && to > from)
                {
                    double local = (progress - from) / (to - from);
                    return AnimValue.Lerp(track.Values[i], track.Values[i + 1], (float)Ease(track.Easings[i] ?? _defaultEasing, local), track.Property);
                }
            }

            return track.Values[last];
        }

        private static double Ease(string easing, double t)
        {
            return Easing.Evaluate(easing, t);
        }
    }

    public readonly struct MotionPlan
    {
        public MotionPlan(bool skip, bool jumpToEnd, AnimDef definition)
        {
            Skip = skip;
            JumpToEnd = jumpToEnd;
            Definition = definition;
        }

        public bool Skip { get; }

        public bool JumpToEnd { get; }

        public AnimDef Definition { get; }
    }

    public static class ReducedMotionPolicy
    {
        public const double SimplifiedDuration = 0.15;

        public static MotionPlan Apply(AnimDef def, ReducedMotionMode mode, bool systemReduced)
        {
            if (!systemReduced || mode == ReducedMotionMode.Off)
                return new MotionPlan(false, false, def);
            if (mode == ReducedMotionMode.Skip)
                return new MotionPlan(true, !def.IsInfinite && (def.Fill == AnimFill.Forwards || def.Fill == AnimFill.Both), def);
            List<AnimStep> steps = new List<AnimStep>();
            for (int i = 0; i < def.Steps.Count; i++)
            {
                List<KeyValuePair<string, string>> set = new List<KeyValuePair<string, string>>();
                for (int p = 0; p < def.Steps[i].Set.Count; p++)
                {
                    string property = def.Steps[i].Set[p].Key;
                    if (string.Equals(property, "opacity", StringComparison.Ordinal))
                        set.Add(def.Steps[i].Set[p]);
                }

                steps.Add(new AnimStep(def.Steps[i].Offset, def.Steps[i].Easing, set));
            }

            bool any = false;
            for (int i = 0; i < steps.Count; i++)
                any |= steps[i].Set.Count > 0;
            if (!any)
                return new MotionPlan(true, !def.IsInfinite && (def.Fill == AnimFill.Forwards || def.Fill == AnimFill.Both), def);
            AnimDef simplified = def.With(Math.Min(def.Duration, SimplifiedDuration), 0.0, 1.0, AnimDirection.Normal, steps);
            return new MotionPlan(false, false, simplified);
        }
    }
}
