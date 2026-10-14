using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToUitk.Core.Css
{
    public sealed class UssWriterOptions
    {
        public int Indent = 4;
        public bool SortDeclarations = true;
        public bool IncludeAtContextRules;
    }

    public static class UssPropertyOrder
    {
        private static readonly string[][] Groups =
        {
            new[] { "position", "left", "top", "right", "bottom", "display", "visibility", "overflow", "opacity" },
            new[] { "flex-direction", "flex-wrap", "flex-grow", "flex-shrink", "flex-basis", "flex", "align-content", "align-items", "align-self", "justify-content" },
            new[] { "width", "min-width", "max-width", "height", "min-height", "max-height", "aspect-ratio" },
            new[] { "margin", "margin-top", "margin-right", "margin-bottom", "margin-left" },
            new[] { "padding", "padding-top", "padding-right", "padding-bottom", "padding-left" },
            new[]
            {
                "border-width", "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
                "border-color", "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
                "border-radius", "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius"
            },
            new[]
            {
                "background-color", "background-image", "background-position", "background-position-x", "background-position-y",
                "background-repeat", "background-size", "-unity-background-scale-mode", "-unity-background-image-tint-color",
                "-unity-slice-left", "-unity-slice-top", "-unity-slice-right", "-unity-slice-bottom", "-unity-slice-scale", "-unity-slice-type"
            },
            new[]
            {
                "color", "-unity-font-definition", "-unity-font", "-unity-font-style", "font-size", "letter-spacing", "word-spacing",
                "-unity-text-align", "white-space", "text-overflow", "text-shadow", "-unity-text-outline", "-unity-text-outline-width", "-unity-text-outline-color"
            },
            new[] { "translate", "rotate", "scale", "transform-origin", "cursor", "filter", "backdrop-filter" },
            new[] { "transition-property", "transition-duration", "transition-timing-function", "transition-delay" }
        };

        private static readonly Dictionary<string, int> Index = BuildIndex();

        private static readonly KeyValuePair<string, string[]>[] Overlaps =
        {
            new KeyValuePair<string, string[]>("margin", new[] { "margin-top", "margin-right", "margin-bottom", "margin-left" }),
            new KeyValuePair<string, string[]>("padding", new[] { "padding-top", "padding-right", "padding-bottom", "padding-left" }),
            new KeyValuePair<string, string[]>("border-width", new[] { "border-top-width", "border-right-width", "border-bottom-width", "border-left-width" }),
            new KeyValuePair<string, string[]>("border-color", new[] { "border-top-color", "border-right-color", "border-bottom-color", "border-left-color" }),
            new KeyValuePair<string, string[]>("border-radius", new[] { "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius" }),
            new KeyValuePair<string, string[]>("flex", new[] { "flex-grow", "flex-shrink", "flex-basis" }),
            new KeyValuePair<string, string[]>("background-position", new[] { "background-position-x", "background-position-y" })
        };

        private static Dictionary<string, int> BuildIndex()
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int g = 0; g < Groups.Length; g++)
            {
                for (int i = 0; i < Groups[g].Length; i++) map[Groups[g][i]] = g * 100 + i;
            }
            return map;
        }

        public static int RankOf(string name)
        {
            if (name.StartsWith("--", StringComparison.Ordinal)) return -1;
            int rank;
            return Index.TryGetValue(name, out rank) ? rank : 9999;
        }

        public static IReadOnlyList<Declaration> Sort(IReadOnlyList<Declaration> declarations)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Declaration d in declarations) names.Add(d.Name);
            foreach (KeyValuePair<string, string[]> overlap in Overlaps)
            {
                if (!names.Contains(overlap.Key)) continue;
                foreach (string longhand in overlap.Value)
                {
                    if (names.Contains(longhand)) return declarations;
                }
            }
            var keyed = new List<KeyValuePair<int, Declaration>>(declarations.Count);
            for (int i = 0; i < declarations.Count; i++) keyed.Add(new KeyValuePair<int, Declaration>(i, declarations[i]));
            keyed.Sort((a, b) =>
            {
                int c = RankOf(a.Value.Name).CompareTo(RankOf(b.Value.Name));
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            var result = new List<Declaration>(keyed.Count);
            foreach (KeyValuePair<int, Declaration> item in keyed) result.Add(item.Value);
            return result;
        }
    }

    public static class UssWriter
    {
        public static string Write(Stylesheet sheet, UssWriterOptions options = null)
        {
            options = options ?? new UssWriterOptions();
            var blocks = new List<string>();
            foreach (StyleRule rule in sheet.Rules)
            {
                if (rule.AtContext.Count > 0 && !options.IncludeAtContextRules) continue;
                blocks.Add(WriteRule(rule, options));
            }
            return blocks.Count == 0 ? "" : string.Join("\n\n", blocks) + "\n";
        }

        public static string WriteRule(StyleRule rule, UssWriterOptions options = null)
        {
            options = options ?? new UssWriterOptions();
            string indent = new string(' ', Math.Max(0, options.Indent));
            var sb = new StringBuilder();
            for (int i = 0; i < rule.Selectors.Count; i++)
            {
                Selector selector = rule.Selectors[i];
                string text = selector.Text ?? SelectorParser.Format(selector);
                sb.Append(text);
                sb.Append(i < rule.Selectors.Count - 1 ? ",\n" : " {\n");
            }
            IReadOnlyList<Declaration> declarations = options.SortDeclarations ? UssPropertyOrder.Sort(rule.Declarations) : rule.Declarations;
            foreach (Declaration declaration in declarations)
            {
                sb.Append(indent).Append(declaration.Name).Append(": ").Append(declaration.Value.ToText());
                if (declaration.Important) sb.Append(" !important");
                sb.Append(";\n");
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
