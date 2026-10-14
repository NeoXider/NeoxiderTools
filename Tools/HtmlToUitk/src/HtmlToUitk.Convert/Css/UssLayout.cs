using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Convert.Css
{
    internal sealed class UssRule
    {
        public List<Selector> Selectors = new List<Selector>();
        public List<UssDecl> Decls = new List<UssDecl>();
        public int Order;
        public SourceSpan Span;
        public bool Token;
        public string AtContext;

        public UssRule Clone()
        {
            var clone = new UssRule { Order = Order, Span = Span, Token = Token, AtContext = AtContext };
            clone.Selectors.AddRange(Selectors);
            foreach (UssDecl d in Decls) clone.Decls.Add(new UssDecl(d.Name, d.Value, d.Span));
            return clone;
        }

        public string SelectorKey()
        {
            var parts = new string[Selectors.Count];
            for (int i = 0; i < Selectors.Count; i++) parts[i] = SelectorFormat.Text(Selectors[i]);
            return string.Join("\u0001", parts);
        }

        public string DeclKey()
        {
            var parts = new string[Decls.Count];
            for (int i = 0; i < Decls.Count; i++) parts[i] = Decls[i].Name + "\u0002" + Decls[i].Value;
            return string.Join("\u0001", parts);
        }
    }

    internal static class UssLayout
    {
        private static readonly string[][] GroupOrder =
        {
            new[] { "position", "left", "top", "right", "bottom", "display", "visibility", "overflow", "opacity" },
            new[] { "flex-direction", "flex-wrap", "flex-grow", "flex-shrink", "flex-basis", "flex", "align-content", "align-items", "align-self", "justify-content" },
            new[] { "width", "min-width", "max-width", "height", "min-height", "max-height", "aspect-ratio" },
            new[] { "margin", "margin-top", "margin-right", "margin-bottom", "margin-left" },
            new[] { "padding", "padding-top", "padding-right", "padding-bottom", "padding-left" },
            new[] { "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
                "border-color", "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
                "border-radius", "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius" },
            new[] { "background-color", "background-image", "background-position", "background-position-x", "background-position-y",
                "background-repeat", "background-size", "-unity-background-scale-mode", "-unity-background-image-tint-color",
                "-unity-slice-left", "-unity-slice-top", "-unity-slice-right", "-unity-slice-bottom", "-unity-slice-scale", "-unity-slice-type" },
            new[] { "color", "-unity-font-definition", "-unity-font", "-unity-font-style", "font-size", "letter-spacing", "word-spacing",
                "-unity-text-align", "white-space", "text-overflow", "text-shadow", "-unity-text-outline", "-unity-text-outline-width", "-unity-text-outline-color" },
            new[] { "translate", "rotate", "scale", "transform-origin", "cursor", "filter", "backdrop-filter" },
            new[] { "transition-property", "transition-duration", "transition-timing-function", "transition-delay" }
        };

        private static readonly Dictionary<string, KeyValuePair<int, int>> GroupIndex = BuildGroupIndex();

        private static readonly Dictionary<string, string[]> Shorthands = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "margin", new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" } },
            { "padding", new[] { "padding-top", "padding-right", "padding-bottom", "padding-left" } },
            { "border-width", new[] { "border-top-width", "border-right-width", "border-bottom-width", "border-left-width" } },
            { "border-color", new[] { "border-top-color", "border-right-color", "border-bottom-color", "border-left-color" } },
            { "border-radius", new[] { "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius" } },
            { "flex", new[] { "flex-grow", "flex-shrink", "flex-basis" } },
            { "background-position", new[] { "background-position-x", "background-position-y" } }
        };

        private static readonly HashSet<string> LayoutSet = new HashSet<string>(StringComparer.Ordinal)
        {
            "position", "left", "top", "right", "bottom", "display", "visibility", "overflow", "opacity", "flex", "flex-direction", "flex-wrap",
            "flex-grow", "flex-shrink", "flex-basis", "align-content", "align-items", "align-self", "justify-content", "width", "min-width",
            "max-width", "height", "min-height", "max-height", "aspect-ratio", "margin", "margin-top", "margin-right", "margin-bottom",
            "margin-left", "padding", "padding-top", "padding-right", "padding-bottom", "padding-left"
        };

        public const int SectionVariables = 0;
        public const int SectionLayout = 1;
        public const int SectionComponents = 2;
        public const int SectionStates = 3;
        public const int SectionUtilities = 4;

        private static Dictionary<string, KeyValuePair<int, int>> BuildGroupIndex()
        {
            var map = new Dictionary<string, KeyValuePair<int, int>>(StringComparer.Ordinal);
            for (int g = 0; g < GroupOrder.Length; g++)
            {
                for (int i = 0; i < GroupOrder[g].Length; i++) map[GroupOrder[g][i]] = new KeyValuePair<int, int>(g, i);
            }

            return map;
        }

        public static List<UssDecl> CanonicalOrder(List<UssDecl> decls)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (UssDecl d in decls) names.Add(d.Name);
            foreach (KeyValuePair<string, string[]> pair in Shorthands)
            {
                if (!names.Contains(pair.Key)) continue;
                foreach (string longhand in pair.Value)
                {
                    if (names.Contains(longhand)) return decls;
                }
            }

            var keyed = new List<KeyValuePair<long, UssDecl>>(decls.Count);
            for (int index = 0; index < decls.Count; index++)
            {
                UssDecl d = decls[index];
                long key;
                KeyValuePair<int, int> group;
                if (d.Name.StartsWith("--", StringComparison.Ordinal)) key = ((long)(-1 + 1) << 40) | (long)index;
                else if (!GroupIndex.TryGetValue(d.Name, out group)) key = ((long)(99 + 1) << 40) | (long)index;
                else key = ((long)(group.Key + 1) << 40) | ((long)group.Value << 20) | (long)index;
                keyed.Add(new KeyValuePair<long, UssDecl>(key, d));
            }

            keyed.Sort((a, b) => a.Key.CompareTo(b.Key));
            var result = new List<UssDecl>(keyed.Count);
            foreach (KeyValuePair<long, UssDecl> pair in keyed) result.Add(pair.Value);
            return result;
        }

        public static int Classify(UssRule rule, ConversionConfig config)
        {
            var utility = new Regex(config.Uss.UtilityPattern ?? "^u-", RegexOptions.CultureInvariant);
            string dynamicPattern = string.IsNullOrEmpty(config.Naming.DynamicClassPattern) ? "^$" : config.Naming.DynamicClassPattern;
            var dynamic = new Regex(dynamicPattern, RegexOptions.CultureInvariant);
            if (rule.Token && AllRoot(rule)) return SectionVariables;
            foreach (Selector selector in rule.Selectors)
            {
                for (int i = 0; i < selector.Parts.Count; i++)
                {
                    SimpleSelector compound = selector.Parts[i].Part;
                    if (compound.Pseudos.Count > 0 && !(selector.Parts.Count == 1 && compound.Pseudos[0].Name == "root")) return SectionStates;
                    foreach (string cls in compound.Classes)
                    {
                        if (dynamic.IsMatch(cls)) return SectionStates;
                    }
                }
            }

            bool allUtility = rule.Selectors.Count > 0;
            foreach (Selector selector in rule.Selectors)
            {
                if (!(selector.Parts.Count == 1 && selector.Parts[0].Part.Classes.Count == 1 && string.IsNullOrEmpty(selector.Parts[0].Part.Id)
                    && utility.IsMatch(selector.Parts[0].Part.Classes[0])))
                {
                    allUtility = false;
                    break;
                }
            }

            if (allUtility) return SectionUtilities;
            if (rule.Decls.Count > 0)
            {
                bool allLayout = true;
                foreach (UssDecl d in rule.Decls)
                {
                    if (!LayoutSet.Contains(d.Name))
                    {
                        allLayout = false;
                        break;
                    }
                }

                if (allLayout) return SectionLayout;
            }

            return SectionComponents;
        }

        private static bool AllRoot(UssRule rule)
        {
            foreach (Selector s in rule.Selectors)
            {
                if (SelectorFormat.Text(s) != ":root") return false;
            }

            return true;
        }

        private static bool Related(string a, string b)
        {
            if (a == b) return true;
            foreach (KeyValuePair<string, string[]> pair in Shorthands)
            {
                if (a == pair.Key && Array.IndexOf(pair.Value, b) >= 0) return true;
                if (b == pair.Key && Array.IndexOf(pair.Value, a) >= 0) return true;
            }

            return false;
        }

        private static bool Conflicts(UssRule a, UssRule b)
        {
            foreach (UssDecl da in a.Decls)
            {
                foreach (UssDecl db in b.Decls)
                {
                    if (Related(da.Name, db.Name)) return true;
                }
            }

            return false;
        }

        private static bool SameSpec(UssRule a, UssRule b)
        {
            foreach (Selector sa in a.Selectors)
            {
                foreach (Selector sb in b.Selectors)
                {
                    if (sa.Specificity.Equals(sb.Specificity)) return true;
                }
            }

            return false;
        }

        public static List<UssRule> Order(List<UssRule> rules, ConversionConfig config)
        {
            if (!config.Uss.SectionOrder) return new List<UssRule>(rules);
            var sections = new int[rules.Count];
            for (int i = 0; i < rules.Count; i++) sections[i] = Classify(rules[i], config);
            for (int a = 0; a < rules.Count; a++)
            {
                for (int b = a + 1; b < rules.Count; b++)
                {
                    if (sections[a] > sections[b] && Conflicts(rules[a], rules[b]) && SameSpec(rules[a], rules[b])) return new List<UssRule>(rules);
                }
            }

            var indices = new List<int>(rules.Count);
            for (int i = 0; i < rules.Count; i++) indices.Add(i);
            indices.Sort((x, y) =>
            {
                int c = sections[x].CompareTo(sections[y]);
                return c != 0 ? c : x.CompareTo(y);
            });
            var ordered = new List<UssRule>(rules.Count);
            foreach (int index in indices) ordered.Add(rules[index]);
            return ordered;
        }

        public static List<UssRule> Dedupe(List<UssRule> rules)
        {
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < rules.Count; index++) seen[rules[index].AtContext + "\u0003" + rules[index].SelectorKey() + "\u0004" + rules[index].DeclKey()] = index;
            var kept = new List<UssRule>();
            for (int index = 0; index < rules.Count; index++)
            {
                UssRule rule = rules[index];
                if (seen[rule.AtContext + "\u0003" + rule.SelectorKey() + "\u0004" + rule.DeclKey()] == index && rule.Decls.Count > 0) kept.Add(rule);
            }

            var merged = new List<UssRule>();
            foreach (UssRule rule in kept)
            {
                if (merged.Count > 0 && merged[merged.Count - 1].SelectorKey() == rule.SelectorKey() && merged[merged.Count - 1].AtContext == rule.AtContext)
                {
                    UssRule previous = merged[merged.Count - 1];
                    var combined = new List<UssDecl>();
                    foreach (UssDecl d in previous.Decls) combined.Add(d);
                    foreach (UssDecl d in rule.Decls)
                    {
                        combined.RemoveAll(x => x.Name == d.Name);
                        combined.Add(d);
                    }

                    previous.Decls = combined;
                }
                else merged.Add(rule.Clone());
            }

            return merged;
        }
    }
}
