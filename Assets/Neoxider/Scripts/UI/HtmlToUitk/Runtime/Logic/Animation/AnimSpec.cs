using System;
using System.Collections.Generic;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Animation
{
    public enum AnimDirection
    {
        Normal,
        Reverse,
        Alternate,
        AlternateReverse
    }

    public enum AnimFill
    {
        None,
        Forwards,
        Backwards,
        Both
    }

    public enum AnimTrigger
    {
        OnShow,
        OnHide,
        OnLoad,
        Manual
    }

    public enum ReducedMotionMode
    {
        Off,
        Skip,
        Simplify
    }

    public sealed class AnimStep
    {
        public AnimStep(float offset, string easing, IReadOnlyList<KeyValuePair<string, string>> set)
        {
            Offset = offset;
            Easing = easing;
            Set = set;
        }

        public float Offset { get; }

        public string Easing { get; }

        public IReadOnlyList<KeyValuePair<string, string>> Set { get; }
    }

    public sealed class AnimDef
    {
        public AnimDef(string id, string keyframes, double duration, double delay, double iterations, AnimDirection direction, AnimFill fill, string easing, IReadOnlyList<AnimStep> steps)
        {
            Id = id;
            Keyframes = keyframes;
            Duration = duration;
            Delay = delay;
            Iterations = iterations;
            Direction = direction;
            Fill = fill;
            Easing = easing;
            Steps = steps;
        }

        public string Id { get; }

        public string Keyframes { get; }

        public double Duration { get; }

        public double Delay { get; }

        public double Iterations { get; }

        public bool IsInfinite => double.IsPositiveInfinity(Iterations);

        public AnimDirection Direction { get; }

        public AnimFill Fill { get; }

        public string Easing { get; }

        public IReadOnlyList<AnimStep> Steps { get; }

        public AnimDef With(double duration, double delay, double iterations, AnimDirection direction, IReadOnlyList<AnimStep> steps)
        {
            return new AnimDef(Id, Keyframes, duration, delay, iterations, direction, Fill, Easing, steps);
        }

        public AnimDef WithFill(AnimFill fill)
        {
            return new AnimDef(Id, Keyframes, Duration, Delay, Iterations, Direction, fill, Easing, Steps);
        }

        public IReadOnlyList<string> Properties()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            List<string> list = new List<string>();
            for (int i = 0; i < Steps.Count; i++)
            {
                for (int p = 0; p < Steps[i].Set.Count; p++)
                {
                    if (seen.Add(Steps[i].Set[p].Key))
                        list.Add(Steps[i].Set[p].Key);
                }
            }

            return list;
        }
    }

    public sealed class AnimBinding
    {
        public AnimBinding(string selector, string animationId, AnimTrigger trigger, double stagger, ReducedMotionMode reducedMotion)
        {
            Selector = selector;
            AnimationId = animationId;
            Trigger = trigger;
            Stagger = stagger;
            ReducedMotion = reducedMotion;
        }

        public string Selector { get; }

        public string AnimationId { get; }

        public AnimTrigger Trigger { get; }

        public double Stagger { get; }

        public ReducedMotionMode ReducedMotion { get; }
    }

    public sealed class AnimSpec
    {
        private readonly Dictionary<string, AnimDef> _byId;

        public AnimSpec(IReadOnlyList<AnimDef> animations, IReadOnlyList<AnimBinding> bindings)
        {
            Animations = animations;
            Bindings = bindings;
            _byId = new Dictionary<string, AnimDef>(StringComparer.Ordinal);
            for (int i = 0; i < animations.Count; i++)
                _byId[animations[i].Id] = animations[i];
        }

        public IReadOnlyList<AnimDef> Animations { get; }

        public IReadOnlyList<AnimBinding> Bindings { get; }

        public AnimDef Find(string id)
        {
            return id != null && _byId.TryGetValue(id, out AnimDef def) ? def : null;
        }

        public static AnimSpec Parse(string json)
        {
            return FromJson(MiniJson.Parse(json));
        }

        public static AnimSpec FromJson(JsonValue root)
        {
            List<AnimDef> animations = new List<AnimDef>();
            JsonValue list = root["animations"];
            for (int i = 0; i < list.Count; i++)
                animations.Add(ParseDef(list[i]));

            List<AnimBinding> bindings = new List<AnimBinding>();
            JsonValue bindingList = root["bindings"];
            for (int i = 0; i < bindingList.Count; i++)
            {
                JsonValue b = bindingList[i];
                bindings.Add(new AnimBinding(
                    b["selector"].AsString(string.Empty),
                    b["animation"].AsString(string.Empty),
                    ParseTrigger(b["trigger"].AsString("manual")),
                    b["stagger"].AsNumber(0),
                    ParseReduced(b["reducedMotion"].AsString("off"))));
            }

            return new AnimSpec(animations, bindings);
        }

        private static AnimDef ParseDef(JsonValue d)
        {
            double iterations = 1;
            JsonValue it = d["iterations"];
            if (it.Kind == JsonKind.String && string.Equals(it.AsString(), "infinite", StringComparison.Ordinal))
                iterations = double.PositiveInfinity;
            else if (it.Kind == JsonKind.Number)
                iterations = Math.Max(0, it.AsNumber(1));

            List<AnimStep> steps = new List<AnimStep>();
            JsonValue stepList = d["steps"];
            for (int i = 0; i < stepList.Count; i++)
            {
                JsonValue s = stepList[i];
                List<KeyValuePair<string, string>> set = new List<KeyValuePair<string, string>>();
                JsonValue setObject = s["set"];
                for (int m = 0; m < setObject.Members.Count; m++)
                    set.Add(new KeyValuePair<string, string>(setObject.Members[m].Key, setObject.Members[m].Value.AsString(string.Empty)));
                float offset = (float)Math.Min(1.0, Math.Max(0.0, s["offset"].AsNumber(0)));
                steps.Add(new AnimStep(offset, s["easing"].AsString(), set));
            }

            return new AnimDef(
                d["id"].AsString(string.Empty),
                d["keyframes"].AsString(),
                Math.Max(0.0001, d["duration"].AsNumber(0.3)),
                Math.Max(0, d["delay"].AsNumber(0)),
                iterations,
                ParseDirection(d["direction"].AsString("normal")),
                ParseFill(d["fill"].AsString("none")),
                d["easing"].AsString("ease"),
                StableSort(steps));
        }

        private static IReadOnlyList<AnimStep> StableSort(List<AnimStep> steps)
        {
            List<KeyValuePair<int, AnimStep>> indexed = new List<KeyValuePair<int, AnimStep>>();
            for (int i = 0; i < steps.Count; i++)
                indexed.Add(new KeyValuePair<int, AnimStep>(i, steps[i]));
            indexed.Sort((a, b) =>
            {
                int c = a.Value.Offset.CompareTo(b.Value.Offset);
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            List<AnimStep> result = new List<AnimStep>(indexed.Count);
            for (int i = 0; i < indexed.Count; i++)
                result.Add(indexed[i].Value);
            return result;
        }

        public static AnimDirection ParseDirection(string text)
        {
            switch (text)
            {
                case "reverse": return AnimDirection.Reverse;
                case "alternate": return AnimDirection.Alternate;
                case "alternate-reverse": return AnimDirection.AlternateReverse;
                default: return AnimDirection.Normal;
            }
        }

        public static AnimFill ParseFill(string text)
        {
            switch (text)
            {
                case "forwards": return AnimFill.Forwards;
                case "backwards": return AnimFill.Backwards;
                case "both": return AnimFill.Both;
                default: return AnimFill.None;
            }
        }

        public static AnimTrigger ParseTrigger(string text)
        {
            switch (text)
            {
                case "onShow": return AnimTrigger.OnShow;
                case "onHide": return AnimTrigger.OnHide;
                case "onLoad": return AnimTrigger.OnLoad;
                default: return AnimTrigger.Manual;
            }
        }

        public static ReducedMotionMode ParseReduced(string text)
        {
            switch (text)
            {
                case "skip": return ReducedMotionMode.Skip;
                case "simplify": return ReducedMotionMode.Simplify;
                default: return ReducedMotionMode.Off;
            }
        }
    }
}
