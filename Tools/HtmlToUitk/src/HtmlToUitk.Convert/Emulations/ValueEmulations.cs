using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    internal static class UnitText
    {
        private static readonly Regex RelativeRe = new Regex(@"(?<![A-Za-z0-9_#.])([+-]?(?:\d+\.?\d*|\.\d+))(em|rem|ch|ex|pt|pc|in|cm|mm|q)(?![A-Za-z0-9_-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex ViewportRe = new Regex(@"(?<![A-Za-z0-9_#.])([+-]?(?:\d+\.?\d*|\.\d+))(vw|vh|vmin|vmax)(?![A-Za-z0-9_-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool HasRelative(string text)
        {
            return RelativeRe.IsMatch(Scrub(text));
        }

        public static bool HasViewport(string text)
        {
            return ViewportRe.IsMatch(Scrub(text));
        }

        private static string Scrub(string text)
        {
            return text.IndexOf("url(", StringComparison.OrdinalIgnoreCase) >= 0 ? Regex.Replace(text, @"url\([^)]*\)", "url()", RegexOptions.IgnoreCase) : text;
        }

        public static string ReplaceRelative(string text, double emBase, double rootBase, ref bool approximate)
        {
            bool approx = approximate;
            string result = RelativeRe.Replace(text, m =>
            {
                double number = double.Parse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                string unit = m.Groups[2].Value.ToLowerInvariant();
                double px;
                switch (unit)
                {
                    case "em": px = number * emBase; break;
                    case "rem": px = number * rootBase; break;
                    case "ch": px = number * emBase * 0.5; approx = true; break;
                    case "ex": px = number * emBase * 0.5; approx = true; break;
                    case "pt": px = number * 96.0 / 72.0; break;
                    case "pc": px = number * 16.0; break;
                    case "in": px = number * 96.0; break;
                    case "cm": px = number * 96.0 / 2.54; break;
                    case "mm": px = number * 96.0 / 25.4; break;
                    default: px = number * 96.0 / 101.6; break;
                }

                return CssText.FormatLength(px, "px");
            });
            approximate = approx;
            return result;
        }

        public static string ReplaceViewport(string text, double width, double height, bool fullBleed, string property)
        {
            return ViewportRe.Replace(text, m =>
            {
                double number = double.Parse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                string unit = m.Groups[2].Value.ToLowerInvariant();
                if (fullBleed && number == 100 && ((unit == "vw" && property == "width") || (unit == "vh" && property == "height"))) return "100%";
                double basis = unit == "vw" ? width : unit == "vh" ? height : unit == "vmin" ? Math.Min(width, height) : Math.Max(width, height);
                return CssText.FormatLength(number * basis / 100.0, "px");
            });
        }
    }

    internal static class FontSizes
    {
        public static double Computed(TransformState state, UxmlElement element)
        {
            var chain = new List<UxmlElement>();
            for (UxmlElement e = element; e != null; e = e.Parent) chain.Add(e);
            chain.Reverse();
            double root = state.Extras.RootFontSize;
            double current = root;
            foreach (UxmlElement e in chain)
            {
                ResolvedValue value = state.PropSource(e, "font-size");
                if (value == null || value.Inherited || value.Value == null) continue;
                current = Parse(value.Value.ToText().Trim(), current, root, current);
            }

            return current;
        }

        public static double Parse(string text, double parent, double root, double fallback)
        {
            double number;
            string unit;
            if (!CssText.TryNumber(text, out number, out unit)) return fallback;
            switch (unit)
            {
                case "px": return number;
                case "": return number == 0 ? 0 : fallback;
                case "em": return number * parent;
                case "rem": return number * root;
                case "%": return number * parent / 100.0;
                case "pt": return number * 96.0 / 72.0;
                default: return fallback;
            }
        }

        public static IReadOnlyList<UxmlElement> Targets(TransformState state, StyleRule rule)
        {
            var result = new List<UxmlElement>();
            foreach (Selector selector in rule.Selectors)
            {
                bool pseudoElement = false;
                foreach (var part in selector.Parts)
                {
                    if (part.Part.PseudoElements.Count > 0) pseudoElement = true;
                }

                if (pseudoElement) continue;
                foreach (UxmlElement el in state.MatchDom(selector))
                {
                    if (!result.Contains(el)) result.Add(el);
                }
            }

            return result;
        }
    }

    public sealed class RelativeUnitsEmulation : EmulationBase
    {
        public override string Id => "relative-units";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "unit:em", "unit:rem", "unit:ch", "unit:pt" };

        public override int Order => 20;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            double root = state.Extras.RootFontSize;
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool changed = false;
                var declarations = new List<Declaration>(rule.Declarations.Count);
                IReadOnlyList<UxmlElement> targets = null;
                foreach (Declaration d in rule.Declarations)
                {
                    string text = RuleOps.Text(d);
                    if (d.Name.StartsWith("--", StringComparison.Ordinal) || !UnitText.HasRelative(text))
                    {
                        declarations.Add(d);
                        continue;
                    }

                    if (targets == null) targets = FontSizes.Targets(state, rule);
                    double own = double.NaN;
                    Declaration fontDecl = RuleOps.Find(rule, "font-size");
                    double parentSize = targets.Count > 0 && targets[0].Parent != null ? FontSizes.Computed(state, targets[0].Parent) : root;
                    if (d.Name == "font-size") own = parentSize;
                    else if (fontDecl != null)
                    {
                        string fontText = RuleOps.Text(fontDecl);
                        own = FontSizes.Parse(fontText, parentSize, root, double.NaN);
                    }

                    if (double.IsNaN(own)) own = targets.Count > 0 ? FontSizes.Computed(state, targets[0]) : root;
                    bool approximate = false;
                    string rewritten = UnitText.ReplaceRelative(text, own, root, ref approximate);
                    if (rewritten == text)
                    {
                        declarations.Add(d);
                        continue;
                    }

                    changed = true;
                    declarations.Add(new Declaration { Name = d.Name, Value = new CssRaw(rewritten), Important = d.Important, Span = d.Span });
                }

                if (!changed) return null;
                return new[] { RuleOps.WithDeclarations(rule, declarations) };
            });
        }
    }

    public sealed class ViewportUnitsEmulation : EmulationBase
    {
        public override string Id => "viewport-units";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "unit:vw", "unit:vh", "unit:vmin", "unit:vmax" };

        public override int Order => 30;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            double width = state.Extras.ReferenceWidth;
            double height = state.Extras.ReferenceHeight;
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool changed = false;
                var declarations = new List<Declaration>(rule.Declarations.Count);
                bool? fullBleed = null;
                foreach (Declaration d in rule.Declarations)
                {
                    string text = RuleOps.Text(d);
                    if (d.Name.StartsWith("--", StringComparison.Ordinal) || !UnitText.HasViewport(text))
                    {
                        declarations.Add(d);
                        continue;
                    }

                    if (fullBleed == null)
                    {
                        IReadOnlyList<UxmlElement> targets = FontSizes.Targets(state, rule);
                        bool root = targets.Count > 0;
                        foreach (UxmlElement t in targets)
                        {
                            if (t.Parent != null) root = false;
                        }

                        fullBleed = root;
                    }

                    string rewritten = UnitText.ReplaceViewport(text, width, height, fullBleed.Value, d.Name);
                    if (rewritten == text)
                    {
                        declarations.Add(d);
                        continue;
                    }

                    changed = true;
                    declarations.Add(new Declaration { Name = d.Name, Value = new CssRaw(rewritten), Important = d.Important, Span = d.Span });
                }

                if (!changed) return null;
                return new[] { RuleOps.WithDeclarations(rule, declarations) };
            });
        }
    }

    public sealed class CalcFoldEmulation : EmulationBase
    {
        public override string Id => "calc-fold";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "value:calc", "value:min", "value:max", "value:clamp" };

        public override int Order => 40;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            RuleOps.MapRules(state.Sheets, (sheetId, rule) =>
            {
                bool changed = false;
                var declarations = new List<Declaration>(rule.Declarations.Count);
                foreach (Declaration d in rule.Declarations)
                {
                    string text = RuleOps.Text(d);
                    if (d.Name.StartsWith("--", StringComparison.Ordinal) || !CalcFold.ContainsMath(text))
                    {
                        declarations.Add(d);
                        continue;
                    }

                    bool folded, failed;
                    string rewritten = CalcFold.Fold(text, out folded, out failed);
                    if (failed)
                    {
                        changed = true;
                        continue;
                    }

                    if (rewritten == text)
                    {
                        declarations.Add(d);
                        continue;
                    }

                    changed = true;
                    declarations.Add(new Declaration { Name = d.Name, Value = new CssRaw(rewritten), Important = d.Important, Span = d.Span });
                }

                if (!changed) return null;
                return new[] { RuleOps.WithDeclarations(rule, declarations) };
            });
        }
    }

    public sealed class ShorthandExpandEmulation : EmulationBase
    {
        public override string Id => "shorthand-expand";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "prop:inset", "prop:flex-flow", "prop:border", "prop:place-items" };

        public override int Order => 10;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
        }
    }
}
