using System;
using System.Collections.Generic;
using System.Globalization;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Spec;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Convert.Dialect
{
    internal static class AnimCollector
    {
        private static readonly HashSet<string> Directions = new HashSet<string>(StringComparer.Ordinal) { "normal", "reverse", "alternate", "alternate-reverse" };
        private static readonly HashSet<string> Fills = new HashSet<string>(StringComparer.Ordinal) { "none", "forwards", "backwards", "both" };
        private static readonly HashSet<string> Triggers = new HashSet<string>(StringComparer.Ordinal) { "onShow", "onHide", "onLoad", "manual" };

        private sealed class Timing
        {
            public string Name;
            public double Duration;
            public double Delay;
            public int Iterations = 1;
            public string Direction = "normal";
            public string Fill = "none";
            public string Easing = "ease";
        }

        public static AnimModel Collect(PageState page, CapabilityMatrix caps, ConversionConfig config)
        {
            var keyframes = new Dictionary<string, AtRule>(StringComparer.Ordinal);
            var order = new List<string>();
            foreach (Stylesheet sheet in page.Cascade)
            {
                if (sheet.Origin == StyleOrigin.Skin || sheet.Origin == StyleOrigin.UssPassThrough) continue;
                foreach (AtRule at in sheet.AtRules)
                {
                    if (at.Name == null || !at.Name.EndsWith("keyframes", StringComparison.OrdinalIgnoreCase)) continue;
                    string name = (at.Prelude ?? "").Trim();
                    if (name.Length == 0) continue;
                    keyframes[name] = at;
                }
            }

            var env = new TranslateEnv { Caps = caps, Config = config, Fs = null, ProjectRoot = "", BaseDirectory = "" };
            var animations = new List<AnimDef>();
            var bindings = new List<AnimBinding>();
            var variants = new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.Ordinal);
            foreach (Stylesheet sheet in page.Cascade)
            {
                if (sheet.Origin == StyleOrigin.Skin || sheet.Origin == StyleOrigin.UssPassThrough) continue;
                foreach (StyleRule rule in sheet.Rules)
                {
                    if (rule.AtContext != null && rule.AtContext.Count > 0) continue;
                    List<Timing> timings = ReadTimings(rule);
                    if (timings.Count == 0) continue;
                    double stagger = 0;
                    string reduced = null;
                    string trigger = "onShow";
                    bool hasStagger = false;
                    foreach (Declaration d in rule.Declarations)
                    {
                        string text = d.Value == null ? "" : d.Value.ToText().Trim();
                        if (d.Name == "--anim-stagger")
                        {
                            double seconds;
                            if (TryTime(text, out seconds))
                            {
                                stagger = seconds;
                                hasStagger = true;
                            }
                        }
                        else if (d.Name == "--anim-reduced-motion") reduced = text.ToLowerInvariant();
                        else if (d.Name == "--anim-trigger" && Triggers.Contains(text)) trigger = text;
                    }

                    foreach (Timing timing in timings)
                    {
                        AtRule at;
                        if (!keyframes.TryGetValue(timing.Name, out at))
                        {
                            foreach (Selector selector in rule.Selectors)
                            {
                                bindings.Add(new AnimBinding { Selector = SelectorFormat.Text(selector), Animation = timing.Name, Trigger = trigger, Stagger = stagger, ReducedMotion = reduced });
                            }

                            continue;
                        }

                        string id = timing.Name;
                        string signature = timing.Duration.ToString("R", CultureInfo.InvariantCulture) + "|" + timing.Delay.ToString("R", CultureInfo.InvariantCulture) + "|" + timing.Iterations + "|" + timing.Direction + "|" + timing.Fill + "|" + timing.Easing;
                        List<KeyValuePair<string, string>> list;
                        if (!variants.TryGetValue(timing.Name, out list))
                        {
                            list = new List<KeyValuePair<string, string>>();
                            variants[timing.Name] = list;
                        }

                        string existing = null;
                        foreach (KeyValuePair<string, string> v in list)
                        {
                            if (v.Value == signature) existing = v.Key;
                        }

                        if (existing == null)
                        {
                            id = list.Count == 0 ? timing.Name : timing.Name + "-" + (list.Count + 1).ToString(CultureInfo.InvariantCulture);
                            list.Add(new KeyValuePair<string, string>(id, signature));
                            animations.Add(new AnimDef
                            {
                                Id = id,
                                Keyframes = timing.Name,
                                Duration = timing.Duration,
                                Delay = timing.Delay,
                                Iterations = timing.Iterations,
                                Direction = timing.Direction,
                                Fill = timing.Fill,
                                Easing = timing.Easing,
                                Steps = Steps(at, env)
                            });
                        }
                        else id = existing;

                        foreach (Selector selector in rule.Selectors)
                        {
                            bindings.Add(new AnimBinding
                            {
                                Selector = SelectorFormat.Text(selector),
                                Animation = id,
                                Trigger = trigger,
                                Stagger = hasStagger ? stagger : 0,
                                ReducedMotion = reduced
                            });
                        }
                    }
                }
            }

            return new AnimModel { Animations = animations, Bindings = bindings };
        }

        private static IReadOnlyList<AnimStep> Steps(AtRule at, TranslateEnv env)
        {
            var steps = new List<KeyValuePair<double, AnimStep>>();
            foreach (StyleRule block in at.Rules)
            {
                List<UssDecl> decls = DeclTranslator.Translate(block.Declarations, env);
                var set = new SortedDictionary<string, string>(StringComparer.Ordinal);
                string easing = null;
                foreach (UssDecl d in decls)
                {
                    if (d.Name == "transition-timing-function") easing = d.Value;
                    else if (!d.Name.StartsWith("transition", StringComparison.Ordinal)) set[d.Name] = d.Value;
                }

                foreach (Selector selector in block.Selectors)
                {
                    double offset;
                    if (!TryOffset(selector.Text, out offset)) continue;
                    steps.Add(new KeyValuePair<double, AnimStep>(offset, new AnimStep { Offset = offset, Easing = easing, Set = new Dictionary<string, string>(set) }));
                }
            }

            var indexed = new List<KeyValuePair<int, KeyValuePair<double, AnimStep>>>();
            for (int i = 0; i < steps.Count; i++) indexed.Add(new KeyValuePair<int, KeyValuePair<double, AnimStep>>(i, steps[i]));
            indexed.Sort((a, b) =>
            {
                int c = a.Value.Key.CompareTo(b.Value.Key);
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            var result = new List<AnimStep>();
            foreach (KeyValuePair<int, KeyValuePair<double, AnimStep>> item in indexed) result.Add(item.Value.Value);
            return result;
        }

        private static bool TryOffset(string text, out double offset)
        {
            offset = 0;
            string t = (text ?? "").Trim().ToLowerInvariant();
            if (t == "from") return true;
            if (t == "to")
            {
                offset = 1;
                return true;
            }

            if (t.EndsWith("%", StringComparison.Ordinal))
            {
                double value;
                if (double.TryParse(t.Substring(0, t.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    offset = Math.Round(value / 100.0, 4);
                    return true;
                }
            }

            return false;
        }

        private static bool TryTime(string text, out double seconds)
        {
            seconds = 0;
            double n;
            string unit;
            if (!CssText.TryNumber(text, out n, out unit)) return false;
            if (unit == "s") seconds = n;
            else if (unit == "ms") seconds = n / 1000.0;
            else return false;
            return true;
        }

        private static List<Timing> ReadTimings(StyleRule rule)
        {
            var result = new List<Timing>();
            Declaration shorthand = null;
            var longhands = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (Declaration d in rule.Declarations)
            {
                string text = d.Value == null ? "" : d.Value.ToText().Trim();
                if (d.Name == "animation") shorthand = d;
                else if (d.Name.StartsWith("animation-", StringComparison.Ordinal)) longhands[d.Name] = CssText.SplitTop(text, ',').ConvertAll(p => p.Trim());
            }

            if (shorthand != null)
            {
                string text = shorthand.Value.ToText().Trim();
                foreach (string item in CssText.SplitTop(text, ','))
                {
                    Timing timing = ParseShorthand(item.Trim());
                    if (timing != null) result.Add(timing);
                }
            }

            if (result.Count == 0 && longhands.ContainsKey("animation-name"))
            {
                List<string> names = longhands["animation-name"];
                for (int i = 0; i < names.Count; i++)
                {
                    var timing = new Timing { Name = names[i] };
                    string value;
                    if (Pick(longhands, "animation-duration", i, out value)) TryTime(value, out timing.Duration);
                    if (Pick(longhands, "animation-delay", i, out value)) TryTime(value, out timing.Delay);
                    if (Pick(longhands, "animation-iteration-count", i, out value)) timing.Iterations = Iterations(value);
                    if (Pick(longhands, "animation-direction", i, out value) && Directions.Contains(value)) timing.Direction = value;
                    if (Pick(longhands, "animation-fill-mode", i, out value) && Fills.Contains(value)) timing.Fill = value;
                    if (Pick(longhands, "animation-timing-function", i, out value)) timing.Easing = value.ToLowerInvariant();
                    result.Add(timing);
                }
            }

            return result;
        }

        private static bool Pick(Dictionary<string, List<string>> map, string key, int index, out string value)
        {
            value = null;
            List<string> list;
            if (!map.TryGetValue(key, out list) || list.Count == 0) return false;
            value = list[index % list.Count];
            return true;
        }

        private static int Iterations(string text)
        {
            string t = text.Trim().ToLowerInvariant();
            if (t == "infinite") return -1;
            int n;
            return int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n >= 1 ? n : 1;
        }

        private static Timing ParseShorthand(string item)
        {
            var timing = new Timing();
            var times = new List<double>();
            bool nameSet = false;
            foreach (string token in CssText.SplitWs(item))
            {
                string low = token.ToLowerInvariant();
                double seconds;
                if (TryTime(low, out seconds))
                {
                    times.Add(seconds);
                    continue;
                }

                if (low == "infinite")
                {
                    timing.Iterations = -1;
                    continue;
                }

                double number;
                string unit;
                if (CssText.TryNumber(low, out number, out unit) && unit == "")
                {
                    timing.Iterations = number >= 1 ? (int)number : 1;
                    continue;
                }

                if (Directions.Contains(low))
                {
                    timing.Direction = low;
                    continue;
                }

                if (Fills.Contains(low) && low != "none")
                {
                    timing.Fill = low;
                    continue;
                }

                if (low == "running" || low == "paused") continue;
                if (low.StartsWith("cubic-bezier(", StringComparison.Ordinal) || low.StartsWith("steps(", StringComparison.Ordinal) || UssSpec.IsEasing(low))
                {
                    timing.Easing = low;
                    continue;
                }

                if (low == "none" && !nameSet) continue;
                if (!nameSet)
                {
                    timing.Name = token;
                    nameSet = true;
                }
            }

            if (!nameSet) return null;
            if (times.Count > 0) timing.Duration = times[0];
            if (times.Count > 1) timing.Delay = times[1];
            return timing;
        }
    }
}
