using System;
using System.Collections.Generic;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Responsive
{
    public enum BreakpointOrientation
    {
        Any,
        Portrait,
        Landscape
    }

    public sealed class BreakpointQuery
    {
        public double? MinWidth;
        public double? MaxWidth;
        public double? MinHeight;
        public double? MaxHeight;
        public double? MinAspect;
        public double? MaxAspect;
        public BreakpointOrientation Orientation;

        public bool Matches(double width, double height)
        {
            if (double.IsNaN(width) || double.IsInfinity(width) || double.IsNaN(height) || double.IsInfinity(height)
                || width <= 0.0 || height <= 0.0) return false;
            if (MinWidth.HasValue && width < MinWidth.Value)
                return false;
            if (MaxWidth.HasValue && width > MaxWidth.Value)
                return false;
            if (MinHeight.HasValue && height < MinHeight.Value)
                return false;
            if (MaxHeight.HasValue && height > MaxHeight.Value)
                return false;
            if (MinAspect.HasValue || MaxAspect.HasValue)
            {
                if (height <= 0.0)
                    return false;
                double aspect = width / height;
                if (MinAspect.HasValue && aspect < MinAspect.Value)
                    return false;
                if (MaxAspect.HasValue && aspect > MaxAspect.Value)
                    return false;
            }

            if (Orientation == BreakpointOrientation.Portrait && width > height)
                return false;
            if (Orientation == BreakpointOrientation.Landscape && width <= height)
                return false;
            return true;
        }
    }

    public sealed class BreakpointRule
    {
        public BreakpointRule(string className, BreakpointQuery query, string selector = null)
        {
            ClassName = className;
            Query = query;
            Selector = selector;
        }

        public string ClassName { get; }

        public BreakpointQuery Query { get; }
        public string Selector { get; }
    }

    public sealed class BreakpointSet
    {
        public BreakpointSet(string root, IReadOnlyList<BreakpointRule> rules)
        {
            Root = root;
            Rules = rules;
        }

        public string Root { get; }

        public IReadOnlyList<BreakpointRule> Rules { get; }

        public static BreakpointSet Parse(string json)
        {
            return FromJson(MiniJson.Parse(json));
        }

        public static BreakpointSet FromJson(JsonValue root)
        {
            List<BreakpointRule> rules = new List<BreakpointRule>();
            JsonValue list = root["classes"];
            if (list.Kind != JsonKind.Array) throw new FormatException("Media spec requires a classes array.");
            for (int i = 0; i < list.Count; i++)
            {
                JsonValue entry = list[i];
                JsonValue q = entry["query"];
                if (q.Kind != JsonKind.Object) throw new FormatException("Media rule requires a query object.");
                BreakpointQuery query = new BreakpointQuery
                {
                    MinWidth = Optional(q, "minWidth"),
                    MaxWidth = Optional(q, "maxWidth"),
                    MinHeight = Optional(q, "minHeight"),
                    MaxHeight = Optional(q, "maxHeight"),
                    MinAspect = Optional(q, "minAspect"),
                    MaxAspect = Optional(q, "maxAspect"),
                    Orientation = ParseOrientation(q["orientation"].AsString())
                };
                ValidateRange(query.MinWidth, query.MaxWidth);
                ValidateRange(query.MinHeight, query.MaxHeight);
                ValidateRange(query.MinAspect, query.MaxAspect);
                string className = entry["class"].AsString();
                if (string.IsNullOrWhiteSpace(className)) throw new FormatException("Media rule requires a class.");
                rules.Add(new BreakpointRule(className, query, entry["selector"].AsString()));
            }

            return new BreakpointSet(root["root"].AsString(), rules);
        }

        public void Evaluate(double width, double height, List<string> active)
        {
            active.Clear();
            for (int i = 0; i < Rules.Count; i++)
            {
                if (Rules[i].Query.Matches(width, height))
                    active.Add(Rules[i].ClassName);
            }
        }

        public static bool SameSet(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static double? Optional(JsonValue query, string key)
        {
            JsonValue v = query[key];
            if (v.IsMissingOrNull) return null;
            double value = v.AsNumber(double.NaN);
            if (v.Kind != JsonKind.Number || double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new FormatException("Invalid media bound: " + key);
            return value;
        }

        private static void ValidateRange(double? minimum, double? maximum)
        {
            if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value) throw new FormatException("Media minimum exceeds maximum.");
        }

        private static BreakpointOrientation ParseOrientation(string text)
        {
            if (string.Equals(text, "portrait", StringComparison.Ordinal))
                return BreakpointOrientation.Portrait;
            if (string.Equals(text, "landscape", StringComparison.Ordinal))
                return BreakpointOrientation.Landscape;
            if (!string.IsNullOrEmpty(text) && text != "any") throw new FormatException("Unknown media orientation: " + text);
            return BreakpointOrientation.Any;
        }
    }
}
